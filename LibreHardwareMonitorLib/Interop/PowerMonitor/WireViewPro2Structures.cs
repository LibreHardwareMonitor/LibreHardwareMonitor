// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop.PowerMonitor;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct VendorDataStruct
{
    public byte VendorId;
    public byte ProductId;
    public byte FwVersion;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PowerSensor
{
    public short Voltage;
    public uint Current;
    public uint Power;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SensorStruct
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public short[] Ts; // 0.1 °C

    public ushort Vdd; // mV
    public byte FanDuty; // %

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    public PowerSensor[] PowerReadings;

    public uint TotalPower; // mW
    public uint TotalCurrent; // mA
    public ushort AvgVoltage; // mV
    public HpwrCapability HpwrCapability; // 8-bit enum
    public ushort FaultStatus;
    public ushort FaultLog;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct FanConfigStruct
{
    public FanMode Mode;
    public TempSource TempSource;
    public byte DutyMin;
    public byte DutyMax;
    public short TempMin;
    public short TempMax;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct UiConfigStruct
{
    public CurrentScale CurrentScale;
    public PowerScale PowerScale;
    public Theme Theme;
    public DisplayRotation DisplayRotation;
    public TimeoutMode TimeoutMode;
    public byte CycleScreens;
    public byte CycleTime;
    public byte Timeout;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct DeviceConfigStructV1
{
    public ushort Crc;
    public byte Version;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] FriendlyName;

    public FanConfigStruct FanConfig;
    public byte BacklightDuty;

    public ushort FaultDisplayEnable;
    public ushort FaultBuzzerEnable;
    public ushort FaultSoftPowerEnable;
    public ushort FaultHardPowerEnable;
    public short TsFaultThreshold; // 0.1 °C
    public byte OcpFaultThreshold; // A
    public byte WireOcpFaultThreshold; // 0.1A
    public ushort OppFaultThreshold; // W
    public byte CurrentImbalanceFaultThreshold; // %
    public byte CurrentImbalanceFaultMinLoad; // A
    public byte ShutdownWaitTime; // seconds
    public byte LoggingInterval; // seconds
    public UiConfigStruct Ui;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
public struct DeviceConfigStructV2
{
    public ushort Crc;
    public byte Version;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] FriendlyName;

    public FanConfigStruct FanConfig;
    public byte BacklightDuty;

    public ushort FaultDisplayEnable;
    public ushort FaultBuzzerEnable;
    public ushort FaultSoftPowerEnable;
    public ushort FaultHardPowerEnable;
    public short TsFaultThreshold; // 0.1 °C
    public byte OcpFaultThreshold; // A
    public byte WireOcpFaultThreshold; // 0.1A
    public ushort OppFaultThreshold; // W
    public byte CurrentImbalanceFaultThreshold; // %
    public byte CurrentImbalanceFaultMinLoad; // A
    public byte ShutdownWaitTime; // seconds
    public byte LoggingInterval; // seconds
    public AVG Average;
    public UiConfigStruct Ui;
}
