using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest.Tests
{
    public class MainViewModelProofTests
    {
        [Fact]
        public async Task ThumbnailSize_Debounce_Works()
        {
            var vm = new MainViewModel();
            double lastValue = 0;
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(vm.ThumbnailSize)) lastValue = vm.ThumbnailSize; };
            vm.ThumbnailSize = 100;
            vm.ThumbnailSize = 200;
            vm.ThumbnailSize = 300;
            await Task.Delay(300); // Wait for debounce
            Assert.Equal(300, lastValue);
        }

        [Fact]
        public async Task GroupingHours_Debounce_Works()
        {
            var vm = new MainViewModel();
            int lastValue = 0;
            vm.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(vm.GroupingHours)) lastValue = vm.GroupingHours; };
            vm.GroupingHours = 1;
            vm.GroupingHours = 2;
            vm.GroupingHours = 3;
            await Task.Delay(300); // Wait for debounce
            Assert.Equal(3, lastValue);
        }
    }
}
