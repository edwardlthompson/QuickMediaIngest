#nullable enable
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickMediaIngest.Core
{
    /// <summary>Rejects corrupt or placeholder-sized decode results from partial FTP buffers.</summary>
    internal static class ThumbnailPreviewValidator
    {
        public const int MinPixelEdge = 32;

        public static bool IsAcceptable(BitmapSource? thumb)
        {
            if (thumb == null)
            {
                return false;
            }

            if (thumb.PixelWidth < MinPixelEdge || thumb.PixelHeight < MinPixelEdge)
            {
                return false;
            }

            long pixels = (long)thumb.PixelWidth * thumb.PixelHeight;
            if (pixels < MinPixelEdge * MinPixelEdge)
            {
                return false;
            }

            return HasPlausibleImageContent(thumb);
        }

        private static bool HasPlausibleImageContent(BitmapSource thumb)
        {
            try
            {
                BitmapSource source = thumb;
                if (thumb.Format != PixelFormats.Bgra32 && thumb.Format != PixelFormats.Pbgra32)
                {
                    source = new FormatConvertedBitmap(thumb, PixelFormats.Bgra32, null, 0);
                    source.Freeze();
                }

                int width = source.PixelWidth;
                int height = source.PixelHeight;
                int stride = width * 4;
                byte[] pixels = new byte[stride * height];
                source.CopyPixels(pixels, stride, 0);

                int samples = 0;
                int greenDominant = 0;
                int blackDominant = 0;
                int luminanceSum = 0;
                int luminanceSumSq = 0;

                for (int sy = 0; sy < 8; sy++)
                {
                    for (int sx = 0; sx < 8; sx++)
                    {
                        int x = width == 1 ? 0 : (sx * (width - 1)) / 7;
                        int y = height == 1 ? 0 : (sy * (height - 1)) / 7;
                        int index = (y * stride) + (x * 4);
                        byte b = pixels[index];
                        byte g = pixels[index + 1];
                        byte r = pixels[index + 2];
                        int luminance = r + g + b;
                        luminanceSum += luminance;
                        luminanceSumSq += luminance * luminance;
                        samples++;

                        if (g > r + 35 && g > b + 35 && g > 80)
                        {
                            greenDominant++;
                        }

                        if (r < 24 && g < 24 && b < 24)
                        {
                            blackDominant++;
                        }
                    }
                }

                if (samples == 0)
                {
                    return false;
                }

                if (greenDominant * 100 / samples >= 55)
                {
                    return false;
                }

                if (blackDominant * 100 / samples >= 80)
                {
                    return false;
                }

                double mean = luminanceSum / (double)samples;
                double variance = (luminanceSumSq / (double)samples) - (mean * mean);
                return variance >= 40;
            }
            catch
            {
                return true;
            }
        }
    }
}
