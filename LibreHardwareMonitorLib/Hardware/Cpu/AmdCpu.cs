// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Cpu;

internal abstract class AmdCpu : GenericCpu
{
    protected AmdCpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    { }

    protected uint GetPciAddress(byte function, ushort deviceId)
    {
        // assemble the pci address
        uint address = Ring0.GetPciAddress(PCI_BUS, (byte)(PCI_BASE_DEVICE + Index), function);

        // verify that we have the correct bus, device and function
        if (!Ring0.ReadPciConfig(address, DEVICE_VENDOR_ID_REGISTER, out uint deviceVendor))
            return Interop.Ring0.INVALID_PCI_ADDRESS;

        if (deviceVendor != (deviceId << 16 | AMD_VENDOR_ID))
            return Interop.Ring0.INVALID_PCI_ADDRESS;

        return address;
    }

    // ReSharper disable InconsistentNaming
    private const ushort AMD_VENDOR_ID = 0x1022;
    private const byte DEVICE_VENDOR_ID_REGISTER = 0;
    private const byte PCI_BASE_DEVICE = 0x18;

    private const byte PCI_BUS = 0;
    // ReSharper restore InconsistentNaming
}