/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2010 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using HidSharp;


namespace OpenHardwareMonitor.Hardware.WindowsHIDSensor
{
  internal class WindowsHIDSensorGroup : IGroup {

    private readonly List<WindowsHIDSensor> hardware = new List<WindowsHIDSensor>();
    private readonly StringBuilder report = new StringBuilder();




        public WindowsHIDSensorGroup(ISettings settings) {
            
            // No implementation for WindowsHIDSensor on Unix systems
            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 128))
                return;

            HidDeviceLoader loader = new HidDeviceLoader();
            var deviceList = loader.GetDevices().ToArray();

            HidDevice selected = loader.GetDevices(1155, 22288).FirstOrDefault();//VID 1155, PID 22288,
            if (selected != null) hardware.Add(new WindowsHIDSensor(selected, settings));
        
        }

        public IHardware[] Hardware {
      get {
        return hardware.ToArray();
      }
    }

    public string GetReport() {
      if (report.Length > 0) {
        StringBuilder r = new StringBuilder();
        r.AppendLine("Windows HID Sensor");
        r.AppendLine();
        r.Append(report);
        r.AppendLine();
        return r.ToString();
      } else
        return null;
    }

    public void Close() {
      foreach (WindowsHIDSensor windowshidsensor in hardware)
                windowshidsensor.Close();
    }
  }
}
