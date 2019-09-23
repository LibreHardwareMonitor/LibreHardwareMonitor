// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinearAxis.cs" company="OxyPlot">
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
//   Represents an axis with linear scale.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Axes
{
    /// <summary>
    /// Represents an axis with linear scale.
    /// </summary>
    public class LinearAxis : Axis
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinearAxis"/> class.
        /// </summary>
        public LinearAxis()
        {
            this.FractionUnit = 1.0;
            this.FractionUnitSymbol = null;
            this.FormatAsFractions = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearAxis"/> class.
        /// </summary>
        /// <param name="pos">
        /// The pos.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        public LinearAxis(AxisPosition pos, string title)
            : this()
        {
            this.Position = pos;
            this.Title = title;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearAxis"/> class.
        /// </summary>
        /// <param name="pos">
        /// The pos.
        /// </param>
        /// <param name="minimum">
        /// The minimum.
        /// </param>
        /// <param name="maximum">
        /// The maximum.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        public LinearAxis(
            AxisPosition pos, double minimum = double.NaN, double maximum = double.NaN, string title = null)
            : this(pos, minimum, maximum, double.NaN, double.NaN, title)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearAxis"/> class.
        /// </summary>
        /// <param name="pos">
        /// The pos.
        /// </param>
        /// <param name="minimum">
        /// The minimum.
        /// </param>
        /// <param name="maximum">
        /// The maximum.
        /// </param>
        /// <param name="majorStep">
        /// The major step.
        /// </param>
        /// <param name="minorStep">
        /// The minor step.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        public LinearAxis(
            AxisPosition pos, double minimum, double maximum, double majorStep, double minorStep, string title = null)
            : this(pos, title)
        {
            this.Minimum = minimum;
            this.Maximum = maximum;
            this.MajorStep = majorStep;
            this.MinorStep = minorStep;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to format numbers as fractions.
        /// </summary>
        public bool FormatAsFractions { get; set; }

        /// <summary>
        /// Gets or sets the fraction unit. Remember to set FormatAsFractions to true.
        /// </summary>
        /// <value> The fraction unit. </value>
        public double FractionUnit { get; set; }

        /// <summary>
        /// Gets or sets the fraction unit symbol. Use FractionUnit = Math.PI and FractionUnitSymbol = "π" if you want the axis to show "π/2,π,3π/2,2π" etc. Use FractionUnit = 1 and FractionUnitSymbol = "L" if you want the axis to show "0,L/2,L" etc. Remember to set FormatAsFractions to true.
        /// </summary>
        /// <value> The fraction unit symbol. </value>
        public string FractionUnitSymbol { get; set; }

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
            if (this.FormatAsFractions)
            {
                return FractionHelper.ConvertToFractionString(
                    x, this.FractionUnit, this.FractionUnitSymbol, 1e-6, this.ActualCulture);
            }

            return base.FormatValue(x);
        }

        /// <summary>
        /// Determines whether the axis is used for X/Y values.
        /// </summary>
        /// <returns>
        /// <c>true</c> if it is an XY axis; otherwise, <c>false</c> .
        /// </returns>
        public override bool IsXyAxis()
        {
            return true;
        }

    }
}