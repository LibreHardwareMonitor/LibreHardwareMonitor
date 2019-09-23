// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectablePlotElement.cs" company="OxyPlot">
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
//   Represents a plot element that supports selection.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    /// <summary>
    /// Provides an abstract base class for plot elements that support selection.
    /// </summary>
    public abstract class SelectablePlotElement : PlotElement
    {
        /// <summary>
        /// The is selected.
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectablePlotElement"/> class.
        /// </summary>
        protected SelectablePlotElement()
        {
            this.Selectable = true;
            this.IsSelected = false;
        }

        /// <summary>
        /// Occurs when the IsSelected property is changed.
        /// </summary>
        public event EventHandler Selected;

        /// <summary>
        /// Gets or sets the index of the selected item (or -1 if all items are selected).
        /// </summary>
        /// <value>
        /// The index of the selected.
        /// </value>
        public int SelectedIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this plot element is selected.
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return this.isSelected;
            }

            set
            {
                if (value == this.isSelected)
                {
                    return;
                }

                this.isSelected = value;
                this.OnIsSelectedChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this plot element can be selected.
        /// </summary>
        public bool Selectable { get; set; }

        /// <summary>
        /// Gets the actual selection color.
        /// </summary>
        /// <value> The actual selection color. </value>
        protected OxyColor ActualSelectedColor
        {
            get
            {
                if (this.PlotModel != null)
                {
                    return this.PlotModel.SelectionColor ?? PlotModel.DefaultSelectionColor;
                }

                return PlotModel.DefaultSelectionColor;
            }
        }

        /// <summary>
        /// Gets the selection color it the element is selected, or the specified color if it is not.
        /// </summary>
        /// <param name="originalColor">The unselected color of the element.</param>
        /// <param name="index">The index of the item to check (use -1 for all items).</param>
        /// <returns>
        /// A color.
        /// </returns>
        protected OxyColor GetSelectableColor(OxyColor originalColor, int index = -1)
        {
            if (originalColor == null)
            {
                return null;
            }

            if (this.IsSelected && (index == -1 || index == this.SelectedIndex))
            {
                return this.ActualSelectedColor;
            }

            return originalColor;
        }

        /// <summary>
        /// Gets the selection fill color it the element is selected, or the specified fill color if it is not.
        /// </summary>
        /// <param name="originalColor">The unselected fill color of the element.</param>
        /// <param name="index">The index of the item to check (use -1 for all items).</param>
        /// <returns>
        /// A fill color.
        /// </returns>
        protected OxyColor GetSelectableFillColor(OxyColor originalColor, int index = -1)
        {
            return this.GetSelectableColor(originalColor, index);
        }

        /// <summary>
        /// Raises the Selected event.
        /// </summary>
        protected void OnIsSelectedChanged()
        {
            var eh = this.Selected;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

    }
}