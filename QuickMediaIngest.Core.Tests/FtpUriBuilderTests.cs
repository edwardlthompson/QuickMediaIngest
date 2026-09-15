using System;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpUriBuilderTests
    {
        [Theory]
        [InlineData("10.0.0.23", 2221, "/DCIM/Camera/photo.jpg", "10.0.0.23", 2221)]
        [InlineData("ftp://10.0.0.23", 21, "/DCIM/photo.jpg", "10.0.0.23", 21)]
        public void BuildFileUri_NormalizesHostAndPort(string host, int port, string remotePath, string expectedHost, int expectedPort)
        {
            var uri = FtpFileDownloader.BuildFileUri(host, port, remotePath);

            Assert.Equal(expectedHost, uri.Host);
            Assert.Equal(expectedPort, uri.Port);
            Assert.Contains("DCIM", uri.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BuildFileUri_EncodesSpecialCharactersInPathSegments()
        {
            var uri = FtpFileDownloader.BuildFileUri("10.0.0.23", 2221, "/DCIM/Point & Shoot/img.jpg");

            Assert.Equal("10.0.0.23", uri.Host);
            string uriText = uri.ToString();
            Assert.Contains("%26", uriText, StringComparison.Ordinal);
            Assert.Contains("img.jpg", uriText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
