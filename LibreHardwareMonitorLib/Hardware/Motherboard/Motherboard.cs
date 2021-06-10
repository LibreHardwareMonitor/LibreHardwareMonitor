// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Text;
using LibreHardwareMonitor.Hardware.Motherboard.Lpc;
using LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC;
using OperatingSystem = LibreHardwareMonitor.Software.OperatingSystem;

namespace LibreHardwareMonitor.Hardware.Motherboard
{
    public class Motherboard : IHardware
    {
        private readonly LMSensors _lmSensors;
        private readonly LpcIO _lpcIO;
        private readonly string _name;
        private readonly ISettings _settings;
        private string _customName;

        public Motherboard(SMBios smBios, ISettings settings)
        {
            IReadOnlyList<ISuperIO> superIO;
            _settings = settings;
            SMBios = smBios;

            Manufacturer manufacturer = smBios.Board == null ? Manufacturer.Unknown : Identification.GetManufacturer(smBios.Board.ManufacturerName);
            Model model = smBios.Board == null ? Model.Unknown : Identification.GetModel(smBios.Board.ProductName);

            if (smBios.Board != null)
            {
                if (!string.IsNullOrEmpty(smBios.Board.ProductName))
                {
                    if (manufacturer == Manufacturer.Unknown)
                        _name = smBios.Board.ProductName;
                    else
                        _name = manufacturer + " " + smBios.Board.ProductName;
                }
                else
                {
                    _name = manufacturer.ToString();
                }
            }
            else
            {
                _name = Manufacturer.Unknown.ToString();
            }

            _customName = settings.GetValue(new Identifier(Identifier, "name").ToString(), _name);

            if (OperatingSystem.IsUnix)
            {
                _lmSensors = new LMSensors();
                superIO = _lmSensors.SuperIO;
            }
            else
            {
                _lpcIO = new LpcIO();
                superIO = _lpcIO.SuperIO;
            }

            EmbeddedController embeddedController = CreateEmbeddedController(model, settings);

            SubHardware = new IHardware[superIO.Count + (embeddedController != null ? 1 : 0)];
            for (int i = 0; i < superIO.Count; i++)
                SubHardware[i] = new SuperIOHardware(this, superIO[i], manufacturer, model, settings);

            if (embeddedController != null)
            {
                SubHardware[superIO.Count] = embeddedController;
            }
        }

        public event SensorEventHandler SensorAdded;

        public event SensorEventHandler SensorRemoved;

        public HardwareType HardwareType
        {
            get { return HardwareType.Motherboard; }
        }

        public Identifier Identifier
        {
            get { return new("motherboard"); }
        }

        public string Name
        {
            get { return _customName; }
            set
            {
                _customName = !string.IsNullOrEmpty(value) ? value : _name;

                _settings.SetValue(new Identifier(Identifier, "name").ToString(), _customName);
            }
        }

        public virtual IHardware Parent
        {
            get { return null; }
        }

        public ISensor[] Sensors
        {
            get { return new ISensor[0]; }
        }

        /// <summary>
        /// Gets the SMBios information.
        /// </summary>
        public SMBios SMBios { get; }

        public IHardware[] SubHardware { get; }

        public string GetReport()
        {
            StringBuilder r = new();

            r.AppendLine("Motherboard");
            r.AppendLine();
            r.Append(SMBios.GetReport());

            if (_lpcIO != null)
                r.Append(_lpcIO.GetReport());

            return r.ToString();
        }

        public void Update()
        { }

        public void Accept(IVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException(nameof(visitor));


            visitor.VisitHardware(this);
        }

        public void Traverse(IVisitor visitor)
        {
            foreach (IHardware hardware in SubHardware)
                hardware.Accept(visitor);
        }

        public void Close()
        {
            _lmSensors?.Close();
            foreach (IHardware iHardware in SubHardware)
            {
                if (iHardware is Hardware hardware)
                    hardware.Close();
            }
        }

        private static EmbeddedController CreateEmbeddedController(Model model, ISettings settings)
        {
            var sources = new List<EmbeddedControllerSource>();

            switch (model)
            {
                case Model.ROG_STRIX_X570_E_GAMING:
                case Model.ROG_CROSSHAIR_VIII_HERO:
                {
                    sources.AddRange(new EmbeddedControllerSource[]
                    {
                        new("Chipset", 0x3A, SensorType.Temperature, EmbeddedController.ReadByte),
                        new("CPU", 0x3B, SensorType.Temperature, EmbeddedController.ReadByte),
                        new("Motherboard", 0x3C, SensorType.Temperature, EmbeddedController.ReadByte),
                        new("T Sensor", 0x3D, SensorType.Temperature, EmbeddedController.ReadByte),
                        new("VRM", 0x3E, SensorType.Temperature, EmbeddedController.ReadByte),
                        new("CPU Opt", 0xB0, SensorType.Fan, EmbeddedController.ReadWordBE),
                        new("Chipset", 0xB4, SensorType.Fan, EmbeddedController.ReadWordBE),
                        new("CPU", 0xF4, SensorType.Current, EmbeddedController.ReadByte)
                    });
                    break;
                }
            }

            switch (model)
            {
                case Model.ROG_CROSSHAIR_VIII_HERO:
                {
                    // TODO: "why 42?" is a silly question, I know, but still, why? On the serious side, it might be 41.6(6)
                    sources.Add(new("Flow Rate", 0xBC, SensorType.Flow, (ecIO, port) => ecIO.ReadWordBE(port) / 42f * 60f));
                    break;
                }
            }

            return sources.Count > 0 ? EmbeddedController.Create(sources, settings) : null;
        }
    }
}
