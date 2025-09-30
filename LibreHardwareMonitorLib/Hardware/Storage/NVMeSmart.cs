// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32.Storage.Nvme;
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

        // Test Windows generic driver protocol.
        if (NVMeDrive == null)
        {
            _handle = NVMeWindows.IdentifyDevice(storageInfo);
            if (_handle != null)
            {
                NVMeDrive = new NVMeWindows();
            }
        }

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
        var data = new NVME_IDENTIFY_CONTROLLER_DATA();
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
        var data = new NVME_HEALTH_INFO_LOG();
        if (NVMeDrive != null)
            valid = NVMeDrive.HealthInfoLog(_handle, out data);

        if (!valid)
            return null;

        return new NVMeHealthInfo(data);
    }

    private class NVMeInfo : Storage.NVMeInfo
    {
        public NVMeInfo(int index, NVME_IDENTIFY_CONTROLLER_DATA data)
        {
            Index = index;
            VID = data.VID;
            SSVID = data.SSVID;
            Serial = GetString(data.SN.ToArray());
            Model = GetString(data.MN.ToArray());
            Revision = GetString(data.FR.ToArray());
            IEEE = data.IEEE.ToArray();
            TotalCapacity = BitConverter.ToUInt64(data.TNVMCAP.ToArray(), 0); // 128bit little endian
            UnallocatedCapacity = BitConverter.ToUInt64(data.UNVMCAP.ToArray(), 0);
            ControllerId = data.CNTLID;
            NumberNamespaces = data.NN;
        }
    }

    private class NVMeHealthInfo : Storage.NVMeHealthInfo
    {
        public NVMeHealthInfo(NVME_HEALTH_INFO_LOG log)
        {
            Temperature = KelvinToCelsius(log.Temperature.ToArray());
            AvailableSpare = log.AvailableSpare;
            AvailableSpareThreshold = log.AvailableSpareThreshold;
            PercentageUsed = log.PercentageUsed;
            DataUnitRead = BitConverter.ToUInt64(log.DataUnitRead.ToArray(), 0);
            DataUnitWritten = BitConverter.ToUInt64(log.DataUnitWritten.ToArray(), 0);
            HostReadCommands = BitConverter.ToUInt64(log.HostReadCommands.ToArray(), 0);
            HostWriteCommands = BitConverter.ToUInt64(log.HostWrittenCommands.ToArray(), 0);
            ControllerBusyTime = BitConverter.ToUInt64(log.ControllerBusyTime.ToArray(), 0);
            PowerCycle = BitConverter.ToUInt64(log.PowerCycle.ToArray(), 0);
            PowerOnHours = BitConverter.ToUInt64(log.PowerOnHours.ToArray(), 0);
            UnsafeShutdowns = BitConverter.ToUInt64(log.UnsafeShutdowns.ToArray(), 0);
            MediaErrors = BitConverter.ToUInt64(log.MediaErrors.ToArray(), 0);
            ErrorInfoLogEntryCount = BitConverter.ToUInt64(log.ErrorInfoLogEntryCount.ToArray(), 0);
            WarningCompositeTemperatureTime = log.WarningCompositeTemperatureTime;
            CriticalCompositeTemperatureTime = log.CriticalCompositeTemperatureTime;

            TemperatureSensors = new short[8];
            TemperatureSensors[0] = KelvinToCelsius(log.TemperatureSensor1);
            TemperatureSensors[1] = KelvinToCelsius(log.TemperatureSensor2);
            TemperatureSensors[2] = KelvinToCelsius(log.TemperatureSensor3);
            TemperatureSensors[3] = KelvinToCelsius(log.TemperatureSensor4);
            TemperatureSensors[4] = KelvinToCelsius(log.TemperatureSensor5);
            TemperatureSensors[5] = KelvinToCelsius(log.TemperatureSensor6);
            TemperatureSensors[6] = KelvinToCelsius(log.TemperatureSensor7);
            TemperatureSensors[7] = KelvinToCelsius(log.TemperatureSensor8);
        }
    }
}
