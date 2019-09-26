// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Content.cs" company="OxyPlot">
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
using System.Collections.Generic;

namespace OxyPlot.Reporting
{
    public class Content : Table
    {
        public List<ContentItem> Contents { get; set; }
        public ReportItem Base { get; set; }

        public Content(ReportItem b)
        {
            this.Base = b;
            Class = "content";
            Contents = new List<ContentItem>();
            Columns.Add(new TableColumn(null, "Chapter"));
            Columns.Add(new TableColumn(null, "Title"));
            Items = Contents;
        }

        public class ContentItem
        {
            public string Chapter { get; set; }
            public string Title { get; set; }
        }

        public override void Update()
        {
            Contents.Clear();
            var hh = new HeaderHelper();
            Search(Base, hh);
            base.Update();
        }

        private void Search(ReportItem item, HeaderHelper hh)
        {
            var h = item as Header;
            if (h != null)
            {
                h.Chapter = hh.GetHeader(h.Level);
                Contents.Add(new ContentItem() { Chapter = h.Chapter, Title = h.Text });
            }
            foreach (var c in item.Children)
                Search(c,hh);
        }
        public override void WriteContent(IReportWriter w)
        {
        }
    }
}