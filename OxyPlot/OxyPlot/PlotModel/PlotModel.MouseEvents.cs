// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotModel.MouseEvents.cs" company="OxyPlot">
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
//   Partial PlotModel class - this file contains mouse events and handlers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Linq;

    public partial class PlotModel
    {
        /// <summary>
        /// The mouse hit tolerance.
        /// </summary>
        private const double MouseHitTolerance = 10;

        /// <summary>
        /// The current mouse events element.
        /// </summary>
        private UIPlotElement currentMouseEventElement;

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
        /// Handles the mouse down event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="OxyPlot.OxyMouseEventArgs"/> instance containing the event data.
        /// </param>
        public void HandleMouseDown(object sender, OxyMouseEventArgs e)
        {
            // Revert the order to handle the top-level elements first
            foreach (var element in this.GetElements().Reverse())
            {
                var uiElement = element as UIPlotElement;
                if (uiElement == null)
                {
                    continue;
                }

                var result = uiElement.HitTest(e.Position, MouseHitTolerance);
                if (result != null)
                {
                    e.HitTestResult = result;
                    uiElement.OnMouseDown(sender, e);
                    if (e.Handled)
                    {
                        this.currentMouseEventElement = uiElement;
                    }
                }

                if (e.Handled)
                {
                    break;
                }
            }

            if (!e.Handled)
            {
                this.OnMouseDown(sender, e);
            }
        }

        /// <summary>
        /// Handles the mouse move event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="OxyPlot.OxyMouseEventArgs"/> instance containing the event data.
        /// </param>
        public void HandleMouseMove(object sender, OxyMouseEventArgs e)
        {
            if (this.currentMouseEventElement != null)
            {
                this.currentMouseEventElement.OnMouseMove(sender, e);
            }

            if (!e.Handled)
            {
                this.OnMouseMove(sender, e);
            }
        }

        /// <summary>
        /// Handles the mouse up event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="OxyPlot.OxyMouseEventArgs"/> instance containing the event data.
        /// </param>
        public void HandleMouseUp(object sender, OxyMouseEventArgs e)
        {
            if (this.currentMouseEventElement != null)
            {
                this.currentMouseEventElement.OnMouseUp(sender, e);
                this.currentMouseEventElement = null;
            }

            if (!e.Handled)
            {
                this.OnMouseUp(sender, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="MouseDown"/> event.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The <see cref="OxyMouseEventArgs"/> instance containing the event data.
        /// </param>
        protected virtual void OnMouseDown(object sender, OxyMouseEventArgs e)
        {
            if (this.MouseDown != null && !e.Handled)
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
        protected virtual void OnMouseMove(object sender, OxyMouseEventArgs e)
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
        protected virtual void OnMouseUp(object sender, OxyMouseEventArgs e)
        {
            if (this.MouseUp != null)
            {
                this.MouseUp(sender, e);
            }
        }

    }
}