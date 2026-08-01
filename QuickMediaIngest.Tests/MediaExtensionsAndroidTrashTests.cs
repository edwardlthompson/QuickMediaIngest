#nullable enable
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class MediaExtensionsAndroidTrashTests
    {
        [Theory]
        [InlineData(".trashed-123-photo.dng", true)]
        [InlineData(".TRASHED-abc.heic", true)]
        [InlineData(".nomedia", true)]
        [InlineData("IMG_0001.jpg", false)]
        [InlineData("photo.dng", false)]
        public void IsAndroidTrashOrNoise(string name, bool expected) =>
            Assert.Equal(expected, MediaExtensions.IsAndroidTrashOrNoise(name));

        [Theory]
        [InlineData(".Trash", true)]
        [InlineData("trash", true)]
        [InlineData("Camera", false)]
        public void IsAndroidTrashDirectory(string name, bool expected) =>
            Assert.Equal(expected, MediaExtensions.IsAndroidTrashDirectory(name));

        [Fact]
        public void IsMediaFile_RejectsTrashedEvenWithMediaExtension() =>
            Assert.False(MediaExtensions.IsMediaFile(".trashed-1-file.dng"));

        [Fact]
        public void IsMediaFile_AcceptsNormalMedia() =>
            Assert.True(MediaExtensions.IsMediaFile("shot.heic"));
    }
}
