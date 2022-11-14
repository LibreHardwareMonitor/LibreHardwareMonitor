// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Storage;

/// <summary>
/// Localization class for SMART attribute names.
/// </summary>
public static class SmartNames
{
    public static string AirflowTemperature => "Airflow Temperature";

    public static string AlternativeEraseFailCount => "Alternative Erase Fail Count";

    public static string AlternativeGSenseErrorRate => "Alternative G-Sense Error Rate";

    public static string AlternativeProgramFailCount => "Alternative Program Fail Count";

    public static string AvailableReservedSpace => "Available Reserved Space";

    public static string AverageEraseCount => "Average Erase Count";

    public static string BadBlockFullFlag => "Bad Block Full Flag";

    public static string BitErrors => "Bit Errors";

    public static string CalibrationRetryCount => "Calibration Retry Count";

    public static string CommandTimeout => "Command Timeout";

    public static string ControllerWritesToNand => "Controller Writes to NAND";

    public static string CorrectedErrors => "Corrected Errors";

    public static string CrcErrorCount => "CRC Error Count";

    public static string CurrentPendingSectorCount => "Current Pending Sector Count";

    public static string DataAddressMarkErrors => "Data Address Mark errors";

    public static string DiskShift => "Disk Shift";

    public static string DriveTemperature => "Drive Temperature";

    public static string EccRate => "ECC Rate";

    public static string EmergencyRetractCycleCount => "Emergency Retract Cycle Count";

    public static string EndToEndError => "End-to-End error";

    public static string EnduranceRemaining => "Endurance Remaining";

    public static string EraseFailCount => "Erase Fail Count";

    public static string EraseFailCountChip => "Erase Fail Count (Chip)";

    public static string EraseFailCountTotal => "Erase Fail Count (Total)";

    public static string EraseFailure => "Erase Failure";

    public static string ErrorCorrectionCount => "Error Correction Count";

    public static string ExceptionModeStatus => "Exception Mode Status";

    public static string FactoryBadBlockCount => "Factory Bad Block Count";

    public static string FlyingHeight => "Flying Height";

    public static string FreeFallProtection => "Free Fall Protection";

    public static string FTLProgramNANDPagesCount => "FTL Program NAND Pages Count";

    public static string GmrHeadAmplitude => "GMR Head Amplitude";

    public static string GSenseErrorRate => "G-sense Error Rate";

    public static string HardwareEccRecovered => "Hardware ECC Recovered";

    public static string HeadFlyingHours => "Head Flying Hours";

    public static string HeadStability => "Head Stability";

    public static string HighFlyWrites => "High Fly Writes";

    public static string HostProgramNANDPagesCount => "Host Program NAND Pages Count";

    public static string HostReads => "Host Reads";

    public static string HostWrites => "Host Writes";

    public static string HostWritesToController => "Host Writes to Controller";

    public static string InducedOpVibrationDetection => "Induced Op-Vibration Detection";

    public static string InitialBadBlockCount => "Initial Bad Block Count";

    public static string LoadCycleCount => "Load Cycle Count";

    public static string LoadedHours => "Loaded Hours";

    public static string LoadFriction => "Load Friction";

    public static string LoadInTime => "Load 'In'-time";

    public static string LoadUnloadCycleCount => "Load/Unload Cycle Count";

    public static string LoadUnloadCycleCountFujitsu => "Load/Unload Cycle Count (Fujitus)";

    public static string LoadUnloadRetryCount => "Load/Unload Retry Count";

    public static string MaxCellCycles => "Max Cell Cycles";

    public static string MaxErase => "Max Erase";

    public static string MediaWearOutIndicator => "Media Wear Out Indicator";

    public static string MinErase => "Min Erase";

    public static string MultiZoneErrorRate => "Multi-Zone Error Rate";

    public static string NewFailingBlockCount => "New Failing Block Count";

    public static string Non4KAlignedAccess => "Non-4k Aligned Access";

    public static string OfflineSeekPerformance => "Offline Seek Performance";

    public static string OffLineUncorrectableErrorCount => "Off-Line Uncorrectable Error Count";

    public static string PowerCycleCount => "Power Cycle Count";

    public static string PowerOffRetractCycle => "Power-Off Retract Cycle";

    public static string PowerOnHours => "Power-On Hours (POH)";

    public static string PowerRecoveryCount => "Power Recovery Count";

    public static string ProgramFailCount => "Program Fail Count";

    public static string ProgramFailCountChip => "Program Fail Count (Chip)";

    public static string ProgramFailCountTotal => "Program Fail Count (Total)";

    public static string ProgramFailure => "Program Failure";

    public static string RawReadErrorRate => "Raw Read Error Rate";

    public static string ReadChannelMargin => "Read Channel Margin";

    public static string ReadCommands => "Read Commands";

    public static string ReadErrorRate => "Read Error Rate";

    public static string ReadErrorRetryRate => "Read Error Retry Rate";

    public static string ReadFailure => "Read Failure";

    public static string ReallocatedNANDBlockCount => "Reallocated NAND Block Count";

    public static string ReallocatedSectorsCount => "Reallocated Sectors Count";

    public static string ReallocationEventCount => "Reallocation Event Count";

    public static string RecalibrationRetries => "Recalibration Retries";

    public static string RemainingLife => "Remaining Life";

    public static string ReportedUncorrectableErrors => "Reported Uncorrectable Errors";

    public static string RetiredBlockCount => "Retired Block Count";

    public static string RunOutCancel => "Run Out Cancel";

    public static string RuntimeBadBlockTotal => "Runtime Bad Block Total";

    public static string SataDownshiftErrorCount => "SATA Downshift Error Count";

    public static string SataErrorCountCrc => "SATA Error Count CRC";

    public static string SataErrorCountHandshake => "SATA Error Count Handshake";

    public static string SectorsRead => "Sectors Read";

    public static string SectorsWritten => "Sectors Written";

    public static string SeekErrorRate => "Seek Error Rate";

    public static string SeekTimePerformance => "Seek Time Performance";

    public static string ShockDuringWrite => "Shock During Write";

    public static string SoftEccCorrection => "Soft ECC Correction";

    public static string SoftReadErrorRate => "Soft Read Error Rate";

    public static string SpinBuzz => "Spin Buzz";

    public static string SpinHighCurrent => "Spin High Current";

    public static string SpinRetryCount => "Spin Retry Count";

    public static string SpinUpTime => "Spin-Up Time";

    public static string StartStopCount => "Start/Stop Count";

    public static string SuccessfulRAINRecoveryCount => "Successful RAIN Recovery Count";

    public static string SupercapStatus => "Supercap Status";

    public static string TaCounterDetected => "TA Counter Detected";

    public static string Temperature => "Temperature";

    public static string TemperatureDifferenceFrom100 => "Temperature Difference from 100";

    public static string TemperatureExceedCount => "Temperature Exceed Count";

    public static string ThermalAsperityRate => "Thermal Asperity Rate (TAR)";

    public static string ThroughputPerformance => "Throughput Performance";

    public static string TorqueAmplificationCount => "Torque Amplification Count";

    public static string TotalLbasRead => "Total LBAs Read";

    public static string TotalLbasWritten => "Total LBAs Written";

    public static string TransferErrorRate => "Transfer Error Rate";

    public static string UltraDmaCrcErrorCount => "UltraDMA CRC Error Count";

    public static string UncorrectableErrorCount => "Uncorrectable Error Count";

    public static string UncorrectableSectorCount => "Uncorrectable Sector Count";

    public static string UnexpectedPowerLossCount => "Unexpected Power Loss Count";

    public static string Unknown => "Unknown";

    public static string UnknownUnique => "Unknown Unique";

    public static string UnrecoverableEcc => "Unrecoverable ECC";

    public static string UnsafeShutdownCount => "Unsafe Shutdown Count";

    public static string UnusedReserveNANDBlocks => "Unused Reserve NAND Blocks";

    public static string UsedReservedBlockCountChip => "Used Reserved Block Count (Chip)";

    public static string UsedReservedBlockCountTotal => "Used Reserved Block Count (Total)";

    public static string VibrationDuringWrite => "Vibration During Write";

    public static string WearLevelingCount => "Wear Leveling Count";

    public static string WearRangeDelta => "Wear Range Delta";

    public static string WriteCommands => "Write Commands";

    public static string WriteErrorRate => "Write Error Rate";
}