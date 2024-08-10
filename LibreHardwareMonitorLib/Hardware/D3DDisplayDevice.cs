// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.
// Ported from: https://github.com/processhacker/processhacker/blob/master/plugins/ExtendedTools/gpumon.c

using System;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware;

internal static class D3DDisplayDevice
{
    public static string[] GetDeviceIdentifiers()
    {
        if (CfgMgr32.CM_Get_Device_Interface_List_Size(out uint size, ref CfgMgr32.GUID_DISPLAY_DEVICE_ARRIVAL, null, CfgMgr32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT) != CfgMgr32.CR_SUCCESS)
            return null;

        char[] data = new char[size];
        if (CfgMgr32.CM_Get_Device_Interface_List(ref CfgMgr32.GUID_DISPLAY_DEVICE_ARRIVAL, null, data, (uint)data.Length, CfgMgr32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT) == CfgMgr32.CR_SUCCESS)
            return new string(data).Split('\0').Where(m => !string.IsNullOrEmpty(m)).ToArray();

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

        OpenAdapterFromDeviceName(out uint status, deviceIdentifier, out D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter);
        if (status != WinNt.STATUS_SUCCESS)
            return false;

        GetAdapterType(out status, adapter, out D3dkmth.D3DKMT_ADAPTERTYPE adapterType);
        if (status != WinNt.STATUS_SUCCESS)
            return false;

        if (!adapterType.Value.HasFlag(D3dkmth.D3DKMT_ADAPTERTYPE_FLAGS.SoftwareDevice))
            return false;

        deviceInfo.Integrated = !adapterType.Value.HasFlag(D3dkmth.D3DKMT_ADAPTERTYPE_FLAGS.HybridIntegrated);

        GetQueryStatisticsAdapterInformation(out status, adapter, out D3dkmth.D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation);
        if (status != WinNt.STATUS_SUCCESS)
            return false;

        uint segmentCount = adapterInformation.NbSegments;
        uint nodeCount = adapterInformation.NodeCount;

        deviceInfo.Nodes = new D3DDeviceNodeInfo[nodeCount];

        DateTime queryTime = DateTime.Now;

        for (uint nodeId = 0; nodeId < nodeCount; nodeId++)
        {
            GetNodeMetaData(out status, adapter, nodeId, out D3dkmth.D3DKMT_NODEMETADATA nodeMetaData);
            if (status != WinNt.STATUS_SUCCESS)
                return false;

            GetQueryStatisticsNode(out status, adapter, nodeId, out D3dkmth.D3DKMT_QUERYSTATISTICS_NODE_INFORMATION nodeInformation);
            if (status != WinNt.STATUS_SUCCESS)
                return false;

            deviceInfo.Nodes[nodeId] = new D3DDeviceNodeInfo
            {
                Id = nodeId,
                Name = GetNodeEngineTypeString(nodeMetaData),
                RunningTime = nodeInformation.GlobalInformation.RunningTime.QuadPart,
                QueryTime = queryTime
            };
        }

        GetSegmentSize(out status, adapter, out D3dkmth.D3DKMT_SEGMENTSIZEINFO segmentSizeInfo);
        if (status != WinNt.STATUS_SUCCESS)
            return false;

        deviceInfo.GpuSharedLimit = segmentSizeInfo.SharedSystemMemorySize;
        deviceInfo.GpuDedicatedLimit = segmentSizeInfo.DedicatedSystemMemorySize;

        for (uint segmentId = 0; segmentId < segmentCount; segmentId++)
        {
            GetQueryStatisticsSegment(out status, adapter, segmentId, out D3dkmth.D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation);
            if (status != WinNt.STATUS_SUCCESS)
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
        return status == WinNt.STATUS_SUCCESS;
    }

    private static string GetNodeEngineTypeString(D3dkmth.D3DKMT_NODEMETADATA nodeMetaData)
    {
        return nodeMetaData.NodeData.EngineType switch
        {
            D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_OTHER => "D3D " + (!string.IsNullOrWhiteSpace(nodeMetaData.NodeData.FriendlyName) ? nodeMetaData.NodeData.FriendlyName : "Other"),
            D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_3D => "D3D 3D",
            D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_DECODE => "D3D Video Decode",
            D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_ENCODE => "D3D Video Encode",
            D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_PROCESSING => "D3D Video Processing",
            D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_SCENE_ASSEMBLY => "D3D Scene Assembly",
            D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_COPY => "D3D Copy",
            D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_OVERLAY => "D3D Overlay",
            D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_CRYPTO => "D3D Crypto",
            _ => "D3D Unknown"
        };
    }

    private static void GetSegmentSize
    (
        out uint status,
        D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter,
        out D3dkmth.D3DKMT_SEGMENTSIZEINFO sizeInformation)
    {
        IntPtr segmentSizePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(D3dkmth.D3DKMT_SEGMENTSIZEINFO)));
        sizeInformation = new D3dkmth.D3DKMT_SEGMENTSIZEINFO();
        Marshal.StructureToPtr(sizeInformation, segmentSizePtr, true);

        var queryAdapterInfo = new D3dkmth.D3DKMT_QUERYADAPTERINFO
        {
            hAdapter = adapter.hAdapter,
            Type = D3dkmth.KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_GETSEGMENTSIZE,
            pPrivateDriverData = segmentSizePtr,
            PrivateDriverDataSize = Marshal.SizeOf(typeof(D3dkmth.D3DKMT_SEGMENTSIZEINFO))
        };

        status = Gdi32.D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
        sizeInformation = Marshal.PtrToStructure<D3dkmth.D3DKMT_SEGMENTSIZEINFO>(segmentSizePtr);
        Marshal.FreeHGlobal(segmentSizePtr);
    }

    private static void GetNodeMetaData(out uint status, D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint nodeId, out D3dkmth.D3DKMT_NODEMETADATA nodeMetaDataResult)
    {
        IntPtr nodeMetaDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(D3dkmth.D3DKMT_NODEMETADATA)));
        nodeMetaDataResult = new D3dkmth.D3DKMT_NODEMETADATA { NodeOrdinalAndAdapterIndex = nodeId };
        Marshal.StructureToPtr(nodeMetaDataResult, nodeMetaDataPtr, true);

        var queryAdapterInfo = new D3dkmth.D3DKMT_QUERYADAPTERINFO
        {
            hAdapter = adapter.hAdapter,
            Type = D3dkmth.KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_NODEMETADATA,
            pPrivateDriverData = nodeMetaDataPtr,
            PrivateDriverDataSize = Marshal.SizeOf(typeof(D3dkmth.D3DKMT_NODEMETADATA))
        };

        status = Gdi32.D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
        nodeMetaDataResult = Marshal.PtrToStructure<D3dkmth.D3DKMT_NODEMETADATA>(nodeMetaDataPtr);
        Marshal.FreeHGlobal(nodeMetaDataPtr);
    }

    private static void GetQueryStatisticsNode(out uint status, D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint nodeId, out D3dkmth.D3DKMT_QUERYSTATISTICS_NODE_INFORMATION nodeInformation)
    {
        var queryElement = new D3dkmth.D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT { QueryNode = { NodeId = nodeId } };

        var queryStatistics = new D3dkmth.D3DKMT_QUERYSTATISTICS
        {
            AdapterLuid = adapter.AdapterLuid, Type = D3dkmth.D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_NODE, QueryElement = queryElement
        };

        status = Gdi32.D3DKMTQueryStatistics(ref queryStatistics);

        nodeInformation = queryStatistics.QueryResult.NodeInformation;
    }

    private static void GetQueryStatisticsSegment
    (
        out uint status,
        D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter,
        uint segmentId,
        out D3dkmth.D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation)
    {
        var queryElement = new D3dkmth.D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT { QuerySegment = { SegmentId = segmentId } };

        var queryStatistics = new D3dkmth.D3DKMT_QUERYSTATISTICS
        {
            AdapterLuid = adapter.AdapterLuid, Type = D3dkmth.D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_SEGMENT, QueryElement = queryElement
        };

        status = Gdi32.D3DKMTQueryStatistics(ref queryStatistics);

        segmentInformation = queryStatistics.QueryResult.SegmentInformation;
    }

    private static void GetQueryStatisticsAdapterInformation
    (
        out uint status,
        D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter,
        out D3dkmth.D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation)
    {
        var queryStatistics = new D3dkmth.D3DKMT_QUERYSTATISTICS { AdapterLuid = adapter.AdapterLuid, Type = D3dkmth.D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_ADAPTER };

        status = Gdi32.D3DKMTQueryStatistics(ref queryStatistics);

        adapterInformation = queryStatistics.QueryResult.AdapterInformation;
    }

    private static void GetAdapterType(out uint status, D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter, out D3dkmth.D3DKMT_ADAPTERTYPE adapterTypeResult)
    {
        IntPtr adapterTypePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(D3dkmth.D3DKMT_ADAPTERTYPE)));
        var queryAdapterInfo = new D3dkmth.D3DKMT_QUERYADAPTERINFO
        {
            hAdapter = adapter.hAdapter,
            Type = D3dkmth.KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_ADAPTERTYPE,
            pPrivateDriverData = adapterTypePtr,
            PrivateDriverDataSize = Marshal.SizeOf(typeof(D3dkmth.D3DKMT_ADAPTERTYPE))
        };

        status = Gdi32.D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
        adapterTypeResult = Marshal.PtrToStructure<D3dkmth.D3DKMT_ADAPTERTYPE>(adapterTypePtr);
        Marshal.FreeHGlobal(adapterTypePtr);
    }

    private static void OpenAdapterFromDeviceName(out uint status, string displayDeviceName, out D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter)
    {
        adapter = new D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME { pDeviceName = displayDeviceName };
        status = Gdi32.D3DKMTOpenAdapterFromDeviceName(ref adapter);
    }

    private static void CloseAdapter(out uint status, D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter)
    {
        var closeAdapter = new D3dkmth.D3DKMT_CLOSEADAPTER { hAdapter = adapter.hAdapter };
        status = Gdi32.D3DKMTCloseAdapter(ref closeAdapter);
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

        public ulong GpuSharedUsed;
        public ulong GpuDedicatedUsed;

        public ulong GpuSharedMax;
        public ulong GpuDedicatedMax;

        public D3DDeviceNodeInfo[] Nodes;
        public bool Integrated;
    }
}
