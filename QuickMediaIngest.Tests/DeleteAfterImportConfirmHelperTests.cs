#nullable enable
using QuickMediaIngest.Services;
using QuickMediaIngest.ViewModels;
using Xunit;

namespace QuickMediaIngest.Tests
{
    [Collection("Wpf")]
    public class DeleteAfterImportConfirmHelperTests
    {
        [Fact]
        public void HandleChecked_WhenPromptDismissed_SkipsDialogAndKeepsEnabled()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();
            vm.DeleteAfterImport = true;
            vm.DeleteAfterImportPromptDismissed = true;

            bool userInitiated = true;
            bool reverted = false;

            DeleteAfterImportConfirmHelper.HandleChecked(vm, ref userInitiated, () => reverted = true);

            Assert.False(userInitiated);
            Assert.False(reverted);
            Assert.True(vm.DeleteAfterImport);
            Assert.True(vm.DeleteAfterImportPromptDismissed);
        }

        [Fact]
        public void HandleChecked_WhenNotUserInitiated_DoesNothing()
        {
            WpfTestHost.EnsureInitialized();
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();
            vm.DeleteAfterImportPromptDismissed = false;

            bool userInitiated = false;
            bool reverted = false;

            DeleteAfterImportConfirmHelper.HandleChecked(vm, ref userInitiated, () => reverted = true);

            Assert.False(userInitiated);
            Assert.False(reverted);
            Assert.False(vm.DeleteAfterImportPromptDismissed);
        }
    }
}
