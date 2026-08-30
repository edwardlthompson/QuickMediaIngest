#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    public static partial class MetadataKeywordWriter
    {
        /// <summary>
        /// Writes an XMP sidecar containing creator, copyright notice, and keywords without mutating the media file.
        /// </summary>
        public static void WriteXmpSidecarMetadata(string mediaPath, IReadOnlyList<string>? keywords, string? creator = null, string? copyright = null, ILogger? logger = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mediaPath) || !File.Exists(mediaPath)) return;

                var list = NormalizeKeywords(keywords);
                string sidecar = Path.ChangeExtension(mediaPath, ".xmp");
                XNamespace dc = "http://purl.org/dc/elements/1.1/";
                XNamespace rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
                XNamespace xmpRights = "http://ns.adobe.com/xap/1.0/rights/";

                var descriptionElement = new XElement(rdf + "Description",
                    new XAttribute(rdf + "about", ""),
                    new XAttribute(XNamespace.Xmlns + "dc", dc.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "xmpRights", xmpRights.NamespaceName));

                if (list.Count > 0)
                {
                    var bagElements = list.Select(k => new XElement(rdf + "li", EscapeXmlText(k)));
                    descriptionElement.Add(new XElement(dc + "subject", new XElement(rdf + "Bag", bagElements)));
                }

                if (!string.IsNullOrWhiteSpace(creator))
                {
                    descriptionElement.Add(new XElement(dc + "creator",
                        new XElement(rdf + "Seq", new XElement(rdf + "li", EscapeXmlText(creator)))));
                }

                if (!string.IsNullOrWhiteSpace(copyright))
                {
                    descriptionElement.Add(new XElement(dc + "rights",
                        new XElement(rdf + "Alt",
                            new XElement(rdf + "li",
                                new XAttribute(XNamespace.Xml + "lang", "x-default"),
                                EscapeXmlText(copyright)))));
                    descriptionElement.Add(new XElement(xmpRights + "Marked", "True"));
                }

                var doc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement(rdf + "RDF", descriptionElement));

                using var ms = new MemoryStream();
                using (var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
                {
                    doc.Save(writer);
                }

                File.WriteAllBytes(sidecar, ms.ToArray());
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Writing XMP sidecar failed for {Path}.", mediaPath);
            }
        }

        private static void WriteXmpSidecar(string mediaPath, List<string> keywords, ILogger? logger) =>
            WriteXmpSidecarMetadata(mediaPath, keywords, null, null, logger);

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
