#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// Removed MetadataExtractor using to avoid ambiguous 'Directory' symbol

namespace QuickMediaIngest.Core
{
    public class ThumbnailService : IThumbnailService
    {
        private readonly ILogger<ThumbnailService> _logger;
        private static readonly string[] RawExtensions =
        {
            ".dng", ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".rw2", ".srw"
        };
        private static readonly string[] VideoExtensions =
        {
            ".mp4", ".mov", ".m4v", ".avi", ".wmv", ".mkv", ".3gp", ".mts", ".m2ts"
        };

        public ThumbnailService(ILogger<ThumbnailService> logger)
        {
            _logger = logger;
        }

        public BitmapSource? GetThumbnail(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            bool isRaw = RawExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
            bool isVideo = VideoExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);

            string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "Thumbnails");
            System.IO.Directory.CreateDirectory(cacheDir);

            string cacheKey = GetCacheKey(filePath);
            string cachePath = Path.Combine(cacheDir, cacheKey + ".jpg");

            // Try to load from disk cache first
            if (File.Exists(cachePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    using (var stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }
                    return bitmap;
                }
                catch { /* If cache is corrupt, fall through to regenerate */ }
            }

            BitmapSource? thumb = null;

            // 1. Embedded EXIF JPEG thumbnail — fastest path for JPEG images.
            if (ext == ".jpg" || ext == ".jpeg")
            {
                try
                {
                    var exifThumb = TryGetExifThumbnail(filePath);
                    if (exifThumb != null) thumb = exifThumb;
                }
                catch { }
            }

            // 1b. For RAW/DNG, ask the Windows shell preview first (more consistent
            // than thumbnail-only for many camera codecs).
            if (thumb == null && isRaw)
            {
                // Best-practice fallback: if companion rendered file exists (JPG/HEIC),
                // use it for thumbnail parity with camera output.
                if (TryGetSiblingRenderedPath(filePath, out string siblingRenderedPath))
                {
                    try
                    {
                        thumb = GetThumbnail(siblingRenderedPath);
                    }
                    catch
                    {
                        // Continue to shell-based RAW thumbnail if sibling lookup fails.
                    }
                }

                if (thumb != null)
                {
                    return thumb;
                }

                try
                {
                    thumb = TryGetShellImage(filePath, 512, thumbnailOnly: false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "RAW shell preview extraction failed for {FilePath}.", filePath);
                }
            }

            // 2. Native WPF decoder (skip RAW to avoid distorted mosaic frames)
            if (thumb == null && !isRaw)
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnLoad);
                        if (decoder.Frames.Count > 0)
                        {
                            thumb = CreateResizedThumbnail(decoder.Frames[0], isRaw ? 320 : 240);
                        }
                    }
                }
                catch { }
            }

            // 3. Windows Shell fallback
            if (thumb == null)
            {
                try
                {
                    thumb = TryGetShellImage(filePath, isRaw || isVideo ? 512 : 240, thumbnailOnly: true);
                    if (thumb == null && isVideo)
                    {
                        // Some video codecs return preview frames only via non-thumbnail mode.
                        thumb = TryGetShellImage(filePath, 512, thumbnailOnly: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Shell thumbnail extraction failed for {FilePath}.", filePath);
                }
            }

            // Save to disk cache if generated
            if (thumb != null)
            {
                try
                {
                    var encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(thumb));
                    using (var fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        encoder.Save(fs);
                    }
                }
                catch { /* Ignore cache write errors */ }
            }

            return thumb;
        }

        private static bool TryGetSiblingRenderedPath(string rawPath, out string siblingPath)
        {
            siblingPath = string.Empty;
            try
            {
                string basePath = Path.Combine(
                    Path.GetDirectoryName(rawPath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(rawPath));

                string[] candidates =
                {
                    basePath + ".jpg",
                    basePath + ".jpeg",
                    basePath + ".heic",
                    basePath + ".heif",
                    basePath + ".JPG",
                    basePath + ".JPEG",
                    basePath + ".HEIC",
                    basePath + ".HEIF"
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        siblingPath = candidate;
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore sibling matching errors.
            }

            return false;
        }

        private static string GetCacheKey(string filePath)
        {
            // Use SourcePath + LastWriteTime as cache key for uniqueness
            const string cacheVersion = "thumb-v4";
            string input = cacheVersion + "|" + filePath + "|" + File.GetLastWriteTimeUtc(filePath).Ticks.ToString();
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static BitmapSource? TryGetExifThumbnail(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var br = new BinaryReader(fs);

                // Scan JPEG segments for APP1 Exif marker (0xFFE1) which starts with "Exif\0\0"
                if (br.ReadByte() != 0xFF || br.ReadByte() != 0xD8) return null; // Not a JPEG

                while (fs.Position < fs.Length)
                {
                    int markerPrefix = br.ReadByte();
                    if (markerPrefix != 0xFF) break;
                    int marker = br.ReadByte();
                    int segLen = (int)ReadUInt16BE(br); // big-endian length
                    if (marker == 0xE1) // APP1
                    {
                        long segStart = fs.Position;
                        byte[] header = br.ReadBytes(6);
                        if (header.Length == 6 && header[0] == (byte)'E' && header[1] == (byte)'x' && header[2] == (byte)'i' && header[3] == (byte)'f' && header[4] == 0 && header[5] == 0)
                        {
                            long tiffStart = segStart + 6;
                            fs.Position = tiffStart;

                            // Read TIFF header
                            bool littleEndian = false;
                            ushort endianMark = ReadUInt16BE(br);
                            if (endianMark == 0x4949) littleEndian = true;
                            else if (endianMark == 0x4D4D) littleEndian = false;
                            else return null;

                            ushort magic = ReadUInt16(br, littleEndian);
                            if (magic != 0x002A) return null;

                            uint ifd0Offset = ReadUInt32(br, littleEndian);
                            long ifd0Pos = tiffStart + ifd0Offset;
                            if (ifd0Pos >= fs.Length) return null;

                            // Read IFD0 entries and then follow pointer to IFD1 (thumbnail IFD)
                            fs.Position = ifd0Pos;
                            ushort entryCount = ReadUInt16(br, littleEndian);
                            fs.Position += entryCount * 12; // skip entries
                            uint nextIfdOffset = ReadUInt32(br, littleEndian);
                            if (nextIfdOffset == 0) return null;

                            long ifd1Pos = tiffStart + nextIfdOffset;
                            if (ifd1Pos >= fs.Length) return null;

                            fs.Position = ifd1Pos;
                            ushort ifd1Count = ReadUInt16(br, littleEndian);
                            for (int i = 0; i < ifd1Count; i++)
                            {
                                long entryPos = fs.Position + i * 12;
                                fs.Position = entryPos;
                                ushort tag = ReadUInt16(br, littleEndian);
                                ushort type = ReadUInt16(br, littleEndian);
                                uint count = ReadUInt32(br, littleEndian);
                                uint valueOffset = ReadUInt32(br, littleEndian);

                                const ushort TagThumbnailOffset = 0x0201;
                                const ushort TagThumbnailLength = 0x0202;

                                if (tag == TagThumbnailOffset)
                                {
                                    uint thumbOffset = valueOffset;
                                    // find length from tag 0x0202
                                    // search entries again for length
                                    fs.Position = ifd1Pos + 2;
                                    int thumbLen = 0;
                                    for (int j = 0; j < ifd1Count; j++)
                                    {
                                        long ePos = fs.Position + j * 12;
                                        fs.Position = ePos;
                                        ushort t = ReadUInt16(br, littleEndian);
                                        fs.Position += 2; // skip type
                                        uint c = ReadUInt32(br, littleEndian);
                                        uint vo = ReadUInt32(br, littleEndian);
                                        if (t == TagThumbnailLength)
                                        {
                                            thumbLen = (int)vo;
                                            break;
                                        }
                                    }

                                    if (thumbLen <= 0) return null;
                                    long thumbDataPos = tiffStart + thumbOffset;
                                    if (thumbDataPos + thumbLen > fs.Length) return null;
                                    fs.Position = thumbDataPos;
                                    byte[] thumbBytes = br.ReadBytes(thumbLen);
                                    if (thumbBytes.Length >= 2 && thumbBytes[0] == 0xFF && thumbBytes[1] == 0xD8)
                                    {
                                        using var ms = new MemoryStream(thumbBytes);
                                        var bitmap = new BitmapImage();
                                        bitmap.BeginInit();
                                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmap.StreamSource = ms;
                                        bitmap.EndInit();
                                        bitmap.Freeze();
                                        return bitmap;
                                    }
                                }
                            }
                        }
                        // advance to next segment
                        fs.Position = segStart + segLen - 2;
                    }
                    else
                    {
                        // skip this segment
                        fs.Position += segLen - 2;
                    }
                }
            }
            catch { }

            return null;
        }

        private static ushort ReadUInt16(BinaryReader br, bool littleEndian)
        {
            var data = br.ReadBytes(2);
            if (data.Length < 2) return 0;
            if (BitConverter.IsLittleEndian == littleEndian) return BitConverter.ToUInt16(data, 0);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        private static ushort ReadUInt16BE(BinaryReader br)
        {
            var data = br.ReadBytes(2);
            if (data.Length < 2) return 0;
            if (!BitConverter.IsLittleEndian) return BitConverter.ToUInt16(data, 0);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        private static uint ReadUInt32(BinaryReader br, bool littleEndian)
        {
            var data = br.ReadBytes(4);
            if (data.Length < 4) return 0u;
            if (BitConverter.IsLittleEndian == littleEndian) return BitConverter.ToUInt32(data, 0);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        private BitmapImage CreateResizedThumbnail(BitmapFrame frame, int decodePixelWidth)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.DecodePixelWidth = Math.Max(120, decodePixelWidth);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            
            var memoryStream = new MemoryStream();
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(frame);
            encoder.Save(memoryStream);
            memoryStream.Position = 0;

            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze(); 
            return bitmap;
        }

        private static BitmapSource? TryGetShellImage(string filePath, int size, bool thumbnailOnly)
        {
            Guid shellItemImageFactoryGuid = new("BCC18B79-BA16-442F-80C4-8A59C30C463B");
            SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref shellItemImageFactoryGuid, out IShellItemImageFactory imageFactory);

            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                ShellItemImageFactoryFlags flags = ShellItemImageFactoryFlags.BiggerSizeOk;
                if (thumbnailOnly)
                {
                    flags |= ShellItemImageFactoryFlags.ThumbnailOnly;
                }

                imageFactory.GetImage(new NativeSize(size, size), flags, out hBitmap);
                if (hBitmap == IntPtr.Zero)
                {
                    return null;
                }

                // Keep the shell-provided bitmap dimensions/aspect ratio.
                // Forcing square dimensions here can distort RAW previews.
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        [ComImport]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage(NativeSize size, ShellItemImageFactoryFlags flags, out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct NativeSize
        {
            public NativeSize(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public int Width { get; }
            public int Height { get; }
        }

        [Flags]
        private enum ShellItemImageFactoryFlags
        {
            BiggerSizeOk = 0x1,
            ThumbnailOnly = 0x8
        }
    }
}
