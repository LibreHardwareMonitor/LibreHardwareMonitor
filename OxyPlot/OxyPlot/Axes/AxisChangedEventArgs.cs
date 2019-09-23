// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AxisChangedEventArgs.cs" company="OxyPlot">
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
//   EventArgs for the Axis.AxisChanged event.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.Axes
{
    using System;

    /// <summary>
    /// Provides additional data for the <see cref="Axis.AxisChanged"/> event.
    /// </summary>
    public class AxisChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AxisChangedEventArgs"/> class.
        /// </summary>
        /// <param name="changeType">
        /// Type of the change.
        /// </param>
        public AxisChangedEventArgs(AxisChangeTypes changeType)
        {
            this.ChangeType = changeType;
        }

        /// <summary>
        /// Gets or sets the type of the change.
        /// </summary>
        /// <value>The type of the change.</value>
        public AxisChangeTypes ChangeType { get; set; }

    }
}