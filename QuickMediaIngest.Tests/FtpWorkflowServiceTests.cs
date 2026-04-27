using System.Threading;
using System.Threading.Tasks;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpWorkflowServiceTests
    {
        [Fact]
        public async Task TestConnectionAsync_ForwardsToScanner()
        {
            var ftpScanner = new Mock<IFtpScanner>();
            ftpScanner
                .Setup(s => s.TestConnectionAsync(
                    "ftp.example.test",
                    2121,
                    "u",
                    "p",
                    "/remote",
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "Connected."));

            var sut = new FtpWorkflowService(ftpScanner.Object);

            var result = await sut.TestConnectionAsync(
                    "ftp.example.test",
                    2121,
                    "u",
                    "p",
                    "/remote",
                    30,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.True(result.Success);
            Assert.Equal("Connected.", result.Message);

            ftpScanner.Verify(
                s => s.TestConnectionAsync(
                    "ftp.example.test",
                    2121,
                    "u",
                    "p",
                    "/remote",
                    30,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
