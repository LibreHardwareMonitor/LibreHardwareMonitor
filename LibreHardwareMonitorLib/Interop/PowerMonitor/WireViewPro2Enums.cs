// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Interop.PowerMonitor;

public enum UsbCmd : byte
{
    CMD_WELCOME = 0,
    CMD_READ_VENDOR_DATA = 1,
    CMD_READ_UID = 2,
    CMD_READ_DEVICE_DATA = 3,
    CMD_READ_SENSOR_VALUES = 4,
    CMD_READ_CONFIG = 5,
    CMD_WRITE_CONFIG = 6,
    CMD_READ_CALIBRATION = 7,
    CMD_WRITE_CALIBRATION = 8,
    CMD_SPI_FLASH_WRITE_PAGE = 9,
    CMD_SPI_FLASH_READ_PAGE = 10,
    CMD_SPI_FLASH_ERASE_SECTOR = 11,
    CMD_SCREEN_CHANGE = 12,
    CMD_READ_BUILD_INFO = 13,
    CMD_RESET = 0xF0,
    CMD_BOOTLOADER = 0xF1,
    CMD_NVM_CONFIG = 0xF2,
    CMD_NOP = 0xFF,
}

public enum SensorTs
{
    SENSOR_TS_IN,
    SENSOR_TS_OUT,
    SENSOR_TS3,
    SENSOR_TS4,
}

public enum HpwrCapability : byte
{
    PSU_CAP_600W,
    PSU_CAP_450W,
    PSU_CAP_300W,
    PSU_CAP_150W,
}

public enum FanMode : byte
{
    FanModeCurve,
    FanModeFixed,
}

public enum TempSource : byte
{
    TempSourceTsIn,
    TempSourceTsOut,
    TempSourceTs1,
    TempSourceTs2,
    TempSourceTmax,
}

public enum CurrentScale : byte
{
    CurrentScale5A,
    CurrentScale10A,
    CurrentScale15A,
    CurrentScale20A,
}

public enum PowerScale : byte
{
    PowerScaleAuto,
    PowerScale300W,
    PowerScale600W,
}

public enum Theme : byte
{
    ThemeTg1,
    ThemeTg2,
    ThemeTg3,
}

public enum DisplayRotation : byte
{
    DisplayRotation0,
    DisplayRotation180,
}

public enum TimeoutMode : byte
{
    TimeoutModeStatic,
    TimeoutModeCycle,
    TimeoutModeSleep,
}

public enum NVM_CMD : byte
{
    NVM_CMD_NONE,
    NVM_CMD_LOAD,
    NVM_CMD_STORE,
    NVM_CMD_RESET,
    NVM_CMD_LOAD_CAL,
    NVM_CMD_STORE_CAL,
    NVM_CMD_LOAD_CAL_FACTORY,
    NVM_CMD_STORE_CAL_FACTORY,
}
