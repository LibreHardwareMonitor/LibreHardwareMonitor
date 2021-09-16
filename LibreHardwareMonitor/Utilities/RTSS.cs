﻿using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI;
using RTSSSharedMemoryNET;
using System;
using System.Diagnostics;
using System.Drawing;

namespace LibreHardwareMonitor.Utilities
{
    public class RTSS
    {
        private OSD _osd;
        private readonly string _name = "LHM"; // Instance name that identifies LHM inside RTSS app
        private readonly string _rtssProcessName = "RTSS"; // String to identify process name and check if RTSS is running
        private readonly int _updateCallsThreshhold = 0; // Max amount of update calls to actually update OSD. 0 is instant
        private int _updateCalls = 0; // Variable to keep track of update calls
        private bool _showFPS;
        private Color _sensorColor;
        private Color _valueColor;
        private int _textSize;
        private bool _enabled;
        private AddedSensors[] _addedSensors;

        private struct AddedSensors
        {
            public ISensor _sensor; // Sensor that will be displayed through RTSS
            public int _priority; // Priority value that each sensor has. Lower number means higher priority, so upper in the list of displayed sensors.
            public Color _sensorColor; // Color of the sensor name
            public Color _valueColor; // Color of the sensor value

            public AddedSensors(ISensor sensor, int priority, Color sensorColor, Color valueColor)
            {
                _sensor = sensor;
                _priority = priority;
                _sensorColor = sensorColor;
                _valueColor = valueColor;
            }
        }

        public bool enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled)
                {
                    Disonnect();
                }
                _enabled = value;
            }
        }

        public Color sensorColor
        {
            get
            {
                return _sensorColor;
            }
            set
            {
                _sensorColor = value;
            }
        }

        public Color valueColor
        {
            get
            {
                return _valueColor;
            }
            set
            {
                _valueColor = value;
            }
        }

        public bool showFPS
        {
            get
            {
                return _showFPS;
            }
            set
            {
                _showFPS = value;
            }
        }

        public int textSize
        {
            get
            {
                return _textSize;
            }
            set
            {
                _textSize = value;
            }
        }

        public void Add(ISensor sensor, int priority, Color sensorColor, Color valueColor)
        {
            try
            {
                AddedSensors sensorToAdd = new AddedSensors(sensor, priority, sensorColor, valueColor);
                if (_addedSensors != null)
                {
                    Array.Resize<AddedSensors>(ref _addedSensors, (int)_addedSensors.Length + 1);
                    _addedSensors[_addedSensors.GetUpperBound(0)] = sensorToAdd;
                }
                else
                {
                    _addedSensors = new AddedSensors[] { sensorToAdd };
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(string.Concat("Could not add sensor. ", exception.Message));
            }
        }

        private void ClearOsd()
        {
            try
            {
                if (_osd != null && IsRtssRunning())
                {
                    _osd.Update("");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(string.Concat("Could not clear OSD. ", exception.Message));
            }
        }

        private bool Connect()
        {
            bool flag;
            try
            {
                if (!enabled)
                {
                    Disonnect();
                    flag = false;
                }
                else if (_osd != null)
                {
                    if (!IsRtssRunning())
                    {
                        Disonnect();
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                }
                else if (!IsRtssRunning())
                {
                    Disonnect();
                    flag = false;
                }
                else
                {
                    _osd = new OSD(_name);
                    _updateCalls = -3;
                    flag = false;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(string.Concat("Could not initialize RTSS OSD object", exception.Message));
                Disonnect();
                flag = false;
            }
            return flag;
        }

        public bool Contains(ISensor sensor)
        {
            if (_addedSensors == null)
            {
                return false;
            }
            if (Array.FindIndex(_addedSensors, s => s._sensor == sensor) >= 0)
            {

            }
            return false;
        }

        public void Disonnect()
        {
            ClearOsd();
            _osd = null;
            _updateCalls = _updateCallsThreshhold;
        }

        private string GetTextToBeDisplayed()
        {
            string str = "";
            string str1 = "";
            int length = 0;
            string str2 = "";
            string str3 = "";
            string sColor = "";
            string vColor = "";
            int num = 100 - _textSize * 25;
            Console.WriteLine(num);
            if (_addedSensors != null)
            {
                for (int i = 0; i < (int)_addedSensors.Length; i++)
                {
                    /*_addedSensors[i]._sensorColor = sensorColor;
                    _addedSensors[i]._valueColor = valueColor;*/
                    float value = 0f;
                    if (_addedSensors[i]._sensor.Value.HasValue)
                    {
                        value = (float)_addedSensors[i]._sensor.Value.Value;
                    }
                    switch (_addedSensors[i]._sensor.SensorType)
                    {
                        case SensorType.Voltage:
                        {
                            str1 = " V";
                            str3 = value.ToString("#,###,##0.000");
                            break;
                        }
                        case SensorType.Clock:
                        {
                            str1 = " MHz";
                            str3 = value.ToString("#,###,##0.0");
                            break;
                        }
                        case SensorType.Temperature:
                        {
                            str1 = " °C";
                            str3 = value.ToString("#,###,##0");
                            break;
                        }
                        case SensorType.Load:
                        {
                            str1 = " %";
                            str3 = value.ToString("#,###,##0.0");
                            break;
                        }
                        case SensorType.Frequency:
                        {
                            str1 = " Hz";
                            str3 = value.ToString("#,###,##0.0");
                            break;
                        }
                        case SensorType.Fan:
                        {
                            str1 = " RPM";
                            str3 = value.ToString("#,###,##0");
                            break;
                        }
                        case SensorType.Flow:
                        {
                            str1 = " L/h";
                            str3 = value.ToString("#,###,##0");
                            break;
                        }
                        case SensorType.Control:
                        {
                            str1 = " %";
                            str3 = value.ToString("#,###,##0.0");
                            break;
                        }
                        case SensorType.Level:
                        {
                            str1 = " %";
                            str3 = value.ToString("#,###,##0");
                            break;
                        }
                        case SensorType.Factor:
                        {
                            str1 = " 1";
                            str3 = value.ToString("#,###,##0");
                            break;
                        }
                        case SensorType.Power:
                        {
                            str1 = " W";
                            str3 = value.ToString("#,###,##0.0");
                            break;
                        }
                        case SensorType.Data:
                        {
                            str1 = " GB";
                            str3 = value.ToString("#,###,##0.0");
                            break;
                        }
                        case SensorType.SmallData:
                        {
                            str1 = " MB";
                            str3 = value.ToString("#,###,##0");
                            break;
                        }
                        case SensorType.Throughput:
                        {
                            int j = 0;
                            string[] strArrays = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB" };
                            for (j = 0; value > 1024f && j < (int)strArrays.Length - 1; j++)
                            {
                                value /= 1024f;
                            }
                            str1 = string.Concat(" ", strArrays[j], "/s");
                            str3 = value.ToString("#,###,##0.0");
                            break;
                        }
                        default:
                        {
                            str1 = "";
                            str3 = value.ToString("#,###,##0.00");
                            break;
                        }
                    }
                    if (length <= str3.Length + str1.Length + 1)
                    {
                        length = str3.Length + str1.Length + 1;
                    }
                    sColor = ColorTranslator.ToHtml(Color.FromArgb((int)_addedSensors[i]._sensorColor.R, (int)_addedSensors[i]._sensorColor.G, (int)_addedSensors[i]._sensorColor.B)).Substring(1);
                    vColor = ColorTranslator.ToHtml(Color.FromArgb((int)_addedSensors[i]._valueColor.R, (int)_addedSensors[i]._valueColor.G, (int)_addedSensors[i]._valueColor.B)).Substring(1);
                    str2 = string.Concat(new string[] { str2, "<A0><S0><C=", sColor, ">", _addedSensors[i]._sensor.Name.ToUpper(), ":<C><S><A><A1><S0><C=", vColor, ">", str3, "<C><C=", vColor,">", str1, "<C><S><A>", Environment.NewLine });
                }
                
            }
            if (length <= 7)
            {
                length = 8;
            }
            sColor = ColorTranslator.ToHtml(Color.FromArgb((int)_sensorColor.R, (int)_sensorColor.G, (int)_sensorColor.B)).Substring(1);
            vColor = ColorTranslator.ToHtml(Color.FromArgb((int)_valueColor.R, (int)_valueColor.G, (int)_valueColor.B)).Substring(1);

            //str = string.Concat(new string[] { "<A0=-4><A1=-", length.ToString(), "><A><S0=", num.ToString(), "><C4=", sColor, "><C250=", vColor, ">", Environment.NewLine });
            str = string.Concat(new string[] { "<A0=-4><A1=-", length.ToString(), "><A><S0=", num.ToString(), ">", Environment.NewLine });
            //Console.WriteLine(str);
            if (_showFPS)
            {
                //str = string.Concat(str, "<A0><S0><C250><APP>:<C><S><A><A1><S0><C4><FR> FPS<C><S><A>", Environment.NewLine);
                str = string.Concat(str, "<A0><S0><C=", sColor,"><APP>:<C><S><A><A1><S0><C=", vColor,"><FR> FPS<C><S><A>", Environment.NewLine);
            }
            str = string.Concat(str, str2);
            if (str.Length > 0xfff)
            {
                str = str.Substring(0, 0xfff);
            }
            return str;
        }

        public bool IsRtssRunning()
        {
            bool flag;
            string str;
            try
            {
                flag = (Process.GetProcessesByName(_rtssProcessName).Length == 0 ? false : true);
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                if (exception != null)
                {
                    str = exception.ToString();
                }
                else
                {
                    str = null;
                }
                Console.WriteLine(string.Concat("Error checking RTSS process. ", str));
                flag = false;
            }
            return flag;
        }

        public void Remove(ISensor sensor)
        {
            try
            {
                if(_addedSensors != null && Array.FindIndex(_addedSensors, s => s._sensor == sensor) >= 0)
                {
                    for (int i = Array.FindIndex(_addedSensors, s => s._sensor == sensor); i < (int)_addedSensors.Length - 1; i++)
                    {
                        _addedSensors[i] = _addedSensors[i + 1];
                    }
                    if ((int)_addedSensors.Length - 1 != 0)
                    {
                        Array.Resize<AddedSensors>(ref _addedSensors, (int)_addedSensors.Length - 1);
                    }
                    else
                    {
                        _addedSensors = null;
                    }
                }
                /*if (_sensorsAdded != null && Array.IndexOf<ISensor>(_sensorsAdded, sensor) >= 0)
                {
                    for (int i = Array.IndexOf<ISensor>(_sensorsAdded, sensor); i < (int)_sensorsAdded.Length - 1; i++)
                    {
                        _sensorsAdded[i] = _sensorsAdded[i + 1];
                        _sensorsPriority[i] = _sensorsPriority[i + 1];
                    }
                    if ((int)_sensorsAdded.Length - 1 != 0)
                    {
                        Array.Resize<ISensor>(ref _sensorsAdded, (int)_sensorsAdded.Length - 1);
                        Array.Resize<int>(ref _sensorsPriority, (int)_sensorsPriority.Length - 1);
                    }
                    else
                    {
                        _sensorsAdded = null;
                        _sensorsPriority = null;
                    }
                }*/
            }
            catch (Exception exception)
            {
                Console.WriteLine(string.Concat("Could not remove sensor. ", exception.Message));
            }
        }

        public void RemoveAllSensors()
        {
            /*_sensorsAdded = null;
            _sensorsPriority = null;*/
            _addedSensors = null;
        }
        public void Update()
        {
            try
            {
                if (enabled)
                {
                    if (!IsRtssRunning())
                    {
                        Disonnect();
                    }
                    else if (_updateCalls != _updateCallsThreshhold)
                    {
                        _updateCalls++;
                    }
                    else
                    {
                        if (_osd != null)
                        {
                            _osd.Update(GetTextToBeDisplayed());
                        }
                        else if (Connect())
                        {
                            _osd.Update(GetTextToBeDisplayed());
                        }
                        _updateCalls = 0;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(string.Concat("Could not update. ", exception.Message));
                Disonnect();
            }
        }
    }
}
