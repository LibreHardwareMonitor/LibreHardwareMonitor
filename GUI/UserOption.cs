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
    public class UserOption
    {
        private string _name;
        private bool _value;
        private MenuItem _menuItem;
        private event EventHandler _changed;
        private PersistentSettings _settings;

        public UserOption(string name, bool value, MenuItem menuItem, PersistentSettings settings)
        {
            _settings = settings;
            _name = name;
            if (name != null)
                _value = settings.GetValue(name, value);
            else
                _value = value;
            _menuItem = menuItem;
            _menuItem.Checked = _value;
            _menuItem.Click += new EventHandler(menuItem_Click);
        }

        private void menuItem_Click(object sender, EventArgs e)
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
