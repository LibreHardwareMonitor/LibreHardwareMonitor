// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ZoomStepManipulator.cs" company="OxyPlot">
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
//   The step manipulator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    /// <summary>
    /// Provides a plot control manipulator for stepwise zoom functionality.
    /// </summary>
    public class ZoomStepManipulator : ManipulatorBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomStepManipulator"/> class.
        /// </summary>
        /// <param name="plotControl">
        /// The plot control.
        /// </param>
        /// <param name="step">
        /// The step.
        /// </param>
        /// <param name="fineControl">
        /// The fine Control.
        /// </param>
        public ZoomStepManipulator(IPlotControl plotControl, double step, bool fineControl)
            : base(plotControl)
        {
            this.Step = step;
            this.FineControl = fineControl;
        }

        /// <summary>
        /// Gets or sets a value indicating whether FineControl.
        /// </summary>
        public bool FineControl { get; set; }

        /// <summary>
        /// Gets or sets Step.
        /// </summary>
        public double Step { get; set; }

        /// <summary>
        /// The started.
        /// </summary>
        /// <param name="e">
        /// The e.
        /// </param>
        public override void Started(ManipulationEventArgs e)
        {
            base.Started(e);

            DataPoint current = this.InverseTransform(e.CurrentPosition.X, e.CurrentPosition.Y);

            double scale = this.Step;
            if (this.FineControl)
            {
                scale *= 3;
            }

            scale = 1 + scale;

            // make sure the zoom factor is not negative
            if (scale < 0.1)
            {
                scale = 0.1;
            }

            if (this.XAxis != null)
            {
                this.PlotControl.ZoomAt(this.XAxis, scale, current.X);
            }

            if (this.YAxis != null)
            {
                this.PlotControl.ZoomAt(this.YAxis, scale, current.Y);
            }

            this.PlotControl.InvalidatePlot();
        }

    }
}