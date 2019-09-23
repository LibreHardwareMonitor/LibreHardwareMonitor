// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OxyImage.cs" company="OxyPlot">
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
// <summary>
//   Represents an image, encoded as DIB, JPEG or PNG.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Represents an image, encoded as DIB, JPEG or PNG.
    /// </summary>
    public class OxyImage
    {
        /// <summary>
        /// The image data.
        /// </summary>
        private readonly byte[] data;

        /// <summary>
        /// Initializes a new instance of the <see cref="OxyImage"/> class from the specified stream.
        /// </summary>
        /// <param name="s">
        /// A stream that provides the image data.
        /// </param>
        public OxyImage(Stream s)
        {
            using (var ms = new MemoryStream())
            {
                s.CopyTo(ms);
                this.data = ms.ToArray();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OxyImage"/> class from a byte array.
        /// </summary>
        /// <param name="bytes">
        /// The image bytes.
        /// </param>
        public OxyImage(byte[] bytes)
        {
            this.data = bytes;
        }

        /// <summary>
        /// Creates an <see cref="OxyImage"/> from the specified <see cref="OxyColor"/> array.
        /// </summary>
        /// <param name="data">
        /// The pixel data, indexed as [row,column] (from bottom-left).
        /// </param>
        /// <param name="dpi">
        /// The resolution.
        /// </param>
        /// <returns>
        /// An <see cref="OxyImage"/>.
        /// </returns>
        /// <remarks>
        /// This method is creating a simple BitmapInfoHeader.
        /// </remarks>
        public static OxyImage FromArgbX(OxyColor[,] data, int dpi = 96)
        {
            int height = data.GetLength(0);
            int width = data.GetLength(1);
            var bytes = new byte[width * height * 4];
            int k = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    bytes[k++] = data[i, j].B;
                    bytes[k++] = data[i, j].G;
                    bytes[k++] = data[i, j].R;
                    bytes[k++] = data[i, j].A;
                }
            }

            return FromArgbX(width, height, bytes, dpi);
        }

        /// <summary>
        /// Creates an <see cref="OxyImage"/> from the specified <see cref="OxyColor"/> array.
        /// </summary>
        /// <param name="data">
        /// The pixel data, indexed as [row,column] (from bottom-left).
        /// </param>
        /// <param name="dpi">
        /// The resolution.
        /// </param>
        /// <returns>
        /// An <see cref="OxyImage"/>.
        /// </returns>
        /// <remarks>
        /// This method is creating a Bitmap V4 info header, including channel bit masks and color space information.
        /// </remarks>
        public static OxyImage FromArgb(OxyColor[,] data, int dpi = 96)
        {
            int height = data.GetLength(0);
            int width = data.GetLength(1);
            var bytes = new byte[width * height * 4];
            int k = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    bytes[k++] = data[i, j].B;
                    bytes[k++] = data[i, j].G;
                    bytes[k++] = data[i, j].R;
                    bytes[k++] = data[i, j].A;
                }
            }

            return FromArgb(width, height, bytes, dpi);
        }

        /// <summary>
        /// Creates an <see cref="OxyImage"/> from the specified pixel data.
        /// </summary>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="pixelData">
        /// The pixel data (BGRA from bottom-left).
        /// </param>
        /// <param name="dpi">
        /// The resolution.
        /// </param>
        /// <returns>
        /// An <see cref="OxyImage"/>.
        /// </returns>
        /// <remarks>
        /// This method is creating a Bitmap V4 info header, including channel bit masks and color space information.
        /// </remarks>
        public static OxyImage FromArgb(int width, int height, byte[] pixelData, int dpi = 96)
        {
            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);

            const int OffBits = 14 + 108;
            var size = OffBits + pixelData.Length;

            // Bitmap file header (14 bytes)
            w.Write((byte)'B');
            w.Write((byte)'M');
            w.Write((uint)size);
            w.Write((ushort)0);
            w.Write((ushort)0);
            w.Write((uint)OffBits);

            // Bitmap V4 info header (108 bytes)
            WriteBitmapV4Header(w, width, height, 32, pixelData.Length, dpi);

            // Pixel array (from bottom-left corner)
            w.Write(pixelData);

            return new OxyImage(ms.ToArray());
        }

        /// <summary>
        /// Creates an <see cref="OxyImage"/> from the specified 8-bit indexed pixel data.
        /// </summary>
        /// <param name="indexedData">
        /// The indexed pixel data (from bottom-left).
        /// </param>
        /// <param name="palette">
        /// The palette.
        /// </param>
        /// <param name="dpi">
        /// The resolution.
        /// </param>
        /// <returns>
        /// An <see cref="OxyImage"/>.
        /// </returns>
        public static OxyImage FromIndexed8(byte[,] indexedData, OxyColor[] palette, int dpi = 96)
        {
            int height = indexedData.GetLength(0);
            int width = indexedData.GetLength(1);
            return FromIndexed8(width, height, indexedData.Cast<byte>().ToArray(), palette, dpi);
        }

        /// <summary>
        /// Creates an <see cref="OxyImage"/> from the specified 8-bit indexed pixel data.
        /// </summary>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="indexedPixelData">
        /// The indexed pixel data (from bottom-left).
        /// </param>
        /// <param name="palette">
        /// The palette.
        /// </param>
        /// <param name="dpi">
        /// The resolution.
        /// </param>
        /// <returns>
        /// An <see cref="OxyImage"/>.
        /// </returns>
        public static OxyImage FromIndexed8(
            int width, int height, byte[] indexedPixelData, OxyColor[] palette, int dpi = 96)
        {
            if (indexedPixelData.Length != width * height)
            {
                throw new ArgumentException("Length of data is not correct.", "indexedPixelData");
            }

            if (palette.Length == 0)
            {
                throw new ArgumentException("Palette not defined.", "palette");
            }

            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);

            var offBits = 14 + 40 + (4 * palette.Length);
            var size = offBits + indexedPixelData.Length;

            // Bitmap file header (14 bytes)
            w.Write((byte)'B');
            w.Write((byte)'M');
            w.Write((uint)size);
            w.Write((ushort)0);
            w.Write((ushort)0);
            w.Write((uint)offBits);

            // Bitmap info header
            WriteBitmapInfoHeader(w, width, height, 8, indexedPixelData.Length, dpi, palette.Length);

            // Color table
            foreach (var color in palette)
            {
                w.Write(color.B);
                w.Write(color.G);
                w.Write(color.R);
                w.Write(color.A);
            }

            // Pixel array (from bottom-left corner)
            w.Write(indexedPixelData);

            return new OxyImage(ms.ToArray());
        }

        /// <summary>
        /// Creates an <see cref="OxyImage"/> from the specified pixel data.
        /// </summary>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="pixelData">
        /// The pixel data (BGRA from bottom-left).
        /// </param>
        /// <param name="dpi">
        /// The resolution.
        /// </param>
        /// <returns>
        /// An <see cref="OxyImage"/>.
        /// </returns>
        /// <remarks>
        /// This method is creating a simple BitmapInfoHeader.
        /// </remarks>
        public static OxyImage FromArgbX(int width, int height, byte[] pixelData, int dpi = 96)
        {
            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);

            const int OffBits = 14 + 40;
            var size = OffBits + pixelData.Length;

            // Bitmap file header (14 bytes)
            w.Write((byte)'B');
            w.Write((byte)'M');
            w.Write((uint)size);
            w.Write((ushort)0);
            w.Write((ushort)0);
            w.Write((uint)OffBits);

            // Bitmap info header
            WriteBitmapInfoHeader(w, width, height, 32, pixelData.Length, dpi);

            // Pixel array (from bottom-left corner)
            w.Write(pixelData);

            return new OxyImage(ms.ToArray());
        }

        /// <summary>
        /// Gets the image data.
        /// </summary>
        /// <returns>
        /// The image data as a byte array.
        /// </returns>
        public byte[] GetData()
        {
            return this.data;
        }

        /// <summary>
        /// Writes the bitmap info header.
        /// </summary>
        /// <param name="w">
        /// The writer.
        /// </param>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="bitsPerPixel">
        /// The number of bits per pixel.
        /// </param>
        /// <param name="length">
        /// The length of the pixel data.
        /// </param>
        /// <param name="dpi">
        /// The dpi.
        /// </param>
        /// <param name="colors">
        /// The number of colors.
        /// </param>
        private static void WriteBitmapInfoHeader(
            BinaryWriter w, int width, int height, int bitsPerPixel, int length, int dpi, int colors = 0)
        {
            // Convert resolution to pixels per meter
            var ppm = (uint)(dpi / 0.0254);

            w.Write((uint)40);
            w.Write((uint)width);
            w.Write((uint)height);
            w.Write((ushort)1);
            w.Write((ushort)bitsPerPixel);
            w.Write((uint)0);
            w.Write((uint)length);
            w.Write(ppm);
            w.Write(ppm);
            w.Write((uint)colors);
            w.Write((uint)colors);
        }

        /// <summary>
        /// Writes the bitmap V4 header.
        /// </summary>
        /// <param name="w">
        /// The writer.
        /// </param>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="bitsPerPixel">
        /// The number of bits per pixel.
        /// </param>
        /// <param name="length">
        /// The length.
        /// </param>
        /// <param name="dpi">
        /// The resolution.
        /// </param>
        /// <param name="colors">
        /// The number of colors.
        /// </param>
        private static void WriteBitmapV4Header(
            BinaryWriter w, int width, int height, int bitsPerPixel, int length, int dpi, int colors = 0)
        {
            // Convert resolution to pixels per meter
            var ppm = (uint)(dpi / 0.0254);

            w.Write((uint)108);
            w.Write((uint)width);
            w.Write((uint)height);
            w.Write((ushort)1);
            w.Write((ushort)bitsPerPixel);
            w.Write((uint)3);
            w.Write((uint)length);
            w.Write(ppm);
            w.Write(ppm);
            w.Write((uint)colors);
            w.Write((uint)colors);

            // Write the channel bit masks
            w.Write(0x00FF0000);
            w.Write(0x0000FF00);
            w.Write(0x000000FF);
            w.Write(0xFF000000);

            // Write the color space
            w.Write((uint)0x206E6957);
            w.Write(new byte[3 * 3 * 4]);

            // Write the gamma RGB
            w.Write((uint)0);
            w.Write((uint)0);
            w.Write((uint)0);
        }

        /// <summary>
        /// Creates a PNG image from the specified pixels.
        /// </summary>
        /// <param name="pixels">The pixels (bottom line first).</param>
        /// <returns>An OxyImage.</returns>
        public static OxyImage PngFromArgb(OxyColor[,] pixels)
        {
            return new OxyImage(PngEncoder.Encode(pixels));
        }
    }
}