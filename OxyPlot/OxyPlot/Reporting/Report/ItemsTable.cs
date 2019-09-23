// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemsTable.cs" company="OxyPlot">
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
//   Represents a table of items.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Reporting
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Represents a table of items.
    /// </summary>
    public class ItemsTable : Table
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsTable"/> class.
        /// </summary>
        /// <param name="itemsInRows">
        /// The items in rows.
        /// </param>
        public ItemsTable(bool itemsInRows = true)
        {
            this.Fields = new List<ItemsTableField>();
            this.ItemsInRows = itemsInRows;
            this.Alignment = Alignment.Center;
        }

        /// <summary>
        /// Gets or sets Alignment.
        /// </summary>
        public Alignment Alignment { get; set; }

        /// <summary>
        /// Gets or sets Fields.
        /// </summary>
        public IList<ItemsTableField> Fields { get; set; }

        /// <summary>
        /// Gets or sets the items.
        /// The table will be filled when this property is set.
        /// </summary>
        /// <value>The items.</value>
        public IEnumerable Items { get; set; }

        /// <summary>
        /// Gets a value indicating whether ItemsInRows.
        /// </summary>
        public bool ItemsInRows { get; private set; }

        /// <summary>
        /// The has header.
        /// </summary>
        /// <returns>
        /// The has header.
        /// </returns>
        public bool HasHeader()
        {
            foreach (var c in this.Fields)
            {
                if (c.Header != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The to array.
        /// </summary>
        /// <returns>
        /// </returns>
        public string[,] ToArray()
        {
            List<object> items = this.Items.Cast<object>().ToList();
            int nrows = items.Count;

            bool hasHeader = this.HasHeader();
            if (hasHeader)
            {
                nrows++;
            }

            var result = new string[nrows, this.Fields.Count];

            int row = 0;
            if (hasHeader)
            {
                for (int i = 0; i < this.Fields.Count; i++)
                {
                    ItemsTableField c = this.Fields[i];
                    result[row, i] = c.Header;
                }

                row++;
            }

            foreach (var item in items)
            {
                for (int i = 0; i < this.Fields.Count; i++)
                {
                    ItemsTableField c = this.Fields[i];
                    string text = c.GetText(item, this.Report.ActualCulture);
                    result[row, i] = text;
                }

                row++;
            }

            if (!this.ItemsInRows)
            {
                result = Transpose(result);
            }

            return result;
        }

        /// <summary>
        /// The update.
        /// </summary>
        public override void Update()
        {
            base.Update();
            this.UpdateItems();
        }

        /// <summary>
        /// The update items.
        /// </summary>
        public void UpdateItems()
        {
            this.Rows.Clear();
            this.Columns.Clear();
            if (this.Fields == null || this.Fields.Count == 0)
            {
                return;
            }

            string[,] cells = this.ToArray();

            int rows = cells.GetUpperBound(0) + 1;
            int columns = cells.GetUpperBound(1) + 1;
            for (int i = 0; i < rows; i++)
            {
                var tr = new TableRow();
                if (this.ItemsInRows)
                {
                    tr.IsHeader = i == 0;
                }

                this.Rows.Add(tr);
                for (int j = 0; j < columns; j++)
                {
                    var tc = new TableCell();
                    tc.Content = cells[i, j];
                    tr.Cells.Add(tc);
                }
            }

            for (int j = 0; j < columns; j++)
            {
                var tc = new TableColumn();
                if (this.ItemsInRows)
                {
                    ItemsTableField f = this.Fields[j];
                    tc.Alignment = f.Alignment;
                    tc.Width = f.Width;
                }
                else
                {
                    tc.IsHeader = j == 0;
                    tc.Alignment = this.Alignment;
                }

                this.Columns.Add(tc);
            }
        }

        /// <summary>
        /// Writes the content of the item.
        /// </summary>
        /// <param name="w">
        /// The writer.
        /// </param>
        public override void WriteContent(IReportWriter w)
        {
            w.WriteTable(this);
        }

        /// <summary>
        /// The transpose.
        /// </summary>
        /// <param name="input">
        /// The input.
        /// </param>
        /// <returns>
        /// </returns>
        private static string[,] Transpose(string[,] input)
        {
            int rows = input.GetUpperBound(0) + 1;
            int cols = input.GetUpperBound(1) + 1;
            var result = new string[cols, rows];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[j, i] = input[i, j];
                }
            }

            return result;
        }

    }
}