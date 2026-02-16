// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) Florian K. (Blacktempel)
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Controller.MSI;

public enum MsiDeviceType
{
    S280,
    S360,
    S360MEG,
    X360,
    X240,
    D360,
    D240,
}

public class MsiDevice
{
    public MsiDevice(MsiDeviceType msiDeviceType, int vendorId, int productId, int productIdController)
    {
        DeviceType = msiDeviceType;
        VendorId = vendorId;
        ProductId = productId;
        ProductIdController = productIdController;
    }

    public MsiDeviceType DeviceType { get; }
    public int VendorId { get; }
    public int ProductId { get; }
    public int ProductIdController { get; }

    public string Name
    {
        get
        {
            switch (DeviceType)
            {
                case MsiDeviceType.S280:
                    return "MSI CoreLiquid S280";
                case MsiDeviceType.S360:
                    return "MSI CoreLiquid S360";
                case MsiDeviceType.S360MEG:
                    return "MSI CoreLiquid S360 MEG";
                case MsiDeviceType.X360:
                    return "MSI CoreLiquid X360";
                case MsiDeviceType.X240:
                    return "MSI CoreLiquid X240";
                case MsiDeviceType.D360:
                    return "MSI CoreLiquid D360";
                case MsiDeviceType.D240:
                    return "MSI CoreLiquid D240";
                default:
                    return "Other";
            }
        }
    }

    //Relevant for further HWMonitoring later
    public bool SupportsHWMonitorIndex13and14(uint firmwareVersion)
    {
        switch (DeviceType)
        {
            case MsiDeviceType.S280:
            case MsiDeviceType.S360:
                return (firmwareVersion & byte.MaxValue) >= 10;
            case MsiDeviceType.S360MEG:
                return (firmwareVersion & byte.MaxValue) >= 7;
            case MsiDeviceType.X360:
            case MsiDeviceType.X240:
                return (firmwareVersion & byte.MaxValue) >= 3;
            case MsiDeviceType.D360:
            case MsiDeviceType.D240:
                return true;
        }

        return false;
    }
}
