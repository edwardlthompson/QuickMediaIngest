#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core;
using Xunit;
using Xunit.Abstractions;

namespace QuickMediaIngest.Tests
{
    /// <summary>PreferAdb MediaStore JPEG smoke for DCIM Camera MP4s (skips when no device).</summary>
    public class AdbVideoThumbnailPreferAdbSmokeTests
    {
        private readonly ITestOutputHelper _output;

        public AdbVideoThumbnailPreferAdbSmokeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TryFetchVideoThumbJpeg_DcimCameraMp4s_WhenDeviceAttached()
        {
            if (!AdbDeviceProbe.IsAdbAvailable())
            {
                _output.WriteLine("adb unavailable — skip");
                return;
            }

            string? serial = AdbDeviceProbe.GetFirstDeviceSerial();
            if (string.IsNullOrWhiteSpace(serial))
            {
                _output.WriteLine("no device — skip");
                return;
            }

            string[] mp4Names = await ListDcimMp4NamesAsync(serial);
            if (mp4Names.Length == 0)
            {
                _output.WriteLine("no DCIM MP4s — skip");
                return;
            }

            var fetcher = new AdbVideoThumbnailFetcher(NullLogger<AdbVideoThumbnailFetcher>.Instance);
            var session = new AdbTransferSession(serial, "/sdcard/DCIM");
            string tempDir = Path.Combine(Path.GetTempPath(), "qmi_vid_thumb_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                int ok = 0;
                foreach (string name in mp4Names)
                {
                    string local = Path.Combine(tempDir, Path.ChangeExtension(name, ".jpg"));
                    string remote = $"/Camera/{name}";
                    bool got = await fetcher.TryFetchVideoThumbJpegAsync(
                        session,
                        remote,
                        local,
                        CancellationToken.None);
                    long len = got && File.Exists(local) ? new FileInfo(local).Length : 0;
                    _output.WriteLine($"{name}: got={got} bytes={len}");
                    if (got && len >= 64)
                    {
                        ok++;
                    }
                }

                Assert.True(ok > 0, "Expected at least one MediaStore JPEG thumb from connected device.");
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // ignore
                }
            }
        }

        private static async Task<string[]> ListDcimMp4NamesAsync(string serial)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {serial} shell ls /sdcard/DCIM/Camera/*.mp4",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null)
                {
                    return Array.Empty<string>();
                }

                string stdout = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return stdout
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => Path.GetFileName(line.Trim()))
                    .Where(n => n.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                    .Take(3)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
