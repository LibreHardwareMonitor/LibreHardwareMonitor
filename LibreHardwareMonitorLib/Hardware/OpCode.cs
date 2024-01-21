// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware;

internal static class OpCode
{
    public static CpuidDelegate CpuId;
    public static RdtscDelegate Rdtsc;

    private static IntPtr _codeBuffer;
    private static ulong _size;

    // void __stdcall cpuidex(unsigned int index, unsigned int ecxValue,
    //   unsigned int* eax, unsigned int* ebx, unsigned int* ecx,
    //   unsigned int* edx)
    // {
    //   int info[4];
    //   __cpuidex(info, index, ecxValue);
    //   *eax = info[0];
    //   *ebx = info[1];
    //   *ecx = info[2];
    //   *edx = info[3];
    // }

    private static readonly byte[] CpuId32 =
    {
        0x55, // push ebp
        0x8B,
        0xEC, // mov ebp, esp
        0x83,
        0xEC,
        0x10, // sub esp, 10h
        0x8B,
        0x45,
        0x08, // mov eax, dword ptr [ebp+8]
        0x8B,
        0x4D,
        0x0C, // mov ecx, dword ptr [ebp+0Ch]
        0x53, // push ebx
        0x0F,
        0xA2, // cpuid
        0x56, // push esi
        0x8D,
        0x75,
        0xF0, // lea esi, [info]
        0x89,
        0x06, // mov dword ptr [esi],eax
        0x8B,
        0x45,
        0x10, // mov eax, dword ptr [eax]
        0x89,
        0x5E,
        0x04, // mov dword ptr [esi+4], ebx
        0x89,
        0x4E,
        0x08, // mov dword ptr [esi+8], ecx
        0x89,
        0x56,
        0x0C, // mov dword ptr [esi+0Ch], edx
        0x8B,
        0x4D,
        0xF0, // mov ecx, dword ptr [info]
        0x89,
        0x08, // mov dword ptr [eax], ecx
        0x8B,
        0x45,
        0x14, // mov eax, dword ptr [ebx]
        0x8B,
        0x4D,
        0xF4, // mov ecx, dword ptr [ebp-0Ch]
        0x89,
        0x08, // mov dword ptr [eax], ecx
        0x8B,
        0x45,
        0x18, // mov eax, dword ptr [ecx]
        0x8B,
        0x4D,
        0xF8, // mov ecx, dword ptr [ebp-8]
        0x89,
        0x08, // mov dword ptr [eax], ecx
        0x8B,
        0x45,
        0x1C, // mov eax, dword ptr [edx]
        0x8B,
        0x4D,
        0xFC, // mov ecx, dword ptr [ebp-4]
        0x5E, // pop esi
        0x89,
        0x08, // mov dword ptr [eax], ecx
        0x5B, // pop ebx
        0xC9, // leave
        0xC2,
        0x18,
        0x00 // ret 18h
    };

    private static readonly byte[] CpuId64Linux =
    {
        0x49,
        0x89,
        0xD2, // mov r10, rdx
        0x49,
        0x89,
        0xCB, // mov r11, rcx
        0x53, // push rbx
        0x89,
        0xF8, // mov eax, edi
        0x89,
        0xF1, // mov ecx, esi
        0x0F,
        0xA2, // cpuid
        0x41,
        0x89,
        0x02, // mov dword ptr [r10], eax
        0x41,
        0x89,
        0x1B, // mov dword ptr [r11], ebx
        0x41,
        0x89,
        0x08, // mov dword ptr [r8], ecx
        0x41,
        0x89,
        0x11, // mov dword ptr [r9], edx
        0x5B, // pop rbx
        0xC3 // ret
    };

    private static readonly byte[] CpuId64Windows =
    {
        0x48,
        0x89,
        0x5C,
        0x24,
        0x08, // mov qword ptr [rsp+8], rbx
        0x8B,
        0xC1, // mov eax, ecx
        0x8B,
        0xCA, // mov ecx, edx
        0x0F,
        0xA2, // cpuid
        0x41,
        0x89,
        0x00, // mov dword ptr [r8], eax
        0x48,
        0x8B,
        0x44,
        0x24,
        0x28, // mov rax, qword ptr [rsp+28h]
        0x41,
        0x89,
        0x19, // mov dword ptr [r9], ebx
        0x48,
        0x8B,
        0x5C,
        0x24,
        0x08, // mov rbx, qword ptr [rsp+8]
        0x89,
        0x08, // mov dword ptr [rax], ecx
        0x48,
        0x8B,
        0x44,
        0x24,
        0x30, // mov rax, qword ptr [rsp+30h]
        0x89,
        0x10, // mov dword ptr [rax], edx
        0xC3 // ret
    };

    // unsigned __int64 __stdcall rdtsc() {
    //   return __rdtsc();
    // }

    private static readonly byte[] Rdtsc32 =
    {
        0x0F,
        0x31, // rdtsc
        0xC3 // ret
    };

    private static readonly byte[] Rdtsc64 =
    {
        0x0F,
        0x31, // rdtsc
        0x48,
        0xC1,
        0xE2,
        0x20, // shl rdx, 20h
        0x48,
        0x0B,
        0xC2, // or rax, rdx
        0xC3 // ret
    };

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool CpuidDelegate(uint index, uint ecxValue, out uint eax, out uint ebx, out uint ecx, out uint edx);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate ulong RdtscDelegate();

    public static void Open()
    {
        byte[] rdTscCode;
        byte[] cpuidCode;
        if (IntPtr.Size == 4)
        {
            rdTscCode = Rdtsc32;
            cpuidCode = CpuId32;
        }
        else
        {
            rdTscCode = Rdtsc64;
            cpuidCode = Software.OperatingSystem.IsUnix ? CpuId64Linux : CpuId64Windows;
        }

        _size = (ulong)(rdTscCode.Length + cpuidCode.Length);

        if (Software.OperatingSystem.IsUnix)
        {
#if NETFRAMEWORK
            Assembly assembly = Assembly.Load("Mono.Posix, Version=2.0.0.0, Culture=neutral, " + "PublicKeyToken=0738eb9f132ed756");
#else
                Assembly assembly = Assembly.Load("Mono.Posix.NETStandard, Version=1.0.0.0, Culture=neutral");
#endif

            Type sysCall = assembly.GetType("Mono.Unix.Native.Syscall");
            MethodInfo mmap = sysCall.GetMethod("mmap");

            Type mmapProts = assembly.GetType("Mono.Unix.Native.MmapProts");
            object mmapProtsParam = Enum.ToObject(mmapProts,
                                                  (int)mmapProts.GetField("PROT_READ").GetValue(null) |
                                                  (int)mmapProts.GetField("PROT_WRITE").GetValue(null) |
                                                  (int)mmapProts.GetField("PROT_EXEC").GetValue(null));

            Type mmapFlags = assembly.GetType("Mono.Unix.Native.MmapFlags");
            object mmapFlagsParam = Enum.ToObject(mmapFlags,
                                                  (int)mmapFlags.GetField("MAP_ANONYMOUS").GetValue(null) |
                                                  (int)mmapFlags.GetField("MAP_PRIVATE").GetValue(null));

            if (mmap != null)
                _codeBuffer = (IntPtr)mmap.Invoke(null, new[] { IntPtr.Zero, _size, mmapProtsParam, mmapFlagsParam, -1, 0 });
        }
        else
        {
            _codeBuffer = Interop.Kernel32.VirtualAlloc(IntPtr.Zero,
                                                        (UIntPtr)_size,
                                                        Interop.Kernel32.MEM.MEM_COMMIT | Interop.Kernel32.MEM.MEM_RESERVE,
                                                        Interop.Kernel32.PAGE.PAGE_EXECUTE_READWRITE);
        }

        Marshal.Copy(rdTscCode, 0, _codeBuffer, rdTscCode.Length);
        Rdtsc = Marshal.GetDelegateForFunctionPointer(_codeBuffer, typeof(RdtscDelegate)) as RdtscDelegate;
        IntPtr cpuidAddress = (IntPtr)((long)_codeBuffer + rdTscCode.Length);
        Marshal.Copy(cpuidCode, 0, cpuidAddress, cpuidCode.Length);
        CpuId = Marshal.GetDelegateForFunctionPointer(cpuidAddress, typeof(CpuidDelegate)) as CpuidDelegate;
    }

    public static void Close()
    {
        Rdtsc = null;
        CpuId = null;

        if (Software.OperatingSystem.IsUnix)
        {
#if NETFRAMEWORK
            Assembly assembly = Assembly.Load("Mono.Posix, Version=2.0.0.0, Culture=neutral, " + "PublicKeyToken=0738eb9f132ed756");
#else
                Assembly assembly = Assembly.Load("Mono.Posix.NETStandard, Version=1.0.0.0, Culture=neutral");
#endif

            Type sysCall = assembly.GetType("Mono.Unix.Native.Syscall");
            MethodInfo method = sysCall.GetMethod("munmap");
            method?.Invoke(null, new object[] { _codeBuffer, _size });
        }
        else
        {
            Interop.Kernel32.VirtualFree(_codeBuffer, UIntPtr.Zero, Interop.Kernel32.MEM.MEM_RELEASE);
        }
    }

    public static bool CpuIdTx(uint index, uint ecxValue, out uint eax, out uint ebx, out uint ecx, out uint edx, GroupAffinity affinity)
    {
        GroupAffinity previousAffinity = ThreadAffinity.Set(affinity);
        if (previousAffinity == GroupAffinity.Undefined)
        {
            eax = ebx = ecx = edx = 0;
            return false;
        }

        CpuId(index, ecxValue, out eax, out ebx, out ecx, out edx);
        ThreadAffinity.Set(previousAffinity);
        return true;
    }
}
