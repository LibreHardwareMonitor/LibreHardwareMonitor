// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ZoomManipulator.cs" company="OxyPlot">
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
//   The zoom manipulator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    /// <summary>
    /// Provides a plot control manipulator for zoom functionality.
    /// </summary>
    public class ZoomManipulator : ManipulatorBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomManipulator"/> class.
        /// </summary>
        /// <param name="plotControl">
        /// The plot control.
        /// </param>
        public ZoomManipulator(IPlotControl plotControl)
            : base(plotControl)
        {
        }

        /// <summary>
        /// Occurs when the input device changes position during a manipulation.
        /// </summary>
        /// <param name="e">The <see cref="OxyPlot.ManipulationEventArgs"/> instance containing the event data.</param>
        public override void Delta(ManipulationEventArgs e)
        {
            base.Delta(e);

            DataPoint current = this.InverseTransform(e.CurrentPosition.X, e.CurrentPosition.Y);

            if (this.XAxis != null)
            {
                this.PlotControl.ZoomAt(this.XAxis, e.ScaleX, current.X);
            }

            if (this.YAxis != null)
            {
                this.PlotControl.ZoomAt(this.YAxis, e.ScaleY, current.Y);
            }

            this.PlotControl.InvalidatePlot();
        }

    }
}