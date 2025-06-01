using LibreHardwareMonitor.Hardware;

using Driver = WinRing0Driver.Driver.DriverManager;

namespace LibreHardwareMonitor.WinRing0
{
    internal static class OLS
    {
        #region Public Static

        public static bool ReadMsr(uint index, out uint eax, out uint edx)
        {
            eax = 0;
            edx = 0;

            var result = Driver.Ols.Rdmsr(index, ref eax, ref edx);

            return result != 0;
        }

        public static bool ReadMsr(uint index, out ulong edxeax)
        {
            uint eax = 0;
            uint edx = 0;

            var result = Driver.Ols.Rdmsr(index, ref eax, ref edx);

            edxeax = edx << 32;
            edxeax = edxeax | eax;

            return result != 0;
        }

        public static bool ReadMsr(uint index, out uint eax, out uint edx, GroupAffinity affinity)
        {
            GroupAffinity previousAffinity = ThreadAffinity.Set(affinity);
            bool result = ReadMsr(index, out eax, out edx);
            ThreadAffinity.Set(previousAffinity);
            return result;
        }

        public static bool WriteMsr(uint index, uint eax, uint edx)
        {
            if (!Driver.Ols.IsOpen)
            {
                return false;
            }

            var result = Driver.Ols.Wrmsr(index, eax, edx);

            return result != 0;
        }

        public static byte ReadIoPort(uint port)
        {
            if (!Driver.Ols.IsOpen)
            {
                return 0;
            }

            return Driver.Ols.ReadIoPortByte((ushort)port);
        }

        public static void WriteIoPort(uint port, byte value)
        {
            if (!Driver.Ols.IsOpen)
            {
                return;
            }

            Driver.Ols.WriteIoPortByte((ushort)port, value);
        }

        public static uint GetPciAddress(byte bus, byte device, byte function)
        {
            return (uint)(((bus & 0xFF) << 8) | ((device & 0x1F) << 3) | (function & 7));
        }

        public static bool ReadPciConfig(uint pciAddress, uint regAddress, out uint value)
        {
            if (!Driver.Ols.IsOpen || (regAddress & 3) != 0)
            {
                value = 0;
                return false;
            }

            value = Driver.Ols.ReadPciConfigDword(pciAddress, (byte)regAddress);

            return true;
        }

        public static bool WritePciConfig(uint pciAddress, uint regAddress, uint value)
        {
            if (!Driver.Ols.IsOpen || (regAddress & 3) != 0)
            {
                return false;
            }

            Driver.Ols.WritePciConfigDword(pciAddress, (byte)regAddress, value);

            return true;
        }

        #endregion
    }
}
