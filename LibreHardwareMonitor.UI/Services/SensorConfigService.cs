using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI.Services;

/// <summary>
/// Persists sensor visibility and custom names to a JSON file.
/// Supports import/export of naming profiles for sharing.
/// </summary>
public sealed class SensorConfigService
{
    private readonly string _configPath;
    private SensorConfigData _data = new();

    public SensorConfigService()
    {
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LibreHardwareMonitor.UI");
        Directory.CreateDirectory(appData);
        _configPath = Path.Combine(appData, "sensor-config.json");
        Load();
    }

    /// <summary>Returns true if the sensor should be shown in the Hardware tab.</summary>
    public bool IsSensorVisible(string identifier)
    {
        return !_data.HiddenSensors.Contains(identifier);
    }

    /// <summary>Returns true if the entire sensor group is hidden for a hardware device.</summary>
    public bool IsGroupVisible(string hardwareId, string groupKey)
    {
        string key = $"{hardwareId}|{groupKey}";
        return !_data.HiddenGroups.Contains(key);
    }

    /// <summary>Returns custom name or null if not renamed.</summary>
    public string? GetCustomName(string identifier)
    {
        _data.CustomNames.TryGetValue(identifier, out string? name);
        return name;
    }

    /// <summary>Returns all custom names.</summary>
    public IReadOnlyDictionary<string, string> GetAllCustomNames()
    {
        return _data.CustomNames;
    }

    public void SetSensorVisible(string identifier, bool visible)
    {
        if (visible)
            _data.HiddenSensors.Remove(identifier);
        else
            _data.HiddenSensors.Add(identifier);
    }

    public void SetGroupVisible(string hardwareId, string groupKey, bool visible)
    {
        string key = $"{hardwareId}|{groupKey}";
        if (visible)
            _data.HiddenGroups.Remove(key);
        else
            _data.HiddenGroups.Add(key);
    }

    public void SetCustomName(string identifier, string? customName)
    {
        if (string.IsNullOrWhiteSpace(customName))
            _data.CustomNames.Remove(identifier);
        else
            _data.CustomNames[identifier] = customName;
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_data, options);
            File.WriteAllText(_configPath, json);
        }
        catch
        {
            // Silently fail — config is non-critical
        }
    }

    /// <summary>
    /// Export sensor naming profile for a specific hardware device.
    /// Includes all sensors (renamed ones get CustomName, others get OriginalName as reference).
    /// </summary>
    public string ExportDeviceNamingProfile(IHardware hardware)
    {
        var entries = new List<SensorNamingEntry>();
        ExportHardwareSensors(hardware, entries);
        foreach (IHardware sub in hardware.SubHardware)
            ExportHardwareSensors(sub, entries);

        var profile = new SensorNamingProfile
        {
            DeviceName = hardware.Name,
            DeviceType = hardware.HardwareType.ToString(),
            Description = $"Sensor names for {hardware.Name}",
            ExportDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            Entries = entries
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(profile, options);
    }

    private void ExportHardwareSensors(IHardware hw, List<SensorNamingEntry> entries)
    {
        foreach (ISensor sensor in hw.Sensors)
        {
            string id = sensor.Identifier.ToString();
            string? customName = GetCustomName(id);

            entries.Add(new SensorNamingEntry
            {
                Identifier = id,
                OriginalName = sensor.Name,
                CustomName = customName ?? "",
                Hardware = hw.Name,
                HardwareType = hw.HardwareType.ToString(),
                SensorType = sensor.SensorType.ToString()
            });
        }
    }

    /// <summary>
    /// Import sensor naming profile from JSON.
    /// Returns the number of names applied.
    /// </summary>
    public int ImportNamingProfile(string json)
    {
        var profile = JsonSerializer.Deserialize<SensorNamingProfile>(json);
        if (profile?.Entries == null || profile.Entries.Count == 0)
            return 0;

        int applied = 0;
        foreach (var entry in profile.Entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.Identifier) &&
                !string.IsNullOrWhiteSpace(entry.CustomName))
            {
                SetCustomName(entry.Identifier, entry.CustomName);
                applied++;
            }
        }

        return applied;
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                _data = JsonSerializer.Deserialize<SensorConfigData>(json) ?? new SensorConfigData();
            }
        }
        catch
        {
            _data = new SensorConfigData();
        }
    }

    public bool UseFahrenheit
    {
        get => _data.UseFahrenheit;
        set { _data.UseFahrenheit = value; Save(); }
    }

    public bool MinimizeToTray
    {
        get => _data.MinimizeToTray;
        set { _data.MinimizeToTray = value; Save(); }
    }

    private sealed class SensorConfigData
    {
        public HashSet<string> HiddenSensors { get; set; } = new();
        public HashSet<string> HiddenGroups { get; set; } = new();
        public Dictionary<string, string> CustomNames { get; set; } = new();
        public bool UseFahrenheit { get; set; }
        public bool MinimizeToTray { get; set; } = true;
    }
}

/// <summary>Shareable naming profile format — per device.</summary>
public sealed class SensorNamingProfile
{
    public string DeviceName { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string Description { get; set; } = "";
    public string ExportDate { get; set; } = "";
    public List<SensorNamingEntry> Entries { get; set; } = new();
}

/// <summary>A single sensor naming entry in an import/export profile.</summary>
public sealed class SensorNamingEntry
{
    public string Identifier { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public string CustomName { get; set; } = "";
    public string Hardware { get; set; } = "";
    public string HardwareType { get; set; } = "";
    public string SensorType { get; set; } = "";
}
