#nullable enable
using System;
using System.IO;

namespace QuickMediaIngest.Core
{
    internal static class ExifBinaryReaders
    {
        public static ushort ReadUInt16(BinaryReader br, bool littleEndian)
        {
            var data = br.ReadBytes(2);
            if (data.Length < 2)
            {
                return 0;
            }

            if (BitConverter.IsLittleEndian == littleEndian)
            {
                return BitConverter.ToUInt16(data, 0);
            }

            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public static ushort ReadUInt16BE(BinaryReader br)
        {
            var data = br.ReadBytes(2);
            if (data.Length < 2)
            {
                return 0;
            }

            if (!BitConverter.IsLittleEndian)
            {
                return BitConverter.ToUInt16(data, 0);
            }

            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public static uint ReadUInt32(BinaryReader br, bool littleEndian)
        {
            var data = br.ReadBytes(4);
            if (data.Length < 4)
            {
                return 0u;
            }

            if (BitConverter.IsLittleEndian == littleEndian)
            {
                return BitConverter.ToUInt32(data, 0);
            }

            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }
    }
}
