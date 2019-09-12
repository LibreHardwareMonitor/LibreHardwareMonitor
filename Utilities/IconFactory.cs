// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace OpenHardwareMonitor.Utilities
{
    public class IconFactory
    {
        private struct BITMAPINFOHEADER
        {
            public uint Size;
            public int Width;
            public int Height;
            public ushort Planes;
            public ushort BitCount;
            public uint Compression;
            public uint SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public uint ClrUsed;
            public uint ClrImportant;

            public BITMAPINFOHEADER(int width, int height, int bitCount)
            {
                this.Size = 40;
                this.Width = width;
                this.Height = height;
                this.Planes = 1;
                this.BitCount = (ushort)bitCount;
                this.Compression = 0;
                this.SizeImage = 0;
                this.XPelsPerMeter = 0;
                this.YPelsPerMeter = 0;
                this.ClrUsed = 0;
                this.ClrImportant = 0;
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(Size);
                bw.Write(Width);
                bw.Write(Height);
                bw.Write(Planes);
                bw.Write(BitCount);
                bw.Write(Compression);
                bw.Write(SizeImage);
                bw.Write(XPelsPerMeter);
                bw.Write(YPelsPerMeter);
                bw.Write(ClrUsed);
                bw.Write(ClrImportant);
            }
        }

        private struct ICONIMAGE
        {
            public BITMAPINFOHEADER Header;
            public byte[] Colors;
            public int MaskSize;

            public ICONIMAGE(int width, int height, byte[] colors)
            {
                this.Header = new BITMAPINFOHEADER(width, height << 1,
                  (8 * colors.Length) / (width * height));
                this.Colors = colors;
                MaskSize = (width * height) >> 3;
            }

            public void Write(BinaryWriter bw)
            {
                Header.Write(bw);
                int stride = Header.Width << 2;
                for (int i = (Header.Height >> 1) - 1; i >= 0; i--)
                    bw.Write(Colors, i * stride, stride);
                for (int i = 0; i < 2 * MaskSize; i++)
                    bw.Write((byte)0);
            }
        }

        private struct ICONDIRENTRY
        {
            public byte Width;
            public byte Height;
            public byte ColorCount;
            public byte Reserved;
            public ushort Planes;
            public ushort BitCount;
            public uint BytesInRes;
            public uint ImageOffset;

            public ICONDIRENTRY(ICONIMAGE image, int imageOffset)
            {
                this.Width = (byte)image.Header.Width;
                this.Height = (byte)(image.Header.Height >> 1);
                this.ColorCount = 0;
                this.Reserved = 0;
                this.Planes = image.Header.Planes;
                this.BitCount = image.Header.BitCount;
                this.BytesInRes = (uint)(image.Header.Size + image.Colors.Length + image.MaskSize + image.MaskSize);
                this.ImageOffset = (uint)imageOffset;
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(Width);
                bw.Write(Height);
                bw.Write(ColorCount);
                bw.Write(Reserved);
                bw.Write(Planes);
                bw.Write(BitCount);
                bw.Write(BytesInRes);
                bw.Write(ImageOffset);
            }

            public uint Size
            {
                get { return 16; }
            }
        }

        private struct ICONDIR
        {
            public ushort Reserved;
            public ushort Type;
            public ushort Count;
            public ICONDIRENTRY[] Entries;

            public ICONDIR(ICONDIRENTRY[] entries)
            {
                this.Reserved = 0;
                this.Type = 1;
                this.Count = (ushort)entries.Length;
                this.Entries = entries;
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(Reserved);
                bw.Write(Type);
                bw.Write(Count);
                for (int i = 0; i < Entries.Length; i++)
                    Entries[i].Write(bw);
            }

            public uint Size
            {
                get
                {
                    return (uint)(6 + Entries.Length * (Entries.Length > 0 ? Entries[0].Size : 0));
                }
            }
        }

        private static BinaryWriter binaryWriter = new BinaryWriter(new MemoryStream());

        public static Icon Create(byte[] colors, int width, int height,
          PixelFormat format)
        {
            if (format != PixelFormat.Format32bppArgb)
                throw new NotImplementedException();

            ICONIMAGE image = new ICONIMAGE(width, height, colors);
            ICONDIR dir = new ICONDIR(new ICONDIRENTRY[] { new ICONDIRENTRY(image, 0) });
            dir.Entries[0].ImageOffset = dir.Size;

            Icon icon;
            binaryWriter.BaseStream.Position = 0;
            dir.Write(binaryWriter);
            image.Write(binaryWriter);

            binaryWriter.BaseStream.Position = 0;
            icon = new Icon(binaryWriter.BaseStream);

            return icon;
        }

    }
}
