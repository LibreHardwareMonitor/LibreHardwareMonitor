// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CategoryAxis.cs" company="OxyPlot">
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
//   Represents a category axes.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Axes
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a category axis.
    /// </summary>
    /// <remarks>
    /// The category axis is using the label collection indices as the coordinate. If you have 5 categories in the Labels collection, the categories will be placed at coordinates 0 to 4. The range of the axis will be from -0.5 to 4.5 (excl. padding).
    /// </remarks>
    public class CategoryAxis : LinearAxis
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryAxis"/> class.
        /// </summary>
        public CategoryAxis()
        {
            this.Labels = new List<string>();
            this.TickStyle = TickStyle.Outside;
            this.Position = AxisPosition.Bottom;
            this.MinimumPadding = 0;
            this.MaximumPadding = 0;
            this.MajorStep = 1;
            this.GapWidth = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryAxis"/> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="title">The title.</param>
        /// <param name="categories">The categories.</param>
        public CategoryAxis(AxisPosition position, string title = null, params string[] categories)
            : this(title, categories)
        {
            this.Position = position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryAxis"/> class.
        /// </summary>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="categories">
        /// The categories.
        /// </param>
        public CategoryAxis(string title, params string[] categories)
            : this()
        {
            this.Title = title;
            if (categories != null)
            {
                foreach (var c in categories)
                {
                    this.Labels.Add(c);
                }
            }
        }

        /// <summary>
        /// Gets or sets the gap width.
        /// </summary>
        /// <remarks>
        /// The default value is 1.0 (100%). The gap width is given as a fraction of the total width/height of the items in a category.
        /// </remarks>
        public double GapWidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ticks are centered. If this is false, ticks will be drawn between each category. If this is true, ticks will be drawn in the middle of each category.
        /// </summary>
        public bool IsTickCentered { get; set; }

        /// <summary>
        /// Gets or sets the items source (used to update the Labels collection).
        /// </summary>
        /// <value>
        /// The items source.
        /// </value>
        public IEnumerable ItemsSource { get; set; }

        /// <summary>
        /// Gets or sets the data field for the labels.
        /// </summary>
        public string LabelField { get; set; }

        /// <summary>
        /// Gets or sets the labels collection.
        /// </summary>
        public IList<string> Labels { get; set; }

        /// <summary>
        /// Gets or sets the current offset of the bars (not used for stacked bar series).
        /// </summary>
        internal double[] BarOffset { get; set; }

        /// <summary>
        /// Gets or sets the max value per StackIndex and Label (only used for stacked bar series).
        /// </summary>
        internal double[,] MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the maximal width of all labels
        /// </summary>
        internal double MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the min value per StackIndex and Label (only used for stacked bar series).
        /// </summary>
        internal double[,] MinValue { get; set; }

        /// <summary>
        /// Gets or sets per StackIndex and Label the base value for negative values of stacked bar series.
        /// </summary>
        internal double[,] NegativeBaseValues { get; set; }

        /// <summary>
        /// Gets or sets per StackIndex and Label the base value for positive values of stacked bar series.
        /// </summary>
        internal double[,] PositiveBaseValues { get; set; }

        /// <summary>
        /// Gets or sets the StackIndexMapping. The mapping indicates to which rank a specific stack index belongs.
        /// </summary>
        internal Dictionary<string, int> StackIndexMapping { get; set; }

        /// <summary>
        /// Gets or sets the offset of the bars per StackIndex and Label (only used for stacked bar series).
        /// </summary>
        internal double[,] StackedBarOffset { get; set; }

        /// <summary>
        /// Gets or sets sum of the widths of the single bars per label. This is used to find the bar width of BarSeries
        /// </summary>
        internal double[] TotalWidthPerCategory { get; set; }

        /// <summary>
        /// Fills the specified array.
        /// </summary>
        /// <param name="array">
        /// The array.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public static void Fill(double[] array, double value)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        /// <summary>
        /// Fills the specified array.
        /// </summary>
        /// <param name="array">
        /// The array.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public static void Fill(double[,] array, double value)
        {
            for (var i = 0; i < array.GetLength(0); i++)
            {
                for (var j = 0; j < array.GetLength(1); j++)
                {
                    array[i, j] = value;
                }
            }
        }

        /// <summary>
        /// Formats the value to be used on the axis.
        /// </summary>
        /// <param name="x">
        /// The value.
        /// </param>
        /// <returns>
        /// The formatted value.
        /// </returns>
        public override string FormatValue(double x)
        {
            var index = (int)x;
            if (this.Labels != null && index >= 0 && index < this.Labels.Count)
            {
                return this.Labels[index];
            }

            return null;
        }

        /// <summary>
        /// Formats the value to be used by the tracker.
        /// </summary>
        /// <param name="x">
        /// The value.
        /// </param>
        /// <returns>
        /// The formatted value.
        /// </returns>
        public override string FormatValueForTracker(double x)
        {
            return this.FormatValue(x);
        }

        /// <summary>
        /// Gets the category value.
        /// </summary>
        /// <param name="categoryIndex">
        /// Index of the category.
        /// </param>
        /// <param name="stackIndex">
        /// Index of the stack.
        /// </param>
        /// <param name="actualBarWidth">
        /// Actual width of the bar.
        /// </param>
        /// <returns>
        /// The get category value.
        /// </returns>
        public double GetCategoryValue(int categoryIndex, int stackIndex, double actualBarWidth)
        {
            var offsetBegin = this.StackedBarOffset[stackIndex, categoryIndex];
            var offsetEnd = this.StackedBarOffset[stackIndex + 1, categoryIndex];
            return categoryIndex - 0.5 + ((offsetEnd + offsetBegin - actualBarWidth) * 0.5);
        }

        /// <summary>
        /// Gets the category value.
        /// </summary>
        /// <param name="categoryIndex">
        /// Index of the category.
        /// </param>
        /// <returns>
        /// The get category value.
        /// </returns>
        public double GetCategoryValue(int categoryIndex)
        {
            return categoryIndex - 0.5 + this.BarOffset[categoryIndex];
        }

        /// <summary>
        /// Gets the coordinates used to draw ticks and tick labels (numbers or category names).
        /// </summary>
        /// <param name="majorLabelValues">
        /// The major label values.
        /// </param>
        /// <param name="majorTickValues">
        /// The major tick values.
        /// </param>
        /// <param name="minorTickValues">
        /// The minor tick values.
        /// </param>
        public override void GetTickValues(
            out IList<double> majorLabelValues, out IList<double> majorTickValues, out IList<double> minorTickValues)
        {
            base.GetTickValues(out majorLabelValues, out majorTickValues, out minorTickValues);
            minorTickValues.Clear();

            if (!this.IsTickCentered)
            {
                // Subtract 0.5 from the label values to get the tick values.
                // Add one extra tick at the end.
                var mv = new List<double>(majorLabelValues.Count);
                mv.AddRange(majorLabelValues.Select(v => v - 0.5));
                if (mv.Count > 0)
                {
                    mv.Add(mv[mv.Count - 1] + 1);
                }

                majorTickValues = mv;
            }
        }

        /// <summary>
        /// Gets the value from an axis coordinate, converts from double to the correct data type if necessary. e.g. DateTimeAxis returns the DateTime and CategoryAxis returns category strings.
        /// </summary>
        /// <param name="x">
        /// The coordinate.
        /// </param>
        /// <returns>
        /// The value.
        /// </returns>
        public override object GetValue(double x)
        {
            return this.FormatValue(x);
        }

        /// <summary>
        /// Updates the actual maximum and minimum values. If the user has zoomed/panned the axis, the internal ViewMaximum/ViewMinimum values will be used. If Maximum or Minimum have been set, these values will be used. Otherwise the maximum and minimum values of the series will be used, including the 'padding'.
        /// </summary>
        internal override void UpdateActualMaxMin()
        {
            // Update the DataMinimum/DataMaximum from the number of categories
            this.Include(-0.5);

            if (this.Labels != null && this.Labels.Count > 0)
            {
                this.Include((this.Labels.Count - 1) + 0.5);
            }
            else
            {
                this.Include(0.5);
            }

            base.UpdateActualMaxMin();

            this.MinorStep = 1;
        }

        /// <summary>
        /// Updates the axis with information from the plot series.
        /// </summary>
        /// <param name="series">
        /// The series collection.
        /// </param>
        /// <remarks>
        /// This is used by the category axis that need to know the number of series using the axis.
        /// </remarks>
        internal override void UpdateFromSeries(IEnumerable<Series> series)
        {
            if (this.Labels.Count == 0)
            {
                this.TotalWidthPerCategory = null;
                this.MaxWidth = double.NaN;
                this.BarOffset = null;
                this.StackedBarOffset = null;
                this.StackIndexMapping = null;
                this.PositiveBaseValues = null;
                this.NegativeBaseValues = null;
                this.MaxValue = null;
                this.MinValue = null;

                return;
            }

            this.TotalWidthPerCategory = new double[this.Labels.Count];

            var usedSeries = series.Where(s => s.IsUsing(this)).ToList();

            // Add width of stacked series
            var categorizedSeries = usedSeries.OfType<CategorizedSeries>().ToList();
            var stackedSeries = categorizedSeries.OfType<IStackableSeries>().Where(s => s.IsStacked).ToList();
            var stackIndices = stackedSeries.Select(s => s.StackGroup).Distinct().ToList();
            var stackRankBarWidth = new Dictionary<int, double>();
            for (var j = 0; j < stackIndices.Count; j++)
            {
                var maxBarWidth =
                    stackedSeries.Where(s => s.StackGroup == stackIndices[j]).Select(
                        s => ((CategorizedSeries)s).GetBarWidth()).Concat(new[] { 0.0 }).Max();
                for (var i = 0; i < this.Labels.Count; i++)
                {
                    int k = 0;
                    if (
                        stackedSeries.SelectMany(s => ((CategorizedSeries)s).GetItems()).Any(
                            item => item.GetCategoryIndex(k++) == i))
                    {
                        this.TotalWidthPerCategory[i] += maxBarWidth;
                    }
                }

                stackRankBarWidth[j] = maxBarWidth;
            }

            // Add width of unstacked series
            var unstackedBarSeries =
                categorizedSeries.Where(s => !(s is IStackableSeries) || !((IStackableSeries)s).IsStacked).ToList();
            foreach (var s in unstackedBarSeries)
            {
                for (var i = 0; i < this.Labels.Count; i++)
                {
                    int j = 0;
                    var numberOfItems = s.GetItems().Count(item => item.GetCategoryIndex(j++) == i);
                    this.TotalWidthPerCategory[i] += s.GetBarWidth() * numberOfItems;
                }
            }

            this.MaxWidth = this.TotalWidthPerCategory.Max();

            // Calculate BarOffset and StackedBarOffset
            this.BarOffset = new double[this.Labels.Count];
            this.StackedBarOffset = new double[stackIndices.Count + 1, this.Labels.Count];

            var factor = 0.5 / (1 + this.GapWidth) / this.MaxWidth;
            for (var i = 0; i < this.Labels.Count; i++)
            {
                this.BarOffset[i] = 0.5 - (this.TotalWidthPerCategory[i] * factor);
            }

            for (var j = 0; j <= stackIndices.Count; j++)
            {
                for (var i = 0; i < this.Labels.Count; i++)
                {
                    int k = 0;
                    if (
                        stackedSeries.SelectMany(s => ((CategorizedSeries)s).GetItems()).All(
                            item => item.GetCategoryIndex(k++) != i))
                    {
                        continue;
                    }

                    this.StackedBarOffset[j, i] = this.BarOffset[i];
                    if (j < stackIndices.Count)
                    {
                        this.BarOffset[i] += stackRankBarWidth[j] / (1 + this.GapWidth) / this.MaxWidth;
                    }
                }
            }

            stackIndices.Sort();
            this.StackIndexMapping = new Dictionary<string, int>();
            for (var i = 0; i < stackIndices.Count; i++)
            {
                this.StackIndexMapping.Add(stackIndices[i], i);
            }

            this.PositiveBaseValues = new double[stackIndices.Count, this.Labels.Count];
            Fill(this.PositiveBaseValues, double.NaN);
            this.NegativeBaseValues = new double[stackIndices.Count, this.Labels.Count];
            Fill(this.NegativeBaseValues, double.NaN);

            this.MaxValue = new double[stackIndices.Count, this.Labels.Count];
            Fill(this.MaxValue, double.NaN);
            this.MinValue = new double[stackIndices.Count, this.Labels.Count];
            Fill(this.MinValue, double.NaN);
        }

        /// <summary>
        /// Creates Labels list if no labels were set
        /// </summary>
        /// <param name="series">
        /// The list of series which are rendered
        /// </param>
        internal void UpdateLabels(IEnumerable<Series> series)
        {
            if (this.ItemsSource != null)
            {
                this.Labels.Clear();
                ReflectionHelper.FillList(this.ItemsSource, this.LabelField, this.Labels);
            }

            if (this.Labels.Count == 0)
            {
                foreach (var s in series)
                {
                    if (!s.IsUsing(this))
                    {
                        continue;
                    }

                    var bsb = s as CategorizedSeries;
                    if (bsb != null)
                    {
                        int max = bsb.GetItems().Count;
                        while (this.Labels.Count < max)
                        {
                            this.Labels.Add((this.Labels.Count + 1).ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }
            }
        }

    }
}