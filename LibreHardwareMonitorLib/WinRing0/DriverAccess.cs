using LibreHardwareMonitor.Hardware;
using Driver = WinRing0Driver.Driver.DriverManager;

namespace LibreHardwareMonitor.WinRing0
{
    internal static class DriverAccess
    {
        #region Properties

        public static bool IsOpen
        {
            get { return Driver.Ols.IsOpen; }
        }

        #endregion

        #region Public

        public static void WriteIoPortByte(ushort port, byte value)
        {
            Driver.Ols.WriteIoPortByte(port, value);
        }

        public static byte ReadIoPortByte(ushort port)
        {
            return Driver.Ols.ReadIoPortByte(port);
        }

        public static uint FindPciDeviceById(ushort vendorId, ushort deviceId, byte index)
        {
            return Driver.Ols.FindPciDeviceById(vendorId, deviceId, index);
        }

        public static ushort ReadPciConfigWord(uint pciAddress, byte regAddress)
        {
            return Driver.Ols.ReadPciConfigWord(pciAddress, regAddress);
        }

        public static bool ReadMsr(uint index, out uint eax, out uint edx)
        {
            return OLS.ReadMsr(index, out eax, out edx);
        }

        public static bool ReadMsr(uint index, out ulong edxeax)
        {
            return OLS.ReadMsr(index, out edxeax);
        }

        public static bool ReadMsr(uint index, out uint eax, out uint edx, GroupAffinity affinity)
        {
            return OLS.ReadMsr(index, out eax, out edx, affinity);
        }

        public static bool WriteMsr(uint index, uint eax, uint edx)
        {
            return OLS.WriteMsr(index, eax, edx);
        }

        public static byte ReadIoPort(uint port)
        {
            return OLS.ReadIoPort(port);
        }

        public static void WriteIoPort(uint port, byte value)
        {
            OLS.WriteIoPort(port, value);
        }

        public static uint GetPciAddress(byte bus, byte device, byte function)
        {
            return OLS.GetPciAddress(bus, device, function);
        }

        public static bool ReadPciConfig(uint pciAddress, uint regAddress, out uint value)
        {
            return OLS.ReadPciConfig(pciAddress, regAddress, out value);
        }

        public static bool WritePciConfig(uint pciAddress, uint regAddress, uint value)
        {
            return OLS.WritePciConfig(pciAddress, regAddress, value);
        }

        public static uint PciGetBus(uint address)
        {
            return Driver.Ols.PciGetBus(address);
        }

        public static uint PciGetDev(uint address)
        {
            return Driver.Ols.PciGetDev(address);
        }

        public static uint PciGetFunc(uint address)
        {
            return Driver.Ols.PciGetFunc(address);
        }

        #endregion
    }
}
