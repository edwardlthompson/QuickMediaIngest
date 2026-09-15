#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>One hasher overlaps dest SHA-256 with copy/delete. Drops each file as it verifies.</summary>
    internal sealed partial class IngestBenchImportVerify
    {
        private readonly IngestBenchHost _host;
        private readonly int _total;
        private readonly Stopwatch _clock;
        private readonly Channel<(string source, string dest)> _channel;
        private readonly Task _worker;
        private readonly Dictionary<string, List<(string file, string hash)>> _manifests = new(StringComparer.OrdinalIgnoreCase);
        private readonly bool _writeManifest;
        private readonly long[] _stamp = new long[1];
        private long _filterStamp;
        private int _copy;
        private int _verify;

        public IReadOnlyDictionary<string, List<(string file, string hash)>> Manifests => _manifests;

        public static IngestBenchImportVerify Start(
            IngestBenchHost host, int total, Stopwatch clock, CancellationToken token) =>
            new(host, Math.Max(1, total), clock, token);

        public static async Task<IReadOnlyDictionary<string, List<(string file, string hash)>>> CopyAndHashAsync(
            IngestBenchHost host,
            IReadOnlyList<ItemGroup> groups,
            IngestOptions options,
            Stopwatch clock,
            CancellationToken token)
        {
            int total = groups.Sum(g => g.Items.Count(i => i.IsSelected));
            IngestBenchImportVerify session = Start(host, total, clock, token);
            await session.RunCopyAsync(host, groups, options, token).ConfigureAwait(false);
            return session.Manifests;
        }

        private IngestBenchImportVerify(IngestBenchHost host, int total, Stopwatch clock, CancellationToken token)
        {
            _host = host;
            _total = total;
            _clock = clock;
            _writeManifest = host.Model.Prefs.WriteChecksumManifest;
            _channel = Channel.CreateUnbounded<(string, string)>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
            });
            _worker = Task.Run(() => HashLoop(token), CancellationToken.None);
            Publish(force: true);
        }

        public async Task RunCopyAsync(
            IngestBenchHost host,
            IReadOnlyList<ItemGroup> groups,
            IngestOptions options,
            CancellationToken token)
        {
            try
            {
                await IngestBenchImportCopy.RunGroupsAsync(host, groups, options, token, this).ConfigureAwait(false);
            }
            finally
            {
                Complete();
            }

            await Drain().ConfigureAwait(false);
        }

        internal void Complete() => _channel.Writer.TryComplete();

        internal async Task Drain()
        {
            try
            {
                await _worker.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }

            _host.Post(() => IngestBenchShoots.ApplyFilter(_host));
        }

        public void NoteCopy(IngestProgressInfo info)
        {
            if (!info.Success)
            {
                IngestBenchQueue.NoteFailed(_host, new[] { info.SourcePath });
                Interlocked.Increment(ref _copy);
                Publish(force: false);
                return;
            }

            int n = Interlocked.Increment(ref _copy);
            string dest = info.DestinationPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dest) || !File.Exists(dest))
            {
                IngestBenchQueue.NoteFailed(_host, new[] { info.SourcePath });
                Publish(force: n >= _total);
                return;
            }

            _channel.Writer.TryWrite((info.SourcePath, dest));
            Publish(force: n >= _total);
        }
    }
}
