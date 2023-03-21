// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Cpu;

internal class CpuGroup : IGroup
{
    private readonly List<GenericCpu> _hardware = new();
    private readonly CpuId[][][] _threads;

    public CpuGroup(ISettings settings)
    {
        CpuId[][] processorThreads = GetProcessorThreads();
        _threads = new CpuId[processorThreads.Length][][];

        int index = 0;
        foreach (CpuId[] threads in processorThreads)
        {
            if (threads.Length == 0)
                continue;

            CpuId[][] coreThreads = GroupThreadsByCore(threads);
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
                        case 0x19:
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

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        if (_threads == null)
            return null;

        StringBuilder r = new();
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
                    r.AppendLine(" CPU Group: " + _threads[i][j][k].Group);
                    r.AppendLine(" CPU Thread: " + _threads[i][j][k].Thread);
                    r.AppendLine(" APIC ID: " + _threads[i][j][k].ApicId);
                    r.AppendLine(" Processor ID: " + _threads[i][j][k].ProcessorId);
                    r.AppendLine(" Core ID: " + _threads[i][j][k].CoreId);
                    r.AppendLine(" Thread ID: " + _threads[i][j][k].ThreadId);
                    r.AppendLine();
                    r.AppendLine(" Function  EAX       EBX       ECX       EDX");
                    AppendCpuidData(r, _threads[i][j][k].Data, CpuId.CPUID_0);
                    AppendCpuidData(r, _threads[i][j][k].ExtData, CpuId.CPUID_EXT);
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

    private static CpuId[][] GetProcessorThreads()
    {
        List<CpuId> threads = new();

        for (int i = 0; i < ThreadAffinity.ProcessorGroupCount; i++)
        {
            for (int j = 0; j < 192; j++)
            {
                try
                {
                    var cpuid = CpuId.Get(i, j);
                    if (cpuid != null)
                        threads.Add(cpuid);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Continue...
                }
            }
        }

        SortedDictionary<uint, List<CpuId>> processors = new();
        foreach (CpuId thread in threads)
        {
            processors.TryGetValue(thread.ProcessorId, out List<CpuId> list);
            if (list == null)
            {
                list = new List<CpuId>();
                processors.Add(thread.ProcessorId, list);
            }

            list.Add(thread);
        }

        CpuId[][] processorThreads = new CpuId[processors.Count][];
        int index = 0;
        foreach (List<CpuId> list in processors.Values)
        {
            processorThreads[index] = list.ToArray();
            index++;
        }

        return processorThreads;
    }

    private static CpuId[][] GroupThreadsByCore(IEnumerable<CpuId> threads)
    {
        SortedDictionary<uint, List<CpuId>> cores = new();
        foreach (CpuId thread in threads)
        {
            cores.TryGetValue(thread.CoreId, out List<CpuId> coreList);
            if (coreList == null)
            {
                coreList = new List<CpuId>();
                cores.Add(thread.CoreId, coreList);
            }

            coreList.Add(thread);
        }

        CpuId[][] coreThreads = new CpuId[cores.Count][];
        int index = 0;
        foreach (List<CpuId> list in cores.Values)
        {
            coreThreads[index] = list.ToArray();
            index++;
        }

        return coreThreads;
    }

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
}
