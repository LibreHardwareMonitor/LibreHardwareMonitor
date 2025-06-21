// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using RAMSPDToolkit.Windows.Driver;

using IOCC = LibreHardwareMonitor.Interop.Ring0;

namespace LibreHardwareMonitor.Hardware
{
    /// <summary>
    /// Implementation of <see cref="IDriver"/> interface for RAMSPDToolkit.
    /// </summary>
    internal class RAMSPDToolkitDriver : IDriver
    {
        private KernelDriver _kernelDriver;

        private const byte PCI_MAX_NUMBER_OF_BUS  = 255;
        private const byte PCI_NUMBER_OF_DEVICE   =  32;
        private const byte PCI_NUMBER_OF_FUNCTION =   8;

        public bool IsOpen => _kernelDriver != null;

        public RAMSPDToolkitDriver(KernelDriver kernelDriver)
        {
            _kernelDriver = kernelDriver;
        }

        //Driver is being managed internally by other classes, so nothing to do in Load + Unload

        public bool Load()
        {
            return true;
        }

        public void Unload()
        {
        }

        public byte ReadIoPortByte(ushort port)
        {
            byte value = 0;

            ReadIoPortByteEx(port, ref value);

            return value;
        }

        public ushort ReadIoPortWord(ushort port)
        {
            ushort value = 0;

            ReadIoPortWordEx(port, ref value);

            return value;
        }

        public uint ReadIoPortDword(ushort port)
        {
            uint value = 0;

            ReadIoPortDwordEx(port, ref value);

            return value;
        }

        public bool ReadIoPortByteEx(ushort port, ref byte value)
        {
            if (!IsOpen)
                return false;

            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_READ_IO_PORT_BYTE, port, ref value);
        }

        public bool ReadIoPortWordEx(ushort port, ref ushort value)
        {
            if (!IsOpen)
                return false;

            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_READ_IO_PORT_WORD, port, ref value);
        }

        public bool ReadIoPortDwordEx(ushort port, ref uint value)
        {
            if (!IsOpen)
                return false;

            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_READ_IO_PORT_DWORD, port, ref value);
        }

        public void WriteIoPortByte(ushort port, byte value)
        {
            WriteIoPortByteEx(port, value);
        }

        public void WriteIoPortWord(ushort port, ushort value)
        {
            WriteIoPortWordEx(port, value);
        }

        public void WriteIoPortDword(ushort port, uint value)
        {
            WriteIoPortDwordEx(port, value);
        }

        public bool WriteIoPortByteEx(ushort port, byte value)
        {
            if (!IsOpen)
                return false;

            var input = new WriteIoPortInputByte { PortNumber = port, Value = value };
            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_WRITE_IO_PORT_BYTE, input);
        }

        public bool WriteIoPortWordEx(ushort port, ushort value)
        {
            if (!IsOpen)
                return false;

            var input = new WriteIoPortInputWord { PortNumber = port, Value = value };
            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_WRITE_IO_PORT_WORD, input);
        }

        public bool WriteIoPortDwordEx(ushort port, uint value)
        {
            if (!IsOpen)
                return false;

            var input = new WriteIoPortInputDword { PortNumber = port, Value = value };
            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_WRITE_IO_PORT_DWORD, input);
        }

        public uint FindPciDeviceById(ushort vendorId, ushort deviceId, byte index)
        {
            if (!IsOpen)
                return uint.MaxValue;

            if (vendorId == ushort.MaxValue)
            {
                return uint.MaxValue;
            }

            uint count = 0;

            for (byte bus = 0; bus < PCI_MAX_NUMBER_OF_BUS; ++bus)
            {
                for (byte dev = 0; dev < PCI_NUMBER_OF_DEVICE; ++dev)
                {
                    bool multiFuncFlag = false;

                    for (byte func = 0; func < PCI_NUMBER_OF_FUNCTION; ++func)
                    {
                        if (!multiFuncFlag && func > 0)
                        {
                            break;
                        }

                        uint pciAddress = Ring0.GetPciAddress(bus, dev, func);

                        uint id = 0;

                        if (PciConfigRead(pciAddress, 0, ref id))
                        {
                            if (func == 0) //Is Multi Function Device
                            {
                                byte type = 0;

                                if (PciConfigRead(pciAddress, 0x0E, ref type))
                                {
                                    if ((type & 0x80) != 0)
                                    {
                                        multiFuncFlag = true;
                                    }
                                }
                            }

                            if (id == (vendorId | ((uint)deviceId << 16)))
                            {
                                if (count == index)
                                {
                                    return pciAddress;
                                }

                                ++count;
                                continue;
                            }
                        }
                    }
                }
            }

            return uint.MaxValue;
        }

        public uint FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf, byte index)
        {
            if (!IsOpen)
                return uint.MaxValue;

            uint count = 0;

            for (byte bus = 0; bus < PCI_MAX_NUMBER_OF_BUS; ++bus)
            {
                for (byte dev = 0; dev < PCI_NUMBER_OF_DEVICE; ++dev)
                {
                    bool multiFuncFlag = false;

                    for (byte func = 0; func < PCI_NUMBER_OF_FUNCTION; ++func)
                    {
                        if (!multiFuncFlag && func > 0)
                        {
                            break;
                        }

                        uint pciAddress = Ring0.GetPciAddress(bus, dev, func);

                        var conf = new Conf();

                        if (PciConfigRead(pciAddress, 0, ref conf))
                        {
                            if (func == 0) //Is Multi Function Device
                            {
                                byte type = 0;

                                if (PciConfigRead(pciAddress, 0x0E, ref type))
                                {
                                    if ((type & 0x80) != 0)
                                    {
                                        multiFuncFlag = true;
                                    }
                                }
                            }

                            var temp = (uint)baseClass << 24 | (uint)subClass << 16 | (uint)programIf << 8;

                            if ((conf.C & 0xFFFFFF00) == temp)
                            {
                                if (count == index)
                                {
                                    return pciAddress;
                                }

                                ++count;
                                continue;
                            }
                        }
                    }
                }
            }

            return uint.MaxValue;
        }

        public byte ReadPciConfigByte(uint pciAddress, byte regAddress)
        {
            byte value = 0;

            if (ReadPciConfigByteEx(pciAddress, regAddress, ref value))
            {
                return value;
            }
            else
            {
                return byte.MaxValue;
            }
        }

        public ushort ReadPciConfigWord(uint pciAddress, byte regAddress)
        {
            ushort value = 0;

            if (ReadPciConfigWordEx(pciAddress, regAddress, ref value))
            {
                return value;
            }
            else
            {
                return ushort.MaxValue;
            }
        }

        public uint ReadPciConfigDword(uint pciAddress, byte regAddress)
        {
            uint value = 0;

            if (ReadPciConfigDwordEx(pciAddress, regAddress, ref value))
            {
                return value;
            }
            else
            {
                return uint.MaxValue;
            }
        }

        public bool ReadPciConfigByteEx(uint pciAddress, uint regAddress, ref byte value)
        {
            return PciConfigRead(pciAddress, regAddress, ref value);
        }

        public bool ReadPciConfigWordEx(uint pciAddress, uint regAddress, ref ushort value)
        {
            return PciConfigRead(pciAddress, regAddress, ref value);
        }

        public bool ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint value)
        {
            return PciConfigRead(pciAddress, regAddress, ref value);
        }

        public void WritePciConfigByte(uint pciAddress, byte regAddress, byte value)
        {
            WritePciConfigByteEx(pciAddress, regAddress, value);
        }

        public void WritePciConfigWord(uint pciAddress, byte regAddress, ushort value)
        {
            WritePciConfigWordEx(pciAddress, regAddress, value);
        }

        public void WritePciConfigDword(uint pciAddress, byte regAddress, uint value)
        {
            WritePciConfigDwordEx(pciAddress, regAddress, value);
        }

        public bool WritePciConfigByteEx(uint pciAddress, uint regAddress, byte value)
        {
            var input = new WritePciConfigInputByte { PciAddress = pciAddress, RegAddress = regAddress, Value = value };
            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_WRITE_PCI_CONFIG, input);
        }

        public bool WritePciConfigWordEx(uint pciAddress, uint regAddress, ushort value)
        {
            var input = new WritePciConfigInputWord { PciAddress = pciAddress, RegAddress = regAddress, Value = value };
            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_WRITE_PCI_CONFIG, input);
        }

        public bool WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint value)
        {
            var input = new WritePciConfigInputDword { PciAddress = pciAddress, RegAddress = regAddress, Value = value };
            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_WRITE_PCI_CONFIG, input);
        }

        private bool PciConfigRead<TValue>(uint pciAddress, uint regAddress, ref TValue value)
            where TValue : struct
        {
            var input = new ReadPciConfigInput { PciAddress = pciAddress, RegAddress = regAddress };
            return _kernelDriver.DeviceIOControl(IOCC.IOCTL_OLS_READ_PCI_CONFIG, input, ref value);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WriteIoPortInputByte
        {
            public uint PortNumber;
            public byte Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WriteIoPortInputWord
        {
            public uint PortNumber;
            public ushort Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WriteIoPortInputDword
        {
            public uint PortNumber;
            public uint Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ReadPciConfigInput
        {
            public uint PciAddress;
            public uint RegAddress;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WritePciConfigInputByte
        {
            public uint PciAddress;
            public uint RegAddress;
            public byte Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WritePciConfigInputWord
        {
            public uint PciAddress;
            public uint RegAddress;
            public ushort Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WritePciConfigInputDword
        {
            public uint PciAddress;
            public uint RegAddress;
            public uint Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Conf
        {
            public uint A;
            public uint B;
            public uint C;
        }
    }
}
