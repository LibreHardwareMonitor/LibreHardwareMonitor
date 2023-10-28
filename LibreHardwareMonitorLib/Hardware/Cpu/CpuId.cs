// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Cpu;

public enum Vendor
{
    Unknown,
    Intel,
    AMD
}

public class CpuId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CpuId" /> class.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <param name="thread">The thread.</param>
    /// <param name="affinity">The affinity.</param>
    private CpuId(int group, int thread, GroupAffinity affinity)
    {
        Thread = thread;
        Group = group;
        Affinity = affinity;

        uint threadMaskWith;
        uint coreMaskWith;
        uint maxCpuidExt;

        if (thread >= 64)
            throw new ArgumentOutOfRangeException(nameof(thread));

        uint maxCpuid;
        if (OpCode.CpuId(CPUID_0, 0, out uint eax, out uint ebx, out uint ecx, out uint edx))
        {
            if (eax > 0)
                maxCpuid = eax;
            else
                return;

            StringBuilder vendorBuilder = new();
            AppendRegister(vendorBuilder, ebx);
            AppendRegister(vendorBuilder, edx);
            AppendRegister(vendorBuilder, ecx);

            Vendor = vendorBuilder.ToString() switch
            {
                "GenuineIntel" => Vendor.Intel,
                "AuthenticAMD" => Vendor.AMD,
                _ => Vendor.Unknown
            };

            if (OpCode.CpuId(CPUID_EXT, 0, out eax, out _, out _, out _))
            {
                if (eax > CPUID_EXT)
                    maxCpuidExt = eax - CPUID_EXT;
                else
                    return;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(thread));
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(thread));
        }

        maxCpuid = Math.Min(maxCpuid, 1024);
        maxCpuidExt = Math.Min(maxCpuidExt, 1024);

        Data = new uint[maxCpuid + 1, 4];
        for (uint i = 0; i < maxCpuid + 1; i++)
        {
            OpCode.CpuId(CPUID_0 + i, 0, out Data[i, 0], out Data[i, 1], out Data[i, 2], out Data[i, 3]);
        }

        ExtData = new uint[maxCpuidExt + 1, 4];
        for (uint i = 0; i < maxCpuidExt + 1; i++)
        {
            OpCode.CpuId(CPUID_EXT + i, 0, out ExtData[i, 0], out ExtData[i, 1], out ExtData[i, 2], out ExtData[i, 3]);
        }

        StringBuilder nameBuilder = new();
        for (uint i = 2; i <= 4; i++)
        {
            if (OpCode.CpuId(CPUID_EXT + i, 0, out eax, out ebx, out ecx, out edx))
            {
                AppendRegister(nameBuilder, eax);
                AppendRegister(nameBuilder, ebx);
                AppendRegister(nameBuilder, ecx);
                AppendRegister(nameBuilder, edx);
            }
        }

        nameBuilder.Replace('\0', ' ');
        BrandString = nameBuilder.ToString().Trim();
        nameBuilder.Replace("(R)", string.Empty);
        nameBuilder.Replace("(TM)", string.Empty);
        nameBuilder.Replace("(tm)", string.Empty);
        nameBuilder.Replace("CPU", string.Empty);
        nameBuilder.Replace("Dual-Core Processor", string.Empty);
        nameBuilder.Replace("Triple-Core Processor", string.Empty);
        nameBuilder.Replace("Quad-Core Processor", string.Empty);
        nameBuilder.Replace("Six-Core Processor", string.Empty);
        nameBuilder.Replace("Eight-Core Processor", string.Empty);
        nameBuilder.Replace("6-Core Processor", string.Empty);
        nameBuilder.Replace("8-Core Processor", string.Empty);
        nameBuilder.Replace("12-Core Processor", string.Empty);
        nameBuilder.Replace("16-Core Processor", string.Empty);
        nameBuilder.Replace("24-Core Processor", string.Empty);
        nameBuilder.Replace("32-Core Processor", string.Empty);
        nameBuilder.Replace("64-Core Processor", string.Empty);

        for (int i = 0; i < 10; i++)
            nameBuilder.Replace("  ", " ");

        Name = nameBuilder.ToString();
        if (Name.Contains("@"))
            Name = Name.Remove(Name.LastIndexOf('@'));

        Name = Name.Trim();
        Family = ((Data[1, 0] & 0x0FF00000) >> 20) + ((Data[1, 0] & 0x0F00) >> 8);
        Model = ((Data[1, 0] & 0x0F0000) >> 12) + ((Data[1, 0] & 0xF0) >> 4);
        Stepping = Data[1, 0] & 0x0F;
        ApicId = (Data[1, 1] >> 24) & 0xFF;
        PkgType = (ExtData[1, 1] >> 28) & 0xFF;

        switch (Vendor)
        {
            case Vendor.Intel:
                uint maxCoreAndThreadIdPerPackage = (Data[1, 1] >> 16) & 0xFF;
                uint maxCoreIdPerPackage;
                if (maxCpuid >= 4)
                    maxCoreIdPerPackage = ((Data[4, 0] >> 26) & 0x3F) + 1;
                else
                    maxCoreIdPerPackage = 1;

                threadMaskWith = NextLog2(maxCoreAndThreadIdPerPackage / maxCoreIdPerPackage);
                coreMaskWith = NextLog2(maxCoreIdPerPackage);
                break;
            case Vendor.AMD:
                uint corePerPackage;
                if (maxCpuidExt >= 8)
                    corePerPackage = (ExtData[8, 2] & 0xFF) + 1;
                else
                    corePerPackage = 1;

                threadMaskWith = 0;
                coreMaskWith = NextLog2(corePerPackage);

                if (Family is 0x17 or 0x19)
                {
                    // ApicIdCoreIdSize: APIC ID size.
                    // cores per DIE
                    // we need this for Ryzen 5 (4 cores, 8 threads) ans Ryzen 6 (6 cores, 12 threads)
                    // Ryzen 5: [core0][core1][dummy][dummy][core2][core3] (Core0 EBX = 00080800, Core2 EBX = 08080800)
                    coreMaskWith = ((ExtData[8, 2] >> 12) & 0xF) switch
                    {
                        0x04 => NextLog2(16), // Ryzen
                        0x05 => NextLog2(32), // Threadripper
                        0x06 => NextLog2(64), // Epic
                        _ => coreMaskWith
                    };
                }

                break;
            default:
                threadMaskWith = 0;
                coreMaskWith = 0;
                break;
        }

        ProcessorId = ApicId >> (int)(coreMaskWith + threadMaskWith);
        CoreId = (ApicId >> (int)threadMaskWith) - (ProcessorId << (int)coreMaskWith);
        ThreadId = ApicId - (ProcessorId << (int)(coreMaskWith + threadMaskWith)) - (CoreId << (int)threadMaskWith);
    }

    public GroupAffinity Affinity { get; }

    public uint ApicId { get; }

    public string BrandString { get; } = string.Empty;

    public uint CoreId { get; }

    public uint[,] Data { get; } = new uint[0, 0];

    public uint[,] ExtData { get; } = new uint[0, 0];

    public uint Family { get; }

    public int Group { get; }

    public uint Model { get; }

    public string Name { get; } = string.Empty;

    public uint PkgType { get; }

    public uint ProcessorId { get; }

    public uint Stepping { get; }

    public int Thread { get; }

    public uint ThreadId { get; }

    public Vendor Vendor { get; } = Vendor.Unknown;

    /// <summary>
    /// Gets the specified <see cref="CpuId" />.
    /// </summary>
    /// <param name="group">The group.</param>
    /// <param name="thread">The thread.</param>
    /// <returns><see cref="CpuId" />.</returns>
    public static CpuId Get(int group, int thread)
    {
        if (thread >= 64)
            return null;

        var affinity = GroupAffinity.Single((ushort)group, thread);

        GroupAffinity previousAffinity = ThreadAffinity.Set(affinity);
        if (previousAffinity == GroupAffinity.Undefined)
            return null;

        try
        {
            return new CpuId(group, thread, affinity);
        }
        finally
        {
            ThreadAffinity.Set(previousAffinity);
        }
    }

    private static void AppendRegister(StringBuilder b, uint value)
    {
        b.Append((char)(value & 0xff));
        b.Append((char)((value >> 8) & 0xff));
        b.Append((char)((value >> 16) & 0xff));
        b.Append((char)((value >> 24) & 0xff));
    }

    private static uint NextLog2(long x)
    {
        if (x <= 0)
            return 0;

        x--;
        uint count = 0;
        while (x > 0)
        {
            x >>= 1;
            count++;
        }

        return count;
    }

    // ReSharper disable InconsistentNaming
    public const uint CPUID_0 = 0;
    public const uint CPUID_EXT = 0x80000000;
    // ReSharper restore InconsistentNaming
}
