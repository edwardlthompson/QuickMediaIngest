#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Writes keywords for Windows search (EXIF / IPTC) and Lightroom (XMP / sidecar).
    /// </summary>
    public static partial class MetadataKeywordWriter
    {
        private static readonly HashSet<string> RawExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dng", ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".rw2", ".srw"
        };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mov", ".m4v", ".avi", ".wmv", ".mkv", ".3gp", ".mts", ".m2ts"
        };

        /// <summary>
        /// Applies keywords to the destination file when possible; always falls back to an XMP sidecar for formats
        /// that cannot be safely embedded.
        /// </summary>
        public static void TryApplyKeywords(string destinationFilePath, IReadOnlyList<string>? keywords, ILogger? logger = null) =>
            TryApplyKeywords(destinationFilePath, keywords, stripGpsAndPii: false, logger);

        /// <summary>
        /// Applies keywords and optionally strips GPS / location tags from EXIF / XMP.
        /// </summary>
        public static void TryApplyKeywords(string destinationFilePath, IReadOnlyList<string>? keywords, bool stripGpsAndPii, ILogger? logger = null)
        {
            var list = NormalizeKeywords(keywords);
            if (list.Count == 0 && !stripGpsAndPii)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(destinationFilePath) || !File.Exists(destinationFilePath))
            {
                return;
            }

            try
            {
                string ext = Path.GetExtension(destinationFilePath);

                if (RawExtensions.Contains(ext) || VideoExtensions.Contains(ext))
                {
                    if (list.Count > 0)
                    {
                        WriteXmpSidecar(destinationFilePath, list, logger);
                    }
                    return;
                }

                if (TryMagickEmbed(destinationFilePath, list, stripGpsAndPii, logger))
                {
                    return;
                }

                if (list.Count > 0)
                {
                    WriteXmpSidecar(destinationFilePath, list, logger);
                }
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Keyword / privacy write failed for {Path}.", destinationFilePath);
            }
        }

        /// <summary>
        /// Strips GPS and location tags from media metadata without adding keywords.
        /// </summary>
        public static void TryStripGpsAndPii(string destinationFilePath, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(destinationFilePath) || !File.Exists(destinationFilePath))
            {
                return;
            }

            try
            {
                string ext = Path.GetExtension(destinationFilePath);
                if (RawExtensions.Contains(ext) || VideoExtensions.Contains(ext))
                {
                    return;
                }

                TryMagickEmbed(destinationFilePath, new List<string>(), stripGpsAndPii: true, logger);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "GPS/PII strip failed for {Path}.", destinationFilePath);
            }
        }

        private static List<string> NormalizeKeywords(IReadOnlyList<string>? keywords)
        {
            if (keywords == null || keywords.Count == 0)
            {
                return new List<string>();
            }

            return keywords
                .Select(k => k.Trim())
                .Where(k => k.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TryMagickEmbed(string path, List<string> keywords, bool stripGpsAndPii, ILogger? logger)
        {
            string ext = Path.GetExtension(path);
            if (ext.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".heif", StringComparison.OrdinalIgnoreCase))
            {
                // Delegate / codec variance: prefer sidecar for reliability.
                return false;
            }

            try
            {
                using var image = new MagickImage(path);

                if (stripGpsAndPii)
                {
                    var profile = image.GetExifProfile();
                    if (profile != null)
                    {
                        var gpsTags = profile.Values
                            .Where(v => v.Tag.ToString().StartsWith("GPS", StringComparison.OrdinalIgnoreCase))
                            .Select(v => v.Tag)
                            .ToList();
                        if (gpsTags.Count > 0)
                        {
                            foreach (var tag in gpsTags)
                            {
                                profile.RemoveValue(tag);
                            }
                            image.RemoveProfile("exif");
                            byte[] bytes = profile.ToByteArray();
                            if (bytes != null && bytes.Length > 0)
                            {
                                image.SetProfile(new ExifProfile(bytes));
                            }
                        }
                    }
                }

                if (keywords.Count > 0)
                {
                    // Windows Explorer often surfaces "Tags" from EXIF XPKeywords (semicolon-separated).
                    string xp = string.Join("; ", keywords);
                    image.SetAttribute("exif:XPKeywords", xp);
                    var iptc = image.GetIptcProfile() ?? new IptcProfile();
                    foreach (var kw in keywords)
                    {
                        iptc.SetValue(IptcTag.Keyword, kw);
                    }
                    image.SetProfile(iptc);
                }

                image.Write(path);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Magick keyword embed failed for {Path}; will try sidecar.", path);
                return false;
            }
        }
    }
}
