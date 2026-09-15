using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpListingParserTests
    {
        [Fact]
        public void TryParseListingLine_UnixFile_ParsesNameSizeAndPath()
        {
            bool ok = FtpListingParser.TryParseListingLine(
                "-rw-r--r--    1 user  group     1234567 Mar 15 14:30 photo.jpg",
                "/DCIM/100CANON",
                out var entry);

            Assert.True(ok);
            Assert.NotNull(entry);
            Assert.Equal("photo.jpg", entry!.Name);
            Assert.Equal(1234567, entry.Size);
            Assert.False(entry.IsDirectory);
            Assert.Equal("/DCIM/100CANON/photo.jpg", entry.FullPath);
        }

        [Fact]
        public void TryParseListingLine_UnixDirectory_ParsesType()
        {
            bool ok = FtpListingParser.TryParseListingLine(
                "drwxr-xr-x    2 user  group           0 Apr  1  2024 100CANON",
                "/DCIM",
                out var entry);

            Assert.True(ok);
            Assert.NotNull(entry);
            Assert.True(entry!.IsDirectory);
            Assert.Equal(0, entry.Size);
            Assert.Equal("/DCIM/100CANON", entry.FullPath);
        }

        [Fact]
        public void TryParseListingLine_DosFile_ParsesSize()
        {
            bool ok = FtpListingParser.TryParseListingLine(
                "03-15-24  02:30PM               321 IMG_0001.JPG",
                "/photos",
                out var entry);

            Assert.True(ok);
            Assert.NotNull(entry);
            Assert.Equal("IMG_0001.JPG", entry!.Name);
            Assert.Equal(321, entry.Size);
            Assert.False(entry.IsDirectory);
        }

        [Fact]
        public void TryParseListingLine_DosDirectory_ParsesDirMarker()
        {
            bool ok = FtpListingParser.TryParseListingLine(
                "03-15-24  02:30PM       <DIR>          subfolder",
                "/",
                out var entry);

            Assert.True(ok);
            Assert.NotNull(entry);
            Assert.True(entry!.IsDirectory);
            Assert.Equal("/subfolder", entry.FullPath);
        }

        [Theory]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData("")]
        [InlineData("   ")]
        public void TryParseListingLine_SkipsDotEntriesAndBlank(string line)
        {
            bool ok = FtpListingParser.TryParseListingLine(line, "/", out var entry);
            Assert.False(ok);
            Assert.Null(entry);
        }

        [Fact]
        public void TryParseListingLine_UnrecognizedLine_ReturnsFalse()
        {
            bool ok = FtpListingParser.TryParseListingLine("not a listing line", "/", out var entry);
            Assert.False(ok);
            Assert.Null(entry);
        }

        [Fact]
        public void CombineRemotePath_JoinsParentAndChild()
        {
            Assert.Equal("/a/b/c", FtpListingParser.CombineRemotePath("/a/b", "c"));
            Assert.Equal("/child", FtpListingParser.CombineRemotePath("/", "child"));
        }

        [Fact]
        public void NormalizeRemotePath_EnsuresLeadingSlash()
        {
            Assert.Equal("/", FtpListingParser.NormalizeRemotePath(""));
            Assert.Equal("/DCIM", FtpListingParser.NormalizeRemotePath("DCIM"));
            Assert.Equal("/DCIM/100", FtpListingParser.NormalizeRemotePath("\\DCIM\\100"));
        }
    }
}
