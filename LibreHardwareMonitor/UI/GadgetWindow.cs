// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI;

public sealed class GadgetWindow : NativeWindow, IDisposable
{
    private bool _visible;
    private bool _alwaysOnTop;
    private byte _opacity = 255;
    private Point _location = new Point(100, 100);
    private Size _size = new Size(130, 84);
    private readonly MethodInfo _commandDispatch;
    private IntPtr _handleBitmapDC;
    private Size _bufferSize;
    private Graphics _graphics;

    public event EventHandler SizeChanged;
    public event EventHandler LocationChanged;
    public event HitTestEventHandler HitTest;
    public event MouseEventHandler MouseDoubleClick;

    public GadgetWindow()
    {
        Type commandType = typeof(Form).Assembly.GetType("System.Windows.Forms.Command");
        _commandDispatch = commandType.GetMethod("DispatchID",
                                                 BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                                                 null, new[] { typeof(int) }, null);

        CreateHandle(CreateParams);

        // move window to the bottom
        MoveToBottom(Handle);

        // prevent window from fading to a glass sheet when peek is invoked
        try
        {
            bool value = true;
            NativeMethods.DwmSetWindowAttribute(Handle, WindowAttribute.DWMWA_EXCLUDED_FROM_PEEK, ref value, Marshal.SizeOf(true));
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }

        CreateBuffer();
    }

    private void ShowDesktopChanged(bool showDesktop)
    {
        if (showDesktop)
            MoveToTopMost(Handle);
        else
            MoveToBottom(Handle);
    }

    private void MoveToBottom(IntPtr handle)
    {
        NativeMethods.SetWindowPos(handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOSENDCHANGING);
    }

    private void MoveToTopMost(IntPtr handle)
    {
        NativeMethods.SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOSENDCHANGING);
    }

    private CreateParams CreateParams
    {
        get
        {
            CreateParams cp = new CreateParams
            {
                Width = 4096,
                Height = 4096,
                X = _location.X,
                Y = _location.Y,
                ExStyle = WS_EX_LAYERED | WS_EX_TOOLWINDOW
            };

            return cp;
        }
    }

    protected override void WndProc(ref Message message)
    {
        switch (message.Msg)
        {
            case WM_COMMAND:
                {
                    // need to dispatch the message for the context menu
                    if (message.LParam == IntPtr.Zero)
                        _commandDispatch.Invoke(null, new object[] {message.WParam.ToInt32() & 0xFFFF });
                }
                break;
            case WM_NCHITTEST:
                {
                    message.Result = (IntPtr)HitResult.Caption;
                    if (HitTest != null)
                    {
                        Point p = new Point(
                                            Macros.GET_X_LPARAM(message.LParam) - _location.X,
                                            Macros.GET_Y_LPARAM(message.LParam) - _location.Y
                                           );
                        HitTestEventArgs e = new HitTestEventArgs(p, HitResult.Caption);
                        HitTest(this, e);
                        message.Result = (IntPtr)e.HitResult;
                    }
                }
                break;
            case WM_NCLBUTTONDBLCLK:
                {
                    MouseDoubleClick?.Invoke(this, new MouseEventArgs(MouseButtons.Left, 2, Macros.GET_X_LPARAM(message.LParam) - _location.X, Macros.GET_Y_LPARAM(message.LParam) - _location.Y, 0));
                    message.Result = IntPtr.Zero;
                }
                break;
            case WM_NCRBUTTONDOWN:
                {
                    message.Result = IntPtr.Zero;
                }
                break;
            case WM_NCRBUTTONUP:
                {
                    ContextMenuStrip?.Show(new Point(Macros.GET_X_LPARAM(message.LParam), Macros.GET_Y_LPARAM(message.LParam)));
                    message.Result = IntPtr.Zero;
                }
                break;
            case WM_WINDOWPOSCHANGING:
                {
                    WINDOWPOS wp = (WINDOWPOS)Marshal.PtrToStructure(message.LParam, typeof(WINDOWPOS));
                    if (!LockPositionAndSize)
                    {
                        // prevent the window from leaving the screen
                        if ((wp.flags & SWP_NOMOVE) == 0)
                        {
                            Rectangle rect = Screen.GetWorkingArea(new Rectangle(wp.x, wp.y, wp.cx, wp.cy));
                            const int margin = 16;
                            wp.x = Math.Max(wp.x, rect.Left - wp.cx + margin);
                            wp.x = Math.Min(wp.x, rect.Right - margin);
                            wp.y = Math.Max(wp.y, rect.Top - wp.cy + margin);
                            wp.y = Math.Min(wp.y, rect.Bottom - margin);
                        }

                        // update location and fire event
                        if ((wp.flags & SWP_NOMOVE) == 0)
                        {
                            if (_location.X != wp.x || _location.Y != wp.y)
                            {
                                _location = new Point(wp.x, wp.y);
                                LocationChanged?.Invoke(this, EventArgs.Empty);
                            }
                        }

                        // update size and fire event
                        if ((wp.flags & SWP_NOSIZE) == 0)
                        {
                            if (_size.Width != wp.cx || _size.Height != wp.cy)
                            {
                                _size = new Size(wp.cx, wp.cy);
                                SizeChanged?.Invoke(this, EventArgs.Empty);
                            }
                        }

                        // update the size of the layered window
                        if ((wp.flags & SWP_NOSIZE) == 0)
                            NativeMethods.UpdateLayeredWindow(Handle, IntPtr.Zero, IntPtr.Zero, ref _size, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, 0);

                        // update the position of the layered window
                        if ((wp.flags & SWP_NOMOVE) == 0)
                            NativeMethods.SetWindowPos(Handle, IntPtr.Zero, _location.X, _location.Y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOSENDCHANGING);
                    }

                    // do not forward any move or size messages
                    wp.flags |= SWP_NOSIZE | SWP_NOMOVE;

                    // suppress any frame changed events
                    wp.flags &= ~SWP_FRAMECHANGED;

                    Marshal.StructureToPtr(wp, message.LParam, false);
                    message.Result = IntPtr.Zero;
                }
                break;
            default:
                {
                    base.WndProc(ref message);
                }
                break;
        }
    }

    private BlendFunction CreateBlendFunction()
    {
        return new BlendFunction { BlendOp = AC_SRC_OVER, BlendFlags = 0, SourceConstantAlpha = _opacity, AlphaFormat = AC_SRC_ALPHA };
    }

    private void CreateBuffer()
    {
        IntPtr handleScreenDC = NativeMethods.GetDC(IntPtr.Zero);
        _handleBitmapDC = NativeMethods.CreateCompatibleDC(handleScreenDC);
        NativeMethods.ReleaseDC(IntPtr.Zero, handleScreenDC);
        _bufferSize = _size;

        BITMAPINFO info = new BITMAPINFO();
        info.Size = Marshal.SizeOf(info);
        info.Width = _size.Width;
        info.Height = -_size.Height;
        info.BitCount = 32;
        info.Planes = 1;

        IntPtr hBmp = NativeMethods.CreateDIBSection(_handleBitmapDC, ref info, 0, out IntPtr _, IntPtr.Zero, 0);
        IntPtr hBmpOld = NativeMethods.SelectObject(_handleBitmapDC, hBmp);
        NativeMethods.DeleteObject(hBmpOld);

        _graphics = Graphics.FromHdc(_handleBitmapDC);

        if (Environment.OSVersion.Version.Major > 5)
        {
            _graphics.TextRenderingHint = TextRenderingHint.SystemDefault;
            _graphics.SmoothingMode = SmoothingMode.HighQuality;
        }
    }

    private void DisposeBuffer()
    {
        _graphics.Dispose();
        NativeMethods.DeleteDC(_handleBitmapDC);
    }

    public void Dispose()
    {
        DisposeBuffer();
    }

    public PaintEventHandler Paint;

    public void Redraw()
    {
        if (!_visible || Paint == null)
            return;

        if (_size != _bufferSize)
        {
            DisposeBuffer();
            CreateBuffer();
        }

        Paint(this, new PaintEventArgs(_graphics, new Rectangle(Point.Empty, _size)));
        Point pointSource = Point.Empty;
        BlendFunction blend = CreateBlendFunction();
        NativeMethods.UpdateLayeredWindow(Handle, IntPtr.Zero, IntPtr.Zero, ref _size, _handleBitmapDC, ref pointSource, 0, ref blend, ULW_ALPHA);
        // make sure the window is at the right location
        NativeMethods.SetWindowPos(Handle, IntPtr.Zero, _location.X, _location.Y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOSENDCHANGING);
    }

    public byte Opacity
    {
        get
        {
            return _opacity;
        }
        set
        {
            if (_opacity != value)
            {
                _opacity = value;
                BlendFunction blend = CreateBlendFunction();
                NativeMethods.UpdateLayeredWindow(Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref blend, ULW_ALPHA);
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
                NativeMethods.SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOZORDER | (value ? SWP_SHOWWINDOW : SWP_HIDEWINDOW));

                if (value)
                {
                    if (!_alwaysOnTop)
                        ShowDesktop.Instance.ShowDesktopChanged += ShowDesktopChanged;
                }
                else
                {
                    if (!_alwaysOnTop)
                        ShowDesktop.Instance.ShowDesktopChanged -= ShowDesktopChanged;
                }
            }
        }
    }

    // if locked, the window can not be moved or resized
    public bool LockPositionAndSize { get; set; }

    public bool AlwaysOnTop
    {
        get
        {
            return _alwaysOnTop;
        }
        set
        {
            if (value != _alwaysOnTop)
            {
                _alwaysOnTop = value;

                if (_alwaysOnTop)
                {
                    if (_visible)
                        ShowDesktop.Instance.ShowDesktopChanged -= ShowDesktopChanged;

                    MoveToTopMost(Handle);
                }
                else
                {
                    MoveToBottom(Handle);

                    if (_visible)
                        ShowDesktop.Instance.ShowDesktopChanged += ShowDesktopChanged;
                }
            }
        }
    }

    public Size Size
    {
        get
        {
            return _size;
        }
        set
        {
            if (_size != value)
            {
                _size = value;
                NativeMethods.UpdateLayeredWindow(Handle, IntPtr.Zero, IntPtr.Zero, ref _size, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero, 0);
                SizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public Point Location
    {
        get
        {
            return _location;
        }
        set
        {
            if (_location != value)
            {
                _location = value;
                NativeMethods.SetWindowPos(Handle, IntPtr.Zero, _location.X, _location.Y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOSENDCHANGING);
                LocationChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ContextMenuStrip ContextMenuStrip { get; set; }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct WINDOWPOS
    {
        public readonly IntPtr hwnd;
        public readonly IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public readonly int cx;
        public readonly int cy;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public int Size;
        public int Width;
        public int Height;
        public short Planes;
        public short BitCount;
        public int Compression;
        public int SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public int ClrUsed;
        public int ClrImportant;
        public int Colors;
    }

    public static readonly IntPtr HWND_BOTTOM = (IntPtr)1;
    public static readonly IntPtr HWND_TOPMOST = (IntPtr)(-1);

    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TOOLWINDOW = 0x00000080;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_HIDEWINDOW = 0x0080;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOSENDCHANGING = 0x0400;

    public const int ULW_COLORKEY = 0x00000001;
    public const int ULW_ALPHA = 0x00000002;
    public const int ULW_OPAQUE = 0x00000004;

    public const byte AC_SRC_OVER = 0x00;
    public const byte AC_SRC_ALPHA = 0x01;

    public const int WM_NCHITTEST = 0x0084;
    public const int WM_NCLBUTTONDBLCLK = 0x00A3;
    public const int WM_NCLBUTTONDOWN = 0x00A1;
    public const int WM_NCLBUTTONUP = 0x00A2;
    public const int WM_NCRBUTTONDOWN = 0x00A4;
    public const int WM_NCRBUTTONUP = 0x00A5;
    public const int WM_WINDOWPOSCHANGING = 0x0046;
    public const int WM_COMMAND = 0x0111;

    public const int TPM_RIGHTBUTTON = 0x0002;
    public const int TPM_VERTICAL = 0x0040;

    private enum WindowAttribute : int
    {
        DWMWA_NCRENDERING_ENABLED = 1,
        DWMWA_NCRENDERING_POLICY,
        DWMWA_TRANSITIONS_FORCEDISABLED,
        DWMWA_ALLOW_NCPAINT,
        DWMWA_CAPTION_BUTTON_BOUNDS,
        DWMWA_NONCLIENT_RTL_LAYOUT,
        DWMWA_FORCE_ICONIC_REPRESENTATION,
        DWMWA_FLIP3D_POLICY,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        DWMWA_HAS_ICONIC_BITMAP,
        DWMWA_DISALLOW_PEEK,
        DWMWA_EXCLUDED_FROM_PEEK,
        DWMWA_LAST
    }

    /// <summary>
    /// Some macros imported and converted from the Windows SDK
    /// </summary>
    private static class Macros
    {
        public static ushort LOWORD(IntPtr l)
        {
            return (ushort)((ulong)l & 0xFFFF);
        }

        public static ushort HIWORD(IntPtr l)
        {
            return (ushort)(((ulong)l >> 16) & 0xFFFF);
        }

        public static int GET_X_LPARAM(IntPtr lp)
        {
            return (short)LOWORD(lp);
        }

        public static int GET_Y_LPARAM(IntPtr lp)
        {
            return (short)HIWORD(lp);
        }
    }

    /// <summary>
    /// Imported native methods
    /// </summary>
    private static class NativeMethods
    {
        private const string USER = "user32.dll";
        private const string GDI = "gdi32.dll";
        private const string DWMAPI = "dwmapi.dll";

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, IntPtr pptDst, ref Size psize, IntPtr hdcSrc, IntPtr pprSrc, int crKey, IntPtr pblend, int dwFlags);

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, IntPtr pptDst, ref Size psize, IntPtr hdcSrc, ref Point pprSrc, int crKey, ref BlendFunction pblend, int dwFlags);

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, IntPtr pptDst, IntPtr psize, IntPtr hdcSrc, IntPtr pprSrc, int crKey, ref BlendFunction pblend, int dwFlags);

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        public static extern bool TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr tpmParams);

        [DllImport(GDI, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport(GDI, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint pila, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport(GDI, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport(GDI, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport(GDI, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport(DWMAPI, CallingConvention = CallingConvention.Winapi)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, WindowAttribute dwAttribute, ref bool pvAttribute, int cbAttribute);
    }
}

public delegate void HitTestEventHandler(object sender, HitTestEventArgs e);

public enum HitResult
{
    Transparent = -1,
    Nowhere = 0,
    Client = 1,
    Caption = 2,
    Left = 10,
    Right = 11,
    Top = 12,
    TopLeft = 13,
    TopRight = 14,
    Bottom = 15,
    BottomLeft = 16,
    BottomRight = 17,
    Border = 18
}

public class HitTestEventArgs : EventArgs
{
    public HitTestEventArgs(Point location, HitResult hitResult)
    {
        Location = location;
        HitResult = hitResult;
    }
    public Point Location { get; }
    public HitResult HitResult { get; set; }
}