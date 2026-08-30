#nullable enable
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ImportItemPickRejectRatingTests
    {
        [Fact]
        public void PickAndReject_UpdatesSelectionAndStatus()
        {
            var item = new ImportItem
            {
                FileName = "IMG_001.JPG",
                IsSelected = true
            };

            item.Reject();
            Assert.True(item.IsRejected);
            Assert.False(item.IsSelected);

            item.Pick();
            Assert.False(item.IsRejected);
            Assert.True(item.IsSelected);
        }

        [Fact]
        public void RatingAndColorLabel_PersistsValues()
        {
            var item = new ImportItem
            {
                FileName = "IMG_002.JPG",
                Rating = 5,
                ColorLabel = "Green"
            };

            Assert.Equal(5, item.Rating);
            Assert.Equal("Green", item.ColorLabel);

            item.Rating = 10; // Clamped to 5
            Assert.Equal(5, item.Rating);

            item.Rating = -1; // Clamped to 0
            Assert.Equal(0, item.Rating);
        }
    }
}
