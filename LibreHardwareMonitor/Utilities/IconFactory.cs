// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Utilities;

public static class IconFactory
{
    private struct BITMAPINFOHEADER
    {
        public readonly uint Size;
        public readonly int Width;
        public readonly int Height;
        public readonly ushort Planes;
        public readonly ushort BitCount;
        public readonly uint Compression;
        public readonly uint SizeImage;
        public readonly int XPelsPerMeter;
        public readonly int YPelsPerMeter;
        public readonly uint ClrUsed;
        public readonly uint ClrImportant;

        public BITMAPINFOHEADER(int width, int height, int bitCount)
        {
            Size = 40;
            Width = width;
            Height = height;
            Planes = 1;
            BitCount = (ushort)bitCount;
            Compression = 0;
            SizeImage = 0;
            XPelsPerMeter = 0;
            YPelsPerMeter = 0;
            ClrUsed = 0;
            ClrImportant = 0;
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
        public readonly byte[] Colors;
        public readonly int MaskSize;

        public ICONIMAGE(int width, int height, byte[] colors)
        {
            Header = new BITMAPINFOHEADER(width, height << 1, (8 * colors.Length) / (width * height));
            Colors = colors;
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
        public readonly byte Width;
        public readonly byte Height;
        public readonly byte ColorCount;
        public readonly byte Reserved;
        public readonly ushort Planes;
        public readonly ushort BitCount;
        public readonly uint BytesInRes;
        public uint ImageOffset;

        public ICONDIRENTRY(ICONIMAGE image, int imageOffset)
        {
            Width = (byte)image.Header.Width;
            Height = (byte)(image.Header.Height >> 1);
            ColorCount = 0;
            Reserved = 0;
            Planes = image.Header.Planes;
            BitCount = image.Header.BitCount;
            BytesInRes = (uint)(image.Header.Size + image.Colors.Length + image.MaskSize + image.MaskSize);
            ImageOffset = (uint)imageOffset;
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
        public readonly ushort Reserved;
        public readonly ushort Type;
        public readonly ushort Count;
        public readonly ICONDIRENTRY[] Entries;

        public ICONDIR(ICONDIRENTRY[] entries)
        {
            Reserved = 0;
            Type = 1;
            Count = (ushort)entries.Length;
            Entries = entries;
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

    private static readonly BinaryWriter BinaryWriter = new BinaryWriter(new MemoryStream());

    public static Icon Create(byte[] colors, int width, int height, PixelFormat format)
    {
        if (format != PixelFormat.Format32bppArgb)
            throw new NotImplementedException();

        ICONIMAGE image = new ICONIMAGE(width, height, colors);
        ICONDIR dir = new ICONDIR(new[] { new ICONDIRENTRY(image, 0) });
        dir.Entries[0].ImageOffset = dir.Size;

        BinaryWriter.BaseStream.Position = 0;
        dir.Write(BinaryWriter);
        image.Write(BinaryWriter);

        BinaryWriter.BaseStream.Position = 0;
        Icon icon = new Icon(BinaryWriter.BaseStream);

        return icon;
    }

    [DllImport("user32", SetLastError = true)]
    static extern bool DestroyIcon(IntPtr handle);

    public static Icon Create(Bitmap bitmap)
    {
        IntPtr hIcon = bitmap.GetHicon();
        Icon icon = Icon.FromHandle(hIcon);

        return icon;
    }

    public static void Destroy(this Icon icon)
    {
        DestroyIcon(icon.Handle);
        icon.Dispose();
    }
}