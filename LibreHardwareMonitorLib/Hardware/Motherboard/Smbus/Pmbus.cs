// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Motherboard.Smbus
{
    internal sealed class Pmbus : Hardware
    {
        private readonly byte _address;
        private readonly List<Sensor> _pmbusDevice = new();
        private readonly Sensor _voltagein;
        private readonly Sensor _currentin;
        private readonly Sensor _voltageout;
        private readonly Sensor _currentout;
        private readonly Sensor _temperature1;
        private readonly Sensor _temperature2;
        private readonly Sensor _wattsout;
        private readonly Sensor _wattsin;

        public Pmbus(string name, ISettings settings, byte address) : base(name, new Identifier("Smbus"), settings)
        {
            _address = address;
            // Pmbus devices can support many attributes but the most basic are here for now
            // these are in a specific order. make sure to change the regs array below to match if changed
            _voltagein = new Sensor("In", 2, SensorType.Voltage, this, settings);
            _currentin = new Sensor("In", 4, SensorType.Current, this, settings);
            _voltageout = new Sensor("Out", 3, SensorType.Voltage, this, settings);
            _currentout = new Sensor("Out", 5, SensorType.Current, this, settings);
            _temperature1 = new Sensor("Loop 1 ", 0, SensorType.Temperature, this, settings);
            _temperature2 = new Sensor("Loop 2 ", 1, SensorType.Temperature, this, settings);
            _wattsout = new Sensor("Out", 7, SensorType.Power, this, settings);
            _wattsin = new Sensor("In", 6, SensorType.Power, this, settings);
            
            _pmbusDevice.Add(_voltagein);
            _pmbusDevice.Add(_currentin);
            _pmbusDevice.Add(_voltageout);
            _pmbusDevice.Add(_currentout);
            _pmbusDevice.Add(_temperature1);
            _pmbusDevice.Add(_temperature2);
            _pmbusDevice.Add(_wattsout);
            _pmbusDevice.Add(_wattsin);
            
            ActivateSensor(_voltagein);
            ActivateSensor(_currentin);
            ActivateSensor(_voltageout);
            ActivateSensor(_currentout);
            ActivateSensor(_temperature1);
            ActivateSensor(_temperature2);
            ActivateSensor(_wattsout);
            ActivateSensor(_wattsin); 
        }

        public static string Detect(byte address)
        {
            string deviceName = "";
            List<byte> mfrmodel = new();
            List<byte> mfrrevision = new();
            // this is hoping no other smbus devices respond to a block read on these commands
            if (SmbusIO.GetBlock(address, MFR_MODEL, mfrmodel) != 0 & SmbusIO.GetBlock(address, MFR_REVISION, mfrrevision) != 0)
                return "";
            // pmbus devices "should"? also state their pmbus revision
            byte pmbusrev = (byte)SmbusIO.GetWord(address, PMBUS_REVISION);
            if (pmbusrev < 0x10 | pmbusrev > 0x14)
                return "";
                
            // only way I can find to uniquely identify this device but this is without documentation
            if (mfrmodel[0] == 0x25 && mfrrevision[0] == 0x04)
                deviceName = "IR3564 VRM";

            if (deviceName == "")
                deviceName = "PMBus: " + BitConverter.ToString(mfrmodel.ToArray()) + " " + BitConverter.ToString(mfrrevision.ToArray());

            return deviceName;
        }

        static float Linear11ToFloat32(ushort val)
        {
            int exp = ((short)val) >> 11;
            int mant = (((short)(val & 0x7ff)) << 5) >> 5;
            return mant * (float)Math.Pow(2, exp);
        }

        static float Linear16ToFloat32(ushort val, byte command20)
        {
            // -11 seems to be the value for my IR3564 but i can't work out how I'm supposed
            // to get this from VOUT_MODE command. one side of the chip gives 82h the other 84h
            // I can't find any pmbus documentation that mentions the meaning of the first bit (80h)
            // and theres no way to derive -11 from 2h or 4h
            int exp = -11;

            return (float) val / 2048;// * Math.Pow(2, exp);// some documentation talks about dividing by 1024 2048 and 4096.
                                      // 2048 gets pretty close here But doesn't explain why there's a different VOUT_MODE value in both sides of the vrm
        }

        public override void Update()
        {
            byte[] commands = {
                    READ_VIN,
                    READ_IIN,
                    READ_VOUT,
                    READ_IOUT,
                    READ_TEMPERATURE_1,
                    READ_TEMPERATURE_2,
                    READ_POUT,
                    READ_PIN
             };

            if (!Ring0.WaitSmBusMutex(10))
                return;
            
            for (int i = 0; i < _pmbusDevice.Count; i++)
            {
                // keep these in the same order to simplify things
                switch (i)
                {
                    case 4: // temperature is a direct reading
                    case 5:
                    _pmbusDevice[i].Value = SmbusIO.GetWord(_address, commands[i]);
                    break;

                    case 2: // Vout is linear16? I think we're supposed to read VOUT_MODE for the other part to this
                    byte command20 = (byte)(SmbusIO.GetWord(_address, VOUT_MODE) >> 8 );
                    _pmbusDevice[i].Value = Linear16ToFloat32(SmbusIO.GetWord(_address, commands[i]), command20);
                    break;

                    default: // most are linear11
                    _pmbusDevice[i].Value = Linear11ToFloat32(SmbusIO.GetWord(_address, commands[i]));
                    break;
                }
            }
            Ring0.ReleaseSmBusMutex();
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.EmbeddedController; }// for the icon
        }

        // basic set of registers to get started
        private static byte VOUT_MODE = 0x20;
        private static byte READ_VIN = 0x88;
        private static byte READ_IIN = 0x89;
        private static byte READ_VOUT = 0x8B;
        private static byte READ_IOUT = 0x8C;
        private static byte READ_TEMPERATURE_1 = 0x8D;
        private static byte READ_TEMPERATURE_2 = 0x8E;
        private static byte READ_DUTY_CYCLE = 0x94;
        private static byte READ_POUT = 0x96;
        private static byte READ_PIN = 0x97;
        private static byte PMBUS_REVISION = 0x98;
        private static byte MFR_ID = 0x99;
        private static byte MFR_MODEL = 0x9A;
        private static byte MFR_REVISION = 0x9B;
    }
}
