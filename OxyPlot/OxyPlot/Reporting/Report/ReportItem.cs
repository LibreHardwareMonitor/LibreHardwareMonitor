// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReportItem.cs" company="OxyPlot">
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
//   Represents a report item (abstract base class).
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Represents a report item (abstract base class).
    /// </summary>
    public abstract class ReportItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref = "ReportItem" /> class.
        /// </summary>
        protected ReportItem()
        {
            this.Children = new Collection<ReportItem>();
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        public Collection<ReportItem> Children { get; private set; }

        /// <summary>
        /// Gets the report.
        /// </summary>
        public Report Report { get; internal set; }

        /// <summary>
        /// Adds a report item to the report.
        /// </summary>
        /// <param name="child">
        /// The child.
        /// </param>
        public void Add(ReportItem child)
        {
            this.Children.Add(child);
        }

        /// <summary>
        /// Adds a drawing to the report.
        /// </summary>
        /// <param name="content">
        /// The content.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        public void AddDrawing(string content, string text)
        {
            this.Add(new DrawingFigure { Content = content, FigureText = text });
        }

        /// <summary>
        /// Adds a plot to the report.
        /// </summary>
        /// <param name="plot">The plot model.</param>
        /// <param name="text">The text.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public void AddPlot(PlotModel plot, string text, double width, double height)
        {
            this.Add(new PlotFigure { PlotModel = plot, Width = width, Height = height, FigureText = text });
        }

        /// <summary>
        /// Adds an equation to the report.
        /// </summary>
        /// <param name="equation">
        /// The equation.
        /// </param>
        /// <param name="caption">
        /// The caption.
        /// </param>
        public void AddEquation(string equation, string caption = null)
        {
            this.Add(new Equation { Content = equation, Caption = caption });
        }

        /// <summary>
        /// Adds a header to the report.
        /// </summary>
        /// <param name="level">
        /// The level.
        /// </param>
        /// <param name="header">
        /// The header.
        /// </param>
        public void AddHeader(int level, string header)
        {
            this.Add(new Header { Level = level, Text = header });
        }

        /// <summary>
        /// Adds an image to the report.
        /// </summary>
        /// <param name="src">
        /// The src.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        public void AddImage(string src, string text)
        {
            this.Add(new Image { Source = src, FigureText = text });
        }

        /// <summary>
        /// Adds an items table to the report.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="items">
        /// The items.
        /// </param>
        /// <param name="fields">
        /// The fields.
        /// </param>
        public void AddItemsTable(string title, IEnumerable items, IList<ItemsTableField> fields)
        {
            this.Add(new ItemsTable { Caption = title, Items = items, Fields = fields });
        }

        /// <summary>
        /// Adds a paragraph to the report.
        /// </summary>
        /// <param name="content">
        /// The content.
        /// </param>
        public void AddParagraph(string content)
        {
            this.Add(new Paragraph { Text = content });
        }

        /// <summary>
        /// Adds a property table to the report.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="obj">
        /// The object.
        /// </param>
        /// <returns>
        /// A PropertyTable.
        /// </returns>
        public PropertyTable AddPropertyTable(string title, object obj)
        {
            var items = obj as IEnumerable;
            if (items == null)
            {
                items = new[] { obj };
            }

            var pt = new PropertyTable(items, false) { Caption = title };
            this.Add(pt);
            return pt;
        }

        /// <summary>
        /// The add table of contents.
        /// </summary>
        /// <param name="b">
        /// The b.
        /// </param>
        public void AddTableOfContents(ReportItem b)
        {
            this.Add(new TableOfContents(b));
        }

        /// <summary>
        /// The update.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// The write.
        /// </summary>
        /// <param name="w">
        /// The w.
        /// </param>
        public virtual void Write(IReportWriter w)
        {
            this.Update();
            this.WriteContent(w);
            foreach (var child in this.Children)
            {
                child.Write(w);
            }
        }

        /// <summary>
        /// Writes the content of the item.
        /// </summary>
        /// <param name="w">
        /// The writer.
        /// </param>
        public virtual void WriteContent(IReportWriter w)
        {
        }

        /// <summary>
        /// The update figure numbers.
        /// </summary>
        protected void UpdateFigureNumbers()
        {
            var fc = new FigureCounter();
            this.UpdateFigureNumbers(fc);
        }

        /// <summary>
        /// Updates the Report property.
        /// </summary>
        /// <param name="report">
        /// The report.
        /// </param>
        protected void UpdateParent(Report report)
        {
            this.Report = report;
            foreach (var child in this.Children)
            {
                child.UpdateParent(report);
            }
        }

        /// <summary>
        /// The update figure numbers.
        /// </summary>
        /// <param name="fc">
        /// The fc.
        /// </param>
        private void UpdateFigureNumbers(FigureCounter fc)
        {
            var table = this as Table;
            if (table != null)
            {
                table.TableNumber = fc.TableNumber++;
            }

            var figure = this as Figure;
            if (figure != null)
            {
                figure.FigureNumber = fc.FigureNumber++;
            }

            foreach (var child in this.Children)
            {
                child.UpdateFigureNumbers(fc);
            }
        }

        /// <summary>
        /// The figure counter.
        /// </summary>
        private class FigureCounter
        {
            /// <summary>
            /// Initializes a new instance of the <see cref = "FigureCounter" /> class.
            /// </summary>
            public FigureCounter()
            {
                this.FigureNumber = 1;
                this.TableNumber = 1;
            }

            /// <summary>
            /// Gets or sets FigureNumber.
            /// </summary>
            public int FigureNumber { get; set; }

            /// <summary>
            /// Gets or sets TableNumber.
            /// </summary>
            public int TableNumber { get; set; }

        }
    }
}