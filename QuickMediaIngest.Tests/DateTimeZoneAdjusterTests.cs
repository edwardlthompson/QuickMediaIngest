#nullable enable
using System;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class DateTimeZoneAdjusterTests
    {
        [Fact]
        public void AdjustDateTaken_CustomOffset_AddsOffsetCorrectly()
        {
            var dt = new DateTime(2026, 8, 30, 12, 0, 0);
            var adjusted = DateTimeZoneAdjuster.AdjustDateTaken(dt, TimeZoneOverrideMode.CustomOffset, TimeSpan.FromHours(-4));
            Assert.Equal(new DateTime(2026, 8, 30, 8, 0, 0), adjusted);
        }

        [Fact]
        public void ApplyTimeZoneOverride_ModifiesCollection()
        {
            var item = new ImportItem { DateTaken = new DateTime(2026, 8, 30, 10, 0, 0) };
            DateTimeZoneAdjuster.ApplyTimeZoneOverride(new[] { item }, TimeZoneOverrideMode.CustomOffset, TimeSpan.FromHours(2));
            Assert.Equal(new DateTime(2026, 8, 30, 12, 0, 0), item.DateTaken);
        }
    }
}
