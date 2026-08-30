#nullable enable
using System;
using System.Collections.Generic;

namespace QuickMediaIngest.Core.Services
{
    public enum CameraBrand
    {
        Unknown = 0,
        Sony = 1,
        Canon = 2,
        Nikon = 3,
        Fujifilm = 4,
        Panasonic = 5,
    }

    public sealed class CameraWifiPreset
    {
        public CameraBrand Brand { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DefaultPort { get; set; } = 21;
        public List<string> StandardScanPaths { get; set; } = new();
    }

    public static class CameraWifiProfilePresets
    {
        public static IReadOnlyList<CameraWifiPreset> Presets { get; } = new List<CameraWifiPreset>
        {
            new CameraWifiPreset
            {
                Brand = CameraBrand.Sony,
                Name = "Sony Alpha (FTP / Wi-Fi)",
                DefaultPort = 21,
                StandardScanPaths = new List<string> { "/DCIM", "/DCIM/100MSDCF", "/PRIVATE/M4ROOT/CLIP" }
            },
            new CameraWifiPreset
            {
                Brand = CameraBrand.Canon,
                Name = "Canon EOS (FTP / Wi-Fi)",
                DefaultPort = 21,
                StandardScanPaths = new List<string> { "/DCIM", "/DCIM/100CANON", "/DCIM/CANONMSC" }
            },
            new CameraWifiPreset
            {
                Brand = CameraBrand.Nikon,
                Name = "Nikon Z / D (FTP / Wi-Fi)",
                DefaultPort = 21,
                StandardScanPaths = new List<string> { "/DCIM", "/DCIM/100NC_D", "/DCIM/100NIKON" }
            },
            new CameraWifiPreset
            {
                Brand = CameraBrand.Fujifilm,
                Name = "Fujifilm X / GFX (FTP / Wi-Fi)",
                DefaultPort = 21,
                StandardScanPaths = new List<string> { "/DCIM", "/DCIM/100_FUJI" }
            },
            new CameraWifiPreset
            {
                Brand = CameraBrand.Panasonic,
                Name = "Panasonic Lumix (FTP / Wi-Fi)",
                DefaultPort = 21,
                StandardScanPaths = new List<string> { "/DCIM", "/DCIM/100_PANA", "/PRIVATE/PANA_GRP" }
            }
        };

        public static CameraWifiPreset? FindPresetForBrand(CameraBrand brand)
        {
            foreach (var preset in Presets)
            {
                if (preset.Brand == brand) return preset;
            }
            return null;
        }
    }
}
