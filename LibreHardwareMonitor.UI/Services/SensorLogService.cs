using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI.Services;

public sealed class SensorLogService
{
    private readonly HardwareMonitorService _service;
    private StreamWriter? _writer;
    private string[]? _sensorIds;
    private DateTime _lastWrite = DateTime.MinValue;

    public bool IsLogging { get; private set; }
    public int LogIntervalSeconds { get; set; } = 5;
    public string LogFilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "LibreHardwareMonitor-log.csv");

    public SensorLogService(HardwareMonitorService service)
    {
        _service = service;
    }

    public void Start()
    {
        if (IsLogging) return;

        try
        {
            var sensors = _service.GetAllSensors().ToList();
            _sensorIds = sensors.Select(s => s.Identifier.ToString()).ToArray();

            string dir = Path.GetDirectoryName(LogFilePath)!;
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            _writer = new StreamWriter(LogFilePath, append: false);

            string header = "Timestamp," + string.Join(",",
                sensors.Select(s =>
                {
                    string unit = s.SensorType == SensorType.Temperature
                        ? "\u00B0C"
                        : Converters.SensorUnitHelper.GetUnit(s.SensorType);
                    return $"\"{s.Hardware.Name} - {s.Name} ({unit})\"";
                }));
            _writer.WriteLine(header);
            _writer.Flush();

            _lastWrite = DateTime.MinValue;
            IsLogging = true;
        }
        catch
        {
            _writer?.Dispose();
            _writer = null;
            IsLogging = false;
        }
    }

    public void Stop()
    {
        IsLogging = false;
        _writer?.Dispose();
        _writer = null;
        _sensorIds = null;
    }

    public void OnUpdate()
    {
        if (!IsLogging || _writer == null || _sensorIds == null) return;

        var now = DateTime.Now;
        if ((now - _lastWrite).TotalSeconds < LogIntervalSeconds) return;
        _lastWrite = now;

        try
        {
            var lookup = new Dictionary<string, ISensor>();
            foreach (var s in _service.GetAllSensors())
                lookup[s.Identifier.ToString()] = s;

            var values = _sensorIds.Select(id =>
                lookup.TryGetValue(id, out var s) && s.Value.HasValue
                    ? s.Value.Value.ToString("F2", CultureInfo.InvariantCulture)
                    : "");

            _writer.WriteLine(now.ToString("yyyy-MM-dd HH:mm:ss") + "," + string.Join(",", values));
            _writer.Flush();
        }
        catch { }
    }
}
