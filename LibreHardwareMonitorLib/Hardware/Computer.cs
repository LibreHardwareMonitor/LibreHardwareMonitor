// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Permissions;
using System.Net.NetworkInformation;

namespace LibreHardwareMonitor.Hardware
{
    public class Computer : IComputer
    {
        private readonly List<IGroup> _groups = new List<IGroup>();
        private readonly ISettings _settings;
        private SmBios _smbios;
        private bool _open;

        private bool _mainboardEnabled;
        private bool _cpuEnabled;
        private bool _ramEnabled;
        private bool _gpuEnabled;
        private bool _fanControllerEnabled;
        private bool _hddEnabled;
        private bool _nicEnabled;
        private int _nicCount;

        public event HardwareEventHandler HardwareAdded;
        public event HardwareEventHandler HardwareRemoved;

        public Computer()
        {
            _settings = new Settings();
        }

        public Computer(ISettings settings)
        {
            _settings = settings ?? new Settings();
        }

        private void Add(IGroup group)
        {
            if (_groups.Contains(group))
                return;

            _groups.Add(group);

            if (HardwareAdded != null)
            {
                foreach (IHardware hardware in group.Hardware)
                    HardwareAdded(hardware);
            }
        }

        private void Remove(IGroup group)
        {
            if (!_groups.Contains(group))
                return;

            _groups.Remove(group);
            if (HardwareRemoved != null)
            {
                foreach (IHardware hardware in group.Hardware)
                    HardwareRemoved(hardware);
            }
            group.Close();
        }

        private void RemoveType<T>() where T : IGroup
        {
            List<IGroup> list = new List<IGroup>();
            foreach (IGroup group in _groups)
            {
                if (group is T)
                    list.Add(group);
            }
            foreach (IGroup group in list)
                Remove(group);
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public void Open()
        {
            if (_open)
                return;

            _smbios = new SmBios();

            Ring0.Open();
            Opcode.Open();

            if (_mainboardEnabled)
                Add(new Mainboard.MainboardGroup(_smbios, _settings));

            if (_cpuEnabled)
                Add(new CPU.CpuGroup(_settings));

            if (_ramEnabled)
                Add(new Ram.RAMGroup(_smbios, _settings));

            if (_gpuEnabled)
            {
                Add(new Gpu.AmdGpuGroup(_settings));
                Add(new Gpu.NvidiaGroup(_settings));
            }

            if (_fanControllerEnabled)
            {
                Add(new Controller.TBalancerGroup(_settings));
                Add(new Controller.HeatmasterGroup(_settings));
                Add(new Controller.AquacomputerGroup(_settings));
            }

            if (_hddEnabled)
                Add(new Hdd.HarddriveGroup(_settings));

            if (_nicEnabled)
            {
                _nicCount = NetworkInterface.GetAllNetworkInterfaces().Length;
                Add(new Nic.NicGroup(_settings));
            }
            _open = true;
        }

        public bool MainboardEnabled
        {
            get { return _mainboardEnabled; }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            set
            {
                if (_open && value != _mainboardEnabled)
                {
                    if (value)
                        Add(new Mainboard.MainboardGroup(_smbios, _settings));
                    else
                        RemoveType<Mainboard.MainboardGroup>();
                }
                _mainboardEnabled = value;
            }
        }

        public bool CPUEnabled
        {
            get { return _cpuEnabled; }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
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

        public bool RAMEnabled
        {
            get { return _ramEnabled; }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            set
            {
                if (_open && value != _ramEnabled)
                {
                    if (value)
                        Add(new Ram.RAMGroup(_smbios, _settings));
                    else
                        RemoveType<Ram.RAMGroup>();
                }
                _ramEnabled = value;
            }
        }

        public bool GPUEnabled
        {
            get { return _gpuEnabled; }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            set
            {
                if (_open && value != _gpuEnabled)
                {
                    if (value)
                    {
                        Add(new Gpu.AmdGpuGroup(_settings));
                        Add(new Gpu.NvidiaGroup(_settings));
                    }
                    else
                    {
                        RemoveType<Gpu.AmdGpuGroup>();
                        RemoveType<Gpu.NvidiaGroup>();
                    }
                }
                _gpuEnabled = value;
            }
        }

        public bool FanControllerEnabled
        {
            get { return _fanControllerEnabled; }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            set
            {
                if (_open && value != _fanControllerEnabled)
                {
                    if (value)
                    {
                        Add(new Controller.TBalancerGroup(_settings));
                        Add(new Controller.HeatmasterGroup(_settings));
                        Add(new Controller.AquacomputerGroup(_settings));
                    }
                    else
                    {
                        RemoveType<Controller.TBalancerGroup>();
                        RemoveType<Controller.HeatmasterGroup>();
                        RemoveType<Controller.AquacomputerGroup>();
                    }
                }
                _fanControllerEnabled = value;
            }
        }

        public bool HDDEnabled
        {
            get { return _hddEnabled; }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            set
            {
                if (_open && value != _hddEnabled)
                {
                    if (value)
                        Add(new Hdd.HarddriveGroup(_settings));
                    else
                        RemoveType<Hdd.HarddriveGroup>();
                }
                _hddEnabled = value;
            }
        }

        public bool NICEnabled
        {
            get { return _nicEnabled; }

            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            set
            {
                if (_open && value != _nicEnabled)
                {
                    if (value)
                        Add(new Nic.NicGroup(_settings));
                    else
                        RemoveType<Nic.NicGroup>();
                }
                _nicEnabled = value;
            }
        }

        public IHardware[] Hardware
        {
            get
            {
                List<IHardware> list = new List<IHardware>();
                foreach (IGroup group in _groups)
                {
                    foreach (IHardware hardware in group.Hardware)
                        list.Add(hardware);
                }
                return list.ToArray();
            }
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
            else
                return c;
        }

        private static void ReportHardwareSensorTree(IHardware hardware, TextWriter w, string space)
        {
            w.WriteLine("{0}|", space);
            w.WriteLine("{0}+- {1} ({2})", space, hardware.Name, hardware.Identifier);
            ISensor[] sensors = hardware.Sensors;
            Array.Sort(sensors, CompareSensor);
            foreach (ISensor sensor in sensors)
            {
                w.WriteLine("{0}|  +- {1,-14} : {2,8:G6} {3,8:G6} {4,8:G6} ({5})", space, sensor.Name, sensor.Value, sensor.Min, sensor.Max, sensor.Identifier);
            }
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

        public string GetReport()
        {
            using (StringWriter w = new StringWriter(CultureInfo.InvariantCulture))
            {
                w.WriteLine();
                w.WriteLine("Open Hardware Monitor Report");
                w.WriteLine();

                Version version = typeof(Computer).Assembly.GetName().Version;

                NewSection(w);
                w.Write("Version: "); w.WriteLine(version.ToString());
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
                        ReportHardwareSensorTree(hardware, w, "");
                }
                w.WriteLine();

                NewSection(w);
                w.WriteLine("Parameters");
                w.WriteLine();
                foreach (IGroup group in _groups)
                {
                    foreach (IHardware hardware in group.Hardware)
                        ReportHardwareParameterTree(hardware, w, "");
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

                    var hardwareArray = group.Hardware;
                    foreach (IHardware hardware in hardwareArray)
                        ReportHardware(hardware, w);
                }
                return w.ToString();
            }
        }

        public void Close()
        {
            if (!_open)
                return;

            while (_groups.Count > 0)
            {
                IGroup group = _groups[_groups.Count - 1];
                Remove(group);
            }

            Opcode.Close();
            Ring0.Close();

            _smbios = null;
            _open = false;
        }

        public void Accept(IVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException("visitor");
            visitor.VisitComputer(this);
        }

        public void Traverse(IVisitor visitor)
        {
            foreach (IGroup group in _groups)
            {
                foreach (IHardware hardware in group.Hardware)
                    hardware.Accept(visitor);
            }
        }

        private class Settings : ISettings
        {
            public bool Contains(string name)
            {
                return false;
            }

            public void SetValue(string name, string value) { }

            public string GetValue(string name, string value)
            {
                return value;
            }

            public void Remove(string name) { }
        }
    }
}
