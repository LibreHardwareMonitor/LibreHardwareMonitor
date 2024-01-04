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
            ushort smb_addr = SmBusDevice.DetectSmBus();
            for (byte addr = 0x09; addr != 0;)
            {
                addr = SmBusDevice.DetectDevice((byte)(addr + 1), smb_addr);
                if (addr != 0)
                {
                    if (DetectFintekF753XX(addr, smb_addr)) continue;

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

        private bool DetectFintekF753XX(byte addr, ushort smb_addr)
        {
            SmBusDevice tempDev = new SmBusDevice(addr, smb_addr);

            // read word don't work for this chip, or for target hardware
            ushort vid = (ushort)((tempDev.ReadByte(FINTEK_VENDOR_ID_SMBUS_REGISTER) << 8) | tempDev.ReadByte(FINTEK_VENDOR_ID_SMBUS_REGISTER + 1));
            if (vid != FINTEK_VENDOR_ID)
                return false; // this is not a Fintek device

            Chip chip = (Chip)((tempDev.ReadByte(CHIP_ID_SMBUS_REGISTER) << 8) | tempDev.ReadByte(CHIP_ID_SMBUS_REGISTER + 1));

            switch (chip)
            {
                case Chip.F75373S:
                case Chip.F75375S:
                case Chip.F75387:
                    _superIOs.Add(new F753XX(chip, tempDev));
                    break;

                default:
                    ReportUnknownChip(addr, smb_addr, "Fintek", (int)chip);
                    return false;
            }
            return true;
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
