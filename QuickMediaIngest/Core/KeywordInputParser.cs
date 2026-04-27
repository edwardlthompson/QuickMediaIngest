#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Parses user keyword input: comma/semicolon-separated tokens and hashtags (#tag).
    /// </summary>
    public static class KeywordInputParser
    {
        private static readonly Regex HashtagRegex = new(@"#([\w][\w\-]*)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Returns distinct keywords without leading '#' (suitable for EXIF/XMP).
        /// </summary>
        public static List<string> Parse(string? input)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(input))
            {
                return new List<string>();
            }

            string text = input.Trim();

            foreach (Match m in HashtagRegex.Matches(text))
            {
                string word = m.Groups[1].Value.Trim();
                if (word.Length > 0)
                {
                    set.Add(word);
                }
            }

            foreach (string segment in Regex.Split(text, @"[,;\r\n]+"))
            {
                string s = segment.Trim();
                while (s.StartsWith('#'))
                {
                    s = s.Substring(1).TrimStart();
                }

                if (s.Length > 0)
                {
                    set.Add(s);
                }
            }

            return set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
