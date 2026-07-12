#nullable enable
using System;

namespace QuickMediaIngest.Core
{
    /// <summary>Reads width/height from JPEG SOF markers without a full decode.</summary>
    internal static class JpegSofDimensionParser
    {
        public static bool TryGetDimensions(ReadOnlySpan<byte> jpeg, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
            {
                return false;
            }

            int i = 2;
            while (i + 3 < jpeg.Length)
            {
                if (jpeg[i] != 0xFF)
                {
                    i++;
                    continue;
                }

                byte marker = jpeg[i + 1];
                if (marker == 0xFF)
                {
                    i++;
                    continue;
                }

                // SOI / EOI / RSTn — no length field
                if (marker == 0xD8 || marker == 0xD9 || (marker >= 0xD0 && marker <= 0xD7))
                {
                    i += 2;
                    continue;
                }

                if (i + 3 >= jpeg.Length)
                {
                    return false;
                }

                int segLen = (jpeg[i + 2] << 8) | jpeg[i + 3];
                if (segLen < 2 || i + 2 + segLen > jpeg.Length)
                {
                    return false;
                }

                // SOF0–SOF3, SOF5–SOF7, SOF9–SOF11, SOF13–SOF15 (skip DHT/JPG/DAC)
                bool isSof = marker is >= 0xC0 and <= 0xCF
                    && marker is not 0xC4 and not 0xC8 and not 0xCC;
                if (isSof)
                {
                    if (segLen < 7 || i + 8 >= jpeg.Length)
                    {
                        return false;
                    }

                    height = (jpeg[i + 5] << 8) | jpeg[i + 6];
                    width = (jpeg[i + 7] << 8) | jpeg[i + 8];
                    return width > 0 && height > 0;
                }

                i += 2 + segLen;
            }

            return false;
        }

        public static DecodedThumbnail? TryCreate(byte[]? jpegBytes)
        {
            if (jpegBytes == null || jpegBytes.Length == 0)
            {
                return null;
            }

            if (!TryGetDimensions(jpegBytes, out int width, out int height))
            {
                return null;
            }

            var thumb = new DecodedThumbnail(jpegBytes, width, height);
            return ThumbnailPreviewValidator.IsAcceptable(thumb) ? thumb : null;
        }
    }
}
