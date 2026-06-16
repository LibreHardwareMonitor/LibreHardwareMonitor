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

namespace LibreHardwareMonitor.Hardware;

internal class KernelDriver
{
    private readonly string _driverId;
    private readonly string _serviceName;
    private SafeFileHandle _device;

    public KernelDriver(string serviceName, string driverId)
    {
        _serviceName = serviceName;
        _driverId = driverId;
    }

    public bool IsOpen => _device != null;

    public bool Install(string path, out string errorMessage)
    {
        IntPtr manager = AdvApi32.OpenSCManager(null, null, AdvApi32.SC_MANAGER_ACCESS_MASK.SC_MANAGER_CREATE_SERVICE);
        if (manager == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            errorMessage = $"OpenSCManager returned the error code: {errorCode:X8}.";
            return false;
        }

        IntPtr service = AdvApi32.CreateService(manager,
                                                _serviceName,
                                                _serviceName,
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
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == Kernel32.ERROR_SERVICE_EXISTS)
            {
                errorMessage = "Service already exists";
                return false;
            }

            errorMessage = $"CreateService returned the error code: {errorCode:X8}.";
            AdvApi32.CloseServiceHandle(manager);
            return false;
        }

        if (!AdvApi32.StartService(service, 0, null))
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode != Kernel32.ERROR_SERVICE_ALREADY_RUNNING)
            {
                errorMessage = $"StartService returned the error code: {errorCode:X8}.";
                AdvApi32.CloseServiceHandle(service);
                AdvApi32.CloseServiceHandle(manager);
                return false;
            }
        }

        AdvApi32.CloseServiceHandle(service);
        AdvApi32.CloseServiceHandle(manager);

        try
        {
            // restrict the driver access to system (SY) and builtin admins (BA)
            // TODO: replace with a call to IoCreateDeviceSecure in the driver
            FileInfo fileInfo = new(@"\\.\" + _driverId);
            FileSecurity fileSecurity = fileInfo.GetAccessControl();
            fileSecurity.SetSecurityDescriptorSddlForm("O:BAG:SYD:(A;;FA;;;SY)(A;;FA;;;BA)");
            fileInfo.SetAccessControl(fileSecurity);
        }
        catch
        { }

        errorMessage = null;
        return true;
    }

    public bool Open()
    {
        IntPtr fileHandle = Kernel32.CreateFile(@"\\.\" + _driverId, 0xC0000000, FileShare.None, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);

        _device = new SafeFileHandle(fileHandle, true);
        if (_device.IsInvalid)
            Close();

        return _device != null;
    }

    public bool DeviceIOControl(Kernel32.IOControlCode ioControlCode, object inBuffer)
    {
        if (_device == null)
            return false;

        if (inBuffer == null)
            return DeviceIOControlCore(ioControlCode, IntPtr.Zero, 0, IntPtr.Zero, 0);

        return inBuffer switch
        {
            byte value => DeviceIOControl(ioControlCode, value),
            ushort value => DeviceIOControl(ioControlCode, value),
            uint value => DeviceIOControl(ioControlCode, value),
            ulong value => DeviceIOControl(ioControlCode, value),
            _ => DeviceIOControlBoxed(ioControlCode, inBuffer),
        };
    }

    public bool DeviceIOControl<TInput>(Kernel32.IOControlCode ioControlCode, TInput inBuffer)
        where TInput : struct
    {
        if (_device == null)
            return false;

        IntPtr inPtr = StructureToPtr(inBuffer, out uint inSize);
        try
        {
            return DeviceIOControlCore(ioControlCode, inPtr, inSize, IntPtr.Zero, 0);
        }
        finally
        {
            Marshal.FreeHGlobal(inPtr);
        }
    }

    public bool DeviceIOControl<T>(Kernel32.IOControlCode ioControlCode, object inBuffer, ref T outBuffer)
        where T : struct
    {
        if (_device == null)
            return false;

        if (inBuffer == null)
            return DeviceIOControlNoInput(ioControlCode, ref outBuffer);

        return inBuffer switch
        {
            byte value => DeviceIOControl(ioControlCode, value, ref outBuffer),
            ushort value => DeviceIOControl(ioControlCode, value, ref outBuffer),
            uint value => DeviceIOControl(ioControlCode, value, ref outBuffer),
            ulong value => DeviceIOControl(ioControlCode, value, ref outBuffer),
            _ => DeviceIOControlBoxed(ioControlCode, inBuffer, ref outBuffer),
        };
    }

    public bool DeviceIOControl<TInput, TOutput>(
        Kernel32.IOControlCode ioControlCode,
        TInput inBuffer,
        ref TOutput outBuffer)
        where TInput : struct
        where TOutput : struct
    {
        if (_device == null)
            return false;

        IntPtr inPtr = StructureToPtr(inBuffer, out uint inSize);
        try
        {
            return DeviceIOControlWithInput(ioControlCode, inPtr, inSize, ref outBuffer);
        }
        finally
        {
            Marshal.FreeHGlobal(inPtr);
        }
    }

    private bool DeviceIOControlNoInput<TOutput>(Kernel32.IOControlCode ioControlCode, ref TOutput outBuffer)
        where TOutput : struct
    {
        return DeviceIOControlWithInput(ioControlCode, IntPtr.Zero, 0, ref outBuffer);
    }

    private bool DeviceIOControlWithInput<TOutput>(
        Kernel32.IOControlCode ioControlCode,
        IntPtr inPtr,
        uint inSize,
        ref TOutput outBuffer)
        where TOutput : struct
    {
        int outSize = Marshal.SizeOf<TOutput>();
        IntPtr outPtr = Marshal.AllocHGlobal(outSize);
        try
        {
            Marshal.StructureToPtr(outBuffer, outPtr, false);
            bool result = DeviceIOControlCore(ioControlCode, inPtr, inSize, outPtr, (uint)outSize);
            if (result)
                outBuffer = Marshal.PtrToStructure<TOutput>(outPtr);

            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(outPtr);
        }
    }

    public bool DeviceIOControl<T>(Kernel32.IOControlCode ioControlCode, object inBuffer, ref T[] outBuffer)
        where T : struct
    {
        if (_device == null)
            return false;

        if (inBuffer == null)
            return DeviceIOControlNoInput(ioControlCode, ref outBuffer);

        return inBuffer switch
        {
            byte value => DeviceIOControl(ioControlCode, value, ref outBuffer),
            ushort value => DeviceIOControl(ioControlCode, value, ref outBuffer),
            uint value => DeviceIOControl(ioControlCode, value, ref outBuffer),
            ulong value => DeviceIOControl(ioControlCode, value, ref outBuffer),
            _ => DeviceIOControlBoxed(ioControlCode, inBuffer, ref outBuffer),
        };
    }

    public bool DeviceIOControl<TInput, TOutput>(
        Kernel32.IOControlCode ioControlCode,
        TInput inBuffer,
        ref TOutput[] outBuffer)
        where TInput : struct
        where TOutput : struct
    {
        if (_device == null)
            return false;

        IntPtr inPtr = StructureToPtr(inBuffer, out uint inSize);
        try
        {
            return DeviceIOControlWithInput(ioControlCode, inPtr, inSize, ref outBuffer);
        }
        finally
        {
            Marshal.FreeHGlobal(inPtr);
        }
    }

    private bool DeviceIOControlNoInput<TOutput>(Kernel32.IOControlCode ioControlCode, ref TOutput[] outBuffer)
        where TOutput : struct
    {
        return DeviceIOControlWithInput(ioControlCode, IntPtr.Zero, 0, ref outBuffer);
    }

    private bool DeviceIOControlWithInput<TOutput>(
        Kernel32.IOControlCode ioControlCode,
        IntPtr inPtr,
        uint inSize,
        ref TOutput[] outBuffer)
        where TOutput : struct
    {
        int elementSize = Marshal.SizeOf<TOutput>();
        int outSize = elementSize * outBuffer.Length;
        IntPtr outPtr = Marshal.AllocHGlobal(outSize);
        try
        {
            bool result = DeviceIOControlCore(ioControlCode, inPtr, inSize, outPtr, (uint)outSize);
            if (result)
            {
                for (int i = 0; i < outBuffer.Length; i++)
                    outBuffer[i] = Marshal.PtrToStructure<TOutput>(IntPtr.Add(outPtr, i * elementSize));
            }

            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(outPtr);
        }
    }

    private bool DeviceIOControlBoxed(Kernel32.IOControlCode ioControlCode, object inBuffer)
    {
        IntPtr inPtr = BoxedStructureToPtr(inBuffer, out uint inSize);
        try
        {
            return DeviceIOControlCore(ioControlCode, inPtr, inSize, IntPtr.Zero, 0);
        }
        finally
        {
            Marshal.FreeHGlobal(inPtr);
        }
    }

    private bool DeviceIOControlBoxed<TOutput>(
        Kernel32.IOControlCode ioControlCode,
        object inBuffer,
        ref TOutput outBuffer)
        where TOutput : struct
    {
        IntPtr inPtr = BoxedStructureToPtr(inBuffer, out uint inSize);
        try
        {
            return DeviceIOControlWithInput(ioControlCode, inPtr, inSize, ref outBuffer);
        }
        finally
        {
            Marshal.FreeHGlobal(inPtr);
        }
    }

    private bool DeviceIOControlBoxed<TOutput>(
        Kernel32.IOControlCode ioControlCode,
        object inBuffer,
        ref TOutput[] outBuffer)
        where TOutput : struct
    {
        IntPtr inPtr = BoxedStructureToPtr(inBuffer, out uint inSize);
        try
        {
            return DeviceIOControlWithInput(ioControlCode, inPtr, inSize, ref outBuffer);
        }
        finally
        {
            Marshal.FreeHGlobal(inPtr);
        }
    }

    private bool DeviceIOControlCore(
        Kernel32.IOControlCode ioControlCode,
        IntPtr inBuffer,
        uint inBufferSize,
        IntPtr outBuffer,
        uint outBufferSize)
    {
        return Kernel32.DeviceIoControl(
            _device,
            ioControlCode.Code,
            inBuffer,
            inBufferSize,
            outBuffer,
            outBufferSize,
            out uint _,
            IntPtr.Zero);
    }

    private static IntPtr StructureToPtr<T>(T value, out uint size)
        where T : struct
    {
        int byteCount = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(byteCount);
        Marshal.StructureToPtr(value, ptr, false);
        size = (uint)byteCount;
        return ptr;
    }

    private static IntPtr BoxedStructureToPtr(object value, out uint size)
    {
        int byteCount = Marshal.SizeOf(value);
        IntPtr ptr = Marshal.AllocHGlobal(byteCount);
        Marshal.StructureToPtr(value, ptr, false);
        size = (uint)byteCount;
        return ptr;
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
        IntPtr manager = AdvApi32.OpenSCManager(null, null, AdvApi32.SC_MANAGER_ACCESS_MASK.SC_MANAGER_CONNECT);
        if (manager == IntPtr.Zero)
            return false;

        IntPtr service = AdvApi32.OpenService(manager, _serviceName, AdvApi32.SERVICE_ACCESS_MASK.SERVICE_ALL_ACCESS);
        if (service == IntPtr.Zero)
        {
            AdvApi32.CloseServiceHandle(manager);
            return true;
        }

        AdvApi32.SERVICE_STATUS status = new();
        AdvApi32.ControlService(service, AdvApi32.SERVICE_CONTROL.SERVICE_CONTROL_STOP, ref status);
        AdvApi32.DeleteService(service);
        AdvApi32.CloseServiceHandle(service);
        AdvApi32.CloseServiceHandle(manager);

        return true;
    }
}
