// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.WinRing0.Utilities
{
    /// <summary>
    /// Helper class for dynamic loading of native functions via <see cref="LibreHardwareMonitor.Interop.Kernel32.GetProcAddress"/>.
    /// </summary>
    internal class DynamicLoader
    {
        #region Public

        public static T GetDelegate<T>(IntPtr module, string procName)
            where T : Delegate
        {
            return GetDelegate(module, procName, typeof(T)) as T;
        }

        public static Delegate GetDelegate(IntPtr module, string procName, Type delegateType)
        {
            IntPtr ptr = Kernel32.GetProcAddress(module, procName);
            if (ptr != IntPtr.Zero)
            {
                Delegate d = Marshal.GetDelegateForFunctionPointer(ptr, delegateType);
                return d;
            }

            int result = Marshal.GetHRForLastWin32Error();
            throw Marshal.GetExceptionForHR(result);
        }

        #endregion
    }
}
