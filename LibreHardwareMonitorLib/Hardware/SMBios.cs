// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Management;
using System.Text;

namespace LibreHardwareMonitor.Hardware
{
    /*
     * DSP0134 System Management BIOS (SMBIOS) Reference Specification v.3.3.0
     * Chapter 7.4.3
     */
    public enum ChassisSecurityStatus
    {
        Other = 1,
        Unknown,
        None,
        ExternalInterfaceLockedOut,
        ExternalInterfaceEnabled
    }

    /*
     * DSP0134 System Management BIOS (SMBIOS) Reference Specification v.3.3.0
     * Chapter 7.4.2
     */
    public enum ChassisStates
    {
        Other = 1,
        Unknown,
        Safe,
        Warning,
        Critical,
        NonRecoverable
    }

    /*
     * DSP0134 System Management BIOS (SMBIOS) Reference Specification v.3.3.0
     * Chapter 7.4.1
     */
    public enum ChassisType
    {
        Other = 1,
        Unknown,
        Desktop,
        LowProfileDesktop,
        PizzaBox,
        MiniTower,
        Tower,
        Portable,
        Laptop,
        Notebook,
        HandHeld,
        DockingStation,
        AllInOne,
        SubNotebook,
        SpaceSaving,
        LunchBox,
        MainServerChassis,
        ExpansionChassis,
        SubChassis,
        BusExpansionChassis,
        PeripheralChassis,
        RaidChassis,
        RackMountChassis,
        SealedCasePC,
        MultiSystemChassis,
        CompactPci,
        AdvancedTca,
        Blade,
        BladeEnclosure,
        Tablet,
        Convertible,
        Detachable,
        IoTGateway,
        EmbeddedPC,
        MiniPC,
        StickPC
    }

    /*
     * DSP0134 System Management BIOS (SMBIOS) Reference Specification v.3.3.0
     * Chapter 7.5.2
     */
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    public enum ProcessorFamily
    {
        Other = 1,
        Intel8086 = 3,
        Intel80286 = 4,
        Intel386,
        Intel486,
        Intel8087,
        Intel80287,
        Intel80387,
        Intel80487,
        IntelPentium,
        IntelPentiumPro,
        IntelPentiumII,
        IntelPentiumMMX,
        IntelCeleron,
        IntelPentiumIIXeon,
        IntelPentiumIII,
        M1,
        M2,
        IntelCeleronM,
        IntelPentium4HT,
        AmdDuron = 24,
        AmdK5,
        AmdK6,
        AmdK62,
        AmdK63,
        AmdAthlon,
        Amd2900,
        AmdK62Plus,
        PowerPc,
        PowerPc601,
        PowerPc603,
        PowerPc603Plus,
        PowerPc604,
        PowerPc620,
        PowerPcx704,
        PowerPc750,
        IntelCoreDuo,
        IntelCoreDuoMobile,
        IntelCoreSoloMobile,
        IntelAtom,
        IntelCoreM,
        IntelCoreM3,
        IntelCoreM5,
        IntelCoreM7,
        Alpha,
        Alpha21064,
        Alpha21066,
        Alpha21164,
        Alpha21164Pc,
        Alpha21164a,
        Alpha21264,
        Alpha21364,
        AmdTurionIIUltraDualCoreMobileM,
        AmdTurionDualCoreMobileM,
        AmdAthlonIIDualCoreM,
        AmdOpteron6100Series,
        AmdOpteron4100Series,
        AmdOpteron6200Series,
        AmdOpteron4200Series,
        AmdFxSeries,
        Mips,
        MipsR4000,
        MipsR4200,
        MipsR4400,
        MipsR4600,
        MipsR10000,
        AmdCSeries,
        AmdESeries,
        AmdASeries,
        AmdGSeries,
        AmdZSeries,
        AmdRSeries,
        AmdOpteron4300Series,
        AmdOpteron6300Series,
        AmdOpteron3300Series,
        AmdFireProSeries,
        Sparc,
        SuperSparc,
        MicroSparcII,
        MicroSparcIIep,
        UltraSparc,
        UltraSparcII,
        UltraSparcIIi,
        UltraSparcIII,
        UltraSparcIIIi,
        Motorola68040 = 96,
        Motorola68xxx,
        Motorola68000,
        Motorola68010,
        Motorola68020,
        Motorola68030,
        AmdAthlonX4QuadCore,
        AmdOpteronX1000Series,
        AmdOpteronX2000Series,
        AmdOpteronASeries,
        AmdOpteronX3000Series,
        AmdZen,
        Hobbit = 112,
        CrusoeTm5000 = 120,
        CrusoeTm3000,
        EfficeonTm8000,
        Weitek = 128,
        IntelItanium = 130,
        AmdAthlon64,
        AmdOpteron,
        AmdSempron,
        AmdTurio64Mobile,
        AmdOpteronDualCore,
        AmdAthlon64X2DualCore,
        AmdTurion64X2Mobile,
        AmdOpteronQuadCore,
        AmdOpteronThirdGen,
        AmdPhenomFXQuadCore,
        AmdPhenomX4QuadCore,
        AmdPhenomX2DualCore,
        AmdAthlonX2DualCore,
        PaRisc,
        PaRisc8500,
        PaRisc8000,
        PaRisc7300LC,
        PaRisc7200,
        PaRisc7100LC,
        PaRisc7100,
        V30 = 160,
        IntelXeon3200QuadCoreSeries,
        IntelXeon3000DualCoreSeries,
        IntelXeon5300QuadCoreSeries,
        IntelXeon5100DualCoreSeries,
        IntelXeon5000DualCoreSeries,
        IntelXeonLVDualCore,
        IntelXeonULVDualCore,
        IntelXeon7100Series,
        IntelXeon5400Series,
        IntelXeonQuadCore,
        IntelXeon5200DualCoreSeries,
        IntelXeon7200DualCoreSeries,
        IntelXeon7300QuadCoreSeries,
        IntelXeon7400QuadCoreSeries,
        IntelXeon7400MultiCoreSeries,
        IntelPentiumIIIXeon,
        IntelPentiumIIISpeedStep,
        IntelPentium4,
        IntelXeon,
        As400,
        IntelXeonMP,
        AmdAthlonXP,
        AmdAthlonMP,
        IntelItanium2,
        IntelPentiumM,
        IntelCeleronD,
        IntelPentiumD,
        IntelPentiumExtreme,
        IntelCoreSolo,
        IntelCore2Duo = 191,
        IntelCore2Solo,
        IntelCore2Extreme,
        IntelCore2Quad,
        IntelCore2ExtremeMobile,
        IntelCore2DuoMobile,
        IntelCore2SoloMobile,
        IntelCoreI7,
        IntelCeleronDualCore,
        Ibm390,
        PowerPcG4,
        PowerPcG5,
        Esa390G6,
        ZArchitecture,
        IntelCoreI5,
        IntelCoreI3,
        IntelCoreI9,
        ViaC7M = 210,
        ViaC7D,
        ViaC7,
        ViaEden,
        IntelXeonMultiCore,
        IntelXeon3xxxDualCoreSeries,
        IntelXeon3xxxQuadCoreSeries,
        ViaNano,
        IntelXeon5xxxDualCoreSeries,
        IntelXeon5xxxQuadCoreSeries,
        IntelXeon7xxxDualCoreSeries = 221,
        IntelXeon7xxxQuadCoreSeries,
        IntelXeon7xxxMultiCoreSeries,
        IntelXeon3400MultiCoreSeries,
        AmdOpteron3000Series = 228,
        AmdSempronII,
        AmdOpteronQuadCoreEmbedded,
        AmdPhenomTripleCore,
        AmdTurionUltraDualCoreMobile,
        AmdTurionDualCoreMobile,
        AmdTurionDualCore,
        AmdAthlonDualCore,
        AmdSempronSI,
        AmdPhenomII,
        AmdAthlonII,
        AmdOpteronSixCore,
        AmdSempronM,
        IntelI860 = 250,
        IntelI960,
        ArmV7 = 256,
        ArmV8,
        HitachiSh3,
        HitachiSh4,
        Arm,
        StrongArm,
        _686,
        MediaGX,
        MII,
        WinChip,
        Dsp,
        VideoProcessor
    }

    /*
     * DSP0134 System Management BIOS (SMBIOS) Reference Specification v.3.3.0
     * Chapter 7.5.1
     */
    public enum ProcessorType
    {
        Other = 1,
        Unknown,
        CentralProcessor,
        MathProcessor,
        DSPProcessor,
        VideoProcessor
    }

    /*
     * DSP0134 System Management BIOS (SMBIOS) Reference Specification v.3.3.0
     * Chapter 7.5.5
     */
    public enum ProcessorSocket
    {
        Other = 1,
        Unknown,
        DaughterBoard,
        ZifSocket,
        PiggyBack,
        None,
        LifSocket,
        Zif423 = 13,
        A,
        Zif478,
        Zif754,
        Zif940,
        Zif939,
        MPga604,
        Lga771,
        Lga775,
        S1,
        AM2,
        F,
        Lga1366,
        G34,
        AM3,
        C32,
        Lga1156,
        Lga1567,
        Pga988A,
        Bga1288,
        RPga088B,
        Bga1023,
        Bga1224,
        Lga1155,
        Lga1356,
        Lga2011,
        FS1,
        FS2,
        FM1,
        FM2,
        Lga20113,
        Lga13563,
        Lga1150,
        Bga1168,
        Bga1234,
        Bga1364,
        AM4,
        Lga1151,
        Bga1356,
        Bga1440,
        Bga1515,
        Lga36471,
        SP3,
        SP3R2,
        Lga2066,
        Bga1510,
        Bga1528,
        Lga4189
    }

    public enum SystemWakeUp
    {
        Reserved,
        Other,
        Unknown,
        ApmTimer,
        ModemRing,
        LanRemote,
        PowerSwitch,
        PciPme,
        ACPowerRestored
    }

    public class InformationBase
    {
        private readonly byte[] _data;
        private readonly string[] _strings;

        protected InformationBase(byte type, ushort handle, byte[] data, string[] strings)
        {
            Type = type;
            Handle = handle;
            _data = data;
            _strings = strings;
        }

        protected ushort Handle { get; }

        protected byte Type { get; }

        protected int GetByte(int offset)
        {
            if (offset < _data.Length && offset >= 0)
                return _data[offset];


            return 0;
        }

        protected int GetWord(int offset)
        {
            if (offset + 1 < _data.Length && offset >= 0)
                return (_data[offset + 1] << 8) | _data[offset];


            return 0;
        }

        protected string GetString(int offset)
        {
            if (offset < _data.Length && _data[offset] > 0 && _data[offset] <= _strings.Length)
                return _strings[_data[offset] - 1];


            return string.Empty;
        }
    }

    public class BiosInformation : InformationBase
    {
        internal BiosInformation(string vendor, string version, string date = null, ulong? size = null) : base(0x00, 0, null, null)
        {
            Vendor = vendor;
            Version = version;
            Date = ParseBiosDate(date);
            Size = size;
        }

        internal BiosInformation(byte type, ushort handle, byte[] data, string[] strings) : base(type, handle, data, strings)
        {
            Vendor = GetString(0x04);
            Version = GetString(0x05);
            Date = ParseBiosDate(GetString(0x08));
            Size = CalculateBiosRomSize();
        }

        public DateTime? Date { get; }

        public ulong? Size { get; }

        public string Vendor { get; }

        public string Version { get; }

        private ulong? CalculateBiosRomSize()
        {
            int biosRomSize = GetByte(0x09);
            int extendedBiosRomSize = GetWord(0x18);
            bool isExtendedBiosRomSize = biosRomSize == 0xFF && extendedBiosRomSize != 0;
            if (!isExtendedBiosRomSize)
                return 65536 * (ulong)(biosRomSize + 1);


            int unit = (extendedBiosRomSize & 0xC000) >> 14;
            ulong extendedSize = (ulong)(extendedBiosRomSize & ~0xC000) * 1024 * 1024;

            switch (unit)
            {
                case 0x00: return extendedSize; // Megabytes
                case 0x01: return extendedSize * 1024; // Gigabytes - might overflow in the future
                default:
                    return null; // Other patterns not defined in DMI 3.2.0
            }
        }

        private static DateTime? ParseBiosDate(string biosDate)
        {
            string[] parts = (biosDate ?? string.Empty).Split('/');

            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int month) &&
                int.TryParse(parts[1], out int day) &&
                int.TryParse(parts[2], out int year))
            {
                return new DateTime(year < 100 ? 1900 + year : year, month, day);
            }

            return null;
        }
    }

    public class SystemInformation : InformationBase
    {
        internal SystemInformation
            (string manufacturerName, string productName, string version, string serialNumber, string family, SystemWakeUp wakeUp = SystemWakeUp.Unknown) : base(0x01, 0, null, null)
        {
            ManufacturerName = manufacturerName;
            ProductName = productName;
            Version = version;
            SerialNumber = serialNumber;
            Family = family;
            WakeUp = wakeUp;
        }

        internal SystemInformation(byte type, ushort handle, byte[] data, string[] strings) : base(type, handle, data, strings)
        {
            ManufacturerName = GetString(0x04);
            ProductName = GetString(0x05);
            Version = GetString(0x06);
            SerialNumber = GetString(0x07);
            Family = GetString(0x1A);
            WakeUp = (SystemWakeUp)GetByte(0x18);
        }

        public string Family { get; }

        public string ManufacturerName { get; }

        public string ProductName { get; }

        public string SerialNumber { get; }

        public string Version { get; }

        public SystemWakeUp WakeUp { get; }
    }

    public class ChassisInformation : InformationBase
    {
        internal ChassisInformation(byte type, ushort handle, byte[] data, string[] strings) : base(type, handle, data, strings)
        {
            ManufacturerName = GetString(0x04).Trim();
            Version = GetString(0x06).Trim();
            SerialNumber = GetString(0x07).Trim();
            AssetTag = GetString(0x08).Trim();
            RackHeight = GetByte(0x11);
            PowerCords = GetByte(0x12);
            SKU = GetString(0x15).Trim();
            LockDetected = (GetByte(0x05) & 128) == 128;
            ChassisType = (ChassisType)(GetByte(0x05) & 127);
            BootUpState = (ChassisStates)GetByte(0x09);
            PowerSupplyState = (ChassisStates)GetByte(0x0A);
            ThermalState = (ChassisStates)GetByte(0x0B);
            SecurityStatus = (ChassisSecurityStatus)GetByte(0x0C);
        }

        public string AssetTag { get; }

        public ChassisStates BootUpState { get; }

        public ChassisType ChassisType { get; }

        public bool LockDetected { get; set; }

        public string ManufacturerName { get; }

        public int PowerCords { get; }

        public ChassisStates PowerSupplyState { get; }

        public int RackHeight { get; }

        public ChassisSecurityStatus SecurityStatus { get; set; }

        public string SerialNumber { get; }

        public string SKU { get; }

        public ChassisStates ThermalState { get; }

        public string Version { get; }
    }

    public class BaseBoardInformation : InformationBase
    {
        internal BaseBoardInformation(string manufacturerName, string productName, string version, string serialNumber) : base(0x02, 0, null, null)
        {
            ManufacturerName = manufacturerName;
            ProductName = productName;
            Version = version;
            SerialNumber = serialNumber;
        }

        internal BaseBoardInformation(byte type, ushort handle, byte[] data, string[] strings) : base(type, handle, data, strings)
        {
            ManufacturerName = GetString(0x04).Trim();
            ProductName = GetString(0x05).Trim();
            Version = GetString(0x06).Trim();
            SerialNumber = GetString(0x07).Trim();
        }

        public string ManufacturerName { get; }

        public string ProductName { get; }

        public string SerialNumber { get; }

        public string Version { get; }
    }

    public class ProcessorInformation : InformationBase
    {
        internal ProcessorInformation(byte type, ushort handle, byte[] data, string[] strings) : base(type, handle, data, strings)
        {
            SocketDesignation = GetString(0x04).Trim();
            ManufacturerName = GetString(0x07).Trim();
            Version = GetString(0x10).Trim();
            CoreCount = GetByte(0x23) != 255 ? GetByte(0x23) : GetWord(0x2A);
            CoreEnabled = GetByte(0x24) != 255 ? GetByte(0x24) : GetWord(0x2C);
            ThreadCount = GetByte(0x25) != 255 ? GetByte(0x25) : GetWord(0x2E);
            ExternalClock = GetWord(0x12);
            MaxSpeed = GetWord(0x14);
            CurrentSpeed = GetWord(0x16);
            Serial = GetString(0x20).Trim();

            ProcessorType = (ProcessorType)GetByte(0x05);
            Socket = (ProcessorSocket)GetByte(0x19);

            int family = GetByte(0x06);
            Family = (ProcessorFamily)(family == 254 ? GetWord(0x28) : family);
        }

        public int CoreCount { get; }

        public int CoreEnabled { get; }

        public int ExternalClock { get; }

        public int MaxSpeed { get; }

        public int CurrentSpeed { get; }

        public string Serial { get; }

        public ProcessorType ProcessorType { get; }

        public ProcessorSocket Socket { get; }

        public ProcessorFamily Family { get; }

        public string SocketDesignation { get; }

        public string ManufacturerName { get; }

        public int ThreadCount { get; }

        public string Version { get; }
    }

    public class MemoryDevice : InformationBase
    {
        internal MemoryDevice(byte type, ushort handle, byte[] data, string[] strings) : base(type, handle, data, strings)
        {
            DeviceLocator = GetString(0x10).Trim();
            BankLocator = GetString(0x11).Trim();
            ManufacturerName = GetString(0x17).Trim();
            SerialNumber = GetString(0x18).Trim();
            PartNumber = GetString(0x1A).Trim();
            Speed = GetWord(0x15);
        }

        public string BankLocator { get; }

        public string DeviceLocator { get; }

        public string ManufacturerName { get; }

        public string PartNumber { get; }

        public string SerialNumber { get; }

        public int Speed { get; }
    }

    public class SMBios
    {
        private readonly byte[] _raw;
        private readonly Version _version;

        public SMBios()
        {
            if (Software.OperatingSystem.IsUnix)
            {
                _raw = null;

                string boardVendor = ReadSysFs("/sys/class/dmi/id/board_vendor");
                string boardName = ReadSysFs("/sys/class/dmi/id/board_name");
                string boardVersion = ReadSysFs("/sys/class/dmi/id/board_version");
                Board = new BaseBoardInformation(boardVendor, boardName, boardVersion, null);

                string systemVendor = ReadSysFs("/sys/class/dmi/id/sys_vendor");
                string productName = ReadSysFs("/sys/class/dmi/id/product_name");
                string productVersion = ReadSysFs("/sys/class/dmi/id/product_version");
                System = new SystemInformation(systemVendor, productName, productVersion, null, null);

                string biosVendor = ReadSysFs("/sys/class/dmi/id/bios_vendor");
                string biosVersion = ReadSysFs("/sys/class/dmi/id/bios_version");
                string biosDate = ReadSysFs("/sys/class/dmi/id/bios_date");
                Bios = new BiosInformation(biosVendor, biosVersion, biosDate);

                MemoryDevices = new MemoryDevice[0];
            }
            else
            {
                List<MemoryDevice> memoryDeviceList = new List<MemoryDevice>();

                _raw = null;
                byte majorVersion = 0;
                byte minorVersion = 0;

                try
                {
                    ManagementObjectCollection collection;
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM MSSMBios_RawSMBiosTables"))
                    {
                        collection = searcher.Get();
                    }

                    foreach (ManagementBaseObject mo in collection)
                    {
                        _raw = (byte[])mo["SMBiosData"];
                        majorVersion = (byte)mo["SmbiosMajorVersion"];
                        minorVersion = (byte)mo["SmbiosMinorVersion"];

                        break;
                    }
                }
                catch
                { }

                if (majorVersion > 0 || minorVersion > 0)
                    _version = new Version(majorVersion, minorVersion);

                if (_raw != null && _raw.Length > 0)
                {
                    int offset = 0;
                    byte type = _raw[offset];
                    while (offset + 4 < _raw.Length && type != 127)
                    {
                        type = _raw[offset];
                        int length = _raw[offset + 1];
                        ushort handle = (ushort)((_raw[offset + 2] << 8) | _raw[offset + 3]);

                        if (offset + length > _raw.Length)
                            break;


                        byte[] data = new byte[length];
                        Array.Copy(_raw, offset, data, 0, length);
                        offset += length;

                        List<string> stringsList = new List<string>();
                        if (offset < _raw.Length && _raw[offset] == 0)
                            offset++;

                        while (offset < _raw.Length && _raw[offset] != 0)
                        {
                            StringBuilder sb = new StringBuilder();
                            while (offset < _raw.Length && _raw[offset] != 0)
                            {
                                sb.Append((char)_raw[offset]);
                                offset++;
                            }

                            offset++;
                            stringsList.Add(sb.ToString());
                        }

                        offset++;
                        switch (type)
                        {
                            case 0x00:
                                Bios = new BiosInformation(type, handle, data, stringsList.ToArray());
                                break;
                            case 0x01:
                                System = new SystemInformation(type, handle, data, stringsList.ToArray());
                                break;
                            case 0x02:
                                Board = new BaseBoardInformation(type, handle, data, stringsList.ToArray());
                                break;
                            case 0x03:
                                Chassis = new ChassisInformation(type, handle, data, stringsList.ToArray());
                                break;
                            case 0x04:
                                Processor = new ProcessorInformation(type, handle, data, stringsList.ToArray());
                                break;
                            case 0x11:
                                MemoryDevice m = new MemoryDevice(type, handle, data, stringsList.ToArray());
                                memoryDeviceList.Add(m);
                                break;
                        }
                    }
                }

                MemoryDevices = memoryDeviceList.ToArray();
            }
        }

        public BiosInformation Bios { get; }

        public BaseBoardInformation Board { get; }

        public ChassisInformation Chassis { get; }

        public MemoryDevice[] MemoryDevices { get; }

        public ProcessorInformation Processor { get; }

        public SystemInformation System { get; }

        private static string ReadSysFs(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (StreamReader reader = new StreamReader(path))
                        return reader.ReadLine();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public string GetReport()
        {
            StringBuilder r = new StringBuilder();

            if (_version != null)
            {
                r.Append("SMBios Version: ");
                r.AppendLine(_version.ToString(2));
                r.AppendLine();
            }

            if (Bios != null)
            {
                r.Append("BIOS Vendor: ");
                r.AppendLine(Bios.Vendor);
                r.Append("BIOS Version: ");
                r.AppendLine(Bios.Version);
                if (Bios.Date != null)
                {
                    r.Append("BIOS Date: ");
                    r.AppendLine(Bios.Date.Value.ToShortDateString());
                }

                if (Bios.Size != null)
                {
                    const int megabyte = 1024 * 1024;
                    r.Append("BIOS Size: ");
                    if (Bios.Size > megabyte)
                        r.AppendLine(Bios.Size.Value / megabyte + " MB");
                    else
                        r.AppendLine(Bios.Size.Value / 1024 + " KB");
                }

                r.AppendLine();
            }

            if (System != null)
            {
                r.Append("System Manufacturer: ");
                r.AppendLine(System.ManufacturerName);
                r.Append("System Name: ");
                r.AppendLine(System.ProductName);
                r.Append("System Version: ");
                r.AppendLine(System.Version);
                r.Append("System Wakeup: ");
                r.AppendLine(System.WakeUp.ToString());
                r.AppendLine();
            }

            if (Board != null)
            {
                r.Append("Motherboard Manufacturer: ");
                r.AppendLine(Board.ManufacturerName);
                r.Append("Motherboard Name: ");
                r.AppendLine(Board.ProductName);
                r.Append("Motherboard Version: ");
                r.AppendLine(Board.Version);
                r.Append("Motherboard Serial: ");
                r.AppendLine(Board.SerialNumber);
                r.AppendLine();
            }

            if (Chassis != null)
            {
                r.Append("Chassis Type: ");
                r.AppendLine(Chassis.ChassisType.ToString());
                r.Append("Chassis Manufacturer: ");
                r.AppendLine(Chassis.ManufacturerName);
                r.Append("Chassis Version: ");
                r.AppendLine(Chassis.Version);
                r.Append("Chassis Serial: ");
                r.AppendLine(Chassis.SerialNumber);
                r.Append("Chassis Asset Tag: ");
                r.AppendLine(Chassis.AssetTag);
                if (!string.IsNullOrEmpty(Chassis.SKU))
                {
                    r.Append("Chassis SKU: ");
                    r.AppendLine(Chassis.SKU);
                }

                r.Append("Chassis Boot Up State: ");
                r.AppendLine(Chassis.BootUpState.ToString());
                r.Append("Chassis Power Supply State: ");
                r.AppendLine(Chassis.PowerSupplyState.ToString());
                r.Append("Chassis Thermal State: ");
                r.AppendLine(Chassis.ThermalState.ToString());
                r.Append("Chassis Power Cords: ");
                r.AppendLine(Chassis.PowerCords.ToString());
                if (Chassis.RackHeight > 0)
                {
                    r.Append("Chassis Rack Height: ");
                    r.AppendLine(Chassis.RackHeight.ToString());
                }

                r.Append("Chassis Lock Detected: ");
                r.AppendLine(Chassis.LockDetected ? "Yes" : "No");
                r.Append("Chassis Security Status: ");
                r.AppendLine(Chassis.SecurityStatus.ToString());
                r.AppendLine();
            }

            if (Processor != null)
            {
                r.Append("Processor Manufacturer: ");
                r.AppendLine(Processor.ManufacturerName);
                r.Append("Processor Type: ");
                r.AppendLine(Processor.ProcessorType.ToString());
                r.Append("Processor Version: ");
                r.AppendLine(Processor.Version);
                r.Append("Processor Serial: ");
                r.AppendLine(Processor.Serial);
                r.Append("Processor Socket Destignation: ");
                r.AppendLine(Processor.SocketDesignation);
                r.Append("Processor Socket: ");
                r.AppendLine(Processor.Socket.ToString());
                r.Append("Processor Version: ");
                r.AppendLine(Processor.Version);
                r.Append("Processor Family: ");
                r.AppendLine(Processor.Family.ToString());
                r.Append("Processor Core Count: ");
                r.AppendLine(Processor.CoreCount.ToString());
                r.Append("Processor Core Enabled: ");
                r.AppendLine(Processor.CoreEnabled.ToString());
                r.Append("Processor Thread Count: ");
                r.AppendLine(Processor.ThreadCount.ToString());
                r.Append("Processor External Clock: ");
                r.Append(Processor.ExternalClock);
                r.AppendLine(" Mhz");
                r.Append("Processor Max Speed: ");
                r.Append(Processor.MaxSpeed);
                r.AppendLine(" Mhz");
                r.Append("Processor Current Speed: ");
                r.Append(Processor.CurrentSpeed);
                r.AppendLine(" Mhz");
                r.AppendLine();
            }

            for (int i = 0; i < MemoryDevices.Length; i++)
            {
                r.Append("Memory Device [" + i + "] Manufacturer: ");
                r.AppendLine(MemoryDevices[i].ManufacturerName);
                r.Append("Memory Device [" + i + "] Part Number: ");
                r.AppendLine(MemoryDevices[i].PartNumber);
                r.Append("Memory Device [" + i + "] Device Locator: ");
                r.AppendLine(MemoryDevices[i].DeviceLocator);
                r.Append("Memory Device [" + i + "] Bank Locator: ");
                r.AppendLine(MemoryDevices[i].BankLocator);
                r.Append("Memory Device [" + i + "] Speed: ");
                r.Append(MemoryDevices[i].Speed);
                r.AppendLine(" MHz");
                r.AppendLine();
            }

            if (_raw != null)
            {
                string base64 = Convert.ToBase64String(_raw);
                r.AppendLine("SMBios Table");
                r.AppendLine();

                for (int i = 0; i < Math.Ceiling(base64.Length / 64.0); i++)
                {
                    r.Append(" ");
                    for (int j = 0; j < 0x40; j++)
                    {
                        int index = (i << 6) | j;
                        if (index < base64.Length)
                        {
                            r.Append(base64[index]);
                        }
                    }

                    r.AppendLine();
                }

                r.AppendLine();
            }

            return r.ToString();
        }
    }
}
