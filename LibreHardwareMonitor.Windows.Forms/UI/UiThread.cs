// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Windows.Forms;

namespace LibreHardwareMonitor.Windows.Forms.UI;

internal static class UIThread
{
    public static void BeginInvoke(Control owner, Action action)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (owner.IsDisposed || owner.Disposing)
        {
            return;
        }

        if (!owner.InvokeRequired)
        {
            action();
            return;
        }

        if (!owner.IsHandleCreated)
        {
            return;
        }

        try
        {
            owner.BeginInvoke(action);
        }
        catch (InvalidOperationException)
        {
            //Form can be disposed between state checks and BeginInvoke.
        }
    }
}
