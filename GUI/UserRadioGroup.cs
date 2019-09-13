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
            this._settings = settings;
            this._name = name;
            if (name != null)
                this._value = settings.GetValue(name, value);
            else
                this._value = value;
            this._menuItems = menuItems;
            this._value = Math.Max(Math.Min(this._value, menuItems.Length - 1), 0);

            for (int i = 0; i < this._menuItems.Length; i++)
            {
                this._menuItems[i].Checked = i == this._value;
                int index = i;
                this._menuItems[i].Click += delegate (object sender, EventArgs e)
                {
                    this.Value = index;
                };
            }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                if (this._value != value)
                {
                    this._value = value;
                    if (this._name != null)
                        _settings.SetValue(_name, value);
                    for (int i = 0; i < this._menuItems.Length; i++)
                        this._menuItems[i].Checked = i == value;
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
