using Moq;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpSourceCredentialsTests
    {
        [Fact]
        public void ResolvePassword_ReturnsInMemoryPassWhenPresent()
        {
            var store = new Mock<IFtpCredentialStore>(MockBehavior.Strict);

            string resolved = FtpSourceCredentials.ResolvePassword("secret", "10.0.0.23", 2221, "10.0.0.23", store.Object);

            Assert.Equal("secret", resolved);
        }

        [Fact]
        public void ResolvePassword_FallsBackToVaultWhenPassEmpty()
        {
            var store = new Mock<IFtpCredentialStore>();
            store.Setup(s => s.TryReadPasswordWithLegacyKeys("10.0.0.23", 2221, "10.0.0.23", out It.Ref<string>.IsAny))
                .Callback(new TryReadPasswordWithLegacyKeysCallback((string _, int _, string? _, out string password) => password = "vault"))
                .Returns(true);

            string resolved = FtpSourceCredentials.ResolvePassword(string.Empty, "10.0.0.23", 2221, "10.0.0.23", store.Object);

            Assert.Equal("vault", resolved);
        }

        [Fact]
        public void ResolvePassword_NormalizesHostBeforeVaultLookup()
        {
            var store = new Mock<IFtpCredentialStore>();
            store.Setup(s => s.TryReadPasswordWithLegacyKeys("10.0.0.23", 2221, "ftp://10.0.0.23", out It.Ref<string>.IsAny))
                .Callback(new TryReadPasswordWithLegacyKeysCallback((string _, int _, string? _, out string password) => password = "vault"))
                .Returns(true);

            string resolved = FtpSourceCredentials.ResolvePassword(string.Empty, "ftp://10.0.0.23", 2221, "ftp://10.0.0.23", store.Object);

            Assert.Equal("vault", resolved);
        }

        private delegate void TryReadPasswordWithLegacyKeysCallback(string host, int port, string? rawHost, out string password);
    }
}
