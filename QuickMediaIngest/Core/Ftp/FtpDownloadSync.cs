#nullable enable
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace QuickMediaIngest.Core
{
    /// <summary>Synchronous capped FTP download via FtpWebRequest (shared with directory listing stack).</summary>
    internal static class FtpDownloadSync
    {
        internal static bool DownloadCapped(
            string host,
            int port,
            string user,
            string pass,
            string remotePath,
            string localPath,
            long maxBytes,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            Uri uri = FtpUriBuilder.Build(host, port, remotePath);
            int timeoutMs = Math.Max(5, timeoutSeconds) * 1000;

#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(user, pass);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Timeout = timeoutMs;
            request.ReadWriteTimeout = timeoutMs;

            using var response = (FtpWebResponse)request.GetResponse();
            using var source = response.GetResponseStream();
            if (source == null)
            {
                return false;
            }

            using var dest = File.Create(localPath);
            byte[] buffer = new byte[65536];
            long totalBytes = 0;

            while (true)
            {
                int read = source.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (totalBytes + read > maxBytes)
                {
                    int allowed = (int)Math.Min(read, maxBytes - totalBytes);
                    if (allowed > 0)
                    {
                        dest.Write(buffer, 0, allowed);
                        totalBytes += allowed;
                    }

                    break;
                }

                dest.Write(buffer, 0, read);
                totalBytes += read;
            }

            return totalBytes > 0;
        }
    }
}
