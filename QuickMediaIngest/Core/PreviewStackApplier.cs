#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>Hides RAW/JPEG stack members unless the shoot is expanded.</summary>
    public static class PreviewStackApplier
    {
        private static readonly HashSet<string> Rendered = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".jpe", ".jfif", ".thm", ".heic", ".heif", ".hif", ".avif", ".jxl",
        };

        public static void Apply(IList<ImportItem> items, bool groupPairs, bool expandStacks)
        {
            foreach (ImportItem item in items)
            {
                item.IsPreviewVisible = true;
                item.IsStackRepresentative = true;
                item.StackKey = item.SourcePath;
            }

            if (!groupPairs)
            {
                return;
            }

            foreach (IGrouping<string, ImportItem> group in items.Where(i => !i.IsVideo)
                         .GroupBy(i => Path.GetFileNameWithoutExtension(i.FileName), StringComparer.OrdinalIgnoreCase))
            {
                List<ImportItem> members = group.ToList();
                if (members.Count <= 1)
                {
                    continue;
                }

                bool hasRendered = members.Any(m => Rendered.Contains(Path.GetExtension(m.FileName)));
                bool hasRaw = members.Any(m => MediaExtensions.IsRawExtension(Path.GetExtension(m.FileName)));
                if (!hasRendered || !hasRaw)
                {
                    continue;
                }

                ImportItem representative = members
                    .OrderBy(m => Rank(Path.GetExtension(m.FileName)))
                    .First();
                foreach (ImportItem member in members)
                {
                    member.StackKey = group.Key;
                    member.IsStackRepresentative = ReferenceEquals(member, representative);
                    member.IsPreviewVisible = expandStacks || member.IsStackRepresentative;
                }
            }
        }

        private static int Rank(string ext)
        {
            if (MediaExtensions.IsJpegFamily(ext))
            {
                return 0;
            }

            if (MediaExtensions.IsHeifFamily(ext) || ext.Equals(".jxl", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return 3;
        }
    }
}
