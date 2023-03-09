// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Motherboard.Lpc
{

    /// <summary>
    /// This is a controller present on some Gigabyte motherboards for both Intel and AMD, that is in custom firmware
    /// loaded onto the 2nd ITE EC.
    ///
    /// It can be accessed by using memory mapped IO, mapping its internal RAM onto main RAM via the ISA Bridge.
    /// 
    /// This class can disable it so that the regular IT87XX code can drive the fans.
    /// </summary>
    internal class GigabyteController
    {
        private bool? _initialState;
        private ProcessorFamily _processorFamily;

        /// <summary>
        /// Base address in PCI RAM that maps to the EC's RAM
        /// </summary>
        private uint _controllerBaseAddress;

        public GigabyteController(uint address, ProcessorFamily family)
        {
            _controllerBaseAddress = address;
            _processorFamily = family;
        }


        /// <summary>
        /// Enable/Disable Fan Control
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns>true on success</returns>
        public bool Enable(bool enabled)
        {
            switch (_processorFamily)
            {
                case ProcessorFamily.AmdZen:
                    return AMDEnable(enabled);
                // TODO: Intel
                default:
                    return false;
            }
        }

        private bool AMDEnable(bool enabled)
        {
            Ring0.Open();

            if (!Ring0.WaitPciBusMutex(10))
                return false;

            // see D14F3x https://www.amd.com/system/files/TechDocs/55072_AMD_Family_15h_Models_70h-7Fh_BKDG.pdf 
            uint AmdIsaBridgeAddress = Ring0.GetPciAddress(0x0, 0x14, 0x3);

            const uint IOorMemoryPortDecodeEnableRegister = 0x48;
            const uint MemoryRangePortEnableMask = 0x1 << 5;
            const uint PCIMemoryAddressforLPCTargetCyclesRegister = 0x60;
            const uint ROMAddressRange2Register = 0x6C;
            uint ControllerFanControlAddress = _controllerBaseAddress + ControllerFanControlArea;

            uint pciAddressStart = _controllerBaseAddress >> 0x10;
            uint pciAddressEnd = pciAddressStart + 1;

            uint enabledPCIMemoryAddressRegister = pciAddressEnd << 0x10 | pciAddressStart;
            uint enabledROMAddressRegister = 0xFFFFU << 0x10 | pciAddressEnd;

            Ring0.ReadPciConfig(AmdIsaBridgeAddress, IOorMemoryPortDecodeEnableRegister, out uint originalDecodeEnableRegister);
            Ring0.ReadPciConfig(AmdIsaBridgeAddress, PCIMemoryAddressforLPCTargetCyclesRegister, out uint originalPCIMemoryAddressRegister);
            Ring0.ReadPciConfig(AmdIsaBridgeAddress, ROMAddressRange2Register, out uint originalROMAddressRegister);

            bool originalMMIOEnabled =
                (originalDecodeEnableRegister & MemoryRangePortEnableMask) != 0
                && originalPCIMemoryAddressRegister == enabledPCIMemoryAddressRegister
                && originalROMAddressRegister == enabledROMAddressRegister;

            if (!originalMMIOEnabled)
            {
                Ring0.WritePciConfig(AmdIsaBridgeAddress, IOorMemoryPortDecodeEnableRegister, originalDecodeEnableRegister | MemoryRangePortEnableMask);
                Ring0.WritePciConfig(AmdIsaBridgeAddress, PCIMemoryAddressforLPCTargetCyclesRegister, enabledPCIMemoryAddressRegister);
                Ring0.WritePciConfig(AmdIsaBridgeAddress, ROMAddressRange2Register, enabledROMAddressRegister);
            }

            var result = _Enable(enabled, new IntPtr(ControllerFanControlAddress));

            // Restore previous values
            if (!originalMMIOEnabled)
            {
                Ring0.WritePciConfig(AmdIsaBridgeAddress, IOorMemoryPortDecodeEnableRegister, originalDecodeEnableRegister);
                Ring0.WritePciConfig(AmdIsaBridgeAddress, PCIMemoryAddressforLPCTargetCyclesRegister, originalROMAddressRegister);
                Ring0.WritePciConfig(AmdIsaBridgeAddress, ROMAddressRange2Register, originalROMAddressRegister);
            }

            Ring0.ReleasePciBusMutex();

            return result;
        }

        private bool _Enable(bool enabled, IntPtr PCIMMIOBaseAddress)
        {
            // Map PCI memory to this process memory
            if (!InpOut.Open())
                return false;

            IntPtr mapped = InpOut.MapMemory(PCIMMIOBaseAddress, ControllerAddressRange, out IntPtr handle);

            if (mapped == IntPtr.Zero)
                return false;

            var current = Convert.ToBoolean(Marshal.ReadByte(mapped, ControllerEnableRegister));

            if (!_initialState.HasValue)
                _initialState = current;

            // Update Controller State
            if (current != enabled)
            {
                Marshal.WriteByte(mapped, ControllerEnableRegister, Convert.ToByte(enabled));
                // Give it some time to see the change
                Thread.Sleep(200);
            }

            InpOut.UnmapMemory(handle, mapped);

            return true;
        }

        /// <summary>
        /// Restore settings back to initial values
        /// </summary>
        public void Restore()
        {
            if (_initialState.HasValue)
                Enable(_initialState.Value);
        }

        const int ControllerEnableRegister = 0x47;
        const uint ControllerAddressRange = 0xFF;
        const uint ControllerFanControlArea = 0x900;
    }
}
