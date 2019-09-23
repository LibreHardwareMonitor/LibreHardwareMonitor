// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OxyMouseEventArgs.cs" company="OxyPlot">
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
//   Represents event arguments for 3D mouse events events.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    /// <summary>
    /// Provides data for the mouse events.
    /// </summary>
    public class OxyMouseEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the mouse button that has changed.
        /// </summary>
        public OxyMouseButton ChangedButton { get; set; }

        /// <summary>
        /// Gets or sets the click count.
        /// </summary>
        /// <value> The click count. </value>
        public int ClickCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Handled.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the alt key was pressed when the event was raised.
        /// </summary>
        public bool IsAltDown { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the control key was pressed when the event was raised.
        /// </summary>
        public bool IsControlDown { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shift key was pressed when the event was raised.
        /// </summary>
        public bool IsShiftDown { get; set; }

        /// <summary>
        /// Gets or sets the hit test result.
        /// </summary>
        public HitTestResult HitTestResult { get; set; }

        /// <summary>
        /// Gets or sets the plot control.
        /// </summary>
        /// <value> The plot control. </value>
        public IPlotControl PlotControl { get; set; }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        public ScreenPoint Position { get; set; }

    }
}