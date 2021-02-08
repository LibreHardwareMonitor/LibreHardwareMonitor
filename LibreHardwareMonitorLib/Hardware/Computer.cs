// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Permissions;
using LibreHardwareMonitor.Hardware.Controller.AeroCool;
using LibreHardwareMonitor.Hardware.Controller.AquaComputer;
using LibreHardwareMonitor.Hardware.Controller.Heatmaster;
using LibreHardwareMonitor.Hardware.Controller.Nzxt;
using LibreHardwareMonitor.Hardware.Controller.TBalancer;
using LibreHardwareMonitor.Hardware.Gpu;
using LibreHardwareMonitor.Hardware.Memory;
using LibreHardwareMonitor.Hardware.Motherboard;
using LibreHardwareMonitor.Hardware.Network;
using LibreHardwareMonitor.Hardware.Storage;

namespace LibreHardwareMonitor.Hardware
{
    public class Computer : IComputer
    {
        public event HardwareEventHandler HardwareAdded;
        public event HardwareEventHandler HardwareRemoved;

        private readonly List<IGroup> _groups = new List<IGroup>();
        private readonly ISettings _settings;
        private bool _controllerEnabled;
        private bool _cpuEnabled;
        private bool _gpuEnabled;
        private readonly object _lock = new object();
        private bool _memoryEnabled;
        private bool _motherboardEnabled;
        private bool _networkEnabled;
        private bool _open;
        private SMBios _smbios;
        private bool _storageEnabled;

        public Computer()
        {
            _settings = new Settings();
        }

        public Computer(ISettings settings)
        {
            _settings = settings ?? new Settings();
        }

        public IList<IHardware> Hardware
        {
            get
            {
                lock (_lock)
                {
                    List<IHardware> list = new List<IHardware>();

                    foreach (IGroup group in _groups)
                        list.AddRange(group.Hardware);

                    return list;
                }
            }
        }

        public bool IsCpuEnabled
        {
            get { return _cpuEnabled; }
            set
            {
                if (_open && value != _cpuEnabled)
                {
                    if (value)
                        Add(new CPU.CpuGroup(_settings));
                    else
                        RemoveType<CPU.CpuGroup>();
                }

                _cpuEnabled = value;
            }
        }

        public bool IsControllerEnabled
        {
            get { return _controllerEnabled; }
            set
            {
                if (_open && value != _controllerEnabled)
                {
                    if (value)
                    {
                        Add(new TBalancerGroup(_settings));
                        Add(new HeatmasterGroup(_settings));
                        Add(new AquaComputerGroup(_settings));
                        Add(new AeroCoolGroup(_settings));
                        Add(new NzxtGroup(_settings));
                    }
                    else
                    {
                        RemoveType<TBalancerGroup>();
                        RemoveType<HeatmasterGroup>();
                        RemoveType<AquaComputerGroup>();
                        RemoveType<AeroCoolGroup>();
                        RemoveType<NzxtGroup>();
                    }
                }

                _controllerEnabled = value;
            }
        }

        public bool IsGpuEnabled
        {
            get { return _gpuEnabled; }
            set
            {
                if (_open && value != _gpuEnabled)
                {
                    if (value)
                    {
                        Add(new AmdGpuGroup(_settings));
                        Add(new NvidiaGroup(_settings));
                    }
                    else
                    {
                        RemoveType<AmdGpuGroup>();
                        RemoveType<NvidiaGroup>();
                    }
                }

                _gpuEnabled = value;
            }
        }

        public bool IsMemoryEnabled
        {
            get { return _memoryEnabled; }
            set
            {
                if (_open && value != _memoryEnabled)
                {
                    if (value)
                        Add(new MemoryGroup(_settings));
                    else
                        RemoveType<MemoryGroup>();
                }

                _memoryEnabled = value;
            }
        }

        public bool IsMotherboardEnabled
        {
            get { return _motherboardEnabled; }
            set
            {
                if (_open && value != _motherboardEnabled)
                {
                    if (value)
                        Add(new MotherboardGroup(_smbios, _settings));
                    else
                        RemoveType<MotherboardGroup>();
                }

                _motherboardEnabled = value;
            }
        }

        public bool IsNetworkEnabled
        {
            get { return _networkEnabled; }
            set
            {
                if (_open && value != _networkEnabled)
                {
                    if (value)
                        Add(new NetworkGroup(_settings));
                    else
                        RemoveType<NetworkGroup>();
                }

                _networkEnabled = value;
            }
        }

        public bool IsStorageEnabled
        {
            get { return _storageEnabled; }
            set
            {
                if (_open && value != _storageEnabled)
                {
                    if (value)
                        Add(new StorageGroup(_settings));
                    else
                        RemoveType<StorageGroup>();
                }

                _storageEnabled = value;
            }
        }

        public string GetReport()
        {
            lock (_lock)
            {
                using StringWriter w = new StringWriter(CultureInfo.InvariantCulture);

                w.WriteLine();
                w.WriteLine(nameof(LibreHardwareMonitor) + " Report");
                w.WriteLine();

                Version version = typeof(Computer).Assembly.GetName().Version;

                NewSection(w);
                w.Write("Version: ");
                w.WriteLine(version.ToString());
                w.WriteLine();

                NewSection(w);
                w.Write("Common Language Runtime: ");
                w.WriteLine(Environment.Version.ToString());
                w.Write("Operating System: ");
                w.WriteLine(Environment.OSVersion.ToString());
                w.Write("Process Type: ");
                w.WriteLine(IntPtr.Size == 4 ? "32-Bit" : "64-Bit");
                w.WriteLine();

                string r = Ring0.GetReport();
                if (r != null)
                {
                    NewSection(w);
                    w.Write(r);
                    w.WriteLine();
                }

                NewSection(w);
                w.WriteLine("Sensors");
                w.WriteLine();

                foreach (IGroup group in _groups)
                {
                    foreach (IHardware hardware in group.Hardware)
                        ReportHardwareSensorTree(hardware, w, string.Empty);
                }

                w.WriteLine();

                NewSection(w);
                w.WriteLine("Parameters");
                w.WriteLine();

                foreach (IGroup group in _groups)
                {
                    foreach (IHardware hardware in group.Hardware)
                        ReportHardwareParameterTree(hardware, w, string.Empty);
                }

                w.WriteLine();

                foreach (IGroup group in _groups)
                {
                    string report = group.GetReport();
                    if (!string.IsNullOrEmpty(report))
                    {
                        NewSection(w);
                        w.Write(report);
                    }

                    IEnumerable<IHardware> hardwareArray = group.Hardware;
                    foreach (IHardware hardware in hardwareArray)
                        ReportHardware(hardware, w);
                }

                return w.ToString();
            }
        }

        public void Accept(IVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException(nameof(visitor));


            visitor.VisitComputer(this);
        }

        public void Traverse(IVisitor visitor)
        {
            lock (_lock)
            {
                // Use a for-loop instead of foreach to avoid a collection modified exception after sleep, even though everything is under a lock.
                for (int i = 0; i < _groups.Count; i++)
                {
                    IGroup group = _groups[i];

                    for (int j = 0; j < group.Hardware.Count; j++)
                        group.Hardware[j].Accept(visitor);
                }
            }
        }

        private void HardwareAddedEvent(IHardware hardware)
        {
            HardwareAdded?.Invoke(hardware);
        }

        private void HardwareRemovedEvent(IHardware hardware)
        {
            HardwareRemoved?.Invoke(hardware);
        }

        private void Add(IGroup group)
        {
            if (group == null)
                return;


            lock (_lock)
            {
                if (_groups.Contains(group))
                    return;


                _groups.Add(group);

                if (group is IHardwareChanged hardwareChanged)
                {
                    hardwareChanged.HardwareAdded += HardwareAddedEvent;
                    hardwareChanged.HardwareRemoved += HardwareRemovedEvent;
                }
            }

            if (HardwareAdded != null)
            {
                foreach (IHardware hardware in group.Hardware)
                    HardwareAdded(hardware);
            }
        }

        private void Remove(IGroup group)
        {
            lock (_lock)
            {
                if (!_groups.Contains(group))
                    return;


                _groups.Remove(group);

                if (group is IHardwareChanged hardwareChanged)
                {
                    hardwareChanged.HardwareAdded -= HardwareAddedEvent;
                    hardwareChanged.HardwareRemoved -= HardwareRemovedEvent;
                }
            }

            if (HardwareRemoved != null)
            {
                foreach (IHardware hardware in group.Hardware)
                    HardwareRemoved(hardware);
            }

            group.Close();
        }

        private void RemoveType<T>() where T : IGroup
        {
            List<T> list = new List<T>();

            lock (_lock)
            {
                foreach (IGroup group in _groups)
                {
                    if (group is T t)
                        list.Add(t);
                }
            }

            foreach (T group in list)
                Remove(group);
        }

        public void Open()
        {
            if (_open)
                return;


            _smbios = new SMBios();

            Ring0.Open();
            OpCode.Open();

            AddGroups();

            _open = true;
        }

        private void AddGroups()
        {
            if (_motherboardEnabled)
                Add(new MotherboardGroup(_smbios, _settings));

            if (_cpuEnabled)
                Add(new CPU.CpuGroup(_settings));

            if (_memoryEnabled)
                Add(new MemoryGroup(_settings));

            if (_gpuEnabled)
            {
                Add(new AmdGpuGroup(_settings));
                Add(new NvidiaGroup(_settings));
            }

            if (_controllerEnabled)
            {
                Add(new TBalancerGroup(_settings));
                Add(new HeatmasterGroup(_settings));
                Add(new AquaComputerGroup(_settings));
                Add(new AeroCoolGroup(_settings));
                Add(new NzxtGroup(_settings));
            }

            if (_storageEnabled)
                Add(new StorageGroup(_settings));

            if (_networkEnabled)
                Add(new NetworkGroup(_settings));
        }

        private static void NewSection(TextWriter writer)
        {
            for (int i = 0; i < 8; i++)
                writer.Write("----------");

            writer.WriteLine();
            writer.WriteLine();
        }

        private static int CompareSensor(ISensor a, ISensor b)
        {
            int c = a.SensorType.CompareTo(b.SensorType);
            if (c == 0)
                return a.Index.CompareTo(b.Index);


            return c;
        }

        private static void ReportHardwareSensorTree(IHardware hardware, TextWriter w, string space)
        {
            w.WriteLine("{0}|", space);
            w.WriteLine("{0}+- {1} ({2})", space, hardware.Name, hardware.Identifier);

            ISensor[] sensors = hardware.Sensors;
            Array.Sort(sensors, CompareSensor);

            foreach (ISensor sensor in sensors)
                w.WriteLine("{0}|  +- {1,-14} : {2,8:G6} {3,8:G6} {4,8:G6} ({5})", space, sensor.Name, sensor.Value, sensor.Min, sensor.Max, sensor.Identifier);

            foreach (IHardware subHardware in hardware.SubHardware)
                ReportHardwareSensorTree(subHardware, w, "|  ");
        }

        private static void ReportHardwareParameterTree(IHardware hardware, TextWriter w, string space)
        {
            w.WriteLine("{0}|", space);
            w.WriteLine("{0}+- {1} ({2})", space, hardware.Name, hardware.Identifier);

            ISensor[] sensors = hardware.Sensors;
            Array.Sort(sensors, CompareSensor);

            foreach (ISensor sensor in sensors)
            {
                string innerSpace = space + "|  ";
                if (sensor.Parameters.Count > 0)
                {
                    w.WriteLine("{0}|", innerSpace);
                    w.WriteLine("{0}+- {1} ({2})", innerSpace, sensor.Name, sensor.Identifier);

                    foreach (IParameter parameter in sensor.Parameters)
                    {
                        string innerInnerSpace = innerSpace + "|  ";
                        w.WriteLine("{0}+- {1} : {2}", innerInnerSpace, parameter.Name, string.Format(CultureInfo.InvariantCulture, "{0} : {1}", parameter.DefaultValue, parameter.Value));
                    }
                }
            }

            foreach (IHardware subHardware in hardware.SubHardware)
                ReportHardwareParameterTree(subHardware, w, "|  ");
        }

        private static void ReportHardware(IHardware hardware, TextWriter w)
        {
            string hardwareReport = hardware.GetReport();
            if (!string.IsNullOrEmpty(hardwareReport))
            {
                NewSection(w);
                w.Write(hardwareReport);
            }

            foreach (IHardware subHardware in hardware.SubHardware)
                ReportHardware(subHardware, w);
        }

        public void Close()
        {
            if (!_open)
                return;


            lock (_lock)
            {
                while (_groups.Count > 0)
                {
                    IGroup group = _groups[_groups.Count - 1];
                    Remove(group);
                }
            }

            OpCode.Close();
            Ring0.Close();

            _smbios = null;
            _open = false;
        }

        public void Reset()
        {
            if (!_open)
                return;


            RemoveGroups();
            AddGroups();
        }

        private void RemoveGroups()
        {
            lock (_lock)
            {
                while (_groups.Count > 0)
                {
                    IGroup group = _groups[_groups.Count - 1];
                    Remove(group);
                }
            }
        }

        private class Settings : ISettings
        {
            public bool Contains(string name)
            {
                return false;
            }

            public void SetValue(string name, string value)
            { }

            public string GetValue(string name, string value)
            {
                return value;
            }

            public void Remove(string name)
            { }
        }
    }
}
