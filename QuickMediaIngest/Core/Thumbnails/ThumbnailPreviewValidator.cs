#nullable enable
using System;
using ImageMagick;

namespace QuickMediaIngest.Core
{
    /// <summary>Rejects corrupt, placeholder-sized, or glitchy decode results from partial FTP/ADB buffers.</summary>
    internal static class ThumbnailPreviewValidator
    {
        public const int MinPixelEdge = 32;

        public static bool IsAcceptable(DecodedThumbnail? thumb)
        {
            if (thumb == null)
            {
                return false;
            }

            if (thumb.JpegBytes == null || thumb.JpegBytes.Length == 0)
            {
                return false;
            }

            if (thumb.Width < MinPixelEdge || thumb.Height < MinPixelEdge)
            {
                return false;
            }

            return !LooksGlitchy(thumb.JpegBytes, thumb.Width, thumb.Height);
        }

        /// <summary>
        /// Detects extreme green/magenta dominance and stripe banding typical of truncated Magick RAW.
        /// </summary>
        internal static bool LooksGlitchy(byte[] jpegBytes, int width, int height)
        {
            if (jpegBytes.Length < 16)
            {
                return true;
            }

            // Not a JPEG payload — skip chroma heuristics (unit stubs).
            if (jpegBytes[0] != 0xFF || jpegBytes[1] != 0xD8)
            {
                return false;
            }

            try
            {
                using var image = new MagickImage(jpegBytes);
                image.AutoOrient();
                uint sampleW = (uint)Math.Clamp(Math.Max(width / 4, 16), 16, 64);
                uint sampleH = (uint)Math.Clamp(Math.Max(height / 4, 16), 16, 64);
                image.Thumbnail(sampleW, sampleH);

                using var pixels = image.GetPixels();
                int w = (int)image.Width;
                int h = (int)image.Height;
                if (w < 4 || h < 4)
                {
                    return false;
                }

                long sumR = 0, sumG = 0, sumB = 0;
                int count = 0;
                int bandMatches = 0;
                int bandChecks = 0;

                int stepX = Math.Max(1, w / 16);
                int stepY = Math.Max(1, h / 16);
                byte[]? prevRowG = null;
                for (int y = 0; y < h; y += stepY)
                {
                    var rowG = new byte[(w / stepX) + 1];
                    int ri = 0;
                    for (int x = 0; x < w; x += stepX)
                    {
                        IMagickColor<ushort>? color = pixels.GetPixel(x, y).ToColor();
                        if (color == null)
                        {
                            continue;
                        }

                        // Quantum depth may be 16-bit; scale to 0–255 for heuristics.
                        byte r = (byte)(color.R >> 8);
                        byte g = (byte)(color.G >> 8);
                        byte b = (byte)(color.B >> 8);
                        sumR += r;
                        sumG += g;
                        sumB += b;
                        count++;
                        rowG[ri++] = g;
                    }

                    if (prevRowG != null && ri > 0)
                    {
                        int compare = Math.Min(prevRowG.Length, ri);
                        int same = 0;
                        for (int i = 0; i < compare; i++)
                        {
                            if (Math.Abs(prevRowG[i] - rowG[i]) <= 2)
                            {
                                same++;
                            }
                        }

                        bandChecks++;
                        if (same * 100 / compare >= 90)
                        {
                            bandMatches++;
                        }
                    }

                    prevRowG = rowG;
                }

                if (count == 0)
                {
                    return false;
                }

                double avgR = sumR / (double)count;
                double avgG = sumG / (double)count;
                double avgB = sumB / (double)count;
                double lum = 0.299 * avgR + 0.587 * avgG + 0.114 * avgB;

                bool greenFlood = avgG > 140 && avgG > avgR * 1.55 && avgG > avgB * 1.55 && lum < 200;
                bool magentaFlood = avgR > 120 && avgB > 120 && avgG < avgR * 0.55 && avgG < avgB * 0.55;

                if (greenFlood || magentaFlood)
                {
                    return true;
                }

                // Banding alone is common in skies/walls — only reject with mild chroma skew.
                bool mildGreenSkew = avgG > avgR * 1.25 && avgG > avgB * 1.25;
                bool mildMagentaSkew = avgR > avgG * 1.25 && avgB > avgG * 1.25;
                if (bandChecks >= 4 && bandMatches * 100 / bandChecks >= 70 && (mildGreenSkew || mildMagentaSkew))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                // SOI present but Magick cannot decode — reject (common HEIC false-positive spans).
                return true;
            }
        }
    }
}
