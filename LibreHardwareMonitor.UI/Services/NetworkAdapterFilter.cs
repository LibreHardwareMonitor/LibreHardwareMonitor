using System;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI.Services;

internal static class NetworkAdapterFilter
{
    private static readonly string[] VirtualAdapterKeywords =
    {
        "Virtual", "VPN", "Tunnel", "Loopback", "vEthernet", "VMware",
        "Hyper-V", "WAN Miniport", "Bluetooth", "Wi-Fi Direct",
        "Teredo", "ISATAP", "6to4", "vSwitch", "VMSwitch",
        "Pseudo", "Microsoft Kernel Debug"
    };

    public static bool IsVirtualAdapter(IHardware hw)
    {
        if (hw.HardwareType != HardwareType.Network)
            return false;

        string name = hw.Name;
        foreach (string keyword in VirtualAdapterKeywords)
        {
            if (name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static bool IsPhysicalAdapter(IHardware hw)
    {
        return hw.HardwareType == HardwareType.Network && !IsVirtualAdapter(hw);
    }
}
