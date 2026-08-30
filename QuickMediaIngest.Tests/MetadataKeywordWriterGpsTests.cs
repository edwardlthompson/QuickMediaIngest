#nullable enable
using System;
using System.IO;
using ImageMagick;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class MetadataKeywordWriterGpsTests
    {
        [Fact]
        public void TryApplyKeywords_WithStripGps_RemovesGpsTags()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"gps-test-{Guid.NewGuid():N}.jpg");

            try
            {
                using (var img = new MagickImage(MagickColors.Blue, 100, 100))
                {
                    img.Format = MagickFormat.Jpeg;
                    var profile = new ExifProfile();
                    profile.SetValue(ExifTag.GPSLatitude, new Rational[] { new Rational(37, 1), new Rational(46, 1), new Rational(0, 1) });
                    profile.SetValue(ExifTag.GPSLatitudeRef, "N");
                    img.SetProfile(profile);
                    img.Write(tempFile);
                }

                // Verify GPS is initially present
                using (var checkImg = new MagickImage(tempFile))
                {
                    var p = checkImg.GetExifProfile();
                    Assert.NotNull(p);
                    Assert.NotNull(p.GetValue(ExifTag.GPSLatitude));
                }

                // Apply with stripGpsAndPii = true
                MetadataKeywordWriter.TryApplyKeywords(tempFile, new[] { "tag1", "tag2" }, stripGpsAndPii: true, NullLogger.Instance);

                // Verify GPS is removed and keywords are embedded
                using (var afterImg = new MagickImage(tempFile))
                {
                    var p = afterImg.GetExifProfile();
                    if (p != null)
                    {
                        Assert.Null(p.GetValue(ExifTag.GPSLatitude));
                        Assert.Null(p.GetValue(ExifTag.GPSLatitudeRef));
                    }
                    var iptc = afterImg.GetIptcProfile();
                    string? xp = afterImg.GetAttribute("exif:XPKeywords") ?? afterImg.GetAttribute("comment");
                    Assert.True(!string.IsNullOrEmpty(xp) || afterImg.Comment?.Contains("tag1") == true || iptc != null);
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void TryStripGpsAndPii_RemovesGpsWithoutAddingKeywords()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"strip-gps-{Guid.NewGuid():N}.jpg");

            try
            {
                using (var img = new MagickImage(MagickColors.Red, 100, 100))
                {
                    img.Format = MagickFormat.Jpeg;
                    var profile = new ExifProfile();
                    profile.SetValue(ExifTag.GPSLatitude, new Rational[] { new Rational(40, 1), new Rational(0, 1), new Rational(0, 1) });
                    img.SetProfile(profile);
                    img.Write(tempFile);
                }

                MetadataKeywordWriter.TryStripGpsAndPii(tempFile, NullLogger.Instance);

                using (var afterImg = new MagickImage(tempFile))
                {
                    var p = afterImg.GetExifProfile();
                    if (p != null)
                    {
                        Assert.Null(p.GetValue(ExifTag.GPSLatitude));
                    }
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
