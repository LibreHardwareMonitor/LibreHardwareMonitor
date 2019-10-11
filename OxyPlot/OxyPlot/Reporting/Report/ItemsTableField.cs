// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemsTableField.cs" company="OxyPlot">
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
//   The alignment.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    using System;
    using System.Reflection;

    /// <summary>
    /// The alignment.
    /// </summary>
    public enum Alignment
    {
        /// <summary>
        /// The left.
        /// </summary>
        Left,

        /// <summary>
        /// The right.
        /// </summary>
        Right,

        /// <summary>
        /// The center.
        /// </summary>
        Center
    }

    /// <summary>
    /// Represents a field in an items table.
    /// </summary>
    public class ItemsTableField
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsTableField"/> class.
        /// </summary>
        /// <param name="header">
        /// The header.
        /// </param>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="stringFormat">
        /// The string format.
        /// </param>
        /// <param name="alignment">
        /// The alignment.
        /// </param>
        public ItemsTableField(
            string header, string path, string stringFormat = null, Alignment alignment = Alignment.Center)
        {
            this.Header = header;
            this.Path = path;
            this.StringFormat = stringFormat;
            this.Alignment = alignment;
        }

        /// <summary>
        /// Gets or sets Alignment.
        /// </summary>
        public Alignment Alignment { get; set; }

        /// <summary>
        /// Gets or sets Header.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Gets or sets Path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets StringFormat.
        /// </summary>
        public string StringFormat { get; set; }

        /// <summary>
        /// Gets or sets Width.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="formatProvider">
        /// The format provider.
        /// </param>
        /// <returns>
        /// The text.
        /// </returns>
        public string GetText(object item, IFormatProvider formatProvider)
        {
            PropertyInfo pi = item.GetType().GetProperty(this.Path);
            object o = pi.GetValue(item, null);
            var of = o as IFormattable;
            if (of != null)
            {
                return of.ToString(this.StringFormat, formatProvider);
            }

            return o != null ? o.ToString() : null;
        }

    }
}