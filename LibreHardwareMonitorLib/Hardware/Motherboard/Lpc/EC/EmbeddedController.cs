using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc.EC
{
    public abstract class EmbeddedController : Hardware
    {
        public delegate float ECReader(IEmbeddedControllerIO ecIO, byte port);
        public class Source
        {
            public Source(string name, byte port, SensorType type, ECReader reader)
            {
                Name = name;
                Port = port;
                Type = type;
                Reader = reader;
            }
            public string Name { get; private set; }
            public byte Port { get; private set; }
            public SensorType Type { get; private set; }
            public ECReader Reader { get; private set; }
        }

        class ECSensor : Sensor
        {
            public ECSensor(Source source, int index, EmbeddedController hardware, ISettings settings)
                : base(source.Name, index, source.Type, hardware, settings)
            {
                _port = source.Port;
                _reader = source.Reader;
            }

            public void Update(IEmbeddedControllerIO ecIO)
            {
                Value = _reader(ecIO, _port);
            }

            readonly byte _port;
            readonly ECReader _reader;
        }

        public static EmbeddedController Create(List<Source> sources, ISettings settings)
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => new WindowsEmbeddedController(sources, settings),
                _ => null
            };
        }

        protected EmbeddedController(List<Source> sources, ISettings settings)
            : base("Embedded Controller", new Identifier("lpc", "ec"), settings)
        {
            var indices = new Dictionary<SensorType, int>();
            foreach (SensorType t in Enum.GetValues(typeof(SensorType)))
            {
                indices.Add(t, 0);
            }

            _sensors = new List<ECSensor>();
            foreach (Source s in sources)
            {
                int index = indices[s.Type];
                indices[s.Type] = index + 1;
                _sensors.Add(new ECSensor(s, index, this, settings));
                ActivateSensor(_sensors[_sensors.Count - 1]);
            }
        }

        public override void Update()
        {
            try
            {
                using (var ecIO = accquireIOInterface())
                {
                    foreach (ECSensor sensor in _sensors)
                    {
                        sensor.Update(ecIO);
                    }
                }
            }
            catch (WindowsEmbeddedControllerIO.BusMutexLockingFailedException)
            {
                // just skip this update cycle?
            }
        }

        public override HardwareType HardwareType => HardwareType.EmbeddedController;

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();

            r.AppendLine("EC " + GetType().Name);
            r.AppendLine("Embedded Controller Registers");
            r.AppendLine();
            r.AppendLine("      00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F");
            r.AppendLine();

            try
            {
                using (var ecIO = accquireIOInterface())
                {
                    for (int i = 0; i <= 0xF; ++i)
                    {
                        r.Append(" ");
                        r.Append((i << 4).ToString("X2", CultureInfo.InvariantCulture));
                        r.Append("  ");
                        for (int j = 0; j <= 0xF; ++j)
                        {
                            byte address = (byte)(i << 4 | j);
                            r.Append(" ");
                            r.Append(ecIO.ReadByte(address).ToString("X2", CultureInfo.InvariantCulture));
                        }

                        r.AppendLine();
                    }
                }
            }
            catch (WindowsEmbeddedControllerIO.BusMutexLockingFailedException e)
            {
                r.AppendLine(e.Message);
            }
            return r.ToString();
        }

        public static float ReadByte(IEmbeddedControllerIO ecIO, byte port)
        {
            return ecIO.ReadByte(port);
        }

        public static float ReadWordLE(IEmbeddedControllerIO ecIO, byte port)
        {
            return ecIO.ReadWordLE(port);
        }
        public static float ReadWordBE(IEmbeddedControllerIO ecIO, byte port)
        {
            return ecIO.ReadWordBE(port);
        }

        protected abstract IEmbeddedControllerIO accquireIOInterface();

        private readonly List<ECSensor> _sensors;
    }

    internal class WindowsEmbeddedController : EmbeddedController
    {
        public WindowsEmbeddedController(List<Source> sources, ISettings settings)
            : base(sources, settings)
        {
        }

        protected override IEmbeddedControllerIO accquireIOInterface()
        {
            return new WindowsEmbeddedControllerIO();
        }
    }
}
