// ported from: https://gitlab.com/leogx9r/ryzen_smu
// and: https://github.com/irusanov/SMUDebugTool

using LibreHardwareMonitor.Hardware.CPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware
{
    internal class RyzenSMU
    {
        private Mutex _mutex = new Mutex();
        private uint _dram_base_addr = 0;
        private uint _pm_table_version = 0;
        private uint _pm_table_size = 0;
        private uint _pm_table_size_alt = 0;
        private bool _supported_cpu = false;

        private const uint SMU_RETRIES_MAX = 8096;
        private const byte SMU_PCI_ADDR_REG = 0xC4;
        private const byte SMU_PCI_DATA_REG = 0xC8;
        private const uint SMU_REQ_MAX_ARGS = 6;

        private CpuCodeName _cpu_code_name;
        private uint _cmd_addr;
        private uint _rsp_addr;
        private uint _args_addr;

        public RyzenSMU(uint family, uint model, uint packageType)
        {
            _cpu_code_name = GetCpuCodeName(family, model, packageType);

            _supported_cpu = SetAddrs(_cpu_code_name);

            SetupPmTableAddrAndSize();
        }

        private CpuCodeName GetCpuCodeName(uint family, uint model, uint packageType)
        {
            if (family == 0x17)
            {
                switch (model)
                {
                    case 0x01:
                        {
                            if (packageType == 7)
                                return CpuCodeName.Threadripper;
                            else
                                return CpuCodeName.SummitRidge;
                        }
                    case 0x08:
                        {
                            if (packageType == 7)
                                return CpuCodeName.Colfax;
                            else
                                return CpuCodeName.PinnacleRidge;
                        }
                    case 0x11:
                        return CpuCodeName.RavenRidge;
                    case 0x18:
                        {
                            if (packageType == 2)
                                return CpuCodeName.RavenRidge2;
                            else
                                return CpuCodeName.Picasso;
                        }
                    case 0x20:
                        return CpuCodeName.Dali;
                    case 0x31:
                        return CpuCodeName.CastlePeak;
                    case 0x60:
                        return CpuCodeName.Renoir;
                    case 0x71:
                        return CpuCodeName.Matisse;
                    case 0x90:
                        return CpuCodeName.Vangogh;
                    default:
                        return CpuCodeName.Undefined;
                }
            }
            else if (family == 0x19)
            {
                switch (model)
                {
                    case 0x00:
                        return CpuCodeName.Milan;
                    case 0x20:
                    case 0x21:
                        return CpuCodeName.Vermeer;
                    case 0x40:
                        return CpuCodeName.Rembrandt;
                    case 0x50:
                        return CpuCodeName.Cezanne;
                    default:
                        return CpuCodeName.Undefined;
                }
            }

            return CpuCodeName.Undefined;
        }

        public string GetReport()
        {
            StringBuilder r = new StringBuilder();

            r.AppendLine("Ryzen SMU");
            r.AppendLine();
            r.AppendLine($" PM table version: 0x{_pm_table_version:X8}");
            r.AppendLine($" PM table supported: {_supported_cpu}");
            r.AppendLine($" PM table layout defined: {IsPmTableLayoutDefined()}");
            if (_supported_cpu)
            {
                r.AppendLine($" PM table size: 0x{_pm_table_size:X3}");
                r.AppendLine($" PM table start address: 0x{_dram_base_addr:X8}");
                r.AppendLine();
                r.AppendLine($" PM table dump:");
                r.AppendLine($"  Idx    Offset   Value");
                r.AppendLine($" ------------------------");
                var pm_values = GetPmTable();
                for (var i = 0; i < pm_values.Length; i++)
                {
                    r.AppendLine($" {i,4}    0x{i * 4:X3}    {pm_values[i]}");
                }
            }


            return r.ToString();
        }

        private bool SetAddrs(CpuCodeName codeName)
        {
            switch (codeName)
            {
                case CpuCodeName.CastlePeak:
                case CpuCodeName.Matisse:
                case CpuCodeName.Vermeer:
                    _cmd_addr = 0x3B10524;
                    _rsp_addr = 0x3B10570;
                    _args_addr = 0x3B10A40;
                    return true;
                case CpuCodeName.Colfax:
                case CpuCodeName.SummitRidge:
                case CpuCodeName.Threadripper:
                case CpuCodeName.PinnacleRidge:
                    _cmd_addr = 0x3B1051C;
                    _rsp_addr = 0x3B10568;
                    _args_addr = 0x3B10590;
                    return true;
                case CpuCodeName.Renoir:
                case CpuCodeName.Picasso:
                case CpuCodeName.RavenRidge:
                case CpuCodeName.RavenRidge2:
                case CpuCodeName.Dali:
                    _cmd_addr = 0x3B10A20;
                    _rsp_addr = 0x3B10A80;
                    _args_addr = 0x3B10A88;
                    return true;
                default:
                    return false;
            }
        }


        public uint GetSmuVersion()
        {
            uint[] args = new uint[] { 1 };

            if (SendCommand(0x02, ref args))
            {
                return args[0];
            }

            return 0;
        }

        public Dictionary<uint, SmuSensorType> GetPmTableStructure()
        {
            if (!IsPmTableLayoutDefined()) return new Dictionary<uint, SmuSensorType>();

            return _supported_pm_table_versions[_pm_table_version];
        }

        public bool IsPmTableLayoutDefined()
        {
            return _supported_pm_table_versions.ContainsKey(_pm_table_version);
        }

        public float[] GetPmTable()
        {
            if (!_supported_cpu) return new float[] { 0 };
            if (!SetupPmTableAddrAndSize()) return new float[] { 0 };
            if (!TransferTableToDram()) return new float[] { 0 };

            return ReadDramToArray();
        }

        private float[] ReadDramToArray()
        {
            float[] table = new float[_pm_table_size / 4];

            for (uint i = 0; i < table.Length; i++)
            {
                Ring0.ReadMemory(_dram_base_addr + i * 4, ref table[i]);
            }

            return table;
        }

        private bool SetupPmTableAddrAndSize()
        {
            if (_pm_table_size == 0)
            {
                SetupPmTableSize();
            }

            if (_dram_base_addr == 0)
            {
                SetupDramBaseAddr();
            }

            if (_dram_base_addr == 0 || _pm_table_size == 0)
            {
                return false;
            }

            return true;
        }

        private bool SetupPmTableSize()
        {
            if (!GetPmTableVersion(ref _pm_table_version)) return false;

            switch (_cpu_code_name)
            {
                case CpuCodeName.Matisse:
                    switch (_pm_table_version)
                    {
                        case 0x240902:
                            _pm_table_size = 0x514;
                            break;
                        case 0x240903:
                            _pm_table_size = 0x518;
                            break;
                        case 0x240802:
                            _pm_table_size = 0x7E0;
                            break;
                        case 0x240803:
                            _pm_table_size = 0x7E4;
                            break;
                        default:
                            return false;
                    }
                    break;
                case CpuCodeName.Vermeer:
                    switch (_pm_table_version)
                    {
                        case 0x2D0903:
                            _pm_table_size = 0x594;
                            break;
                        case 0x380904:
                            _pm_table_size = 0x5A4;
                            break;
                        case 0x2D0803:
                            _pm_table_size = 0x894;
                            break;
                        case 0x380804:
                            _pm_table_size = 0x8A4;
                            break;
                        default:
                            return false;
                    }
                    break;
                case CpuCodeName.Renoir:
                    switch (_pm_table_version)
                    {
                        case 0x370000:
                            _pm_table_size = 0x794;
                            break;
                        case 0x370001:
                            _pm_table_size = 0x884;
                            break;
                        case 0x370002:
                        case 0x370003:
                            _pm_table_size = 0x88C;
                            break;
                        case 0x370004:
                            _pm_table_size = 0x8AC;
                            break;
                        case 0x370005:
                            _pm_table_size = 0x8C8;
                            break;
                        default:
                            return false;
                    }
                    break;
                case CpuCodeName.Picasso:
                case CpuCodeName.RavenRidge:
                case CpuCodeName.RavenRidge2:
                    _pm_table_size_alt = 0xA4;
                    _pm_table_size = 0x608 + _pm_table_size_alt;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private bool GetPmTableVersion(ref uint version)
        {
            uint[] args = { 0 };
            uint fn;

            switch (_cpu_code_name)
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
                default:
                    return false;
            }

            bool ret = SendCommand(fn, ref args);
            version = args[0];

            return ret;
        }

        private bool SetupAddrClass1(uint[] fn)
        {
            bool ret;
            uint[] args = { 1, 1 };

            ret = SendCommand(fn[0], ref args);
            if (!ret) return false;

            _dram_base_addr = args[0] | (args[1] << 32);
            return true;
        }

        private bool SetupAddrClass2(uint[] fn)
        {
            bool ret;
            uint[] args = { 0, 0, 0, 0, 0, 0 };

            ret = SendCommand(fn[0], ref args);
            if (!ret) return false;

            args = new uint[] { 0 };
            ret = SendCommand(fn[1], ref args);
            if (!ret) return false;

            _dram_base_addr = args[0];
            return true;
        }

        private bool SetupAddrClass3(uint[] fn)
        {
            bool ret;
            uint[] args;
            uint[] parts = { 0, 0 };

            // == Part 1 ==
            args = new uint[] { 3 };
            ret = SendCommand(fn[0], ref args);
            if (!ret) return false;

            args = new uint[] { 3 };
            ret = SendCommand(fn[2], ref args);
            if (!ret) return false;

            // 1st Base.
            parts[0] = args[0];
            // == Part 1 End ==

            // == Part 2 ==
            args = new uint[] { 3 };
            ret = SendCommand(fn[1], ref args);
            if (!ret) return false;


            args = new uint[] { 5 };
            ret = SendCommand(fn[0], ref args);
            if (!ret) return false;

            args = new uint[] { 5 };
            ret = SendCommand(fn[2], ref args);
            if (!ret) return false;

            // 2nd base.
            parts[1] = args[0];
            // == Part 2 End ==

            _dram_base_addr = parts[0] & 0xFFFFFFFF;
            return true;
        }

        private bool SetupDramBaseAddr()
        {
            uint[] fn = { 0, 0, 0 };

            switch (_cpu_code_name)
            {
                case CpuCodeName.Vermeer:
                case CpuCodeName.Matisse:
                case CpuCodeName.CastlePeak:
                    fn[0] = 0x06;
                    return SetupAddrClass1(fn);
                case CpuCodeName.Renoir:
                    fn[0] = 0x66;
                    return SetupAddrClass1(fn);
                case CpuCodeName.Colfax:
                case CpuCodeName.PinnacleRidge:
                    fn[0] = 0x0b;
                    fn[1] = 0x0c;
                    return SetupAddrClass2(fn);
                case CpuCodeName.Dali:
                case CpuCodeName.Picasso:
                case CpuCodeName.RavenRidge:
                case CpuCodeName.RavenRidge2:
                    fn[0] = 0x0a;
                    fn[1] = 0x3d;
                    fn[2] = 0x0b;
                    return SetupAddrClass3(fn);
                default:
                    return false;
            }
        }

        public bool TransferTableToDram()
        {
            uint[] args = { 0 };
            uint fn;

            switch (_cpu_code_name)
            {
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
                uint retries;

                // Step 1: Wait until the RSP register is non-zero.

                tmp = 0;
                retries = SMU_RETRIES_MAX;
                do
                    if (!ReadReg(_rsp_addr, ref tmp))
                    {
                        _mutex.ReleaseMutex();
                        return false;
                    }
                while (tmp == 0 && 0 != retries--);

                // Step 1.b: A command is still being processed meaning a new command cannot be issued.

                if (retries == 0 && tmp == 0)
                {
                    _mutex.ReleaseMutex();
                    return false;
                }


                // Step 2: Write zero (0) to the RSP register
                WriteReg(_rsp_addr, 0);

                // Step 3: Write the argument(s) into the argument register(s)
                for (int i = 0; i < cmdArgs.Length; ++i)
                    WriteReg(_args_addr + (uint)(i * 4), cmdArgs[i]);

                // Step 4: Write the message Id into the Message ID register
                WriteReg(_cmd_addr, msg);

                // Step 5: Wait until the Response register is non-zero.
                tmp = 0;
                retries = SMU_RETRIES_MAX;
                do
                    if (!ReadReg(_rsp_addr, ref tmp))
                    {
                        _mutex.ReleaseMutex();
                        return false;
                    }
                while (tmp == 0 && 0 != retries--);

                if (retries == 0 && tmp != (uint)Status.OK)
                {
                    _mutex.ReleaseMutex();
                    return false;
                }

                // Step 6: If the Response register contains OK, then SMU has finished processing  the message.

                args = new uint[SMU_REQ_MAX_ARGS];
                for (byte i = 0; i < SMU_REQ_MAX_ARGS; i++)
                {
                    if (!ReadReg(_args_addr + (uint)(i * 4), ref args[i]))
                    {
                        _mutex.ReleaseMutex();
                        return false;
                    }
                }

                ReadReg(_rsp_addr, ref tmp);
                _mutex.ReleaseMutex();
            }

            return tmp == (uint)Status.OK;
        }

        private bool WriteReg(uint addr, uint data)
        {
            bool res = false;
            if (Ring0.WaitPciBusMutex(10))
            {
                if (Ring0.WritePciConfig(0x00, SMU_PCI_ADDR_REG, addr) == true)
                {
                    res = Ring0.WritePciConfig(0x00, SMU_PCI_DATA_REG, data);
                }
            }
            Ring0.ReleasePciBusMutex();
            return res;
        }

        private bool ReadReg(uint addr, ref uint data)
        {
            bool res = false;
            if (Ring0.WaitPciBusMutex(10))
            {
                if (Ring0.WritePciConfig(0x00, SMU_PCI_ADDR_REG, addr) == true)
                {
                    res = Ring0.ReadPciConfig(0x00, SMU_PCI_DATA_REG, out data);
                }
            }
            Ring0.ReleasePciBusMutex();
            return res;
        }

        public struct SmuSensorType
        {
            public string Name;
            public SensorType Type;
        }

        private Dictionary<uint, Dictionary<uint, SmuSensorType>> _supported_pm_table_versions = new Dictionary<uint, Dictionary<uint, SmuSensorType>>()
        {
            {0x001E0004, new Dictionary<uint, SmuSensorType>()
                {
                    // {61, new SmuSensorType() { Name = "CPU Core", Type = SensorType.Voltage } },
                    {62, new SmuSensorType() { Name = "CPU Core", Type = SensorType.Current } },
                    {63, new SmuSensorType() { Name = "CPU Core", Type = SensorType.Power } },
                    // {65, new SmuSensorType() { Name = "CPU SoC", Type = SensorType.Voltage } },
                    {66, new SmuSensorType() { Name = "CPU SoC", Type = SensorType.Current } },
                    {67, new SmuSensorType() { Name = "CPU SoC", Type = SensorType.Power } },
                    // {96, new SmuSensorType() { Name = "Core #1", Type = SensorType.Power } },
                    // {97, new SmuSensorType() { Name = "Core #2", Type = SensorType.Power } },
                    // {98, new SmuSensorType() { Name = "Core #3", Type = SensorType.Power } },
                    // {99, new SmuSensorType() { Name = "Core #4", Type = SensorType.Power } },
                    {108, new SmuSensorType() { Name = "Core #1", Type = SensorType.Temperature } },
                    {109, new SmuSensorType() { Name = "Core #2", Type = SensorType.Temperature } },
                    {110, new SmuSensorType() { Name = "Core #3", Type = SensorType.Temperature } },
                    {111, new SmuSensorType() { Name = "Core #4", Type = SensorType.Temperature } },
                    {150, new SmuSensorType() { Name = "CPU GFX", Type = SensorType.Voltage} },
                    {151, new SmuSensorType() { Name = "CPU GFX", Type = SensorType.Temperature} },
                    {154, new SmuSensorType() { Name = "CPU GFX", Type = SensorType.Clock } },
                    {156, new SmuSensorType() { Name = "CPU GFX", Type = SensorType.Load } },
                    {166, new SmuSensorType() { Name = "FCLK", Type = SensorType.Clock } },
                    {177, new SmuSensorType() { Name = "UCLK", Type = SensorType.Clock } },
                    {178, new SmuSensorType() { Name = "MCLK", Type = SensorType.Clock } },
                    {342, new SmuSensorType() { Name = "Display Count", Type = SensorType.Factor } },
                }
            }
        };

        private enum Status : uint
        {
            OK = 0x01,
            Failed = 0xFF,
            UnknownCmd = 0xFE,
            CmdRejectedPrereq = 0xFD,
            CmdRejectedBusy = 0xFC
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
            Dali
        }
    }
}
