// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotControl.cs" company="OxyPlot">
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
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using OxyPlot;

namespace Oxyplot.WindowsForms
{
    public class PlotControl : Control, IPlotControl
    {
        public List<MouseAction> MouseActions { get; private set; }

        private readonly PanAction panAction;
        private readonly SliderAction sliderAction;
        private readonly ZoomAction zoomAction;
        private Rectangle zoomRectangle;

        public PlotControl()
        {
            //    InitializeComponent();
            DoubleBuffered = true;
            Model = new PlotModel();

            panAction = new PanAction(this);
            zoomAction = new ZoomAction(this);
            sliderAction = new SliderAction(this);

            MouseActions = new List<MouseAction>();
            MouseActions.Add(panAction);
            MouseActions.Add(zoomAction);
            MouseActions.Add(sliderAction);
        }

        private PlotModel model;

        [Browsable(false), DefaultValue(null)]
        public PlotModel Model
        {
            get { return model; }
            set
            {
                model = value;
                Refresh();
            }
        }

        public override void Refresh()
        {
            if (model != null)
                model.UpdateData();
            base.Refresh();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var rc = new GraphicsRenderContext(this, e.Graphics, e.ClipRectangle);
            if (model != null)
                model.Render(rc);
            if (zoomRectangle != Rectangle.Empty)
            {
                using (var zoomBrush = new SolidBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0x00)))
                using (var zoomPen = new Pen(Color.Black))
                {
                    zoomPen.DashPattern = new float[] { 3, 1 };
                    e.Graphics.FillRectangle(zoomBrush, zoomRectangle);
                    e.Graphics.DrawRectangle(zoomPen, zoomRectangle);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.A)
            {
                ZoomAll();
            }
        }

        public void ZoomAll()
        {
            foreach (var a in Model.Axes)
                a.Reset();
            Refresh();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            Focus();
            Capture = true;

            bool control = Control.ModifierKeys == Keys.Control;
            bool shift = Control.ModifierKeys == Keys.Shift;

            var button = OxyMouseButton.Left;
            if (e.Button == MouseButtons.Middle)
                button = OxyMouseButton.Middle;
            if (e.Button == MouseButtons.Right)
                button = OxyMouseButton.Right;
            if (e.Button == MouseButtons.XButton1)
                button = OxyMouseButton.XButton1;
            if (e.Button == MouseButtons.XButton2)
                button = OxyMouseButton.XButton2;

            var p = new ScreenPoint(e.X, e.Y);
            foreach (var a in MouseActions)
                a.OnMouseDown(p, button, e.Clicks, control, shift);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool control = Control.ModifierKeys == Keys.Control;
            bool shift = Control.ModifierKeys == Keys.Shift;
            var p = new ScreenPoint(e.X, e.Y);
            foreach (var a in MouseActions)
                a.OnMouseMove(p, control, shift);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            foreach (var a in MouseActions)
                a.OnMouseUp();
            Capture = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            bool control = Control.ModifierKeys == Keys.Control;
            bool shift = Control.ModifierKeys == Keys.Shift;
            var p = new ScreenPoint(e.X, e.Y);
            foreach (var a in MouseActions)
                a.OnMouseWheel(p, e.Delta, control, shift);
        }

        public void GetAxesFromPoint(ScreenPoint pt, out AxisBase xaxis, out AxisBase yaxis)
        {
            Model.GetAxesFromPoint(pt, out xaxis, out yaxis);
        }

        public DataSeries GetSeriesFromPoint(ScreenPoint pt, double limit)
        {
            return Model.GetSeriesFromPoint(pt, limit);
        }

        public void Refresh(bool refreshData)
        {
            if (refreshData)
                Model.UpdateData();
            Invalidate();
        }

        public void Pan(AxisBase axis, double dx)
        {
            axis.Pan(dx);
        }

        public void Reset(AxisBase axis)
        {
            axis.Reset();
        }

        public void Zoom(AxisBase axis, double p1, double p2)
        {
            axis.Zoom(p1, p2);
        }

        public void ZoomAt(AxisBase axis, double factor, double x)
        {
            axis.ZoomAt(factor, x);
        }

        public OxyRect GetPlotArea()
        {
            return Model.PlotArea;
        }

        public void ShowSlider(DataSeries s, DataPoint dp)
        {
        }

        public void HideSlider()
        {
        }

        public void ShowZoomRectangle(OxyRect r)
        {
            zoomRectangle = new Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
            Invalidate();
        }

        public void HideZoomRectangle()
        {
            zoomRectangle = Rectangle.Empty;
            Invalidate();
        }
    }
}