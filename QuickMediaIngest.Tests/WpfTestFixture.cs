using System;
using System.Windows;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public sealed class WpfTestFixture : IDisposable
    {
        public WpfTestFixture()
        {
            if (Application.Current == null)
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
        }

        public void Dispose()
        {
        }
    }

    [CollectionDefinition("Wpf")]
    public sealed class WpfTestCollection : ICollectionFixture<WpfTestFixture>
    {
    }

    internal static class WpfTestHost
    {
        public static void EnsureInitialized()
        {
            _ = Application.Current ?? throw new InvalidOperationException("WPF Application was not initialized by WpfTestFixture.");
        }
    }
}
