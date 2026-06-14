#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    internal static class FtpDirectoryClient
    {
        public static async Task<List<FtpListingEntry>> ListDirectoryEntriesAsync(
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
                    return await Task.Run(
                        () => ListDirectoryEntriesSync(host, port, user, pass, path, timeoutSeconds),
                        cancellationToken);
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

        private static List<FtpListingEntry> ListDirectoryEntriesSync(
            string host,
            int port,
            string user,
            string pass,
            string path,
            int timeoutSeconds)
        {
            var uri = FtpUriBuilder.Build(host, port, path);

#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(user, pass);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = true;
            request.Timeout = Math.Max(5, timeoutSeconds) * 1000;
            request.ReadWriteTimeout = Math.Max(5, timeoutSeconds) * 1000;

            using var response = (FtpWebResponse)request.GetResponse();
            using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream ?? Stream.Null);

            string raw = reader.ReadToEnd();
            string[] lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var entries = new List<FtpListingEntry>();
            foreach (string line in lines)
            {
                if (FtpListingParser.TryParseListingLine(line, path, out FtpListingEntry? entry) && entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

    }
}
