#nullable enable
using System;
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class CullSelectionPersistenceTests
    {
        [Fact]
        public void SnapshotAndRestore_PreservesCullAndRatingsAcrossRescan()
        {
            CullSelectionPersistence.Clear();

            var originalItem = new ImportItem
            {
                FileName = "DSC001.JPG",
                FileSize = 5000,
                DateTaken = new DateTime(2026, 8, 30, 9, 0, 0),
                IsSelected = false,
                IsRejected = true,
                Rating = 4,
                ColorLabel = "Red"
            };

            CullSelectionPersistence.Snapshot(new[] { originalItem });

            // Simulating a fresh rescan item with default values
            var rescannedItem = new ImportItem
            {
                FileName = "DSC001.JPG",
                FileSize = 5000,
                DateTaken = new DateTime(2026, 8, 30, 9, 0, 0),
                IsSelected = true,
                IsRejected = false,
                Rating = 0,
                ColorLabel = ""
            };

            CullSelectionPersistence.Restore(new[] { rescannedItem });

            Assert.False(rescannedItem.IsSelected);
            Assert.True(rescannedItem.IsRejected);
            Assert.Equal(4, rescannedItem.Rating);
            Assert.Equal("Red", rescannedItem.ColorLabel);
        }
    }
}
