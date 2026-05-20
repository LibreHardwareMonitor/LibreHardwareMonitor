using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Rtss;

[Flags]
internal enum RtssAppFlags
{
    OpenGL = 0x00000001,
    DirectDraw = 0x00000002,
    Direct3D8 = 0x00000003,
    Direct3D9 = 0x00000004,
    Direct3D9Ex = 0x00000005,
    Direct3D10 = 0x00000006,
    Direct3D11 = 0x00000007,
    Direct3D12 = 0x00000008,
    Direct3D12Afr = 0x00000009,
    Vulkan = 0x0000000A,
    ApiUsageMask = 0x0000FFFF,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct RTSS_SHARED_MEMORY_HEADER
{
    public uint Signature;
    public uint Version;
    public uint AppEntrySize;
    public uint AppArrOffset;
    public uint AppArrSize;
    public uint OSDEntrySize;
    public uint OSDArrOffset;
    public uint OSDArrSize;
    public uint OSDFrame;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct RTSS_SHARED_MEMORY_APP_ENTRY
{
    public uint ProcessID;
    public fixed byte Name[256];
    public uint DummyPadding;
    public uint Flags;
    public uint Time0;
    public uint Time1;
    public uint Frames;
    public uint FrameTime;
    public uint StatFlags;
    public uint StatTime0;
    public uint StatTime1;
    public uint StatFrames;
    public uint StatCount;
    public uint StatFramerateMin;
    public uint StatFramerateAvg;
    public uint StatFramerateMax;
}

internal static class RtssHelpers
{
    public const uint RTSS_SIGNATURE = 0x52545353; // 0x52 = 'R', 0x54 = 'T', 0x53 = 'S', 0x53 = 'S'
    public const string MMF_NAME = "RTSSSharedMemoryV2";

    public static uint GenerateVersion(ushort major, ushort minor)
    {
        return ((uint)major << 16) + minor;
    }

    public static string EntryToDisplayName(RTSS_SHARED_MEMORY_APP_ENTRY entry)
    {
        string name = ExtractEntryName(entry);
        return string.IsNullOrWhiteSpace(name) ? entry.ProcessID.ToString() : Path.GetFileNameWithoutExtension(name);
    }

    public static string FlagsToDisplayName(RtssAppFlags flags)
    {
        var apiUsageFlags = flags & RtssAppFlags.ApiUsageMask;
        return apiUsageFlags switch
        {
            RtssAppFlags.OpenGL => "OpenGL",
            RtssAppFlags.DirectDraw => "DirectDraw",
            RtssAppFlags.Direct3D8 => "Direct3D 8",
            RtssAppFlags.Direct3D9 => "Direct3D 9",
            RtssAppFlags.Direct3D9Ex => "Direct3D 9",
            RtssAppFlags.Direct3D10 => "Direct3D 10",
            RtssAppFlags.Direct3D11 => "Direct3D 11",
            RtssAppFlags.Direct3D12 => "Direct3D 12",
            RtssAppFlags.Direct3D12Afr => "Direct3D 12",
            RtssAppFlags.Vulkan => "Vulkan",
            _ => null
        };
    }

    private static string ExtractEntryName(RTSS_SHARED_MEMORY_APP_ENTRY entry)
    {
        unsafe
        {
            int length = 0;
            while (length < 256 && entry.Name[length] != 0)
            {
                length++;
            }

            return Encoding.Default.GetString(entry.Name, length);
        }
    }
}
