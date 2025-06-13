using System.Text;
using RAMSPDToolkit.Windows.Driver;
using RAMSPDToolkit.Windows.Driver.Implementations.WinRing0;

namespace LibreHardwareMonitor.Hardware
{
    internal static class DriverAccess
    {
        #region Fields

        static OLS _OLS;
        static IDriver _OLSDriver;

        #endregion

        #region Properties

        public static bool IsOpen => _OLSDriver.IsOpen;

        #endregion

        #region Public

        public static void Open()
        {
            //No implementation for unix systems
            if (Software.OperatingSystem.IsUnix)
                return;

            _OLS = new OLS();
            _OLSDriver = _OLS;

            DriverManager.Driver = _OLS;
        }

        public static void Close()
        {
            _OLSDriver = null;

            _OLS.Dispose();
            _OLS = null;
        }

        public static string GetReport()
        {
            if (_OLS.Report.Length > 0)
            {
                StringBuilder r = new();
                r.AppendLine("Ring0");
                r.AppendLine();
                r.Append(_OLS.Report);
                r.AppendLine();
                return r.ToString();
            }

            return null;
        }

        public static byte ReadIoPortByte(ushort port)
        {
            return _OLSDriver.ReadIoPortByte(port);
        }

        public static ushort ReadIoPortWord(ushort port)
        {
            return _OLSDriver.ReadIoPortWord(port);
        }

        public static uint ReadIoPortDword(ushort port)
        {
            return _OLSDriver.ReadIoPortDword(port);
        }

        public static int ReadIoPortByteEx(ushort port, ref byte value)
        {
            return _OLSDriver.ReadIoPortByteEx(port, ref value);
        }

        public static int ReadIoPortWordEx(ushort port, ref ushort value)
        {
            return _OLSDriver.ReadIoPortWordEx(port, ref value);
        }

        public static int ReadIoPortDwordEx(ushort port, ref uint value)
        {
            return _OLSDriver.ReadIoPortDwordEx(port, ref value);
        }

        public static void WriteIoPortByte(ushort port, byte value)
        {
            _OLSDriver.WriteIoPortByte(port, value);
        }

        public static void WriteIoPortWord(ushort port, ushort value)
        {
            _OLSDriver.WriteIoPortWord(port, value);
        }

        public static void WriteIoPortDword(ushort port, uint value)
        {
            _OLSDriver.WriteIoPortDword(port, value);
        }

        public static int WriteIoPortByteEx(ushort port, byte value)
        {
            return _OLSDriver.WriteIoPortByteEx(port, value);
        }

        public static int WriteIoPortWordEx(ushort port, ushort value)
        {
            return _OLSDriver.WriteIoPortWordEx(port, value);
        }

        public static int WriteIoPortDwordEx(ushort port, uint value)
        {
            return _OLSDriver.WriteIoPortDwordEx(port, value);
        }

        public static uint FindPciDeviceById(ushort vendorId, ushort deviceId, byte index)
        {
            return _OLSDriver.FindPciDeviceById(vendorId, deviceId, index);
        }

        public static uint FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf, byte index)
        {
            return _OLSDriver.FindPciDeviceByClass(baseClass, subClass, programIf, index);
        }

        public static byte ReadPciConfigByte(uint pciAddress, byte regAddress)
        {
            return _OLSDriver.ReadPciConfigByte(pciAddress, regAddress);
        }

        public static ushort ReadPciConfigWord(uint pciAddress, byte regAddress)
        {
            return _OLSDriver.ReadPciConfigWord(pciAddress, regAddress);
        }

        public static uint ReadPciConfigDword(uint pciAddress, byte regAddress)
        {
            return _OLSDriver.ReadPciConfigDword(pciAddress, regAddress);
        }

        public static int ReadPciConfigByteEx(uint pciAddress, uint regAddress, ref byte value)
        {
            return _OLSDriver.ReadPciConfigByteEx(pciAddress, regAddress, ref value);
        }

        public static int ReadPciConfigWordEx(uint pciAddress, uint regAddress, ref ushort value)
        {
            return _OLSDriver.ReadPciConfigWordEx(pciAddress, regAddress, ref value);
        }

        public static int ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint value)
        {
            return _OLSDriver.ReadPciConfigDwordEx(pciAddress, regAddress, ref value);
        }

        public static void WritePciConfigByte(uint pciAddress, byte regAddress, byte value)
        {
            _OLSDriver.WritePciConfigByte(pciAddress, regAddress, value);
        }

        public static void WritePciConfigWord(uint pciAddress, byte regAddress, ushort value)
        {
            _OLSDriver.WritePciConfigWord(pciAddress, regAddress, value);
        }

        public static void WritePciConfigDword(uint pciAddress, byte regAddress, uint value)
        {
            _OLSDriver.WritePciConfigDword(pciAddress, regAddress, value);
        }

        public static int WritePciConfigByteEx(uint pciAddress, uint regAddress, byte value)
        {
            return _OLSDriver.WritePciConfigByteEx(pciAddress, regAddress, value);
        }

        public static int WritePciConfigWordEx(uint pciAddress, uint regAddress, ushort value)
        {
            return _OLSDriver.WritePciConfigWordEx(pciAddress, regAddress, value);
        }

        public static int WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint value)
        {
            return _OLSDriver.WritePciConfigDwordEx(pciAddress, regAddress, value);
        }

        public static bool ReadMsr(uint index, out uint eax, out uint edx)
        {
            eax = 0;
            edx = 0;

            var result = _OLS.Rdmsr(index, ref eax, ref edx);

            return result != 0;
        }

        public static bool ReadMsr(uint index, out ulong edxeax)
        {
            uint eax = 0;
            uint edx = 0;

            var result = _OLS.Rdmsr(index, ref eax, ref edx);

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
            if (!IsOpen)
            {
                return false;
            }

            var result = _OLS.Wrmsr(index, eax, edx);

            return result != 0;
        }

        public static uint GetPciAddress(byte bus, byte device, byte function)
        {
            return (uint)(((bus & 0xFF) << 8) | ((device & 0x1F) << 3) | (function & 7));
        }

        #endregion
    }
}
