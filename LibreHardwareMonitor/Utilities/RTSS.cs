using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI;
using LibreHardwareMonitor.Utilities;
using RTSSSharedMemoryNET;
using System;
using System.Diagnostics;
using System.Drawing;

namespace LibreHardwareMonitor.Utilities
{
    public class RTSS
    {
        private OSD _osd;
        private readonly string _name = "LHM";
        private readonly string _rtssProcessName = "RTSS";
        private ISensor[] _sensorsAdded;
        private readonly int _updateThreshhold = 1;
        private int _updateCalls = 1;
        private int[] _sensorsPriority;
        private bool _showFPS;
        private Color _sensorColor;
        private Color _valueColor;
        private int _textSize;
        private bool _enabled;
        private UnitManager _unitManager;
        private PersistentSettings _settings;

        public RTSS(PersistentSettings settings, UnitManager unitManager)
        {
            _settings = settings;
            _unitManager = unitManager;
            _sensorColor = Color.Orange;
            _valueColor = Color.White;
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

        public void Add(ISensor sensor)
        {
            try
            {
                int num = CalculateSensorPriority(sensor);
                if (_sensorsAdded != null)
                {
                    Array.Resize<ISensor>(ref _sensorsAdded, (int)_sensorsAdded.Length + 1);
                    Array.Resize<int>(ref _sensorsPriority, (int)_sensorsPriority.Length + 1);
                    _sensorsPriority[_sensorsPriority.GetUpperBound(0)] = _sensorsPriority[_sensorsPriority.GetUpperBound(0) - 1];
                    int num1 = 0;
                    while (num > _sensorsPriority[num1] && num1 < (int)_sensorsAdded.Length - 1)
                    {
                        num1++;
                    }
                    if (num1 != (int)_sensorsAdded.Length - 1)
                    {
                        for (int i = (int)_sensorsAdded.Length - 2; i >= num1; i--)
                        {
                            _sensorsAdded[i + 1] = _sensorsAdded[i];
                            _sensorsPriority[i + 1] = _sensorsPriority[i];
                        }
                        _sensorsAdded[num1] = sensor;
                        _sensorsPriority[num1] = num;
                    }
                    else
                    {
                        _sensorsAdded[_sensorsAdded.GetUpperBound(0)] = sensor;
                        _sensorsPriority[_sensorsPriority.GetUpperBound(0)] = num;
                    }
                }
                else
                {
                    _sensorsAdded = new ISensor[] { sensor };
                    _sensorsPriority = new int[] { num };
                }
                _settings.SetValue((new Identifier(sensor.Identifier, new string[] { "osd" })).ToString(), true);
            }
            catch (Exception exception)
            {
                Console.WriteLine(string.Concat("Could not add sensor. ", exception.Message));
            }
        }

        private int CalculateSensorPriority(ISensor sensor)
        {
            int num1 = 0;
            int num2;
            switch (sensor.Hardware.HardwareType)
            {
                case HardwareType.Motherboard:
                {
                    num2 = 30000;
                    break;
                }
                case HardwareType.SuperIO:
                {
                    num2 = 40000;
                    break;
                }
                case HardwareType.Cpu:
                {
                    num2 = 0;
                    break;
                }
                case HardwareType.Memory:
                {
                    num2 = 50000;
                    break;
                }
                case HardwareType.GpuNvidia:
                {
                    num2 = 20000;
                    break;
                }
                case HardwareType.GpuAmd:
                {
                    num2 = 10000;
                    break;
                }
                case HardwareType.Storage:
                {
                    num2 = 60000;
                    break;
                }
                case HardwareType.Network:
                {
                    num2 = 70000;
                    break;
                }
                default:
                {
                    num2 = num1 + 80000;
                    break;
                }
            }
            int num3;
            switch (sensor.SensorType)
            {
                case SensorType.Voltage:
                {
                    num3 = num2 + 900;
                    break;
                }
                case SensorType.Clock:
                {
                    num3 = num2 + 300;
                    break;
                }
                case SensorType.Temperature:
                {
                    num3 = num2;
                    break;
                }
                case SensorType.Load:
                {
                    num3 = num2 + 500;
                    break;
                }
                case SensorType.Frequency:
                {
                    num3 = num2 + 400;
                    break;
                }
                case SensorType.Fan:
                {
                    num3 = num2 + 200;
                    break;
                }
                case SensorType.Flow:
                {
                    num3 = num2 + 1100;
                    break;
                }
                case SensorType.Control:
                {
                    num3 = num2 + 100;
                    break;
                }
                case SensorType.Level:
                {
                    num3 = num2 + 1200;
                    break;
                }
                case SensorType.Factor:
                {
                    num3 = num2 + 1300;
                    break;
                }
                case SensorType.Power:
                {
                    num3 = num2 + 700;
                    break;
                }
                case SensorType.Data:
                {
                    num3 = num2 + 600;
                    break;
                }
                case SensorType.SmallData:
                {
                    num3 = num2 + 1000;
                    break;
                }
                case SensorType.Throughput:
                {
                    num3 = num2 + 800;
                    break;
                }
                default:
                {
                    num3 = num2 + 1400;
                    break;
                }
            }
            return num3 + sensor.Index;
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
            if (_sensorsAdded == null)
            {
                return false;
            }
            if (Array.IndexOf<ISensor>(_sensorsAdded, sensor) >= 0)
            {
                return true;
            }
            return false;
        }

        public void Disonnect()
        {
            ClearOsd();
            _osd = null;
            _updateCalls = _updateThreshhold;
        }

        private string GetTextToBeDisplayed()
        {
            int j;
            string str = "";
            string str1 = "";
            int length = 0;
            string str2 = "";
            string str3 = "";
            int num = 100 - _textSize * 25;
            Console.WriteLine(num);
            if (_sensorsAdded != null)
            {
                for (int i = 0; i < (int)_sensorsAdded.Length; i++)
                {
                    float value = 0f;
                    if (_sensorsAdded[i].Value.HasValue)
                    {
                        value = (float)_sensorsAdded[i].Value.Value;
                    }
                    switch (_sensorsAdded[i].SensorType)
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
                    str2 = string.Concat(new string[] { str2, "<A0><S0><C250>", _sensorsAdded[i].Name.ToUpper(), ":<C><S><A><A1><S0><C4>", str3, "<C><C4>", str1, "<C><S><A>", Environment.NewLine });
                }
            }
            if (length <= 7)
            {
                length = 8;
            }
            string str4 = ColorTranslator.ToHtml(Color.FromArgb((int)_valueColor.R, (int)_valueColor.G, (int)_valueColor.B)).Substring(1);
            string str5 = ColorTranslator.ToHtml(Color.FromArgb((int)_sensorColor.R, (int)_sensorColor.G, (int)_sensorColor.B)).Substring(1);
            str = string.Concat(new string[] { "<A0=-4><A1=-", length.ToString(), "><A><S0=", num.ToString(), "><C4=", str4, "><C250=", str5, ">", Environment.NewLine });
            Console.WriteLine(str);
            if (_showFPS)
            {
                str = string.Concat(str, "<A0><S0><C250><APP>:<C><S><A><A1><S0><C4><FR> FPS<C><S><A>", Environment.NewLine);
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
                if (_sensorsAdded != null && Array.IndexOf<ISensor>(_sensorsAdded, sensor) >= 0)
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
                    _settings.SetValue((new Identifier(sensor.Identifier, new string[] { "osd" })).ToString(), false);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(string.Concat("Could not remove sensor. ", exception.Message));
            }
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
                    else if (_updateCalls != _updateThreshhold)
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
