#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core.Ftp
{
    public sealed class FtpBandwidthThrottler
    {
        private readonly long _bytesPerSecondLimit;
        private readonly Stopwatch _stopwatch = new();
        private long _bytesTransferred;

        public long BytesPerSecondLimit => _bytesPerSecondLimit;

        public FtpBandwidthThrottler(long bytesPerSecondLimit)
        {
            _bytesPerSecondLimit = Math.Max(0, bytesPerSecondLimit);
            _stopwatch.Start();
        }

        public async Task ThrottleAsync(int bytesRead, CancellationToken cancellationToken = default)
        {
            if (_bytesPerSecondLimit <= 0 || bytesRead <= 0)
            {
                return;
            }

            _bytesTransferred += bytesRead;
            double targetSeconds = (double)_bytesTransferred / _bytesPerSecondLimit;
            double elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;

            if (targetSeconds > elapsedSeconds)
            {
                int delayMs = (int)((targetSeconds - elapsedSeconds) * 1000);
                if (delayMs > 0)
                {
                    await Task.Delay(Math.Min(delayMs, 1000), cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
