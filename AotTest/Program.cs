// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

// Minimal AOT compilation test - verifies that LibreHardwareMonitorLib can be published as native AOT.

using System;
using LibreHardwareMonitor.Hardware;

Console.WriteLine("AOT compilation test started.");

// Verify core types are accessible and constructible under AOT.
var settings = new TestSettings();
var computer = new Computer
{
    IsCpuEnabled = true,
    IsGpuEnabled = true,
    IsMemoryEnabled = true,
    IsMotherboardEnabled = true,
    IsStorageEnabled = true,
    IsNetworkEnabled = true,
    IsControllerEnabled = true,
    IsPsuEnabled = true,
    IsBatteryEnabled = true
};

Console.WriteLine($"Computer instance created: {computer.GetType().Name}");
Console.WriteLine("AOT compilation test passed.");
return 0;

internal sealed class TestSettings : ISettings
{
    public bool Contains(string name) => false;
    public void SetValue(string name, string value) { }
    public string GetValue(string name, string value) => value;
    public void Remove(string name) { }
}
