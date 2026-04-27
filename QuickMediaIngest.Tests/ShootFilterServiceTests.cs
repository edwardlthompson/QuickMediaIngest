using System;
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Localization;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ShootFilterServiceTests
    {
        private readonly ShootFilterService _sut = new();

        private static ImportItem Item(string fileName, string fileType, DateTime taken, bool isVideo = false, bool previewVisible = true)
        {
            return new ImportItem
            {
                FileName = fileName,
                FileType = fileType,
                DateTaken = taken,
                IsVideo = isVideo,
                IsPreviewVisible = previewVisible
            };
        }

        [Fact]
        public void PassesToolbarRules_FiltersByStartDate()
        {
            var item = Item("a.jpg", "JPG", new DateTime(2025, 1, 15));
            var criteria = new ShootFilterCriteria { FilterStartDate = new DateTime(2025, 2, 1) };
            Assert.False(_sut.PassesToolbarRules(item, criteria));
        }

        [Fact]
        public void PassesToolbarRules_FiltersByEndDate_EndOfDay()
        {
            var item = Item("a.jpg", "JPG", new DateTime(2025, 3, 10, 23, 59, 0));
            var criteria = new ShootFilterCriteria { FilterEndDate = new DateTime(2025, 3, 1) };
            Assert.False(_sut.PassesToolbarRules(item, criteria));
        }

        [Fact]
        public void PassesToolbarRules_FiltersByKeyword()
        {
            var hit = Item("vacation_IMG.jpg", "JPG", DateTime.UtcNow);
            var miss = Item("other.jpg", "JPG", DateTime.UtcNow);
            var criteria = new ShootFilterCriteria { FilterKeyword = "vacation" };
            Assert.True(_sut.PassesToolbarRules(hit, criteria));
            Assert.False(_sut.PassesToolbarRules(miss, criteria));
        }

        [Fact]
        public void PassesToolbarRules_ImageFilterExcludesVideo()
        {
            var vid = Item("clip.mp4", "MP4", DateTime.UtcNow, isVideo: true);
            var criteria = new ShootFilterCriteria { FilterFileType = FilterFileTypeLocalization.Images };
            Assert.False(_sut.PassesToolbarRules(vid, criteria));
        }

        [Fact]
        public void ApplyToolbarFilters_KeepsVisibleOnlyMatchingItems()
        {
            var g = new ItemGroup();
            var i1 = Item("keep.jpg", "JPG", DateTime.UtcNow);
            var i2 = Item("drop.mp4", "MP4", DateTime.UtcNow, isVideo: true);
            i1.IsPreviewVisible = true;
            i2.IsPreviewVisible = true;
            g.Items.Add(i1);
            g.Items.Add(i2);

            var groups = new List<ItemGroup> { g };
            _sut.ApplyToolbarFilters(groups, new ShootFilterCriteria { FilterFileType = FilterFileTypeLocalization.Images });

            Assert.True(i1.IsPreviewVisible);
            Assert.False(i2.IsPreviewVisible);
        }
    }
}
