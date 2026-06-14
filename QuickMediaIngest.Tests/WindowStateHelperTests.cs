using System.Collections.Generic;
using System.Windows;
using QuickMediaIngest.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class WindowStateHelperTests
    {
        private static readonly Rect PrimaryMonitor = new(0, 0, 1920, 1040);

        [Fact]
        public void ClampToVisibleBounds_KeepsValidPosition()
        {
            var result = WindowStateHelper.ClampToVisibleBounds(
                960, 620, 200, 150, new[] { PrimaryMonitor });

            Assert.Equal(960, result.Width);
            Assert.Equal(620, result.Height);
            Assert.Equal(200, result.Left);
            Assert.Equal(150, result.Top);
        }

        [Fact]
        public void ClampToVisibleBounds_RepositionsOffScreenLeftWindow()
        {
            var result = WindowStateHelper.ClampToVisibleBounds(
                960, 620, -5000, 150, new[] { PrimaryMonitor });

            Assert.True(result.Left >= PrimaryMonitor.Left);
            Assert.True(result.Top >= PrimaryMonitor.Top);
            Assert.True(result.Left + result.Width <= PrimaryMonitor.Right);
            Assert.True(result.Top + result.Height <= PrimaryMonitor.Bottom);
        }

        [Fact]
        public void ClampToVisibleBounds_RepositionsOffScreenTopWindow()
        {
            var result = WindowStateHelper.ClampToVisibleBounds(
                960, 620, 200, -5000, new[] { PrimaryMonitor });

            Assert.True(result.Left >= PrimaryMonitor.Left);
            Assert.True(result.Top >= PrimaryMonitor.Top);
        }

        [Fact]
        public void ClampToVisibleBounds_EnforcesMinimumSize()
        {
            var result = WindowStateHelper.ClampToVisibleBounds(
                200, 100, 200, 150, new[] { PrimaryMonitor });

            Assert.Equal(WindowStateHelper.MinWidth, result.Width);
            Assert.Equal(WindowStateHelper.MinHeight, result.Height);
        }

        [Fact]
        public void ClampToVisibleBounds_CentersOnPrimaryWhenNoOverlap()
        {
            var result = WindowStateHelper.ClampToVisibleBounds(
                800, 600, 10000, 10000, new[] { PrimaryMonitor });

            double expectedLeft = PrimaryMonitor.Left + (PrimaryMonitor.Width - 800) / 2;
            double expectedTop = PrimaryMonitor.Top + (PrimaryMonitor.Height - 600) / 2;
            Assert.Equal(expectedLeft, result.Left);
            Assert.Equal(expectedTop, result.Top);
        }

        [Fact]
        public void ClampToVisibleBounds_UsesFallbackWhenNoMonitorsProvided()
        {
            var result = WindowStateHelper.ClampToVisibleBounds(
                960, 620, 200, 150, new List<Rect>());

            Assert.True(result.Width >= WindowStateHelper.MinWidth);
            Assert.True(result.Height >= WindowStateHelper.MinHeight);
        }
    }
}
