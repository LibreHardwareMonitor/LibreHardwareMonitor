// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Text;
using LibreHardwareMonitor.Hardware.Motherboard.Lpc;

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

            if (Software.OperatingSystem.IsUnix)
            {
                _lmSensors = new LMSensors();
                superIO = _lmSensors.SuperIO;
            }
            else
            {
                _lpcIO = new LpcIO();
                superIO = _lpcIO.SuperIO;
            }

            SubHardware = new IHardware[superIO.Count];
            for (int i = 0; i < superIO.Count; i++)
                SubHardware[i] = new SuperIOHardware(this, superIO[i], manufacturer, model, settings);
        }

        public HardwareType HardwareType
        {
            get { return HardwareType.Motherboard; }
        }

        public Identifier Identifier
        {
            get { return new Identifier("motherboard"); }
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
            StringBuilder r = new StringBuilder();

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

        public event SensorEventHandler SensorAdded;

        public event SensorEventHandler SensorRemoved;

        public void Close()
        {
            _lmSensors?.Close();
            foreach (IHardware iHardware in SubHardware)
            {
                if (iHardware is Hardware hardware)
                    hardware.Close();
            }
        }
    }
}
