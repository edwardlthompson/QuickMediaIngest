using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class MediaExtensionsTests
    {
        [Theory]
        [InlineData(".jpg", true)]
        [InlineData(".JPEG", true)]
        [InlineData(".cr3", true)]
        [InlineData(".mp4", true)]
        [InlineData(".txt", false)]
        [InlineData(".doc", false)]
        public void IsMediaExtension_ClassifiesExtensions(string ext, bool expected) =>
            Assert.Equal(expected, MediaExtensions.IsMediaExtension(ext));

        [Theory]
        [InlineData("photo.JPG", true)]
        [InlineData("clip.MOV", true)]
        [InlineData("readme.txt", false)]
        public void IsMediaFile_UsesFileName(string fileName, bool expected) =>
            Assert.Equal(expected, MediaExtensions.IsMediaFile(fileName));
    }
}
