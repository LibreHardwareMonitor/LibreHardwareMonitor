// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PanManipulator.cs" company="OxyPlot">
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
//   The pan manipulator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    /// <summary>
    /// Provides a plot control manipulator for panning functionality.
    /// </summary>
    public class PanManipulator : ManipulatorBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PanManipulator"/> class.
        /// </summary>
        /// <param name="plotControl">
        /// The plot control.
        /// </param>
        public PanManipulator(IPlotControl plotControl)
            : base(plotControl)
        {
        }

        /// <summary>
        /// Gets or sets the previous position.
        /// </summary>
        private ScreenPoint PreviousPosition { get; set; }

        /// <summary>
        /// Occurs when the input device changes position during a manipulation.
        /// </summary>
        /// <param name="e">
        /// The <see cref="OxyPlot.ManipulationEventArgs"/> instance containing the event data.
        /// </param>
        public override void Delta(ManipulationEventArgs e)
        {
            base.Delta(e);
            if (this.XAxis != null)
            {
                this.PlotControl.Pan(this.XAxis, this.PreviousPosition, e.CurrentPosition);
            }

            if (this.YAxis != null)
            {
                this.PlotControl.Pan(this.YAxis, this.PreviousPosition, e.CurrentPosition);
            }

            this.PlotControl.RefreshPlot(false);
            this.PreviousPosition = e.CurrentPosition;
        }

        /// <summary>
        /// Gets the cursor for the manipulation.
        /// </summary>
        /// <returns>
        /// The cursor.
        /// </returns>
        public override CursorType GetCursorType()
        {
            return CursorType.Pan;
        }

        /// <summary>
        /// Occurs when an input device begins a manipulation on the plot.
        /// </summary>
        /// <param name="e">
        /// The <see cref="OxyPlot.ManipulationEventArgs"/> instance containing the event data.
        /// </param>
        public override void Started(ManipulationEventArgs e)
        {
            base.Started(e);
            this.PreviousPosition = e.CurrentPosition;
        }

    }
}