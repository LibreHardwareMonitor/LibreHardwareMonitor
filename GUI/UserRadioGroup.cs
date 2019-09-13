// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using OpenHardwareMonitor.Utilities;

namespace OpenHardwareMonitor.GUI
{
    public class UserRadioGroup
    {
        private string _name;
        private int _value;
        private MenuItem[] _menuItems;
        private event EventHandler _changed;
        private PersistentSettings _settings;

        public UserRadioGroup(string name, int value, MenuItem[] menuItems, PersistentSettings settings)
        {
            _settings = settings;
            _name = name;
            if (name != null)
                _value = settings.GetValue(name, value);
            else
                _value = value;
            _menuItems = menuItems;
            _value = Math.Max(Math.Min(_value, menuItems.Length - 1), 0);

            for (int i = 0; i < _menuItems.Length; i++)
            {
                _menuItems[i].Checked = i == _value;
                int index = i;
                _menuItems[i].Click += delegate (object sender, EventArgs e)
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
                    if (_changed != null)
                        _changed(this, null);
                }
            }
        }

        public event EventHandler Changed
        {
            add
            {
                _changed += value;
                if (_changed != null)
                    _changed(this, null);
            }
            remove
            {
                _changed -= value;
            }
        }
    }
}
