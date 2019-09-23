// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Table.cs" company="OxyPlot">
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
//   Represents a table column definition.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a table column definition.
    /// </summary>
    public class TableColumn
    {
        /// <summary>
        /// Initializes a new instance of the <see cref = "TableColumn" /> class.
        /// </summary>
        public TableColumn()
        {
            this.Width = double.NaN;
            this.Alignment = Alignment.Center;
        }

        /// <summary>
        /// Gets or sets the actual width (mm).
        /// </summary>
        /// <value>The actual width.</value>
        public double ActualWidth { get; internal set; }

        /// <summary>
        /// Gets or sets Alignment.
        /// </summary>
        public Alignment Alignment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsHeader.
        /// </summary>
        public bool IsHeader { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// NaN: auto width.
        /// Negative numbers: weights
        /// </summary>
        /// <value>The width.</value>
        public double Width { get; set; }

    }

    /// <summary>
    /// Represents a table row definition.
    /// </summary>
    public class TableRow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref = "TableRow" /> class.
        /// </summary>
        public TableRow()
        {
            this.Cells = new List<TableCell>();
        }

        /// <summary>
        /// Gets Cells.
        /// </summary>
        public IList<TableCell> Cells { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsHeader.
        /// </summary>
        public bool IsHeader { get; set; }

    }

    /// <summary>
    /// Represents a table cell.
    /// </summary>
    public class TableCell
    {
        // public Alignment Alignment { get; set; }
        // public int RowSpan { get; set; }
        // public int ColumnSpan { get; set; }
        /// <summary>
        /// Gets or sets Content.
        /// </summary>
        public string Content { get; set; }

    }

    /// <summary>
    /// Represents a table.
    /// </summary>
    public class Table : ReportItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref = "Table" /> class.
        /// </summary>
        public Table()
        {
            this.Rows = new List<TableRow>();
            this.Columns = new List<TableColumn>();
            this.Width = double.NaN;
        }

        /// <summary>
        /// Gets or sets the actual width of the table (mm).
        /// </summary>
        /// <value>The actual width.</value>
        public double ActualWidth { get; private set; }

        /// <summary>
        /// Gets or sets Caption.
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Gets Columns.
        /// </summary>
        public IList<TableColumn> Columns { get; private set; }

        /// <summary>
        /// Gets Rows.
        /// </summary>
        public IList<TableRow> Rows { get; private set; }

        /// <summary>
        /// Gets or sets TableNumber.
        /// </summary>
        public int TableNumber { get; set; }

        /// <summary>
        /// Gets or sets the width of the table (mm).
        /// NaN: auto width.
        /// 0..-1: fraction of page width.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// The get full caption.
        /// </summary>
        /// <param name="style">
        /// The style.
        /// </param>
        /// <returns>
        /// The get full caption.
        /// </returns>
        public string GetFullCaption(ReportStyle style)
        {
            return string.Format(style.TableCaptionFormatString, this.TableNumber, this.Caption);
        }

        /// <summary>
        /// The update.
        /// </summary>
        public override void Update()
        {
            base.Update();
            this.UpdateWidths();
        }

        /// <summary>
        /// The write content.
        /// </summary>
        /// <param name="w">
        /// The w.
        /// </param>
        public override void WriteContent(IReportWriter w)
        {
            // todo
        }

        /// <summary>
        /// The update widths.
        /// </summary>
        private void UpdateWidths()
        {
            if (this.Width < 0)
            {
                this.ActualWidth = 150 * (-this.Width);
            }
            else
            {
                this.ActualWidth = this.Width;
            }

            // update actual widths of all columns
            double totalWeight = 0;
            double totalWidth = 0;
            foreach (var c in this.Columns)
            {
                if (double.IsNaN(c.Width))
                {
                    // todo: find auto width
                    c.ActualWidth = 40;
                    totalWidth += c.ActualWidth;
                }

                if (c.Width < 0)
                {
                    totalWeight += -c.Width;
                }

                if (c.Width >= 0)
                {
                    totalWidth += c.Width;
                    c.ActualWidth = c.Width;
                }
            }

            if (double.IsNaN(this.ActualWidth))
            {
                this.ActualWidth = Math.Max(150, totalWidth + 100);
            }

            double w = this.ActualWidth - totalWidth;
            foreach (var c in this.Columns)
            {
                if (c.Width < 0 && totalWeight != 0)
                {
                    double weight = -c.Width;
                    c.ActualWidth = w * (weight / totalWeight);
                }
            }
        }

    }
}