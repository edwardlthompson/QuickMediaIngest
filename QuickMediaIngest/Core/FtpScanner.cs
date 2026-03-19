using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    public class FtpScanProgress
    {
        public string Phase { get; set; } = "Prescan";
        public string CurrentFolder { get; set; } = "/";
        public int ProcessedFolders { get; set; }
        public int TotalFolders { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public int CurrentFolderProcessedFiles { get; set; }
        public int CurrentFolderTotalFiles { get; set; }
        public int SkippedFolders { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class FtpScanner
    {
        private static readonly Regex UnixListRegex = new Regex(
            "^(?<type>[dl-])[rwxstST-]{9}\\s+\\d+\\s+\\S+\\s+\\S+\\s+(?<size>\\d+)\\s+(?<month>[A-Za-z]{3})\\s+(?<day>\\d{1,2})\\s+(?<timeyear>[0-9:]{4,5}|\\d{4})\\s+(?<name>.+)$",
            RegexOptions.Compiled);

        private static readonly Regex DosListRegex = new Regex(
            "^(?<date>\\d{2}-\\d{2}-\\d{2})\\s+(?<time>\\d{2}:\\d{2}[AP]M)\\s+(?<dir><DIR>|\\d+)\\s+(?<name>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public async Task<List<string>> ListDirectoriesAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds = 15,
            CancellationToken cancellationToken = default)
        {
            string normalizedPath = NormalizeRemotePath(remotePath);
            var entries = await ListDirectoryEntriesAsync(
                host,
                port,
                user,
                pass,
                normalizedPath,
                timeoutSeconds,
                cancellationToken);

            return entries
                .Where(e => e.IsDirectory)
                .Select(e => e.FullPath)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            int timeoutSeconds = 15,
            CancellationToken cancellationToken = default)
        {
            string normalizedPath = NormalizeRemotePath(remotePath);

            try
            {
                _ = await ListDirectoryEntriesAsync(
                    host,
                    port,
                    user,
                    pass,
                    normalizedPath,
                    timeoutSeconds,
                    cancellationToken);

                return (true, $"Connected to {host}:{port} and listed folder {normalizedPath}");
            }
            catch (OperationCanceledException)
            {
                return (false, "Connection was canceled.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<List<ImportItem>> ScanAsync(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            bool includeSubfolders,
            int timeoutSeconds = 20,
            CancellationToken cancellationToken = default,
            Action<FtpScanProgress>? progressCallback = null)
        {
            var items = new List<ImportItem>();
            string normalizedPath = NormalizeRemotePath(remotePath);

            List<FtpFolderScanPlan> plans = await BuildScanPlanAsync(
                host,
                port,
                user,
                pass,
                normalizedPath,
                includeSubfolders,
                timeoutSeconds,
                cancellationToken,
                progressCallback);

            int totalFolders = plans.Count;
            int totalFiles = plans.Sum(p => p.Files.Count);
            int scannedFolders = 0;
            int scannedFiles = 0;
            int skippedFolders = plans.Count(p => p.IsSkipped);

            foreach (var plan in plans)
            {
                int currentFolderProcessedFiles = 0;
                int currentFolderTotalFiles = plan.Files.Count;

                foreach (var file in plan.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    items.Add(new ImportItem
                    {
                        SourcePath = file.FullPath,
                        FileName = file.Name,
                        FileSize = file.Size,
                        DateTaken = file.Modified,
                        IsVideo = IsVideoFile(file.Name),
                        FileType = Path.GetExtension(file.Name).TrimStart('.').ToUpperInvariant()
                    });

                    scannedFiles++;
                    currentFolderProcessedFiles++;
                    if (scannedFiles == 1 || scannedFiles % 25 == 0 || scannedFiles == totalFiles)
                    {
                        progressCallback?.Invoke(new FtpScanProgress
                        {
                            Phase = "Scanning",
                            CurrentFolder = plan.Folder,
                            ProcessedFolders = scannedFolders,
                            TotalFolders = totalFolders,
                            ProcessedFiles = scannedFiles,
                            TotalFiles = totalFiles,
                            CurrentFolderProcessedFiles = currentFolderProcessedFiles,
                            CurrentFolderTotalFiles = currentFolderTotalFiles,
                            SkippedFolders = skippedFolders,
                            Note = plan.IsSkipped ? plan.SkipReason : string.Empty
                        });
                    }
                }

                scannedFolders++;
                progressCallback?.Invoke(new FtpScanProgress
                {
                    Phase = "Scanning",
                    CurrentFolder = plan.Folder,
                    ProcessedFolders = scannedFolders,
                    TotalFolders = totalFolders,
                    ProcessedFiles = scannedFiles,
                    TotalFiles = totalFiles,
                    CurrentFolderProcessedFiles = currentFolderProcessedFiles,
                    CurrentFolderTotalFiles = currentFolderTotalFiles,
                    SkippedFolders = skippedFolders,
                    Note = plan.IsSkipped ? plan.SkipReason : string.Empty
                });
            }

            return items;
        }

        private static async Task<List<FtpFolderScanPlan>> BuildScanPlanAsync(
            string host,
            int port,
            string user,
            string pass,
            string rootPath,
            bool includeSubfolders,
            int timeoutSeconds,
            CancellationToken cancellationToken,
            Action<FtpScanProgress>? progressCallback)
        {
            var queue = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var plans = new List<FtpFolderScanPlan>();

            string normalizedRoot = NormalizeRemotePath(rootPath);
            queue.Enqueue(normalizedRoot);
            visited.Add(normalizedRoot);

            int processedFolders = 0;
            int discoveredFolders = 1;
            int discoveredFiles = 0;
            int skippedFolders = 0;

            while (queue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string current = queue.Dequeue();

                progressCallback?.Invoke(new FtpScanProgress
                {
                    Phase = "Prescan",
                    CurrentFolder = current,
                    ProcessedFolders = processedFolders,
                    TotalFolders = discoveredFolders,
                    ProcessedFiles = 0,
                    TotalFiles = discoveredFiles,
                    SkippedFolders = skippedFolders,
                    Note = "Listing folder..."
                });

                List<FtpEntry> entries;
                bool folderSkipped = false;
                string skipReason = string.Empty;

                try
                {
                    entries = await ListDirectoryEntriesAsync(
                        host,
                        port,
                        user,
                        pass,
                        current,
                        timeoutSeconds,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (string.Equals(current, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Failed to list starting folder {current}: {ex.Message}", ex);
                    }

                    entries = new List<FtpEntry>();
                    folderSkipped = true;
                    skippedFolders++;
                    skipReason = $"Failed to list folder after retries: {ex.Message}";
                }

                var files = new List<FtpScanFile>();
                foreach (var entry in entries)
                {
                    if (!entry.IsDirectory)
                    {
                        files.Add(new FtpScanFile
                        {
                            FullPath = entry.FullPath,
                            Name = entry.Name,
                            Size = entry.Size,
                            Modified = entry.Modified
                        });
                        continue;
                    }

                    if (!includeSubfolders)
                    {
                        continue;
                    }

                    string folder = NormalizeRemotePath(entry.FullPath);
                    if (visited.Add(folder))
                    {
                        queue.Enqueue(folder);
                        discoveredFolders++;
                    }
                }

                discoveredFiles += files.Count;
                processedFolders++;
                plans.Add(new FtpFolderScanPlan
                {
                    Folder = current,
                    Files = files,
                    IsSkipped = folderSkipped,
                    SkipReason = skipReason
                });

                progressCallback?.Invoke(new FtpScanProgress
                {
                    Phase = "Prescan",
                    CurrentFolder = current,
                    ProcessedFolders = processedFolders,
                    TotalFolders = discoveredFolders,
                    ProcessedFiles = 0,
                    TotalFiles = discoveredFiles,
                    SkippedFolders = skippedFolders,
                    Note = skipReason
                });
            }

            return plans;
        }

        private static async Task<List<FtpEntry>> ListDirectoryEntriesAsync(
            string host,
            int port,
            string user,
            string pass,
            string path,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            Exception? lastError = null;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await Task.Run(() =>
                        ListDirectoryEntriesSync(host, port, user, pass, path, timeoutSeconds), cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    lastError = ex;
                }

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), cancellationToken);
                }
            }

            throw lastError ?? new InvalidOperationException($"Unable to list FTP folder {path}");
        }

        private static List<FtpEntry> ListDirectoryEntriesSync(
            string host,
            int port,
            string user,
            string pass,
            string path,
            int timeoutSeconds)
        {
            var uri = BuildFtpUri(host, port, path);

#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(user, pass);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Timeout = Math.Max(5, timeoutSeconds) * 1000;
            request.ReadWriteTimeout = Math.Max(5, timeoutSeconds) * 1000;

            using var response = (FtpWebResponse)request.GetResponse();
            using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream ?? Stream.Null);

            string raw = reader.ReadToEnd();
            string[] lines = raw
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var entries = new List<FtpEntry>();
            foreach (string line in lines)
            {
                if (!TryParseListingLine(line, path, out FtpEntry? entry) || entry == null)
                {
                    continue;
                }

                entries.Add(entry);
            }

            return entries;
        }

        private static bool TryParseListingLine(string line, string parentPath, out FtpEntry? entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            Match unixMatch = UnixListRegex.Match(line);
            if (unixMatch.Success)
            {
                string name = unixMatch.Groups["name"].Value.Trim();
                if (name == "." || name == "..")
                {
                    return false;
                }

                bool isDirectory = string.Equals(unixMatch.Groups["type"].Value, "d", StringComparison.OrdinalIgnoreCase);
                long.TryParse(unixMatch.Groups["size"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long size);
                DateTime modified = ParseUnixModified(unixMatch.Groups["month"].Value, unixMatch.Groups["day"].Value, unixMatch.Groups["timeyear"].Value);

                entry = new FtpEntry
                {
                    Name = name,
                    FullPath = CombineRemotePath(parentPath, name),
                    IsDirectory = isDirectory,
                    Size = isDirectory ? 0 : size,
                    Modified = modified
                };
                return true;
            }

            Match dosMatch = DosListRegex.Match(line);
            if (dosMatch.Success)
            {
                string name = dosMatch.Groups["name"].Value.Trim();
                if (name == "." || name == "..")
                {
                    return false;
                }

                bool isDirectory = string.Equals(dosMatch.Groups["dir"].Value, "<DIR>", StringComparison.OrdinalIgnoreCase);
                long size = 0;
                if (!isDirectory)
                {
                    long.TryParse(dosMatch.Groups["dir"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out size);
                }

                DateTime modified = DateTime.Now;
                string dateTimeText = $"{dosMatch.Groups["date"].Value} {dosMatch.Groups["time"].Value}";
                if (DateTime.TryParseExact(
                    dateTimeText,
                    "MM-dd-yy hh:mmtt",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime parsed))
                {
                    modified = parsed;
                }

                entry = new FtpEntry
                {
                    Name = name,
                    FullPath = CombineRemotePath(parentPath, name),
                    IsDirectory = isDirectory,
                    Size = isDirectory ? 0 : size,
                    Modified = modified
                };
                return true;
            }

            return false;
        }

        private static DateTime ParseUnixModified(string month, string day, string timeOrYear)
        {
            string dayText = day.PadLeft(2, '0');
            if (timeOrYear.Contains(':'))
            {
                string text = $"{month} {dayText} {DateTime.Now.Year} {timeOrYear}";
                if (DateTime.TryParseExact(
                    text,
                    "MMM dd yyyy HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime parsedWithTime))
                {
                    return parsedWithTime;
                }
            }
            else
            {
                string text = $"{month} {dayText} {timeOrYear}";
                if (DateTime.TryParseExact(
                    text,
                    "MMM dd yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out DateTime parsedWithYear))
                {
                    return parsedWithYear;
                }
            }

            return DateTime.Now;
        }

        private static Uri BuildFtpUri(string host, int port, string path)
        {
            string normalized = NormalizeRemotePath(path);
            string encodedPath = string.Join("/", normalized
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));

            string uriText = string.IsNullOrEmpty(encodedPath)
                ? $"ftp://{host}:{port}/"
                : $"ftp://{host}:{port}/{encodedPath}";

            return new Uri(uriText);
        }

        private static string CombineRemotePath(string parent, string child)
        {
            string normalizedParent = NormalizeRemotePath(parent).TrimEnd('/');
            string normalizedChild = child.Replace("\\", "/").Trim('/');
            if (string.IsNullOrEmpty(normalizedChild))
            {
                return normalizedParent;
            }

            if (string.IsNullOrEmpty(normalizedParent) || normalizedParent == "/")
            {
                return "/" + normalizedChild;
            }

            return normalizedParent + "/" + normalizedChild;
        }

        private static string NormalizeRemotePath(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath)) return "/";

            string normalized = remotePath.Trim().Replace("\\", "/");
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }

            return normalized;
        }

        private static bool IsVideoFile(string name)
        {
            string ext = Path.GetExtension(name).ToLowerInvariant();
            return ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".mkv";
        }

        private sealed class FtpEntry
        {
            public string Name { get; set; } = string.Empty;
            public string FullPath { get; set; } = "/";
            public bool IsDirectory { get; set; }
            public long Size { get; set; }
            public DateTime Modified { get; set; } = DateTime.Now;
        }

        private sealed class FtpScanFile
        {
            public string FullPath { get; set; } = "/";
            public string Name { get; set; } = string.Empty;
            public long Size { get; set; }
            public DateTime Modified { get; set; } = DateTime.Now;
        }

        private sealed class FtpFolderScanPlan
        {
            public string Folder { get; set; } = "/";
            public List<FtpScanFile> Files { get; set; } = new List<FtpScanFile>();
            public bool IsSkipped { get; set; }
            public string SkipReason { get; set; } = string.Empty;
        }
    }
}
