// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.
// Ported from: https://github.com/processhacker/processhacker/blob/master/plugins/ExtendedTools/gpumon.c

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Wdk.Graphics.Direct3D;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;

namespace LibreHardwareMonitor.Hardware;

internal static class D3DDisplayDevice
{
    public static unsafe string[] GetDeviceIdentifiers()
    {
        if (PInvoke.CM_Get_Device_Interface_List_Size(out uint size, PInvoke.GUID_DISPLAY_DEVICE_ARRIVAL, null, CM_GET_DEVICE_INTERFACE_LIST_FLAGS.CM_GET_DEVICE_INTERFACE_LIST_PRESENT) !=
            CONFIGRET.CR_SUCCESS)
            return null;

        char[] data = new char[size];
        fixed (char* pData = data)
        {
            if (PInvoke.CM_Get_Device_Interface_List(PInvoke.GUID_DISPLAY_DEVICE_ARRIVAL, null, pData, (uint)data.Length, CM_GET_DEVICE_INTERFACE_LIST_FLAGS.CM_GET_DEVICE_INTERFACE_LIST_PRESENT) ==
                CONFIGRET.CR_SUCCESS)
            {
                return new string(data).Split('\0').Where(m => !string.IsNullOrEmpty(m)).ToArray();
            }
        }

        return null;
    }

    public static string GetActualDeviceIdentifier(string deviceIdentifier)
    {
        if (string.IsNullOrEmpty(deviceIdentifier))
            return deviceIdentifier;

        // For example:
        // \\?\ROOT#BasicRender#0000#{1ca05180-a699-450a-9a0c-de4fbe3ddd89}  -->  ROOT\BasicRender\0000
        // \\?\PCI#VEN_1002&DEV_731F&SUBSYS_57051682&REV_C4#6&e539058&0&00000019#{1ca05180-a699-450a-9a0c-de4fbe3ddd89}  -->  PCI\VEN_1002&DEV_731F&SUBSYS_57051682&REV_C4\6&e539058&0&00000019

        if (deviceIdentifier.StartsWith(@"\\?\"))
            deviceIdentifier = deviceIdentifier.Substring(4);

        if (deviceIdentifier.Length > 0 && deviceIdentifier[deviceIdentifier.Length - 1] == '}')
        {
            int lastIndex = deviceIdentifier.LastIndexOf('{');
            if (lastIndex > 0)
                deviceIdentifier = deviceIdentifier.Substring(0, lastIndex - 1);
        }

        return deviceIdentifier.Replace('#', '\\');
    }

    public static bool GetDeviceInfoByIdentifier(string deviceIdentifier, out D3DDeviceInfo deviceInfo)
    {
        deviceInfo = new D3DDeviceInfo();

        OpenAdapterFromDeviceName(out NTSTATUS status, deviceIdentifier, out D3DKMT_OPENADAPTERFROMDEVICENAME adapter);
        if (status != NTSTATUS.STATUS_SUCCESS)
            return false;

        GetAdapterType(out status, adapter, out D3DKMT_ADAPTERTYPE adapterType);
        if (status != NTSTATUS.STATUS_SUCCESS)
            return false;

        if (adapterType.Anonymous.Anonymous.SoftwareDevice)
            return false;

        deviceInfo.Integrated = adapterType.Anonymous.Anonymous.HybridIntegrated;

        GetQueryStatisticsAdapterInformation(out status, adapter, out D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation);
        if (status != NTSTATUS.STATUS_SUCCESS)
            return false;

        uint segmentCount = adapterInformation.NbSegments;
        uint nodeCount = adapterInformation.NodeCount;

        deviceInfo.Nodes = new D3DDeviceNodeInfo[nodeCount];

        DateTime queryTime = DateTime.Now;

        for (uint nodeId = 0; nodeId < nodeCount; nodeId++)
        {
            GetNodeMetaData(out status, adapter, nodeId, out D3DKMT_NODEMETADATA nodeMetaData);
            if (status != NTSTATUS.STATUS_SUCCESS)
                return false;

            GetQueryStatisticsNode(out status, adapter, nodeId, out D3DKMT_QUERYSTATISTICS_NODE_INFORMATION nodeInformation);
            if (status != NTSTATUS.STATUS_SUCCESS)
                return false;

            deviceInfo.Nodes[nodeId] = new D3DDeviceNodeInfo
            {
                Id = nodeId, 
                Name = GetNodeEngineTypeString(nodeMetaData),
                RunningTime = nodeInformation.GlobalInformation.RunningTime, 
                QueryTime = queryTime
            };
        }

        GetSegmentSize(out status, adapter, out D3DKMT_SEGMENTSIZEINFO segmentSizeInfo);
        if (status != NTSTATUS.STATUS_SUCCESS)
            return false;

        deviceInfo.GpuSharedLimit = segmentSizeInfo.SharedSystemMemorySize;
        deviceInfo.GpuVideoMemoryLimit = segmentSizeInfo.DedicatedVideoMemorySize;
        deviceInfo.GpuDedicatedLimit = segmentSizeInfo.DedicatedSystemMemorySize;

        for (uint segmentId = 0; segmentId < segmentCount; segmentId++)
        {
            GetQueryStatisticsSegment(out status, adapter, segmentId, out D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation);
            if (status != NTSTATUS.STATUS_SUCCESS)
                return false;

            ulong bytesResident = segmentInformation.BytesResident;
            ulong bytesCommitted = segmentInformation.BytesCommitted;

            uint aperture = segmentInformation.Aperture;

            if (aperture == 1)
            {
                deviceInfo.GpuSharedUsed += bytesResident;
                deviceInfo.GpuSharedMax += bytesCommitted;
            }
            else
            {
                deviceInfo.GpuDedicatedUsed += bytesResident;
                deviceInfo.GpuDedicatedMax += bytesCommitted;
            }
        }

        CloseAdapter(out status, adapter);
        return status == NTSTATUS.STATUS_SUCCESS;
    }

    private static string GetNodeEngineTypeString(D3DKMT_NODEMETADATA nodeMetaData)
    {
        return nodeMetaData.NodeData.EngineType switch
        {
            DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_OTHER => "D3D " + (nodeMetaData.NodeData.FriendlyName.Length > 0 ? nodeMetaData.NodeData.FriendlyName : "Other"),
            DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_3D => "D3D 3D",
            DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_DECODE => "D3D Video Decode",
            DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_ENCODE => "D3D Video Encode",
            DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_PROCESSING => "D3D Video Processing",
            DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_SCENE_ASSEMBLY => "D3D Scene Assembly",
            DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_COPY => "D3D Copy",
            DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_OVERLAY => "D3D Overlay",
            DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_CRYPTO => "D3D Crypto",
            _ => "D3D Unknown"
        };
    }

    private static unsafe void GetSegmentSize
    (
        out NTSTATUS status,
        D3DKMT_OPENADAPTERFROMDEVICENAME adapter,
        out D3DKMT_SEGMENTSIZEINFO sizeInformation)
    {
        IntPtr segmentSizePtr = Marshal.AllocHGlobal(sizeof(D3DKMT_SEGMENTSIZEINFO));

        var queryAdapterInfo = new D3DKMT_QUERYADAPTERINFO
        {
            hAdapter = adapter.hAdapter,
            Type = KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_GETSEGMENTSIZE,
            pPrivateDriverData = (void*)segmentSizePtr,
            PrivateDriverDataSize = (uint)sizeof(D3DKMT_SEGMENTSIZEINFO)
        };

        status = Windows.Wdk.PInvoke.D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
        sizeInformation = *(D3DKMT_SEGMENTSIZEINFO*)segmentSizePtr;
        Marshal.FreeHGlobal(segmentSizePtr);
    }

    private static unsafe void GetNodeMetaData(out NTSTATUS status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint nodeId, out D3DKMT_NODEMETADATA nodeMetaDataResult)
    {
        IntPtr nodeMetaDataPtr = Marshal.AllocHGlobal(sizeof(D3DKMT_NODEMETADATA));

        D3DKMT_NODEMETADATA* pData = (D3DKMT_NODEMETADATA*)nodeMetaDataPtr;
        pData->NodeOrdinalAndAdapterIndex = nodeId;

        var queryAdapterInfo = new D3DKMT_QUERYADAPTERINFO
        {
            hAdapter = adapter.hAdapter,
            Type = KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_NODEMETADATA,
            pPrivateDriverData = (void*)nodeMetaDataPtr,
            PrivateDriverDataSize = (uint)sizeof(D3DKMT_NODEMETADATA)
        };

        status = Windows.Wdk.PInvoke.D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
        nodeMetaDataResult = *(D3DKMT_NODEMETADATA*)nodeMetaDataPtr;
        Marshal.FreeHGlobal(nodeMetaDataPtr);
    }

    private static void GetQueryStatisticsNode(out NTSTATUS status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint nodeId, out D3DKMT_QUERYSTATISTICS_NODE_INFORMATION nodeInformation)
    {
        var queryStatistics = new D3DKMT_QUERYSTATISTICS
        {
            AdapterLuid = adapter.AdapterLuid,
            Type = D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_NODE,
            Anonymous = new D3DKMT_QUERYSTATISTICS._Anonymous_e__Union { QueryNode = new D3DKMT_QUERYSTATISTICS_QUERY_NODE { NodeId = nodeId } }
        };

        status = Windows.Wdk.PInvoke.D3DKMTQueryStatistics(queryStatistics);
        nodeInformation = queryStatistics.QueryResult.NodeInformation;
    }

    private static void GetQueryStatisticsSegment
    (
        out NTSTATUS status,
        D3DKMT_OPENADAPTERFROMDEVICENAME adapter,
        uint segmentId,
        out D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation)
    {
        var queryStatistics = new D3DKMT_QUERYSTATISTICS
        {
            AdapterLuid = adapter.AdapterLuid,
            Type = D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_SEGMENT,
            Anonymous = new D3DKMT_QUERYSTATISTICS._Anonymous_e__Union { QuerySegment = new D3DKMT_QUERYSTATISTICS_QUERY_SEGMENT { SegmentId = segmentId } }
        };

        status = Windows.Wdk.PInvoke.D3DKMTQueryStatistics(queryStatistics);
        segmentInformation = queryStatistics.QueryResult.SegmentInformation;
    }

    private static void GetQueryStatisticsAdapterInformation
    (
        out NTSTATUS status,
        D3DKMT_OPENADAPTERFROMDEVICENAME adapter,
        out D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation)
    {
        var queryStatistics = new D3DKMT_QUERYSTATISTICS { AdapterLuid = adapter.AdapterLuid, Type = D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_ADAPTER };

        status = Windows.Wdk.PInvoke.D3DKMTQueryStatistics(queryStatistics);

        adapterInformation = queryStatistics.QueryResult.AdapterInformation;
    }

    private static unsafe void GetAdapterType(out NTSTATUS status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter, out D3DKMT_ADAPTERTYPE adapterTypeResult)
    {
        IntPtr adapterTypePtr = Marshal.AllocHGlobal(sizeof(D3DKMT_ADAPTERTYPE));

        var queryAdapterInfo = new D3DKMT_QUERYADAPTERINFO
        {
            hAdapter = adapter.hAdapter, Type = KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_ADAPTERTYPE, pPrivateDriverData = (void*)adapterTypePtr, PrivateDriverDataSize = (uint)sizeof(D3DKMT_ADAPTERTYPE)
        };

        status = Windows.Wdk.PInvoke.D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
        adapterTypeResult = *(D3DKMT_ADAPTERTYPE*)adapterTypePtr;
        Marshal.FreeHGlobal(adapterTypePtr);
    }

    private static unsafe void OpenAdapterFromDeviceName(out NTSTATUS status, string displayDeviceName, out D3DKMT_OPENADAPTERFROMDEVICENAME adapter)
    {
        fixed (char* pDisplayDeviceName = displayDeviceName)
        {
            adapter = new D3DKMT_OPENADAPTERFROMDEVICENAME { pDeviceName = new PCWSTR(pDisplayDeviceName) };
            status = Windows.Wdk.PInvoke.D3DKMTOpenAdapterFromDeviceName(ref adapter);
        }
    }

    private static void CloseAdapter(out NTSTATUS status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter)
    {
        var closeAdapter = new D3DKMT_CLOSEADAPTER { hAdapter = adapter.hAdapter };
        status = Windows.Wdk.PInvoke.D3DKMTCloseAdapter(closeAdapter);
    }

    public struct D3DDeviceNodeInfo
    {
        public ulong Id;
        public string Name;
        public long RunningTime;
        public DateTime QueryTime;
    }

    public struct D3DDeviceInfo
    {
        public ulong GpuSharedLimit;
        public ulong GpuDedicatedLimit;
        public ulong GpuVideoMemoryLimit;

        public ulong GpuSharedUsed;
        public ulong GpuDedicatedUsed;

        public ulong GpuSharedMax;
        public ulong GpuDedicatedMax;

        public D3DDeviceNodeInfo[] Nodes;
        public bool Integrated;
    }
}
