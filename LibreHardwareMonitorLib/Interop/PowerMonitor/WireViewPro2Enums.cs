// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Interop.PowerMonitor;

public enum UsbCmd : byte
{
    CMD_WELCOME,
    CMD_READ_VENDOR_DATA,
    CMD_READ_UID,
    CMD_READ_DEVICE_DATA,
    CMD_READ_SENSOR_VALUES,
    CMD_READ_CONFIG,
    CMD_WRITE_CONFIG,
    CMD_READ_CALIBRATION,
    CMD_WRITE_CALIBRATION,
    CMD_SPI_FLASH_WRITE_PAGE,
    CMD_SPI_FLASH_READ_PAGE,
    CMD_SPI_FLASH_ERASE_SECTOR,
    CMD_SCREEN_CHANGE,
    CMD_READ_BUILD_INFO,
    CMD_CLEAR_FAULTS,
    CMD_RESET = 0xF0,
    CMD_BOOTLOADER = 0xF1,
    CMD_NVM_CONFIG = 0xF2,
    CMD_NOP = 0xFF
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
    PSU_CAP_600W = 0,
    PSU_CAP_450W = 1,
    PSU_CAP_300W = 2,
    PSU_CAP_150W = 3
}

public enum FanMode : byte
{
    FanModeCurve = 0,
    FanModeFixed = 1
}

public enum TempSource : byte
{
    TempSourceTsIn = 0,
    TempSourceTsOut = 1,
    TempSourceTs1 = 2,
    TempSourceTs2 = 3,
    TempSourceTmax = 4
}

public enum CurrentScale : byte
{
    CurrentScale5A = 0,
    CurrentScale10A = 1,
    CurrentScale15A = 2,
    CurrentScale20A = 3
}

public enum PowerScale : byte
{
    PowerScaleAuto = 0,
    PowerScale300W = 1,
    PowerScale600W = 2
}

public enum Theme : byte
{
    ThemeTg1 = 0,
    ThemeTg2 = 1,
    ThemeTg3 = 2
}

public enum DisplayRotation : byte
{
    DisplayRotation0 = 0,
    DisplayRotation180 = 1
}

public enum TimeoutMode : byte
{
    TimeoutModeStatic = 0,
    TimeoutModeCycle = 1,
    TimeoutModeSleep = 2
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

public enum SCREEN_CMD : byte
{
    SCREEN_GOTO_MAIN = 0xE0,
    SCREEN_GOTO_SIMPLE = 0xE1,
    SCREEN_GOTO_CURRENT = 0xE2,
    SCREEN_GOTO_TEMP = 0xE3,
    SCREEN_GOTO_STATUS = 0xE4,
    SCREEN_GOTO_SAME = 0xEF,
    SCREEN_PAUSE_UPDATES = 0xF0,
    SCREEN_RESUME_UPDATES = 0xF1
}
