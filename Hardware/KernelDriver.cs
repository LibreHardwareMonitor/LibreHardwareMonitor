// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;

namespace LibreHardwareMonitor.Hardware
{
    internal class KernelDriver
    {
        private const int ERROR_SERVICE_EXISTS = unchecked((int)0x80070431), ERROR_SERVICE_ALREADY_RUNNING = unchecked((int)0x80070420);
        private string _id;
        private SafeFileHandle _device;

        public KernelDriver(string id)
        {
            this._id = id;
        }

        public bool Install(string path, out string errorMessage)
        {
            IntPtr manager = Interop.Advapi32.OpenSCManager(null, null, Interop.Advapi32.ServiceControlManagerAccessRights.SC_MANAGER_ALL_ACCESS);
            if (manager == IntPtr.Zero)
            {
                errorMessage = "OpenSCManager returned zero.";
                return false;
            }

            IntPtr service = Interop.Advapi32.CreateService(manager, _id, _id, Interop.Advapi32.ServiceAccessRights.SERVICE_ALL_ACCESS,
                Interop.Advapi32.ServiceType.SERVICE_KERNEL_DRIVER, Interop.Advapi32.StartType.SERVICE_DEMAND_START,
                Interop.Advapi32.ErrorControl.SERVICE_ERROR_NORMAL, path, null, null, null, null, null);

            if (service == IntPtr.Zero)
            {
                if (Marshal.GetHRForLastWin32Error() == ERROR_SERVICE_EXISTS)
                {
                    errorMessage = "Service already exists";
                    return false;
                }
                else
                {
                    errorMessage = "CreateService returned the error: " + Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message;
                    Interop.Advapi32.CloseServiceHandle(manager);
                    return false;
                }
            }

            if (!Interop.Advapi32.StartService(service, 0, null))
            {
                if (Marshal.GetHRForLastWin32Error() != ERROR_SERVICE_ALREADY_RUNNING)
                {
                    errorMessage = "StartService returned the error: " + Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message;
                    Interop.Advapi32.CloseServiceHandle(service);
                    Interop.Advapi32.CloseServiceHandle(manager);
                    return false;
                }
            }
            Interop.Advapi32.CloseServiceHandle(service);
            Interop.Advapi32.CloseServiceHandle(manager);

#if !NETSTANDARD2_0
            try
            {
                // restrict the driver access to system (SY) and builtin admins (BA)
                // TODO: replace with a call to IoCreateDeviceSecure in the driver
                FileSecurity fileSecurity = File.GetAccessControl(@"\\.\" + _id);
                fileSecurity.SetSecurityDescriptorSddlForm("O:BAG:SYD:(A;;FA;;;SY)(A;;FA;;;BA)");
                File.SetAccessControl(@"\\.\" + _id, fileSecurity);
            }
            catch { }
#endif
            errorMessage = null;
            return true;
        }


        public bool Open()
        {
            _device = new SafeFileHandle(Interop.Kernel32.CreateFile(@"\\.\" + _id,
                Interop.Kernel32.Win32FileAccess.GENERIC_READ | Interop.Kernel32.Win32FileAccess.GENERIC_WRITE, 0, IntPtr.Zero,
                Interop.Kernel32.CreationDisposition.OPEN_EXISTING, System.IO.FileAttributes.Normal, IntPtr.Zero), true);

            if (_device.IsInvalid)
            {
                _device.Close();
                _device.Dispose();
                _device = null;
            }
            return _device != null;
        }

        public bool IsOpen
        {
            get { return _device != null; }
        }

        public bool DeviceIOControl(Interop.Kernel32.IOControlCode ioControlCode, object inBuffer)
        {
            if (_device == null)
                return false;

            uint bytesReturned;
            bool b = Interop.Kernel32.DeviceIoControl(_device, ioControlCode, inBuffer, inBuffer == null ? 0 : (uint)Marshal.SizeOf(inBuffer), null, 0, out bytesReturned, IntPtr.Zero);
            return b;
        }

        public bool DeviceIOControl<T>(Interop.Kernel32.IOControlCode ioControlCode, object inBuffer, ref T outBuffer)
        {
            if (_device == null)
                return false;

            object boxedOutBuffer = outBuffer;
            uint bytesReturned;
            bool b = Interop.Kernel32.DeviceIoControl(_device, ioControlCode, inBuffer, inBuffer == null ? 0 : (uint)Marshal.SizeOf(inBuffer),
                boxedOutBuffer, (uint)Marshal.SizeOf(boxedOutBuffer), out bytesReturned, IntPtr.Zero);
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
            IntPtr manager = Interop.Advapi32.OpenSCManager(null, null, Interop.Advapi32.ServiceControlManagerAccessRights.SC_MANAGER_ALL_ACCESS);
            if (manager == IntPtr.Zero)
                return false;

            IntPtr service = Interop.Advapi32.OpenService(manager, _id, Interop.Advapi32.ServiceAccessRights.SERVICE_ALL_ACCESS);
            if (service == IntPtr.Zero)
                return true;

            Interop.Advapi32.ServiceStatus status = new Interop.Advapi32.ServiceStatus();
            Interop.Advapi32.ControlService(service, Interop.Advapi32.ServiceControl.SERVICE_CONTROL_STOP, ref status);
            Interop.Advapi32.DeleteService(service);
            Interop.Advapi32.CloseServiceHandle(service);
            Interop.Advapi32.CloseServiceHandle(manager);

            return true;
        }

    }
}
