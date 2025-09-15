// ported from: https://gitlab.com/leogx9r/ryzen_smu
// and: https://github.com/irusanov/SMUDebugTool

using System;
using System.Collections.Generic;
using System.Text;
using LibreHardwareMonitor.PawnIo;

// ReSharper disable InconsistentNaming

namespace LibreHardwareMonitor.Hardware;

internal class RyzenSMU
{
    private readonly CpuCodeName _cpuCodeName;
    private readonly bool _supportedCPU;
    private readonly Exception _unsupportedCPUException;

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

                //Core effective clock is now calculated in Amd17Cpu/Core
                //{ 268, new SmuSensorType { Name = "Core #1 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 269, new SmuSensorType { Name = "Core #2 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 270, new SmuSensorType { Name = "Core #3 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 271, new SmuSensorType { Name = "Core #4 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 272, new SmuSensorType { Name = "Core #5 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 273, new SmuSensorType { Name = "Core #6 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 274, new SmuSensorType { Name = "Core #7 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 275, new SmuSensorType { Name = "Core #8 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 276, new SmuSensorType { Name = "Core #9 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 277, new SmuSensorType { Name = "Core #10 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 278, new SmuSensorType { Name = "Core #11 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 279, new SmuSensorType { Name = "Core #12 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 280, new SmuSensorType { Name = "Core #13 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 281, new SmuSensorType { Name = "Core #14 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 282, new SmuSensorType { Name = "Core #15 (Effective)", Type = SensorType.Clock, Scale = 1000 } },
                //{ 283, new SmuSensorType { Name = "Core #16 (Effective)", Type = SensorType.Clock, Scale = 1000 } }
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

    private uint _pmTableSize;
    private uint _pmTableSizeAlt;
    private uint _pmTableVersion;
    private uint _dramBaseAddr;

    private readonly RyzenSmu _ryzenSmu;

    public RyzenSMU()
    {
        try
        {
            _ryzenSmu = new RyzenSmu();

            _cpuCodeName = (CpuCodeName)_ryzenSmu.GetCodeName();

            _ryzenSmu.ResolvePmTable(out _pmTableVersion, out _dramBaseAddr);

            SetupPmTableSize();

            _supportedCPU = true;
        }
        catch (Exception e)
        {
            _supportedCPU = false;
            _unsupportedCPUException = e;
        }
    }

    public void Close() => _ryzenSmu.Close();

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

            try
            {
                float[] pm_values = UpdateAndReadDram();
                r.AppendLine("  Idx    Offset   Value");
                r.AppendLine(" ------------------------");
                for (int i = 0; i < pm_values.Length; i++)
                {
                    r.AppendLine($" {i,4}    0x{i * 4:X3}    {pm_values[i]}");
                }
            }
            catch (Exception e)
            {
                r.AppendLine($" Exception: {e.Message}");
            }
        }
        else
        {
            r.AppendLine($" Initialization exception: {_unsupportedCPUException.Message}");
        }

        return r.ToString();
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
        if (!_supportedCPU)
            return [0];

        float[] table = null;
        for (int tries_left = 2; tries_left != 0; --tries_left)
        {
            table = null;
            try
            {
                table = UpdateAndReadDram();
            }
            catch
            {
                // ignored
            }

            if (table is { Length: > 0 } && table[0] != 0)
            {
                return table;
            }
        }

        return table is { Length: > 0 } ? table : [0];
    }

    private float[] UpdateAndReadDram()
    {
        float[] table = new float[_pmTableSize / 4];

        _ryzenSmu.UpdatePmTable();
        long[] read = _ryzenSmu.ReadPmTable((int)((_pmTableSize + 7) / 8));
        Buffer.BlockCopy(read, 0, table, 0, (int)_pmTableSize);

        return table;
    }

    private void SetupPmTableSize()
    {
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
            case CpuCodeName.GraniteRidge:
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

    public struct SmuSensorType
    {
        public string Name;
        public SensorType Type;
        public float Scale;
    }


    private enum CpuCodeName
    {
        Undefined = -1,
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
        Raphael,
        GraniteRidge
    }
}
