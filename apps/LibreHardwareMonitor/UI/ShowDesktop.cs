// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI;

public class ShowDesktop : IDisposable
{
    private readonly NativeWindow _referenceWindow;
    private readonly string _referenceWindowCaption = "LibreHardwareMonitorShowDesktopReferenceWindow";
    private readonly System.Threading.Timer _timer;
    private bool _showDesktop;

    /// <summary>
    /// Prevents a default instance of the <see cref="ShowDesktop" /> class from being created.
    /// </summary>
    private ShowDesktop()
    {
        // Create a reference window to detect show desktop
        _referenceWindow = new NativeWindow();

        CreateParams cp = new CreateParams { ExStyle = GadgetWindow.WS_EX_TOOLWINDOW, Caption = _referenceWindowCaption };
        _referenceWindow.CreateHandle(cp);
        NativeMethods.SetWindowPos(_referenceWindow.Handle,
                                   GadgetWindow.HWND_BOTTOM,
                                   0,
                                   0,
                                   0,
                                   0,
                                   GadgetWindow.SWP_NOMOVE |
                                   GadgetWindow.SWP_NOSIZE |
                                   GadgetWindow.SWP_NOACTIVATE |
                                   GadgetWindow.SWP_NOSENDCHANGING);

        // start a repeated timer to detect "Show Desktop" events
        _timer = new System.Threading.Timer(OnTimer, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
    }

    public delegate void ShowDesktopChangedEventHandler(bool showDesktop);

    private event ShowDesktopChangedEventHandler ShowDesktopChangedEvent;

    // notify when the "show desktop" mode is changed
    public event ShowDesktopChangedEventHandler ShowDesktopChanged
    {
        add
        {
            // start the monitor timer when someone is listening
            if (ShowDesktopChangedEvent == null)
                StartTimer();

            ShowDesktopChangedEvent += value;
        }
        remove
        {
            ShowDesktopChangedEvent -= value;
            // stop the monitor timer if nobody is interested
            if (ShowDesktopChangedEvent == null)
                StopTimer();
        }
    }

    public static ShowDesktop Instance { get; } = new ShowDesktop();

    /// <inheritdoc />
    public void Dispose()
    {
        _timer?.Dispose();
        _referenceWindow.ReleaseHandle();
    }

    private void StartTimer()
    {
        _timer.Change(0, 200);
    }

    private void StopTimer()
    {
        _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
    }

    // the desktop worker window (if available) can hide the reference window
    private IntPtr GetDesktopWorkerWindow()
    {
        IntPtr shellWindow = NativeMethods.GetShellWindow();
        if (shellWindow == IntPtr.Zero)
            return IntPtr.Zero;


        NativeMethods.GetWindowThreadProcessId(shellWindow, out int shellId);

        IntPtr workerWindow = IntPtr.Zero;
        while ((workerWindow = NativeMethods.FindWindowEx(IntPtr.Zero, workerWindow, "WorkerW", null)) != IntPtr.Zero)
        {
            NativeMethods.GetWindowThreadProcessId(workerWindow, out int workerId);
            if (workerId == shellId)
            {
                IntPtr window = NativeMethods.FindWindowEx(workerWindow, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (window != IntPtr.Zero)
                {
                    IntPtr desktopWindow = NativeMethods.FindWindowEx(window, IntPtr.Zero, "SysListView32", null);
                    if (desktopWindow != IntPtr.Zero)
                        return workerWindow;
                }
            }
        }

        return IntPtr.Zero;
    }

    private void OnTimer(object state)
    {
        bool showDesktopDetected;

        IntPtr workerWindow = GetDesktopWorkerWindow();
        if (workerWindow != IntPtr.Zero)
        {
            // search if the reference window is behind the worker window
            IntPtr reference = NativeMethods.FindWindowEx(IntPtr.Zero, workerWindow, null, _referenceWindowCaption);
            showDesktopDetected = reference != IntPtr.Zero;
        }
        else
        {
            // if there is no worker window, then nothing can hide the reference
            showDesktopDetected = false;
        }

        if (_showDesktop != showDesktopDetected)
        {
            _showDesktop = showDesktopDetected;
            ShowDesktopChangedEvent?.Invoke(_showDesktop);
        }
    }

    private static class NativeMethods
    {
        private const string USER = "user32.dll";

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr GetShellWindow();

        [DllImport(USER, CallingConvention = CallingConvention.Winapi)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);
    }
}