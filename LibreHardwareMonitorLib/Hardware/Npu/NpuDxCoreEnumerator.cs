// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace LibreHardwareMonitor.Hardware.Npu;

internal static unsafe class NpuDxCoreEnumerator
{
    private static readonly Guid DxCoreAdapterFactoryGuid = new(0x78ee5945, 0xc36e, 0x4b13, 0xa6, 0x69, 0x00, 0x5d, 0xd1, 0x1c, 0x0f, 0x06);
    private static readonly Guid DxCoreAdapterListGuid = new(0x526c7776, 0x40e9, 0x459b, 0xb7, 0x11, 0xf3, 0x2a, 0xd7, 0x6d, 0xfc, 0x28);
    private static readonly Guid DxCoreAdapterGuid = new(0xf0db4c7f, 0xfe5a, 0x42a2, 0xbd, 0x62, 0xf2, 0xa6, 0xcf, 0x6f, 0xc8, 0x3e);
    private static readonly Guid DxCoreNpuAttributeGuid = new(0xd46140c4, 0xadd7, 0x451b, 0x9e, 0x56, 0x06, 0xfe, 0x8c, 0x3b, 0x58, 0xed);

    public static AdapterInfo[] EnumerateAdapters()
    {
        if (Software.OperatingSystem.IsUnix)
            return Array.Empty<AdapterInfo>();

        var adapters = new List<AdapterInfo>();
        IDXCoreAdapterFactory* factory = null;
        IDXCoreAdapterList* adapterList = null;

        try
        {
            Guid factoryGuid = DxCoreAdapterFactoryGuid;
            HRESULT hr = DXCoreCreateAdapterFactory(&factoryGuid, (void**)&factory);
            if (hr.Failed || factory == null)
                return Array.Empty<AdapterInfo>();

            Guid filterGuid = DxCoreNpuAttributeGuid;
            Guid adapterListGuid = DxCoreAdapterListGuid;
            hr = CreateAdapterList(factory, 1, &filterGuid, &adapterListGuid, (void**)&adapterList);
            if (hr.Failed || adapterList == null)
                return Array.Empty<AdapterInfo>();

            uint adapterCount = GetAdapterCount(adapterList);
            for (uint index = 0; index < adapterCount; index++)
            {
                IDXCoreAdapter* adapter = null;

                try
                {
                    Guid adapterGuid = DxCoreAdapterGuid;
                    hr = GetAdapter(adapterList, index, &adapterGuid, (void**)&adapter);
                    if (hr.Failed || adapter == null)
                        continue;

                    if (!TryGetProperty(adapter, DXCoreAdapterProperty.InstanceLuid, out LUID luid))
                        continue;

                    if (!TryGetStringProperty(adapter, DXCoreAdapterProperty.DriverDescription, out string description))
                        description = "NPU";

                    adapters.Add(new AdapterInfo(luid, description));
                }
                finally
                {
                    Release(adapter);
                }
            }
        }
        catch (DllNotFoundException)
        {
            return Array.Empty<AdapterInfo>();
        }
        finally
        {
            Release(adapterList);
            Release(factory);
        }

        return adapters.ToArray();
    }

    public readonly struct AdapterInfo
    {
        public AdapterInfo(LUID luid, string description)
        {
            Luid = luid;
            Description = description;
        }

        public LUID Luid { get; }

        public string Description { get; }
    }

    [DllImport("dxcore.dll", ExactSpelling = true)]
    private static extern HRESULT DXCoreCreateAdapterFactory(Guid* riid, void** ppvFactory);

    private static HRESULT CreateAdapterList(IDXCoreAdapterFactory* factory, uint attributeCount, Guid* attributes, Guid* riid, void** adapterList)
    {
        return ((delegate* unmanaged[Stdcall]<IDXCoreAdapterFactory*, uint, Guid*, Guid*, void**, HRESULT>)factory->LpVtbl[3])(factory, attributeCount, attributes, riid, adapterList);
    }

    private static uint GetAdapterCount(IDXCoreAdapterList* adapterList)
    {
        return ((delegate* unmanaged[Stdcall]<IDXCoreAdapterList*, uint>)adapterList->LpVtbl[4])(adapterList);
    }

    private static HRESULT GetAdapter(IDXCoreAdapterList* adapterList, uint index, Guid* riid, void** adapter)
    {
        return ((delegate* unmanaged[Stdcall]<IDXCoreAdapterList*, uint, Guid*, void**, HRESULT>)adapterList->LpVtbl[3])(adapterList, index, riid, adapter);
    }

    private static bool TryGetProperty<T>(IDXCoreAdapter* adapter, DXCoreAdapterProperty property, out T value)
        where T : unmanaged
    {
        T propertyValue = default;
        HRESULT hr = ((delegate* unmanaged[Stdcall]<IDXCoreAdapter*, DXCoreAdapterProperty, nuint, void*, HRESULT>)adapter->LpVtbl[6])(adapter, property, (nuint)sizeof(T), &propertyValue);
        value = propertyValue;
        return !hr.Failed;
    }

    private static bool TryGetStringProperty(IDXCoreAdapter* adapter, DXCoreAdapterProperty property, out string value)
    {
        value = null;
        nuint bufferSize = 0;
        HRESULT hr = ((delegate* unmanaged[Stdcall]<IDXCoreAdapter*, DXCoreAdapterProperty, nuint*, HRESULT>)adapter->LpVtbl[7])(adapter, property, &bufferSize);
        if (hr.Failed || bufferSize == 0)
            return false;

        byte[] buffer = new byte[checked((int)bufferSize)];
        fixed (byte* pBuffer = buffer)
        {
            hr = ((delegate* unmanaged[Stdcall]<IDXCoreAdapter*, DXCoreAdapterProperty, nuint, void*, HRESULT>)adapter->LpVtbl[6])(adapter, property, bufferSize, pBuffer);
            if (hr.Failed)
                return false;

            // DXCore DriverDescription is a null-terminated UTF-8 string, not UTF-16.
            value = Marshal.PtrToStringAnsi((IntPtr)pBuffer)?.TrimEnd('\0');
            return !string.IsNullOrWhiteSpace(value);
        }
    }

    private static void Release(void* instance)
    {
        if (instance == null)
            return;

        ((delegate* unmanaged[Stdcall]<void*, uint>)(*(void***)instance)[2])(instance);
    }

    private struct IDXCoreAdapterFactory
    {
        public void** LpVtbl;
    }

    private struct IDXCoreAdapterList
    {
        public void** LpVtbl;
    }

    private struct IDXCoreAdapter
    {
        public void** LpVtbl;
    }

    private enum DXCoreAdapterProperty : uint
    {
        InstanceLuid = 0,
        DriverDescription = 2
    }
}