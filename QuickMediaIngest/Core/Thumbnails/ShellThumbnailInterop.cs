#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickMediaIngest.Core
{
    internal static class ShellThumbnailInterop
    {
        public static BitmapSource? TryGetShellImage(string filePath, int size, bool thumbnailOnly)
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
        private static extern void SHCreateItemFromParsingName(
            string pszPath,
            IntPtr pbc,
            ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

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
            ThumbnailOnly = 0x8,
        }
    }
}
