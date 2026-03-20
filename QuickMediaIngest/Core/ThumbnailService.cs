using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace QuickMediaIngest.Core
{
    public class ThumbnailService
    {
        public BitmapSource? GetThumbnail(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            // 1. Try Native WPF Decoder First
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        return CreateResizedThumbnail(frame);
                    }
                }
            }
            catch { /* Native decoder failed, fallback to EXIF */ }

            // 2. Fallback to MetadataExtractor (Embedded JPEG thumbnail usually in RAWs like CR2)
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var thumbDir = directories.OfType<ExifThumbnailDirectory>().FirstOrDefault();
                
                if (thumbDir != null)
                {
                    const int TagThumbnailOffset = 513; // 0x0201
                    const int TagThumbnailLength = 514; // 0x0202

                    if (thumbDir.ContainsTag(TagThumbnailOffset) && thumbDir.ContainsTag(TagThumbnailLength))
                    {
                        int offset = thumbDir.GetInt32(TagThumbnailOffset);
                        int length = thumbDir.GetInt32(TagThumbnailLength);

                        if (length > 0)
                        {
                            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                fs.Seek(offset, SeekOrigin.Begin);
                                byte[] thumbBytes = new byte[length];
                                int read = fs.Read(thumbBytes, 0, length);

                                if (read > 4 && thumbBytes[0] == 0xFF && thumbBytes[1] == 0xD8) // Safe JPEG Header check
                                {
                                    using (var ms = new MemoryStream(thumbBytes))
                                    {
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
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Thumbnail Service Error] {filePath}: {ex.Message}");
            }

            // 3. Last-resort Windows Shell thumbnail extraction for codec-backed formats like DNG/HEIC.
            try
            {
                var shellThumb = TryGetShellThumbnail(filePath, 160);
                if (shellThumb != null)
                {
                    return shellThumb;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Shell Thumbnail Error] {filePath}: {ex.Message}");
            }

            return null;
        }

        private BitmapImage CreateResizedThumbnail(BitmapFrame frame)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.DecodePixelWidth = 120; // Match Card widths well
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

        private static BitmapSource? TryGetShellThumbnail(string filePath, int size)
        {
            Guid shellItemImageFactoryGuid = new("BCC18B79-BA16-442F-80C4-8A59C30C463B");
            SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref shellItemImageFactoryGuid, out IShellItemImageFactory imageFactory);

            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                imageFactory.GetImage(new NativeSize(size, size), ShellItemImageFactoryFlags.BiggerSizeOk | ShellItemImageFactoryFlags.ThumbnailOnly, out hBitmap);
                if (hBitmap == IntPtr.Zero)
                {
                    return null;
                }

                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(size, size));
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
