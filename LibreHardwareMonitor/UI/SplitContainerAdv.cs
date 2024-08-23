// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Drawing;
using System.Windows.Forms;
using LibreHardwareMonitor.UI.Themes;

namespace LibreHardwareMonitor.UI;

public class SplitContainerAdv : SplitContainer
{
    private int _delta;
    private bool _mouseOver;

    public SplitContainerAdv()
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

        using (SolidBrush brush = new SolidBrush(_mouseOver ? Theme.Current.SplitterHoverColor : Theme.Current.SplitterColor))
            g.FillRectangle(brush, r);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsSplitterFixed)
        {
            switch (e.KeyData)
            {
                case Keys.Right:
                case Keys.Down:
                    SplitterDistance += SplitterIncrement;
                    break;
                case Keys.Left:
                case Keys.Up:
                    SplitterDistance -= SplitterIncrement;
                    break;
            }

            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (Orientation == Orientation.Vertical)
        {
            _delta = SplitterDistance - e.X;
            Cursor.Current = Cursors.VSplit;
        }
        else
        {
            _delta = SplitterDistance - e.Y;
            Cursor.Current = Cursors.HSplit;
        }
        IsSplitterFixed = true;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _mouseOver = true;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (IsSplitterFixed)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Orientation == Orientation.Vertical)
                {
                    if (e.X > 0 && e.X < Width)
                        SplitterDistance = e.X + _delta < 0 ? 0 : e.X + _delta;
                }
                else
                {
                    if (e.Y > 0 && e.Y < Height)
                        SplitterDistance = e.Y + _delta < 0 ? 0 : e.Y + _delta;
                }
            }
            else
            {
                IsSplitterFixed = false;
            }
            Invalidate();
        }
        else
        {
            if (SplitterRectangle.Contains(e.Location))
                Cursor = Orientation == Orientation.Vertical ? Cursors.VSplit : Cursors.HSplit;
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Cursor = Cursors.Default;
        _mouseOver = false;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _delta = 0;
        IsSplitterFixed = false;
        Cursor.Current = Cursors.Default;
    }
}
