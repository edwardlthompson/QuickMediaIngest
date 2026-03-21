#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Data.Models;

namespace QuickMediaIngest.Core
{
    public class WhitelistFilter : IWhitelistFilter
    {
        private readonly ILogger<WhitelistFilter> _logger;

        public WhitelistFilter(ILogger<WhitelistFilter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Filters a list of items based on folder or extension paths.
        /// </summary>
        public List<ImportItem> Filter(List<ImportItem> items, List<WhitelistRule> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                _logger.LogInformation("Whitelist filter skipped because no rules were provided. Items={ItemCount}", items.Count);
                return items; // Allow all if no whitelist is registered.
            }

            var filteredItems = items.Where(item => rules.Any(rule => MatchesRule(item, rule))).ToList();
            _logger.LogInformation("Whitelist filter applied. Before={BeforeCount}, After={AfterCount}, Rules={RuleCount}", items.Count, filteredItems.Count, rules.Count);
            return filteredItems;
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
