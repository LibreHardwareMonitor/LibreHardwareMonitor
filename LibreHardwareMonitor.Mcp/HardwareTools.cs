using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace LibreHardwareMonitor.Mcp;

[McpServerToolType]
public sealed class HardwareTools(HardwareMonitorClient monitor)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [McpServerTool(Name = "list_hardware_sensors")]
    [Description("Reads current LibreHardwareMonitor sensor values. Optionally filter by a case-insensitive substring in the hardware or sensor name. This tool is read-only.")]
    public async Task<string> ListHardwareSensorsAsync(
        [Description("Optional substring to filter hardware and sensor names.")] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HardwareSensorReading> sensors = await monitor.GetSensorsAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            sensors = sensors
                .Where(sensor =>
                    sensor.Hardware.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    sensor.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        return JsonSerializer.Serialize(sensors, JsonOptions);
    }

    [McpServerTool(Name = "get_hardware_sensor")]
    [Description("Reads one LibreHardwareMonitor sensor by its exact sensorId. Use list_hardware_sensors to find sensor IDs. This tool is read-only.")]
    public async Task<string> GetHardwareSensorAsync(
        [Description("Exact sensorId returned by list_hardware_sensors.")] string sensorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sensorId))
            throw new McpException("sensorId is required.");

        IReadOnlyList<HardwareSensorReading> sensors = await monitor.GetSensorsAsync(cancellationToken);
        HardwareSensorReading? sensor = sensors.FirstOrDefault(candidate =>
            string.Equals(candidate.SensorId, sensorId, StringComparison.Ordinal));

        if (sensor is null)
            throw new McpException($"No hardware sensor was found with sensorId '{sensorId}'.");

        return JsonSerializer.Serialize(sensor, JsonOptions);
    }
}
