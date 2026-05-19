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

namespace LibreHardwareMonitor.Hardware.KontronSensors;

internal sealed class KontronSensors : Hardware
{
    private static IntPtr _kontronKscDll = IntPtr.Zero;
    private readonly StringBuilder _report = new();

    private static int _kscError = 0;

    private static byte _voltSensorCount;
    private static ulong _voltSensorCountValueAddress = 0;
    private static KscApiSensorInfoData _voltSensorInfoData;
    private static ulong _voltValueInMilliVolt = 0;
    private static ulong _voltValueAddress = 0;
    private readonly Sensor[] _voltages;
    private static bool[] _shouldVBatSensorReadOnlyOnce;
    private static bool[] _wasVBatValueAlreadyRead;

    private static byte _tempSensorCount;
    private static ulong _tempSensorCountValueAddress = 0;
    private static KscApiSensorInfoData _tempSensorInfoData;
    private static ulong _tempValueInMilliKelvin = 0;
    private static ulong _tempValueAddress = 0;
    private readonly Sensor[] _temperatures;

    private static byte _fanSensorCount;
    private static ulong _fanSensorCountValueAddress = 0;
    private static KscApiSensorInfoData _fanSensorInfoData;
    private static ulong _fanValueInRpm = 0;
    private static ulong _fanValueAddress = 0;
    private readonly Sensor[] _fans;

    private delegate int KscApiFuncType_Ulong(ulong ulongValue);
    private delegate int KscApiFuncType_Byte_Ulong(byte byteValue, ulong ulongValue);
    private delegate int KscApiFuncType_Byte_Byte_Ulong(byte byteValue, byte byteValue2, ulong ulongValue);

    private static KscApiFuncType_Ulong _kscApiVoltCountFunc;
    private static KscApiFuncType_Byte_Ulong _kscApiVoltInfoFunc;
    private static KscApiFuncType_Byte_Ulong _kscApiVoltValueFunc;

    private static KscApiFuncType_Ulong _kscApiTempCountFunc;
    private static KscApiFuncType_Byte_Ulong _kscApiTempInfoFunc;
    private static KscApiFuncType_Byte_Ulong _kscApiTempValueFunc;

    private static KscApiFuncType_Ulong _kscApiFanCountFunc;
    private static KscApiFuncType_Byte_Ulong _kscApiFanInfoFunc;
    private static KscApiFuncType_Byte_Ulong _kscApiFanValueFunc;

    public KontronSensors(IntPtr kontronKscDll, ISettings settings)
        : base("Kontron_Sensors",
               new Identifier("Kontron_Sensors"),
               settings)
    {
        _kontronKscDll = kontronKscDll;

        _report.AppendLine("Kontron Sensors:");
        _report.AppendLine();

        // Query KSC DLL procedure addresses
        if (!KscDllQueryAddresses())
        {
            _report.AppendLine("Query KSC DLL procedure addresses - failed!");
            return;
        }

        // Build KSC DLL value addresses
        if (!KscDllBuildValueAdresses())
        {
            _report.AppendLine("Build KSC DLL value addresses - failed!");
            return;
        }

        // Query KSC sensor numbers 
        _report.AppendLine("Query KSC sensor numbers:");
        if (!KscDllQueryAllSensorCounts())
        {
            _report.AppendLine("Query KSC sensor numbers - failed! error = " + _kscError);
            return;
        }

        _report.AppendLine("- Volt sensor count: " + _voltSensorCount);
        _report.AppendLine("- Temp sensor count: " + _tempSensorCount);
        _report.AppendLine("- Fan Sensor count:  " + _fanSensorCount);

        // create voltage sensors
        _report.AppendLine("Activate voltage sensors:");
        _voltages = new Sensor[_voltSensorCount];
        _shouldVBatSensorReadOnlyOnce = new bool[_voltSensorCount];
        _wasVBatValueAlreadyRead = new bool[_voltSensorCount];

        for (int i = 0; i < _voltSensorCount; i++)
        {
            string voltInfoLabel;

            // Query KSC volt info data 
            if (!KscDllQueryVoltInfo((byte)i))
            {
                _report.AppendLine("Query KSC volt info data - failed! error = " + _kscError);
                return;
            }

            unsafe
            {
                fixed (byte* pLabelText = _voltSensorInfoData.Label)
                {
                    voltInfoLabel = Encoding.ASCII.GetString(pLabelText, KSC_API_LABEL_LEN).Trim('\0');
                }
            }

            _report.AppendLine("- Cat: " + _voltSensorInfoData.CategoryId);
            _report.AppendLine("- Label: " + voltInfoLabel);

            if (_voltSensorInfoData.CategoryId == (byte)KscApiVoltCategory.eKscApiVolt_VBat)
            {
                _report.AppendLine("- A VBAT sensor was found.");

                // try to read a config entry "~/queryonce" == "true"

                string vbatSensorQueryOnceConfigEntryString =
                    String.Format("/Kontron_Sensors/voltage/{0}/queryonce", i);
                string vbatSensorQueryOnceConfigEntryValue =
                    settings.GetValue(vbatSensorQueryOnceConfigEntryString, "false");
                _shouldVBatSensorReadOnlyOnce[i] = vbatSensorQueryOnceConfigEntryValue == "true";
            }

            if(_shouldVBatSensorReadOnlyOnce[i])
            {
                _report.AppendLine("- This VBAT sensor should be read only once.");

                string timeText = $" ({DateTime.Now:H:mm:ss})";
                _voltages[i] = new Sensor(voltInfoLabel + timeText,
                                          i,
                                          SensorType.Voltage,
                                          this,
                                          Array.Empty<ParameterDescription>(),
                                          settings);
            }
            else
            {
                _voltages[i] = new Sensor(voltInfoLabel,
                                          i,
                                          SensorType.Voltage,
                                          this,
                                          Array.Empty<ParameterDescription>(),
                                          settings);
            }
            _wasVBatValueAlreadyRead[i] = false;

            ActivateSensor(_voltages[i]);
        }

        // create temperature sensors
        _report.AppendLine("Activate temperature sensors:");
        _temperatures = new Sensor[_tempSensorCount];
        for (int i = 0; i < _tempSensorCount; i++)
        {
            string tempInfoLabel;

            // Query KSC temp info data 
            if (!KscDllQueryTempInfo((byte)i))
            {
                _report.AppendLine("Query KSC temp info data - failed! error = " + _kscError);
                return;
            }

            unsafe
            {
                fixed (byte* pLabelText = _tempSensorInfoData.Label)
                {
                    tempInfoLabel = Encoding.ASCII.GetString(pLabelText, KSC_API_LABEL_LEN).Trim('\0');
                }
            }

            _report.AppendLine("- Cat: " + _tempSensorInfoData.CategoryId);
            _report.AppendLine("- Label: " + tempInfoLabel);

            _temperatures[i] = new Sensor(tempInfoLabel,
                                          i,
                                          SensorType.Temperature,
                                          this,
                                          Array.Empty<ParameterDescription>(),
                                          settings);

            ActivateSensor(_temperatures[i]);
        }

        // create fan sensors
        _report.AppendLine("Activate fan sensors:");
        _fans = new Sensor[_fanSensorCount];
        for (int i = 0; i < _fanSensorCount; i++)
        {
            string fanInfoLabel;

            // Query KSC fan info data 
            if (!KscDllQueryFanInfo((byte)i))
            {
                _report.AppendLine("Query KSC fan info data - failed! error = " + _kscError);
                return;
            }

            unsafe
            {
                fixed (byte* pLabelText = _fanSensorInfoData.Label)
                {
                    fanInfoLabel = Encoding.ASCII.GetString(pLabelText, KSC_API_LABEL_LEN).Trim('\0');
                }
            }

            _report.AppendLine("- Cat: " + _fanSensorInfoData.CategoryId);
            _report.AppendLine("- Label: " + fanInfoLabel);

            _fans[i] = new Sensor(fanInfoLabel,
                                  i,
                                  SensorType.Fan,
                                  this,
                                  Array.Empty<ParameterDescription>(),
                                  settings);

            ActivateSensor(_fans[i]);
        }

        _report.AppendLine();
        _report.AppendLine("> All Kontron Sensor queries have been activated.");
        _report.AppendLine();

        Update();
    }

    public override HardwareType HardwareType
    {
        get { return HardwareType.KontronSensors; }
    }

    public override void Update()
    {
        // update voltage values
        for (int i = 0; i < _voltSensorCount; i++)
        {
            if (_shouldVBatSensorReadOnlyOnce[i] && _wasVBatValueAlreadyRead[i])
                continue;

            // Query KSC volt vlaue 
            if (!KscDllQueryVoltValue((byte)i))
            {
                _report.AppendLine("> An error occurred while querying the KSC volt value!");
                return;
            }

            _voltages[i].Value = (float)_voltValueInMilliVolt / 1000;

            if( !_wasVBatValueAlreadyRead[i] )
                _wasVBatValueAlreadyRead[i] = true;
        }

        // update temperature values
        for (int i = 0; i < _tempSensorCount; i++)
        {
            // Query KSC temp vlaue 
            if (!KscDllQueryTempValue((byte)i))
            {
                _report.AppendLine("> An error occurred while querying the KSC temp value!");
                return;
            }

            _temperatures[i].Value = (float)_tempValueInMilliKelvin / 1000;
        }

        // update fan values
        for (int i = 0; i < _fanSensorCount; i++)
        {
            // Query KSC fan vlaue 
            if (!KscDllQueryFanValue((byte)i))
            {
                _report.AppendLine("> An error occurred while querying the KSC fan value!");
                return;
            }

            _fans[i].Value = (float)_fanValueInRpm;
        }
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
        // voltage functions

        IntPtr kscApiVoltCountPtr = Kernel32.GetProcAddress(_kontronKscDll, "KscApiVoltCount");
        if (kscApiVoltCountPtr == IntPtr.Zero)
            return false;
        _kscApiVoltCountFunc =
            (KscApiFuncType_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiVoltCountPtr,
                                                                        typeof(KscApiFuncType_Ulong));

        IntPtr kscApiVoltInfoPtr = Kernel32.GetProcAddress(_kontronKscDll, "KscApiVoltInfo");
        if (kscApiVoltInfoPtr == IntPtr.Zero)
            return false;
        _kscApiVoltInfoFunc =
            (KscApiFuncType_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiVoltInfoPtr,
                                                                            typeof(KscApiFuncType_Byte_Ulong));

        IntPtr kscApiVoltValuePtr = Kernel32.GetProcAddress(_kontronKscDll, "KscApiVoltValue");
        if (kscApiVoltValuePtr == IntPtr.Zero)
            return false;
        _kscApiVoltValueFunc =
            (KscApiFuncType_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiVoltValuePtr,
                                                                             typeof(KscApiFuncType_Byte_Ulong));

        // temperature functions

        IntPtr kscApiTempCountPtr = Kernel32.GetProcAddress(_kontronKscDll, "KscApiTempCount");
        if (kscApiTempCountPtr == IntPtr.Zero)
            return false;
        _kscApiTempCountFunc =
            (KscApiFuncType_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiTempCountPtr,
                                                                        typeof(KscApiFuncType_Ulong));

        IntPtr kscApiTempInfoPtr = Kernel32.GetProcAddress(_kontronKscDll, "KscApiTempInfo");
        if (kscApiTempInfoPtr == IntPtr.Zero)
            return false;
        _kscApiTempInfoFunc =
            (KscApiFuncType_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiTempInfoPtr,
                                                                            typeof(KscApiFuncType_Byte_Ulong));

        IntPtr kscApiTempValuePtr = Kernel32.GetProcAddress(_kontronKscDll, "KscApiTempValue");
        if (kscApiTempValuePtr == IntPtr.Zero)
            return false;
        _kscApiTempValueFunc =
            (KscApiFuncType_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiTempValuePtr,
                                                                             typeof(KscApiFuncType_Byte_Ulong));

        // fan functions

        IntPtr kscApiFanCountPtr = Kernel32.GetProcAddress(_kontronKscDll, "KscApiFanCount");
        if (kscApiFanCountPtr == IntPtr.Zero)
            return false;
        _kscApiFanCountFunc =
            (KscApiFuncType_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiFanCountPtr,
                                                                        typeof(KscApiFuncType_Ulong));

        IntPtr kscApiFanInfoPtr = Kernel32.GetProcAddress(_kontronKscDll, "KscApiFanInfo");
        if (kscApiFanInfoPtr == IntPtr.Zero)
            return false;
        _kscApiFanInfoFunc =
            (KscApiFuncType_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiFanInfoPtr,
                                                                            typeof(KscApiFuncType_Byte_Ulong));

        IntPtr kscApiFanValuePtr = Kernel32.GetProcAddress(_kontronKscDll, "KscApiFanValue");
        if (kscApiFanValuePtr == IntPtr.Zero)
            return false;
        _kscApiFanValueFunc =
            (KscApiFuncType_Byte_Ulong)Marshal.GetDelegateForFunctionPointer(kscApiFanValuePtr,
                                                                             typeof(KscApiFuncType_Byte_Ulong));

        return true;
    }
    public static bool KscDllBuildValueAdresses()
    {
        unsafe
        {
            // volt value addresses

            fixed (byte* pVoltSensorCountValue = &_voltSensorCount)
            {
                _voltSensorCountValueAddress = (ulong)pVoltSensorCountValue;
            }
            fixed (ulong* pVoltValue = &_voltValueInMilliVolt)
            {
                _voltValueAddress = (ulong)pVoltValue;
            }

            // temp value addresses

            fixed (byte* pTempSensorCountValue = &_tempSensorCount)
            {
                _tempSensorCountValueAddress = (ulong)pTempSensorCountValue;
            }
            fixed (ulong* pTempValue = &_tempValueInMilliKelvin)
            {
                _tempValueAddress = (ulong)pTempValue;
            }

            // fan value addresses

            fixed (byte* pFanSensorCountValue = &_fanSensorCount)
            {
                _fanSensorCountValueAddress = (ulong)pFanSensorCountValue;
            }
            fixed (ulong* pFanValue = &_fanValueInRpm)
            {
                _fanValueAddress = (ulong)pFanValue;
            }
        }

        return true;
    }

    public static bool KscDllQueryAllSensorCounts()
    {
        int result = 0;

        result = _kscApiVoltCountFunc(_voltSensorCountValueAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        result = _kscApiTempCountFunc(_tempSensorCountValueAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        result = _kscApiFanCountFunc(_fanSensorCountValueAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }

    public static bool KscDllQueryVoltInfo(byte index)
    {
        ulong voltSensorInfoDataAddress;

        unsafe
        {
            fixed (KscApiSensorInfoData* pKscVoltInfoData = &_voltSensorInfoData)
            {
                voltSensorInfoDataAddress = (ulong)pKscVoltInfoData;
            }
        }

        int result = _kscApiVoltInfoFunc(index, voltSensorInfoDataAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }

    public static bool KscDllQueryVoltValue(byte index)
    {
        int result = _kscApiVoltValueFunc(index, _voltValueAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }

    public static bool KscDllQueryTempInfo(byte index)
    {
        ulong tempSensorInfoDataAddress;

        unsafe
        {
            fixed (KscApiSensorInfoData* pKscTempInfoData = &_tempSensorInfoData)
            {
                tempSensorInfoDataAddress = (ulong)pKscTempInfoData;
            }
        }

        int result = _kscApiTempInfoFunc(index, tempSensorInfoDataAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }

    public static bool KscDllQueryTempValue(byte index)
    {
        int result = _kscApiTempValueFunc(index, _tempValueAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }

    public static bool KscDllQueryFanInfo(byte index)
    {
        ulong fanSensorInfoDataAddress;

        unsafe
        {
            fixed (KscApiSensorInfoData* pKscFanInfoData = &_fanSensorInfoData)
            {
                fanSensorInfoDataAddress = (ulong)pKscFanInfoData;
            }

        }

        int result = _kscApiFanInfoFunc(index, fanSensorInfoDataAddress);
        if (result != 0)
        {
            _kscError = result;
            return false;
        }

        return true;
    }

    public static bool KscDllQueryFanValue(byte index)
    {
        int result = _kscApiFanValueFunc(index, _fanValueAddress);
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

internal enum KscApiVoltCategory
{
    eKscApiVolt_VCore = 1,
    eKscApiVolt_1_8V,
    eKscApiVolt_2_5V,
    eKscApiVolt_3_3V,
    eKscApiVolt_VBat,
    eKscApiVolt_5V,
    eKscApiVolt_5V_Stdby,
    eKscApiVolt_12V,
    eKscApiVolt_AC,
    eKscApiVolt_Other
}
