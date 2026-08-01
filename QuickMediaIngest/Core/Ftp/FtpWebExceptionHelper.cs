#nullable enable
using System;
using System.Net;

namespace QuickMediaIngest.Core
{
    /// <summary>Classifies FTP WebExceptions for retry vs fail-fast.</summary>
    internal static class FtpWebExceptionHelper
    {
        /// <summary>True for permanent “file unavailable” style errors (550 and similar 5xx).</summary>
        public static bool IsPermanentFileUnavailable(Exception? ex)
        {
            for (Exception? current = ex; current != null; current = current.InnerException)
            {
                if (current is WebException web)
                {
                    if (web.Response is FtpWebResponse ftp)
                    {
                        int code = (int)ftp.StatusCode;
                        // 550 File unavailable, 553 Illegal filename, 501 Syntax — do not retry.
                        if (code is 550 or 553 or 501)
                        {
                            return true;
                        }

                        string desc = ftp.StatusDescription ?? string.Empty;
                        if (desc.Contains("550", StringComparison.Ordinal) ||
                            desc.Contains("File unavailable", StringComparison.OrdinalIgnoreCase) ||
                            desc.Contains("No such file", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    string message = web.Message ?? string.Empty;
                    if (message.Contains("550", StringComparison.Ordinal) ||
                        message.Contains("File unavailable", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                string text = current.Message ?? string.Empty;
                if (text.Contains("550", StringComparison.Ordinal) ||
                    text.Contains("File unavailable", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string Describe(Exception? ex)
        {
            if (ex == null)
            {
                return "unknown";
            }

            for (Exception? current = ex; current != null; current = current.InnerException)
            {
                if (current is WebException web && web.Response is FtpWebResponse ftp)
                {
                    return $"{(int)ftp.StatusCode} {ftp.StatusDescription}".Trim();
                }
            }

            return ex.Message;
        }
    }
}
