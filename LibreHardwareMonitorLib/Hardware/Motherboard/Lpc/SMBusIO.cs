using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc
{
    internal class SmBusIO
    {
        private readonly StringBuilder _report = new();
        private readonly List<ISuperIO> _superIOs = new();

        public SmBusIO(Motherboard motherboard)
        {

            if (!Ring0.IsOpen || !allowDetection)
                return;

            Detect();
        }

        private void ReportUnknownChip(byte addr, ushort smb_addr, string type, int chip)
        {
            _report.Append("SMBus Chip ID: Unknown ");
            _report.Append(type);
            _report.Append(" with ID 0x");
            _report.Append(chip.ToString("X", CultureInfo.InvariantCulture));
            _report.Append(" addr:0x");
            _report.Append(addr.ToString("X", CultureInfo.InvariantCulture));
            _report.Append(" bus:");
            _report.AppendLine(smb_addr.ToString("X", CultureInfo.InvariantCulture));
            _report.AppendLine();
        }

        public string GetReport()
        {
            if (_report.Length > 0)
            {
                return "SMBusIO" + Environment.NewLine + Environment.NewLine + _report;
            }

            return null;
        }

        public ISuperIO[] SuperIO => _superIOs.ToArray();

        private void Detect()
        {
            ushort smb_addr = 0; // TODO: Detect SM Bus, "0" disables device detection
            for (byte addr = 0x09; addr != 0;)
            {
                addr = SmBusDevice.DetectDevice((byte)(addr + 1), smb_addr);
                if (addr != 0)
                {
                    // chip id check here, then "continue" if it's ok

                    // if no matches, but device on bus exists:
                    _report.Append("Unknown SMBus device:");
                    _report.Append(" addr:0x");
                    _report.Append(addr.ToString("X", CultureInfo.InvariantCulture));
                    _report.Append(" bus:");
                    _report.AppendLine(smb_addr.ToString("X", CultureInfo.InvariantCulture));
                    _report.AppendLine();
                }
            }
        }

        public static bool isDetectEnabled
        {
            get { return allowDetection; }
            set { allowDetection = value; }
        }

        private static bool allowDetection;

        private const byte CHIP_ID_SMBUS_REGISTER = 0x5A;
        private const byte FINTEK_VENDOR_ID_SMBUS_REGISTER = 0x5D;
        private const ushort FINTEK_VENDOR_ID = 0x1934;
    }
}
