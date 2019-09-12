// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenHardwareMonitor.GUI
{
    public abstract class Gadget : IDisposable
    {
        private GadgetWindow window;

        public Gadget()
        {
            this.window = new GadgetWindow();
            this.window.Paint += delegate (object sender, PaintEventArgs e)
            {
                OnPaint(e);
            };
        }

        public virtual void Dispose()
        {
            window.Dispose();
        }

        public Point Location
        {
            get
            {
                return window.Location;
            }
            set
            {
                window.Location = value;
            }
        }

        public event EventHandler LocationChanged
        {
            add
            {
                window.LocationChanged += value;
            }
            remove
            {
                window.LocationChanged -= value;
            }
        }

        public virtual Size Size
        {
            get
            {
                return window.Size;
            }
            set
            {
                this.window.Size = value;
            }
        }

        public event EventHandler SizeChanged
        {
            add
            {
                window.SizeChanged += value;
            }
            remove
            {
                window.SizeChanged -= value;
            }
        }

        public byte Opacity
        {
            get
            {
                return window.Opacity;
            }
            set
            {
                window.Opacity = value;
            }
        }

        public bool LockPositionAndSize
        {
            get
            {
                return window.LockPositionAndSize;
            }
            set
            {
                window.LockPositionAndSize = value;
            }
        }

        public bool AlwaysOnTop
        {
            get
            {
                return window.AlwaysOnTop;
            }
            set
            {
                window.AlwaysOnTop = value;
            }
        }

        public ContextMenu ContextMenu
        {
            get
            {
                return window.ContextMenu;
            }
            set
            {
                window.ContextMenu = value;
            }
        }

        public event HitTestEventHandler HitTest
        {
            add
            {
                window.HitTest += value;
            }
            remove
            {
                window.HitTest -= value;
            }
        }

        public event MouseEventHandler MouseDoubleClick
        {
            add
            {
                window.MouseDoubleClick += value;
            }
            remove
            {
                window.MouseDoubleClick -= value;
            }
        }

        public bool Visible
        {
            get
            {
                return window.Visible;
            }
            set
            {
                if (value != window.Visible)
                {
                    window.Visible = value;
                    if (VisibleChanged != null)
                        VisibleChanged(this, EventArgs.Empty);
                    if (value)
                        Redraw();
                }
            }
        }

        public event EventHandler VisibleChanged;

        public void Redraw()
        {
            window.Redraw();
        }

        protected abstract void OnPaint(PaintEventArgs e);

    }
}
