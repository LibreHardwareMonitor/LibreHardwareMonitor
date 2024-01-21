// ported from: https://gitlab.com/leogx9r/ryzen_smu
// and: https://github.com/irusanov/SMUDebugTool

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

// ReSharper disable InconsistentNaming

namespace LibreHardwareMonitor.Hardware;

internal class RyzenSMU
{
    private const byte SMU_PCI_ADDR_REG = 0xC4;
    private const byte SMU_PCI_DATA_REG = 0xC8;
    private const uint SMU_REQ_MAX_ARGS = 6;
    private const uint SMU_RETRIES_MAX = 8096;

    private readonly CpuCodeName _cpuCodeName;
    private readonly Mutex _mutex = new();
    private readonly bool _supportedCPU;

    private readonly Dictionary<uint, Dictionary<uint, SmuSensorType>> _supportedPmTableVersions = new()
    {
        {
            // Zen Raven Ridge APU.
            0x001E0004, new Dictionary<uint, SmuSensorType>
            {
                { 7, new SmuSensorType { Name = "TDC", Type = SensorType.Current, Scale = 1 } },
                { 11, new SmuSensorType { Name = "EDC", Type = SensorType.Current, Scale = 1 } },
                //{ 61, new SmuSensorType { Name = "Core", Type = SensorType.Voltage } },
                //{ 62, new SmuSensorType { Name = "Core", Type = SensorType.Current, Scale = 1} },
                //{ 63, new SmuSensorType { Name = "Core", Type = SensorType.Power, Scale = 1 } },
                //{ 65, new SmuSensorType { Name = "SoC", Type = SensorType.Voltage } },
                { 66, new SmuSensorType { Name = "SoC", Type = SensorType.Current, Scale = 1 } },
                { 67, new SmuSensorType { Name = "SoC", Type = SensorType.Power, Scale = 1 } },
                //{ 96, new SmuSensorType { Name = "Core #1", Type = SensorType.Power } },
                //{ 97, new SmuSensorType { Name = "Core #2", Type = SensorType.Power } },
                //{ 98, new SmuSensorType { Name = "Core #3", Type = SensorType.Power } },
                //{ 99, new SmuSensorType { Name = "Core #4", Type = SensorType.Power } },
                { 108, new SmuSensorType { Name = "Core #1", Type = SensorType.Temperature, Scale = 1 } },
                { 109, new SmuSensorType { Name = "Core #2", Type = SensorType.Temperature, Scale = 1 } },
                { 110, new SmuSensorType { Name = "Core #3", Type = SensorType.Temperature, Scale = 1 } },
                { 111, new SmuSensorType { Name = "Core #4", Type = SensorType.Temperature, Scale = 1 } },
                { 150, new SmuSensorType { Name = "GFX", Type = SensorType.Voltage, Scale = 1 } },
                { 151, new SmuSensorType { Name = "GFX", Type = SensorType.Temperature, Scale = 1 } },
                { 154, new SmuSensorType { Name = "GFX", Type = SensorType.Clock, Scale = 1 } },
                { 156, new SmuSensorType { Name = "GFX", Type = SensorType.Load, Scale = 1 } },
                { 166, new SmuSensorType { Name = "Fabric", Type = SensorType.Clock, Scale = 1 } },
                { 177, new SmuSensorType { Name = "Uncore", Type = SensorType.Clock, Scale = 1 } },
                { 178, new SmuSensorType { Name = "Memory", Type = SensorType.Clock, Scale = 1 } },
                { 342, new SmuSensorType { Name = "Displays", Type = SensorType.Factor, Scale = 1 } }
            }
        },
        {
            // Zen 2.
            0x00240903, new Dictionary<uint, SmuSensorType>
            {
                { 15, new SmuSensorType { Name = "TDC", Type = SensorType.Current, Scale = 1 } },
                { 21, new SmuSensorType { Name = "EDC", Type = SensorType.Current, Scale = 1 } },
                { 48, new SmuSensorType { Name = "Fabric", Type = SensorType.Clock, Scale = 1 } },
                { 50, new SmuSensorType { Name = "Uncore", Type = SensorType.Clock, Scale = 1 } },
                { 51, new SmuSensorType { Name = "Memory", Type = SensorType.Clock, Scale = 1 } },
                { 115, new SmuSensorType { Name = "SoC", Type = SensorType.Temperature, Scale = 1 } }
                //{ 66, new SmuSensorType { Name = "Bus Speed", Type = SensorType.Clock, Scale = 1 } },
                //{ 188, new SmuSensorType { Name = "Core #1", Type = SensorType.Clock, Scale = 1000 } },
                //{ 189, new SmuSensorType { Name = "Core #2", Type = SensorType.Clock, Scale = 1000 } },
                //{ 190, new SmuSensorType { Name = "Core #3", Type = SensorType.Clock, Scale = 1000 } },
                //{ 191, new SmuSensorType { Name = "Core #4", Type = SensorType.Clock, Scale = 1000 } },
                //{ 192, new SmuSensorType { Name = "Core #5", Type = SensorType.Clock, Scale = 1000 } },
                //{ 193, new SmuSensorType { Name = "Core #6", Type = SensorType.Clock, Scale = 1000 } },
            }
        },
        {
            // Zen 3.
            0x00380805, new Dictionary<uint, SmuSensorType>
            {
                { 3, new SmuSensorType { Name = "TDC", Type = SensorType.Current, Scale = 1 } },
                // TODO: requires some post-processing
                // see: https://gitlab.com/leogx9r/ryzen_smu/-/blob/master/userspace/monitor_cpu.c#L577
                // { 9, new SmuSensorType { Name = "EDC", Type = SensorType.Current, Scale = 1 } },
                { 48, new SmuSensorType { Name = "Fabric", Type = SensorType.Clock, Scale = 1 } },
                { 50, new SmuSensorType { Name = "Uncore", Type = SensorType.Clock, Scale = 1 } },
                { 51, new SmuSensorType { Name = "Memory", Type = SensorType.Clock, Scale = 1 } },
                { 127, new SmuSensorType { Name = "SoC", Type = SensorType.Temperature, Scale = 1 } },
                { 268, new SmuSensorType { Name = "Core #1 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 269, new SmuSensorType { Name = "Core #2 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 270, new SmuSensorType { Name = "Core #3 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 271, new SmuSensorType { Name = "Core #4 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 272, new SmuSensorType { Name = "Core #5 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 273, new SmuSensorType { Name = "Core #6 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 274, new SmuSensorType { Name = "Core #7 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 275, new SmuSensorType { Name = "Core #8 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 276, new SmuSensorType { Name = "Core #9 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 277, new SmuSensorType { Name = "Core #10 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 278, new SmuSensorType { Name = "Core #11 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 279, new SmuSensorType { Name = "Core #12 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 280, new SmuSensorType { Name = "Core #13 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 281, new SmuSensorType { Name = "Core #14 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 282, new SmuSensorType { Name = "Core #15 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                { 283, new SmuSensorType { Name = "Core #16 (Effective)", Type = SensorType.Clock, Scale = 1000 } }
            }
        },
        {
            // Zen 4.
            0x00540004, new Dictionary<uint, SmuSensorType>
            {
                { 3, new SmuSensorType { Name = "CPU PPT", Type = SensorType.Power, Scale = 1 } },
                { 11, new SmuSensorType { Name = "Package", Type = SensorType.Temperature, Scale = 1 } },
                { 20, new SmuSensorType { Name = "Core Power", Type = SensorType.Power, Scale = 1 } },
                { 21, new SmuSensorType { Name = "SOC Power", Type = SensorType.Power, Scale = 1 } },
                { 22, new SmuSensorType { Name = "Misc Power", Type = SensorType.Power, Scale = 1 } },
                { 26, new SmuSensorType { Name = "Total Power", Type = SensorType.Power, Scale = 1 } },
                { 47, new SmuSensorType { Name = "VDDCR", Type = SensorType.Voltage, Scale = 1 } },
                { 48, new SmuSensorType { Name = "TDC", Type = SensorType.Current, Scale = 1 } },
                { 49, new SmuSensorType { Name = "EDC", Type = SensorType.Current, Scale = 1 } },
                { 52, new SmuSensorType { Name = "VDDCR SoC", Type = SensorType.Voltage, Scale = 1 } },
                { 57, new SmuSensorType { Name = "VDD Misc", Type = SensorType.Voltage, Scale = 1 } },
                { 70, new SmuSensorType { Name = "Fabric", Type = SensorType.Clock, Scale = 1 } },
                { 74, new SmuSensorType { Name = "Uncore", Type = SensorType.Clock, Scale = 1 } },
                { 78, new SmuSensorType { Name = "Memory", Type = SensorType.Clock, Scale = 1 } },
                { 211, new SmuSensorType { Name = "IOD Hotspot", Type = SensorType.Temperature, Scale = 1 } },
                { 539, new SmuSensorType { Name = "L3 (CCD1)", Type = SensorType.Temperature, Scale = 1 } },
                { 540, new SmuSensorType { Name = "L3 (CCD2)", Type = SensorType.Temperature, Scale = 1 } },
                { 268, new SmuSensorType { Name = "LDO VDD", Type = SensorType.Voltage, Scale = 1 } },

                // This is not working, some cores can be deactivated with the core disabled map.
                // When Core 2 is disabled and Core 3 is enabled, the name of Core 3 == "Core 2".
                //{ 357, new SmuSensorType { Name = "Core #1 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 358, new SmuSensorType { Name = "Core #2 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 359, new SmuSensorType { Name = "Core #3 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 360, new SmuSensorType { Name = "Core #4 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 361, new SmuSensorType { Name = "Core #5 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 362, new SmuSensorType { Name = "Core #6 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 363, new SmuSensorType { Name = "Core #7 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 364, new SmuSensorType { Name = "Core #8 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 365, new SmuSensorType { Name = "Core #9 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 366, new SmuSensorType { Name = "Core #10 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 367, new SmuSensorType { Name = "Core #11 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 368, new SmuSensorType { Name = "Core #12 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 369, new SmuSensorType { Name = "Core #13 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 370, new SmuSensorType { Name = "Core #14 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 371, new SmuSensorType { Name = "Core #15 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 372, new SmuSensorType { Name = "Core #16 (Effective)", Type = SensorType.Clock, Scale = 1000 } }
            }
        }
    };

    private uint _argsAddr;
    private uint _cmdAddr;
    private uint _dramAddrHi;
    private uint _dramBaseAddr;
    private uint _pmTableSize;
    private uint _pmTableSizeAlt;
    private uint _pmTableVersion;
    private uint _rspAddr;

    public RyzenSMU(uint family, uint model, uint packageType)
    {
        _cpuCodeName = GetCpuCodeName(family, model, packageType);

        _supportedCPU = Environment.Is64BitOperatingSystem == Environment.Is64BitProcess && SetAddresses(_cpuCodeName);

        if (_supportedCPU && InpOut.Open())
            SetupPmTableAddrAndSize();
    }

    private static CpuCodeName GetCpuCodeName(uint family, uint model, uint packageType)
    {
        return family switch
        {
            0x17 => model switch
            {
                0x01 => packageType == 7 ? CpuCodeName.Threadripper : CpuCodeName.SummitRidge,
                0x08 => packageType == 7 ? CpuCodeName.Colfax : CpuCodeName.PinnacleRidge,
                0x11 => CpuCodeName.RavenRidge,
                0x18 => packageType == 2 ? CpuCodeName.RavenRidge2 : CpuCodeName.Picasso,
                0x20 => CpuCodeName.Dali,
                0x31 => CpuCodeName.CastlePeak,
                0x60 => CpuCodeName.Renoir,
                0x71 => CpuCodeName.Matisse,
                0x90 => CpuCodeName.Vangogh,
                _ => CpuCodeName.Undefined
            },
            0x19 => model switch
            {
                0x00 => CpuCodeName.Milan,
                0x20 or 0x21 => CpuCodeName.Vermeer,
                0x40 => CpuCodeName.Rembrandt,
                0x50 => CpuCodeName.Cezanne,
                0x61 => CpuCodeName.Raphael,
                _ => CpuCodeName.Undefined
            },
            _ => CpuCodeName.Undefined
        };
    }

    public string GetReport()
    {
        StringBuilder r = new();

        r.AppendLine("Ryzen SMU");
        r.AppendLine();
        r.AppendLine($" PM table version: 0x{_pmTableVersion:X8}");
        r.AppendLine($" PM table supported: {_supportedCPU}");
        r.AppendLine($" PM table layout defined: {IsPmTableLayoutDefined()}");

        if (_supportedCPU)
        {
            r.AppendLine($" PM table size: 0x{_pmTableSize:X3}");
            r.AppendLine($" PM table start address: 0x{_dramBaseAddr:X8}");
            r.AppendLine();
            r.AppendLine(" PM table dump:");
            r.AppendLine("  Idx    Offset   Value");
            r.AppendLine(" ------------------------");

            float[] pm_values = GetPmTable();
            for (int i = 0; i < pm_values.Length; i++)
            {
                r.AppendLine($" {i,4}    0x{i * 4:X3}    {pm_values[i]}");
            }
        }

        return r.ToString();
    }

    private bool SetAddresses(CpuCodeName codeName)
    {
        switch (codeName)
        {
            case CpuCodeName.CastlePeak:
            case CpuCodeName.Matisse:
            case CpuCodeName.Vermeer:
            case CpuCodeName.Raphael:
                _cmdAddr = 0x3B10524;
                _rspAddr = 0x3B10570;
                _argsAddr = 0x3B10A40;
                return true;

            case CpuCodeName.Colfax:
            case CpuCodeName.SummitRidge:
            case CpuCodeName.Threadripper:
            case CpuCodeName.PinnacleRidge:
                _cmdAddr = 0x3B1051C;
                _rspAddr = 0x3B10568;
                _argsAddr = 0x3B10590;
                return true;

            case CpuCodeName.Renoir:
            case CpuCodeName.Picasso:
            case CpuCodeName.RavenRidge:
            case CpuCodeName.RavenRidge2:
            case CpuCodeName.Dali:
                _cmdAddr = 0x3B10A20;
                _rspAddr = 0x3B10A80;
                _argsAddr = 0x3B10A88;
                return true;

            default:
                return false;
        }
    }

    public uint GetSmuVersion()
    {
        uint[] args = { 1 };

        if (SendCommand(0x02, ref args))
            return args[0];

        return 0;
    }

    public Dictionary<uint, SmuSensorType> GetPmTableStructure()
    {
        if (!IsPmTableLayoutDefined())
            return new Dictionary<uint, SmuSensorType>();

        return _supportedPmTableVersions[_pmTableVersion];
    }

    public bool IsPmTableLayoutDefined()
    {
        return _supportedPmTableVersions.ContainsKey(_pmTableVersion);
    }

    public float[] GetPmTable()
    {
        if (!_supportedCPU || !TransferTableToDram())
            return new float[] { 0 };

        float[] table = ReadDramToArray();

        // Fix for Zen+ empty values on first call.
        if (table.Length == 0 || table[0] == 0)
        {
            Thread.Sleep(10);
            TransferTableToDram();
            table = ReadDramToArray();
        }

        return table;
    }

    private float[] ReadDramToArray()
    {
        float[] table = new float[_pmTableSize / 4];

        IntPtr pMemory = Environment.Is64BitProcess ? new IntPtr(_dramBaseAddr | (long)_dramAddrHi << 32) : new IntPtr(_dramBaseAddr);

        byte[] bytes = InpOut.ReadMemory(pMemory, _pmTableSize);
        if (bytes != null)
            Buffer.BlockCopy(bytes, 0, table, 0, bytes.Length);

        return table;
    }

    private bool SetupPmTableAddrAndSize()
    {
        if (_pmTableSize == 0)
            SetupPmTableSize();

        if (_dramBaseAddr == 0)
            SetupDramBaseAddr();

        return _dramBaseAddr != 0 && _pmTableSize != 0;
    }

    private void SetupPmTableSize()
    {
        if (!GetPmTableVersion(ref _pmTableVersion))
            return;

        switch (_cpuCodeName)
        {
            case CpuCodeName.Matisse:
                switch (_pmTableVersion)
                {
                    case 0x240902:
                        _pmTableSize = 0x514;
                        break;
                    case 0x240903:
                        _pmTableSize = 0x518;
                        break;
                    case 0x240802:
                        _pmTableSize = 0x7E0;
                        break;
                    case 0x240803:
                        _pmTableSize = 0x7E4;
                        break;
                    default:
                        return;
                }

                break;

            case CpuCodeName.Vermeer:
                switch (_pmTableVersion)
                {
                    case 0x2D0903:
                        _pmTableSize = 0x594;
                        break;
                    case 0x380904:
                        _pmTableSize = 0x5A4;
                        break;
                    case 0x380905:
                        _pmTableSize = 0x5D0;
                        break;
                    case 0x2D0803:
                        _pmTableSize = 0x894;
                        break;
                    case 0x380804:
                        _pmTableSize = 0x8A4;
                        break;
                    case 0x380805:
                        _pmTableSize = 0x8F0;
                        break;
                    default:
                        return;
                }

                break;

            case CpuCodeName.Renoir:
                switch (_pmTableVersion)
                {
                    case 0x370000:
                        _pmTableSize = 0x794;
                        break;
                    case 0x370001:
                        _pmTableSize = 0x884;
                        break;
                    case 0x370002:
                    case 0x370003:
                        _pmTableSize = 0x88C;
                        break;
                    case 0x370004:
                        _pmTableSize = 0x8AC;
                        break;
                    case 0x370005:
                        _pmTableSize = 0x8C8;
                        break;
                    default:
                        return;
                }

                break;

            case CpuCodeName.Cezanne:
                switch (_pmTableVersion)
                {
                    case 0x400005:
                        _pmTableSize = 0x944;
                        break;

                    default:
                        return;
                }

                break;

            case CpuCodeName.Picasso:
            case CpuCodeName.RavenRidge:
            case CpuCodeName.RavenRidge2:
                _pmTableSizeAlt = 0xA4;
                _pmTableSize = 0x608 + _pmTableSizeAlt;
                break;

            case CpuCodeName.Raphael:
                switch (_pmTableVersion)
                {
                    case 0x00540004:
                        _pmTableSize = 0x948;
                        break;

                    case 0x00540104:
                        _pmTableSize = 0x950;
                        break;

                    default:
                        return;
                }

                break;

            default:
                return;
        }
    }

    private bool GetPmTableVersion(ref uint version)
    {
        uint[] args = { 0 };
        uint fn;

        switch (_cpuCodeName)
        {
            case CpuCodeName.RavenRidge:
            case CpuCodeName.Picasso:
                fn = 0x0c;
                break;
            case CpuCodeName.Matisse:
            case CpuCodeName.Vermeer:
                fn = 0x08;
                break;
            case CpuCodeName.Renoir:
                fn = 0x06;
                break;
            case CpuCodeName.Raphael:
                fn = 0x05;
                break;
            default:
                return false;
        }

        bool ret = SendCommand(fn, ref args);
        version = args[0];

        return ret;
    }

    private void SetupAddrClass1(uint[] fn)
    {
        uint[] args = { 1, 1 };

        bool command = SendCommand(fn[0], ref args);
        if (!command)
            return;

        _dramBaseAddr = args[0];
        _dramAddrHi = args[1];
    }

    private void SetupAddrClass2(uint[] fn)
    {
        uint[] args = { 0, 0, 0, 0, 0, 0 };

        bool command = SendCommand(fn[0], ref args);
        if (!command)
            return;

        args = new uint[] { 0 };
        command = SendCommand(fn[1], ref args);
        if (!command)
            return;

        _dramBaseAddr = args[0];
    }

    private void SetupAddrClass3(uint[] fn)
    {
        uint[] parts = { 0, 0 };

        // == Part 1 ==
        uint[] args = { 3 };
        bool command = SendCommand(fn[0], ref args);
        if (!command)
            return;

        args = new uint[] { 3 };
        command = SendCommand(fn[2], ref args);
        if (!command)
            return;

        // 1st Base.
        parts[0] = args[0];
        // == Part 1 End ==

        // == Part 2 ==
        args = new uint[] { 3 };
        command = SendCommand(fn[1], ref args);
        if (!command)
            return;

        args = new uint[] { 5 };
        command = SendCommand(fn[0], ref args);
        if (!command)
            return;

        args = new uint[] { 5 };
        command = SendCommand(fn[2], ref args);
        if (!command)
            return;

        // 2nd base.
        parts[1] = args[0];
        // == Part 2 End ==

        _dramBaseAddr = parts[0] & 0xFFFFFFFF;
    }

    private void SetupDramBaseAddr()
    {
        uint[] fn = { 0, 0, 0 };

        switch (_cpuCodeName)
        {
            case CpuCodeName.Raphael:
                fn[0] = 0x04;
                SetupAddrClass1(fn);
                return;
            case CpuCodeName.Vermeer:
            case CpuCodeName.Matisse:
            case CpuCodeName.CastlePeak:
                fn[0] = 0x06;
                SetupAddrClass1(fn);
                return;
            case CpuCodeName.Renoir:
                fn[0] = 0x66;
                SetupAddrClass1(fn);
                return;
            case CpuCodeName.Colfax:
            case CpuCodeName.PinnacleRidge:
                fn[0] = 0x0b;
                fn[1] = 0x0c;
                SetupAddrClass2(fn);
                return;
            case CpuCodeName.Dali:
            case CpuCodeName.Picasso:
            case CpuCodeName.RavenRidge:
            case CpuCodeName.RavenRidge2:
                fn[0] = 0x0a;
                fn[1] = 0x3d;
                fn[2] = 0x0b;
                SetupAddrClass3(fn);
                return;
            default:
                return;
        }
    }

    public bool TransferTableToDram()
    {
        uint[] args = { 0 };
        uint fn;

        switch (_cpuCodeName)
        {
            case CpuCodeName.Raphael:
                fn = 0x03;
                break;
            case CpuCodeName.Matisse:
            case CpuCodeName.Vermeer:
                fn = 0x05;
                break;
            case CpuCodeName.Renoir:
                args[0] = 3;
                fn = 0x65;
                break;
            case CpuCodeName.Picasso:
            case CpuCodeName.RavenRidge:
            case CpuCodeName.RavenRidge2:
                args[0] = 3;
                fn = 0x3d;
                break;
            default:
                return false;
        }

        return SendCommand(fn, ref args);
    }

    private bool SendCommand(uint msg, ref uint[] args)
    {
        uint[] cmdArgs = new uint[SMU_REQ_MAX_ARGS];
        int argsLength = Math.Min(args.Length, cmdArgs.Length);

        for (int i = 0; i < argsLength; ++i)
            cmdArgs[i] = args[i];

        uint tmp = 0;
        if (_mutex.WaitOne(5000))
        {
            // Step 1: Wait until the RSP register is non-zero.

            tmp = 0;
            uint retries = SMU_RETRIES_MAX;
            do
            {
                if (!ReadReg(_rspAddr, ref tmp))
                {
                    _mutex.ReleaseMutex();
                    return false;
                }
            }
            while (tmp == 0 && 0 != retries--);

            // Step 1.b: A command is still being processed meaning a new command cannot be issued.

            if (retries == 0 && tmp == 0)
            {
                _mutex.ReleaseMutex();
                return false;
            }

            // Step 2: Write zero (0) to the RSP register
            WriteReg(_rspAddr, 0);

            // Step 3: Write the argument(s) into the argument register(s)
            for (int i = 0; i < cmdArgs.Length; ++i)
                WriteReg(_argsAddr + (uint)(i * 4), cmdArgs[i]);

            // Step 4: Write the message Id into the Message ID register
            WriteReg(_cmdAddr, msg);

            // Step 5: Wait until the Response register is non-zero.
            tmp = 0;
            retries = SMU_RETRIES_MAX;
            do
            {
                if (!ReadReg(_rspAddr, ref tmp))
                {
                    _mutex.ReleaseMutex();
                    return false;
                }
            }
            while (tmp == 0 && retries-- != 0);

            if (retries == 0 && tmp != (uint)Status.OK)
            {
                _mutex.ReleaseMutex();
                return false;
            }

            // Step 6: If the Response register contains OK, then SMU has finished processing  the message.

            args = new uint[SMU_REQ_MAX_ARGS];
            for (byte i = 0; i < SMU_REQ_MAX_ARGS; i++)
            {
                if (!ReadReg(_argsAddr + (uint)(i * 4), ref args[i]))
                {
                    _mutex.ReleaseMutex();
                    return false;
                }
            }

            ReadReg(_rspAddr, ref tmp);
            _mutex.ReleaseMutex();
        }

        return tmp == (uint)Status.OK;
    }

    private static void WriteReg(uint addr, uint data)
    {
        if (Mutexes.WaitPciBus(10))
        {
            if (Ring0.WritePciConfig(0x00, SMU_PCI_ADDR_REG, addr))
                Ring0.WritePciConfig(0x00, SMU_PCI_DATA_REG, data);

            Mutexes.ReleasePciBus();
        }
    }

    private static bool ReadReg(uint addr, ref uint data)
    {
        bool read = false;

        if (Mutexes.WaitPciBus(10))
        {
            if (Ring0.WritePciConfig(0x00, SMU_PCI_ADDR_REG, addr))
                read = Ring0.ReadPciConfig(0x00, SMU_PCI_DATA_REG, out data);

            Mutexes.ReleasePciBus();
        }

        return read;
    }

    public struct SmuSensorType
    {
        public string Name;
        public SensorType Type;
        public float Scale;
    }

    private enum Status : uint
    {
        OK = 0x01,
        CmdRejectedBusy = 0xFC,
        CmdRejectedPrereq = 0xFD,
        UnknownCmd = 0xFE,
        Failed = 0xFF
    }

    private enum CpuCodeName
    {
        Undefined,
        Colfax,
        Renoir,
        Picasso,
        Matisse,
        Threadripper,
        CastlePeak,
        RavenRidge,
        RavenRidge2,
        SummitRidge,
        PinnacleRidge,
        Rembrandt,
        Vermeer,
        Vangogh,
        Cezanne,
        Milan,
        Dali,
        Raphael
    }
}
