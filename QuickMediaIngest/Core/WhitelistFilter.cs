using System;
using System.Collections.Generic;
using System.Linq;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Data.Models;

namespace QuickMediaIngest.Core
{
    public class WhitelistFilter
    {
        /// <summary>
        /// Filters a list of items based on folder or extension paths.
        /// </summary>
        public List<ImportItem> Filter(List<ImportItem> items, List<WhitelistRule> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                return items; // Allow all if no whitelist is registered.
            }

            return items.Where(item => rules.Any(rule => MatchesRule(item, rule))).ToList();
        }

        private bool MatchesRule(ImportItem item, WhitelistRule rule)
        {
            string normFile = item.SourcePath.Replace("\\", "/").ToLower();
            string normRule = rule.Path.Replace("\\", "/").ToLower();

            if (rule.RuleType.Equals("Folder", StringComparison.OrdinalIgnoreCase))
            {
                // Path prefix match
                return normFile.StartsWith(normRule);
            }
            else if (rule.RuleType.Equals("Extension", StringComparison.OrdinalIgnoreCase))
            {
                return item.FileType.Equals(normRule.TrimStart('.'), StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
