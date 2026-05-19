using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Hardware.Rtss;

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
