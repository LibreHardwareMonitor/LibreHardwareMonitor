// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu.CoolerMaster;

internal sealed class CoolerMasterPsu : Hardware
{
    private const int ReadTimeout = 500;

    private readonly HidDevice _device;
    private readonly List<PsuSensor> _sensors = [];

    public CoolerMasterPsu(HidDevice device, string product, ISettings settings, int index)
        : base("Cooler Master PSU", new Identifier("psu", "coolermaster", index.ToString()), settings)
    {
        _device = device;
        Name = $"Cooler Master {product}";

        using HidStream stream = device.Open();
        stream.ReadTimeout = ReadTimeout;
        FirmwareVersion = $"0x{UsbApi.FirmwareVersion(stream):X2}";

        AddSensors(settings);
    }

    public string FirmwareVersion { get; }

    public override HardwareType HardwareType => HardwareType.Psu;

    public override IDictionary<string, string> Properties => new SortedDictionary<string, string> { { "Firmware version", FirmwareVersion } };

    public override void Update()
    {
        float[] info = null;

        // The PSU can be busy or unplugged: leave the sensors empty instead of throwing.
        if (_device.TryOpen(out HidStream stream))
        {
            using (stream)
            {
                stream.ReadTimeout = ReadTimeout;
                info = UsbApi.InfoList(stream);
            }
        }

        _sensors.ForEach(s => s.Update(info));
    }

    private void AddSensors(ISettings settings)
    {
        int sensorIndex = 0;

        _sensors.Add(new PsuSensor("Case", sensorIndex++, SensorType.Fan, this, settings, UsbApi.IndexInfo.FAN_RPM));
        _sensors.Add(new PsuSensor("Case", sensorIndex++, SensorType.Temperature, this, settings, UsbApi.IndexInfo.TEMP));
        _sensors.Add(new PsuSensor("Total watts", sensorIndex, SensorType.Power, this, settings, UsbApi.IndexInfo.POWER));
    }

    private class PsuSensor : Sensor
    {
        private readonly UsbApi.IndexInfo _indexInfo;

        public PsuSensor(string name, int index, SensorType type, CoolerMasterPsu hardware, ISettings settings, UsbApi.IndexInfo indexInfo)
            : base(name, index, false, type, hardware, null, settings)
        {
            _indexInfo = indexInfo;
            hardware.ActivateSensor(this);
        }

        public void Update(float[] info)
        {
            Value = info?[(int)_indexInfo];
        }
    }
}
