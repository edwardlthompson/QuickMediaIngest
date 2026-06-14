#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows;

namespace QuickMediaIngest.Core
{
    /// <summary>Runs work on a dedicated STA thread for Shell/COM thumbnail extraction.</summary>
    internal static class StaRunner
    {
        private static readonly BlockingCollection<Action> Queue = new();
        private static readonly Thread StaThread;
        private static bool _wpfInitialized;

        static StaRunner()
        {
            StaThread = new Thread(ProcessQueue)
            {
                IsBackground = true,
                Name = "QuickMediaIngest.ThumbnailSta"
            };
            StaThread.SetApartmentState(ApartmentState.STA);
            StaThread.Start();
        }

        public static T Run<T>(Func<T> func)
        {
            T result = default!;
            Exception? error = null;
            using var done = new ManualResetEventSlim(false);

            Queue.Add(() =>
            {
                try
                {
                    EnsureWpfApplication();
                    result = func();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    done.Set();
                }
            });

            done.Wait();
            if (error != null)
            {
                throw error;
            }

            return result;
        }

        private static void EnsureWpfApplication()
        {
            if (_wpfInitialized)
            {
                return;
            }

            if (Application.Current == null)
            {
                _ = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }

            _wpfInitialized = true;
        }

        private static void ProcessQueue()
        {
            foreach (Action action in Queue.GetConsumingEnumerable())
            {
                action();
            }
        }
    }
}
