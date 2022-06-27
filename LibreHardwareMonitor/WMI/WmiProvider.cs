// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Management.Instrumentation;
using LibreHardwareMonitor.Hardware;

[assembly: Instrumented("root/LibreHardwareMonitor")]

[System.ComponentModel.RunInstaller(true)]
public class InstanceInstaller : DefaultManagementProjectInstaller
{ }

namespace LibreHardwareMonitor.Wmi
{
    /// <summary>
    /// The WMI Provider.
    /// This class is not exposed to WMI itself.
    /// </summary>
    public class WmiProvider : IDisposable
    {
        private readonly object _activeInstancesLock = new();
        private readonly List<IWmiObject> _activeInstances;

        public WmiProvider(IComputer computer)
        {
            _activeInstances = new List<IWmiObject>();
            foreach (IHardware hardware in computer.Hardware)
                OnHardwareAdded(hardware);

            computer.HardwareAdded += OnHardwareAdded;
            computer.HardwareRemoved += OnHardwareRemoved;
        }

        public void Update()
        {
            lock (_activeInstancesLock)
            {
                foreach (IWmiObject instance in _activeInstances)
                    instance.Update();
            }
        }

        private void OnHardwareAdded(IHardware hardware)
        {
            lock (_activeInstancesLock)
            {
                if (!_activeInstances.Exists(h => h.Identifier == hardware.Identifier.ToString()))
                {
                    foreach (ISensor sensor in hardware.Sensors)
                        OnSensorAdded(sensor);

                    hardware.SensorAdded += OnSensorAdded;
                    hardware.SensorRemoved += HardwareSensorRemoved;

                    Hardware hw = new(hardware);
                    _activeInstances.Add(hw);

                    try
                    {
                        Instrumentation.Publish(hw);
                    }
                    catch
                    { }
                }
            }

            foreach (IHardware subHardware in hardware.SubHardware)
                OnHardwareAdded(subHardware);
        }

        private void OnSensorAdded(ISensor data)
        {
            Sensor sensor = new(data);

            lock (_activeInstancesLock)
                _activeInstances.Add(sensor);

            try
            {
                Instrumentation.Publish(sensor);
            }
            catch
            { }
        }

        private void OnHardwareRemoved(IHardware hardware)
        {
            hardware.SensorAdded -= OnSensorAdded;
            hardware.SensorRemoved -= HardwareSensorRemoved;

            foreach (ISensor sensor in hardware.Sensors)
                HardwareSensorRemoved(sensor);

            foreach (IHardware subHardware in hardware.SubHardware)
                OnHardwareRemoved(subHardware);

            RevokeInstance(hardware.Identifier.ToString());
        }

        private void HardwareSensorRemoved(ISensor sensor)
        {
            RevokeInstance(sensor.Identifier.ToString());
        }

        private void RevokeInstance(string identifier)
        {
            lock (_activeInstancesLock)
            {
                int instanceIndex = _activeInstances.FindIndex(item => item.Identifier == identifier);
                if (instanceIndex == -1)
                    return;

                try
                {
                    Instrumentation.Revoke(_activeInstances[instanceIndex]);
                }
                catch
                { }

                _activeInstances.RemoveAt(instanceIndex);
            }
        }

        public void Dispose()
        {
            lock (_activeInstancesLock)
            {
                foreach (IWmiObject instance in _activeInstances)
                {
                    try
                    {
                        Instrumentation.Revoke(instance);
                    }
                    catch
                    { }
                }
            }
        }
    }
}
