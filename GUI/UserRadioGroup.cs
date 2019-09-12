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
        private string name;
        private int value;
        private MenuItem[] menuItems;
        private event EventHandler changed;
        private PersistentSettings settings;

        public UserRadioGroup(string name, int value,
          MenuItem[] menuItems, PersistentSettings settings)
        {
            this.settings = settings;
            this.name = name;
            if (name != null)
                this.value = settings.GetValue(name, value);
            else
                this.value = value;
            this.menuItems = menuItems;
            this.value = Math.Max(Math.Min(this.value, menuItems.Length - 1), 0);

            for (int i = 0; i < this.menuItems.Length; i++)
            {
                this.menuItems[i].Checked = i == this.value;
                int index = i;
                this.menuItems[i].Click += delegate (object sender, EventArgs e)
                {
                    this.Value = index;
                };
            }
        }

        public int Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    if (this.name != null)
                        settings.SetValue(name, value);
                    for (int i = 0; i < this.menuItems.Length; i++)
                        this.menuItems[i].Checked = i == value;
                    if (changed != null)
                        changed(this, null);
                }
            }
        }

        public event EventHandler Changed
        {
            add
            {
                changed += value;
                if (changed != null)
                    changed(this, null);
            }
            remove
            {
                changed -= value;
            }
        }
    }
}
