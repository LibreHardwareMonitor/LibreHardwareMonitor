using System.Globalization;
using System.Text.Json;
using ModelContextProtocol;

namespace LibreHardwareMonitor.Mcp;

public sealed class HardwareMonitorClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<HardwareSensorReading>> GetSensorsAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendRequestAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "LibreHardwareMonitor rejected authentication. Check LHM_USERNAME and LHM_PASSWORD."
                : $"LibreHardwareMonitor returned HTTP {(int)response.StatusCode}. Check that its web server is running and LHM_BASE_URL is correct.";
            throw new McpException(message);
        }

        string content;
        try
        {
            content = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new McpException("The LibreHardwareMonitor request timed out while reading sensor data.");
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            throw new McpException("The connection to LibreHardwareMonitor ended while reading sensor data.", exception);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            JsonElement children = GetChildren(document.RootElement);
            var sensors = new List<HardwareSensorReading>();
            foreach (JsonElement node in children.EnumerateArray())
                AddSensors(node, null, sensors);

            return sensors;
        }
        catch (JsonException exception)
        {
            throw new McpException("LibreHardwareMonitor returned invalid JSON from data.json.", exception);
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.GetAsync("data.json", cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new McpException("The LibreHardwareMonitor request timed out. Check LHM_BASE_URL and network connectivity.");
        }
        catch (HttpRequestException exception)
        {
            throw new McpException("Could not connect to LibreHardwareMonitor. Check that its web server is running and LHM_BASE_URL is reachable.", exception);
        }
    }

    private static JsonElement GetChildren(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object &&
            node.TryGetProperty("Children", out JsonElement children) &&
            children.ValueKind == JsonValueKind.Array)
        {
            return children;
        }

        throw new McpException("LibreHardwareMonitor returned an unexpected sensor tree from data.json.");
    }

    private static void AddSensors(JsonElement node, string? hardwareName, List<HardwareSensorReading> sensors)
    {
        if (node.ValueKind != JsonValueKind.Object)
            throw new McpException("LibreHardwareMonitor returned an unexpected sensor node from data.json.");

        string text = GetString(node, "Text") ?? string.Empty;
        if (node.TryGetProperty("HardwareId", out _))
            hardwareName = text;

        if (node.TryGetProperty("SensorId", out JsonElement sensorId))
        {
            sensors.Add(new HardwareSensorReading(
                hardwareName ?? string.Empty,
                text,
                sensorId.ToString(),
                GetString(node, "Type") ?? string.Empty,
                GetNumber(node, "RawValue"),
                GetNumber(node, "RawMin"),
                GetNumber(node, "RawMax"),
                GetString(node, "Value") ?? string.Empty,
                GetString(node, "Min") ?? string.Empty,
                GetString(node, "Max") ?? string.Empty));
        }

        JsonElement children = GetChildren(node);
        foreach (JsonElement child in children.EnumerateArray())
            AddSensors(child, hardwareName, sensors);
    }

    private static string? GetString(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out JsonElement value))
            return null;

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static double? GetNumber(JsonElement node, string propertyName)
    {
        if (!node.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
            return null;

        string text = value.ToString();
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) &&
               double.IsFinite(result)
            ? result
            : null;
    }
}

public sealed record HardwareSensorReading(
    string Hardware,
    string Name,
    string SensorId,
    string SensorType,
    double? Value,
    double? Min,
    double? Max,
    string FormattedValue,
    string FormattedMin,
    string FormattedMax);
