# Fan Controller Device Integration Guide

Arctic Fan Controller USB Custom HID device that accepts 10 PWM commands (0–100%) via OUT reports and returns current PWM values plus 10 RPM sensor readings (0–65535) via IN reports every ~1 second, using 32-byte reports.

## Protocol

**Interface:** USB Custom HID  
**Vendor ID (VID):** 0x3904 (14596 decimal)  
**Product ID (PID):** 0xF001 (61441 decimal)  
**Report Size:** 32 bytes  
**Report ID:** 0x01 (byte 0)

------------------------------------------------

## PWM Control (OUT Report)

**Format:**
- Byte 0: Report ID = `0x01`
- Bytes 1-10: PWM duty cycle for channels 1-10 (0-100)
- Bytes 11-31: Ignored

**Specifications:**
- 10 channels (1-10)
- Range: 0-100 (percentage)

**Example:** Set channel 1 to 50%, channel 2 to 75%
```
[0x01, 50, 75, ...]
```

------------------------------------------------

## RPM Reading (IN Report)

**Format:**
- Byte 0: Report ID = `0x01`
- Bytes 1-10: Current PWM values
- Bytes 11-30: RPM values for sensors 1-10 (uint16, little-endian)
  - Sensor 1: bytes 11-12
  - Sensor 2: bytes 13-14
  - ... (2 bytes per sensor)
- Byte 31: Padding

**Specifications:**
- 10 sensors (1-10)
- Range: 0-65535 RPM
- Update rate: ~1 second

**Example:** Read sensor 1 RPM
```
RPM = buffer[11] + (buffer[12] << 8)
```

------------------------------------------------

## Notes

- Report ID `0x01` required in all reports
- RPM updates every ~1 second
- PWM values take effect immediately
- All multi-byte values are little-endian



# Complete Transaction Examples 

### OUT Transaction (PC → Device)

Setting PWM: Channel 1 = 50%, Channels 2-10 = 75%

```
Byte:  0     1     2     3     4     5     6     7     8     9    10    11    12    13    14    15    16    17    18    19    20    21    22    23    24    25    26    27    28    29    30    31
Hex:   0x01  0x32  0x4B  0x4B  0x4B  0x4B  0x4B  0x4B  0x4B  0x4B  0x4B  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00  0x00
Dec:   1     50    75    75    75    75    75    75    75    75    75    0     0     0     0     0     0     0     0     0     0     0     0     0     0     0     0     0     0     0     0     0
       └─┘   └────────────────────────────────────────────────────────┘  └────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
       ID    PWM Channels 1-10 (duty cycle %)                             Ignored
```

**Explanation:**
- Byte 0: Report ID = 1 (0x01)
- Bytes 1-10: PWM duty cycles = 50%, 75%, 75%, 75%, 75%, 75%, 75%, 75%, 75%, 75%
- Bytes 11-31: Ignored (all zeros)

---

### IN Transaction (Device → PC)

Reading current PWM and RPM values

```
Byte:  0     1     2     3     4     5     6     7     8     9    10    11    12    13    14    15    16    17    18    19    20    21    22    23    24    25    26    27    28    29    30    31
Hex:   0x01  0x32  0x4B  0x4B  0x4B  0x4B  0x4B  0x4B  0x4B  0x4B  0x4B  0xDC  0x05  0xD0  0x07  0xD0  0x07  0xD0  0x07  0xD0  0x07  0xD0  0x07  0xD0  0x07  0xD0  0x07  0xD0  0x07  0xD0  0x07  0x00
Dec:   1     50    75    75    75    75    75    75    75    75    75    220   5     208   7     208   7     208   7     208   7     208   7     208   7     208   7     208   7     208   7     0
       └─┘   └────────────────────────────────────────────────────────┘  └──────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘   └┘
       ID    Current PWM values (Channels 1-10)                         RPM values (Sensors 1-10, uint16 little-endian)                                                                       Padding
```

**Explanation:**
- Byte 0: Report ID = 1 (0x01)
- Bytes 1-10: Current PWM values = 50%, 75%, 75%, 75%, 75%, 75%, 75%, 75%, 75%, 75%
- Bytes 11-12: Sensor 1 RPM = 0x05DC = 1500 RPM (220 + 5×256)
- Bytes 13-14: Sensor 2 RPM = 0x07D0 = 2000 RPM (208 + 7×256)
- Bytes 15-16: Sensor 3 RPM = 0x07D0 = 2000 RPM (208 + 7×256)
- Bytes 17-18: Sensor 4 RPM = 0x07D0 = 2000 RPM (208 + 7×256)
- Bytes 19-20: Sensor 5 RPM = 0x07D0 = 2000 RPM (208 + 7×256)
- Bytes 21-22: Sensor 6 RPM = 0x07D0 = 2000 RPM (208 + 7×256)
- Bytes 23-24: Sensor 7 RPM = 0x07D0 = 2000 RPM (208 + 7×256)
- Bytes 25-26: Sensor 8 RPM = 0x07D0 = 2000 RPM (208 + 7×256)
- Bytes 27-28: Sensor 9 RPM = 0x07D0 = 2000 RPM (208 + 7×256)
- Bytes 29-30: Sensor 10 RPM = 0x07D0 = 2000 RPM (208 + 7×256)
- Byte 31: Padding = 0
