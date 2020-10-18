// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;

namespace LibreHardwareMonitor.Hardware
{
    internal class KernelDriver
    {
        private readonly string _id;
        private SafeFileHandle _device;

        public KernelDriver(string id)
        {
            _id = id;
        }

        public bool IsOpen
        {
            get { return _device != null; }
        }

        public bool Install(string path, out string errorMessage)
        {
            IntPtr manager = AdvApi32.OpenSCManager(null, null, AdvApi32.SC_MANAGER_ACCESS_MASK.SC_MANAGER_ALL_ACCESS);
            if (manager == IntPtr.Zero)
            {
                errorMessage = "OpenSCManager returned zero.";
                return false;
            }

            IntPtr service = AdvApi32.CreateService(manager,
                                                    _id,
                                                    _id,
                                                    AdvApi32.SERVICE_ACCESS_MASK.SERVICE_ALL_ACCESS,
                                                    AdvApi32.SERVICE_TYPE.SERVICE_KERNEL_DRIVER,
                                                    AdvApi32.SERVICE_START.SERVICE_DEMAND_START,
                                                    AdvApi32.SERVICE_ERROR.SERVICE_ERROR_NORMAL,
                                                    path,
                                                    null,
                                                    null,
                                                    null,
                                                    null,
                                                    null);

            if (service == IntPtr.Zero)
            {
                if (Marshal.GetHRForLastWin32Error() == Kernel32.ERROR_SERVICE_EXISTS)
                {
                    errorMessage = "Service already exists";
                    return false;
                }

                errorMessage = "CreateService returned the error: " + Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message;
                AdvApi32.CloseServiceHandle(manager);
                return false;
            }

            if (!AdvApi32.StartService(service, 0, null))
            {
                if (Marshal.GetHRForLastWin32Error() != Kernel32.ERROR_SERVICE_ALREADY_RUNNING)
                {
                    errorMessage = "StartService returned the error: " + Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message;
                    AdvApi32.CloseServiceHandle(service);
                    AdvApi32.CloseServiceHandle(manager);
                    return false;
                }
            }

            AdvApi32.CloseServiceHandle(service);
            AdvApi32.CloseServiceHandle(manager);

#if !NETSTANDARD2_0
            try
            {
                // restrict the driver access to system (SY) and builtin admins (BA)
                // TODO: replace with a call to IoCreateDeviceSecure in the driver
                FileSecurity fileSecurity = File.GetAccessControl(@"\\.\" + _id);
                fileSecurity.SetSecurityDescriptorSddlForm("O:BAG:SYD:(A;;FA;;;SY)(A;;FA;;;BA)");
                File.SetAccessControl(@"\\.\" + _id, fileSecurity);
            }
            catch
            { }
#endif
            errorMessage = null;
            return true;
        }

        public bool Open()
        {
            _device = new SafeFileHandle(Kernel32.CreateFile(@"\\.\" + _id, 0xC0000000, FileShare.None, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero), true);
            if (_device.IsInvalid)
            {
                _device.Close();
                _device.Dispose();
                _device = null;
            }

            return _device != null;
        }

        public bool DeviceIOControl(Kernel32.IOControlCode ioControlCode, object inBuffer)
        {
            if (_device == null)
                return false;


            bool b = Kernel32.DeviceIoControl(_device, ioControlCode, inBuffer, inBuffer == null ? 0 : (uint)Marshal.SizeOf(inBuffer), null, 0, out uint _, IntPtr.Zero);
            return b;
        }

        public bool DeviceIOControl<T>(Kernel32.IOControlCode ioControlCode, object inBuffer, ref T outBuffer)
        {
            if (_device == null)
                return false;


            object boxedOutBuffer = outBuffer;
            bool b = Kernel32.DeviceIoControl(_device,
                                              ioControlCode,
                                              inBuffer,
                                              inBuffer == null ? 0 : (uint)Marshal.SizeOf(inBuffer),
                                              boxedOutBuffer,
                                              (uint)Marshal.SizeOf(boxedOutBuffer),
                                              out uint _,
                                              IntPtr.Zero);

            outBuffer = (T)boxedOutBuffer;
            return b;
        }

        public void Close()
        {
            if (_device != null)
            {
                _device.Close();
                _device.Dispose();
                _device = null;
            }
        }

        public bool Delete()
        {
            IntPtr manager = AdvApi32.OpenSCManager(null, null, AdvApi32.SC_MANAGER_ACCESS_MASK.SC_MANAGER_ALL_ACCESS);
            if (manager == IntPtr.Zero)
                return false;


            IntPtr service = AdvApi32.OpenService(manager, _id, AdvApi32.SERVICE_ACCESS_MASK.SERVICE_ALL_ACCESS);
            if (service == IntPtr.Zero)
                return true;


            AdvApi32.SERVICE_STATUS status = new AdvApi32.SERVICE_STATUS();
            AdvApi32.ControlService(service, AdvApi32.SERVICE_CONTROL.SERVICE_CONTROL_STOP, ref status);
            AdvApi32.DeleteService(service);
            AdvApi32.CloseServiceHandle(service);
            AdvApi32.CloseServiceHandle(manager);

            return true;
        }
    }
}
