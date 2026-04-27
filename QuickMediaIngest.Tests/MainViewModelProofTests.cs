using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Data;
using QuickMediaIngest.ViewModels;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class MainViewModelProofTests
    {
        private static MainViewModel CreateViewModel()
        {
            return new MainViewModel(
                new Mock<ILocalScanner>().Object,
                new Mock<IFtpScanner>().Object,
                new Mock<IThumbnailService>().Object,
                new Mock<IUpdateService>().Object,
                new Mock<IDeviceWatcher>().Object,
                new Mock<IFileProviderFactory>().Object,
                new Mock<IIngestEngineFactory>().Object,
                new GroupBuilder(),
                new Mock<IDatabaseService>().Object,
                new Mock<IShootFilterService>().Object,
                new Mock<IFtpWorkflowService>().Object,
                new Mock<IUnifiedConcreteSourceScanService>().Object,
                new Mock<IFtpCredentialStore>().Object,
                new Mock<ILogger<MainViewModel>>().Object);
        }

        [Fact]
        public void ThumbnailSize_CanBeUpdated()
        {
            var vm = CreateViewModel();
            vm.ThumbnailSize = 100;
            Assert.Equal(100, vm.ThumbnailSize);
        }

        [Fact]
        public void DestinationRoot_DefaultsToPicturesPath()
        {
            var vm = CreateViewModel();
            Assert.Contains("QuickMediaIngest", vm.DestinationRoot);
        }
    }
}
