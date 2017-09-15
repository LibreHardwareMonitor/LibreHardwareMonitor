/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2010-2011 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Globalization;
using System.Text;
using System.Threading;
using HidSharp;

namespace OpenHardwareMonitor.Hardware.WindowsHIDSensor {
  internal class WindowsHIDSensor : Hardware {

    private readonly HidStream stream;
    private readonly Sensor[] temperatures;
        private readonly byte[] readData;







        public WindowsHIDSensor(HidDevice device, ISettings settings)
          : base("Windows HID Sensor", new Identifier("windowshidsensor"), settings)
        {
            
            
           
            if (device.TryOpen(out stream))
            {
                temperatures = new Sensor[2];
                temperatures[0] =
                new Sensor("Temerature 0", 0, SensorType.Temperature, this, settings);
                temperatures[1] =
                new Sensor("Temerature 1", 1, SensorType.Temperature, this, settings);
                readData = new byte[device.MaxInputReportLength];
                try
                {
                    stream.Read(readData, 0, readData.Length);
                }
                catch (TimeoutException)
                {
                }

                temperatures[0].Value = (float)((readData[1] << 8) | (readData[2]))/100;
                ActivateSensor(temperatures[0]);
                temperatures[1].Value = (float)((readData[3] << 8) | (readData[4])) / 100;
                ActivateSensor(temperatures[1]);
            }
            



        }

    public override HardwareType HardwareType {
      get { return HardwareType.WindowsHIDSensor; }
    }

    

    public override void Update() {
            try
            {
                stream.Read(readData, 0, readData.Length);
            }
            catch (TimeoutException)
            {
            }

            temperatures[0].Value = (float)((readData[1] << 8) | (readData[2])) / 100;
            ActivateSensor(temperatures[0]);
            temperatures[1].Value = (float)((readData[3] << 8) | (readData[4])) / 100;
            ActivateSensor(temperatures[1]);

        }

    public override string GetReport() {
      StringBuilder r = new StringBuilder();

      r.AppendLine("WindowsHIDSensor");


      return r.ToString();
    }

    public override void Close() {
            stream.Close();
    }

   
  }
}
