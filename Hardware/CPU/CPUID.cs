/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2014 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Text;

namespace OpenHardwareMonitor.Hardware.CPU {
    public enum Vendor {
    Unknown,
    Intel,
    AMD,
  }

    public class CPUID {

    private readonly int thread;

    private readonly Vendor vendor = Vendor.Unknown;

    private readonly string cpuBrandString = "";
    private readonly string name = "";

    private readonly uint[,] cpuidData = new uint[0, 0];
    private readonly uint[,] cpuidExtData = new uint[0, 0];

    private readonly uint family;
    private readonly uint model;
    private readonly uint stepping;

    private readonly uint apicId;

    private readonly uint threadMaskWith;
    private readonly uint coreMaskWith;

    private readonly uint processorId;
    private readonly uint coreId;
    private readonly uint threadId;

    public const uint CPUID_0 = 0;
    public const uint CPUID_EXT = 0x80000000;

    private static void AppendRegister(StringBuilder b, uint value) {
      b.Append((char)((value) & 0xff));
      b.Append((char)((value >> 8) & 0xff));
      b.Append((char)((value >> 16) & 0xff));
      b.Append((char)((value >> 24) & 0xff));
    }

    private static uint NextLog2(long x) {
      if (x <= 0)
        return 0;

      x--;
      uint count = 0;
      while (x > 0) {
        x >>= 1;
        count++;
      }

      return count;
    }

    public CPUID(int thread) {
      this.thread = thread;

      uint maxCpuid = 0;
      uint maxCpuidExt = 0;

      uint eax, ebx, ecx, edx;

      if (thread >= 64)
        throw new ArgumentOutOfRangeException("thread");
      ulong mask = 1UL << thread;

      if (Opcode.CpuidTx(CPUID_0, 0,
          out eax, out ebx, out ecx, out edx, mask)) {
        if (eax > 0)
          maxCpuid = eax;
        else
          return;

        StringBuilder vendorBuilder = new StringBuilder();
        AppendRegister(vendorBuilder, ebx);
        AppendRegister(vendorBuilder, edx);
        AppendRegister(vendorBuilder, ecx);
        string cpuVendor = vendorBuilder.ToString();
        switch (cpuVendor) {
          case "GenuineIntel":
            vendor = Vendor.Intel;
            break;
          case "AuthenticAMD":
            vendor = Vendor.AMD;
            break;
          default:
            vendor = Vendor.Unknown;
            break;
        }
        eax = ebx = ecx = edx = 0;
        if (Opcode.CpuidTx(CPUID_EXT, 0,
          out eax, out ebx, out ecx, out edx, mask)) {
          if (eax > CPUID_EXT)
            maxCpuidExt = eax - CPUID_EXT;
          else
            return;
        } else {
          throw new ArgumentOutOfRangeException("thread");
        }
      } else {
        throw new ArgumentOutOfRangeException("thread");
      }

      maxCpuid = Math.Min(maxCpuid, 1024);
      maxCpuidExt = Math.Min(maxCpuidExt, 1024);   

      cpuidData = new uint[maxCpuid + 1, 4];
      for (uint i = 0; i < (maxCpuid + 1); i++)
        Opcode.CpuidTx(CPUID_0 + i, 0, 
          out cpuidData[i, 0], out cpuidData[i, 1],
          out cpuidData[i, 2], out cpuidData[i, 3], mask);

      cpuidExtData = new uint[maxCpuidExt + 1, 4];
      for (uint i = 0; i < (maxCpuidExt + 1); i++)
        Opcode.CpuidTx(CPUID_EXT + i, 0, 
          out cpuidExtData[i, 0], out cpuidExtData[i, 1], 
          out cpuidExtData[i, 2], out cpuidExtData[i, 3], mask);

      StringBuilder nameBuilder = new StringBuilder();
      for (uint i = 2; i <= 4; i++) {
        if (Opcode.CpuidTx(CPUID_EXT + i, 0, 
          out eax, out ebx, out ecx, out edx, mask)) 
        {
          AppendRegister(nameBuilder, eax);
          AppendRegister(nameBuilder, ebx);
          AppendRegister(nameBuilder, ecx);
          AppendRegister(nameBuilder, edx);
        }
      }
      nameBuilder.Replace('\0', ' ');
      cpuBrandString = nameBuilder.ToString().Trim();
      nameBuilder.Replace("(R)", " ");
      nameBuilder.Replace("(TM)", " ");
      nameBuilder.Replace("(tm)", "");
      nameBuilder.Replace("CPU", "");
      nameBuilder.Replace("Quad-Core Processor", "");
      nameBuilder.Replace("Six-Core Processor", "");
      nameBuilder.Replace("Eight-Core Processor", "");
      for (int i = 0; i < 10; i++) nameBuilder.Replace("  ", " ");
      name = nameBuilder.ToString();
      if (name.Contains("@"))
        name = name.Remove(name.LastIndexOf('@'));
      name = name.Trim();      

      this.family = ((cpuidData[1, 0] & 0x0FF00000) >> 20) +
        ((cpuidData[1, 0] & 0x0F00) >> 8);
      this.model = ((cpuidData[1, 0] & 0x0F0000) >> 12) +
        ((cpuidData[1, 0] & 0xF0) >> 4);
      this.stepping = (cpuidData[1, 0] & 0x0F);

      this.apicId = (cpuidData[1, 1] >> 24) & 0xFF;

      switch (vendor) {
        case Vendor.Intel:
          uint maxCoreAndThreadIdPerPackage = (cpuidData[1, 1] >> 16) & 0xFF;
          uint maxCoreIdPerPackage;
          if (maxCpuid >= 4)
            maxCoreIdPerPackage = ((cpuidData[4, 0] >> 26) & 0x3F) + 1;
          else
            maxCoreIdPerPackage = 1;
          threadMaskWith = 
            NextLog2(maxCoreAndThreadIdPerPackage / maxCoreIdPerPackage);
          coreMaskWith = NextLog2(maxCoreIdPerPackage);
          break;
        case Vendor.AMD:
          uint corePerPackage;
          if (maxCpuidExt >= 8)
            corePerPackage = (cpuidExtData[8, 2] & 0xFF) + 1;
          else
            corePerPackage = 1;
          threadMaskWith = 0;
          coreMaskWith = NextLog2(corePerPackage);
          
          if (this.family == 0x17)
          {
            // ApicIdCoreIdSize: APIC ID size. 
            // cores per DIE 
            // we need this for Ryzen 5 (4 cores, 8 threads) ans Ryzen 6 (6 cores, 12 threads) 
            // Ryzen 5: [core0][core1][dummy][dummy][core2][core3] (Core0 EBX = 00080800, Core2 EBX = 08080800) 
            uint max_cores_per_die = (cpuidExtData[8, 2] >> 12) & 0xF;
            switch (max_cores_per_die)
            {
              case 0x04: // Ryzen 
                coreMaskWith = NextLog2(16);
                break;
              case 0x05:// Threadripper 
                coreMaskWith = NextLog2(32);
                break;
              case 0x06:// Epic 
                coreMaskWith = NextLog2(64);
                break;
            }
          }
          break;
        default:
          threadMaskWith = 0;
          coreMaskWith = 0;
          break;
      }

      processorId = (apicId >> (int)(coreMaskWith + threadMaskWith));
      coreId = ((apicId >> (int)(threadMaskWith)) 
        - (processorId << (int)(coreMaskWith)));
      threadId = apicId
        - (processorId << (int)(coreMaskWith + threadMaskWith))
        - (coreId << (int)(threadMaskWith)); 
    }

    public string Name {
      get { return name; }
    }

    public string BrandString {
      get { return cpuBrandString; }
    }

    public int Thread {
      get { return thread; }
    }

    public Vendor Vendor {
      get { return vendor; }
    }

    public uint Family {
      get { return family; }
    }

    public uint Model {
      get { return model; }
    }

    public uint Stepping {
      get { return stepping; }
    }

    public uint ApicId {
      get { return apicId; }
    }

    public uint ProcessorId {
      get { return processorId; }
    }

    public uint CoreId {
      get { return coreId; }
    }

    public uint ThreadId {
      get { return threadId; }
    }

    public uint[,] Data {
      get { return cpuidData; }
    }

    public uint[,] ExtData {
      get { return cpuidExtData; }
    }
  }
}
