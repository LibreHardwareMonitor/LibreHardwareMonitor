// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UIPlotElement.cs" company="OxyPlot">
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
//   Represents a plot element that handles mouse events.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;

    /// <summary>
    /// Provides an abstract base class for plot elements that handle mouse events.
    /// </summary>
    public abstract class UIPlotElement : SelectablePlotElement
    {
        /// <summary>
        /// Occurs when a mouse button is pressed down on the model.
        /// </summary>
        public event EventHandler<OxyMouseEventArgs> MouseDown;

        /// <summary>
        /// Occurs when the mouse is moved on the plot element (only occurs after MouseDown).
        /// </summary>
        public event EventHandler<OxyMouseEventArgs> MouseMove;

        /// <summary>
        /// Occurs when the mouse button is released on the plot element.
        /// </summary>
        public event EventHandler<OxyMouseEventArgs> MouseUp;

        /// <summary>
        /// Raises the <see cref="MouseDown"/> event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="OxyMouseEventArgs"/> instance containing the event data.
        /// </param>
        protected internal virtual void OnMouseDown(object sender, OxyMouseEventArgs e)
        {
            if (this.MouseDown != null)
            {
                this.MouseDown(sender, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="MouseMove"/> event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="OxyMouseEventArgs"/> instance containing the event data.
        /// </param>
        protected internal virtual void OnMouseMove(object sender, OxyMouseEventArgs e)
        {
            if (this.MouseMove != null)
            {
                this.MouseMove(sender, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="MouseUp"/> event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="OxyMouseEventArgs"/> instance containing the event data.
        /// </param>
        protected internal virtual void OnMouseUp(object sender, OxyMouseEventArgs e)
        {
            if (this.MouseUp != null)
            {
                this.MouseUp(sender, e);
            }
        }

        /// <summary>
        /// Tests if the plot element is hit by the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        /// A hit test result.
        /// </returns>
        protected internal abstract HitTestResult HitTest(ScreenPoint point, double tolerance);

    }
}