﻿// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;

namespace LibreHardwareMonitor.Hardware.Storage
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class NamePrefixAttribute : Attribute
    {
        public NamePrefixAttribute(string namePrefix)
        {
            Prefix = namePrefix;
        }

        public string Prefix { get; }
    }
}
