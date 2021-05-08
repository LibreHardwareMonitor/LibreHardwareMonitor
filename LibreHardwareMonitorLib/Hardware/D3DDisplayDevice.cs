// ported from: https://github.com/processhacker/processhacker/blob/master/plugins/ExtendedTools/gpumon.c

using System;
using System.Linq;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware
{
    internal static class D3DDisplayDevice
    {
        public static string[] GetDisplayDeviceNames()
        {
            if (CfgMgr32.CM_Get_Device_Interface_List_Size(out uint size, ref CfgMgr32.GUID_DISPLAY_DEVICE_ARRIVAL, null, CfgMgr32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT) != CfgMgr32.CR_SUCCESS)
                return null;


            char[] data = new char[size];
            if (CfgMgr32.CM_Get_Device_Interface_List(ref CfgMgr32.GUID_DISPLAY_DEVICE_ARRIVAL, null, data, (uint)data.Length, CfgMgr32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT) == CfgMgr32.CR_SUCCESS)
                return new string(data).Split('\0').Where(m => !string.IsNullOrEmpty(m)).ToArray();


            return null;
        }

        public static bool GetDeviceInfoByName(string displayDeviceName, out D3DDeviceInfo deviceInfo)
        {
            deviceInfo = new D3DDeviceInfo();

            OpenAdapterFromDeviceName(out uint status, displayDeviceName, out D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter);
            if (status != WinNt.STATUS_SUCCESS)
                return false;


            GetAdapterType(out status, adapter, out D3dkmth.D3DKMT_ADAPTERTYPE adapterType);
            if (status != WinNt.STATUS_SUCCESS)
                return false;

            if (!adapterType.Value.HasFlag(D3dkmth.D3DKMT_ADAPTERTYPE_FLAGS.SoftwareDevice))
                return false;


            GetQueryStatisticsAdapterInformation(out status, adapter, out D3dkmth.D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation);
            if (status != WinNt.STATUS_SUCCESS)
                return false;


            uint segmentCount = adapterInformation.NbSegments;
            uint nodeCount = adapterInformation.NodeCount;

            deviceInfo.Nodes = new D3DDeviceNodeInfo[nodeCount];

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
                    Id = nodeId, Name = GetNodeEngineTypeString(nodeMetaData), RunningTime = nodeInformation.GlobalInformation.RunningTime.QuadPart, QueryTime = DateTime.Now
                };
            }

            for (uint segmentId = 0; segmentId < segmentCount; segmentId++)
            {
                GetQueryStatisticsSegment(out status, adapter, segmentId, out D3dkmth.D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation);
                if (status != WinNt.STATUS_SUCCESS)
                    return false;


                ulong commitLimit = segmentInformation.CommitLimit;
                ulong bytesResident = segmentInformation.BytesResident;
                ulong bytesCommitted = segmentInformation.BytesCommitted;

                uint aperture = segmentInformation.Aperture;

                if (aperture == 1)
                {
                    deviceInfo.GpuSharedLimit += commitLimit;
                    deviceInfo.GpuSharedUsed += bytesResident;
                    deviceInfo.GpuSharedMax += bytesCommitted;
                }
                else
                {
                    deviceInfo.GpuDedicatedLimit += commitLimit;
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
                D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_OTHER => nodeMetaData.NodeData.FriendlyName,
                D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_3D => "3D",
                D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_DECODE => "Video Decode",
                D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_ENCODE => "Video Encode",
                D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_PROCESSING => "Video Processing",
                D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_SCENE_ASSEMBLY => "Scene Assembly",
                D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_COPY => "Copy",
                D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_OVERLAY => "Overlay",
                D3dkmdt.DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_CRYPTO => "Crypto",
                _ => "Unknown",
            };
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

        private static void GetQueryStatisticsSegment(out uint status, D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint segmentId, out D3dkmth.D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation)
        {
            var queryElement = new D3dkmth.D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT { QuerySegment = { SegmentId = segmentId } };

            var queryStatistics = new D3dkmth.D3DKMT_QUERYSTATISTICS
            {
                AdapterLuid = adapter.AdapterLuid, Type = D3dkmth.D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_SEGMENT, QueryElement = queryElement
            };

            status = Gdi32.D3DKMTQueryStatistics(ref queryStatistics);

            segmentInformation = queryStatistics.QueryResult.SegmentInformation;
        }

        private static void GetQueryStatisticsAdapterInformation(out uint status, D3dkmth.D3DKMT_OPENADAPTERFROMDEVICENAME adapter, out D3dkmth.D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation)
        {
            var queryStatistics = new D3dkmth.D3DKMT_QUERYSTATISTICS { AdapterLuid = adapter.AdapterLuid, Type = D3dkmth.D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_ADAPTER, };

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
        }
    }
}
