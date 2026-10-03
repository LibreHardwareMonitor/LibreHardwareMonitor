// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using BlackSharp.Core.Interop.Windows.Native;
using HidSharp;
using HidSharp.Reports;
using LibreHardwareMonitor.Interop;
using LibreHardwareMonitor.Hardware.KontronDll;
using static LibreHardwareMonitor.Hardware.KontronDll.KontronDllHandler;

namespace LibreHardwareMonitor.Hardware.KontronWatchdog;

public class KontronWatchdogGroup : IGroup
{
    private readonly List<IHardware> _hardware = new();
    private static readonly StringBuilder _report = new();

    private readonly KontronDllHandler _dllHandler = new KontronDllHandler(_report);

    public KontronWatchdogGroup(ISettings settings)
    {
        _report.AppendLine("Kontron Watchdog hardware group:");
        _report.AppendLine();

        // Check KSC prerequisites ...
        _report.AppendLine("DllHandler > Check KSC prerequisites:");
        if (_dllHandler.CheckDllPrerequisites() == false)
        {
            _report.AppendLine("> Check KSC prerequisites failed!");
            _report.AppendLine();
            return;
        }

        // DllHandler > Open KSC DLL 
        _report.Append("DllHandler > Open KSC DLL .. ");
        if (_dllHandler.OpenKscDll(LhmKscHardwareGroup.LHM_KSC_HWGROUP_WATCHDOG) == false)
        {
            _report.AppendLine("failed!");
            _report.AppendLine();
            return;
        }
        _report.AppendLine("ok.");

        // DllHandler > Query KSC versions
        _report.Append("DllHandler > Query KSC versions:");
        _report.AppendLine();
        if (_dllHandler.KscDllQueryVersions() == false)
        {
            _report.AppendLine("Query KSC DLL versions failed!");
            _report.AppendLine();
            return;
        }

        _report.AppendLine();
        _report.AppendLine("> A Kontron Controller was found.");
        _report.AppendLine();

        _report.Append("Create a Kontron Watchdog class instance .. ");
        _hardware.Add(new KontronWatchdog(_dllHandler.kontronKscDll,
                                          settings));

        if (_hardware.Count == 0)
        {
            _report.AppendLine("failed!");
            _report.AppendLine();
        }
        else
        {
            _report.AppendLine("ok.");
            _report.AppendLine();
        }
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public void Close()
    {
        foreach (IHardware iHardware in _hardware)
        {
            if (iHardware is Hardware hardware)
                hardware.Close();
        }

        // DllHandler > Close KSC DLL 
        _report.Append("DllHandler > Close KSC DLL .. ");
        if (_dllHandler.CloseKscDll(LhmKscHardwareGroup.LHM_KSC_HWGROUP_WATCHDOG) == false)
        {
            _report.AppendLine("failed!");
            _report.AppendLine();
            return;
        }
        _report.AppendLine("ok.");
    }

    public string GetReport()
    {
        return _report.ToString();
    }
}
