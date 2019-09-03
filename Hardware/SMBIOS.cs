/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text;

namespace OpenHardwareMonitor.Hardware {

  public class SMBIOS {

    private readonly byte[] raw;
    private readonly Structure[] table;

    private readonly Version version;
    private readonly BIOSInformation biosInformation;
    private readonly SystemInformation systemInformation;
    private readonly BaseBoardInformation baseBoardInformation;
    private readonly ChassisInformation chassisInformation;
    private readonly ProcessorInformation processorInformation;
    private readonly MemoryDevice[] memoryDevices;

    private static string ReadSysFS(string path) {
      try {
        if (File.Exists(path)) {
          using (StreamReader reader = new StreamReader(path)) 
            return reader.ReadLine();
        } else {
          return null;
        }
      } catch {
        return null;
      }
    }
    
    public SMBIOS()
    {

      if (Software.OperatingSystem.IsLinux)
      {
        this.raw = null;
        this.table = null;
        
        string boardVendor = ReadSysFS("/sys/class/dmi/id/board_vendor");
        string boardName = ReadSysFS("/sys/class/dmi/id/board_name");        
        string boardVersion = ReadSysFS("/sys/class/dmi/id/board_version");        
        this.baseBoardInformation = new BaseBoardInformation(
          boardVendor, boardName, boardVersion, null);

        string systemVendor = ReadSysFS("/sys/class/dmi/id/sys_vendor");
        string productName = ReadSysFS("/sys/class/dmi/id/product_name");
        string productVersion = ReadSysFS("/sys/class/dmi/id/product_version");    
        this.systemInformation = new SystemInformation(systemVendor, 
          productName, productVersion, null, null);

        string biosVendor = ReadSysFS("/sys/class/dmi/id/bios_vendor");
        string biosVersion = ReadSysFS("/sys/class/dmi/id/bios_version");
        string biosDate = ReadSysFS("/sys/class/dmi/id/bios_date");
        this.biosInformation = new BIOSInformation(biosVendor, biosVersion, biosDate);

        this.memoryDevices = new MemoryDevice[0];
      } else {              
        List<Structure> structureList = new List<Structure>();
        List<MemoryDevice> memoryDeviceList = new List<MemoryDevice>();

        raw = null;
        byte majorVersion = 0;
        byte minorVersion = 0;
        try {
          ManagementObjectCollection collection;
          using (ManagementObjectSearcher searcher = 
            new ManagementObjectSearcher("root\\WMI", 
              "SELECT * FROM MSSMBios_RawSMBiosTables")) {
            collection = searcher.Get();
          }
         
          foreach (ManagementObject mo in collection) {
            raw = (byte[])mo["SMBiosData"];
            majorVersion = (byte)mo["SmbiosMajorVersion"];
            minorVersion = (byte)mo["SmbiosMinorVersion"];            
            break;
          }
        } catch { }      

        if (majorVersion > 0 || minorVersion > 0)
          version = new Version(majorVersion, minorVersion);
  
        if (raw != null && raw.Length > 0) {
          int offset = 0;
          byte type = raw[offset];
          while (offset + 4 < raw.Length && type != 127) {
  
            type = raw[offset];
            int length = raw[offset + 1];
            ushort handle = (ushort)((raw[offset + 2] << 8) | raw[offset + 3]);
  
            if (offset + length > raw.Length)
              break;
            byte[] data = new byte[length];
            Array.Copy(raw, offset, data, 0, length);
            offset += length;
  
            List<string> stringsList = new List<string>();
            if (offset < raw.Length && raw[offset] == 0)
              offset++;
  
            while (offset < raw.Length && raw[offset] != 0) {
              StringBuilder sb = new StringBuilder();
              while (offset < raw.Length && raw[offset] != 0) {
                sb.Append((char)raw[offset]); offset++;
              }
              offset++;
              stringsList.Add(sb.ToString());
            }
            offset++;
            switch (type) {
              case 0x00:
                this.biosInformation = new BIOSInformation(
                  type, handle, data, stringsList.ToArray());
                structureList.Add(this.biosInformation); break;
              case 0x01:
                this.systemInformation = new SystemInformation(
                  type, handle, data, stringsList.ToArray());
                structureList.Add(this.systemInformation); break;
              case 0x02: this.baseBoardInformation = new BaseBoardInformation(
                  type, handle, data, stringsList.ToArray());
                structureList.Add(this.baseBoardInformation); break;
              case 0x03: this.chassisInformation = new ChassisInformation(
                  type, handle, data, stringsList.ToArray()); 
                structureList.Add(chassisInformation); break;
              case 0x04: this.processorInformation = new ProcessorInformation(
                  type, handle, data, stringsList.ToArray());
                structureList.Add(this.processorInformation); break;
              case 0x11: MemoryDevice m = new MemoryDevice(
                  type, handle, data, stringsList.ToArray());
                memoryDeviceList.Add(m);
                structureList.Add(m); break;
              default: structureList.Add(new Structure(
                type, handle, data, stringsList.ToArray())); break;
            }
          }
        }

        memoryDevices = memoryDeviceList.ToArray();
        table = structureList.ToArray();
      }
    }

    public string GetReport() {
      StringBuilder r = new StringBuilder();

      if (version != null) {
        r.Append("SMBIOS Version: "); r.AppendLine(version.ToString(2));
        r.AppendLine();
      }

      if (BIOS != null) {
        r.Append("BIOS Vendor: "); r.AppendLine(BIOS.Vendor);
        r.Append("BIOS Version: "); r.AppendLine(BIOS.Version);
        if (BIOS.Date != null) {
            r.Append("BIOS Date: "); r.AppendLine(BIOS.Date.Value.ToShortDateString());
        }
        if (BIOS.Size != null) { 
            const int megabyte = 1024 * 1024;
            r.Append("BIOS Size: ");
            if (BIOS.Size > megabyte)
              r.AppendLine(BIOS.Size.Value / megabyte + " MB");
            else
              r.AppendLine(BIOS.Size.Value / 1024 + " KB");
        }
        r.AppendLine();
      }

      if (System != null) {
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

      if (Board != null) {
        r.Append("Mainboard Manufacturer: ");
        r.AppendLine(Board.ManufacturerName);
        r.Append("Mainboard Name: ");
        r.AppendLine(Board.ProductName);
        r.Append("Mainboard Version: ");
        r.AppendLine(Board.Version);
        r.Append("Mainboard Serial: ");
        r.AppendLine(Board.SerialNumber);
        r.AppendLine();
      }

      if (Chassis != null) { 
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
        if (!String.IsNullOrEmpty(Chassis.SKU)) {
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
        if (Chassis.RackHeight > 0) {
          r.Append("Chassis Rack Height: ");
          r.AppendLine(Chassis.RackHeight.ToString());
        }
        r.Append("Chassis Lock Detected: ");
        r.AppendLine(Chassis.LockDetected ? "Yes" : "No");
        r.Append("Chassis Security Status: ");
        r.AppendLine(Chassis.SecurityStatus.ToString());
        r.AppendLine();
      }

      if (Processor != null) {
        r.Append("Processor Manufacturer: ");
        r.AppendLine(Processor.ManufacturerName);
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
        r.AppendLine();
      }

      for (int i = 0; i < MemoryDevices.Length; i++) {        
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

      if (raw != null) {
        string base64 = Convert.ToBase64String(raw);
        r.AppendLine("SMBIOS Table");
        r.AppendLine();

        for (int i = 0; i < Math.Ceiling(base64.Length / 64.0); i++) {
          r.Append(" ");
          for (int j = 0; j < 0x40; j++) {
            int index = (i << 6) | j;
            if (index < base64.Length) {              
              r.Append(base64[index]);
            }
          }
          r.AppendLine();
        }
        r.AppendLine();
      }

      return r.ToString();
    }

    public BIOSInformation BIOS {
      get { return biosInformation; }
    }

    public SystemInformation System {
      get { return systemInformation; }
    }

    public BaseBoardInformation Board {
      get { return baseBoardInformation; }
    }

    public ChassisInformation Chassis { 
      get { return chassisInformation; }
    }

    public ProcessorInformation Processor {
      get { return processorInformation; }
    }

    public MemoryDevice[] MemoryDevices {
      get { return memoryDevices; }
    }

    public class Structure {
      private readonly byte type;
      private readonly ushort handle;

      private readonly byte[] data;
      private readonly string[] strings;

      protected int GetByte(int offset) {
        if (offset < data.Length && offset >= 0)
          return data[offset];
        else
          return 0;
      }

      protected int GetWord(int offset) {
        if (offset + 1 < data.Length && offset >= 0)
          return (data[offset + 1] << 8) | data[offset];
        else
          return 0;
      }

      protected string GetString(int offset) {
        if (offset < data.Length && data[offset] > 0 &&
         data[offset] <= strings.Length)
          return strings[data[offset] - 1];
        else
          return "";
      }

      public Structure(byte type, ushort handle, byte[] data, string[] strings) 
      {
        this.type = type;
        this.handle = handle;
        this.data = data;
        this.strings = strings;
      }

      public byte Type { get { return type; } }

      public ushort Handle { get { return handle; } }
    }
      
    public class BIOSInformation : Structure {

      private readonly string vendor;
      private readonly string version;
      private readonly DateTime? date;
      private readonly ulong? size;
      
      public BIOSInformation(string vendor, string version, string date = null, ulong? size = null) 
        : base (0x00, 0, null, null) 
      {
        this.vendor = vendor;
        this.version = version;
        this.date = ParseBIOSDate(date);
        this.size = size;
      }
      
      public BIOSInformation(byte type, ushort handle, byte[] data,
        string[] strings)
        : base(type, handle, data, strings) 
      {
        this.vendor = GetString(0x04);
        this.version = GetString(0x05);
        this.date = ParseBIOSDate(GetString(0x08));
        this.size = CalculateBIOSRomSize();
      }

      private ulong? CalculateBIOSRomSize()
      {
        var biosROMSize = GetByte(0x09);
        var extendedBIOSROMSize = GetWord(0x18);
        var isExtendedBIOSROMSize = biosROMSize == 0xFF && extendedBIOSROMSize != 0;
        if (!isExtendedBIOSROMSize)
            return 65536 * (ulong)(biosROMSize + 1);

        var unit = (extendedBIOSROMSize & 0xC000) >> 14;
        var extendedSize = (ulong)(extendedBIOSROMSize & ~0xC000) * 1024 * 1024;

        switch(unit) {
            case 0x00: return extendedSize; // Megabytes
            case 0x01: return extendedSize * 1024; // Gigabytes - might overflow in the future
            default:
                return null; // Other patterns not defined in DMI 3.2.0
        }
      }

      private static DateTime? ParseBIOSDate(string biosDate)
      {
        var parts = (biosDate ?? "").Split('/');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out int month) &&
            int.TryParse(parts[1], out int day) &&
            int.TryParse(parts[2], out int year)) { 
            return new DateTime(year < 100 ? 1900 + year : year, month, day);
        }

        return null;
      }
      
      public DateTime? Date { get { return date; } }

      public ulong? Size { get { return size; } }

      public string Vendor { get { return vendor; } }

      public string Version { get { return version; } }
    }

    public class SystemInformation : Structure {

      private readonly string manufacturerName;
      private readonly string productName;
      private readonly string version;
      private readonly string serialNumber;
      private readonly string family;
      private readonly SystemWakeUp wakeUp;

      public SystemInformation(string manufacturerName, string productName, 
        string version, string serialNumber, string family, SystemWakeUp wakeUp = SystemWakeUp.Unknown) 
        : base (0x01, 0, null, null) 
      {
        this.manufacturerName = manufacturerName;
        this.productName = productName;
        this.version = version;
        this.serialNumber = serialNumber;
        this.family = family;
        this.wakeUp = wakeUp;
      }

      public SystemInformation(byte type, ushort handle, byte[] data,
        string[] strings)
        : base(type, handle, data, strings) 
      {
        this.manufacturerName = GetString(0x04);
        this.productName = GetString(0x05);
        this.version = GetString(0x06);
        this.serialNumber = GetString(0x07);
        this.family = GetString(0x1A);
        this.wakeUp = (SystemWakeUp)GetByte(0x18);
      }

      public string ManufacturerName { get { return manufacturerName; } }

      public string ProductName { get { return productName; } }

      public string Version { get { return version; } }

      public string SerialNumber { get { return serialNumber; } }

      public string Family { get { return family; } }

      public SystemWakeUp WakeUp { get { return wakeUp; } }
    }

    public enum SystemWakeUp
    {
        Reserved,
        Other,
        Unknown,
        APMTimer,
        ModemRing,
        LANRemote,
        PowerSwitch,
        PCIPME,
        ACPowerRestored
    }

    public class ChassisInformation : Structure {
      public ChassisInformation(byte type, ushort handle, byte[] data,
        string[] strings)
        : base(type, handle, data, strings) {
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

      public string ManufacturerName { get; private set; }
      public string Version { get; private set; }
      public string SerialNumber { get; private set; }
      public string AssetTag { get; private set; }
      public string SKU { get; private set; }
      public int RackHeight { get; private set; }
      public int PowerCords { get; private set; }
      public ChassisType ChassisType { get; private set; }
      public ChassisStates BootUpState { get; private set; }
      public ChassisStates PowerSupplyState { get; private set; }
      public ChassisStates ThermalState { get; private set; }
      public ChassisSecurityStatus SecurityStatus { get; set; }
      public bool LockDetected { get; set; }
    }

    public enum ChassisType { 
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
      RAIDChassis,
      RackMountChassis,
      SealedCasePC,
      MultiSystemChassis,
      CompactPCI,
      AdvancedTCA,
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

    public enum ChassisStates {
      Other = 1,
      Unknown,
      Safe,
      Warning,
      Critical,
      NonRecoverable
    }

    public enum ChassisSecurityStatus {
      Other = 1,
      Unknown,
      None,
      ExternalInterfaceLockedOut,
      ExternalInterfaceEnabled
    }

    public class BaseBoardInformation : Structure {

      private readonly string manufacturerName;
      private readonly string productName;
      private readonly string version;
      private readonly string serialNumber;
      
      public BaseBoardInformation(string manufacturerName, string productName, 
        string version, string serialNumber) 
        : base(0x02, 0, null, null) 
      {
        this.manufacturerName = manufacturerName;
        this.productName = productName;
        this.version = version;
        this.serialNumber = serialNumber;
      }
      
      public BaseBoardInformation(byte type, ushort handle, byte[] data,
        string[] strings)
        : base(type, handle, data, strings) {

        this.manufacturerName = GetString(0x04).Trim();
        this.productName = GetString(0x05).Trim();
        this.version = GetString(0x06).Trim();
        this.serialNumber = GetString(0x07).Trim();
      }
      
      public string ManufacturerName { get { return manufacturerName; } }

      public string ProductName { get { return productName; } }

      public string Version { get { return version; } }

      public string SerialNumber { get { return serialNumber; } }
    }

    public class ProcessorInformation : Structure {

      public ProcessorInformation(byte type, ushort handle, byte[] data,
        string[] strings)
        : base(type, handle, data, strings) 
      {
        this.ManufacturerName = GetString(0x07).Trim();
        this.Version = GetString(0x10).Trim();
        this.CoreCount = GetByte(0x23) != 255 ? GetByte(0x23) : GetWord(0x2A);
        this.CoreEnabled = GetByte(0x24) != 255 ? GetByte(0x24) : GetWord(0x2C);
        this.ThreadCount = GetByte(0x25) != 255 ? GetByte(0x25) : GetWord(0x2E);
        this.ExternalClock = GetWord(0x12);
        var family = GetByte(0x06);
        this.Family = (ProcessorFamily) (family == 254 ? GetWord(0x28) : family); 
      }

      public string ManufacturerName { get; private set; }

      public string Version { get; private set; }

      public int CoreCount { get; private set; }

      public int CoreEnabled { get; private set; }

      public int ThreadCount { get; private set; }
     
      public int ExternalClock { get; private set; }

      public ProcessorFamily Family { get; private set; }
    }

    public enum ProcessorFamily { 
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

    public class MemoryDevice : Structure {

      private readonly string deviceLocator;
      private readonly string bankLocator;
      private readonly string manufacturerName;
      private readonly string serialNumber;
      private readonly string partNumber;
      private readonly int speed;

      public MemoryDevice(byte type, ushort handle, byte[] data,
        string[] strings)
        : base(type, handle, data, strings) 
      {
        this.deviceLocator = GetString(0x10).Trim();
        this.bankLocator = GetString(0x11).Trim();
        this.manufacturerName = GetString(0x17).Trim();
        this.serialNumber = GetString(0x18).Trim();
        this.partNumber = GetString(0x1A).Trim();
        this.speed = GetWord(0x15);
      }

      public string DeviceLocator { get { return deviceLocator; } }

      public string BankLocator { get { return bankLocator; } }

      public string ManufacturerName { get { return manufacturerName; } }

      public string SerialNumber { get { return serialNumber; } }

      public string PartNumber { get { return partNumber; } }

      public int Speed { get { return speed; } }

    }
  }
}
