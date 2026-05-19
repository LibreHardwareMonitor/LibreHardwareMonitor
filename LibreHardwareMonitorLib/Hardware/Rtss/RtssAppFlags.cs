using System;

namespace LibreHardwareMonitor.Hardware.Rtss;

[Flags]
public enum RtssAppFlags
{
    None = 0,
    OpenGL = 0x00010000,
    DirectDraw = 0x00000010,
    Direct3D8 = 0x00000100,
    Direct3D9 = 0x00001000,
    Direct3D9Ex = 0x00002000,
    Direct3D10 = 0x00100000,
    Direct3D11 = 0x01000000,
    ProfileUpdateRequested = 0x10000000,
    MASK = DirectDraw | Direct3D8 | Direct3D9 | Direct3D9Ex | OpenGL | Direct3D10 | Direct3D11,
}
