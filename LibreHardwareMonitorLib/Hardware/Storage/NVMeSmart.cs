// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Storage;

public class NVMeSmart : IDisposable
{
    private readonly int _driveNumber;
    private readonly SafeHandle _handle;

    internal NVMeSmart(StorageInfo storageInfo)
    {
        _driveNumber = storageInfo.Index;
        NVMeDrive = null;
        string name = storageInfo.Name;

        // Test Samsung protocol.
        if (NVMeDrive == null && name.IndexOf("Samsung", StringComparison.OrdinalIgnoreCase) > -1)
        {
            _handle = NVMeSamsung.IdentifyDevice(storageInfo);
            if (_handle != null)
            {
                NVMeDrive = new NVMeSamsung();
                if (!NVMeDrive.IdentifyController(_handle, out _))
                {
                    NVMeDrive = null;
                }
            }
        }

        // Test Intel protocol.
        if (NVMeDrive == null && name.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) > -1)
        {
            _handle = NVMeIntel.IdentifyDevice(storageInfo);
            if (_handle != null)
            {
                NVMeDrive = new NVMeIntel();
            }
        }

        // Test Intel raid protocol.
        if (NVMeDrive == null && name.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) > -1)
        {
            _handle = NVMeIntelRst.IdentifyDevice(storageInfo);
            if (_handle != null)
            {
                NVMeDrive = new NVMeIntelRst();
            }
        }

        // Test Windows generic driver protocol.
        if (NVMeDrive == null)
        {
            _handle = NVMeWindows.IdentifyDevice(storageInfo);
            if (_handle != null)
            {
                NVMeDrive = new NVMeWindows();
            }
        }
    }

    public bool IsValid
    {
        get
        {
            return _handle is { IsInvalid: false };
        }
    }

    internal INVMeDrive NVMeDrive { get; }

    public void Dispose()
    {
        Close();
    }

    private static string GetString(byte[] s)
    {
        return Encoding.ASCII.GetString(s).Trim('\t', '\n', '\r', ' ', '\0');
    }

    private static short KelvinToCelsius(ushort k)
    {
        return (short)(k > 0 ? k - 273 : short.MinValue);
    }

    private static short KelvinToCelsius(byte[] k)
    {
        return KelvinToCelsius(BitConverter.ToUInt16(k, 0));
    }

    public void Close()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (disposing && _handle is { IsClosed: false })
        {
            _handle.Close();
        }
    }

    public Storage.NVMeInfo GetInfo()
    {
        if (_handle?.IsClosed != false)
            return null;

        bool valid = false;
        var data = new Kernel32.NVME_IDENTIFY_CONTROLLER_DATA();
        if (NVMeDrive != null)
            valid = NVMeDrive.IdentifyController(_handle, out data);

        if (!valid)
            return null;

        return new NVMeInfo(_driveNumber, data);
    }

    public Storage.NVMeHealthInfo GetHealthInfo()
    {
        if (_handle?.IsClosed != false)
            return null;

        bool valid = false;
        var data = new Kernel32.NVME_HEALTH_INFO_LOG();
        if (NVMeDrive != null)
            valid = NVMeDrive.HealthInfoLog(_handle, out data);

        if (!valid)
            return null;

        return new NVMeHealthInfo(data);
    }

    private class NVMeInfo : Storage.NVMeInfo
    {
        public NVMeInfo(int index, Kernel32.NVME_IDENTIFY_CONTROLLER_DATA data)
        {
            Index = index;
            VID = data.VID;
            SSVID = data.SSVID;
            Serial = GetString(data.SN);
            Model = GetString(data.MN);
            Revision = GetString(data.FR);
            IEEE = data.IEEE;
            TotalCapacity = BitConverter.ToUInt64(data.TNVMCAP, 0); // 128bit little endian
            UnallocatedCapacity = BitConverter.ToUInt64(data.UNVMCAP, 0);
            ControllerId = data.CNTLID;
            NumberNamespaces = data.NN;
        }
    }

    private class NVMeHealthInfo : Storage.NVMeHealthInfo
    {
        public NVMeHealthInfo(Kernel32.NVME_HEALTH_INFO_LOG log)
        {
            CriticalWarning = (Kernel32.NVME_CRITICAL_WARNING)log.CriticalWarning;
            Temperature = KelvinToCelsius(log.CompositeTemp);
            AvailableSpare = log.AvailableSpare;
            AvailableSpareThreshold = log.AvailableSpareThreshold;
            PercentageUsed = log.PercentageUsed;
            DataUnitRead = BitConverter.ToUInt64(log.DataUnitRead, 0);
            DataUnitWritten = BitConverter.ToUInt64(log.DataUnitWritten, 0);
            HostReadCommands = BitConverter.ToUInt64(log.HostReadCommands, 0);
            HostWriteCommands = BitConverter.ToUInt64(log.HostWriteCommands, 0);
            ControllerBusyTime = BitConverter.ToUInt64(log.ControllerBusyTime, 0);
            PowerCycle = BitConverter.ToUInt64(log.PowerCycles, 0);
            PowerOnHours = BitConverter.ToUInt64(log.PowerOnHours, 0);
            UnsafeShutdowns = BitConverter.ToUInt64(log.UnsafeShutdowns, 0);
            MediaErrors = BitConverter.ToUInt64(log.MediaAndDataIntegrityErrors, 0);
            ErrorInfoLogEntryCount = BitConverter.ToUInt64(log.NumberErrorInformationLogEntries, 0);
            WarningCompositeTemperatureTime = log.WarningCompositeTemperatureTime;
            CriticalCompositeTemperatureTime = log.CriticalCompositeTemperatureTime;

            TemperatureSensors = new short[log.TemperatureSensor.Length];
            for (int i = 0; i < TemperatureSensors.Length; i++)
                TemperatureSensors[i] = KelvinToCelsius(log.TemperatureSensor[i]);
        }
    }
}