#nullable enable
using System;
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public enum TimeZoneOverrideMode
    {
        CameraAsIs = 0,
        LocalSystem = 1,
        Utc = 2,
        CustomOffset = 3
    }

    public static class DateTimeZoneAdjuster
    {
        public static DateTime AdjustDateTaken(DateTime dateTaken, TimeZoneOverrideMode mode, TimeSpan? customOffset = null)
        {
            return mode switch
            {
                TimeZoneOverrideMode.LocalSystem => DateTime.SpecifyKind(dateTaken, DateTimeKind.Local),
                TimeZoneOverrideMode.Utc => DateTime.SpecifyKind(dateTaken, DateTimeKind.Utc),
                TimeZoneOverrideMode.CustomOffset when customOffset.HasValue => dateTaken.Add(customOffset.Value),
                _ => dateTaken
            };
        }

        public static void ApplyTimeZoneOverride(IEnumerable<ImportItem> items, TimeZoneOverrideMode mode, TimeSpan? customOffset = null)
        {
            if (items == null || mode == TimeZoneOverrideMode.CameraAsIs) return;

            foreach (var item in items)
            {
                item.DateTaken = AdjustDateTaken(item.DateTaken, mode, customOffset);
            }
        }
    }
}
