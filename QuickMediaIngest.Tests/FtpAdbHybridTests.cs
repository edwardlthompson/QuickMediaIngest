#nullable enable
using System.Collections.Generic;
using System.Linq;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpMediaPathNormalizerTests
    {
        [Fact]
        public void GetRetrCandidates_Heif_TriesHeicThenOriginal()
        {
            string[] paths = FtpMediaPathNormalizer.GetRetrCandidates("/DCIM/a.heif").ToArray();
            Assert.Equal(new[] { "/DCIM/a.heic", "/DCIM/a.heif" }, paths);
        }

        [Fact]
        public void GetRetrCandidates_Heif_PrefersKnownSiblingHeic()
        {
            var known = new HashSet<string> { "a.heic" };
            string[] paths = FtpMediaPathNormalizer.GetRetrCandidates("/DCIM/a.heif", known).ToArray();
            Assert.Equal(new[] { "/DCIM/a.heic", "/DCIM/a.heif" }, paths);
        }

        [Fact]
        public void GetRetrCandidates_Jpeg_Unchanged()
        {
            string[] paths = FtpMediaPathNormalizer.GetRetrCandidates("/DCIM/a.jpg").ToArray();
            Assert.Equal(new[] { "/DCIM/a.jpg" }, paths);
        }

        [Fact]
        public void GetRenderedSiblingRemotePaths_HeicBeforeHeif()
        {
            string[] paths = FtpMediaPathNormalizer
                .GetRenderedSiblingRemotePaths("/DCIM/a.dng", "a.dng")
                .ToArray();
            Assert.Equal(".heic", System.IO.Path.GetExtension(paths[0]));
            Assert.Equal(".heif", System.IO.Path.GetExtension(paths[1]));
        }
    }

    public class FtpPermanentFailureCacheTests
    {
        [Fact]
        public void MarkAndIsFailed_RoundTrip_AndClearEndpoint()
        {
            FtpPermanentFailureCache.ClearAll();
            FtpPermanentFailureCache.MarkFailed("10.0.0.7", 2221, "/DCIM/a.heif");
            Assert.True(FtpPermanentFailureCache.IsFailed("10.0.0.7", 2221, "/DCIM/a.heif"));
            Assert.False(FtpPermanentFailureCache.IsFailed("10.0.0.7", 2221, "/DCIM/b.heif"));
            FtpPermanentFailureCache.ClearEndpoint("10.0.0.7", 2221);
            Assert.False(FtpPermanentFailureCache.IsFailed("10.0.0.7", 2221, "/DCIM/a.heif"));
        }
    }

    public class AdbMediaScannerPathTests
    {
        [Fact]
        public void TryToFtpStylePath_StripsMediaRoot()
        {
            Assert.True(AdbMediaScanner.TryToFtpStylePath("/sdcard", "/sdcard/DCIM/Camera/a.heic", out string ftp));
            Assert.Equal("/DCIM/Camera/a.heic", ftp);
        }

        [Fact]
        public void IsNestedUnderFolder_DetectsNested()
        {
            Assert.True(AdbMediaScanner.IsNestedUnderFolder("/DCIM", "/DCIM/Camera/a.jpg"));
            Assert.False(AdbMediaScanner.IsNestedUnderFolder("/DCIM", "/DCIM/a.jpg"));
        }

        [Fact]
        public void TryParseFindLine_PathOnly()
        {
            Assert.True(AdbMediaScanner.TryParseFindLine("/sdcard/DCIM/a.heic", out string path, out long size));
            Assert.Equal("/sdcard/DCIM/a.heic", path);
            Assert.Equal(0, size);
        }

        [Fact]
        public void TryParseFindLine_TabSeparatedSize()
        {
            Assert.True(AdbMediaScanner.TryParseFindLine("/sdcard/DCIM/a.heic\t2048", out string path, out long size));
            Assert.Equal("/sdcard/DCIM/a.heic", path);
            Assert.Equal(2048, size);
        }

        [Fact]
        public void TryParseFindLine_PipeSeparatedSize_WithSpacesInPath()
        {
            Assert.True(AdbMediaScanner.TryParseFindLine(
                "/sdcard/DCIM/Point & Shoot/a.jpg|6134428",
                out string path,
                out long size));
            Assert.Equal("/sdcard/DCIM/Point & Shoot/a.jpg", path);
            Assert.Equal(6134428, size);
        }
    }

    public class AdbPreviewFetcherPayloadTests
    {
        [Fact]
        public void LooksLikeMediaPayload_AcceptsJpegAndHeicHeaders()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qmi-adb-payload-tests");
            System.IO.Directory.CreateDirectory(dir);
            string jpeg = System.IO.Path.Combine(dir, "a.jpg");
            string heic = System.IO.Path.Combine(dir, "a.heic");
            string ddErr = System.IO.Path.Combine(dir, "err.bin");

            System.IO.File.WriteAllBytes(jpeg, [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0]);
            System.IO.File.WriteAllBytes(heic, [0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'h', (byte)'e', (byte)'i', (byte)'c']);
            System.IO.File.WriteAllText(ddErr, "dd: bad arg &\n");

            Assert.True(AdbPreviewFetcher.LooksLikeMediaPayload(jpeg));
            Assert.True(AdbPreviewFetcher.LooksLikeMediaPayload(heic));
            Assert.False(AdbPreviewFetcher.LooksLikeMediaPayload(ddErr));
        }
    }
}
