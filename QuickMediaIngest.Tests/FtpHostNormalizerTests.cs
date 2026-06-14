using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpHostNormalizerTests
    {
        [Theory]
        [InlineData("ftp://10.0.0.23", "10.0.0.23")]
        [InlineData("FTP://10.0.0.23/", "10.0.0.23")]
        [InlineData("ftps://10.0.0.23", "10.0.0.23")]
        [InlineData("10.0.0.23", "10.0.0.23")]
        [InlineData("ftp://android@10.0.0.23", "10.0.0.23")]
        [InlineData("  ftp://10.0.0.23/DCIM  ", "10.0.0.23")]
        [InlineData("ftp://10.0.0.23:2221", "10.0.0.23")]
        [InlineData("10.0.0.23:2221", "10.0.0.23")]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void Normalize_StripsSchemeUserinfoAndTrailingSlash(string? input, string expected)
        {
            Assert.Equal(expected, FtpHostNormalizer.Normalize(input));
        }

        [Fact]
        public void TryParseHostAndPort_ExtractsPortFromUrl()
        {
            bool ok = FtpHostNormalizer.TryParseHostAndPort("ftp://10.0.0.23:2221", out string host, out int? port);

            Assert.True(ok);
            Assert.Equal("10.0.0.23", host);
            Assert.Equal(2221, port);
        }

        [Fact]
        public void Normalize_ProducesValidConnectionUri()
        {
            string host = FtpHostNormalizer.Normalize("ftp://10.0.0.23");
            string uri = $"ftp://{host}:2221/";
            Assert.Equal("ftp://10.0.0.23:2221/", uri);
        }
    }
}
