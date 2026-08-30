#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace QuickMediaIngest.Core.Services
{
    public sealed class KeywordAutocompleteCatalog
    {
        private readonly ConcurrentDictionary<string, int> _keywordFrequency = new(StringComparer.OrdinalIgnoreCase);

        public void RecordKeywords(IEnumerable<string> keywords)
        {
            if (keywords == null) return;
            foreach (var kw in keywords)
            {
                string clean = kw.Trim().TrimStart('#');
                if (!string.IsNullOrWhiteSpace(clean))
                {
                    _keywordFrequency.AddOrUpdate(clean, 1, (_, count) => count + 1);
                }
            }
        }

        public IReadOnlyList<string> GetSuggestions(string prefix, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return _keywordFrequency.OrderByDescending(kvp => kvp.Value)
                                         .Take(maxResults)
                                         .Select(kvp => kvp.Key)
                                         .ToList();
            }

            string cleanPrefix = prefix.Trim().TrimStart('#');
            return _keywordFrequency.Where(kvp => kvp.Key.StartsWith(cleanPrefix, StringComparison.OrdinalIgnoreCase))
                                     .OrderByDescending(kvp => kvp.Value)
                                     .Take(maxResults)
                                     .Select(kvp => kvp.Key)
                                     .ToList();
        }
    }
}
