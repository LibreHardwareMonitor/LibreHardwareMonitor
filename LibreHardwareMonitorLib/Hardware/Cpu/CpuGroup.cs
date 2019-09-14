// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.CPU
{
    internal class CpuGroup : IGroup
    {
        private readonly List<GenericCpu> _hardware = new List<GenericCpu>();
        private readonly CpuID[][][] _threads;

        private static CpuID[][] GetProcessorThreads()
        {
            List<CpuID> threads = new List<CpuID>();
            for (int i = 0; i < 64; i++)
            {
                try
                {
                    threads.Add(new CpuID(i));
                }
                catch (ArgumentOutOfRangeException)
                {
                    // All cores found.
                    break;
                }
            }

            SortedDictionary<uint, List<CpuID>> processors = new SortedDictionary<uint, List<CpuID>>();
            foreach (CpuID thread in threads)
            {
                List<CpuID> list;
                processors.TryGetValue(thread.ProcessorId, out list);
                if (list == null)
                {
                    list = new List<CpuID>();
                    processors.Add(thread.ProcessorId, list);
                }
                list.Add(thread);
            }

            CpuID[][] processorThreads = new CpuID[processors.Count][];
            int index = 0;
            foreach (List<CpuID> list in processors.Values)
            {
                processorThreads[index] = list.ToArray();
                index++;
            }
            return processorThreads;
        }

        private static CpuID[][] GroupThreadsByCore(IEnumerable<CpuID> threads)
        {

            SortedDictionary<uint, List<CpuID>> cores = new SortedDictionary<uint, List<CpuID>>();
            foreach (CpuID thread in threads)
            {
                List<CpuID> coreList;
                cores.TryGetValue(thread.CoreId, out coreList);
                if (coreList == null)
                {
                    coreList = new List<CpuID>();
                    cores.Add(thread.CoreId, coreList);
                }
                coreList.Add(thread);
            }

            CpuID[][] coreThreads = new CpuID[cores.Count][];
            int index = 0;
            foreach (List<CpuID> list in cores.Values)
            {
                coreThreads[index] = list.ToArray();
                index++;
            }
            return coreThreads;
        }

        public CpuGroup(ISettings settings)
        {
            CpuID[][] processorThreads = GetProcessorThreads();
            _threads = new CpuID[processorThreads.Length][][];

            int index = 0;
            foreach (CpuID[] threads in processorThreads)
            {
                if (threads.Length == 0)
                    continue;

                CpuID[][] coreThreads = GroupThreadsByCore(threads);
                _threads[index] = coreThreads;

                switch (threads[0].Vendor)
                {
                    case Vendor.Intel:
                        _hardware.Add(new IntelCpu(index, coreThreads, settings));
                        break;
                    case Vendor.AMD:
                        switch (threads[0].Family)
                        {
                            case 0x0F:
                                _hardware.Add(new Amd0FCpu(index, coreThreads, settings));
                                break;
                            case 0x10:
                            case 0x11:
                            case 0x12:
                            case 0x14:
                            case 0x15:
                            case 0x16:
                                _hardware.Add(new Amd10Cpu(index, coreThreads, settings));
                                break;
                            case 0x17:
                                _hardware.Add(new Amd17Cpu(index, coreThreads, settings));
                                break;
                            default:
                                _hardware.Add(new GenericCpu(index, coreThreads, settings));
                                break;
                        }
                        break;
                    default:
                        _hardware.Add(new GenericCpu(index, coreThreads, settings));
                        break;
                }
                index++;
            }
        }

        public IEnumerable<IHardware> Hardware => _hardware;

        private static void AppendCpuidData(StringBuilder r, uint[,] data, uint offset)
        {
            for (int i = 0; i < data.GetLength(0); i++)
            {
                r.Append(" ");
                r.Append((i + offset).ToString("X8", CultureInfo.InvariantCulture));
                for (int j = 0; j < 4; j++)
                {
                    r.Append("  ");
                    r.Append(data[i, j].ToString("X8", CultureInfo.InvariantCulture));
                }
                r.AppendLine();
            }
        }

        public string GetReport()
        {
            if (_threads == null)
                return null;

            StringBuilder r = new StringBuilder();
            r.AppendLine("CPUID");
            r.AppendLine();
            for (int i = 0; i < _threads.Length; i++)
            {

                r.AppendLine("Processor " + i);
                r.AppendLine();
                r.AppendFormat("Processor Vendor: {0}{1}", _threads[i][0][0].Vendor, Environment.NewLine);
                r.AppendFormat("Processor Brand: {0}{1}", _threads[i][0][0].BrandString, Environment.NewLine);
                r.AppendFormat("Family: 0x{0}{1}", _threads[i][0][0].Family.ToString("X", CultureInfo.InvariantCulture), Environment.NewLine);
                r.AppendFormat("Model: 0x{0}{1}", _threads[i][0][0].Model.ToString("X", CultureInfo.InvariantCulture), Environment.NewLine);
                r.AppendFormat("Stepping: 0x{0}{1}", _threads[i][0][0].Stepping.ToString("X", CultureInfo.InvariantCulture), Environment.NewLine);
                r.AppendLine();

                r.AppendLine("CPUID Return Values");
                r.AppendLine();
                for (int j = 0; j < _threads[i].Length; j++)
                {
                    for (int k = 0; k < _threads[i][j].Length; k++)
                    {
                        r.AppendLine(" CPU Thread: " + _threads[i][j][k].Thread);
                        r.AppendLine(" APIC ID: " + _threads[i][j][k].ApicId);
                        r.AppendLine(" Processor ID: " + _threads[i][j][k].ProcessorId);
                        r.AppendLine(" Core ID: " + _threads[i][j][k].CoreId);
                        r.AppendLine(" Thread ID: " + _threads[i][j][k].ThreadId);
                        r.AppendLine();
                        r.AppendLine(" Function  EAX       EBX       ECX       EDX");
                        AppendCpuidData(r, _threads[i][j][k].Data, CpuID.CPUID_0);
                        AppendCpuidData(r, _threads[i][j][k].ExtData, CpuID.CPUID_EXT);
                        r.AppendLine();
                    }
                }
            }
            return r.ToString();
        }

        public void Close()
        {
            foreach (GenericCpu cpu in _hardware)
            {
                cpu.Close();
            }
        }
    }
}
