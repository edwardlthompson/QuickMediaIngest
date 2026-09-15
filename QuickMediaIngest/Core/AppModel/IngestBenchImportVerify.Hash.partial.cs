#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core.AppModel
{
    internal sealed partial class IngestBenchImportVerify
    {
        private async Task HashLoop(CancellationToken token)
        {
            try
            {
                await foreach ((string source, string dest) job in _channel.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    await HashOne(job.source, job.dest, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task HashOne(string source, string dest, CancellationToken token)
        {
            bool drop = true;
            try
            {
                if (!File.Exists(dest))
                {
                    _host.Post(() => IngestBenchQueue.NoteFailed(_host, new[] { source }));
                    drop = false;
                }
                else
                {
                    string hash = await _host.HashCatalog.ComputeFileHashAsync(dest, token).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(hash))
                    {
                        _host.HashCatalog.RecordImported(hash, dest);
                        RememberManifest(dest, hash);
                    }
                    else if (!File.Exists(dest))
                    {
                        _host.Post(() => IngestBenchQueue.NoteFailed(_host, new[] { source }));
                        drop = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                drop = File.Exists(dest);
            }
            catch
            {
                drop = File.Exists(dest);
                if (!drop)
                {
                    _host.Post(() => IngestBenchQueue.NoteFailed(_host, new[] { source }));
                }
            }

            Interlocked.Increment(ref _verify);
            if (drop)
            {
                bool refresh = ShouldRefresh();
                _host.Post(() => IngestBenchShootsDrop.DropOne(_host, source, refresh));
            }

            Publish(force: Volatile.Read(ref _verify) >= _total);
        }

        private void RememberManifest(string dest, string hash)
        {
            if (!_writeManifest)
            {
                return;
            }

            string folder = Path.GetDirectoryName(dest) ?? string.Empty;
            lock (_manifests)
            {
                if (!_manifests.TryGetValue(folder, out List<(string file, string hash)>? rows))
                {
                    rows = new List<(string file, string hash)>();
                    _manifests[folder] = rows;
                }

                rows.Add((dest, hash));
            }
        }

        private bool ShouldRefresh()
        {
            long now = Environment.TickCount64;
            if (now - _filterStamp < 120)
            {
                return false;
            }

            _filterStamp = now;
            return true;
        }

        private void Publish(bool force)
        {
            long now = Environment.TickCount64;
            if (!force && now - _stamp[0] < 120)
            {
                return;
            }

            _stamp[0] = now;
            int copy = Volatile.Read(ref _copy);
            int verify = Volatile.Read(ref _verify);
            TimeSpan elapsed = _clock.Elapsed;
            _host.Post(() => IngestBenchActivity.Report(
                _host.Model,
                IngestBenchImportProgress.Percent(verify, _total),
                IngestBenchImportProgress.Dual(copy, verify, _total, elapsed)));
        }
    }
}
