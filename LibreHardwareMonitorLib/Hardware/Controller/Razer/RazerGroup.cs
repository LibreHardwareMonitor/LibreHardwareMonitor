// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Razer;

internal class RazerGroup : IGroup
{
    private readonly List<IHardware> _hardware = new();
    private readonly StringBuilder _report = new();

    public RazerGroup(ISettings settings)
    {
        _report.AppendLine("Razer Hardware");
        _report.AppendLine();

        foreach (HidDevice dev in DeviceList.Local.GetHidDevices(0x1532))
        {
            string productName = dev.GetProductName();

            switch (dev.ProductID)
            {
                case 0x0F3C: // Razer PWM PC fan controller
                    if (dev.GetMaxFeatureReportLength() <= 0)
                        break;
                    var device = new RazerFanController(dev, settings);
                    _report.AppendLine($"Device name: {productName}");
                    _report.AppendLine($"Firmware version: {device.FirmwareVersion}");
                    _report.AppendLine($"{device.Status}");
                    _report.AppendLine();
                    _hardware.Add(device);
                    break;

                default:
                    _report.AppendLine($"Unknown Hardware PID: {dev.ProductID} Name: {productName}");
                    _report.AppendLine();
                    break;
            }
        }

        if (_hardware.Count == 0)
        {
            _report.AppendLine("No Razer Hardware found.");
            _report.AppendLine();
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public void Close()
    {
        foreach (IHardware iHardware in _hardware)
        {
            if (iHardware is Hardware hardware)
                hardware.Close();
        }
    }

    public string GetReport()
    {
        return _report.ToString();
    }
}

internal static class RazerGuard
{
    private static Mutex _razerMutex;

    public static void Open()
    {
        const string razerMutexName = "Global\\RazerReadWriteGuardMutex";
        if (!TryCreateOrOpenExistingMutex(razerMutexName, out _razerMutex))
        {
            // Mutex could not be created or opened.
        }
    }

    public static void Close()
    {
        if (_razerMutex != null)
        {
            _razerMutex.Close();
            _razerMutex = null;
        }
    }

    public static bool WaitRazerMutex(int millisecondsTimeout)
    {
        if (_razerMutex == null)
            return true;

        try
        {
            return _razerMutex.WaitOne(millisecondsTimeout, false);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static void ReleaseRazerMutex()
    {
        _razerMutex?.ReleaseMutex();
    }

    private static bool TryCreateOrOpenExistingMutex(string name, out Mutex mutex)
    {
        try
        {
            mutex = new Mutex(false, name);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            try
            {
                mutex = Mutex.OpenExisting(name);
                return true;
            }
            catch
            {
                mutex = null;
            }
        }

        return false;
    }
}
