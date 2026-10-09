// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Threading;

namespace LibreHardwareMonitor.Windows.Forms.Utilities;

/// <summary>
/// Moves work onto the UI thread. Hardware and sensors can be added or removed on any thread, but the tree must only be changed on the UI thread.
/// </summary>
public static class UiThread
{
    private static SynchronizationContext _context;
    private static int _threadId;

    /// <summary>
    /// Remembers the current thread as the UI thread. Call it from the UI thread before any hardware is added.
    /// </summary>
    public static void Capture()
    {
        _context = SynchronizationContext.Current;
        _threadId = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// Posts <paramref name="action" /> to the UI thread when called from another thread.
    /// </summary>
    /// <returns><see langword="true" /> if the action was posted, <see langword="false" /> if the caller should run it now.</returns>
    public static bool Post(Action action)
    {
        if (_context == null || Environment.CurrentManagedThreadId == _threadId)
            return false;

        try
        {
            _context.Post(_ => action(), null);
        }
        catch (InvalidOperationException)
        {
            // The UI thread has already closed.
        }

        return true;
    }
}
