// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.Storage.Nvme;
using Windows.Win32.System.Ioctl;

namespace LibreHardwareMonitor.Hardware.Storage;

internal class NVMeWindows : INVMeDrive
{
    //windows generic driver nvme access
    public SafeHandle Identify(StorageInfo storageInfo)
    {
        return IdentifyDevice(storageInfo);
    }

    public unsafe bool IdentifyController(SafeHandle hDevice, out NVME_IDENTIFY_CONTROLLER_DATA data)
    {
        data = new NVME_IDENTIFY_CONTROLLER_DATA();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;

        int cb = sizeof(STORAGE_PROPERTY_QUERY) + sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA) + sizeof(NVME_IDENTIFY_CONTROLLER_DATA);
        IntPtr ptr = Marshal.AllocHGlobal(cb);
        Marshal.Copy(new byte[cb], 0, ptr, cb); // Zero memory.

        STORAGE_PROPERTY_QUERY* query = (STORAGE_PROPERTY_QUERY*)ptr;
        query->PropertyId = STORAGE_PROPERTY_ID.StorageAdapterProtocolSpecificProperty;
        query->QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery;

        STORAGE_PROTOCOL_SPECIFIC_DATA* protocolData = (STORAGE_PROTOCOL_SPECIFIC_DATA*)(&query->AdditionalParameters);
        protocolData->ProtocolType = STORAGE_PROTOCOL_TYPE.ProtocolTypeNvme;
        protocolData->DataType = (uint)STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeIdentify;
        protocolData->ProtocolDataRequestValue = 1;
        protocolData->ProtocolDataOffset = (uint)sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA);
        protocolData->ProtocolDataLength = (uint)sizeof(NVME_IDENTIFY_CONTROLLER_DATA);

        bool validTransfer = PInvoke.DeviceIoControl(hDevice, PInvoke.IOCTL_STORAGE_QUERY_PROPERTY, (void*)ptr, (uint)cb, (void*)ptr, (uint)cb, null, null);
        if (validTransfer)
        {
            var dataDescriptor = (STORAGE_PROTOCOL_DATA_DESCRIPTOR*)ptr;
            protocolData = &dataDescriptor->ProtocolSpecificData;
            data = *(NVME_IDENTIFY_CONTROLLER_DATA*)((byte*)protocolData + protocolData->ProtocolDataOffset);
            Marshal.FreeHGlobal(ptr);
            result = true;
        }
        else
        {
            Marshal.FreeHGlobal(ptr);
        }

        return result;
    }

    public unsafe bool HealthInfoLog(SafeHandle hDevice, out NVME_HEALTH_INFO_LOG data)
    {
        data = new NVME_HEALTH_INFO_LOG();
        if (hDevice?.IsInvalid != false)
            return false;

        bool result = false;

        int cb = sizeof(STORAGE_PROPERTY_QUERY) + sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA) + sizeof(NVME_HEALTH_INFO_LOG);
        IntPtr ptr = Marshal.AllocHGlobal(cb);
        Marshal.Copy(new byte[cb], 0, ptr, cb); // Zero memory.

        STORAGE_PROPERTY_QUERY* query = (STORAGE_PROPERTY_QUERY*)ptr;
        query->PropertyId = STORAGE_PROPERTY_ID.StorageAdapterProtocolSpecificProperty;
        query->QueryType = STORAGE_QUERY_TYPE.PropertyStandardQuery;

        STORAGE_PROTOCOL_SPECIFIC_DATA* protocolData = (STORAGE_PROTOCOL_SPECIFIC_DATA*)(&query->AdditionalParameters);
        protocolData->ProtocolType = STORAGE_PROTOCOL_TYPE.ProtocolTypeNvme;
        protocolData->DataType = (uint)STORAGE_PROTOCOL_NVME_DATA_TYPE.NVMeDataTypeLogPage;
        protocolData->ProtocolDataRequestValue = (uint)NVME_LOG_PAGES.NVME_LOG_PAGE_HEALTH_INFO;
        protocolData->ProtocolDataOffset = (uint)sizeof(STORAGE_PROTOCOL_SPECIFIC_DATA);
        protocolData->ProtocolDataLength = (uint)sizeof(NVME_HEALTH_INFO_LOG);

        bool validTransfer = PInvoke.DeviceIoControl(hDevice, PInvoke.IOCTL_STORAGE_QUERY_PROPERTY, (void*)ptr, (uint)cb, (void*)ptr, (uint)cb, null, null);
        if (validTransfer)
        {
            var dataDescriptor = (STORAGE_PROTOCOL_DATA_DESCRIPTOR*)ptr;
            protocolData = &dataDescriptor->ProtocolSpecificData;

            data = *(NVME_HEALTH_INFO_LOG*)((byte*)protocolData + protocolData->ProtocolDataOffset);
            Marshal.FreeHGlobal(ptr);
            result = true;
        }
        else
        {
            Marshal.FreeHGlobal(ptr);
        }

        return result;
    }

    public static SafeHandle IdentifyDevice(StorageInfo storageInfo)
    {
        SafeFileHandle handle = PInvoke.CreateFile(storageInfo.DeviceId,
                                                   (uint)FileAccess.ReadWrite,
                                                   FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
                                                   null,
                                                   FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                                                   FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                                                   null);
        if (handle?.IsInvalid != false)
            return null;

        NVMeWindows nvme = new();
        if (nvme.IdentifyController(handle, out _))
            return handle;

        handle.Close();
        return null;
    }
}
