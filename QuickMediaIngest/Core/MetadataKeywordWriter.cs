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
    public static class MetadataKeywordWriter
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
        public static void TryApplyKeywords(string destinationFilePath, IReadOnlyList<string>? keywords, ILogger? logger = null)
        {
            var list = NormalizeKeywords(keywords);
            if (list.Count == 0 || string.IsNullOrWhiteSpace(destinationFilePath) || !File.Exists(destinationFilePath))
            {
                return;
            }

            try
            {
                string ext = Path.GetExtension(destinationFilePath);

                if (RawExtensions.Contains(ext) || VideoExtensions.Contains(ext))
                {
                    WriteXmpSidecar(destinationFilePath, list, logger);
                    return;
                }

                if (TryMagickEmbed(destinationFilePath, list, logger))
                {
                    return;
                }

                WriteXmpSidecar(destinationFilePath, list, logger);
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Keyword write failed for {Path}.", destinationFilePath);
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

        private static bool TryMagickEmbed(string path, List<string> keywords, ILogger? logger)
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
                // Windows Explorer often surfaces "Tags" from EXIF XPKeywords (semicolon-separated).
                string xp = string.Join("; ", keywords);
                image.SetAttribute("exif:XPKeywords", xp);

                image.Write(path);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Magick keyword embed failed for {Path}; will try sidecar.", path);
                return false;
            }
        }

        private static void WriteXmpSidecar(string mediaPath, List<string> keywords, ILogger? logger)
        {
            try
            {
                string sidecar = Path.ChangeExtension(mediaPath, ".xmp");
                XNamespace dc = "http://purl.org/dc/elements/1.1/";
                XNamespace rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

                var bagElements = keywords.Select(k => new XElement(rdf + "li", EscapeXmlText(k)));

                var doc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement(rdf + "RDF",
                        new XElement(rdf + "Description",
                            new XAttribute(rdf + "about", ""),
                            new XAttribute(XNamespace.Xmlns + "dc", dc.NamespaceName),
                            new XElement(dc + "subject",
                                new XElement(rdf + "Bag", bagElements)))));

                using var ms = new MemoryStream();
                using (var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                {
                    doc.Save(writer);
                }

                File.WriteAllBytes(sidecar, ms.ToArray());
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "XMP sidecar keyword write failed for {Path}.", mediaPath);
            }
        }

        private static string EscapeXmlText(string value)
        {
            return value
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("'", "&apos;", StringComparison.Ordinal);
        }
    }
}
