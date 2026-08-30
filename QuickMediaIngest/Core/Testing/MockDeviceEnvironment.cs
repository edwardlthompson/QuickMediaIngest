#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Testing
{
    public sealed class MockRemovableVolume : IDisposable
    {
        public string VolumeRoot { get; }
        public string VolumeLabel { get; }

        public MockRemovableVolume(string label = "EOS_DIGITAL")
        {
            VolumeLabel = label;
            VolumeRoot = Path.Combine(Path.GetTempPath(), $"MockVolume_{label}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(VolumeRoot);
            PopulateStandardDcimStructure();
        }

        private void PopulateStandardDcimStructure()
        {
            string dcim = Path.Combine(VolumeRoot, "DCIM", "100CANON");
            Directory.CreateDirectory(dcim);
            File.WriteAllText(Path.Combine(dcim, "IMG_0001.JPG"), "fake-jpeg-data");
            File.WriteAllText(Path.Combine(dcim, "IMG_0002.CR3"), "fake-raw-data");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(VolumeRoot))
                {
                    Directory.Delete(VolumeRoot, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    public sealed class MockFtpListing
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 2121;
        public List<ImportItem> MockItems { get; set; } = new();

        public static MockFtpListing CreateSampleSonyListing()
        {
            return new MockFtpListing
            {
                Host = "192.168.1.50",
                Port = 21,
                MockItems = new List<ImportItem>
                {
                    new ImportItem
                    {
                        FileName = "DSC0001.ARW",
                        FileSize = 24_000_000,
                        DateTaken = DateTime.Now.AddHours(-1),
                        FileType = "ARW",
                        IsFtpSource = true,
                        SourcePath = "/DCIM/100MSDCF/DSC0001.ARW"
                    },
                    new ImportItem
                    {
                        FileName = "DSC0001.JPG",
                        FileSize = 4_000_000,
                        DateTaken = DateTime.Now.AddHours(-1),
                        FileType = "JPG",
                        IsFtpSource = true,
                        SourcePath = "/DCIM/100MSDCF/DSC0001.JPG"
                    }
                }
            };
        }
    }
}
