// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LibreHardwareMonitor.Interop;

// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace LibreHardwareMonitor.Hardware
{
    /// <summary>
    /// Chassis security status based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.4.3</see>.
    /// </summary>
    public enum ChassisSecurityStatus
    {
        Other = 1,
        Unknown,
        None,
        ExternalInterfaceLockedOut,
        ExternalInterfaceEnabled
    }

    /// <summary>
    /// Chassis state based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.4.2</see>.
    /// </summary>
    public enum ChassisStates
    {
        Other = 1,
        Unknown,
        Safe,
        Warning,
        Critical,
        NonRecoverable
    }

    /// <summary>
    /// Chassis type based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.4.1</see>.
    /// </summary>
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
        SealedCasePc,
        MultiSystemChassis,
        CompactPci,
        AdvancedTca,
        Blade,
        BladeEnclosure,
        Tablet,
        Convertible,
        Detachable,
        IoTGateway,
        EmbeddedPc,
        MiniPc,
        StickPc
    }

    /// <summary>
    /// Processor family based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.5.2</see>.
    /// </summary>
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

    /// <summary>
    /// Processor type based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.5.1</see>.
    /// </summary>
    public enum ProcessorType
    {
        Other = 1,
        Unknown,
        CentralProcessor,
        MathProcessor,
        DspProcessor,
        VideoProcessor
    }

    /// <summary>
    /// Processor socket based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.5.5</see>.
    /// </summary>
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

    /// <summary>
    /// System wake-up type based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.2.2</see>.
    /// </summary>
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
        AcPowerRestored
    }

    /// <summary>
    /// Cache associativity based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.8.5</see>.
    /// </summary>
    public enum CacheAssociativity
    {
        Other = 1,
        Unknown,
        DirectMapped,
        _2Way,
        _4Way,
        FullyAssociative,
        _8Way,
        _16Way,
        _12Way,
        _24Way,
        _32Way,
        _48Way,
        _64Way,
        _20Way,
    }

    /// <summary>
    /// Processor cache level.
    /// </summary>
    public enum CacheDesignation
    {
        Other,
        L1,
        L2,
        L3
    }

    public class InformationBase
    {
        private readonly byte[] _data;
        private readonly IList<string> _strings;

        /// <summary>
        /// Initializes a new instance of the <see cref="InformationBase" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="strings">The strings.</param>
        protected InformationBase(byte[] data, IList<string> strings)
        {
            _data = data;
            _strings = strings;
        }

        /// <summary>
        /// Gets the byte.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns><see cref="int" />.</returns>
        protected int GetByte(int offset)
        {
            if (offset < _data.Length && offset >= 0)
                return _data[offset];


            return 0;
        }

        /// <summary>
        /// Gets the word.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns><see cref="int" />.</returns>
        protected int GetWord(int offset)
        {
            if (offset + 1 < _data.Length && offset >= 0)
                return (_data[offset + 1] << 8) | _data[offset];


            return 0;
        }

        /// <summary>
        /// Gets the string.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns><see cref="string" />.</returns>
        protected string GetString(int offset)
        {
            if (offset < _data.Length && _data[offset] > 0 && _data[offset] <= _strings.Count)
                return _strings[_data[offset] - 1];


            return string.Empty;
        }
    }

    /// <summary>
    /// Motherboard BIOS information obtained from the SMBIOS table.
    /// </summary>
    public class BiosInformation : InformationBase
    {
        internal BiosInformation(string vendor, string version, string date = null, ulong? size = null) : base(null, null)
        {
            Vendor = vendor;
            Version = version;
            Date = GetDate(date);
            Size = size;
        }

        internal BiosInformation(byte[] data, IList<string> strings) : base(data, strings)
        {
            Vendor = GetString(0x04);
            Version = GetString(0x05);
            Date = GetDate(GetString(0x08));
            Size = GetSize();
        }

        /// <summary>
        /// Gets the BIOS release date.
        /// </summary>
        public DateTime? Date { get; }

        /// <summary>
        /// Gets the size of the physical device containing the BIOS.
        /// </summary>
        public ulong? Size { get; }

        /// <summary>
        /// Gets the string number of the BIOS Vendor’s Name.
        /// </summary>
        public string Vendor { get; }

        /// <summary>
        /// Gets the string number of the BIOS Version. This value is a free-form string that may contain Core and OEM version information.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <returns><see cref="Nullable{Int64}" />.</returns>
        private ulong? GetSize()
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

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="date">The bios date.</param>
        /// <returns><see cref="Nullable{DateTime}" />.</returns>
        private static DateTime? GetDate(string date)
        {
            string[] parts = (date ?? string.Empty).Split('/');

            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int month) &&
                int.TryParse(parts[1], out int day) &&
                int.TryParse(parts[2], out int year))
            {
                if (month > 12)
                {
                    int tmp = month;
                    month = day;
                    day = tmp;
                }

                return new DateTime(year < 100 ? 1900 + year : year, month, day);
            }

            return null;
        }
    }

    /// <summary>
    /// System information obtained from the SMBIOS table.
    /// </summary>
    public class SystemInformation : InformationBase
    {
        internal SystemInformation
        (
            string manufacturerName,
            string productName,
            string version,
            string serialNumber,
            string family,
            SystemWakeUp wakeUp = SystemWakeUp.Unknown) : base(null, null)
        {
            ManufacturerName = manufacturerName;
            ProductName = productName;
            Version = version;
            SerialNumber = serialNumber;
            Family = family;
            WakeUp = wakeUp;
        }

        internal SystemInformation(byte[] data, IList<string> strings) : base(data, strings)
        {
            ManufacturerName = GetString(0x04);
            ProductName = GetString(0x05);
            Version = GetString(0x06);
            SerialNumber = GetString(0x07);
            Family = GetString(0x1A);
            WakeUp = (SystemWakeUp)GetByte(0x18);
        }

        /// <summary>
        /// Gets the family associated with system.
        /// <para>
        /// This text string identifies the family to which a particular computer belongs. A family refers to a set of computers that are similar but not identical from a hardware or software point of
        /// view. Typically, a family is composed of different computer models, which have different configurations and pricing points. Computers in the same family often have similar branding and cosmetic
        /// features.
        /// </para>
        /// </summary>
        public string Family { get; }

        /// <summary>
        /// Gets the manufacturer name associated with system.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the product name associated with system.
        /// </summary>
        public string ProductName { get; }

        /// <summary>
        /// Gets the serial number string associated with system.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// Gets the version string associated with system.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets <inheritdoc cref="SystemWakeUp" />
        /// </summary>
        public SystemWakeUp WakeUp { get; }
    }

    /// <summary>
    /// Chassis information obtained from the SMBIOS table.
    /// </summary>
    public class ChassisInformation : InformationBase
    {
        internal ChassisInformation(byte[] data, IList<string> strings) : base(data, strings)
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

        /// <summary>
        /// Gets the asset tag associated with the enclosure or chassis.
        /// </summary>
        public string AssetTag { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ChassisStates" />
        /// </summary>
        public ChassisStates BootUpState { get; }

        /// <summary>
        /// Gets <inheritdoc cref="LibreHardwareMonitor.Hardware.ChassisType" />
        /// </summary>
        public ChassisType ChassisType { get; }

        /// <summary>
        /// Gets or sets the chassis lock.
        /// </summary>
        /// <returns>Chassis lock is present if <see langword="true" />. Otherwise, either a lock is not present or it is unknown if the enclosure has a lock.</returns>
        public bool LockDetected { get; set; }

        /// <summary>
        /// Gets the string describing the chassis or enclosure manufacturer name.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the number of power cords associated with the enclosure or chassis.
        /// </summary>
        public int PowerCords { get; }

        /// <summary>
        /// Gets the state of the enclosure’s power supply (or supplies) when last booted.
        /// </summary>
        public ChassisStates PowerSupplyState { get; }

        /// <summary>
        /// Gets the height of the enclosure, in 'U's. A U is a standard unit of measure for the height of a rack or rack-mountable component and is equal to 1.75 inches or 4.445 cm. A value of <c>0</c>
        /// indicates that the enclosure height is unspecified.
        /// </summary>
        public int RackHeight { get; }

        /// <summary>
        /// Gets the physical security status of the enclosure when last booted.
        /// </summary>
        public ChassisSecurityStatus SecurityStatus { get; set; }

        /// <summary>
        /// Gets the string describing the chassis or enclosure serial number.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// Gets the string describing the chassis or enclosure SKU number.
        /// </summary>
        public string SKU { get; }

        /// <summary>
        /// Gets the thermal state of the enclosure when last booted.
        /// </summary>
        public ChassisStates ThermalState { get; }

        /// <summary>
        /// Gets the number of null-terminated string representing the chassis or enclosure version.
        /// </summary>
        public string Version { get; }
    }

    /// <summary>
    /// Motherboard information obtained from the SMBIOS table.
    /// </summary>
    public class BaseBoardInformation : InformationBase
    {
        internal BaseBoardInformation(string manufacturerName, string productName, string version, string serialNumber) : base(null, null)
        {
            ManufacturerName = manufacturerName;
            ProductName = productName;
            Version = version;
            SerialNumber = serialNumber;
        }

        internal BaseBoardInformation(byte[] data, IList<string> strings) : base(data, strings)
        {
            ManufacturerName = GetString(0x04).Trim();
            ProductName = GetString(0x05).Trim();
            Version = GetString(0x06).Trim();
            SerialNumber = GetString(0x07).Trim();
        }

        /// <summary>
        /// Gets the value that represents the manufacturer's name.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the value that represents the motherboard's name.
        /// </summary>
        public string ProductName { get; }

        /// <summary>
        /// Gets the value that represents the motherboard's serial number.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// Gets the value that represents the motherboard's revision number.
        /// </summary>
        public string Version { get; }
    }

    /// <summary>
    /// Processor information obtained from the SMBIOS table.
    /// </summary>
    public class ProcessorInformation : InformationBase
    {
        internal ProcessorInformation(byte[] data, IList<string> strings) : base(data, strings)
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

        /// <summary>
        /// Gets the value that represents the number of cores per processor socket.
        /// </summary>
        public int CoreCount { get; }

        /// <summary>
        /// Gets the value that represents the number of enabled cores per processor socket.
        /// </summary>
        public int CoreEnabled { get; }

        /// <summary>
        /// Gets the value that represents the current processor speed (in MHz).
        /// </summary>
        public int CurrentSpeed { get; }

        /// <summary>
        /// Gets the external Clock Frequency, in MHz. If the value is unknown, the field is set to 0.
        /// </summary>
        public int ExternalClock { get; }

        /// <summary>
        /// Gets <inheritdoc cref="LibreHardwareMonitor.Hardware.ProcessorFamily" />
        /// </summary>
        public ProcessorFamily Family { get; }

        /// <summary>
        /// Gets the string number of Processor Manufacturer.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the value that represents the maximum processor speed (in MHz) supported by the system for this processor socket.
        /// </summary>
        public int MaxSpeed { get; }

        /// <summary>
        /// Gets <inheritdoc cref="LibreHardwareMonitor.Hardware.ProcessorType" />
        /// </summary>
        public ProcessorType ProcessorType { get; }

        /// <summary>
        /// Gets the value that represents the string number for the serial number of this processor.
        /// <para>This value is set by the manufacturer and normally not changeable.</para>
        /// </summary>
        public string Serial { get; }

        /// <summary>
        /// Gets <inheritdoc cref="LibreHardwareMonitor.Hardware.ProcessorSocket" />
        /// </summary>
        public ProcessorSocket Socket { get; }

        /// <summary>
        /// Gets the string number for Reference Designation.
        /// </summary>
        public string SocketDesignation { get; }

        /// <summary>
        /// Gets the value that represents the number of threads per processor socket.
        /// </summary>
        public int ThreadCount { get; }

        /// <summary>
        /// Gets the value that represents the string number describing the Processor.
        /// </summary>
        public string Version { get; }
    }

    /// <summary>
    /// Processor cache information obtained from the SMBIOS table.
    /// </summary>
    public class ProcessorCache : InformationBase
    {
        internal ProcessorCache(byte[] data, IList<string> strings) : base(data, strings)
        {
            Designation = GetCacheDesignation();
            Associativity = (CacheAssociativity)GetByte(0x12);
            Size = GetWord(0x09);
        }

        /// <summary>
        /// Gets <inheritdoc cref="CacheAssociativity" />
        /// </summary>
        public CacheAssociativity Associativity { get; }

        /// <summary>
        /// Gets <inheritdoc cref="CacheDesignation" />
        /// </summary>
        public CacheDesignation Designation { get; }

        /// <summary>
        /// Gets the value that represents the installed cache size.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the cache designation.
        /// </summary>
        /// <returns><see cref="CacheDesignation" />.</returns>
        private CacheDesignation GetCacheDesignation()
        {
            string rawCacheType = GetString(0x04);

            if (rawCacheType.Contains("L1"))
                return CacheDesignation.L1;

            if (rawCacheType.Contains("L2"))
                return CacheDesignation.L2;

            if (rawCacheType.Contains("L3"))
                return CacheDesignation.L3;


            return CacheDesignation.Other;
        }
    }

    /// <summary>
    /// Memory information obtained from the SMBIOS table.
    /// </summary>
    public class MemoryDevice : InformationBase
    {
        internal MemoryDevice(byte[] data, IList<string> strings) : base(data, strings)
        {
            DeviceLocator = GetString(0x10).Trim();
            BankLocator = GetString(0x11).Trim();
            ManufacturerName = GetString(0x17).Trim();
            SerialNumber = GetString(0x18).Trim();
            PartNumber = GetString(0x1A).Trim();
            Speed = GetWord(0x15);
            Size = GetWord(0x0C);

            if (GetWord(0x1C) > 0)
                Size += GetWord(0x1C);
        }

        /// <summary>
        /// Gets the string number of the string that identifies the physically labeled bank where the memory device is located.
        /// </summary>
        public string BankLocator { get; }

        /// <summary>
        /// Gets the string number of the string that identifies the physically-labeled socket or board position where the memory device is located.
        /// </summary>
        public string DeviceLocator { get; }

        /// <summary>
        /// Gets the string number for the manufacturer of this memory device.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the string number for the part number of this memory device.
        /// </summary>
        public string PartNumber { get; }

        /// <summary>
        /// Gets the string number for the serial number of this memory device.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// Gets the size of the memory device. If the value is 0, no memory device is installed in the socket.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the the value that identifies the maximum capable speed of the device, in mega transfers per second (MT/s).
        /// </summary>
        public int Speed { get; }
    }

    /// <summary>
    /// Reads and processes information encoded in an SMBIOS table.
    /// </summary>
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
                List<MemoryDevice> memoryDeviceList = new();
                List<ProcessorCache> processorCacheList = new();

                string[] tables = FirmwareTable.EnumerateTables(Kernel32.Provider.RSMB);
                if (tables is { Length: >0 })
                {
                    _raw = FirmwareTable.GetTable(Kernel32.Provider.RSMB, tables[0]);
                    if (_raw == null || _raw.Length == 0)
                        return;


                    byte majorVersion = _raw[1];
                    byte minorVersion = _raw[2];

                    if (majorVersion > 0 || minorVersion > 0)
                        _version = new Version(majorVersion, minorVersion);

                    if (_raw is { Length: > 0 })
                    {
                        int offset = 8;
                        byte type = _raw[offset];

                        while (offset + 4 < _raw.Length && type != 127)
                        {
                            type = _raw[offset];
                            int length = _raw[offset + 1];

                            if (offset + length > _raw.Length)
                                break;


                            byte[] data = new byte[length];
                            Array.Copy(_raw, offset, data, 0, length);
                            offset += length;

                            List<string> strings = new();
                            if (offset < _raw.Length && _raw[offset] == 0)
                                offset++;

                            while (offset < _raw.Length && _raw[offset] != 0)
                            {
                                StringBuilder stringBuilder = new();

                                while (offset < _raw.Length && _raw[offset] != 0)
                                {
                                    stringBuilder.Append((char)_raw[offset]);
                                    offset++;
                                }

                                offset++;

                                strings.Add(stringBuilder.ToString());
                            }

                            offset++;
                            switch (type)
                            {
                                case 0x00:
                                {
                                    Bios = new BiosInformation(data, strings);
                                    break;
                                }
                                case 0x01:
                                {
                                    System = new SystemInformation(data, strings);
                                    break;
                                }
                                case 0x02:
                                {
                                    Board = new BaseBoardInformation(data, strings);
                                    break;
                                }
                                case 0x03:
                                {
                                    Chassis = new ChassisInformation(data, strings);
                                    break;
                                }
                                case 0x04:
                                {
                                    Processor = new ProcessorInformation(data, strings);
                                    break;
                                }
                                case 0x07:
                                {
                                    ProcessorCache processorCache = new(data, strings);
                                    processorCacheList.Add(processorCache);
                                    break;
                                }
                                case 0x11:
                                {
                                    MemoryDevice memoryDevice = new(data, strings);
                                    memoryDeviceList.Add(memoryDevice);
                                    break;
                                }
                            }
                        }
                    }
                }

                MemoryDevices = memoryDeviceList.ToArray();
                ProcessorCaches = processorCacheList.ToArray();
            }
        }

        /// <summary>
        /// Gets <inheritdoc cref="BiosInformation" />
        /// </summary>
        public BiosInformation Bios { get; }

        /// <summary>
        /// Gets <inheritdoc cref="BaseBoardInformation" />
        /// </summary>
        public BaseBoardInformation Board { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ChassisInformation" />
        /// </summary>
        public ChassisInformation Chassis { get; }

        /// <summary>
        /// Gets <inheritdoc cref="MemoryDevice" />
        /// </summary>
        public MemoryDevice[] MemoryDevices { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ProcessorInformation" />
        /// </summary>
        public ProcessorInformation Processor { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ProcessorCache" />
        /// </summary>
        public ProcessorCache[] ProcessorCaches { get; }

        /// <summary>
        /// Gets <inheritdoc cref="SystemInformation" />
        /// </summary>
        public SystemInformation System { get; }

        private static string ReadSysFs(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using StreamReader reader = new(path);

                    return reader.ReadLine();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Report containing most of the information that could be read from the SMBIOS table.
        /// </summary>
        /// <returns>A formatted text string with computer information and the entire SMBIOS table.</returns>
        public string GetReport()
        {
            StringBuilder r = new();

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

            for (int i = 0; i < ProcessorCaches.Length; i++)
            {
                r.Append("Cache [" + ProcessorCaches[i].Designation + "] Size: ");
                r.AppendLine(ProcessorCaches[i].Size.ToString());
                r.Append("Cache [" + ProcessorCaches[i].Designation + "] Associativity: ");
                r.AppendLine(ProcessorCaches[i].Associativity.ToString().Replace("_", string.Empty));
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
                r.AppendLine("Memory Device [" + i + "] Size: ");
                r.Append(MemoryDevices[i].Size);
                r.AppendLine(" MB");
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
