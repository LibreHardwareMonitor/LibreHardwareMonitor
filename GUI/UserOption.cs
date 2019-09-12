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
        private string name;
        private bool value;
        private MenuItem menuItem;
        private event EventHandler changed;
        private PersistentSettings settings;

        public UserOption(string name, bool value, MenuItem menuItem, PersistentSettings settings)
        {
            this.settings = settings;
            this.name = name;
            if (name != null)
                this.value = settings.GetValue(name, value);
            else
                this.value = value;
            this.menuItem = menuItem;
            this.menuItem.Checked = this.value;
            this.menuItem.Click += new EventHandler(menuItem_Click);
        }

        private void menuItem_Click(object sender, EventArgs e)
        {
            this.Value = !this.Value;
        }

        public bool Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    if (this.name != null)
                        settings.SetValue(name, value);
                    this.menuItem.Checked = value;
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
