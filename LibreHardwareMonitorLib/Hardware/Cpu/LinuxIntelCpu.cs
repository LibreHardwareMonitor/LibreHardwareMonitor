using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LibreHardwareMonitor.Hardware.Cpu;

// Linux runtime dependencies for Intel temperature support:
// 1) Kernel coretemp/hwmon exposure under /sys/class/hwmon.
// 2) Intel coretemp driver loaded and exporting temp*_input and temp*_label.
// 3) Read permission to the coretemp sysfs files.
// Notes:
// - No dependency on the userspace `sensors` command at runtime.
// - No extra NuGet package is required specifically for this class.
internal sealed class LinuxIntelCpu : IntelCpu
{
    private const string HwMonPath = "/sys/class/hwmon/";

    private readonly Sensor[] _coreSensors;
    private readonly FileStream[] _coreStreams;
    private readonly Sensor _packageSensor;
    private readonly FileStream _packageStream;

    public LinuxIntelCpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        (_coreSensors, _coreStreams, _packageSensor, _packageStream) = CreateCoretempSensors(settings);
        Update();
    }

    public override string GetReport()
    {
        return base.GetReport();
    }

    public override void Close()
    {
        base.Close();

        foreach (FileStream stream in _coreStreams)
            stream?.Dispose();

        _packageStream?.Dispose();
    }

    public override void Update()
    {
        base.Update();

        for (int i = 0; i < _coreStreams.Length; i++)
        {
            if (TryReadHwmonTemperature(_coreStreams[i], out float temperature))
                _coreSensors[i].Value = temperature;
            else
                _coreSensors[i].Value = null;
        }

        if (_packageSensor == null)
            return;

        if (TryReadHwmonTemperature(_packageStream, out float packageTemperature))
            _packageSensor.Value = packageTemperature;
        else
            _packageSensor.Value = null;
    }

    private (Sensor[] coreSensors, FileStream[] coreStreams, Sensor packageSensor, FileStream packageStream) CreateCoretempSensors(ISettings settings)
    {
        if (!Directory.Exists(HwMonPath))
            return (Array.Empty<Sensor>(), Array.Empty<FileStream>(), null, null);

        var entries = new List<(int index, string label, string inputPath)>();
        string packagePath = null;

        foreach (string hwMonDir in Directory.GetDirectories(HwMonPath))
        {
            string path = ResolveCoretempPath(hwMonDir);
            if (path == null)
                continue;

            foreach (string inputPath in Directory.GetFiles(path, "temp*_input"))
            {
                if (!TryExtractTempIndex(inputPath, out int index))
                    continue;

                string label = TryReadFirstLine(Path.Combine(path, $"temp{index}_label"));
                if (string.IsNullOrWhiteSpace(label))
                    continue;

                if (label.StartsWith("Package", StringComparison.OrdinalIgnoreCase))
                {
                    packagePath ??= inputPath;
                    continue;
                }

                if (label.StartsWith("Core", StringComparison.OrdinalIgnoreCase))
                    entries.Add((index, label, inputPath));
            }
        }

        var sortedEntries = entries
            .OrderBy(entry => entry.index)
            .ThenBy(entry => entry.label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.inputPath, StringComparer.Ordinal)
            .ToArray();

        var coreSensors = new Sensor[sortedEntries.Length];
        var coreStreams = new FileStream[sortedEntries.Length];
        int sensorIndex = 0;

        for (int i = 0; i < sortedEntries.Length; i++)
        {
            coreStreams[i] = new FileStream(sortedEntries[i].inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            coreSensors[i] = new Sensor(sortedEntries[i].label, sensorIndex++, SensorType.Temperature, this, settings);
            ActivateSensor(coreSensors[i]);
        }

        Sensor packageSensor = null;
        FileStream packageStream = null;
        if (packagePath != null)
        {
            packageStream = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            packageSensor = new Sensor("CPU Package", sensorIndex, SensorType.Temperature, this, settings);
            ActivateSensor(packageSensor);
        }

        return (coreSensors, coreStreams, packageSensor, packageStream);
    }

    private static string ResolveCoretempPath(string hwMonDir)
    {
        string directName = TryReadFirstLine(Path.Combine(hwMonDir, "name"));
        if (string.Equals(directName, "coretemp", StringComparison.OrdinalIgnoreCase))
            return hwMonDir;

        string devicePath = Path.Combine(hwMonDir, "device");
        string deviceName = TryReadFirstLine(Path.Combine(devicePath, "name"));
        if (string.Equals(deviceName, "coretemp", StringComparison.OrdinalIgnoreCase))
            return devicePath;

        return null;
    }

    private static bool TryReadHwmonTemperature(Stream stream, out float temperature)
    {
        temperature = 0;
        if (stream == null)
            return false;

        try
        {
            stream.Seek(0, SeekOrigin.Begin);

            Span<byte> buffer = stackalloc byte[32];
            int read = stream.Read(buffer);
            if (read <= 0)
                return false;

            int length = 0;
            while (length < read)
            {
                byte b = buffer[length];
                if (b == '\n' || b == '\r')
                    break;

                length++;
            }

            if (length == 0)
                return false;

            long rawValue = 0;
            for (int i = 0; i < length; i++)
            {
                byte b = buffer[i];
                if (b is < (byte)'0' or > (byte)'9')
                    return false;

                rawValue = (rawValue * 10) + (b - (byte)'0');
            }

            temperature = rawValue * 0.001f;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryExtractTempIndex(string inputPath, out int index)
    {
        index = -1;
        string fileName = Path.GetFileName(inputPath);
        const string prefix = "temp";
        const string suffix = "_input";

        if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith(prefix, StringComparison.Ordinal) || !fileName.EndsWith(suffix, StringComparison.Ordinal))
            return false;

        string indexText = fileName.Substring(prefix.Length, fileName.Length - prefix.Length - suffix.Length);
        return int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }

    private static string TryReadFirstLine(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            using StreamReader reader = new(path);
            return reader.ReadLine();
        }
        catch
        {
            return null;
        }
    }
}
