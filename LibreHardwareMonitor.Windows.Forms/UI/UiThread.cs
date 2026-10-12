// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Threading;
using System.Windows.Forms;

namespace LibreHardwareMonitor.Windows.Forms.UI;

internal sealed class UiThread
{
    private readonly Control _owner;
    private readonly SynchronizationContext _context;
    private readonly int _threadId;

    public UiThread(Control owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        // The form handle is not available yet when the constructor enables hardware groups.
        _context = SynchronizationContext.Current as WindowsFormsSynchronizationContext ?? new WindowsFormsSynchronizationContext();
        _threadId = Environment.CurrentManagedThreadId;
    }

    public void Post(Action action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (_owner.IsDisposed || _owner.Disposing)
            return;

        if (Environment.CurrentManagedThreadId == _threadId)
        {
            action();
            return;
        }

        try
        {
            _context.Post(_ =>
            {
                if (!_owner.IsDisposed && !_owner.Disposing)
                    action();
            }, null);
        }
        catch (InvalidOperationException)
        {
            // The UI thread may already be shutting down.
        }
    }
}
