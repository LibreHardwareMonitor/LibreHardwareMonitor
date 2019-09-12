// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OpenHardwareMonitor.GUI
{
    public class SplitContainerAdv : SplitContainer
    {
        private int delta = 0;
        private Border3DStyle border3DStyle = Border3DStyle.Raised;
        private Color color = SystemColors.Control;

        public SplitContainerAdv()
          : base()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ContainerControl, true);
            UpdateStyles();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            Rectangle r = SplitterRectangle;
            using (SolidBrush brush = new SolidBrush(color))
                g.FillRectangle(brush, r);
            ControlPaint.DrawBorder3D(g, r, border3DStyle);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!base.IsSplitterFixed)
            {
                if (e.KeyData == Keys.Right || e.KeyData == Keys.Down)
                {
                    SplitterDistance += SplitterIncrement;
                }
                else if (e.KeyData == Keys.Left || e.KeyData == Keys.Up)
                {
                    SplitterDistance -= SplitterIncrement;
                }
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Orientation == Orientation.Vertical)
            {
                delta = this.SplitterDistance - e.X;
                Cursor.Current = Cursors.VSplit;
            }
            else
            {
                delta = this.SplitterDistance - e.Y;
                Cursor.Current = Cursors.HSplit;
            }
            base.IsSplitterFixed = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (base.IsSplitterFixed)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (Orientation == Orientation.Vertical)
                    {
                        if (e.X > 0 && e.X < Width)
                        {
                            SplitterDistance = e.X + delta < 0 ? 0 : e.X + delta;
                        }
                    }
                    else
                    {
                        if (e.Y > 0 && e.Y < Height)
                        {
                            SplitterDistance = e.Y + delta < 0 ? 0 : e.Y + delta;
                        }
                    }
                }
                else
                {
                    base.IsSplitterFixed = false;
                }
                Invalidate();
            }
            else
            {
                if (SplitterRectangle.Contains(e.Location))
                {
                    Cursor = Orientation == Orientation.Vertical ?
                      Cursors.VSplit : Cursors.HSplit;
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            delta = 0;
            base.IsSplitterFixed = false;
            Cursor.Current = Cursors.Default;
        }

        public Border3DStyle Border3DStyle
        {
            get { return border3DStyle; }
            set
            {
                border3DStyle = value;
                Invalidate(false);
            }
        }

        public Color Color
        {
            get { return color; }
            set
            {
                color = value;
                Invalidate(false);
            }
        }

        public new bool IsSplitterFixed
        {
            get
            {
                return false;
            }
        }

    }
}
