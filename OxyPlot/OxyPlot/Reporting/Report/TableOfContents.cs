// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TableOfContents.cs" company="OxyPlot">
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
//   Represents a table of contents.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a table of contents.
    /// </summary>
    public class TableOfContents : ItemsTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableOfContents"/> class.
        /// </summary>
        /// <param name="b">
        /// The b.
        /// </param>
        public TableOfContents(ReportItem b)
        {
            this.Base = b;
            this.Contents = new List<ContentItem>();
            this.Fields.Add(new ItemsTableField(null, "Chapter"));
            this.Fields.Add(new ItemsTableField(null, "Title"));
            this.Items = this.Contents;
        }

        /// <summary>
        /// Gets or sets Base.
        /// </summary>
        public ReportItem Base { get; set; }

        /// <summary>
        /// Gets or sets Contents.
        /// </summary>
        public List<ContentItem> Contents { get; set; }

        /// <summary>
        /// The update.
        /// </summary>
        public override void Update()
        {
            this.Contents.Clear();
            var hh = new HeaderHelper();
            this.Search(this.Base, hh);
            base.Update();
        }

        /// <summary>
        /// The search.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="hh">
        /// The hh.
        /// </param>
        private void Search(ReportItem item, HeaderHelper hh)
        {
            var h = item as Header;
            if (h != null)
            {
                h.Chapter = hh.GetHeader(h.Level);
                this.Contents.Add(new ContentItem { Chapter = h.Chapter, Title = h.Text });
            }

            foreach (var c in item.Children)
            {
                this.Search(c, hh);
            }
        }

        /// <summary>
        /// The content item.
        /// </summary>
        public class ContentItem
        {
            /// <summary>
            /// Gets or sets Chapter.
            /// </summary>
            public string Chapter { get; set; }

            /// <summary>
            /// Gets or sets Title.
            /// </summary>
            public string Title { get; set; }
        }
    }
}