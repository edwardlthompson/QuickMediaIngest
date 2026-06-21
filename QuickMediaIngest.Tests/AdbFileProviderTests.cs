#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class AdbFileProviderTests
    {
        [Fact]
        public void AdbDeviceProbe_ListDeviceSerials_ReturnsConnectedDevice()
        {
            if (!AdbDeviceProbe.IsAdbAvailable())
            {
                return;
            }

            var serials = AdbDeviceProbe.ListDeviceSerials();
            if (serials.Count == 0)
            {
                return; // adb present but no device — skip (CI / offline)
            }

            Assert.All(serials, s => Assert.False(string.IsNullOrWhiteSpace(s)));
        }

        [Fact]
        public async Task CopyAsync_PullsFileFromConnectedDevice()
        {
            string? serial = AdbDeviceProbe.GetFirstDeviceSerial();
            if (string.IsNullOrWhiteSpace(serial))
            {
                return;
            }

            string remotePath = await FindSampleDcimFileAsync(serial);
            if (remotePath == null)
            {
                return;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "qmi_adb_test_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            string localPath = Path.Combine(tempDir, Path.GetFileName(remotePath));
            try
            {
                var logger = new Mock<ILogger<AdbFileProvider>>();
                var provider = new AdbFileProvider(serial, logger.Object);
                await provider.CopyAsync(remotePath, localPath, CancellationToken.None);

                Assert.True(File.Exists(localPath));
                Assert.True(new FileInfo(localPath).Length > 0);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore cleanup failures.
                }
            }
        }

        private static async Task<string?> FindSampleDcimFileAsync(string serial)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {serial} shell ls /sdcard/DCIM",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null)
                {
                    return null;
                }

                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    return null;
                }

                string? fileName = process.StandardOutput.ReadToEnd()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .FirstOrDefault(line => line.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                        || line.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

                return fileName == null ? null : $"/sdcard/DCIM/{fileName}";
            }
            catch
            {
                return null;
            }
        }
    }
}
