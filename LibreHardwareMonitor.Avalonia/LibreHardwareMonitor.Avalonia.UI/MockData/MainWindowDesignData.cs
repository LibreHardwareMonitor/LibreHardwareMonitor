// SPDX-License-Identifier: MPL-2.0

using LibreHardwareMonitor.Avalonia.Monitoring.Models;
using LibreHardwareMonitor.Avalonia.Monitoring.Services;
using LibreHardwareMonitor.Avalonia.Mvvm.Services;
using LibreHardwareMonitor.Avalonia.Mvvm.ViewModels;
using LibreHardwareMonitor.Hardware;
using LanguageResources = LibreHardwareMonitor.Avalonia.Localization.Resources.Resources;

namespace LibreHardwareMonitor.Avalonia.UI.MockData;

/// <summary>
/// Provides sample hardware and sensor values.
/// </summary>
public static class MainWindowDesignData
{
    /// <summary>
    /// Gets the main window view model used by the XAML designer.
    /// </summary>
    public static MainWindowViewModel MainWindow { get; } = CreateMainWindow();

    /// <summary>
    /// Gets the font service.
    /// </summary>
    public static IFontService FontService { get; } = new MockFontService();

    /// <summary>
    /// Builds the representative computer, hardware and sensor hierarchy.
    /// </summary>
    /// <returns>The populated designer view model.</returns>
    private static MainWindowViewModel CreateMainWindow()
    {
        SensorTreeNodeSnapshot computer = new("computer",
            LanguageResources.MockComputer,
            SensorTreeNodeKind.Computer,
            null,
            null,
            null,
            null,
            null,
            new[]
            {
                CreateHardware("/cpu/0", LanguageResources.MockCpu, HardwareType.Cpu,
                    CreateCategory("/cpu/0", SensorType.Temperature,
                        CreateSensor("/cpu/0/temperature/0", LanguageResources.MockCpuPackage, SensorType.Temperature, 64.2f, 31.5f, 74.8f),
                        CreateSensor("/cpu/0/temperature/1", LanguageResources.MockCpuCore, SensorType.Temperature, 60.7f, 29.3f, 70.1f)),
                    CreateCategory("/cpu/0", SensorType.Load,
                        CreateSensor("/cpu/0/load/0", LanguageResources.MockCpuTotal, SensorType.Load, 22.8f, 2.1f, 68.4f)),
                    CreateCategory("/cpu/0", SensorType.Clock,
                        CreateSensor("/cpu/0/clock/0", LanguageResources.MockCpuCore, SensorType.Clock, 5248.3f, 798.0f, 5487.2f))),
                CreateHardware("/gpu/0", LanguageResources.MockGpu, HardwareType.GpuNvidia,
                    CreateCategory("/gpu/0", SensorType.Temperature,
                        CreateSensor("/gpu/0/temperature/0", LanguageResources.MockGpuCore, SensorType.Temperature, 55.1f, 34.0f, 67.2f)),
                    CreateCategory("/gpu/0", SensorType.Load,
                        CreateSensor("/gpu/0/load/0", LanguageResources.MockGpuCore, SensorType.Load, 41.2f, 0.0f, 92.4f)),
                    CreateCategory("/gpu/0", SensorType.Fan,
                        CreateSensor("/gpu/0/fan/0", LanguageResources.MockGpuFan, SensorType.Fan, 1120.0f, 0.0f, 1870.0f))),
                CreateHardware("/memory", LanguageResources.MockMemoryHardware, HardwareType.Memory,
                    CreateCategory("/memory", SensorType.Load,
                        CreateSensor("/memory/load/0", LanguageResources.MockMemoryLoad, SensorType.Load, 56.2f, 43.1f, 62.5f)),
                    CreateCategory("/memory", SensorType.Data,
                        CreateSensor("/memory/data/0", LanguageResources.MockMemoryUsed, SensorType.Data, 19327352832.0f, 14817637171.0f, 21474836480.0f))),
                CreateHardware("/storage/0", LanguageResources.MockStorage, HardwareType.Storage,
                    CreateCategory("/storage/0", SensorType.Temperature,
                        CreateSensor("/storage/0/temperature/0", LanguageResources.MockDriveTemperature, SensorType.Temperature, 39.0f, 30.0f, 44.0f)),
                    CreateCategory("/storage/0", SensorType.Load,
                        CreateSensor("/storage/0/load/0", LanguageResources.MockDriveActivity, SensorType.Load, 12.4f, 0.0f, 76.1f))),
                CreateHardware("/mainboard/0", LanguageResources.MockMotherboard, HardwareType.Motherboard,
                    CreateCategory("/mainboard/0", SensorType.Voltage,
                        CreateSensor("/mainboard/0/voltage/0", LanguageResources.MockCpuVcore, SensorType.Voltage, 1.264f, 0.792f, 1.318f)),
                    CreateCategory("/mainboard/0", SensorType.Fan,
                        CreateSensor("/mainboard/0/fan/0", LanguageResources.MockCpuFan, SensorType.Fan, 987.0f, 720.0f, 1420.0f)))
            });

        MainWindowViewModel viewModel = new();
        viewModel.Apply(new SensorTreeSnapshot(DateTime.Now, new[] { computer }));

        return viewModel;
    }

    /// <summary>
    /// Creates a sample hardware node.
    /// </summary>
    /// <param name="identifier">The stable hardware identifier.</param>
    /// <param name="name">The localized hardware name.</param>
    /// <param name="hardwareType">The hardware type.</param>
    /// <param name="children">The sample sensor categories.</param>
    /// <returns>The hardware snapshot node.</returns>
    private static SensorTreeNodeSnapshot CreateHardware(string identifier,
        string name,
        HardwareType hardwareType,
        params SensorTreeNodeSnapshot[] children)
    {
        return new SensorTreeNodeSnapshot(identifier, name, SensorTreeNodeKind.Hardware, hardwareType,
            null, null, null, null, children);
    }

    /// <summary>
    /// Creates a localized sample sensor category.
    /// </summary>
    /// <param name="hardwareIdentifier">The parent hardware identifier.</param>
    /// <param name="sensorType">The category measurement type.</param>
    /// <param name="children">The sample sensors.</param>
    /// <returns>The category snapshot node.</returns>
    private static SensorTreeNodeSnapshot CreateCategory(string hardwareIdentifier,
        SensorType sensorType,
        params SensorTreeNodeSnapshot[] children)
    {
        return new SensorTreeNodeSnapshot(hardwareIdentifier + "/" + sensorType,
            SensorCategoryName.GetName(sensorType),
            SensorTreeNodeKind.Category,
            null,
            sensorType,
            null,
            null,
            null,
            children);
    }

    /// <summary>
    /// Creates a sample sensor with current, minimum and maximum values.
    /// </summary>
    /// <param name="identifier">The stable sensor identifier.</param>
    /// <param name="name">The localized sensor name.</param>
    /// <param name="sensorType">The sensor measurement type.</param>
    /// <param name="value">The current sample value.</param>
    /// <param name="min">The minimum sample value.</param>
    /// <param name="max">The maximum sample value.</param>
    /// <returns>The sensor snapshot node.</returns>
    private static SensorTreeNodeSnapshot CreateSensor(string identifier,
        string name,
        SensorType sensorType,
        float value,
        float min,
        float max)
    {
        return new SensorTreeNodeSnapshot(identifier, name, SensorTreeNodeKind.Sensor, null,
            sensorType, value, min, max, Array.Empty<SensorTreeNodeSnapshot>());
    }
}
