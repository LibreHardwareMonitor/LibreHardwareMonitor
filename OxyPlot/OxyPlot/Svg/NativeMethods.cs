// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="OxyPlot">
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
//   Interface to GDI32 native methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides access to native graphics methods.
    /// </summary>
    public class NativeMethods
    {
        /// <summary>
        /// The delete dc.
        /// </summary>
        /// <param name="hdc">
        /// The hdc.
        /// </param>
        /// <returns>
        /// The delete dc.
        /// </returns>
        [DllImport("gdi32.dll")]
        internal static extern bool DeleteDC(IntPtr hdc);

        /// <summary>
        /// The delete object.
        /// </summary>
        /// <param name="hgdiobj">
        /// The hgdiobj.
        /// </param>
        /// <returns>
        /// The delete object.
        /// </returns>
        [DllImport("gdi32.dll")]
        internal static extern int DeleteObject(IntPtr hgdiobj);

        /// <summary>
        /// The get dc.
        /// </summary>
        /// <param name="hWnd">
        /// The h wnd.
        /// </param>
        /// <returns>
        /// </returns>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>
        /// The get text extent point 32.
        /// </summary>
        /// <param name="hdc">
        /// The hdc.
        /// </param>
        /// <param name="str">
        /// The str.
        /// </param>
        /// <param name="len">
        /// The len.
        /// </param>
        /// <param name="siz">
        /// The siz.
        /// </param>
        /// <returns>
        /// The get text extent point 32.
        /// </returns>
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetTextExtentPoint32(IntPtr hdc, string str, int len, ref Size siz);

        /// <summary>
        /// The measure string.
        /// </summary>
        /// <param name="faceName">
        /// The font face name.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="weight">
        /// The weight.
        /// </param>
        /// <param name="str">
        /// The string.
        /// </param>
        /// <returns>
        /// The size of the rendered string.
        /// </returns>
        public static OxySize MeasureString(string faceName, int height, int weight, string str)
        {
            var lines = Regex.Split(str, "\r\n");
            OxySize result = new OxySize(0, 0);
            foreach (var line in lines)
            {
                var hfont = CreateFont(height, 0, 0, 0, weight, 0, 0, 0, 0, 0, 0, 0, 0, faceName);
                var hdc = GetDC(IntPtr.Zero);
                var oldobj = SelectObject(hdc, hfont);
                var temp = GetTextExtent(hdc, line);
                SelectObject(hdc, oldobj);
                DeleteObject(hfont);
                DeleteDC(hdc);
                var lineSpacing = temp.Height / 3.0;
                result.Height += temp.Height + lineSpacing;
                result.Width = Math.Max(temp.Width * 1.28, result.Width);
            }

            return result;
        }

        /// <summary>
        /// The select object.
        /// </summary>
        /// <param name="hdc">
        /// The hdc.
        /// </param>
        /// <param name="hgdiObj">
        /// The hgdi obj.
        /// </param>
        /// <returns>
        /// </returns>
        [DllImport("gdi32.dll")]
        internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiObj);

        /// <summary>
        /// The create font.
        /// </summary>
        /// <param name="nHeight">
        /// The n height.
        /// </param>
        /// <param name="nWidth">
        /// The n width.
        /// </param>
        /// <param name="nEscapement">
        /// The n escapement.
        /// </param>
        /// <param name="nOrientation">
        /// The n orientation.
        /// </param>
        /// <param name="fnWeight">
        /// The fn weight.
        /// </param>
        /// <param name="fdwItalic">
        /// The fdw italic.
        /// </param>
        /// <param name="fdwUnderline">
        /// The fdw underline.
        /// </param>
        /// <param name="fdwStrikeOut">
        /// The fdw strike out.
        /// </param>
        /// <param name="fdwCharSet">
        /// The fdw char set.
        /// </param>
        /// <param name="fdwOutputPrecision">
        /// The fdw output precision.
        /// </param>
        /// <param name="fdwClipPrecision">
        /// The fdw clip precision.
        /// </param>
        /// <param name="fdwQuality">
        /// The fdw quality.
        /// </param>
        /// <param name="fdwPitchAndFamily">
        /// The fdw pitch and family.
        /// </param>
        /// <param name="lpszFace">
        /// The lpsz face.
        /// </param>
        /// <returns>
        /// </returns>
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFont(
            int nHeight,
            int nWidth,
            int nEscapement,
            int nOrientation,
            int fnWeight,
            uint fdwItalic,
            uint fdwUnderline,
            uint fdwStrikeOut,
            uint fdwCharSet,
            uint fdwOutputPrecision,
            uint fdwClipPrecision,
            uint fdwQuality,
            uint fdwPitchAndFamily,
            string lpszFace);

        /// <summary>
        /// Gets the text extent.
        /// </summary>
        /// <param name="hdc">The HDC.</param>
        /// <param name="str">The STR.</param>
        /// <returns></returns>
        private static OxySize GetTextExtent(IntPtr hdc, string str)
        {
            Size sz = default(Size);
            sz.cx = 0;
            sz.cy = 0;
            GetTextExtentPoint32(hdc, str, str.Length, ref sz);
            return new OxySize(sz.cx, sz.cy);
        }

        /// <summary>
        /// The size.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Size
        {
            /// <summary>
            /// The cx.
            /// </summary>
            public int cx;

            /// <summary>
            /// The cy.
            /// </summary>
            public int cy;
        }
    }
}