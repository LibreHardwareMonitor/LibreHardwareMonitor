// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BlackSharp.Core.Interop.Windows.Native;
using HidSharp.Reports;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32;
using Windows.Win32;

namespace LibreHardwareMonitor.Hardware.KontronWatchdog;

internal sealed class KontronWatchdog : Hardware
{
    private static FreeLibrarySafeHandle _kontronKscDll;
    private readonly StringBuilder _report = new();

    private static int _kscError = 0;

    private static byte _watchdogValueCount;
    private readonly Sensor[] _watchdogValues;

    private delegate int KscApiFuncType_Ulong(ulong ulongValue);
    private delegate int KscApiFuncType_Byte_Ulong(byte byteValue, ulong ulongValue);
    private delegate int KscApiFuncType_Byte_Byte_Ulong(byte byteValue, byte byteValue2, ulong ulongValue);

    private static KscApiFuncType_Byte_Ulong _kscApiWdgConfigFunc;
    private static KscApiFuncType_Byte_Ulong _kscApiWdgStageStatusFunc;
    private static KscApiFuncType_Byte_Byte_Ulong _kscApiWdgStageConfigFunc;
    private static KscApiFuncType_Byte_Ulong _kscApiLastResetCauseFunc;

    public KontronWatchdog(FreeLibrarySafeHandle kontronKscDll, ISettings settings)
        : base("Kontron_Watchdog",
          new Identifier("Kontron_Watchdog"),
          settings)
    {
        _kontronKscDll = kontronKscDll;

        _report.AppendLine("Kontron Watchdog:");
        _report.AppendLine();

        // Query KSC DLL procedure addresses
        if (!KscDllQueryAddresses())
        {
            _report.AppendLine("Query KSC DLL procedure addresses - failed!");
            return;
        }

        // create watchdog value sensors
        _watchdogValueCount = 32;
        _watchdogValues = new Sensor[_watchdogValueCount];

        // value 00: Is Watchdog Running
        string watchdogValue00Name = $"Watchdog Running";
        _watchdogValues[0] = new Sensor(watchdogValue00Name,
                                        0,
                                        false,
                                        SensorType.WatchdogStageStatus,
                                        this,
                                        Array.Empty<ParameterDescription>(),
                                        settings,
                                        true);
        ActivateSensor(_watchdogValues[0]);

        // value 01..03: Stage x Expired Status
        string stageExpiredStatusNameX;
        for (int i = 0; i <= 2; i++)
        {
            stageExpiredStatusNameX = String.Format("Stage {0} Expired", i);
            _watchdogValues[1 + i] = new Sensor(stageExpiredStatusNameX,
                                                1 + i,
                                                false,
                                                SensorType.WatchdogStageStatus,
                                                this,
                                                Array.Empty<ParameterDescription>(),
                                                settings,
                                                true);
            ActivateSensor(_watchdogValues[1 + i]);
        }

        // value 04..06: Stage x Remaining Time
        string StageTimesNameX;
        for (int i = 0; i <= 2; i++)
        {
            StageTimesNameX = String.Format("Stage {0} Remaining Time", i);
            _watchdogValues[4 + i] = new Sensor(StageTimesNameX,
                                                i,
                                                false,
                                                SensorType.StageTimes,
                                                this,
                                                Array.Empty<ParameterDescription>(),
                                                settings,
                                                true);
            ActivateSensor(_watchdogValues[4 + i]);
        }

        // value 07..09: Stage x Configured Time
        for (int i = 0; i <= 2; i++)
        {
            StageTimesNameX = String.Format("Stage {0} Configured Time", i);
            _watchdogValues[7 + i] = new Sensor(StageTimesNameX,
                                                3 + i,
                                                false,
                                                SensorType.StageTimes,
                                                this,
                                                Array.Empty<ParameterDescription>(),
                                                settings,
                                                true);
            ActivateSensor(_watchdogValues[7 + i]);
        }

        // value 10..13: Watchdog Config Values
        string WatchdogConfigNameX = $"";
        for (int i = 0; i <= 3; i++)
        {
            switch(i)
            {
                case 0:
                    WatchdogConfigNameX = $"Wdg Strobe Enable";
                    break;
                case 1:
                    WatchdogConfigNameX = $"Wdg Auto Restart";
                    break;
                case 2:
                    WatchdogConfigNameX = $"Wdg Enable Lock";
                    break;
                case 3:
                    WatchdogConfigNameX = $"Wdg Global Lock";
                    break;
            }

            _watchdogValues[10 + i] = new Sensor(WatchdogConfigNameX,
                                                 i,
                                                 false,
                                                 SensorType.WatchdogStageConfig,
                                                 this,
                                                 Array.Empty<ParameterDescription>(),
                                                 settings,
                                                 true);
            ActivateSensor(_watchdogValues[10 + i]);
        }

        // value 14..22: Stage Config Values
        string StageConfigNameX = $"";
        for (int valueType = 0; valueType < 3; valueType++)
        {
            for (int stageIndex = 0; stageIndex < 3; stageIndex++)
            {
                switch (valueType)
                {
                    case 0:
                        StageConfigNameX = String.Format("Stage {0} Mode", stageIndex);
                        break;
                    case 1:
                        StageConfigNameX = String.Format("Stage {0} Output Pin", stageIndex);
                        break;
                    case 2:
                        StageConfigNameX = String.Format("Stage {0} Auto Disable", stageIndex);
                        break;
                }

                _watchdogValues[14 + valueType*3 + stageIndex] = new Sensor(StageConfigNameX,
                                                                            4 + valueType*3 + stageIndex,
                                                                            false,
                                                                            SensorType.WatchdogStageConfig,
                                                                            this,
                                                                            Array.Empty<ParameterDescription>(),
                                                                            settings,
                                                                            true);
                ActivateSensor(_watchdogValues[14 + valueType*3 + stageIndex]);
            }
        }

        // value 23..31: Last Reset Cause
        string LastResetCauseNameX = $"";
        for (int valueType = 0; valueType <= 8; valueType++)
        {
            switch (valueType)
            {
                case 0:
                    LastResetCauseNameX = $"Power On Reset";
                    break;
                case 1:
                    LastResetCauseNameX = $"External Reset";
                    break;
                case 2:
                    LastResetCauseNameX = $"Watchdog Reset";
                    break;
                case 3:
                    LastResetCauseNameX = $"Over Temperature Reset";
                    break;
                case 4:
                    LastResetCauseNameX = $"Power Good Fail (undervoltage)";
                    break;
                case 5:
                    LastResetCauseNameX = $"Overvoltage Reset";
                    break;
                case 6:
                    LastResetCauseNameX = $"Fan Fault Reset";
                    break;
                case 7:
                    LastResetCauseNameX = $"Software Reset";
                    break;
                case 8:
                    LastResetCauseNameX = $"Lrcr Update Active";
                    break;
            }
            _watchdogValues[23 + valueType] = new Sensor(LastResetCauseNameX,
                                                         valueType,
                                                         false,
                                                         SensorType.LastResetCause,
                                                         this,
                                                         Array.Empty<ParameterDescription>(),
                                                         settings,
                                                         true);
            ActivateSensor(_watchdogValues[23 + valueType]);
        }

        _report.AppendLine("> All Kontron Watchdog queries have been activated.");
        _report.AppendLine();

        Update();
    }
    public override HardwareType HardwareType
    {
        get { return HardwareType.KontronWatchdog; }
    }

    public override void Update()
    {
        // update watchdog values

        KscApiWdgConfig watchdogConfig;
        ulong watchdogConfigAddress = 0;
        KscApiWdgStageStatus wdgStageStatus;
        ulong wdgStageStatusAddress = 0;
        KscApiWdgStageConfig wdgStageConfig;
        ulong wdgStageConfigAddress = 0;
        KscApiWdgLrcr lastResetCause;
        ulong lastResetCauseAddress = 0;

        unsafe
        {
            watchdogConfigAddress = (ulong)&watchdogConfig;
            wdgStageStatusAddress = (ulong)&wdgStageStatus;
            wdgStageConfigAddress = (ulong)&wdgStageConfig;
            lastResetCauseAddress = (ulong)&lastResetCause;
        }

        // Query KSC watchdog config 
        if (!KscDllQueryWatchdogConfig(watchdogConfigAddress))
        {
            _report.AppendLine("> An error occurred while querying the KSC watchdog config!");
            return;
        }

        // value 00: Is Watchdog Running
        _watchdogValues[0].Value = watchdogConfig.WdtEnable != 0 ? 1 : 0;

        // value 10..13: Watchdog Config
        _watchdogValues[10].Value = watchdogConfig.WdStrobeEnable != 0 ? 1 : 0;
        _watchdogValues[11].Value = watchdogConfig.AutoRestart != 0 ? 1 : 0;
        _watchdogValues[12].Value = watchdogConfig.WdEnableLock != 0 ? 1 : 0;
        _watchdogValues[13].Value = watchdogConfig.WdGlobalLock != 0 ? 1 : 0;

        for (byte stageIndex = 0; stageIndex < 3; stageIndex++)
        {
            // Query KSC watchdog stage status 
            if (!KscDllQueryWdgStageStatus(stageIndex, wdgStageStatusAddress))
            {
                _report.AppendLine("> An error occurred while querying the KSC wdg stage status!");
                return;
            }

            // Query KSC watchdog stage config 
            if (!KscDllQueryWdgStageConfig(stageIndex, wdgStageConfigAddress))
            {
                _report.AppendLine("> An error occurred while querying the KSC wdg stage config!");
                return;
            }

            // value 01..03: Stage Expired Status
            ulong stageExpiredStatus = wdgStageStatus.bTimeout;
            _watchdogValues[1 + stageIndex].Value = stageExpiredStatus;

            // value 04..06: Stage Remaining Time
            ulong stageTimeValueInMilliSecs = wdgStageStatus.Time;
            ulong stageTimeValueInSecsRounded = (stageTimeValueInMilliSecs + 500) / 1000;
            _watchdogValues[4 + stageIndex].Value = stageTimeValueInSecsRounded;

            // value 07..09: Stage Configured Time
            stageTimeValueInMilliSecs = wdgStageConfig.Timeout;
            stageTimeValueInSecsRounded = (stageTimeValueInMilliSecs + 500) / 1000;
            _watchdogValues[7 + stageIndex].Value = stageTimeValueInSecsRounded;

            // value 14..22: Stage Config Values
            _watchdogValues[14 + stageIndex].Value = wdgStageConfig.Mode;
            _watchdogValues[17 + stageIndex].Value = wdgStageConfig.OutPin != 0 ? 1 : 0;
            _watchdogValues[20 + stageIndex].Value = wdgStageConfig.AutoDisable != 0 ? 1 : 0;
        }

        // Query Last Reset Cause 
        if (!KscDllQueryLastResetCause(lastResetCauseAddress))
        {
            _report.AppendLine("> An error occurred while querying the last reset cause!");
            return;
        }

        // value 23..31: Last Reset Cause
        _watchdogValues[23].Value = lastResetCause.PowerOnReset != 0 ? 1 : 0;
        _watchdogValues[24].Value = lastResetCause.ExternalReset != 0 ? 1 : 0;
        _watchdogValues[25].Value = lastResetCause.WatchdogReset != 0 ? 1 : 0;
        _watchdogValues[26].Value = lastResetCause.OverTempReset != 0 ? 1 : 0;
        _watchdogValues[27].Value = lastResetCause.PowerGoodFail != 0 ? 1 : 0;
        _watchdogValues[28].Value = lastResetCause.OverVoltageReset != 0 ? 1 : 0;
        _watchdogValues[29].Value = lastResetCause.FanFaultReset != 0 ? 1 : 0;
        _watchdogValues[30].Value = lastResetCause.SoftwareReset != 0 ? 1 : 0;
        _watchdogValues[31].Value = lastResetCause.LrcrUpdateActive != 0 ? 1 : 0;
    }

    public override string GetReport()
    {
        return _report.ToString();
    }

    public override void Close()
    {
        base.Close();
    }

    public static bool KscDllQueryAddresses()
    {
        // watchdog functions

        IntPtr kscApiWdgConfigPtr = PInvoke.GetProcAddress(_kontronKscDll, "KscApiWdgConfig");
        if (kscApiWdgConfigPtr == IntPtr.Zero)
            return false;
        _kscApiWdgConfigFunc =
            (KscApiFuncType_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiWdgConfigPtr,
                                                                             typeof(KscApiFuncType_Byte_Ulong));

        IntPtr kscApiWdgStageStatusPtr = PInvoke.GetProcAddress(_kontronKscDll, "KscApiWdgStageStatus");
        if (kscApiWdgStageStatusPtr == IntPtr.Zero)
            return false;
        _kscApiWdgStageStatusFunc =
            (KscApiFuncType_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiWdgStageStatusPtr,
                                                                             typeof(KscApiFuncType_Byte_Ulong));

        IntPtr kscApiWdgStageConfigPtr = PInvoke.GetProcAddress(_kontronKscDll, "KscApiWdgStageConfig");
        if (kscApiWdgStageConfigPtr == IntPtr.Zero)
            return false;
        _kscApiWdgStageConfigFunc =
            (KscApiFuncType_Byte_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiWdgStageConfigPtr,
                                                                                  typeof(KscApiFuncType_Byte_Byte_Ulong));

        IntPtr _kscApiLastResetCausePtr = PInvoke.GetProcAddress(_kontronKscDll, "KscApiLastResetCause");
        if (_kscApiLastResetCausePtr == IntPtr.Zero)
            return false;
        _kscApiLastResetCauseFunc =
            (KscApiFuncType_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(_kscApiLastResetCausePtr,
                                                                             typeof(KscApiFuncType_Byte_Ulong));

        return true;
    }

    public static bool KscDllQueryWatchdogConfig(ulong watchdogConfigAddress)
    {
        int result = _kscApiWdgConfigFunc((byte)KSC_API_MODE.KSC_API_MODE_GET,
                                                  watchdogConfigAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }
    public static bool KscDllQueryWdgStageStatus(byte stageIndex, ulong wdgStageStatusAddress)
    {
        int result = _kscApiWdgStageStatusFunc(stageIndex,
                                               wdgStageStatusAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }

    public static bool KscDllQueryWdgStageConfig(byte stageIndex, ulong wdgStageConfigAddress)
    {
        int result = _kscApiWdgStageConfigFunc(stageIndex,
                                               (byte)KSC_API_MODE.KSC_API_MODE_GET,
                                               wdgStageConfigAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }

    public static bool KscDllQueryLastResetCause(ulong lastResetCauseDataAddress)
    {
        int result = _kscApiLastResetCauseFunc((byte)KSC_API_MODE.KSC_API_MODE_GET,
                                               lastResetCauseDataAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }

    public const int KSC_API_LABEL_LEN = 24;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct KscApiSensorInfoData
    {
        public byte CategoryId;
        public fixed byte Label[KSC_API_LABEL_LEN];
    }
}

// -------------------------------------------------------------------------------
// consts, structs, enums, ...
// -------------------------------------------------------------------------------

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct KscApiWdgConfig
{
    public uint AutoRestart;
    public uint WdEnableLock;
    public uint WdGlobalLock;
    public uint WdStrobeEnable;
    public uint WdtEnable;
}
internal enum KSC_API_MODE
{
    KSC_API_MODE_GET = 0x00,
    KSC_API_MODE_SET = 0x01
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct KscApiWdgStageStatus
{
    public uint bTimeout;
    public uint Time;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct KscApiWdgStageConfig
{
    public byte Mode;
    public uint OutPin;
    public uint AutoDisable;
    public uint Timeout;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct KscApiWdgLrcr
{
    public uint PowerOnReset;
    public uint ExternalReset;
    public uint WatchdogReset;
    public uint OverTempReset;
    public uint PowerGoodFail;
    public uint OverVoltageReset;
    public uint FanFaultReset;
    public uint SoftwareReset;
    public uint LrcrUpdateActive;
}
