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
        [InlineData(".avif", true)]
        [InlineData(".jxl", true)]
        [InlineData(".hif", true)]
        [InlineData(".webm", true)]
        [InlineData(".jp2", true)]
        [InlineData(".thm", true)]
        [InlineData(".mp4", true)]
        [InlineData(".txt", false)]
        [InlineData(".doc", false)]
        public void IsMediaExtension_ClassifiesExtensions(string ext, bool expected) =>
            Assert.Equal(expected, MediaExtensions.IsMediaExtension(ext));

        [Theory]
        [InlineData("photo.JPG", true)]
        [InlineData("clip.MOV", true)]
        [InlineData("phone.AVIF", true)]
        [InlineData("extra.jxl", true)]
        [InlineData("readme.txt", false)]
        public void IsMediaFile_UsesFileName(string fileName, bool expected) =>
            Assert.Equal(expected, MediaExtensions.IsMediaFile(fileName));

        [Theory]
        [InlineData(".avif")]
        [InlineData(".AVIFS")]
        [InlineData(".hif")]
        public void HeifFamily_IncludesAvifAndHif(string ext) =>
            Assert.True(MediaExtensions.IsHeifFamily(ext));

        [Fact]
        public void Jxl_IsImage_NotHeifFamily()
        {
            Assert.True(MediaExtensions.IsImageExtension(".jxl"));
            Assert.False(MediaExtensions.IsHeifFamily(".jxl"));
            Assert.False(MediaExtensions.IsRawExtension(".jxl"));
        }
    }
}
