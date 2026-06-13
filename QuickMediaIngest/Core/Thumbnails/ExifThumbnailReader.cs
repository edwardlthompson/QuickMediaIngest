#nullable enable
using System;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    internal static class ExifThumbnailReader
    {
        public static BitmapSource? TryGetExifThumbnail(string filePath, ILogger? logger = null)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var br = new BinaryReader(fs);

                if (br.ReadByte() != 0xFF || br.ReadByte() != 0xD8)
                {
                    return null;
                }

                while (fs.Position < fs.Length)
                {
                    int markerPrefix = br.ReadByte();
                    if (markerPrefix != 0xFF)
                    {
                        break;
                    }

                    int marker = br.ReadByte();
                    int segLen = (int)ExifBinaryReaders.ReadUInt16BE(br);
                    if (marker == 0xE1)
                    {
                        long segStart = fs.Position;
                        byte[] header = br.ReadBytes(6);
                        if (header.Length == 6
                            && header[0] == (byte)'E'
                            && header[1] == (byte)'x'
                            && header[2] == (byte)'i'
                            && header[3] == (byte)'f'
                            && header[4] == 0
                            && header[5] == 0)
                        {
                            var thumb = TryReadThumbnailFromExifSegment(fs, br, segStart + 6);
                            if (thumb != null)
                            {
                                return thumb;
                            }
                        }

                        fs.Position = segStart + segLen - 2;
                    }
                    else
                    {
                        fs.Position += segLen - 2;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "EXIF segment parse failed for {FilePath}.", filePath);
            }

            return null;
        }

        private static BitmapSource? TryReadThumbnailFromExifSegment(Stream fs, BinaryReader br, long tiffStart)
        {
            fs.Position = tiffStart;

            bool littleEndian;
            ushort endianMark = ExifBinaryReaders.ReadUInt16BE(br);
            if (endianMark == 0x4949)
            {
                littleEndian = true;
            }
            else if (endianMark == 0x4D4D)
            {
                littleEndian = false;
            }
            else
            {
                return null;
            }

            ushort magic = ExifBinaryReaders.ReadUInt16(br, littleEndian);
            if (magic != 0x002A)
            {
                return null;
            }

            uint ifd0Offset = ExifBinaryReaders.ReadUInt32(br, littleEndian);
            long ifd0Pos = tiffStart + ifd0Offset;
            if (ifd0Pos >= fs.Length)
            {
                return null;
            }

            fs.Position = ifd0Pos;
            ushort entryCount = ExifBinaryReaders.ReadUInt16(br, littleEndian);
            fs.Position += entryCount * 12;
            uint nextIfdOffset = ExifBinaryReaders.ReadUInt32(br, littleEndian);
            if (nextIfdOffset == 0)
            {
                return null;
            }

            long ifd1Pos = tiffStart + nextIfdOffset;
            if (ifd1Pos >= fs.Length)
            {
                return null;
            }

            fs.Position = ifd1Pos;
            ushort ifd1Count = ExifBinaryReaders.ReadUInt16(br, littleEndian);
            for (int i = 0; i < ifd1Count; i++)
            {
                long entryPos = fs.Position + i * 12;
                fs.Position = entryPos;
                ushort tag = ExifBinaryReaders.ReadUInt16(br, littleEndian);
                ExifBinaryReaders.ReadUInt16(br, littleEndian);
                ExifBinaryReaders.ReadUInt32(br, littleEndian);
                uint valueOffset = ExifBinaryReaders.ReadUInt32(br, littleEndian);

                const ushort TagThumbnailOffset = 0x0201;
                const ushort TagThumbnailLength = 0x0202;

                if (tag != TagThumbnailOffset)
                {
                    continue;
                }

                int thumbLen = FindThumbnailLength(fs, br, ifd1Pos, ifd1Count, littleEndian, TagThumbnailLength);
                if (thumbLen <= 0)
                {
                    return null;
                }

                long thumbDataPos = tiffStart + valueOffset;
                if (thumbDataPos + thumbLen > fs.Length)
                {
                    return null;
                }

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

            return null;
        }

        private static int FindThumbnailLength(
            Stream fs,
            BinaryReader br,
            long ifd1Pos,
            int ifd1Count,
            bool littleEndian,
            ushort tagThumbnailLength)
        {
            fs.Position = ifd1Pos + 2;
            for (int j = 0; j < ifd1Count; j++)
            {
                long ePos = fs.Position + j * 12;
                fs.Position = ePos;
                ushort t = ExifBinaryReaders.ReadUInt16(br, littleEndian);
                fs.Position += 2;
                ExifBinaryReaders.ReadUInt32(br, littleEndian);
                uint vo = ExifBinaryReaders.ReadUInt32(br, littleEndian);
                if (t == tagThumbnailLength)
                {
                    return (int)vo;
                }
            }

            return 0;
        }
    }
}
