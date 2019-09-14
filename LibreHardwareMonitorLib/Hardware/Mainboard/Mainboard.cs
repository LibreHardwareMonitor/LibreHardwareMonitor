// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Text;
using LibreHardwareMonitor.Hardware.Lpc;

namespace LibreHardwareMonitor.Hardware.Mainboard
{
    public class Mainboard : IHardware
    {
        private readonly SmBios _smbios;
        private readonly string _name;
        private string _customName;
        private readonly ISettings _settings;
        private readonly LPCIO _lpcio;
        private readonly LMSensors _lmSensors;
        private readonly Hardware[] _superIOHardware;

        /// <summary>
        /// Gets the SMBIOS information.
        /// </summary>
        public SmBios SMBIOS => _smbios;

        public Mainboard(SmBios smbios, ISettings settings)
        {
            ISuperIO[] superIO;
            _settings = settings;
            _smbios = smbios;

            Manufacturer manufacturer = smbios.Board == null ? Manufacturer.Unknown : Identification.GetManufacturer(smbios.Board.ManufacturerName);
            Model model = smbios.Board == null ? Model.Unknown : Identification.GetModel(smbios.Board.ProductName);

            if (smbios.Board != null)
            {
                if (!string.IsNullOrEmpty(smbios.Board.ProductName))
                {
                    if (manufacturer == Manufacturer.Unknown)
                        _name = smbios.Board.ProductName;
                    else
                        _name = manufacturer + " " + smbios.Board.ProductName;
                }
                else
                    _name = manufacturer.ToString();
            }
            else
                _name = Manufacturer.Unknown.ToString();

            _customName = settings.GetValue(new Identifier(Identifier, "name").ToString(), _name);

            if (Software.OperatingSystem.IsLinux)
            {
                _lmSensors = new LMSensors();
                superIO = _lmSensors.SuperIO;
            }
            else
            {
                _lpcio = new LPCIO();
                superIO = _lpcio.SuperIO;
            }

            _superIOHardware = new Hardware[superIO.Length];
            for (int i = 0; i < superIO.Length; i++)
                _superIOHardware[i] = new SuperIOHardware(this, superIO[i], manufacturer, model, settings);
        }

        public string Name
        {
            get
            {
                return _customName;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    _customName = value;
                else
                    _customName = _name;
                _settings.SetValue(new Identifier(Identifier, "name").ToString(), _customName);
            }
        }

        public Identifier Identifier
        {
            get { return new Identifier("mainboard"); }
        }

        public HardwareType HardwareType
        {
            get { return HardwareType.Mainboard; }
        }

        public virtual IHardware Parent
        {
            get { return null; }
        }

        public string GetReport()
        {
            StringBuilder r = new StringBuilder();

            r.AppendLine("Mainboard");
            r.AppendLine();
            r.Append(_smbios.GetReport());

            if (_lpcio != null)
                r.Append(_lpcio.GetReport());

            byte[] table = FirmwareTable.GetTable(Interop.Kernel32.Provider.ACPI, "TAMG");
            if (table != null)
            {
                Gigabyte tamg = new Gigabyte(table);
                r.Append(tamg.GetReport());
            }
            return r.ToString();
        }

        public void Update() { }

        public void Close()
        {
            if (_lmSensors != null)
                _lmSensors.Close();
            foreach (Hardware hardware in _superIOHardware)
                hardware.Close();
        }

        public IHardware[] SubHardware
        {
            get { return _superIOHardware; }
        }

        public ISensor[] Sensors
        {
            get { return new ISensor[0]; }
        }

#pragma warning disable 67
        public event SensorEventHandler SensorAdded;
        public event SensorEventHandler SensorRemoved;
#pragma warning restore 67

        public void Accept(IVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException("visitor");
            visitor.VisitHardware(this);
        }

        public void Traverse(IVisitor visitor)
        {
            foreach (IHardware hardware in _superIOHardware)
                hardware.Accept(visitor);
        }
    }
}
