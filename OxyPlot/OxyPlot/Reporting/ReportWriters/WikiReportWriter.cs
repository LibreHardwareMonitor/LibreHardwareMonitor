// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WikiReportWriter.cs" company="OxyPlot">
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
//   Wiki formatting report writer.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    using System;
    using System.IO;

    /// <summary>
    /// Wiki formatting report writer.
    /// </summary>
    /// <remarks>
    /// This will not write figures/images.
    /// </remarks>
    public class WikiReportWriter : StreamWriter, IReportWriter
    {
        /// <summary>
        /// The table cell separator.
        /// </summary>
        private const string TableCellSeparator = " | ";

        /// <summary>
        /// The table header cell separator.
        /// </summary>
        private const string TableHeaderCellSeparator = " || ";

        /// <summary>
        /// The table header row end.
        /// </summary>
        private const string TableHeaderRowEnd = " ||";

        /// <summary>
        /// The table header row start.
        /// </summary>
        private const string TableHeaderRowStart = "|| ";

        /// <summary>
        /// The table row end.
        /// </summary>
        private const string TableRowEnd = " |";

        /// <summary>
        /// The table row start.
        /// </summary>
        private const string TableRowStart = "| ";

        /// <summary>
        /// The table counter.
        /// </summary>
        private int tableCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="WikiReportWriter"/> class.
        /// </summary>
        /// <param name="s">
        /// The s.
        /// </param>
        public WikiReportWriter(Stream s)
            : base(s)
        {
            this.MaxLineLength = 60;
        }

        /// <summary>
        /// Gets or sets MaxLineLength.
        /// </summary>
        public int MaxLineLength { get; set; }

        /// <summary>
        /// The write drawing.
        /// </summary>
        /// <param name="d">
        /// The d.
        /// </param>
        public void WriteDrawing(DrawingFigure d)
        {
        }

        /// <summary>
        /// The write equation.
        /// </summary>
        /// <param name="equation">
        /// The equation.
        /// </param>
        public void WriteEquation(Equation equation)
        {
        }

        /// <summary>
        /// The write header.
        /// </summary>
        /// <param name="h">
        /// The h.
        /// </param>
        public void WriteHeader(Header h)
        {
            if (h.Text == null)
            {
                return;
            }

            string prefix = string.Empty;
            for (int i = 0; i < h.Level; i++)
            {
                prefix += "!";
            }

            this.WriteLine(prefix + " " + h.Text);
        }

        /// <summary>
        /// The write image.
        /// </summary>
        /// <param name="i">
        /// The i.
        /// </param>
        public void WriteImage(Image i)
        {
        }

        /// <summary>
        /// The write paragraph.
        /// </summary>
        /// <param name="p">
        /// The p.
        /// </param>
        public void WriteParagraph(Paragraph p)
        {
            foreach (string line in p.Text.SplitLines(this.MaxLineLength))
            {
                WriteLine(line);
            }

            this.WriteLine();
        }

        /// <summary>
        /// The write plot.
        /// </summary>
        /// <param name="plot">
        /// The plot.
        /// </param>
        public void WritePlot(PlotFigure plot)
        {
        }

        /// <summary>
        /// The write report.
        /// </summary>
        /// <param name="report">
        /// The report.
        /// </param>
        /// <param name="reportStyle">
        /// The style.
        /// </param>
        public void WriteReport(Report report, ReportStyle reportStyle)
        {
            report.Write(this);
        }

        /// <summary>
        /// The write table.
        /// </summary>
        /// <param name="t">
        /// The t.
        /// </param>
        public void WriteTable(Table t)
        {
            this.tableCounter++;
            this.WriteLine(string.Format("Table {0}. {1}", this.tableCounter, t.Caption));
            this.WriteLine();
            int rows = t.Rows.Count;
            int cols = t.Columns.Count;

            var columnWidth = new int[cols];
            int totalLength = 0;
            for (int j = 0; j < cols; j++)
            {
                columnWidth[j] = 0;
                foreach (var tr in t.Rows)
                {
                    TableCell cell = tr.Cells[j];
                    string text = cell.Content;
                    columnWidth[j] = Math.Max(columnWidth[j], text != null ? text.Length : 0);
                }

                totalLength += columnWidth[j];
            }

            // WriteLine("-".Repeat(totalLength));
            foreach (var tr in t.Rows)
            {
                for (int j = 0; j < cols; j++)
                {
                    TableCell cell = tr.Cells[j];
                    string text = cell.Content;
                    bool isHeader = tr.IsHeader || t.Columns[j].IsHeader;
                    this.Write(GetCellText(j, cols, PadString(text, t.Columns[j].Alignment, columnWidth[j]), isHeader));
                }

                this.WriteLine();
            }

            this.WriteLine();
        }

        /// <summary>
        /// The get cell text.
        /// </summary>
        /// <param name="i">
        /// The i.
        /// </param>
        /// <param name="count">
        /// The count.
        /// </param>
        /// <param name="p">
        /// The p.
        /// </param>
        /// <param name="isHeader">
        /// The is header.
        /// </param>
        /// <returns>
        /// The get cell text.
        /// </returns>
        private static string GetCellText(int i, int count, string p, bool isHeader)
        {
            if (i == 0)
            {
                p = isHeader ? TableHeaderRowStart : TableRowStart + p;
            }

            if (i + 1 < count)
            {
                p += isHeader ? TableHeaderCellSeparator : TableCellSeparator;
            }

            if (i == count - 1)
            {
                p += isHeader ? TableHeaderRowEnd : TableRowEnd;
            }

            return p;
        }

        /// <summary>
        /// The pad string.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="alignment">
        /// The alignment.
        /// </param>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <returns>
        /// The pad string.
        /// </returns>
        private static string PadString(string text, Alignment alignment, int width)
        {
            if (text == null)
            {
                return string.Empty.PadLeft(width);
            }

            switch (alignment)
            {
                case Alignment.Left:
                    return text.PadRight(width);
                case Alignment.Right:
                    return text.PadLeft(width);
                case Alignment.Center:
                    text = text.PadRight((text.Length + width) / 2);
                    return text.PadLeft(width);
            }

            return null;
        }

    }
}