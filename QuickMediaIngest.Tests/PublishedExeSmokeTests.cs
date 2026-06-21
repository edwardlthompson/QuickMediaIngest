#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace QuickMediaIngest.Tests
{
    /// <summary>Smoke tests for publish/local-test portable exe (libvips native bundle).</summary>
    public class PublishedExeSmokeTests
    {
        private readonly ITestOutputHelper _output;

        public PublishedExeSmokeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void PublishedExe_SmokeLibvips_ExitsZeroWhenBuilt()
        {
            string exePath = ResolvePublishedExePath();
            bool require = Environment.GetEnvironmentVariable("QMI_SMOKE_REQUIRE_PUBLISHED") == "1";
            bool invokedByScript = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("QMI_SMOKE_PUBLISHED_EXE"));

            if (!File.Exists(exePath))
            {
                _output.WriteLine($"SKIP: published exe not found at {exePath} (run scripts/smoke-published-exe.ps1 -Rebuild).");
                if (require)
                {
                    Assert.Fail($"QMI_SMOKE_REQUIRE_PUBLISHED=1 but exe missing: {exePath}");
                }

                return;
            }

            if (!invokedByScript && !require)
            {
                _output.WriteLine("SKIP: run scripts/smoke-published-exe.ps1 to build and smoke the published exe.");
                return;
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "--smoke-libvips",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            Assert.True(process.Start(), "Failed to start published exe smoke process.");
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            Assert.True(process.WaitForExit(TimeSpan.FromMinutes(2)), "Published exe smoke timed out.");
            _output.WriteLine($"stdout={stdout.Trim()} stderr={stderr.Trim()} exit={process.ExitCode}");

            Assert.Equal(0, process.ExitCode);
            Assert.Contains("OK libvips", stdout, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolvePublishedExePath()
        {
            string fromEnv = Environment.GetEnvironmentVariable("QMI_SMOKE_PUBLISHED_EXE") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv;
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "publish", "local-test", "QuickMediaIngest.exe"));
        }
    }
}
