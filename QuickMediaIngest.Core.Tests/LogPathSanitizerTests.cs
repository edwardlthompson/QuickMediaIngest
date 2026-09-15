#nullable enable
using QuickMediaIngest.Core.Logging;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class LogPathSanitizerTests
    {
        [Fact]
        public void Local_NullOrEmpty_ReturnsPlaceholder()
        {
            Assert.Equal("(empty)", LogPathSanitizer.Local(null));
            Assert.Equal("(empty)", LogPathSanitizer.Local("   "));
        }

        [Fact]
        public void Local_ShortPath_Unchanged()
        {
            Assert.Equal(@"E:\DCIM", LogPathSanitizer.Local(@"E:\DCIM"));
        }

        [Fact]
        public void Local_DeepUserPath_RedactsMiddle()
        {
            string input = @"C:\Users\edwar\Pictures\Shoot2024\IMG_0001.JPG";
            string sanitized = LogPathSanitizer.Local(input, tailSegments: 2);
            Assert.DoesNotContain(@"Users\edwar", sanitized, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains("IMG_0001.JPG", sanitized);
            Assert.Contains("...", sanitized);
            Assert.StartsWith(@"C:", sanitized);
        }

        [Fact]
        public void AppData_KeepsQuickMediaIngestTail()
        {
            string input = @"C:\Users\edwar\AppData\Roaming\QuickMediaIngest\last_update_check.txt";
            string sanitized = LogPathSanitizer.AppData(input);
            Assert.Contains(@"QuickMediaIngest\last_update_check.txt", sanitized);
            Assert.DoesNotContain(@"Users\edwar", sanitized, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FtpRemote_TruncatesDeepPaths()
        {
            string input = "/home/user/DCIM/100CANON/sub/deep/photo.jpg";
            string sanitized = LogPathSanitizer.FtpRemote(input, maxSegments: 3);
            Assert.Equal("/.../sub/deep/photo.jpg", sanitized);
        }

        [Fact]
        public void FtpRemote_ShortPath_Normalized()
        {
            Assert.Equal("/DCIM/100CANON", LogPathSanitizer.FtpRemote(@"\DCIM\100CANON"));
        }

        [Fact]
        public void ForLog_DispatchesByKind()
        {
            Assert.Contains("QuickMediaIngest", LogPathSanitizer.ForLog(
                @"C:\x\QuickMediaIngest\config.json", LogPathSanitizer.PathKind.AppData));
            Assert.Equal("/DCIM", LogPathSanitizer.ForLog("/DCIM", LogPathSanitizer.PathKind.FtpRemote));
        }
    }
}
