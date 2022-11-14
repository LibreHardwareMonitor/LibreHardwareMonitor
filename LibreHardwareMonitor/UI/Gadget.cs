// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI;

public abstract class Gadget : IDisposable
{
    private readonly GadgetWindow _window;

    public event EventHandler VisibleChanged;

    protected Gadget()
    {
        _window = new GadgetWindow();
        _window.Paint += delegate (object sender, PaintEventArgs e)
        {
            OnPaint(e);
        };
    }

    public virtual void Dispose()
    {
        _window.Dispose();
    }

    public Point Location
    {
        get
        {
            return _window.Location;
        }
        set
        {
            _window.Location = value;
        }
    }

    public event EventHandler LocationChanged
    {
        add
        {
            _window.LocationChanged += value;
        }
        remove
        {
            _window.LocationChanged -= value;
        }
    }

    public virtual Size Size
    {
        get
        {
            return _window.Size;
        }
        set
        {
            _window.Size = value;
        }
    }

    public event EventHandler SizeChanged
    {
        add
        {
            _window.SizeChanged += value;
        }
        remove
        {
            _window.SizeChanged -= value;
        }
    }

    public byte Opacity
    {
        get
        {
            return _window.Opacity;
        }
        set
        {
            _window.Opacity = value;
        }
    }

    public bool LockPositionAndSize
    {
        get
        {
            return _window.LockPositionAndSize;
        }
        set
        {
            _window.LockPositionAndSize = value;
        }
    }

    public bool AlwaysOnTop
    {
        get
        {
            return _window.AlwaysOnTop;
        }
        set
        {
            _window.AlwaysOnTop = value;
        }
    }

    public ContextMenuStrip ContextMenuStrip
    {
        get
        {
            return _window.ContextMenuStrip;
        }
        set
        {
            _window.ContextMenuStrip = value;
        }
    }

    public event HitTestEventHandler HitTest
    {
        add
        {
            _window.HitTest += value;
        }
        remove
        {
            _window.HitTest -= value;
        }
    }

    public event MouseEventHandler MouseDoubleClick
    {
        add
        {
            _window.MouseDoubleClick += value;
        }
        remove
        {
            _window.MouseDoubleClick -= value;
        }
    }

    public bool Visible
    {
        get
        {
            return _window.Visible;
        }
        set
        {
            if (value != _window.Visible)
            {
                _window.Visible = value;
                VisibleChanged?.Invoke(this, EventArgs.Empty);

                if (value)
                    Redraw();
            }
        }
    }

    public void Redraw()
    {
        _window.Redraw();
    }

    protected abstract void OnPaint(PaintEventArgs e);
}