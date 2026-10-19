using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AlibreAddOnManager
{
    /// <summary>Just enough PE parsing to check machine, CLR flags and native exports.</summary>
    public static class PeInspector
    {
        public static PeInfo Inspect(string path)
        {
            return Inspect(File.ReadAllBytes(path));
        }

        public static PeInfo Inspect(byte[] image)
        {
            if (image.Length < 0x40 || image[0] != 'M' || image[1] != 'Z')
                throw new InvalidDataException("Not a PE file (missing MZ header).");

            var peOffset = ReadInt32(image, 0x3C);
            if (peOffset <= 0 || peOffset + 24 > image.Length || ReadUInt32(image, peOffset) != 0x00004550) // "PE\0\0"
                throw new InvalidDataException("Not a PE file (missing PE signature).");

            var coff = peOffset + 4;
            var info = new PeInfo { Machine = ReadUInt16(image, coff) };
            var sectionCount = ReadUInt16(image, coff + 2);
            var optionalSize = ReadUInt16(image, coff + 16);
            var optional = coff + 20;
            if (optional + optionalSize > image.Length)
                throw new InvalidDataException("Truncated PE optional header.");

            var magic = ReadUInt16(image, optional);
            info.IsPe32Plus = magic == 0x20B;
            if (magic != 0x10B && magic != 0x20B)
                throw new InvalidDataException("Unknown PE optional header magic.");

            var rvaCountOffset = optional + (info.IsPe32Plus ? 108 : 92);
            var directories = optional + (info.IsPe32Plus ? 112 : 96);
            var directoryCount = ReadInt32(image, rvaCountOffset);

            var sections = optional + optionalSize;

            // Data directory 14: CLI (COR20) header.
            if (directoryCount > 14)
            {
                var clrRva = ReadUInt32(image, directories + 14 * 8);
                if (clrRva != 0)
                {
                    var clr = RvaToOffset(image, sections, sectionCount, clrRva);
                    if (clr >= 0 && clr + 20 <= image.Length)
                    {
                        info.HasClrHeader = true;
                        info.CorFlags = ReadUInt32(image, clr + 16);
                    }
                }
            }

            // Data directory 0: export table (native entry points).
            if (directoryCount > 0 && !info.HasClrHeader)
            {
                var exportRva = ReadUInt32(image, directories);
                if (exportRva != 0)
                    ReadExports(image, sections, sectionCount, exportRva, info.ExportNames);
            }

            return info;
        }

        private static void ReadExports(byte[] image, int sections, int sectionCount, uint exportRva, List<string> names)
        {
            var export = RvaToOffset(image, sections, sectionCount, exportRva);
            if (export < 0 || export + 40 > image.Length) return;

            var nameCount = ReadInt32(image, export + 24);
            var namesRva = ReadUInt32(image, export + 32);
            var namesOffset = RvaToOffset(image, sections, sectionCount, namesRva);
            if (namesOffset < 0 || nameCount < 0 || nameCount > 65536) return;

            for (var i = 0; i < nameCount; i++)
            {
                var entry = namesOffset + i * 4;
                if (entry + 4 > image.Length) break;
                var nameOffset = RvaToOffset(image, sections, sectionCount, ReadUInt32(image, entry));
                if (nameOffset < 0) continue;
                var end = nameOffset;
                while (end < image.Length && image[end] != 0) end++;
                names.Add(Encoding.ASCII.GetString(image, nameOffset, end - nameOffset));
            }
        }

        private static int RvaToOffset(byte[] image, int sections, int sectionCount, uint rva)
        {
            for (var i = 0; i < sectionCount; i++)
            {
                var header = sections + i * 40;
                if (header + 40 > image.Length) break;
                var virtualSize = ReadUInt32(image, header + 8);
                var virtualAddress = ReadUInt32(image, header + 12);
                var rawSize = ReadUInt32(image, header + 16);
                var rawPointer = ReadUInt32(image, header + 20);
                var size = Math.Max(virtualSize, rawSize);
                if (rva >= virtualAddress && rva < virtualAddress + size)
                {
                    var offset = rva - virtualAddress + rawPointer;
                    return offset < image.Length ? (int) offset : -1;
                }
            }

            return -1;
        }

        private static ushort ReadUInt16(byte[] b, int o) => o + 2 <= b.Length ? BitConverter.ToUInt16(b, o) : (ushort) 0;
        private static uint ReadUInt32(byte[] b, int o) => o + 4 <= b.Length ? BitConverter.ToUInt32(b, o) : 0u;
        private static int ReadInt32(byte[] b, int o) => o + 4 <= b.Length ? BitConverter.ToInt32(b, o) : 0;
    }
}
