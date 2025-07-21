// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RAMSPDToolkit.I2CSMBus;
using RAMSPDToolkit.SPD;
using RAMSPDToolkit.SPD.Enums;
using RAMSPDToolkit.SPD.Interfaces;
using RAMSPDToolkit.SPD.Interop.Shared;
using RAMSPDToolkit.Windows.Driver;

namespace LibreHardwareMonitor.Hardware.Memory;

internal class MemoryGroup : IGroup, IHardwareChanged
{
    private static readonly object _lock = new();
    private List<Hardware> _hardware = [];

    private CancellationTokenSource _cancellationTokenSource;
    private Exception _lastException;
    private bool _opened = false;

    public MemoryGroup(ISettings settings)
    {
        if (Ring0.IsOpen && (DriverManager.Driver is null || !DriverManager.Driver.IsOpen))
        {
            // Assign implementation of IDriver.
            DriverManager.Driver = new RAMSPDToolkitDriver(Ring0.KernelDriver);
            SMBusManager.UseWMI = false;
        }

        _hardware.Add(new VirtualMemory(settings));
        _hardware.Add(new TotalMemory(settings));

        if (DriverManager.Driver == null)
        {
            return;
        }

        if (!TryAddDimms(settings))
        {
            StartRetryTask(settings);
        }

        _opened = true;
    }

    public event HardwareEventHandler HardwareAdded;
    public event HardwareEventHandler HardwareRemoved;

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        StringBuilder report = new();
        report.AppendLine("Memory Report:");
        if (_lastException != null)
        {
            report.AppendLine($"Error while detecting memory: {_lastException.Message}");
        }

        foreach (Hardware hardware in _hardware)
        {
            report.AppendLine($"{hardware.Name} ({hardware.Identifier}):");
            report.AppendLine();
            foreach (ISensor sensor in hardware.Sensors)
            {
                report.AppendLine($"{sensor.Name}: {sensor.Value?.ToString() ?? "No value"}");
            }
        }

        return report.ToString();
    }

    public void Close()
    {
        lock (_lock)
        {
            _opened = false;
            foreach (Hardware ram in _hardware)
                ram.Close();

            _hardware.Clear();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private bool TryAddDimms(ISettings settings)
    {
        try
        {
            lock (_lock)
            {
                if (!_opened)
                {
                    return true;
                }

                if (DetectThermalSensors(out List<SPDAccessor> accessors))
                {
                    AddDimms(accessors, settings);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _lastException = ex;
            Debug.Assert(false, "Exception while detecting RAM: " + ex.Message);
        }

        return false;
    }

    private void StartRetryTask(ISettings settings)
    {
        _cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            int retryRemaining = 5;

            while (!_cancellationTokenSource.IsCancellationRequested && --retryRemaining > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2.5), _cancellationTokenSource.Token).ConfigureAwait(false);

                if (TryAddDimms(settings))
                {
                    lock (_lock)
                    {
                        if (!_opened)
                        {
                            return;
                        }

                        foreach (Hardware hardware in _hardware.OfType<DimmMemory>())
                        {
                            HardwareAdded?.Invoke(hardware);
                        }

                        _cancellationTokenSource.Dispose();
                        _cancellationTokenSource = null;

                        break;
                    }

                }
            }
        }, _cancellationTokenSource.Token);
    }

    private static bool DetectThermalSensors(out List<SPDAccessor> accessors)
    {
        accessors = [];

        bool ramDetected = false;

        SMBusManager.DetectSMBuses();

        //Go through detected SMBuses
        foreach (SMBusInterface smbus in SMBusManager.RegisteredSMBuses)
        {
            //Go through possible RAM slots
            for (byte i = SPDConstants.SPD_BEGIN; i <= SPDConstants.SPD_END; ++i)
            {
                //Detect type of RAM, if available
                SPDDetector detector = new(smbus, i);

                //RAM available and detected
                if (detector.Accessor != null)
                {
                    //We are only interested in modules with thermal sensor
                    if (detector.Accessor is IThermalSensor { HasThermalSensor: true })
                        accessors.Add(detector.Accessor);

                    ramDetected = true;
                }
            }
        }

        return ramDetected;
    }

    private void AddDimms(List<SPDAccessor> accessors, ISettings settings)
    {
        List<Hardware> newHardwareList = [.. _hardware];

        foreach (SPDAccessor ram in accessors)
        {
            //Default value
            string name = $"DIMM #{ram.Index}";

            //Check if we can switch to the correct page
            if (ram.ChangePage(PageData.ModulePartNumber))
                name = $"{ram.GetModuleManufacturerString()} - {ram.ModulePartNumber()} (#{ram.Index})";

            DimmMemory memory = new(ram, name, new Identifier($"memory/dimm/{ram.Index}"), settings);
            newHardwareList.Add(memory);
        }

        _hardware = newHardwareList;
    }
}
