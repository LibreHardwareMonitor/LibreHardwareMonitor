// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PngEncoder.cs" company="OxyPlot">
//   The MIT License (MIT)
//   
//   Copyright (c) 2012 Oystein Bjorke
//   
//   Permission is hereby granted, free of charge, to any person obtaining a
//   copy of this software and associated documentation files (the
//   "Software"), to deal in the Software without restriction, including
//   without limitation the rights to use, copy, modify, merge, publish,
//   distribute, sublicense, and/or sell copies of the Software, and to
//   permit persons to whom the Software is furnished to do so, subject to
//   the following conditions:
//   
//   The above copyright notice and this permission notice shall be included
//   in all copies or substantial portions of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//   CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//   TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Provides encoding of uncompressed png images.
    /// </summary>
    public class PngEncoder
    {
        /// <summary>
        /// The CRC table
        /// </summary>
        private static readonly ulong[] CrcTable;

        /// <summary>
        /// Initializes static members of the <see cref="PngEncoder" /> class.
        /// </summary>
        static PngEncoder()
        {
            CrcTable = new ulong[256];
            for (int n = 0; n < 256; n++)
            {
                var c = (ulong)n;
                for (int k = 0; k < 8; k++)
                {
                    if ((c & 1) != 0)
                    {
                        c = 0xedb88320L ^ (c >> 1);
                    }
                    else
                    {
                        c = c >> 1;
                    }
                }

                CrcTable[n] = c;
            }
        }

        /// <summary>
        /// Encodes the specified image data to png.
        /// </summary>
        /// <param name="pixels">
        /// The pixel data (bottom line first).
        /// </param>
        /// <param name="dpi">
        /// The image resolution in dots per inch.
        /// </param>
        /// <returns>
        /// The png image data.
        /// </returns>
        public static byte[] Encode(OxyColor[,] pixels, int dpi = 96)
        {
            int height = pixels.GetLength(0);
            int width = pixels.GetLength(1);
            var bytes = new byte[(width * height * 4) + height];

            int k = 0;
            for (int i = height - 1; i >= 0; i--)
            {
                bytes[k++] = 0; // Filter
                for (int j = 0; j < width; j++)
                {
                    bytes[k++] = pixels[i, j].R;
                    bytes[k++] = pixels[i, j].G;
                    bytes[k++] = pixels[i, j].B;
                    bytes[k++] = pixels[i, j].A;
                }
            }

            var w = new MemoryWriter();
            w.Write((byte)0x89);
            w.Write("PNG\r\n\x1a\n".ToCharArray());
            WriteChunk(w, "IHDR", CreateHeaderData(width, height));
            WriteChunk(w, "pHYs", CreatePhysicalDimensionsData(dpi, dpi));
            WriteChunk(w, "IDAT", CreateUncompressedBlocks(bytes));
            WriteChunk(w, "IEND", new byte[0]);
            return w.ToArray();
        }

        /// <summary>
        /// Calculates the Adler-32 check sum.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <returns>
        /// The check sum.
        /// </returns>
        private static uint Adler32(IEnumerable<byte> data)
        {
            // http://en.wikipedia.org/wiki/Adler-32
            uint a = 1;
            uint b = 0;
            const uint ModAdler = 65521;
            foreach (var x in data)
            {
                a = (a + x) % ModAdler;
                b = (b + a) % ModAdler;
            }

            return (b << 16) | a;
        }

        /// <summary>
        /// Creates the header data.
        /// </summary>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <returns>
        /// The header.
        /// </returns>
        private static byte[] CreateHeaderData(int width, int height)
        {
            // http://www.w3.org/TR/PNG-Chunks.html
            var w = new MemoryWriter();
            WriteBigEndian(w, width);
            WriteBigEndian(w, height);
            w.Write((byte)8); // bit depth
            w.Write((byte)6); // color type RGBA
            w.Write((byte)0); // compression method
            w.Write((byte)0); // filter method
            w.Write((byte)0); // interlace method
            return w.ToArray();
        }

        /// <summary>
        /// Creates the physical dimensions data.
        /// </summary>
        /// <param name="dpix">
        /// The horizontal resolution.
        /// </param>
        /// <param name="dpiy">
        /// The vertical resolution.
        /// </param>
        /// <returns>
        /// The data.
        /// </returns>
        private static byte[] CreatePhysicalDimensionsData(int dpix, int dpiy)
        {
            var ppux = (int)(dpix / 0.0254);
            var ppuy = (int)(dpiy / 0.0254);
            var w = new MemoryWriter();
            WriteBigEndian(w, ppux);
            WriteBigEndian(w, ppuy);
            w.Write((byte)1); // Unit: metre
            return w.ToArray();
        }

        /// <summary>
        /// Creates the uncompressed blocks.
        /// </summary>
        /// <param name="bytes">
        /// The data.
        /// </param>
        /// <returns>
        /// The output data.
        /// </returns>
        private static byte[] CreateUncompressedBlocks(byte[] bytes)
        {
            // http://www.w3.org/TR/PNG-Compression.html
            const int MaxDeflate = 0xFFFF;
            var w = new MemoryWriter();
            const uint CompressionMethod = 8;
            const uint Check = (31 - ((CompressionMethod << 8) % 31)) % 31;
            w.Write((byte)CompressionMethod);
            w.Write((byte)Check);
            for (int i = 0; i < bytes.Length; i += MaxDeflate)
            {
                var n = (ushort)Math.Min(bytes.Length - i, MaxDeflate);
                var last = (byte)(i + n < bytes.Length ? 0 : 1);
                w.Write(last);
                w.Write((byte)(n & 0xFF));
                w.Write((byte)((n >> 8) & 0xFF));
                var n2 = ~n;
                w.Write((byte)(n2 & 0xFF));
                w.Write((byte)((n2 >> 8) & 0xFF));
                w.Write(bytes, i, n);
            }

            WriteBigEndian(w, Adler32(bytes));
            return w.ToArray();
        }

        /// <summary>
        /// Updates the CRC check sum.
        /// </summary>
        /// <param name="crc">
        /// The input CRC.
        /// </param>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <returns>
        /// The updated CRC.
        /// </returns>
        private static ulong UpdateCrc(ulong crc, IEnumerable<byte> data)
        {
            return data.Aggregate(crc, (current, x) => CrcTable[(current ^ x) & 0xff] ^ (current >> 8));
        }

        /// <summary>
        /// Writes the integer value with big endian byte order.
        /// </summary>
        /// <param name="w">
        /// The writer.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        private static void WriteBigEndian(BinaryWriter w, int value)
        {
            var bytes = BitConverter.GetBytes(value);
            w.Write(bytes[3]);
            w.Write(bytes[2]);
            w.Write(bytes[1]);
            w.Write(bytes[0]);
        }

        /// <summary>
        /// Writes the unsigned integer value with big endian byte order.
        /// </summary>
        /// <param name="w">
        /// The writer.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        private static void WriteBigEndian(BinaryWriter w, uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            w.Write(bytes[3]);
            w.Write(bytes[2]);
            w.Write(bytes[1]);
            w.Write(bytes[0]);
        }

        /// <summary>
        /// Writes a png chunk.
        /// </summary>
        /// <param name="w">
        /// The writer.
        /// </param>
        /// <param name="type">
        /// The chunk type.
        /// </param>
        /// <param name="data">
        /// The chunk data.
        /// </param>
        private static void WriteChunk(BinaryWriter w, string type, byte[] data)
        {
            var ty = type.ToCharArray().Select(ch => (byte)ch).ToArray();
            WriteBigEndian(w, data.Length);
            w.Write(ty);
            w.Write(data);

            var c = 0xffffffff;
            c = (uint)UpdateCrc(c, ty);
            c = (uint)UpdateCrc(c, data);
            var crc = c ^ 0xffffffff;

            WriteBigEndian(w, crc);
        }

        /// <summary>
        /// Provides a binary writer that writes to memory.
        /// </summary>
        private class MemoryWriter : BinaryWriter
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MemoryWriter" /> class.
            /// </summary>
            public MemoryWriter()
                : base(new MemoryStream())
            {
            }

            /// <summary>
            /// Gets the content as a byte array.
            /// </summary>
            /// <returns>The byte array.</returns>
            public byte[] ToArray()
            {
                this.BaseStream.Flush();
                return ((MemoryStream)this.BaseStream).ToArray();
            }
        }
    }
}