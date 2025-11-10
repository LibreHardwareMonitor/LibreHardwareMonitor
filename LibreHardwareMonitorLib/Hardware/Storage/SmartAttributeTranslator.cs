// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using DiskInfoToolkit.Interop.Enums;

namespace LibreHardwareMonitor.Hardware.Storage;

internal static class SmartAttributeTranslator
{
    const int SensorChannelStartIndex = 30;

    public static List<SmartAttribute> GetAttributesFor(DiskInfoToolkit.Storage storage)
    {
        switch (storage.SmartKey)
        {
            case SmartKey.Smart:
                return GetSmart(storage);
            case SmartKey.SSD:
                return GetSSD(storage);
            case SmartKey.Mtron:
                return GetMtron(storage);
            case SmartKey.Indilinx:
                return GetIndilinx(storage);
            case SmartKey.JMicron60x:
                return GetJMicron60X(storage);
            case SmartKey.Intel:
                return GetIntel(storage);
            case SmartKey.Samsung:
                return GetSamsung(storage);
            case SmartKey.SandForce:
                return GetSandforce(storage);
            case SmartKey.JMicron61x:
                return GetJMicron61X(storage);
            case SmartKey.Micron:
                return GetMicron(storage);
            case SmartKey.MicronMU02:
                return GetMicronMU03(storage);
            case SmartKey.Ocz:
                return GetOcz(storage);
            case SmartKey.Plextor:
                return GetPlextor(storage);
            case SmartKey.SanDisk:
                return GetSandisk(storage);
            case SmartKey.OczVector:
                return GetOczVector(storage);
            case SmartKey.Corsair:
                return GetCorsair(storage);
            case SmartKey.Toshiba:
                return GetToshiba(storage);
            case SmartKey.SanDiskGb:
                return GetSandiskGB(storage);
            case SmartKey.Kingston:
                return GetKingston(storage);
            case SmartKey.NVMe:
                return GetNVME(storage);
            case SmartKey.Realtek:
                return GetRealtek(storage);
            case SmartKey.SKhynix:
                return GetSkhynix(storage);
            case SmartKey.Kioxia:
                return GetKioxia(storage);
            case SmartKey.WDC:
                return GetWdc(storage);
            case SmartKey.KingstonSuv:
                return GetKingstonSUV(storage);
            case SmartKey.KingstonKC600:
                return GetKingstonKC600(storage);
            case SmartKey.KingstonDC500:
                return GetKingstonDC500(storage);
            case SmartKey.KingstonSA400:
                return GetKingstonSA400(storage);
            case SmartKey.Ssstc:
                return GetSSSTC(storage);
            case SmartKey.IntelDc:
                return GetIntelDC(storage);
            case SmartKey.Apacer:
                return GetAPACER(storage);
            case SmartKey.SiliconMotion:
                return GetSiliconMotion(storage);
            case SmartKey.JMicron66x:
                return GetJMicron66X(storage);
            case SmartKey.Phison:
                return GetPhison(storage);
            case SmartKey.Seagate:
                return GetSeagate(storage);
            case SmartKey.SeagateIronWolf:
                return GetSeagateIronWolf(storage);
            case SmartKey.Marvell:
                return GetMarvell(storage);
            case SmartKey.Maxiotek:
                return GetMaxiotek(storage);
            case SmartKey.SeagateBarraCuda:
                return GetSeagateBarraCuda(storage);
            case SmartKey.Ymtc:
                return GetYMTC(storage);
            case SmartKey.Scy:
                return GetSCY(storage);
            case SmartKey.Recadata:
                return GetRecadata(storage);
            case SmartKey.SanDiskHP:
                return GetSandiskHP(storage);
            case SmartKey.SanDiskHPVenus:
                return GetSandiskHPVenus(storage);
            case SmartKey.SanDiskDell:
                return GetSandiskDell(storage);
            case SmartKey.SanDiskLenovo:
                return GetSandiskLenovo(storage);
            case SmartKey.SanDiskLenovoHelenVenus:
                return GetSandiskLenovoHelenVenus(storage);
            case SmartKey.MicronMU03:
                return GetMicronMU03(storage);
            case SmartKey.SanDiskCloud:
                return GetSandiskCloud(storage);
            case SmartKey.SiliconMotionCVC:
                return GetSiliconMotionCVC(storage);
            case SmartKey.AdataIndustrial:
                return GetAdataIndustrial(storage);
            default:
                return new();
        }
    }

    private static List<SmartAttribute> GetSmart(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ThroughputPerformance),
            Get(storage, SmartAttributeType.SpinUpTime),
            Get(storage, SmartAttributeType.StartStopCount),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.ReadChannelMargin),
            Get(storage, SmartAttributeType.SeekErrorRate),
            Get(storage, SmartAttributeType.SeekTimePerformance),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.SpinRetryCount),
            Get(storage, SmartAttributeType.RecalibrationRetries),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SoftReadErrorRateStab),
            Get(storage, SmartAttributeType.CurrentHeliumLevel),
            Get(storage, SmartAttributeType.EndToEndError),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.HighFlyWrites),
            Get(storage, SmartAttributeType.AirflowTemperature),
            Get(storage, SmartAttributeType.GSenseErrorRate),
            Get(storage, SmartAttributeType.PowerOffRetractCount),
            Get(storage, SmartAttributeType.LoadUnloadCycleCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.HardwareECCRecovered),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.UncorrectableSectorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.WriteErrorRate),
            Get(storage, SmartAttributeType.SoftReadErrorRate),
            Get(storage, SmartAttributeType.DataAddressMarkError),
            Get(storage, SmartAttributeType.RunOutCancel),
            Get(storage, SmartAttributeType.SoftECCCorrection),
            Get(storage, SmartAttributeType.ThermalAsperityRate),
            Get(storage, SmartAttributeType.FlyingHeight),
            Get(storage, SmartAttributeType.SpinHighCurrent),
            Get(storage, SmartAttributeType.SpinBuzz),
            Get(storage, SmartAttributeType.OfflineSeekPerformance),
            Get(storage, SmartAttributeType.VibrationDuringWrite),
            Get(storage, SmartAttributeType.ShockDuringWrite),
            Get(storage, SmartAttributeType.DiskShift),
            Get(storage, SmartAttributeType.GSenseErrorRate),
            Get(storage, SmartAttributeType.LoadedHours),
            Get(storage, SmartAttributeType.LoadUnloadCycleCount),
            Get(storage, SmartAttributeType.LoadFriction),
            Get(storage, SmartAttributeType.LoadUnloadCycleCount),
            Get(storage, SmartAttributeType.LoadInTime),
            Get(storage, SmartAttributeType.TorqueAmplificationCount),
            Get(storage, SmartAttributeType.PowerOffRetractCount),
            Get(storage, SmartAttributeType.GMRHeadAmplitude),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.HeadFlyingHours),
            Get(storage, SmartAttributeType.ReadErrorRetryRate),
            Get(storage, SmartAttributeType.FreeFallProtection),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.HeliumConditionLower),
            Get(storage, SmartAttributeType.HeliumConditionUpper),
            Get(storage, SmartAttributeType.MAMRHealthMonitor),
        ];
    }

    private static List<SmartAttribute> GetSSD(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ThroughputPerformance),
            Get(storage, SmartAttributeType.SpinUpTime),
            Get(storage, SmartAttributeType.StartStopCount),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.ReadChannelMargin),
            Get(storage, SmartAttributeType.SeekErrorRate),
            Get(storage, SmartAttributeType.SeekTimePerformance),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.SpinRetryCount),
            Get(storage, SmartAttributeType.RecalibrationRetries),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SoftReadErrorRateStab),
            Get(storage, SmartAttributeType.UnsafeShutdownCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.UncorrectableSectorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
        ];
    }

    private static List<SmartAttribute> GetMtron(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.TotalEraseCount),
        ];
    }

    private static List<SmartAttribute> GetIndilinx(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ProgramFailureBlockCount),
            Get(storage, SmartAttributeType.EraseFailureBlockCount),
            Get(storage, SmartAttributeType.ReadFailureBlockCount),
            Get(storage, SmartAttributeType.TotalCountReadSectors),
            Get(storage, SmartAttributeType.TotalCountWriteSectors),
            Get(storage, SmartAttributeType.TotalCountReadCommands),
            Get(storage, SmartAttributeType.TotalCountWriteCommands),
            Get(storage, SmartAttributeType.TotalCountErrorBitsFromFlash),
            Get(storage, SmartAttributeType.TotalCountReadSectorsWithCorrectableBitErrors),
            Get(storage, SmartAttributeType.BadBlockFullFlag),
            Get(storage, SmartAttributeType.MaximumPECountSpecification),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.RemainingLife),
        ];
    }

    private static List<SmartAttribute> GetIntel(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.SpinUpTime),
            Get(storage, SmartAttributeType.StartStopCount),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.PowerLossProtectionFailure),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.EndToEndError),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.UnsafeShutdownCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.TimedWorkloadMediaWear),
            Get(storage, SmartAttributeType.TimedWorkloadHostReadWriteRatio),
            Get(storage, SmartAttributeType.TimedWorkloadTimer),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetSamsung(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.UsedReservedBlockCount),
            Get(storage, SmartAttributeType.UsedReservedBlockCount),
            Get(storage, SmartAttributeType.UnusedReservedBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.RuntimeBadBlock),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.AirflowTemperature),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ECCErrorRate),
            Get(storage, SmartAttributeType.OfflineUncorrectableErrors),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.SuperCapStatus),
            Get(storage, SmartAttributeType.SSDModeStatus),
            Get(storage, SmartAttributeType.PORRecoveryCount),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.UnusedReservedBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ErrorDetection),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
        ];
    }

    private static List<SmartAttribute> GetSandforce(DiskInfoToolkit.Storage storage)
    {
        int sensorStart = SensorChannelStartIndex;
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.RetiredBlockCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SoftReadErrorRate),
            Get(storage, SmartAttributeType.GigabytesErased),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.WearRangeDelta),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.IOErrorDetectionCodeErrors),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.UncorrectableSectorCount),
            Get(storage, SmartAttributeType.SATARErrors),
            Get(storage, SmartAttributeType.SoftReadErrorRate),
            Get(storage, SmartAttributeType.SoftECCCorrection),
            Get(storage, SmartAttributeType.DriveLifeProtectionStatus),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.NandDataWritten, SensorType.Data, sensorStart++),
            Get(storage, SmartAttributeType.PowerFailBackupHealth),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
        ];
    }

    private static List<SmartAttribute> GetMicron(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedNANDBlocks),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SoftReadErrorRate),
            Get(storage, SmartAttributeType.DeviceCapacity),
            Get(storage, SmartAttributeType.UserCapacity),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.RemainingSpareBlocks),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.UnusedReservedBlockCount),
            Get(storage, SmartAttributeType.UnalignedAccessCount),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.ErrorCorrectionCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ECCBitCorrectionCount),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.OfflineUncorrectableErrors),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.LifetimeUsed),
            Get(storage, SmartAttributeType.WriteErrorRate),
            Get(storage, SmartAttributeType.SuccessfulRAINRecoveryCount),
            Get(storage, SmartAttributeType.TotalBytesRead),
            Get(storage, SmartAttributeType.WriteProtectProgress),
            Get(storage, SmartAttributeType.ECCBitsCorrected),
            Get(storage, SmartAttributeType.ECCCumulativeThresholdEvents),
            Get(storage, SmartAttributeType.CumulativeProgramNANDPages),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostProgramPageCount),
            Get(storage, SmartAttributeType.BackgroundProgramPageCount),
            Get(storage, SmartAttributeType.TotalRefreshISPCount),
            Get(storage, SmartAttributeType.TotalDoRefCalCount),
            Get(storage, SmartAttributeType.TotalNANDReadPlaneCountLow),
            Get(storage, SmartAttributeType.TotalNANDReadPlaneCountHigh),
            Get(storage, SmartAttributeType.TotalBlockReMapPassCount),
            Get(storage, SmartAttributeType.TotalBackgroundScanOverLimitCount),
            Get(storage, SmartAttributeType.TotalBackgroundScan),
        ];
    }

    private static List<SmartAttribute> GetOcz(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.SpinUpTime),
            Get(storage, SmartAttributeType.StartStopCount),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.TotalCountWriteSectors),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.TotalBlocksErased),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.TotalNumberCorrectedBits),
            Get(storage, SmartAttributeType.MaxRatedPECounts),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.NandReadRetryCount),
            Get(storage, SmartAttributeType.SimpleReadRetryAttempts),
            Get(storage, SmartAttributeType.AdaptiveReadRetryAttempts),
            Get(storage, SmartAttributeType.OfflineUncorrectableErrors),
            Get(storage, SmartAttributeType.RAIDRecoveryCount),
            Get(storage, SmartAttributeType.PowerLossProtectionFailure),
            Get(storage, SmartAttributeType.NandDataRead),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetSeagate(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.RetiredBlockCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.RemainingSpareBlocks),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.WearRangeDelta),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.SoftReadErrorRate),
            Get(storage, SmartAttributeType.SoftECCCorrection),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.ReadFailureBlockCount),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataRead),
        ];
    }

    private static List<SmartAttribute> GetWdc(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedNANDBlocks),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.TotalNANDEraseCount),
            Get(storage, SmartAttributeType.MinimumPECycles),
            Get(storage, SmartAttributeType.MaximumBadBlocksPerDie),
            Get(storage, SmartAttributeType.MaximumPECycles),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.GrownBadBlocks),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AveragePECycles),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.EndToEndErrorsCorrected),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetPlextor(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ProgramFailCountWorstCase),
            Get(storage, SmartAttributeType.EraseFailCountWorstCase),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.UsedReservedBlockCountWorstCase),
            Get(storage, SmartAttributeType.UsedReservedBlockCount),
            Get(storage, SmartAttributeType.UnusedReservedBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.EndToEndErrorsCorrected),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.UnsafeShutdownCount),
            Get(storage, SmartAttributeType.ECCErrorRate),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.UncorrectableSectorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
        ];
    }

    private static List<SmartAttribute> GetSandisk(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReallocatedNANDBlocks),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.PercentOfTotalEraseCount),
            Get(storage, SmartAttributeType.RemainingSpareBlocks),
            Get(storage, SmartAttributeType.PercentOfTotalEraseCountBCBlocks),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
        ];
    }

    private static List<SmartAttribute> GetOczVector(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.RuntimeBadBlock),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.TotalUncorrectableNANDReads),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.TotalReadFailures),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.TotalBlocksErased),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.TotalNumberCorrectedBits),
            Get(storage, SmartAttributeType.MaxRatedPECounts),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.NandReadRetryCount),
            Get(storage, SmartAttributeType.SimpleReadRetryAttempts),
            Get(storage, SmartAttributeType.AdaptiveReadRetryAttempts),
            Get(storage, SmartAttributeType.OfflineUncorrectableErrors),
            Get(storage, SmartAttributeType.RAIDRecoveryCount),
            Get(storage, SmartAttributeType.InWarranty),
            Get(storage, SmartAttributeType.DASPolarity),
            Get(storage, SmartAttributeType.PartialPfail),
            Get(storage, SmartAttributeType.WriteThrottlingActivationFlag),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataRead),
        ];
    }

    private static List<SmartAttribute> GetToshiba(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ThroughputPerformance),
            Get(storage, SmartAttributeType.SpinUpTime),
            Get(storage, SmartAttributeType.StartStopCount),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.SeekErrorRate),
            Get(storage, SmartAttributeType.SeekTimePerformance),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.SpinRetryCount),
            Get(storage, SmartAttributeType.RecalibrationRetries),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.UncorrectableSectorCount),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
        ];
    }

    private static List<SmartAttribute> GetCorsair(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.RetiredBlockCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
        ];
    }

    private static List<SmartAttribute> GetKingston(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ThroughputPerformance),
            Get(storage, SmartAttributeType.SpinUpTime),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.SeekErrorRate),
            Get(storage, SmartAttributeType.SeekTimePerformance),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.SpinRetryCount),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.BadClusterTableCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.UnsafeShutdownCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.WriteHead),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
        ];
    }

    private static List<SmartAttribute> GetMicronMU03(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedNANDBlocks),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.UnusedSpareNANDBlocks),
            Get(storage, SmartAttributeType.Non4KAlignedAccess),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.ErrorCorrectionCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.ErrorCorrectionCount),
            Get(storage, SmartAttributeType.OfflineUncorrectableErrors),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.WriteErrorRate),
            Get(storage, SmartAttributeType.SuccessfulRAINRecoveryCount),
            Get(storage, SmartAttributeType.HostProgramPageCount),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.HostProgramPageCount),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.CumulativeProgramNANDPages),
            Get(storage, SmartAttributeType.CumulativeProgramNANDPages),
        ];
    }

    private static List<SmartAttribute> GetNVME(DiskInfoToolkit.Storage storage)
    {
        int sensorStart = SensorChannelStartIndex;
        return
        [
            Get(storage, SmartAttributeType.CriticalWarning),
            Get(storage, SmartAttributeType.CompositeTemperature),
            Get(storage, SmartAttributeType.AvailableSpare, SensorType.Level, sensorStart++),
            Get(storage, SmartAttributeType.AvailableSpareThreshold, SensorType.Level, sensorStart++),
            Get(storage, SmartAttributeType.PercentageUsed, SensorType.Level, sensorStart++),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostReadCommands),
            Get(storage, SmartAttributeType.HostWriteCommands),
            Get(storage, SmartAttributeType.ControllerBusyTime),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.UnsafeShutdownCount),
            Get(storage, SmartAttributeType.MediaAndDataIntegrityErrors),
            Get(storage, SmartAttributeType.NumberOfErrorInformationLogEntries),
            Get(storage, SmartAttributeType.WarningCompositeTemperatureTime),
            Get(storage, SmartAttributeType.CriticalCompositeTemperatureTime),
            Get(storage, SmartAttributeType.TemperatureSensor1),
            Get(storage, SmartAttributeType.TemperatureSensor2),
            Get(storage, SmartAttributeType.TemperatureSensor3),
            Get(storage, SmartAttributeType.TemperatureSensor4),
            Get(storage, SmartAttributeType.TemperatureSensor5),
            Get(storage, SmartAttributeType.TemperatureSensor6),
            Get(storage, SmartAttributeType.TemperatureSensor7),
            Get(storage, SmartAttributeType.TemperatureSensor8),
            Get(storage, SmartAttributeType.ThermalManagementTemperature1TransitionCount),
            Get(storage, SmartAttributeType.ThermalManagementTemperature2TransitionCount),
            Get(storage, SmartAttributeType.TotalTimeThermalManagementTemperature1),
            Get(storage, SmartAttributeType.TotalTimeThermalManagementTemperature2),
        ];
    }

    private static List<SmartAttribute> GetRealtek(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.RawDataErrorRate),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.GDN),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.MaximumPECycles),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ECCFailRecord),
            Get(storage, SmartAttributeType.UnalignedAccessCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ECCBitCorrectionCount),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetSkhynix(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ProgramFailCountWorstCase),
            Get(storage, SmartAttributeType.EraseFailCountWorstCase),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.UsedReservedBlockCountWorstCase),
            Get(storage, SmartAttributeType.UsedReservedBlockCount),
            Get(storage, SmartAttributeType.UnusedReservedBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.EndToEndError),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.ShockEventCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ECCErrorRate),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.UncorrectableSectorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.UncorrectableSoftReadErrorRate),
            Get(storage, SmartAttributeType.SoftECCCorrection),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataRead),
        ];
    }

    private static List<SmartAttribute> GetKioxia(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.HostDataWritten),
        ];
    }

    private static List<SmartAttribute> GetSSSTC(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedNANDBlocks),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ProgramFailCountWorstCase),
            Get(storage, SmartAttributeType.EraseFailCountWorstCase),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.UsedReservedBlockCountWorstCase),
            Get(storage, SmartAttributeType.UsedReservedBlockCount),
            Get(storage, SmartAttributeType.UnusedReservedBlockCount),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.EndToEndError),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ECCErrorRate),
            Get(storage, SmartAttributeType.OfflineUncorrectableErrors),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.PowerLossProtectionFailure),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetIntelDC(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedNANDBlocks),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.DeviceCapacity),
            Get(storage, SmartAttributeType.UserCapacity),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.RemainingSpareBlocks),
            Get(storage, SmartAttributeType.TotalBlockEraseFailure),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.PowerLossProtectionFailure),
            Get(storage, SmartAttributeType.ProgramFailureBlockCount),
            Get(storage, SmartAttributeType.EndToEndError),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.OfflineUncorrectableErrors),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.LifetimeUsed),
            Get(storage, SmartAttributeType.TimedWorkloadMediaWear),
            Get(storage, SmartAttributeType.TimedWorkloadHostReadWriteRatio),
            Get(storage, SmartAttributeType.TimedWorkloadTimer),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetAPACER(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.BadClusterTableCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.HostDataWritten),
        ];
    }

    private static List<SmartAttribute> GetSiliconMotion(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SLCTotalEraseCount),
            Get(storage, SmartAttributeType.SLCMaximumEraseCount),
            Get(storage, SmartAttributeType.SLCMinimumEraseCount),
            Get(storage, SmartAttributeType.SLCAverageEraseCount),
            Get(storage, SmartAttributeType.DRAM1BitErrorCount),
            Get(storage, SmartAttributeType.UncorrectableSectorCount),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.NumberOfCacheDataBlock),
            Get(storage, SmartAttributeType.NumberOfInvalidBlocks),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.MaxEraseCountOfSpec),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.ProgramFailCountInWorstDie),
            Get(storage, SmartAttributeType.EraseFailCountInWorstDie),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.RuntimeBadBlock),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.PowerOffRetractCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.HardwareECCRecovered),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.OfflineUncorrectableErrors),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.TotalCountWriteSectors),
        ];
    }

    private static List<SmartAttribute> GetPhison(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.HostDataWritten),
        ];
    }

    private static List<SmartAttribute> GetMarvell(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.PowerOffRetractCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
        ];
    }

    private static List<SmartAttribute> GetMaxiotek(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.BadClusterTableCount),
            Get(storage, SmartAttributeType.ReadErrorRetryRate),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.SLCMinimumEraseCount),
            Get(storage, SmartAttributeType.SLCMaximumEraseCount),
            Get(storage, SmartAttributeType.SLCAverageEraseCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataRead),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.BitErrorCount),
        ];
    }

    private static List<SmartAttribute> GetYMTC(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.MaximumBadBlocksPerDie),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.BadClusterTableCount),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.EndToEndErrorsCorrected),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.SLCMinimumEraseCount),
            Get(storage, SmartAttributeType.SLCMaximumEraseCount),
            Get(storage, SmartAttributeType.SLCAverageEraseCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataRead),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NANDTemperature),
            Get(storage, SmartAttributeType.BitErrorCount),
        ];
    }

    private static List<SmartAttribute> GetSCY(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.SeekErrorRate),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.ReadErrorRetryRate),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.RuntimeBadBlock),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ECCBitCorrectionCount),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetJMicron60X(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.HaltSystemIDFlashID),
            Get(storage, SmartAttributeType.FirmwareVersion),
            Get(storage, SmartAttributeType.ECCFailRecord),
            Get(storage, SmartAttributeType.AverageEraseCountMaxEraseCount),
            Get(storage, SmartAttributeType.GoodBlockCountSystemBlockCount),
        ];
    }

    private static List<SmartAttribute> GetJMicron61X(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ThroughputPerformance),
            Get(storage, SmartAttributeType.SpinUpTime),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.SeekErrorRate),
            Get(storage, SmartAttributeType.SeekTimePerformance),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.SpinRetryCount),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.ECCFailRecord),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
        ];
    }

    private static List<SmartAttribute> GetJMicron66X(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ThroughputPerformance),
            Get(storage, SmartAttributeType.SpinUpTime),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.SeekErrorRate),
            Get(storage, SmartAttributeType.SeekTimePerformance),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.SpinRetryCount),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.BadClusterTableCount),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.WriteHead),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
        ];
    }

    private static List<SmartAttribute> GetSeagateIronWolf(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.GigabytesErased),
            Get(storage, SmartAttributeType.LifetimePS4EntryCount),
            Get(storage, SmartAttributeType.LifetimePS3EntryCount),
            Get(storage, SmartAttributeType.GrownBadBlocks),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.WearRangeDelta),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.RAISEECCCorrectableCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.DriveLifeProtectionStatus),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.FreeSpace),
        ];
    }

    private static List<SmartAttribute> GetSeagateBarraCuda(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.RetiredBlockCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.RemainingSpareBlocks),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.WearRangeDelta),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.SoftReadErrorRate),
            Get(storage, SmartAttributeType.SoftECCCorrection),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.ReadFailureBlockCount),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataRead),
        ];
    }

    private static List<SmartAttribute> GetSandiskGB(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedNANDBlocks),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.TotalBlocksErased),
            Get(storage, SmartAttributeType.MinimumPECycles),
            Get(storage, SmartAttributeType.MaximumBadBlocksPerDie),
            Get(storage, SmartAttributeType.MaximumPECycles),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.GrownBadBlocks),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.PECycles),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.EndToEndErrorsCorrected),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetKingstonSUV(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ProgramFailCountInWorstDie),
            Get(storage, SmartAttributeType.EraseFailCountInWorstDie),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.UsedReservedBlockCountWorstDie),
            Get(storage, SmartAttributeType.UnusedReservedBlockCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ECCErrorRate),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.SoftECCCorrection),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataRead),
        ];
    }

    private static List<SmartAttribute> GetKingstonKC600(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedNANDBlocks),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.Reserved),
            Get(storage, SmartAttributeType.Reserved),
            Get(storage, SmartAttributeType.Reserved),
            Get(storage, SmartAttributeType.SLCAverageEraseCount),
            Get(storage, SmartAttributeType.DRAM1BitErrorCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.Reserved),
            Get(storage, SmartAttributeType.Reserved),
            Get(storage, SmartAttributeType.Reserved),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.Reserved),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.Reserved),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.HardwareECCRecovered),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.UnusedReservedBlockCount),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.HostDataWritten),
        ];
    }

    private static List<SmartAttribute> GetKingstonDC500(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.BadBlockFullFlag),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.TotalReadFailures),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
        ];
    }

    private static List<SmartAttribute> GetKingstonSA400(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.VendorUnique),
            Get(storage, SmartAttributeType.VendorUnique),
            Get(storage, SmartAttributeType.VendorUnique),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.VendorUnique),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.VendorUnique),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.VendorUnique),
            Get(storage, SmartAttributeType.VendorUnique),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
        ];
    }

    private static List<SmartAttribute> GetRecadata(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.SeekErrorRate),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.ReadErrorRetryRate),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.RuntimeBadBlock),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ECCBitCorrectionCount),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.PendingSectorCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetSandiskDell(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.UsedReservedBlockCountWorstCase),
            Get(storage, SmartAttributeType.UsedReservedBlockCount),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ECCErrorRate),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
        ];
    }

    private static List<SmartAttribute> GetSandiskHP(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.EndToEndError),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.AirflowTemperature),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.ThrottleStatistics),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetSandiskHPVenus(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.WearLevelingCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.EndToEndError),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.AirflowTemperature),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ReallocationEventCount),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetSandiskLenovo(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.MinimumPECycles),
            Get(storage, SmartAttributeType.MaximumBadBlocksPerDie),
            Get(storage, SmartAttributeType.MaximumPECycles),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.GrownBadBlocks),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AveragePECycles),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.RemainingSpareBlocks),
            Get(storage, SmartAttributeType.EndToEndErrorsCorrected),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.ThrottleStatistics),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
        ];
    }

    private static List<SmartAttribute> GetSandiskLenovoHelenVenus(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.MinimumPECycles),
            Get(storage, SmartAttributeType.MaximumBadBlocksPerDie),
            Get(storage, SmartAttributeType.MaximumPECycles),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.GrownBadBlocks),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AveragePECycles),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.UsedReservedBlockCount),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.EndToEndError),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.CommandTimeout),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.AvailableReservedSpace),
            Get(storage, SmartAttributeType.MediaWearoutIndicator),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.ThrottleStatistics),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetSandiskCloud(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.RetiredBlockCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.WriteAmplificationMultipliedBy100),
            Get(storage, SmartAttributeType.WriteAmplificationFactor),
            Get(storage, SmartAttributeType.ReserveBlockCount),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.CleanShutdownCount),
            Get(storage, SmartAttributeType.UnsafeShutdownCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.ReallocatedNANDBlocks),
            Get(storage, SmartAttributeType.PercentOfTotalEraseCount),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.CapacitorHealth),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.ThermalThrottleStatus),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.SPITestsRemaining),
        ];
    }

    private static List<SmartAttribute> GetSiliconMotionCVC(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.ReallocatedSectorsCount),
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SLCCache),
            Get(storage, SmartAttributeType.GrownBadBlocks),
            Get(storage, SmartAttributeType.ProgramFailCount),
            Get(storage, SmartAttributeType.EraseFailCount),
            Get(storage, SmartAttributeType.AverageEraseCount),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.ProgramFailCountWorstCase),
            Get(storage, SmartAttributeType.SATADownshiftCount),
            Get(storage, SmartAttributeType.EndToEndError),
            Get(storage, SmartAttributeType.UncorrectableErrors),
            Get(storage, SmartAttributeType.MaximumEraseCount),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.MinimumEraseCount),
            Get(storage, SmartAttributeType.ReadErrorRate),
            Get(storage, SmartAttributeType.OfflineUncorrectableErrors),
            Get(storage, SmartAttributeType.CRCErrorCount),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.NandDataWritten),
            Get(storage, SmartAttributeType.HostDataWritten),
            Get(storage, SmartAttributeType.HostDataRead),
            Get(storage, SmartAttributeType.RAIDEventCount),
            Get(storage, SmartAttributeType.RAIDUncorrectableCount),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.ReadErrorRetryRate),
            Get(storage, SmartAttributeType.NandDataWritten),
        ];
    }

    private static List<SmartAttribute> GetAdataIndustrial(DiskInfoToolkit.Storage storage)
    {
        return
        [
            Get(storage, SmartAttributeType.PowerOnHours),
            Get(storage, SmartAttributeType.PowerCycleCount),
            Get(storage, SmartAttributeType.SSDProtectMode),
            Get(storage, SmartAttributeType.SATAPhysicalErrorCount),
            Get(storage, SmartAttributeType.BadBlockCount),
            Get(storage, SmartAttributeType.TotalEraseCount),
            Get(storage, SmartAttributeType.BadClusterTableCount),
            Get(storage, SmartAttributeType.SpareBlocksAvailable),
            Get(storage, SmartAttributeType.UnexpectedPowerLoss),
            Get(storage, SmartAttributeType.Temperature),
            Get(storage, SmartAttributeType.RemainingLife),
            Get(storage, SmartAttributeType.FlashWriteSectorCount),
            Get(storage, SmartAttributeType.FlashReadSectorCount),
            Get(storage, SmartAttributeType.TotalCountWriteSectors),
            Get(storage, SmartAttributeType.TotalCountReadSectors),
        ];
    }

    private static SmartAttribute Get(DiskInfoToolkit.Storage storage, SmartAttributeType smartAttributeType, SensorType? sensorType = null, int sensorChannel = 0, string sensorName = null)
    {
        var attr = storage.Smart.SmartAttributes.Find(sa => sa.Info.Type == smartAttributeType);

        if (attr == null)
        {
            return null;
        }
        else
        {
            return new(attr, sensorType, sensorChannel, sensorName);
        }
    }
}
