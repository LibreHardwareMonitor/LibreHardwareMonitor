// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Plot.cs" company="OxyPlot">
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
//   Represents a control that displays a plot.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot.WindowsForms
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    using OxyPlot.Axes;
    using OxyPlot.Series;

    /// <summary>
    /// Represents a control that displays a plot.
    /// </summary>
    [Serializable]
    public class Plot : Control, IPlotControl
    {
        /// <summary>
        /// The category for the properties of this control.
        /// </summary>
        private const string OxyPlotCategory = "OxyPlot";

        /// <summary>
        /// The invalidate lock.
        /// </summary>
        private readonly object invalidateLock = new object();

        /// <summary>
        /// The model lock.
        /// </summary>
        private readonly object modelLock = new object();

        /// <summary>
        /// The rendering lock.
        /// </summary>
        private readonly object renderingLock = new object();

        /// <summary>
        /// The current model (holding a reference to this plot control).
        /// </summary>
        [NonSerialized]
        private PlotModel currentModel;

        /// <summary>
        /// The is model invalidated.
        /// </summary>
        private bool isModelInvalidated;

        /// <summary>
        /// The model.
        /// </summary>
        private PlotModel model;

        /// <summary>
        /// The mouse manipulator.
        /// </summary>
        [NonSerialized]
        private ManipulatorBase mouseManipulator;

        /// <summary>
        /// The update data flag.
        /// </summary>
        private bool updateDataFlag = true;

        /// <summary>
        /// The zoom rectangle.
        /// </summary>
        private Rectangle zoomRectangle;

        /// <summary>
        /// The render context.
        /// </summary>
        private GraphicsRenderContext renderContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plot"/> class.
        /// </summary>
        public Plot()
        {
            this.renderContext = new GraphicsRenderContext();

            // ReSharper disable DoNotCallOverridableMethodsInConstructor
            this.DoubleBuffered = true;
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
            this.KeyboardPanHorizontalStep = 0.1;
            this.KeyboardPanVerticalStep = 0.1;
            this.PanCursor = Cursors.Hand;
            this.ZoomRectangleCursor = Cursors.SizeNWSE;
            this.ZoomHorizontalCursor = Cursors.SizeWE;
            this.ZoomVerticalCursor = Cursors.SizeNS;
        }

        /// <summary>
        /// Gets the actual model.
        /// </summary>
        /// <value> The actual model. </value>
        public PlotModel ActualModel
        {
            get
            {
                return this.Model;
            }
        }

        /// <summary>
        /// Gets or sets the keyboard pan horizontal step.
        /// </summary>
        /// <value> The keyboard pan horizontal step. </value>
        [Category(OxyPlotCategory)]
        public double KeyboardPanHorizontalStep { get; set; }

        /// <summary>
        /// Gets or sets the keyboard pan vertical step.
        /// </summary>
        /// <value> The keyboard pan vertical step. </value>
        [Category(OxyPlotCategory)]
        public double KeyboardPanVerticalStep { get; set; }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        [Category(OxyPlotCategory)]
        public PlotModel Model
        {
            get
            {
                return this.model;
            }

            set
            {
                if (this.model != value)
                {
                    this.model = value;
                    this.OnModelChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the pan cursor.
        /// </summary>
        [Category(OxyPlotCategory)]
        public Cursor PanCursor { get; set; }

        /// <summary>
        /// Gets or sets the horizontal zoom cursor.
        /// </summary>
        [Category(OxyPlotCategory)]
        public Cursor ZoomHorizontalCursor { get; set; }

        /// <summary>
        /// Gets or sets the rectangle zoom cursor.
        /// </summary>
        [Category(OxyPlotCategory)]
        public Cursor ZoomRectangleCursor { get; set; }

        /// <summary>
        /// Gets or sets vertical zoom cursor.
        /// </summary>
        [Category(OxyPlotCategory)]
        public Cursor ZoomVerticalCursor { get; set; }

        /// <summary>
        /// Get the axes from a point.
        /// </summary>
        /// <param name="pt">
        /// The point.
        /// </param>
        /// <param name="xaxis">
        /// The x axis.
        /// </param>
        /// <param name="yaxis">
        /// The y axis.
        /// </param>
        public void GetAxesFromPoint(ScreenPoint pt, out Axis xaxis, out Axis yaxis)
        {
            if (this.Model == null)
            {
                xaxis = null;
                yaxis = null;
                return;
            }

            this.Model.GetAxesFromPoint(pt, out xaxis, out yaxis);
        }

        /// <summary>
        /// Get the series from a point.
        /// </summary>
        /// <param name="pt">
        /// The point (screen coordinates).
        /// </param>
        /// <param name="limit">
        /// The limit.
        /// </param>
        /// <returns>
        /// The series.
        /// </returns>
        public Series GetSeriesFromPoint(ScreenPoint pt, double limit)
        {
            if (this.Model == null)
            {
                return null;
            }

            return this.Model.GetSeriesFromPoint(pt, limit);
        }

        /// <summary>
        /// The hide tracker.
        /// </summary>
        public void HideTracker()
        {
        }

        /// <summary>
        /// The hide zoom rectangle.
        /// </summary>
        public void HideZoomRectangle()
        {
            this.zoomRectangle = Rectangle.Empty;
            this.Invalidate();
        }

        /// <summary>
        /// The invalidate plot.
        /// </summary>
        /// <param name="updateData">
        /// The update data.
        /// </param>
        public void InvalidatePlot(bool updateData)
        {
            lock (this.invalidateLock)
            {
                this.isModelInvalidated = true;
                this.updateDataFlag = this.updateDataFlag || updateData;
            }

            this.Invalidate();
        }

        /// <summary>
        /// Called when the Model property has been changed.
        /// </summary>
        public void OnModelChanged()
        {
            lock (this.modelLock)
            {
                if (this.currentModel != null)
                {
                    this.currentModel.AttachPlotControl(null);
                }

                if (this.Model != null)
                {
                    if (this.Model.PlotControl != null)
                    {
                        throw new InvalidOperationException(
                            "This PlotModel is already in use by some other plot control.");
                    }

                    this.Model.AttachPlotControl(this);
                    this.currentModel = this.Model;
                }
            }

            this.InvalidatePlot(true);
        }

        /// <summary>
        /// The pan.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="x0">
        /// The x 0.
        /// </param>
        /// <param name="x1">
        /// The x 1.
        /// </param>
        public void Pan(Axis axis, ScreenPoint x0, ScreenPoint x1)
        {
            axis.Pan(x0, x1);
            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Pans all axes.
        /// </summary>
        /// <param name="deltax">
        /// The horizontal delta.
        /// </param>
        /// <param name="deltay">
        /// The vertical delta.
        /// </param>
        public void PanAll(double deltax, double deltay)
        {
            foreach (var a in this.ActualModel.Axes)
            {
                a.Pan(a.IsHorizontal() ? deltax : deltay);
            }

            this.InvalidatePlot(false);
        }

        /// <summary>
        /// The refresh plot.
        /// </summary>
        /// <param name="updateData">
        /// The update data.
        /// </param>
        public void RefreshPlot(bool updateData)
        {
            lock (this.invalidateLock)
            {
                this.isModelInvalidated = true;
                this.updateDataFlag = this.updateDataFlag || updateData;
            }

            this.Refresh();
        }

        /// <summary>
        /// The reset.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        public void Reset(Axis axis)
        {
            axis.Reset();
            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">
        /// The cursor type.
        /// </param>
        public void SetCursorType(CursorType cursorType)
        {
            switch (cursorType)
            {
                case CursorType.Pan:
                    this.Cursor = this.PanCursor;
                    break;
                case CursorType.ZoomRectangle:
                    this.Cursor = this.ZoomRectangleCursor;
                    break;
                case CursorType.ZoomHorizontal:
                    this.Cursor = this.ZoomHorizontalCursor;
                    break;
                case CursorType.ZoomVertical:
                    this.Cursor = this.ZoomVerticalCursor;
                    break;
                default:
                    this.Cursor = Cursors.Arrow;
                    break;
            }
        }

        /// <summary>
        /// The show tracker.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        public void ShowTracker(TrackerHitResult data)
        {
            // not implemented for WindowsForms
        }

        /// <summary>
        /// The show zoom rectangle.
        /// </summary>
        /// <param name="r">
        /// The r.
        /// </param>
        public void ShowZoomRectangle(OxyRect r)
        {
            this.zoomRectangle = new Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
            this.Invalidate();
        }

        /// <summary>
        /// The zoom.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="p1">
        /// The p 1.
        /// </param>
        /// <param name="p2">
        /// The p 2.
        /// </param>
        public void Zoom(Axis axis, double p1, double p2)
        {
            axis.Zoom(p1, p2);
            this.InvalidatePlot(false);
        }

        /// <summary>
        /// The zoom all.
        /// </summary>
        public void ZoomAll()
        {
            foreach (var a in this.Model.Axes)
            {
                a.Reset();
            }

            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Zooms all axes.
        /// </summary>
        /// <param name="delta">
        /// The delta.
        /// </param>
        public void ZoomAllAxes(double delta)
        {
            foreach (var a in this.ActualModel.Axes)
            {
                this.ZoomAt(a, delta);
            }

            this.RefreshPlot(false);
        }

        /// <summary>
        /// The zoom at.
        /// </summary>
        /// <param name="axis">
        /// The axis.
        /// </param>
        /// <param name="factor">
        /// The factor.
        /// </param>
        /// <param name="x">
        /// The x.
        /// </param>
        public void ZoomAt(Axis axis, double factor, double x = double.NaN)
        {
            if (double.IsNaN(x))
            {
                double sx = (axis.Transform(axis.ActualMaximum) + axis.Transform(axis.ActualMinimum)) * 0.5;
                x = axis.InverseTransform(sx);
            }

            axis.ZoomAt(factor, x);
            this.InvalidatePlot(false);
        }

        /// <summary>
        /// The on mouse down.
        /// </summary>
        /// <param name="e">
        /// The e.
        /// </param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (this.mouseManipulator != null)
            {
                return;
            }

            this.Focus();
            this.Capture = true;

            if (this.ActualModel != null)
            {
                var args = this.CreateMouseEventArgs(e);
                this.ActualModel.HandleMouseDown(this, args);
                if (args.Handled)
                {
                    return;
                }
            }

            this.mouseManipulator = this.GetManipulator(e);

            if (this.mouseManipulator != null)
            {
                this.mouseManipulator.Started(this.CreateManipulationEventArgs(e));
            }
        }

        /// <summary>
        /// The on mouse move.
        /// </summary>
        /// <param name="e">
        /// The e.
        /// </param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (this.ActualModel != null)
            {
                var args = this.CreateMouseEventArgs(e);
                this.ActualModel.HandleMouseMove(this, args);
                if (args.Handled)
                {
                    return;
                }
            }

            if (this.mouseManipulator != null)
            {
                this.mouseManipulator.Delta(this.CreateManipulationEventArgs(e));
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseUp"/> event.
        /// </summary>
        /// <param name="e">
        /// A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.
        /// </param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            this.Capture = false;

            if (this.ActualModel != null)
            {
                var args = this.CreateMouseEventArgs(e);
                this.ActualModel.HandleMouseUp(this, args);
                if (args.Handled)
                {
                    return;
                }
            }

            if (this.mouseManipulator != null)
            {
                this.mouseManipulator.Completed(this.CreateManipulationEventArgs(e));
            }

            this.mouseManipulator = null;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseWheel"/> event.
        /// </summary>
        /// <param name="e">
        /// A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.
        /// </param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            bool isControlDown = ModifierKeys == Keys.Control;
            var m = new ZoomStepManipulator(this, e.Delta * 0.001, isControlDown);
            m.Started(new ManipulationEventArgs(e.Location.ToScreenPoint()));
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
        /// </summary>
        /// <param name="e">
        /// A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.
        /// </param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            try
            {
                lock (this.invalidateLock)
                {
                    if (this.isModelInvalidated)
                    {
                        if (this.model != null)
                        {
                            this.model.Update(this.updateDataFlag);
                            this.updateDataFlag = false;
                        }

                        this.isModelInvalidated = false;
                    }
                }

                lock (this.renderingLock)
                {
                    this.renderContext.SetGraphicsTarget(e.Graphics);
                    if (this.model != null)
                    {
                        this.model.Render(this.renderContext, this.Width, this.Height);
                    }

                    if (this.zoomRectangle != Rectangle.Empty)
                    {
                        using (var zoomBrush = new SolidBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0x00)))
                        using (var zoomPen = new Pen(Color.Black))
                        {
                            zoomPen.DashPattern = new float[] { 3, 1 };
                            e.Graphics.FillRectangle(zoomBrush, this.zoomRectangle);
                            e.Graphics.DrawRectangle(zoomPen, this.zoomRectangle);
                        }
                    }
                }
            }
            catch (Exception paintException)
            {
                var trace = new StackTrace(paintException);
                Debug.WriteLine(paintException);
                Debug.WriteLine(trace);
                using (var font = new Font("Arial", 10))
                {
                    e.Graphics.DrawString(
                        "OxyPlot paint exception: " + paintException.Message, font, Brushes.Red, 10, 10);
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.PreviewKeyDown"/> event.
        /// </summary>
        /// <param name="e">
        /// A <see cref="T:System.Windows.Forms.PreviewKeyDownEventArgs"/> that contains the event data.
        /// </param>
        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.KeyCode == Keys.A)
            {
                this.ZoomAll();
            }

            bool control = (e.Modifiers & Keys.Control) == Keys.Control;
            bool alt = (e.Modifiers & Keys.Alt) == Keys.Alt;

            double deltax = 0;
            double deltay = 0;
            double zoom = 0;
            switch (e.KeyCode)
            {
                case Keys.Up:
                    deltay = -1;
                    break;
                case Keys.Down:
                    deltay = 1;
                    break;
                case Keys.Left:
                    deltax = -1;
                    break;
                case Keys.Right:
                    deltax = 1;
                    break;
                case Keys.Add:
                case Keys.Oemplus:
                case Keys.PageUp:
                    zoom = 1;
                    break;
                case Keys.Subtract:
                case Keys.OemMinus:
                case Keys.PageDown:
                    zoom = -1;
                    break;
            }

            if ((deltax * deltax) + (deltay * deltay) > 0)
            {
                deltax = deltax * this.ActualModel.PlotArea.Width * this.KeyboardPanHorizontalStep;
                deltay = deltay * this.ActualModel.PlotArea.Height * this.KeyboardPanVerticalStep;

                // small steps if the user is pressing control
                if (control)
                {
                    deltax *= 0.2;
                    deltay *= 0.2;
                }

                this.PanAll(deltax, deltay);

                // e.Handled = true;
            }

            if (Math.Abs(zoom) > 1e-8)
            {
                if (control)
                {
                    zoom *= 0.2;
                }

                this.ZoomAllAxes(1 + (zoom * 0.12));

                // e.Handled = true;
            }

            if (control && alt && this.ActualModel != null)
            {
                switch (e.KeyCode)
                {
                    case Keys.R:
                        this.SetClipboardText(this.ActualModel.CreateTextReport());
                        break;
                    case Keys.C:
                        this.SetClipboardText(this.ActualModel.ToCode());
                        break;
                    case Keys.X:

                        // this.SetClipboardText(this.ActualModel.ToXml());
                        break;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Resize"/> event.
        /// </summary>
        /// <param name="e">
        /// An <see cref="T:System.EventArgs"/> that contains the event data.
        /// </param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Converts the changed button.
        /// </summary>
        /// <param name="e">
        /// The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.
        /// </param>
        /// <returns>
        /// The mouse button.
        /// </returns>
        private static OxyMouseButton ConvertChangedButton(MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    return OxyMouseButton.Left;
                case MouseButtons.Middle:
                    return OxyMouseButton.Middle;
                case MouseButtons.Right:
                    return OxyMouseButton.Right;
                case MouseButtons.XButton1:
                    return OxyMouseButton.XButton1;
                case MouseButtons.XButton2:
                    return OxyMouseButton.XButton2;
            }

            return OxyMouseButton.Left;
        }

        /// <summary>
        /// Creates the mouse event arguments.
        /// </summary>
        /// <param name="e">
        /// The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.
        /// </param>
        /// <returns>
        /// Mouse event arguments.
        /// </returns>
        private OxyMouseEventArgs CreateMouseEventArgs(MouseEventArgs e)
        {
            return new OxyMouseEventArgs
            {
                ChangedButton = ConvertChangedButton(e),
                Position = new ScreenPoint(e.Location.X, e.Location.Y),
                IsShiftDown = (ModifierKeys & Keys.Shift) == Keys.Shift,
                IsControlDown = (ModifierKeys & Keys.Control) == Keys.Control,
                IsAltDown = (ModifierKeys & Keys.Alt) == Keys.Alt,
            };
        }

        /// <summary>
        /// Creates the manipulation event args.
        /// </summary>
        /// <param name="e">
        /// The MouseEventArgs instance containing the event data.
        /// </param>
        /// <returns>
        /// A manipulation event args object.
        /// </returns>
        private ManipulationEventArgs CreateManipulationEventArgs(MouseEventArgs e)
        {
            return new ManipulationEventArgs(e.Location.ToScreenPoint());
        }

        /// <summary>
        /// Gets the manipulator for the current mouse button and modifier keys.
        /// </summary>
        /// <param name="e">
        /// The event args.
        /// </param>
        /// <returns>
        /// A manipulator or null if no gesture was recognized.
        /// </returns>
        private ManipulatorBase GetManipulator(MouseEventArgs e)
        {
            bool control = (ModifierKeys & Keys.Control) == Keys.Control;
            bool shift = (ModifierKeys & Keys.Shift) == Keys.Shift;
            bool alt = (ModifierKeys & Keys.Alt) == Keys.Alt;

            bool lmb = e.Button == MouseButtons.Left;
            bool rmb = e.Button == MouseButtons.Right;
            bool mmb = e.Button == MouseButtons.Middle;
            bool xb1 = e.Button == MouseButtons.XButton1;
            bool xb2 = e.Button == MouseButtons.XButton2;

            // MMB / control RMB / control+alt LMB
            if (mmb || (control && lmb) || (control && alt && rmb))
            {
                return new ZoomRectangleManipulator(this);
            }

            // Right mouse button / alt+left mouse button
            if (lmb || (rmb && alt))
            {
                if (e.Clicks == 2)
                {
                    return new ResetManipulator(this);
                }
                return new PanManipulator(this);
            }

            // Left mouse button
            if (rmb)
            {
                return new TrackerManipulator(this) { Snap = !control, PointsOnly = shift };
            }

            // XButtons are zoom-stepping
            if (xb1 || xb2)
            {
                double d = xb1 ? 0.05 : -0.05;
                return new ZoomStepManipulator(this, d, control);
            }

            return null;
        }

        /// <summary>
        /// The set clipboard text.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        private void SetClipboardText(string text)
        {
            try
            {
                // todo: can't get the following solution to work
                // http://stackoverflow.com/questions/5707990/requested-clipboard-operation-did-not-succeed
                Clipboard.SetText(text);
            }
            catch (ExternalException ee)
            {
                // Requested Clipboard operation did not succeed.
                MessageBox.Show(this, ee.Message, "OxyPlot");
            }
        }
    }
}