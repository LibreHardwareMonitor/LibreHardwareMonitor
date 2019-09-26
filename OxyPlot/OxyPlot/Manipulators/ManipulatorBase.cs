// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ManipulatorBase.cs" company="OxyPlot">
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
//   The manipulator base.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using OxyPlot.Axes;

    /// <summary>
    /// Provides an absract base class for plot control manipulators.
    /// </summary>
    public class ManipulatorBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManipulatorBase"/> class.
        /// </summary>
        /// <param name="plotControl">
        /// The plot control.
        /// </param>
        protected ManipulatorBase(IPlotControl plotControl)
        {
            this.PlotControl = plotControl;
        }

        /// <summary>
        /// Gets the first position of the manipulation.
        /// </summary>
        public ScreenPoint StartPosition { get; private set; }

        /// <summary>
        /// Gets the plot control.
        /// </summary>
        protected IPlotControl PlotControl { get; private set; }

        /// <summary>
        /// Gets or sets the X axis.
        /// </summary>
        /// <value>The X axis.</value>
        protected Axis XAxis { get; set; }

        /// <summary>
        /// Gets or sets the Y axis.
        /// </summary>
        /// <value>The Y axis.</value>
        protected Axis YAxis { get; set; }

        /// <summary>
        /// Occurs when a manipulation is complete.
        /// </summary>
        /// <param name="e">
        /// The <see cref="OxyPlot.ManipulationEventArgs"/> instance containing the event data.
        /// </param>
        public virtual void Completed(ManipulationEventArgs e)
        {
            this.PlotControl.SetCursorType(CursorType.Default);
        }

        /// <summary>
        /// Occurs when the input device changes position during a manipulation.
        /// </summary>
        /// <param name="e">
        /// The <see cref="OxyPlot.ManipulationEventArgs"/> instance containing the event data.
        /// </param>
        public virtual void Delta(ManipulationEventArgs e)
        {
        }

        /// <summary>
        /// Gets the cursor for the manipulation.
        /// </summary>
        /// <returns>
        /// The cursor.
        /// </returns>
        public virtual CursorType GetCursorType()
        {
            return CursorType.Default;
        }

        /// <summary>
        /// Occurs when an input device begins a manipulation on the plot.
        /// </summary>
        /// <param name="e">
        /// The <see cref="OxyPlot.ManipulationEventArgs"/> instance containing the event data.
        /// </param>
        public virtual void Started(ManipulationEventArgs e)
        {
            Axis xaxis;
            Axis yaxis;
            this.PlotControl.GetAxesFromPoint(e.CurrentPosition, out xaxis, out yaxis);
            this.StartPosition = e.CurrentPosition;

            this.XAxis = xaxis;
            this.YAxis = yaxis;

            this.PlotControl.SetCursorType(this.GetCursorType());
        }

        /// <summary>
        /// Transforms a point from screen coordinates to data coordinates.
        /// </summary>
        /// <param name="x">
        /// The x coordinate.
        /// </param>
        /// <param name="y">
        /// The y coordinate.
        /// </param>
        /// <returns>
        /// A data point.
        /// </returns>
        protected DataPoint InverseTransform(double x, double y)
        {
            if (this.XAxis != null)
            {
                return this.XAxis.InverseTransform(x, y, this.YAxis);
            }

            if (this.YAxis != null)
            {
                return new DataPoint(0, this.YAxis.InverseTransform(y));
            }

            return new DataPoint();
        }

    }
}