using System.Net.NetworkInformation;

namespace LibreHardwareMonitor.Hardware.Network
{
    public class NetworkEX
    {
        public static bool IsAvailable(IHardware hardware)
        {
            NetworkInterface ni = (hardware as Network).NetworkInterface;
            return ni != null
                && ni.OperationalStatus == OperationalStatus.Up
                && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback;
        }

        public static NetworkInterfaceType GetNetworkInterfaceType(IHardware hardware)
        {
            return (hardware as Network).NetworkInterface.NetworkInterfaceType;
        }
    }
}
