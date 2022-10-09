// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Windows.Forms;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI;

public class UserOption
{
    private readonly string _name;
    private bool _value;
    private readonly ToolStripMenuItem _menuItem;
    private event EventHandler _changed;
    private readonly PersistentSettings _settings;

    public UserOption(string name, bool value, ToolStripMenuItem menuItem, PersistentSettings settings)
    {
        _settings = settings;
        _name = name;
        _value = name != null ? settings.GetValue(name, value) : value;
        _menuItem = menuItem;
        _menuItem.Checked = _value;
        _menuItem.Click += MenuItem_Click;
    }

    private void MenuItem_Click(object sender, EventArgs e)
    {
        Value = !Value;
    }

    public bool Value
    {
        get { return _value; }
        set
        {
            if (_value != value)
            {
                _value = value;
                if (_name != null)
                    _settings.SetValue(_name, value);
                _menuItem.Checked = value;
                _changed?.Invoke(this, null);
            }
        }
    }

    public event EventHandler Changed
    {
        add
        {
            _changed += value;
            _changed?.Invoke(this, null);
        }
        remove
        {
            _changed -= value;
        }
    }
}