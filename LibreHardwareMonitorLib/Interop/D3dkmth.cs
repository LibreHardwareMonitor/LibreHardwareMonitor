// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop;

internal static class D3dkmth
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_CLOSEADAPTER
    {
        public uint hAdapter;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYADAPTERINFO
    {
        public uint hAdapter;
        public KMTQUERYADAPTERINFOTYPE Type;
        public IntPtr pPrivateDriverData;
        public int PrivateDriverDataSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_OPENADAPTERFROMDEVICENAME
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDeviceName;

        public uint hAdapter;
        public WinNt.LUID AdapterLuid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_ADAPTERTYPE
    {
        public D3DKMT_ADAPTERTYPE_FLAGS Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_QUERY_SEGMENT
    {
        public uint SegmentId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_QUERY_NODE
    {
        public uint NodeId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_QUERY_VIDPNSOURCE
    {
        public uint VidPnSourceId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_REFERENCE_DMA_BUFFER
    {
        public uint NbCall;
        public uint NbAllocationsReferenced;
        public uint MaxNbAllocationsReferenced;
        public uint NbNULLReference;
        public uint NbWriteReference;
        public uint NbRenamedAllocationsReferenced;
        public uint NbIterationSearchingRenamedAllocation;
        public uint NbLockedAllocationReferenced;
        public uint NbAllocationWithValidPrepatchingInfoReferenced;
        public uint NbAllocationWithInvalidPrepatchingInfoReferenced;
        public uint NbDMABufferSuccessfullyPrePatched;
        public uint NbPrimariesReferencesOverflow;
        public uint NbAllocationWithNonPreferredResources;
        public uint NbAllocationInsertedInMigrationTable;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_SEGMENTSIZEINFO
    {
        public ulong DedicatedVideoMemorySize;
        public ulong DedicatedSystemMemorySize;
        public ulong SharedSystemMemorySize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_RENAMING
    {
        public uint NbAllocationsRenamed;
        public uint NbAllocationsShrinked;
        public uint NbRenamedBuffer;
        public uint MaxRenamingListLength;
        public uint NbFailuresDueToRenamingLimit;
        public uint NbFailuresDueToCreateAllocation;
        public uint NbFailuresDueToOpenAllocation;
        public uint NbFailuresDueToLowResource;
        public uint NbFailuresDueToNonRetiredLimit;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_COUNTER
    {
        public uint Count;
        public ulong Bytes;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_PREPRATION
    {
        public uint BroadcastStall;
        public uint NbDMAPrepared;
        public uint NbDMAPreparedLongPath;
        public uint ImmediateHighestPreparationPass;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocationsTrimmed;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_PAGING_FAULT
    {
        public D3DKMT_QUERYSTATISTICS_COUNTER Faults;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsFirstTimeAccess;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsReclaimed;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsMigration;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsIncorrectResource;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsLostContent;
        public D3DKMT_QUERYSTATISTICS_COUNTER FaultsEvicted;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocationsMEM_RESET;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocationsUnresetSuccess;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocationsUnresetFail;

        public uint AllocationsUnresetSuccessRead;
        public uint AllocationsUnresetFailRead;

        public D3DKMT_QUERYSTATISTICS_COUNTER Evictions;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToPreparation;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToLock;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToClose;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToPurge;
        public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToSuspendCPUAccess;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_PAGING_TRANSFER
    {
        public ulong BytesFilled;
        public ulong BytesDiscarded;
        public ulong BytesMappedIntoAperture;
        public ulong BytesUnmappedFromAperture;
        public ulong BytesTransferredFromMdlToMemory;
        public ulong BytesTransferredFromMemoryToMdl;
        public ulong BytesTransferredFromApertureToMemory;
        public ulong BytesTransferredFromMemoryToAperture;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_SWIZZLING_RANGE
    {
        public uint NbRangesAcquired;
        public uint NbRangesReleased;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_LOCKS
    {
        public uint NbLocks;
        public uint NbLocksWaitFlag;
        public uint NbLocksDiscardFlag;
        public uint NbLocksNoOverwrite;
        public uint NbLocksNoReadSync;
        public uint NbLocksLinearization;
        public uint NbComplexLocks;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_ALLOCATIONS
    {
        public D3DKMT_QUERYSTATISTICS_COUNTER Created;
        public D3DKMT_QUERYSTATISTICS_COUNTER Destroyed;
        public D3DKMT_QUERYSTATISTICS_COUNTER Opened;
        public D3DKMT_QUERYSTATISTICS_COUNTER Closed;
        public D3DKMT_QUERYSTATISTICS_COUNTER MigratedSuccess;
        public D3DKMT_QUERYSTATISTICS_COUNTER MigratedFail;
        public D3DKMT_QUERYSTATISTICS_COUNTER MigratedAbandoned;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_TERMINATIONS
    {
        public D3DKMT_QUERYSTATISTICS_COUNTER TerminatedShared;
        public D3DKMT_QUERYSTATISTICS_COUNTER TerminatedNonShared;
        public D3DKMT_QUERYSTATISTICS_COUNTER DestroyedShared;
        public D3DKMT_QUERYSTATISTICS_COUNTER DestroyedNonShared;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION
    {
        public uint NbSegments;
        public uint NodeCount;
        public uint VidPnSourceCount;

        public uint VSyncEnabled;
        public uint TdrDetectedCount;

        public long ZeroLengthDmaBuffers;
        public ulong RestartedPeriod;

        public D3DKMT_QUERYSTATISTICS_REFERENCE_DMA_BUFFER ReferenceDmaBuffer;
        public D3DKMT_QUERYSTATISTICS_RENAMING Renaming;
        public D3DKMT_QUERYSTATISTICS_PREPRATION Preparation;
        public D3DKMT_QUERYSTATISTICS_PAGING_FAULT PagingFault;
        public D3DKMT_QUERYSTATISTICS_PAGING_TRANSFER PagingTransfer;
        public D3DKMT_QUERYSTATISTICS_SWIZZLING_RANGE SwizzlingRange;
        public D3DKMT_QUERYSTATISTICS_LOCKS Locks;
        public D3DKMT_QUERYSTATISTICS_ALLOCATIONS Allocations;
        public D3DKMT_QUERYSTATISTICS_TERMINATIONS Terminations;

        private readonly ulong Reserved;
        private readonly ulong Reserved1;
        private readonly ulong Reserved2;
        private readonly ulong Reserved3;
        private readonly ulong Reserved4;
        private readonly ulong Reserved5;
        private readonly ulong Reserved6;
        private readonly ulong Reserved7;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_MEMORY
    {
        public ulong TotalBytesEvicted;
        public uint AllocsCommitted;
        public uint AllocsResident;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION
    {
        public ulong CommitLimit;
        public ulong BytesCommitted;
        public ulong BytesResident;
        public D3DKMT_QUERYSTATISTICS_MEMORY Memory;
        public uint Aperture; // boolean
        public fixed ulong TotalBytesEvictedByPriority[5]; // D3DKMT_QUERYSTATISTICS_SEGMENT_PREFERENCE_MAX
        public ulong SystemMemoryEndAddress;
        public D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION_POWER_FLAGS PowerFlags;
        public fixed ulong Reserved[6];
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION_POWER_FLAGS
    {
        [FieldOffset(0)]
        public ulong PreservedDuringStandby;

        [FieldOffset(1)]
        public ulong PreservedDuringHibernate;

        [FieldOffset(2)]
        public ulong PartiallyPreservedDuringHibernate;

        [FieldOffset(3)]
        public ulong Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_VIDEO_MEMORY
    {
        public uint AllocsCommitted;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn0;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn1;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn2;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn3;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn4;
        public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentInNonPreferred;
        public ulong TotalBytesEvictedDueToPreparation;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_POLICY
    {
        public ulong UseMRU;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_INFORMATION
    {
        public ulong BytesCommitted;
        public ulong MaximumWorkingSet;
        public ulong MinimumWorkingSet;

        public uint NbReferencedAllocationEvictedInPeriod;

        public D3DKMT_QUERYSTATISTICS_VIDEO_MEMORY VideoMemory;
        public D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_POLICY _Policy;

        public fixed ulong Reserved[8];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_PREEMPTION_INFORMATION
    {
        public uint PreemptionCounter;
        public uint PreemptionCounter1;
        public uint PreemptionCounter2;
        public uint PreemptionCounter3;
        public uint PreemptionCounter4;
        public uint PreemptionCounter5;
        public uint PreemptionCounter6;
        public uint PreemptionCounter7;
        public uint PreemptionCounter8;
        public uint PreemptionCounter9;
        public uint PreemptionCounter10;
        public uint PreemptionCounter11;
        public uint PreemptionCounter12;
        public uint PreemptionCounter13;
        public uint PreemptionCounter14;
        public uint PreemptionCounter15;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION
    {
        public WinNt.LARGE_INTEGER RunningTime; // 100ns
        public uint ContextSwitch;
        private readonly D3DKMT_QUERYSTATISTICS_PREEMPTION_INFORMATION PreemptionStatistics;
        private readonly D3DKMT_QUERYSTATISTICS_PACKET_INFORMATION PacketStatistics;
        private fixed ulong Reserved[8];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct D3DKMT_QUERYSTATISTICS_NODE_INFORMATION
    {
        public D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION GlobalInformation; // global

        public D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION SystemInformation; // system thread

        //public UInt32 NodeId; // Win10
        public fixed ulong Reserved[8];
    }

    internal struct D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION
    {
        public uint PacketSubmitted;
        public uint PacketCompleted;
        public uint PacketPreempted;
        public uint PacketFaulted;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION
    {
        public uint PacketSubmited;
        public uint PacketCompleted;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS_PACKET_INFORMATION
    {
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket1;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket2;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket3;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket4;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket5;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket6;
        public D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION QueuePacket7;

        public D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION DmaPacket;
        public D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION DmaPacket1;
        public D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION DmaPacket2;
        public D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION DmaPacket3;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct D3DKMT_QUERYSTATISTICS_RESULT
    {
        [FieldOffset(8)]
        public D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION AdapterInformation;

        [FieldOffset(8)]
        public D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION SegmentInformation;

        [FieldOffset(8)]
        public D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_INFORMATION ProcessSegmentInformation;

        [FieldOffset(8)]
        public D3DKMT_QUERYSTATISTICS_NODE_INFORMATION NodeInformation;

        // D3DKMT_QUERYSTATISTICS_PROCESS_INFORMATION ProcessInformation;
        // D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION ProcessNodeInformation;
        // D3DKMT_QUERYSTATISTICS_PHYSICAL_ADAPTER_INFORMATION PhysAdapterInformation;
        // D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION_V1 SegmentInformationV1; // WIN7
        // D3DKMT_QUERYSTATISTICS_VIDPNSOURCE_INFORMATION VidPnSourceInformation;
        // D3DKMT_QUERYSTATISTICS_PROCESS_ADAPTER_INFORMATION ProcessAdapterInformation;
        // D3DKMT_QUERYSTATISTICS_PROCESS_VIDPNSOURCE_INFORMATION ProcessVidPnSourceInformation;
        // D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_GROUP_INFORMATION ProcessSegmentGroupInformation;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT
    {
        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_SEGMENT QuerySegment;

        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_SEGMENT QueryProcessSegment;

        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_NODE QueryNode;

        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_NODE QueryProcessNode;

        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_VIDPNSOURCE QueryVidPnSource;

        [FieldOffset(0)]
        public D3DKMT_QUERYSTATISTICS_QUERY_VIDPNSOURCE QueryProcessVidPnSource;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct D3DKMT_QUERYSTATISTICS
    {
        public D3DKMT_QUERYSTATISTICS_TYPE Type;
        public WinNt.LUID AdapterLuid;
        public uint ProcessHandle;
        public D3DKMT_QUERYSTATISTICS_RESULT QueryResult;
        public D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT QueryElement;
    }
        
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct D3DKMT_NODEMETADATA
    {
        public uint NodeOrdinalAndAdapterIndex;
        public D3dkmdt.DXGK_NODEMETADATA NodeData;
    }

    [Flags]
    internal enum D3DKMT_ADAPTERTYPE_FLAGS : uint
    {
        RenderSupported = 0,
        DisplaySupported = 1,
        SoftwareDevice = 2,
        PostDevice = 4,
        HybridDiscrete = 8,
        HybridIntegrated = 16,
        IndirectDisplayDevice = 32,
        Paravirtualized = 64,
        ACGSupported = 128,
        SupportSetTimingsFromVidPn = 256,
        Detachable = 512,
        ComputeOnly = 1024,
        Prototype = 2045
    }

    internal enum D3DKMT_QUERYSTATISTICS_TYPE
    {
        D3DKMT_QUERYSTATISTICS_ADAPTER,
        D3DKMT_QUERYSTATISTICS_PROCESS,
        D3DKMT_QUERYSTATISTICS_PROCESS_ADAPTER,
        D3DKMT_QUERYSTATISTICS_SEGMENT,
        D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT,
        D3DKMT_QUERYSTATISTICS_NODE,
        D3DKMT_QUERYSTATISTICS_PROCESS_NODE,
        D3DKMT_QUERYSTATISTICS_VIDPNSOURCE,
        D3DKMT_QUERYSTATISTICS_PROCESS_VIDPNSOURCE,
        D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_GROUP,
        D3DKMT_QUERYSTATISTICS_PHYSICAL_ADAPTER
    }

    internal enum D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE
    {
        D3DKMT_ClientRenderBuffer = 0,
        D3DKMT_ClientPagingBuffer = 1,
        D3DKMT_SystemPagingBuffer = 2,
        D3DKMT_SystemPreemptionBuffer = 3,
        D3DKMT_DmaPacketTypeMax
    }

    internal enum KMTQUERYADAPTERINFOTYPE
    {
        KMTQAITYPE_UMDRIVERPRIVATE = 0,
        KMTQAITYPE_UMDRIVERNAME = 1,
        KMTQAITYPE_UMOPENGLINFO = 2,
        KMTQAITYPE_GETSEGMENTSIZE = 3,
        KMTQAITYPE_ADAPTERGUID = 4,
        KMTQAITYPE_FLIPQUEUEINFO = 5,
        KMTQAITYPE_ADAPTERADDRESS = 6,
        KMTQAITYPE_SETWORKINGSETINFO = 7,
        KMTQAITYPE_ADAPTERREGISTRYINFO = 8,
        KMTQAITYPE_CURRENTDISPLAYMODE = 9,
        KMTQAITYPE_MODELIST = 10,
        KMTQAITYPE_CHECKDRIVERUPDATESTATUS = 11,
        KMTQAITYPE_VIRTUALADDRESSINFO = 12,
        KMTQAITYPE_DRIVERVERSION = 13,
        KMTQAITYPE_ADAPTERTYPE = 15,
        KMTQAITYPE_OUTPUTDUPLCONTEXTSCOUNT = 16,
        KMTQAITYPE_WDDM_1_2_CAPS = 17,
        KMTQAITYPE_UMD_DRIVER_VERSION = 18,
        KMTQAITYPE_DIRECTFLIP_SUPPORT = 19,
        KMTQAITYPE_MULTIPLANEOVERLAY_SUPPORT = 20,
        KMTQAITYPE_DLIST_DRIVER_NAME = 21,
        KMTQAITYPE_WDDM_1_3_CAPS = 22,
        KMTQAITYPE_MULTIPLANEOVERLAY_HUD_SUPPORT = 23,
        KMTQAITYPE_WDDM_2_0_CAPS = 24,
        KMTQAITYPE_NODEMETADATA = 25,
        KMTQAITYPE_CPDRIVERNAME = 26,
        KMTQAITYPE_XBOX = 27,
        KMTQAITYPE_INDEPENDENTFLIP_SUPPORT = 28,
        KMTQAITYPE_MIRACASTCOMPANIONDRIVERNAME = 29,
        KMTQAITYPE_PHYSICALADAPTERCOUNT = 30,
        KMTQAITYPE_PHYSICALADAPTERDEVICEIDS = 31,
        KMTQAITYPE_DRIVERCAPS_EXT = 32,
        KMTQAITYPE_QUERY_MIRACAST_DRIVER_TYPE = 33,
        KMTQAITYPE_QUERY_GPUMMU_CAPS = 34,
        KMTQAITYPE_QUERY_MULTIPLANEOVERLAY_DECODE_SUPPORT = 35,
        KMTQAITYPE_QUERY_HW_PROTECTION_TEARDOWN_COUNT = 36,
        KMTQAITYPE_QUERY_ISBADDRIVERFORHWPROTECTIONDISABLED = 37,
        KMTQAITYPE_MULTIPLANEOVERLAY_SECONDARY_SUPPORT = 38,
        KMTQAITYPE_INDEPENDENTFLIP_SECONDARY_SUPPORT = 39,
        KMTQAITYPE_PANELFITTER_SUPPORT = 40,
        KMTQAITYPE_PHYSICALADAPTERPNPKEY = 41,
        KMTQAITYPE_GETSEGMENTGROUPSIZE = 42,
        KMTQAITYPE_MPO3DDI_SUPPORT = 43,
        KMTQAITYPE_HWDRM_SUPPORT = 44,
        KMTQAITYPE_MPOKERNELCAPS_SUPPORT = 45,
        KMTQAITYPE_MULTIPLANEOVERLAY_STRETCH_SUPPORT = 46,
        KMTQAITYPE_GET_DEVICE_VIDPN_OWNERSHIP_INFO = 47,
        KMTQAITYPE_QUERYREGISTRY = 48,
        KMTQAITYPE_KMD_DRIVER_VERSION = 49,
        KMTQAITYPE_BLOCKLIST_KERNEL = 50,
        KMTQAITYPE_BLOCKLIST_RUNTIME = 51,
        KMTQAITYPE_ADAPTERGUID_RENDER = 52,
        KMTQAITYPE_ADAPTERADDRESS_RENDER = 53,
        KMTQAITYPE_ADAPTERREGISTRYINFO_RENDER = 54,
        KMTQAITYPE_CHECKDRIVERUPDATESTATUS_RENDER = 55,
        KMTQAITYPE_DRIVERVERSION_RENDER = 56,
        KMTQAITYPE_ADAPTERTYPE_RENDER = 57,
        KMTQAITYPE_WDDM_1_2_CAPS_RENDER = 58,
        KMTQAITYPE_WDDM_1_3_CAPS_RENDER = 59,
        KMTQAITYPE_QUERY_ADAPTER_UNIQUE_GUID = 60,
        KMTQAITYPE_NODEPERFDATA = 61,
        KMTQAITYPE_ADAPTERPERFDATA = 62,
        KMTQAITYPE_ADAPTERPERFDATA_CAPS = 63,
        KMTQUITYPE_GPUVERSION = 64,
        KMTQAITYPE_DRIVER_DESCRIPTION = 65,
        KMTQAITYPE_DRIVER_DESCRIPTION_RENDER = 66,
        KMTQAITYPE_SCANOUT_CAPS = 67,
        KMTQAITYPE_DISPLAY_UMDRIVERNAME = 71,
        KMTQAITYPE_PARAVIRTUALIZATION_RENDER = 68,
        KMTQAITYPE_SERVICENAME = 69,
        KMTQAITYPE_WDDM_2_7_CAPS = 70,
        KMTQAITYPE_TRACKEDWORKLOAD_SUPPORT = 72
    }
}
