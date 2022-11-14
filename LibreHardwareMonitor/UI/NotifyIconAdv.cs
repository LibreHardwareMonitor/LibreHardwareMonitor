// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI;

public class NotifyIconAdv : IDisposable
{
    private readonly NotifyIcon _genericNotifyIcon;
    private readonly NotifyIconWindowsImplementation _windowsNotifyIcon;

    public NotifyIconAdv()
    {
        if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
            _genericNotifyIcon = new NotifyIcon();
        else
            _windowsNotifyIcon = new NotifyIconWindowsImplementation();
    }

    public event EventHandler BalloonTipClicked
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.BalloonTipClicked += value;
            else
                _windowsNotifyIcon.BalloonTipClicked += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.BalloonTipClicked -= value;
            else
                _windowsNotifyIcon.BalloonTipClicked -= value;
        }
    }

    public event EventHandler BalloonTipClosed
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.BalloonTipClosed += value;
            else
                _windowsNotifyIcon.BalloonTipClosed += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.BalloonTipClosed -= value;
            else
                _windowsNotifyIcon.BalloonTipClosed -= value;
        }
    }

    public event EventHandler BalloonTipShown
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.BalloonTipShown += value;
            else
                _windowsNotifyIcon.BalloonTipShown += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.BalloonTipShown -= value;
            else
                _windowsNotifyIcon.BalloonTipShown -= value;
        }
    }

    public event EventHandler Click
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.Click += value;
            else
                _windowsNotifyIcon.Click += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.Click -= value;
            else
                _windowsNotifyIcon.Click -= value;
        }
    }

    public event EventHandler DoubleClick
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.DoubleClick += value;
            else
                _windowsNotifyIcon.DoubleClick += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.DoubleClick -= value;
            else
                _windowsNotifyIcon.DoubleClick -= value;
        }
    }

    public event MouseEventHandler MouseClick
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseClick += value;
            else
                _windowsNotifyIcon.MouseClick += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseClick -= value;
            else
                _windowsNotifyIcon.MouseClick -= value;
        }
    }

    public event MouseEventHandler MouseDoubleClick
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseDoubleClick += value;
            else
                _windowsNotifyIcon.MouseDoubleClick += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseDoubleClick -= value;
            else
                _windowsNotifyIcon.MouseDoubleClick -= value;
        }
    }

    public event MouseEventHandler MouseDown
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseDown += value;
            else
                _windowsNotifyIcon.MouseDown += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseDown -= value;
            else
                _windowsNotifyIcon.MouseDown -= value;
        }
    }

    public event MouseEventHandler MouseMove
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseMove += value;
            else
                _windowsNotifyIcon.MouseMove += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseMove -= value;
            else
                _windowsNotifyIcon.MouseMove -= value;
        }
    }

    public event MouseEventHandler MouseUp
    {
        add
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseUp += value;
            else
                _windowsNotifyIcon.MouseUp += value;
        }
        remove
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.MouseUp -= value;
            else
                _windowsNotifyIcon.MouseUp -= value;
        }
    }

    public string BalloonTipText
    {
        get
        {
            if (_genericNotifyIcon != null)
                return _genericNotifyIcon.BalloonTipText;


            return _windowsNotifyIcon.BalloonTipText;
        }
        set
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.BalloonTipText = value;
            else
                _windowsNotifyIcon.BalloonTipText = value;
        }
    }

    public ToolTipIcon BalloonTipIcon
    {
        get
        {
            if (_genericNotifyIcon != null)
                return _genericNotifyIcon.BalloonTipIcon;


            return _windowsNotifyIcon.BalloonTipIcon;
        }
        set
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.BalloonTipIcon = value;
            else
                _windowsNotifyIcon.BalloonTipIcon = value;
        }
    }

    public string BalloonTipTitle
    {
        get
        {
            if (_genericNotifyIcon != null)
                return _genericNotifyIcon.BalloonTipTitle;


            return _windowsNotifyIcon.BalloonTipTitle;
        }
        set
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.BalloonTipTitle = value;
            else
                _windowsNotifyIcon.BalloonTipTitle = value;
        }
    }

    public ContextMenuStrip ContextMenuStrip
    {
        get
        {
            if (_genericNotifyIcon != null)
                return _genericNotifyIcon.ContextMenuStrip;


            return _windowsNotifyIcon.ContextMenuStrip;
        }
        set
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.ContextMenuStrip = value;
            else
                _windowsNotifyIcon.ContextMenuStrip = value;
        }
    }

    public object Tag { get; set; }

    public Icon Icon
    {
        get
        {
            if (_genericNotifyIcon != null)
                return _genericNotifyIcon.Icon;


            return _windowsNotifyIcon.Icon;
        }
        set
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.Icon = value;
            else
                _windowsNotifyIcon.Icon = value;
        }
    }

    public string Text
    {
        get
        {
            if (_genericNotifyIcon != null)
                return _genericNotifyIcon.Text;


            return _windowsNotifyIcon.Text;
        }
        set
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.Text = value;
            else
                _windowsNotifyIcon.Text = value;
        }
    }

    public bool Visible
    {
        get
        {
            if (_genericNotifyIcon != null)
                return _genericNotifyIcon.Visible;


            return _windowsNotifyIcon.Visible;
        }
        set
        {
            if (_genericNotifyIcon != null)
                _genericNotifyIcon.Visible = value;
            else
                _windowsNotifyIcon.Visible = value;
        }
    }

    public void Dispose()
    {
        if (_genericNotifyIcon != null)
            _genericNotifyIcon.Dispose();
        else
            _windowsNotifyIcon.Dispose();
    }

    public void ShowBalloonTip(int timeout)
    {
        ShowBalloonTip(timeout, BalloonTipTitle, BalloonTipText, BalloonTipIcon);
    }

    public void ShowBalloonTip(int timeout, string tipTitle, string tipText,
        ToolTipIcon tipIcon)
    {
        if (_genericNotifyIcon != null)
            _genericNotifyIcon.ShowBalloonTip(timeout, tipTitle, tipText, tipIcon);
        else
            _windowsNotifyIcon.ShowBalloonTip(timeout, tipTitle, tipText, tipIcon);
    }

    private class NotifyIconWindowsImplementation : Component
    {

        private static int _nextId;
        private readonly object _syncObj = new object();
        private Icon _icon;
        private string _text = "";
        private readonly int _id;
        private bool _created;
        private NotifyIconNativeWindow _window;
        private bool _doubleClickDown;
        private bool _visible;
        private readonly MethodInfo _commandDispatch;

        public event EventHandler BalloonTipClicked;
        public event EventHandler BalloonTipClosed;
        public event EventHandler BalloonTipShown;
        public event EventHandler Click;
        public event EventHandler DoubleClick;
        public event MouseEventHandler MouseClick;
        public event MouseEventHandler MouseDoubleClick;
        public event MouseEventHandler MouseDown;
        public event MouseEventHandler MouseMove;
        public event MouseEventHandler MouseUp;

        public string BalloonTipText { get; set; }
        public ToolTipIcon BalloonTipIcon { get; set; }
        public string BalloonTipTitle { get; set; }
        public ContextMenuStrip ContextMenuStrip { get; set; }

        public Icon Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    UpdateNotifyIcon(_visible);
                }
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                if (value == null)
                    value = "";

                if (value.Length > 63)
                    throw new ArgumentOutOfRangeException();

                if (!value.Equals(_text))
                {
                    _text = value;
                    if (_visible)
                        UpdateNotifyIcon(_visible);
                }
            }
        }

        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    UpdateNotifyIcon(_visible);
                }
            }
        }

        public NotifyIconWindowsImplementation()
        {
            BalloonTipText = "";
            BalloonTipTitle = "";

            _commandDispatch = typeof(Form).Assembly.
                                            GetType("System.Windows.Forms.Command").GetMethod("DispatchID",
                                                                                              BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                                                                                              null, new[] { typeof(int) }, null);

            _id = ++_nextId;
            _window = new NotifyIconNativeWindow(this);
            UpdateNotifyIcon(_visible);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_window != null)
                {
                    _icon = null;
                    _text = "";
                    UpdateNotifyIcon(false);
                    _window.DestroyHandle();
                    _window = null;
                    ContextMenuStrip = null;
                }
            }
            else
            {
                if (_window != null && _window.Handle != IntPtr.Zero)
                {
                    NativeMethods.PostMessage(new HandleRef(_window, _window.Handle), WM_CLOSE, 0, 0);
                    _window.ReleaseHandle();
                }
            }
            base.Dispose(disposing);
        }

        public void ShowBalloonTip(int timeout)
        {
            ShowBalloonTip(timeout, BalloonTipTitle, BalloonTipText, BalloonTipIcon);
        }

        public void ShowBalloonTip(int timeout, string tipTitle, string tipText, ToolTipIcon tipIcon)
        {
            if (timeout < 0)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            if (string.IsNullOrEmpty(tipText))
                throw new ArgumentException("tipText");

            if (DesignMode)
                return;

            if (_created)
            {
                NativeMethods.NotifyIconData data = new NativeMethods.NotifyIconData();
                if (_window.Handle == IntPtr.Zero)
                    _window.CreateHandle(new CreateParams());

                data.Window = _window.Handle;
                data.ID = _id;
                data.Flags = NativeMethods.NotifyIconDataFlags.Info;
                data.TimeoutOrVersion = timeout;
                data.InfoTitle = tipTitle;
                data.Info = tipText;
                data.InfoFlags = (int)tipIcon;

                NativeMethods.Shell_NotifyIcon(NativeMethods.NotifyIconMessage.Modify, data);
            }
        }

        private void ShowContextMenu()
        {
            if (ContextMenuStrip == null)
                return;

            NativeMethods.Point p = new NativeMethods.Point();
            NativeMethods.GetCursorPos(ref p);
            NativeMethods.SetForegroundWindow(new HandleRef(_window, _window.Handle));

            ContextMenuStrip?.GetType().InvokeMember("ShowInTaskbar",
                                                     BindingFlags.NonPublic | BindingFlags.InvokeMethod |
                                                     BindingFlags.Instance, null, ContextMenuStrip, new object[] { p.X, p.Y });
        }

        private void UpdateNotifyIcon(bool showNotifyIcon)
        {
            if (DesignMode)
                return;

            lock (_syncObj)
            {
                _window.LockReference(showNotifyIcon);

                NativeMethods.NotifyIconData data = new NativeMethods.NotifyIconData { CallbackMessage = WM_TRAYMOUSEMESSAGE, Flags = NativeMethods.NotifyIconDataFlags.Message };

                if (showNotifyIcon && _window.Handle == IntPtr.Zero)
                    _window.CreateHandle(new CreateParams());

                data.Window = _window.Handle;
                data.ID = _id;

                if (_icon != null)
                {
                    data.Flags |= NativeMethods.NotifyIconDataFlags.Icon;
                    data.Icon = _icon.Handle;
                }

                data.Flags |= NativeMethods.NotifyIconDataFlags.Tip;
                data.Tip = _text;

                if (showNotifyIcon && _icon != null)
                {
                    if (!_created)
                    {
                        if (NativeMethods.Shell_NotifyIcon(NativeMethods.NotifyIconMessage.Modify, data))
                        {
                            _created = true;
                        }
                        else
                        {
                            int i = 0;
                            do
                            {
                                _created = NativeMethods.Shell_NotifyIcon(NativeMethods.NotifyIconMessage.Add, data);
                                if (!_created)
                                {
                                    System.Threading.Thread.Sleep(200);
                                    i++;
                                }
                            } while (!_created && i < 40);
                        }
                    }
                    else
                    {
                        NativeMethods.Shell_NotifyIcon(NativeMethods.NotifyIconMessage.Modify, data);
                    }
                }
                else
                {
                    if (_created)
                    {
                        int i = 0;
                        bool deleted;
                        do
                        {
                            deleted = NativeMethods.Shell_NotifyIcon(NativeMethods.NotifyIconMessage.Delete, data);
                            if (!deleted)
                            {
                                System.Threading.Thread.Sleep(200);
                                i++;
                            }
                        } while (!deleted && i < 40);
                        _created = false;
                    }
                }
            }
        }

        private void ProcessMouseDown(MouseButtons button, bool doubleClick)
        {
            if (doubleClick)
            {
                DoubleClick?.Invoke(this, new MouseEventArgs(button, 2, 0, 0, 0));
                MouseDoubleClick?.Invoke(this, new MouseEventArgs(button, 2, 0, 0, 0));

                _doubleClickDown = true;
            }

            MouseDown?.Invoke(this, new MouseEventArgs(button, doubleClick ? 2 : 1, 0, 0, 0));
        }

        private void ProcessMouseUp(MouseButtons button)
        {
            MouseUp?.Invoke(this, new MouseEventArgs(button, 0, 0, 0, 0));

            if (!_doubleClickDown)
            {
                Click?.Invoke(this, new MouseEventArgs(button, 0, 0, 0, 0));
                MouseClick?.Invoke(this, new MouseEventArgs(button, 0, 0, 0, 0));
            }
            _doubleClickDown = false;
        }

        private void ProcessInitMenuPopup(ref Message message)
        {
            _window.DefWndProc(ref message);
        }

        private void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                case WM_DESTROY:
                    UpdateNotifyIcon(false);
                    return;
                case WM_COMMAND:
                    if (message.LParam != IntPtr.Zero)
                    {
                        _window.DefWndProc(ref message);
                        return;
                    }
                    _commandDispatch.Invoke(null, new object[] { message.WParam.ToInt32() & 0xFFFF });
                    return;
                case WM_INITMENUPOPUP:
                    ProcessInitMenuPopup(ref message);
                    return;
                case WM_TRAYMOUSEMESSAGE:
                    switch ((int)message.LParam)
                    {
                        case WM_MOUSEMOVE:
                            MouseMove?.Invoke(this,
                                              new MouseEventArgs(Control.MouseButtons, 0, 0, 0, 0));
                            return;
                        case WM_LBUTTONDOWN:
                            ProcessMouseDown(MouseButtons.Left, false);
                            return;
                        case WM_LBUTTONUP:
                            ProcessMouseUp(MouseButtons.Left);
                            return;
                        case WM_LBUTTONDBLCLK:
                            ProcessMouseDown(MouseButtons.Left, true);
                            return;
                        case WM_RBUTTONDOWN:
                            ProcessMouseDown(MouseButtons.Right, false);
                            return;
                        case WM_RBUTTONUP:
                            if (ContextMenuStrip != null)
                                ShowContextMenu();
                            ProcessMouseUp(MouseButtons.Right);
                            return;
                        case WM_RBUTTONDBLCLK:
                            ProcessMouseDown(MouseButtons.Right, true);
                            return;
                        case WM_MBUTTONDOWN:
                            ProcessMouseDown(MouseButtons.Middle, false);
                            return;
                        case WM_MBUTTONUP:
                            ProcessMouseUp(MouseButtons.Middle);
                            return;
                        case WM_MBUTTONDBLCLK:
                            ProcessMouseDown(MouseButtons.Middle, true);
                            return;
                        case NIN_BALLOONSHOW:
                            BalloonTipShown?.Invoke(this, EventArgs.Empty);
                            return;
                        case NIN_BALLOONHIDE:
                        case NIN_BALLOONTIMEOUT:
                            BalloonTipClosed?.Invoke(this, EventArgs.Empty);
                            return;
                        case NIN_BALLOONUSERCLICK:
                            BalloonTipClicked?.Invoke(this, EventArgs.Empty);
                            return;
                        default:
                            return;
                    }
            }

            if (message.Msg == _wmTaskBarCreated)
            {
                lock (_syncObj)
                {
                    _created = false;
                }
                UpdateNotifyIcon(_visible);
            }

            _window.DefWndProc(ref message);
        }

        private class NotifyIconNativeWindow : NativeWindow
        {
            private readonly NotifyIconWindowsImplementation _reference;
            private GCHandle _referenceHandle;

            internal NotifyIconNativeWindow(NotifyIconWindowsImplementation component)
            {
                _reference = component;
            }

            ~NotifyIconNativeWindow()
            {
                if (Handle != IntPtr.Zero)
                    NativeMethods.PostMessage(new HandleRef(this, Handle), WM_CLOSE, 0, 0);
            }

            public void LockReference(bool locked)
            {
                if (locked)
                {
                    if (!_referenceHandle.IsAllocated)
                    {
                        _referenceHandle = GCHandle.Alloc(_reference, GCHandleType.Normal);
                    }
                }
                else
                {
                    if (_referenceHandle.IsAllocated)
                        _referenceHandle.Free();
                }
            }

            protected override void OnThreadException(Exception e)
            {
                Application.OnThreadException(e);
            }

            protected override void WndProc(ref Message m)
            {
                _reference.WndProc(ref m);
            }
        }

        private const int WM_NULL = 0x00;
        private const int WM_DESTROY = 0x02;
        private const int WM_CLOSE = 0x10;
        private const int WM_COMMAND = 0x111;
        private const int WM_INITMENUPOPUP = 0x117;
        private const int WM_MOUSEMOVE = 0x200;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_LBUTTONDBLCLK = 0x203;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_RBUTTONUP = 0x205;
        private const int WM_RBUTTONDBLCLK = 0x206;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_MBUTTONUP = 0x208;
        private const int WM_MBUTTONDBLCLK = 0x209;
        private const int WM_TRAYMOUSEMESSAGE = 0x800;

        private const int NIN_BALLOONSHOW = 0x402;
        private const int NIN_BALLOONHIDE = 0x403;
        private const int NIN_BALLOONTIMEOUT = 0x404;
        private const int NIN_BALLOONUSERCLICK = 0x405;

        private static readonly int _wmTaskBarCreated = NativeMethods.RegisterWindowMessage("TaskbarCreated");

        private static class NativeMethods
        {
            private const string DllNameUser32 = "user32.dll";
            private const string DllNameShell32 = "shell32.dll";

            [DllImport(DllNameUser32, CharSet = CharSet.Auto)]
            public static extern IntPtr PostMessage(HandleRef hwnd, int msg, int wparam, int lparam);

            [DllImport(DllNameUser32, CharSet = CharSet.Auto)]
            public static extern int RegisterWindowMessage(string msg);

            [Flags]
            public enum NotifyIconDataFlags : int
            {
                Message = 0x1,
                Icon = 0x2,
                Tip = 0x4,
                State = 0x8,
                Info = 0x10
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public class NotifyIconData
            {
                private int _size = Marshal.SizeOf(typeof(NotifyIconData));
                public IntPtr Window;
                public int ID;
                public NotifyIconDataFlags Flags;
                public int CallbackMessage;
                public IntPtr Icon;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string Tip;
                public int State;
                public int StateMask;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
                public string Info;
                public int TimeoutOrVersion;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
                public string InfoTitle;
                public int InfoFlags;
            }

            public enum NotifyIconMessage : int
            {
                Add = 0x0,
                Modify = 0x1,
                Delete = 0x2
            }

            [DllImport(DllNameShell32, CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool Shell_NotifyIcon(NotifyIconMessage message, NotifyIconData pnid);

            [DllImport(DllNameUser32, CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern bool TrackPopupMenuEx(HandleRef hmenu, int fuFlags, int x, int y, HandleRef hwnd, IntPtr tpm);

            [StructLayout(LayoutKind.Sequential)]
            public struct Point
            {
                public readonly int X;
                public readonly int Y;
            }

            [DllImport(DllNameUser32, CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern bool GetCursorPos(ref Point point);

            [DllImport(DllNameUser32, CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern bool SetForegroundWindow(HandleRef hWnd);
        }
    }
}