// ported from: https://github.com/processhacker/processhacker/blob/master/plugins/ExtendedTools/gpumon.c

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware
{
    static class D3DDisplayDevice
    {
        #region D3DKMT Enums
        enum D3DKMT_QUERYSTATISTICS_TYPE
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
        enum KMTQUERYADAPTERINFOTYPE
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
            KMTQAITYPE_TRACKEDWORKLOAD_SUPPORT = 72,
        }
        enum DXGK_ENGINE_TYPE
        {
            DXGK_ENGINE_TYPE_OTHER = 0, // This value is used for proprietary or unique functionality that is not exposed by typical adapters, as well as for an engine that performs work that doesn't fall under another category.
            DXGK_ENGINE_TYPE_3D = 1, // The adapter's 3-D processing engine. All adapters that are not a display-only device have one 3-D engine.
            DXGK_ENGINE_TYPE_VIDEO_DECODE = 2, // The engine that handles video decoding, including decompression of video frames from an input stream into typical YUV surfaces. The workload packets for an H.264 video codec workload test must appear on either the decode engine or the 3-D engine.
            DXGK_ENGINE_TYPE_VIDEO_ENCODE = 3, // The engine that handles video encoding, including compression of typical video frames into an encoded video format.
            DXGK_ENGINE_TYPE_VIDEO_PROCESSING = 4, // The engine that is responsible for any video processing that is done after a video input stream is decoded. Such processing can include RGB surface conversion, filtering, stretching, color correction, deinterlacing, or other steps that are required before the final image is rendered to the display screen. The workload packets for workload tests must appear on either the video processing engine or the 3-D engine.
            DXGK_ENGINE_TYPE_SCENE_ASSEMBLY = 5, // The engine that performs vertex processing of 3-D workloads as a preliminary pass prior to the remainder of the 3-D rendering. This engine also stores vertices in bins that tile-based rendering engines use.
            DXGK_ENGINE_TYPE_COPY = 6, // The engine that is a copy engine used for moving data. This engine can perform subresource updates, blitting, paging, or other similar data handling. The workload packets for calls to CopySubresourceRegion or UpdateSubResource methods of Direct3D 10 and Direct3D 11 must appear on either the copy engine or the 3-D engine.
            DXGK_ENGINE_TYPE_OVERLAY = 7, // The virtual engine that is used for synchronized flipping of overlays in Direct3D 9.
            DXGK_ENGINE_TYPE_CRYPTO,
            DXGK_ENGINE_TYPE_MAX
        };
        enum D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE
        {
            D3DKMT_ClientRenderBuffer = 0,
            D3DKMT_ClientPagingBuffer = 1,
            D3DKMT_SystemPagingBuffer = 2,
            D3DKMT_SystemPreemptionBuffer = 3,
            D3DKMT_DmaPacketTypeMax
        }
        enum D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE
        {
            D3DKMT_RenderCommandBuffer = 0,
            D3DKMT_DeferredCommandBuffer = 1,
            D3DKMT_SystemCommandBuffer = 2,
            D3DKMT_MmIoFlipCommandBuffer = 3,
            D3DKMT_WaitCommandBuffer = 4,
            D3DKMT_SignalCommandBuffer = 5,
            D3DKMT_DeviceCommandBuffer = 6,
            D3DKMT_SoftwareCommandBuffer = 7,
            D3DKMT_QueuePacketTypeMax
        }
        enum D3DKMT_QUERYRESULT_PREEMPTION_ATTEMPT_RESULT
        {
            D3DKMT_PreemptionAttempt = 0,
            D3DKMT_PreemptionAttemptSuccess = 1,
            D3DKMT_PreemptionAttemptMissNoCommand = 2,
            D3DKMT_PreemptionAttemptMissNotEnabled = 3,
            D3DKMT_PreemptionAttemptMissNextFence = 4,
            D3DKMT_PreemptionAttemptMissPagingCommand = 5,
            D3DKMT_PreemptionAttemptMissSplittedCommand = 6,
            D3DKMT_PreemptionAttemptMissFenceCommand = 7,
            D3DKMT_PreemptionAttemptMissRenderPendingFlip = 8,
            D3DKMT_PreemptionAttemptMissNotMakingProgress = 9,
            D3DKMT_PreemptionAttemptMissLessPriority = 10,
            D3DKMT_PreemptionAttemptMissRemainingQuantum = 11,
            D3DKMT_PreemptionAttemptMissRemainingPreemptionQuantum = 12,
            D3DKMT_PreemptionAttemptMissAlreadyPreempting = 13,
            D3DKMT_PreemptionAttemptMissGlobalBlock = 14,
            D3DKMT_PreemptionAttemptMissAlreadyRunning = 15,
            D3DKMT_PreemptionAttemptStatisticsMax
        }
        enum NtStatus : uint
        {
            // Success
            Success = 0x00000000,
            Wait0 = 0x00000000,
            Wait1 = 0x00000001,
            Wait2 = 0x00000002,
            Wait3 = 0x00000003,
            Wait63 = 0x0000003f,
            Abandoned = 0x00000080,
            AbandonedWait0 = 0x00000080,
            AbandonedWait1 = 0x00000081,
            AbandonedWait2 = 0x00000082,
            AbandonedWait3 = 0x00000083,
            AbandonedWait63 = 0x000000bf,
            UserApc = 0x000000c0,
            KernelApc = 0x00000100,
            Alerted = 0x00000101,
            Timeout = 0x00000102,
            Pending = 0x00000103,
            Reparse = 0x00000104,
            MoreEntries = 0x00000105,
            NotAllAssigned = 0x00000106,
            SomeNotMapped = 0x00000107,
            OpLockBreakInProgress = 0x00000108,
            VolumeMounted = 0x00000109,
            RxActCommitted = 0x0000010a,
            NotifyCleanup = 0x0000010b,
            NotifyEnumDir = 0x0000010c,
            NoQuotasForAccount = 0x0000010d,
            PrimaryTransportConnectFailed = 0x0000010e,
            PageFaultTransition = 0x00000110,
            PageFaultDemandZero = 0x00000111,
            PageFaultCopyOnWrite = 0x00000112,
            PageFaultGuardPage = 0x00000113,
            PageFaultPagingFile = 0x00000114,
            CrashDump = 0x00000116,
            ReparseObject = 0x00000118,
            NothingToTerminate = 0x00000122,
            ProcessNotInJob = 0x00000123,
            ProcessInJob = 0x00000124,
            ProcessCloned = 0x00000129,
            FileLockedWithOnlyReaders = 0x0000012a,
            FileLockedWithWriters = 0x0000012b,

            // Informational
            Informational = 0x40000000,
            ObjectNameExists = 0x40000000,
            ThreadWasSuspended = 0x40000001,
            WorkingSetLimitRange = 0x40000002,
            ImageNotAtBase = 0x40000003,
            RegistryRecovered = 0x40000009,

            // Warning
            Warning = 0x80000000,
            GuardPageViolation = 0x80000001,
            DatatypeMisalignment = 0x80000002,
            Breakpoint = 0x80000003,
            SingleStep = 0x80000004,
            BufferOverflow = 0x80000005,
            NoMoreFiles = 0x80000006,
            HandlesClosed = 0x8000000a,
            PartialCopy = 0x8000000d,
            DeviceBusy = 0x80000011,
            InvalidEaName = 0x80000013,
            EaListInconsistent = 0x80000014,
            NoMoreEntries = 0x8000001a,
            LongJump = 0x80000026,
            DllMightBeInsecure = 0x8000002b,

            // Error
            Error = 0xc0000000,
            Unsuccessful = 0xc0000001,
            NotImplemented = 0xc0000002,
            InvalidInfoClass = 0xc0000003,
            InfoLengthMismatch = 0xc0000004,
            AccessViolation = 0xc0000005,
            InPageError = 0xc0000006,
            PagefileQuota = 0xc0000007,
            InvalidHandle = 0xc0000008,
            BadInitialStack = 0xc0000009,
            BadInitialPc = 0xc000000a,
            InvalidCid = 0xc000000b,
            TimerNotCanceled = 0xc000000c,
            InvalidParameter = 0xc000000d,
            NoSuchDevice = 0xc000000e,
            NoSuchFile = 0xc000000f,
            InvalidDeviceRequest = 0xc0000010,
            EndOfFile = 0xc0000011,
            WrongVolume = 0xc0000012,
            NoMediaInDevice = 0xc0000013,
            NoMemory = 0xc0000017,
            NotMappedView = 0xc0000019,
            UnableToFreeVm = 0xc000001a,
            UnableToDeleteSection = 0xc000001b,
            IllegalInstruction = 0xc000001d,
            AlreadyCommitted = 0xc0000021,
            AccessDenied = 0xc0000022,
            BufferTooSmall = 0xc0000023,
            ObjectTypeMismatch = 0xc0000024,
            NonContinuableException = 0xc0000025,
            BadStack = 0xc0000028,
            NotLocked = 0xc000002a,
            NotCommitted = 0xc000002d,
            InvalidParameterMix = 0xc0000030,
            ObjectNameInvalid = 0xc0000033,
            ObjectNameNotFound = 0xc0000034,
            ObjectNameCollision = 0xc0000035,
            ObjectPathInvalid = 0xc0000039,
            ObjectPathNotFound = 0xc000003a,
            ObjectPathSyntaxBad = 0xc000003b,
            DataOverrun = 0xc000003c,
            DataLate = 0xc000003d,
            DataError = 0xc000003e,
            CrcError = 0xc000003f,
            SectionTooBig = 0xc0000040,
            PortConnectionRefused = 0xc0000041,
            InvalidPortHandle = 0xc0000042,
            SharingViolation = 0xc0000043,
            QuotaExceeded = 0xc0000044,
            InvalidPageProtection = 0xc0000045,
            MutantNotOwned = 0xc0000046,
            SemaphoreLimitExceeded = 0xc0000047,
            PortAlreadySet = 0xc0000048,
            SectionNotImage = 0xc0000049,
            SuspendCountExceeded = 0xc000004a,
            ThreadIsTerminating = 0xc000004b,
            BadWorkingSetLimit = 0xc000004c,
            IncompatibleFileMap = 0xc000004d,
            SectionProtection = 0xc000004e,
            EasNotSupported = 0xc000004f,
            EaTooLarge = 0xc0000050,
            NonExistentEaEntry = 0xc0000051,
            NoEasOnFile = 0xc0000052,
            EaCorruptError = 0xc0000053,
            FileLockConflict = 0xc0000054,
            LockNotGranted = 0xc0000055,
            DeletePending = 0xc0000056,
            CtlFileNotSupported = 0xc0000057,
            UnknownRevision = 0xc0000058,
            RevisionMismatch = 0xc0000059,
            InvalidOwner = 0xc000005a,
            InvalidPrimaryGroup = 0xc000005b,
            NoImpersonationToken = 0xc000005c,
            CantDisableMandatory = 0xc000005d,
            NoLogonServers = 0xc000005e,
            NoSuchLogonSession = 0xc000005f,
            NoSuchPrivilege = 0xc0000060,
            PrivilegeNotHeld = 0xc0000061,
            InvalidAccountName = 0xc0000062,
            UserExists = 0xc0000063,
            NoSuchUser = 0xc0000064,
            GroupExists = 0xc0000065,
            NoSuchGroup = 0xc0000066,
            MemberInGroup = 0xc0000067,
            MemberNotInGroup = 0xc0000068,
            LastAdmin = 0xc0000069,
            WrongPassword = 0xc000006a,
            IllFormedPassword = 0xc000006b,
            PasswordRestriction = 0xc000006c,
            LogonFailure = 0xc000006d,
            AccountRestriction = 0xc000006e,
            InvalidLogonHours = 0xc000006f,
            InvalidWorkstation = 0xc0000070,
            PasswordExpired = 0xc0000071,
            AccountDisabled = 0xc0000072,
            NoneMapped = 0xc0000073,
            TooManyLuidsRequested = 0xc0000074,
            LuidsExhausted = 0xc0000075,
            InvalidSubAuthority = 0xc0000076,
            InvalidAcl = 0xc0000077,
            InvalidSid = 0xc0000078,
            InvalidSecurityDescr = 0xc0000079,
            ProcedureNotFound = 0xc000007a,
            InvalidImageFormat = 0xc000007b,
            NoToken = 0xc000007c,
            BadInheritanceAcl = 0xc000007d,
            RangeNotLocked = 0xc000007e,
            DiskFull = 0xc000007f,
            ServerDisabled = 0xc0000080,
            ServerNotDisabled = 0xc0000081,
            TooManyGuidsRequested = 0xc0000082,
            GuidsExhausted = 0xc0000083,
            InvalidIdAuthority = 0xc0000084,
            AgentsExhausted = 0xc0000085,
            InvalidVolumeLabel = 0xc0000086,
            SectionNotExtended = 0xc0000087,
            NotMappedData = 0xc0000088,
            ResourceDataNotFound = 0xc0000089,
            ResourceTypeNotFound = 0xc000008a,
            ResourceNameNotFound = 0xc000008b,
            ArrayBoundsExceeded = 0xc000008c,
            FloatDenormalOperand = 0xc000008d,
            FloatDivideByZero = 0xc000008e,
            FloatInexactResult = 0xc000008f,
            FloatInvalidOperation = 0xc0000090,
            FloatOverflow = 0xc0000091,
            FloatStackCheck = 0xc0000092,
            FloatUnderflow = 0xc0000093,
            IntegerDivideByZero = 0xc0000094,
            IntegerOverflow = 0xc0000095,
            PrivilegedInstruction = 0xc0000096,
            TooManyPagingFiles = 0xc0000097,
            FileInvalid = 0xc0000098,
            InstanceNotAvailable = 0xc00000ab,
            PipeNotAvailable = 0xc00000ac,
            InvalidPipeState = 0xc00000ad,
            PipeBusy = 0xc00000ae,
            IllegalFunction = 0xc00000af,
            PipeDisconnected = 0xc00000b0,
            PipeClosing = 0xc00000b1,
            PipeConnected = 0xc00000b2,
            PipeListening = 0xc00000b3,
            InvalidReadMode = 0xc00000b4,
            IoTimeout = 0xc00000b5,
            FileForcedClosed = 0xc00000b6,
            ProfilingNotStarted = 0xc00000b7,
            ProfilingNotStopped = 0xc00000b8,
            NotSameDevice = 0xc00000d4,
            FileRenamed = 0xc00000d5,
            CantWait = 0xc00000d8,
            PipeEmpty = 0xc00000d9,
            CantTerminateSelf = 0xc00000db,
            InternalError = 0xc00000e5,
            InvalidParameter1 = 0xc00000ef,
            InvalidParameter2 = 0xc00000f0,
            InvalidParameter3 = 0xc00000f1,
            InvalidParameter4 = 0xc00000f2,
            InvalidParameter5 = 0xc00000f3,
            InvalidParameter6 = 0xc00000f4,
            InvalidParameter7 = 0xc00000f5,
            InvalidParameter8 = 0xc00000f6,
            InvalidParameter9 = 0xc00000f7,
            InvalidParameter10 = 0xc00000f8,
            InvalidParameter11 = 0xc00000f9,
            InvalidParameter12 = 0xc00000fa,
            MappedFileSizeZero = 0xc000011e,
            TooManyOpenedFiles = 0xc000011f,
            Cancelled = 0xc0000120,
            CannotDelete = 0xc0000121,
            InvalidComputerName = 0xc0000122,
            FileDeleted = 0xc0000123,
            SpecialAccount = 0xc0000124,
            SpecialGroup = 0xc0000125,
            SpecialUser = 0xc0000126,
            MembersPrimaryGroup = 0xc0000127,
            FileClosed = 0xc0000128,
            TooManyThreads = 0xc0000129,
            ThreadNotInProcess = 0xc000012a,
            TokenAlreadyInUse = 0xc000012b,
            PagefileQuotaExceeded = 0xc000012c,
            CommitmentLimit = 0xc000012d,
            InvalidImageLeFormat = 0xc000012e,
            InvalidImageNotMz = 0xc000012f,
            InvalidImageProtect = 0xc0000130,
            InvalidImageWin16 = 0xc0000131,
            LogonServer = 0xc0000132,
            DifferenceAtDc = 0xc0000133,
            SynchronizationRequired = 0xc0000134,
            DllNotFound = 0xc0000135,
            IoPrivilegeFailed = 0xc0000137,
            OrdinalNotFound = 0xc0000138,
            EntryPointNotFound = 0xc0000139,
            ControlCExit = 0xc000013a,
            PortNotSet = 0xc0000353,
            DebuggerInactive = 0xc0000354,
            CallbackBypass = 0xc0000503,
            PortClosed = 0xc0000700,
            MessageLost = 0xc0000701,
            InvalidMessage = 0xc0000702,
            RequestCanceled = 0xc0000703,
            RecursiveDispatch = 0xc0000704,
            LpcReceiveBufferExpected = 0xc0000705,
            LpcInvalidConnectionUsage = 0xc0000706,
            LpcRequestsNotAllowed = 0xc0000707,
            ResourceInUse = 0xc0000708,
            ProcessIs = 0xc0000712,
            VolumeDirty = 0xc0000806,
            FileCheckedOut = 0xc0000901,
            CheckOutRequired = 0xc0000902,
            BadFileType = 0xc0000903,
            FileTooLarge = 0xc0000904,
            FormsAuthRequired = 0xc0000905,
            VirusInfected = 0xc0000906,
            VirusDeleted = 0xc0000907,
            TransactionalConflict = 0xc0190001,
            InvalidTransaction = 0xc0190002,
            TransactionNotActive = 0xc0190003,
            TmInitializationFailed = 0xc0190004,
            RmNotActive = 0xc0190005,
            RmMetadataCorrupt = 0xc0190006,
            TransactionNotJoined = 0xc0190007,
            DirectoryNotRm = 0xc0190008,
            CouldNotResizeLog = 0xc0190009,
            TransactionsUnsupportedRemote = 0xc019000a,
            LogResizeInvalidSize = 0xc019000b,
            RemoteFileVersionMismatch = 0xc019000c,
            CrmProtocolAlreadyExists = 0xc019000f,
            TransactionPropagationFailed = 0xc0190010,
            CrmProtocolNotFound = 0xc0190011,
            TransactionSuperiorExists = 0xc0190012,
            TransactionRequestNotValid = 0xc0190013,
            TransactionNotRequested = 0xc0190014,
            TransactionAlreadyAborted = 0xc0190015,
            TransactionAlreadyCommitted = 0xc0190016,
            TransactionInvalidMarshallBuffer = 0xc0190017,
            CurrentTransactionNotValid = 0xc0190018,
            LogGrowthFailed = 0xc0190019,
            ObjectNoLongerExists = 0xc0190021,
            StreamMiniversionNotFound = 0xc0190022,
            StreamMiniversionNotValid = 0xc0190023,
            MiniversionInaccessibleFromSpecifiedTransaction = 0xc0190024,
            CantOpenMiniversionWithModifyIntent = 0xc0190025,
            CantCreateMoreStreamMiniversions = 0xc0190026,
            HandleNoLongerValid = 0xc0190028,
            NoTxfMetadata = 0xc0190029,
            LogCorruptionDetected = 0xc0190030,
            CantRecoverWithHandleOpen = 0xc0190031,
            RmDisconnected = 0xc0190032,
            EnlistmentNotSuperior = 0xc0190033,
            RecoveryNotNeeded = 0xc0190034,
            RmAlreadyStarted = 0xc0190035,
            FileIdentityNotPersistent = 0xc0190036,
            CantBreakTransactionalDependency = 0xc0190037,
            CantCrossRmBoundary = 0xc0190038,
            TxfDirNotEmpty = 0xc0190039,
            IndoubtTransactionsExist = 0xc019003a,
            TmVolatile = 0xc019003b,
            RollbackTimerExpired = 0xc019003c,
            TxfAttributeCorrupt = 0xc019003d,
            EfsNotAllowedInTransaction = 0xc019003e,
            TransactionalOpenNotAllowed = 0xc019003f,
            TransactedMappingUnsupportedRemote = 0xc0190040,
            TxfMetadataAlreadyPresent = 0xc0190041,
            TransactionScopeCallbacksNotSet = 0xc0190042,
            TransactionRequiredPromotion = 0xc0190043,
            CannotExecuteFileInTransaction = 0xc0190044,
            TransactionsNotFrozen = 0xc0190045,

            MaximumNtStatus = 0xffffffff
        }

        #endregion

        #region D3DKMT structs
        [StructLayout(LayoutKind.Sequential)]
        struct LUID
        {
            public readonly uint LowPart;
            public readonly int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_CLOSEADAPTER
        {
            public uint hAdapter;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYADAPTERINFO
        {
            public uint hAdapter;
            public KMTQUERYADAPTERINFOTYPE Type;
            public IntPtr pPrivateDriverData;
            public int PrivateDriverDataSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_OPENADAPTERFROMDEVICENAME
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDeviceName;
            public UInt32 hAdapter;
            public LUID AdapterLuid;
        }

        [Flags]
        enum D3DKMT_ADAPTERTYPE_Flags : UInt32
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

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_ADAPTERTYPE
        {
            public D3DKMT_ADAPTERTYPE_Flags Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_SEGMENTSIZEINFO
        {
            public UInt64 DedicatedVideoMemorySize;
            public UInt64 DedicatedSystemMemorySize;
            public UInt64 SharedSystemMemorySize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_QUERY_SEGMENT
        {
            public UInt32 SegmentId;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_QUERY_NODE
        {
            public UInt32 NodeId;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_QUERY_VIDPNSOURCE
        {
            public UInt32 VidPnSourceId;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATSTICS_REFERENCE_DMA_BUFFER
        {
            public UInt32 NbCall;
            public UInt32 NbAllocationsReferenced;
            public UInt32 MaxNbAllocationsReferenced;
            public UInt32 NbNULLReference;
            public UInt32 NbWriteReference;
            public UInt32 NbRenamedAllocationsReferenced;
            public UInt32 NbIterationSearchingRenamedAllocation;
            public UInt32 NbLockedAllocationReferenced;
            public UInt32 NbAllocationWithValidPrepatchingInfoReferenced;
            public UInt32 NbAllocationWithInvalidPrepatchingInfoReferenced;
            public UInt32 NbDMABufferSuccessfullyPrePatched;
            public UInt32 NbPrimariesReferencesOverflow;
            public UInt32 NbAllocationWithNonPreferredResources;
            public UInt32 NbAllocationInsertedInMigrationTable;
        };

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATSTICS_RENAMING
        {
            public UInt32 NbAllocationsRenamed;
            public UInt32 NbAllocationsShrinked;
            public UInt32 NbRenamedBuffer;
            public UInt32 MaxRenamingListLength;
            public UInt32 NbFailuresDueToRenamingLimit;
            public UInt32 NbFailuresDueToCreateAllocation;
            public UInt32 NbFailuresDueToOpenAllocation;
            public UInt32 NbFailuresDueToLowResource;
            public UInt32 NbFailuresDueToNonRetiredLimit;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_COUNTER
        {
            public UInt32 Count;
            public UInt64 Bytes;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATSTICS_PREPRATION
        {
            public UInt32 BroadcastStall;
            public UInt32 NbDMAPrepared;
            public UInt32 NbDMAPreparedLongPath;
            public UInt32 ImmediateHighestPreparationPass;
            public D3DKMT_QUERYSTATISTICS_COUNTER AllocationsTrimmed;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATSTICS_PAGING_FAULT
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
            public UInt32 AllocationsUnresetSuccessRead;
            public UInt32 AllocationsUnresetFailRead;

            public D3DKMT_QUERYSTATISTICS_COUNTER Evictions;
            public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToPreparation;
            public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToLock;
            public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToClose;
            public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToPurge;
            public D3DKMT_QUERYSTATISTICS_COUNTER EvictionsDueToSuspendCPUAccess;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATSTICS_PAGING_TRANSFER
        {
            public UInt64 BytesFilled;
            public UInt64 BytesDiscarded;
            public UInt64 BytesMappedIntoAperture;
            public UInt64 BytesUnmappedFromAperture;
            public UInt64 BytesTransferredFromMdlToMemory;
            public UInt64 BytesTransferredFromMemoryToMdl;
            public UInt64 BytesTransferredFromApertureToMemory;
            public UInt64 BytesTransferredFromMemoryToAperture;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATSTICS_SWIZZLING_RANGE
        {
            public UInt32 NbRangesAcquired;
            public UInt32 NbRangesReleased;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATSTICS_LOCKS
        {
            public UInt32 NbLocks;
            public UInt32 NbLocksWaitFlag;
            public UInt32 NbLocksDiscardFlag;
            public UInt32 NbLocksNoOverwrite;
            public UInt32 NbLocksNoReadSync;
            public UInt32 NbLocksLinearization;
            public UInt32 NbComplexLocks;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATSTICS_ALLOCATIONS
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
        struct D3DKMT_QUERYSTATSTICS_TERMINATIONS
        {
            public D3DKMT_QUERYSTATISTICS_COUNTER TerminatedShared;
            public D3DKMT_QUERYSTATISTICS_COUNTER TerminatedNonShared;
            public D3DKMT_QUERYSTATISTICS_COUNTER DestroyedShared;
            public D3DKMT_QUERYSTATISTICS_COUNTER DestroyedNonShared;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION
        {
            public UInt32 NbSegments;
            public UInt32 NodeCount;
            public UInt32 VidPnSourceCount;

            public UInt32 VSyncEnabled;
            public UInt32 TdrDetectedCount;

            public Int64 ZeroLengthDmaBuffers;
            public UInt64 RestartedPeriod;

            public D3DKMT_QUERYSTATSTICS_REFERENCE_DMA_BUFFER ReferenceDmaBuffer;
            public D3DKMT_QUERYSTATSTICS_RENAMING Renaming;
            public D3DKMT_QUERYSTATSTICS_PREPRATION Preparation;
            public D3DKMT_QUERYSTATSTICS_PAGING_FAULT PagingFault;
            public D3DKMT_QUERYSTATSTICS_PAGING_TRANSFER PagingTransfer;
            public D3DKMT_QUERYSTATSTICS_SWIZZLING_RANGE SwizzlingRange;
            public D3DKMT_QUERYSTATSTICS_LOCKS Locks;
            public D3DKMT_QUERYSTATSTICS_ALLOCATIONS Allocations;
            public D3DKMT_QUERYSTATSTICS_TERMINATIONS Terminations;

            private UInt64 Reserved;
            private UInt64 Reserved1;
            private UInt64 Reserved2;
            private UInt64 Reserved3;
            private UInt64 Reserved4;
            private UInt64 Reserved5;
            private UInt64 Reserved6;
            private UInt64 Reserved7;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION_PowerFlags
        {
            [FieldOffset(0)]
            public UInt64 PreservedDuringStandby;
            [FieldOffset(1)]
            public UInt64 PreservedDuringHibernate;
            [FieldOffset(2)]
            public UInt64 PartiallyPreservedDuringHibernate;
            [FieldOffset(3)]
            public UInt64 Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_MEMORY
        {
            public UInt64 TotalBytesEvicted;
            public UInt32 AllocsCommitted;
            public UInt32 AllocsResident;
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION
        {
            public UInt64 CommitLimit;
            public UInt64 BytesCommitted;
            public UInt64 BytesResident;

            D3DKMT_QUERYSTATISTICS_MEMORY Memory;

            public UInt32 Aperture; // boolean

            public fixed UInt64 TotalBytesEvictedByPriority[5]; // D3DKMT_QUERYSTATISTICS_SEGMENT_PREFERENCE_MAX

            public UInt64 SystemMemoryEndAddress;
            public D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION_PowerFlags PowerFlags;

            public fixed UInt64 Reserved[6];
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_VIDEO_MEMORY
        {
            public UInt32 AllocsCommitted;
            public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn0;
            public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn1;
            public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn2;
            public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn3;
            public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentIn4;
            public D3DKMT_QUERYSTATISTICS_COUNTER AllocsResidentInNonPreferred;
            public UInt64 TotalBytesEvictedDueToPreparation;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_POLICY
        {
            public UInt64 UseMRU;
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_INFORMATION
        {
            public UInt64 BytesCommitted;
            public UInt64 MaximumWorkingSet;
            public UInt64 MinimumWorkingSet;

            public UInt32 NbReferencedAllocationEvictedInPeriod;

            public D3DKMT_QUERYSTATISTICS_VIDEO_MEMORY VideoMemory;
            public D3DKMT_QUERYSTATISTICS_PROCESS_SEGMENT_POLICY _Policy;

            public fixed UInt64 Reserved[8];
        }


        [StructLayout(LayoutKind.Sequential)]
        struct DXGK_NODEMETADATA_FLAGS
        {
            public UInt32 Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_PREEMPTION_INFORMATION
        {
            public UInt32 PreemptionCounter;
            public UInt32 PreemptionCounter1;
            public UInt32 PreemptionCounter2;
            public UInt32 PreemptionCounter3;
            public UInt32 PreemptionCounter4;
            public UInt32 PreemptionCounter5;
            public UInt32 PreemptionCounter6;
            public UInt32 PreemptionCounter7;
            public UInt32 PreemptionCounter8;
            public UInt32 PreemptionCounter9;
            public UInt32 PreemptionCounter10;
            public UInt32 PreemptionCounter11;
            public UInt32 PreemptionCounter12;
            public UInt32 PreemptionCounter13;
            public UInt32 PreemptionCounter14;
            public UInt32 PreemptionCounter15;
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        struct LARGE_INTEGER
        {
            [FieldOffset(0)]
            public Int64 QuadPart;
            [FieldOffset(0)]
            public UInt32 LowPart;
            [FieldOffset(4)]
            public Int32 HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION
        {
            public LARGE_INTEGER RunningTime; // 100ns
            public UInt32 ContextSwitch;
            D3DKMT_QUERYSTATISTICS_PREEMPTION_INFORMATION PreemptionStatistics;
            D3DKMT_QUERYSTATISTICS_PACKET_INFORMATION PacketStatistics;
            private fixed UInt64 Reserved[8];
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct D3DKMT_QUERYSTATISTICS_NODE_INFORMATION
        {
            public D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION GlobalInformation; // global
            public D3DKMT_QUERYSTATISTICS_PROCESS_NODE_INFORMATION SystemInformation; // system thread
                                                                                      //public UInt32 NodeId; // Win10
            public fixed UInt64 Reserved[8];
        }

        struct D3DKMT_QUERYSTATISTICS_DMA_PACKET_TYPE_INFORMATION
        {
            public UInt32 PacketSubmited;
            public UInt32 PacketCompleted;
            public UInt32 PacketPreempted;
            public UInt32 PacketFaulted;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_QUEUE_PACKET_TYPE_INFORMATION
        {
            public UInt32 PacketSubmited;
            public UInt32 PacketCompleted;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS_PACKET_INFORMATION
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
        struct D3DKMT_QUERYSTATISTICS_RESULT
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
        struct D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT
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
        };

        [StructLayout(LayoutKind.Sequential)]
        struct D3DKMT_QUERYSTATISTICS
        {
            public D3DKMT_QUERYSTATISTICS_TYPE Type;
            public LUID AdapterLuid;
            public UInt32 ProcessHandle;
            public D3DKMT_QUERYSTATISTICS_RESULT QueryResult;
            public D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT QueryElement;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        struct DXGK_NODEMETADATA
        {
            public DXGK_ENGINE_TYPE EngineType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FriendlyName;
            public DXGK_NODEMETADATA_FLAGS Flags;
            public Byte GpuMmuSupported;
            public Byte IoMmuSupported;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DXGKDDI_GETNODEMETADATA
        {
            public uint hAdapter;
            public UInt32 NodeOrdinalAndAdapterIndex;
            public IntPtr pGetNodeMetadata;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct D3DKMT_NODEMETADATA
        {
            public UInt32 NodeOrdinalAndAdapterIndex;
            public DXGK_NODEMETADATA NodeData;
        }

        #endregion

        #region DLL imports
        [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
        static extern NtStatus CM_Get_Device_Interface_List_Size(out uint size, ref Guid interfaceClassGuid, string deviceID, uint flags);

        [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
        static extern NtStatus CM_Get_Device_Interface_List(ref Guid interfaceClassGuid, string deviceID, char[] buffer, uint bufferLength, uint flags);


        [DllImport("gdi32", ExactSpelling = true)]
        static extern NtStatus D3DKMTCloseAdapter(ref D3DKMT_CLOSEADAPTER unnamed_0);
        [DllImport("gdi32", ExactSpelling = true)]
        static extern NtStatus D3DKMTOpenAdapterFromDeviceName(ref D3DKMT_OPENADAPTERFROMDEVICENAME unnamed_0);

        [DllImport("gdi32", ExactSpelling = true)]
        static extern NtStatus D3DKMTQueryAdapterInfo(ref D3DKMT_QUERYADAPTERINFO unnamed__0);

        [DllImport("gdi32", ExactSpelling = true)]
        static extern NtStatus D3DKMTQueryStatistics(ref D3DKMT_QUERYSTATISTICS unnamed__0);
        #endregion

        #region Constants
        const uint CM_GET_DEVICE_INTERFACE_LIST_PRESENT = 0x0;
        const int CR_SUCCESS = 0x0;
        static Guid GUID_DISPLAY_DEVICE_ARRIVAL = new Guid("1CA05180-A699-450A-9A0C-DE4FBE3DDD89");
        #endregion
        public struct D3DDeviceNodeInfo
        {
            public UInt64 Id;
            public string Name;
            public Int64 RunningTime;
            public DateTime QueryTime;
        }

        public struct D3DDeviceInfo
        {
            public UInt64 GpuSharedLimit;
            public UInt64 GpuDedicatedLimit;

            public UInt64 GpuSharedUsed;
            public UInt64 GpuDedicatedUsed;

            public UInt64 GpuSharedMax;
            public UInt64 GpuDedicatedMax;

            public D3DDeviceNodeInfo[] Nodes;
        }

        public static string[] GetDisplayDeviceNames()
        {
            var cr = CM_Get_Device_Interface_List_Size(out uint size, ref GUID_DISPLAY_DEVICE_ARRIVAL, null, CM_GET_DEVICE_INTERFACE_LIST_PRESENT);
            {
                char[] data = new char[size];
                cr = CM_Get_Device_Interface_List(ref GUID_DISPLAY_DEVICE_ARRIVAL, null, data, (uint)data.Length, CM_GET_DEVICE_INTERFACE_LIST_PRESENT);
                if (cr == CR_SUCCESS)
                    return new string(data).Split('\0').ToList().Where(m => !string.IsNullOrEmpty(m)).ToArray();
            }
            return null;
        }

        public static bool GetDeviceInfoByName(string displayDeviceName, out D3DDeviceInfo deviceInfo)
        {
            deviceInfo = new D3DDeviceInfo();

            NtStatus status;
            D3DKMT_OPENADAPTERFROMDEVICENAME adapter;
            OpenAdapterFromDeviceName(out status, displayDeviceName, out adapter);
            if (status != NtStatus.Success) return false;

            D3DKMT_ADAPTERTYPE adapterType;
            GetAdapterType(out status, adapter, out adapterType);
            if (status != NtStatus.Success) return false;

            if (!adapterType.Value.HasFlag(D3DKMT_ADAPTERTYPE_Flags.SoftwareDevice)) return false;

            D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation;
            GetQueryStatisticsAdapterInformation(out status, adapter, out adapterInformation);
            if (status != NtStatus.Success) return false;

            uint segmentCount = adapterInformation.NbSegments;
            uint nodeCount = adapterInformation.NodeCount;

            deviceInfo.Nodes = new D3DDeviceNodeInfo[nodeCount];

            for (uint nodeId = 0; nodeId < nodeCount; nodeId++)
            {
                D3DKMT_NODEMETADATA nodeMetaData;
                GetNodeMetaData(out status, adapter, nodeId, out nodeMetaData);
                if (status != NtStatus.Success) return false;

                D3DKMT_QUERYSTATISTICS_NODE_INFORMATION nodeInformation;
                GetQueryStatisticsNode(out status, adapter, nodeId, out nodeInformation);
                if (status != NtStatus.Success) return false;

                deviceInfo.Nodes[nodeId] = new D3DDeviceNodeInfo()
                {
                    Id = nodeId,
                    Name = GetNodeEngineTypeString(nodeMetaData),
                    RunningTime = nodeInformation.GlobalInformation.RunningTime.QuadPart,
                    QueryTime = DateTime.Now
                };
            }

            for (uint segmentId = 0; segmentId < segmentCount; segmentId++)
            {
                D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation;
                GetQueryStatisticsSegment(out status, adapter, segmentId, out segmentInformation);
                if (status != NtStatus.Success) return false;

                UInt64 commitLimit = segmentInformation.CommitLimit;
                UInt64 bytesResident = segmentInformation.BytesResident;
                UInt64 bytesCommitted = segmentInformation.BytesCommitted;

                UInt32 aperture = segmentInformation.Aperture;

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
            if (status != NtStatus.Success) return false;

            return true;
        }

        static string GetNodeEngineTypeString(D3DKMT_NODEMETADATA nodeMetaData)
        {
            switch (nodeMetaData.NodeData.EngineType)
            {
                case DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_OTHER:
                    return nodeMetaData.NodeData.FriendlyName;
                case DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_3D:
                    return "3D";
                case DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_DECODE:
                    return "Video Decode";
                case DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_ENCODE:
                    return "Video Encode";
                case DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_VIDEO_PROCESSING:
                    return "Video Processing";
                case DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_SCENE_ASSEMBLY:
                    return "Scene Assembly";
                case DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_COPY:
                    return "Copy";
                case DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_OVERLAY:
                    return "Overlay";
                case DXGK_ENGINE_TYPE.DXGK_ENGINE_TYPE_CRYPTO:
                    return "Crypto";
                default:
                    return "Unknown";
            }
        }

        static void GetNodeMetaData(out NtStatus status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint nodeId, out D3DKMT_NODEMETADATA nodeMetaDataResult)
        {
            IntPtr nodeMetaDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(D3DKMT_NODEMETADATA)));
            nodeMetaDataResult = new D3DKMT_NODEMETADATA()
            {
                NodeOrdinalAndAdapterIndex = nodeId
            };
            Marshal.StructureToPtr(nodeMetaDataResult, nodeMetaDataPtr, true);

            var queryAdapterInfo = new D3DKMT_QUERYADAPTERINFO()
            {
                hAdapter = adapter.hAdapter,
                Type = KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_NODEMETADATA,
                pPrivateDriverData = nodeMetaDataPtr,
                PrivateDriverDataSize = Marshal.SizeOf(typeof(D3DKMT_NODEMETADATA))
            };
            status = D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
            nodeMetaDataResult = Marshal.PtrToStructure<D3DKMT_NODEMETADATA>(nodeMetaDataPtr);
            Marshal.FreeHGlobal(nodeMetaDataPtr);
            nodeMetaDataPtr = IntPtr.Zero;
        }

        static void GetQueryStatisticsNode(out NtStatus status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint nodeId, out D3DKMT_QUERYSTATISTICS_NODE_INFORMATION nodeInformation)
        {
            var queryElement = new D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT();
            queryElement.QueryNode.NodeId = nodeId;

            var queryStatistics = new D3DKMT_QUERYSTATISTICS()
            {
                AdapterLuid = adapter.AdapterLuid,
                Type = D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_NODE,
                QueryElement = queryElement
            };

            status = D3DKMTQueryStatistics(ref queryStatistics);

            nodeInformation = queryStatistics.QueryResult.NodeInformation;
        }

        static void GetQueryStatisticsSegment(out NtStatus status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint segmentId, out D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation)
        {
            var queryElement = new D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT();
            queryElement.QuerySegment.SegmentId = segmentId;

            var queryStatistics = new D3DKMT_QUERYSTATISTICS()
            {
                AdapterLuid = adapter.AdapterLuid,
                Type = D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_SEGMENT,
                QueryElement = queryElement
            };

            status = D3DKMTQueryStatistics(ref queryStatistics);

            segmentInformation = queryStatistics.QueryResult.SegmentInformation;
        }

        static void GetQueryStatisticsAdapterInformation(out NtStatus status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter, out D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation)
        {
            var queryStatistics = new D3DKMT_QUERYSTATISTICS()
            {
                AdapterLuid = adapter.AdapterLuid,
                Type = D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_ADAPTER,
            };

            status = D3DKMTQueryStatistics(ref queryStatistics);

            adapterInformation = queryStatistics.QueryResult.AdapterInformation;
        }

        static void GetAdapterType(out NtStatus status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter, out D3DKMT_ADAPTERTYPE adapterTypeResult)
        {
            IntPtr adapterTypePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(D3DKMT_ADAPTERTYPE)));
            var queryAdapterInfo = new D3DKMT_QUERYADAPTERINFO()
            {
                hAdapter = adapter.hAdapter,
                Type = KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_ADAPTERTYPE,
                pPrivateDriverData = adapterTypePtr,
                PrivateDriverDataSize = Marshal.SizeOf(typeof(D3DKMT_ADAPTERTYPE))
            };

            status = D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
            adapterTypeResult = Marshal.PtrToStructure<D3DKMT_ADAPTERTYPE>(adapterTypePtr);
            Marshal.FreeHGlobal(adapterTypePtr);
            adapterTypePtr = IntPtr.Zero;
        }

        static void OpenAdapterFromDeviceName(out NtStatus status, string DisplayDeviceName, out D3DKMT_OPENADAPTERFROMDEVICENAME adapter)
        {
            adapter = new D3DKMT_OPENADAPTERFROMDEVICENAME() { pDeviceName = DisplayDeviceName };
            status = D3DKMTOpenAdapterFromDeviceName(ref adapter);
        }

        static void CloseAdapter(out NtStatus status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter)
        {
            var closeAdapter = new D3DKMT_CLOSEADAPTER { hAdapter = adapter.hAdapter };
            status = D3DKMTCloseAdapter(ref closeAdapter);
        }

        static void GetSegmentSizeInfo(out NtStatus status, D3DKMT_OPENADAPTERFROMDEVICENAME adapter, out D3DKMT_SEGMENTSIZEINFO segmentSizeInfo)
        {
            IntPtr segmentSizeInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(D3DKMT_SEGMENTSIZEINFO)));
            var queryAdapterInfo = new D3DKMT_QUERYADAPTERINFO()
            {
                hAdapter = adapter.hAdapter,
                Type = KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_GETSEGMENTSIZE,
                pPrivateDriverData = segmentSizeInfoPtr,
                PrivateDriverDataSize = Marshal.SizeOf(typeof(D3DKMT_SEGMENTSIZEINFO))
            };

            status = D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
            segmentSizeInfo = Marshal.PtrToStructure<D3DKMT_SEGMENTSIZEINFO>(segmentSizeInfoPtr);
            Marshal.FreeHGlobal(segmentSizeInfoPtr);
            segmentSizeInfoPtr = IntPtr.Zero;
        }
    }
}
