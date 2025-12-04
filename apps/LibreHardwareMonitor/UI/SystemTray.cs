// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI;

public class SystemTray : IDisposable
{
    private IComputer _computer;
    private readonly PersistentSettings _settings;
    private readonly UnitManager _unitManager;
    private readonly List<SensorNotifyIcon> _sensorList = new List<SensorNotifyIcon>();
    private bool _mainIconEnabled;
    private readonly NotifyIconAdv _mainIcon;

    public SystemTray(IComputer computer, PersistentSettings settings, UnitManager unitManager)
    {
        _computer = computer;
        _settings = settings;
        _unitManager = unitManager;
        computer.HardwareAdded += HardwareAdded;
        computer.HardwareRemoved += HardwareRemoved;

        _mainIcon = new NotifyIconAdv();

        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        ToolStripItem hideShowItem = new ToolStripMenuItem("Hide/Show");
        hideShowItem.Click += delegate
        {
            SendHideShowCommand();
        };
        contextMenuStrip.Items.Add(hideShowItem);
        contextMenuStrip.Items.Add(new ToolStripSeparator());
        ToolStripItem exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += delegate
        {
            SendExitCommand();
        };
        contextMenuStrip.Items.Add(exitItem);
        _mainIcon.ContextMenuStrip = contextMenuStrip;
        _mainIcon.DoubleClick += delegate
        {
            SendHideShowCommand();
        };
        _mainIcon.Icon = EmbeddedResources.GetIcon("smallicon.ico");
        _mainIcon.Text = "Libre Hardware Monitor";
    }

    private void HardwareRemoved(IHardware hardware)
    {
        hardware.SensorAdded -= SensorAdded;
        hardware.SensorRemoved -= SensorRemoved;

        foreach (ISensor sensor in hardware.Sensors)
            SensorRemoved(sensor);

        foreach (IHardware subHardware in hardware.SubHardware)
            HardwareRemoved(subHardware);
    }

    private void HardwareAdded(IHardware hardware)
    {
        foreach (ISensor sensor in hardware.Sensors)
            SensorAdded(sensor);

        hardware.SensorAdded += SensorAdded;
        hardware.SensorRemoved += SensorRemoved;

        foreach (IHardware subHardware in hardware.SubHardware)
            HardwareAdded(subHardware);
    }

    private void SensorAdded(ISensor sensor)
    {
        if (_settings.GetValue(new Identifier(sensor.Identifier, "tray").ToString(), false))
            Add(sensor, false);
    }

    private void SensorRemoved(ISensor sensor)
    {
        if (Contains(sensor))
            Remove(sensor, false);
    }

    public void Dispose()
    {
        foreach (SensorNotifyIcon icon in _sensorList)
            icon.Dispose();
        _mainIcon.Dispose();
    }

    public void Redraw()
    {
        foreach (SensorNotifyIcon icon in _sensorList)
            icon.Update();
    }

    public bool Contains(ISensor sensor)
    {
        foreach (SensorNotifyIcon icon in _sensorList)
            if (icon.Sensor == sensor)
                return true;
        return false;
    }

    public void Add(ISensor sensor, bool balloonTip)
    {
        if (Contains(sensor))
            return;


        _sensorList.Add(new SensorNotifyIcon(this, sensor, _settings, _unitManager));
        UpdateMainIconVisibility();
        _settings.SetValue(new Identifier(sensor.Identifier, "tray").ToString(), true);
    }

    public void Remove(ISensor sensor)
    {
        Remove(sensor, true);
    }

    private void Remove(ISensor sensor, bool deleteConfig)
    {
        if (deleteConfig)
        {
            _settings.Remove(new Identifier(sensor.Identifier, "tray").ToString());
            _settings.Remove(new Identifier(sensor.Identifier, "traycolor").ToString());
        }
        SensorNotifyIcon instance = null;
        foreach (SensorNotifyIcon icon in _sensorList)
        {
            if (icon.Sensor == sensor)
                instance = icon;
        }
        if (instance != null)
        {
            _sensorList.Remove(instance);
            UpdateMainIconVisibility();
            instance.Dispose();
        }
    }

    public event EventHandler HideShowCommand;

    public void SendHideShowCommand()
    {
        HideShowCommand?.Invoke(this, null);
    }

    public event EventHandler ExitCommand;

    public void SendExitCommand()
    {
        ExitCommand?.Invoke(this, null);
    }

    private void UpdateMainIconVisibility()
    {
        if (_mainIconEnabled)
            _mainIcon.Visible = _sensorList.Count == 0;
        else
            _mainIcon.Visible = false;
    }

    public bool IsMainIconEnabled
    {
        get { return _mainIconEnabled; }
        set
        {
            if (_mainIconEnabled != value)
            {
                _mainIconEnabled = value;
                UpdateMainIconVisibility();
            }
        }
    }
}