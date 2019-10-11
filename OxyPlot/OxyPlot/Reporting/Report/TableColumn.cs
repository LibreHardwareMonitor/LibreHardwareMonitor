// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TableColumn.cs" company="OxyPlot">
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
using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace OxyPlot.Reporting
{
    public enum Alignment { Left, Right, Center };

    public class TableColumn
    {
        public Alignment Alignment { get; set; }
        public string Header { get; set; }
        public string StringFormat { get; set; }
        public string Path { get; set; }
        public double Width { get; set; }
        // public Collection<TableColumn> SubColumns { get; set; }

        public TableColumn(string header, string path, string stringFormat=null, Alignment alignment=Alignment.Center)
        {
            Header = header;
            Path = path;
            StringFormat = stringFormat;
            Alignment = alignment;
            // SubColumns = new Collection<TableColumn>();
        }

        public string GetText(object item)
        {
            var pi = item.GetType().GetProperty(Path);
            object o = pi.GetValue(item, null);
            var of = o as IFormattable;
            if (of != null)
                return of.ToString(StringFormat, CultureInfo.InvariantCulture);
            return o!=null ? o.ToString():null;
        }
    }
}