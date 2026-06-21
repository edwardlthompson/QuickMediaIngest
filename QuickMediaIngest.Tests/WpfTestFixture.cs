using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public sealed class WpfTestFixture : IDisposable
    {
        private readonly Thread _staThread;
        private readonly ManualResetEventSlim _ready = new(false);

        public WpfTestFixture()
        {
            _staThread = new Thread(() =>
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                _ready.Set();
                Dispatcher.Run();
            })
            {
                IsBackground = true,
                Name = "QuickMediaIngest.WpfTests"
            };
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.Start();
            _ready.Wait();
        }

        public void Dispose()
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                Application.Current.Shutdown();
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
            }
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
            if (Application.Current == null)
            {
                throw new InvalidOperationException("WPF Application was not initialized by WpfTestFixture.");
            }
        }

        public static void RunOnUiThread(Action action)
        {
            EnsureInitialized();
            if (Application.Current!.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(action);
            }
        }

        public static T RunOnUiThread<T>(Func<T> func)
        {
            T result = default!;
            RunOnUiThread(() => result = func());
            return result;
        }
    }
}
