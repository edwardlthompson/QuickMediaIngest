#nullable enable

using System;

using System.IO;

using System.Windows.Media.Imaging;

using ImageMagick;



namespace QuickMediaIngest.Core

{

    internal static class MagickThumbnailDecoder

    {

        public static BitmapSource? TryGetThumbnail(string filePath, int decodePixelWidth)

        {

            if (!File.Exists(filePath))

            {

                return null;

            }



            try

            {

                using var image = new MagickImage(filePath);

                image.AutoOrient();

                uint size = (uint)Math.Max(120, decodePixelWidth);

                image.Thumbnail(size, size);



                byte[] bmpBytes;

                using (var memoryStream = new MemoryStream())

                {

                    image.Write(memoryStream, MagickFormat.Bmp);

                    bmpBytes = memoryStream.ToArray();

                }



                if (bmpBytes.Length == 0)

                {

                    return null;

                }



                using var pixelStream = new MemoryStream(bmpBytes);

                BitmapFrame frame = BitmapFrame.Create(

                    pixelStream,

                    BitmapCreateOptions.None,

                    BitmapCacheOption.OnLoad);

                frame.Freeze();

                return frame;

            }

            catch

            {

                return null;

            }

        }

    }

}

