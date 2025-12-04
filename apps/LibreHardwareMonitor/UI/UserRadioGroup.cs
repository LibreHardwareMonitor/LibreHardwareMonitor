// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Windows.Forms;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI;

public class UserRadioGroup
{
    private readonly string _name;
    private int _value;
    private readonly ToolStripMenuItem[] _menuItems;
    private event EventHandler _changed;
    private readonly PersistentSettings _settings;

    public UserRadioGroup(string name, int value, ToolStripMenuItem[] menuItems, PersistentSettings settings)
    {
        _settings = settings;
        _name = name;
        _value = name != null ? settings.GetValue(name, value) : value;
        _menuItems = menuItems;
        _value = Math.Max(Math.Min(_value, menuItems.Length - 1), 0);

        for (int i = 0; i < _menuItems.Length; i++)
        {
            _menuItems[i].Checked = i == _value;
            int index = i;
            _menuItems[i].Click += delegate
            {
                Value = index;
            };
        }
    }

    public int Value
    {
        get { return _value; }
        set
        {
            if (_value != value)
            {
                _value = value;
                if (_name != null)
                    _settings.SetValue(_name, value);
                for (int i = 0; i < _menuItems.Length; i++)
                    _menuItems[i].Checked = i == value;
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