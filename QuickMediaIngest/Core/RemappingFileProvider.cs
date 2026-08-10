#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    /// <summary>Remaps FTP-style source paths onto an Android media root before delegating to ADB.</summary>
    public sealed class RemappingFileProvider : IFileProvider, IAsyncDisposable
    {
        private readonly IFileProvider _inner;
        private readonly string _mediaRootPrefix;
        private readonly IAdbPathProbe? _pathProbe;
        private readonly string? _deviceSerial;

        public RemappingFileProvider(IFileProvider inner, string mediaRootPrefix)
            : this(inner, mediaRootPrefix, pathProbe: null, deviceSerial: null)
        {
        }

        public RemappingFileProvider(
            IFileProvider inner,
            string mediaRootPrefix,
            IAdbPathProbe? pathProbe,
            string? deviceSerial)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (string.IsNullOrWhiteSpace(mediaRootPrefix))
            {
                throw new ArgumentException("Media root prefix is required.", nameof(mediaRootPrefix));
            }

            _mediaRootPrefix = mediaRootPrefix.Trim().TrimEnd('/');
            _pathProbe = pathProbe;
            _deviceSerial = deviceSerial;
        }

        public string MediaRootPrefix => _mediaRootPrefix;

        /// <summary>True when the inner provider is <see cref="AdbFileProvider"/> (PreferAdb remapping).</summary>
        public bool InnerIsAdb => _inner is AdbFileProvider;

        public Task CopyAsync(
            string srcPath,
            string destPath,
            CancellationToken token,
            IProgress<long>? bytesCopied = null,
            long expectedBytes = 0) =>
            _inner.CopyAsync(ResolveDevicePath(srcPath), destPath, token, bytesCopied, expectedBytes);

        public Task DeleteAsync(string srcPath, CancellationToken token) =>
            _inner.DeleteAsync(ResolveDevicePath(srcPath), token);

        private string ResolveDevicePath(string srcPath)
        {
            if (_pathProbe == null || string.IsNullOrEmpty(_deviceSerial))
            {
                return AdbAndroidPath.ToDevicePath(_mediaRootPrefix, srcPath);
            }

            foreach (string candidate in FtpMediaPathNormalizer.GetRetrCandidates(srcPath))
            {
                string device = AdbAndroidPath.ToDevicePath(_mediaRootPrefix, candidate);
                if (_pathProbe.FileExists(_deviceSerial, device))
                {
                    return device;
                }
            }

            return AdbAndroidPath.ToDevicePath(_mediaRootPrefix, srcPath);
        }

        public ValueTask DisposeAsync()
        {
            if (_inner is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            if (_inner is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return ValueTask.CompletedTask;
        }
    }
}
