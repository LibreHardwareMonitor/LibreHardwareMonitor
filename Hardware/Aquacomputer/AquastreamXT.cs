using System;
using System.Collections.Generic;
using System.Text;
using HidLibrary;

namespace OpenHardwareMonitor.Hardware.Aquacomputer
{
    /*
     * TODO:
     * Check tested and fix unknown variables in Update()
     * Check if property "Variant" is valid interpreted
     * Implement Fan Control in SetControl()
     */
    internal class AquastreamXT : Hardware
    {
        [Flags]
        enum PumpAlarms : byte
        {
            ALARM_SENSOR1 = 1,
            ALARM_SENSOR2 = 2,
            ALARM_PUMP = 4,
            ALARM_FAN = 8,
            ALARM_FLOW = 16,
            ALARM_FAN_SHORT = 32,
            ALARM_FAN_TEMP90 = 64,
            ALARM_FAN_TEMP70 = 128
        }

        [Flags]
        enum PumpMode : byte
        {
            MODE_PUMP_ADV = 1,
            MODE_FAN_AMP = 2,
            MODE_FAN_CONTROLLER = 4
        }

        #region USB
        private HidDevice device;
        private byte[] rawData;
        public UInt16 FirmwareVersion { get; private set; }
        #endregion

        private readonly Sensor _fanControl, _pumpPower, _pumpFlow;
        private readonly Sensor[] _rpmSensors = new Sensor[2];
        private readonly Sensor[] _temperatures = new Sensor[3];
        private readonly Sensor[] _voltages = new Sensor[2];
        private readonly Sensor[] _frequencies = new Sensor[2];
               
        public AquastreamXT(HidDevice dev, ISettings settings) : base("Aquastream XT", new Identifier(dev.DevicePath), settings)
        {
            device = dev;

            do
            {
                device.ReadFeatureData(out rawData, 0x4);
            } while (rawData[0] != 0x4);

            Name = $"Aquastream XT {Variant}";
            FirmwareVersion = BitConverter.ToUInt16(rawData, 50);

            _temperatures[0] = new Sensor("External Fan VRM", 0, SensorType.Temperature, this, new ParameterDescription[0], settings);
            ActivateSensor(_temperatures[0]);
            _temperatures[1] = new Sensor("External", 1, SensorType.Temperature, this, new ParameterDescription[0], settings);
            ActivateSensor(_temperatures[1]);
            _temperatures[2] = new Sensor("Internal Water", 2, SensorType.Temperature, this, new ParameterDescription[0], settings);
            ActivateSensor(_temperatures[2]);

            _voltages[0] = new Sensor("External Fan", 1, SensorType.Voltage, this, new ParameterDescription[0], settings);
            ActivateSensor(_voltages[0]);
            _voltages[1] = new Sensor("Pump", 2, SensorType.Voltage, this, new ParameterDescription[0], settings);
            ActivateSensor(_voltages[1]);

            _pumpPower = new Sensor("Pump", 0, SensorType.Power, this, new ParameterDescription[0], settings);
            ActivateSensor(_pumpPower);

            _pumpFlow = new Sensor("Pump", 0, SensorType.Flow, this, new ParameterDescription[0], settings);
            ActivateSensor(_pumpFlow);

            _rpmSensors[0] = new Sensor("External Fan", 0, SensorType.Fan, this, new ParameterDescription[0], settings);
            ActivateSensor(_rpmSensors[0]);
            _rpmSensors[1] = new Sensor("Pump", 1, SensorType.Fan, this, new ParameterDescription[0], settings);
            ActivateSensor(_rpmSensors[1]);

            _fanControl = new Sensor("External Fan", 0, SensorType.Control, this, new ParameterDescription[0], settings);
            Control control = new Control(_fanControl, settings, 0, 100);
            control.ControlModeChanged += (cc) => {
                switch (cc.ControlMode)
                {
                    case ControlMode.Undefined:
                        return;
                    case ControlMode.Default:
                        SetControl(null);
                        break;
                    case ControlMode.Software:
                        SetControl((byte)(cc.SoftwareValue * 2.55));
                        break;
                    default:
                        return;
                }
            };
            control.SoftwareControlValueChanged += (cc) => {
                if (cc.ControlMode == ControlMode.Software)
                    SetControl((byte)(cc.SoftwareValue * 2.55));
            };

            switch (control.ControlMode)
            {
                case ControlMode.Undefined:
                    break;
                case ControlMode.Default:
                    SetControl(null);
                    break;
                case ControlMode.Software:
                    SetControl((byte)(control.SoftwareValue * 2.55));
                    break;
                default:
                    break;
            }
            _fanControl.Control = control;
            ActivateSensor(_fanControl);

            _frequencies[0] = new Sensor("Pump Frequency", 0, SensorType.Frequency, this, new ParameterDescription[0], settings);
            ActivateSensor(_frequencies[0]);
            _frequencies[1] = new Sensor("Pump MaxFrequency", 1, SensorType.Frequency, this, new ParameterDescription[0], settings);
            ActivateSensor(_frequencies[1]);            
        }

        //TODO: Implement Fan Control
        private void SetControl(byte? v)
        {
            throw new NotImplementedException();
        }

        //TODO: Check if valid
        public string Variant
        {
            get
            {
                PumpMode mode = (PumpMode)rawData[33];

                if (mode.HasFlag(PumpMode.MODE_PUMP_ADV))
                    return "Ultra + Internal Flow Sensor";
                else if (mode.HasFlag(PumpMode.MODE_FAN_CONTROLLER))
                    return "Ultra";
                else if (mode.HasFlag(PumpMode.MODE_FAN_AMP))
                    return "Advanced";
                else
                    return "Standard";
            }
        }

        public override HardwareType HardwareType
        {
            get
            {
                return HardwareType.Aquacomputer;
            }
        }

        public string Status {
            get {
                FirmwareVersion = BitConverter.ToUInt16(rawData, 50);

                if (FirmwareVersion < 1008)
                {
                    return $"Status: Untested Firmware Version {FirmwareVersion}! Please consider Updating to Version 1018";
                }
                return "Status: OK";
            }
        }

        public override void Close()
        {
            device.CloseDevice();
            base.Close();
        }

        //TODO: Check tested and fix unknown variables
        public override void Update()
        {
            device.ReadFeatureData(out rawData, 0x4);

            if (rawData[0] != 0x4)
                return;

            //var rawSensorsFan = BitConverter.ToUInt16(rawData, 1);                        //unknown - redundant?
            //var rawSensorsExt = BitConverter.ToUInt16(rawData, 3);                        //unknown - redundant?
            //var rawSensorsWater = BitConverter.ToUInt16(rawData, 5);                      //unknown - redundant?

            _voltages[0].Value = BitConverter.ToUInt16(rawData, 7) / 61f;                    //External Fan Voltage: tested - OK
            _voltages[1].Value = BitConverter.ToUInt16(rawData, 9) / 61f;                    //Pump Voltage: tested - OK
            _pumpPower.Value = _voltages[1].Value * BitConverter.ToInt16(rawData, 11) / 625f; //Pump Voltage * Pump Current: tested - OK

            _temperatures[0].Value = BitConverter.ToUInt16(rawData, 13) / 100f;              //External Fan VRM Temperature: untested
            _temperatures[1].Value = BitConverter.ToUInt16(rawData, 15) / 100f;              //External Temperature Sensor: untested
            _temperatures[2].Value = BitConverter.ToUInt16(rawData, 17) / 100f;              //Internal Water Temperature Sensor: tested - OK

            _frequencies[0].Value = (1f / BitConverter.ToInt16(rawData, 19)) * 750000;        //Pump Frequency: tested - OK
            _rpmSensors[1].Value = _frequencies[0].Value * 60f;                                      //Pump RPM: tested - OK
            _frequencies[1].Value = (1f / BitConverter.ToUInt16(rawData, 21)) * 750000;    //Pump Max Frequency: tested - OK

            _pumpFlow.Value = BitConverter.ToUInt32(rawData, 23);                            //Internal Pump Flow Sensor: unknown

            _rpmSensors[0].Value = BitConverter.ToUInt32(rawData, 27);                              //External Fan RPM: untested

            _fanControl.Value = 100f / byte.MaxValue * rawData[31];                          //External Fan Control: tested, External Fan Voltage scales by this value - OK
        }
    }
}
