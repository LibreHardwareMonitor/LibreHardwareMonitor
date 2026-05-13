using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI.Services;

namespace LibreHardwareMonitor.UI.ViewModels;

public partial class StorageDriveInfo : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private float _temperature;
    [ObservableProperty] private float _usedPercent;
    [ObservableProperty] private bool _hasTemp;

    public StorageDriveInfo(string name, float temp, float used, bool hasTemp)
    {
        _name = name;
        _temperature = temp;
        _usedPercent = used;
        _hasTemp = hasTemp;
    }
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly HardwareMonitorService _service;

    // CPU
    [ObservableProperty] private float _cpuLoad;
    [ObservableProperty] private float _cpuTemp;
    [ObservableProperty] private string _cpuName = "Detecting...";
    [ObservableProperty] private float _cpuPower;
    [ObservableProperty] private float _cpuMaxClock;
    [ObservableProperty] private int _cpuCoreCount;
    [ObservableProperty] private bool _hasCpu;

    // GPU
    [ObservableProperty] private float _gpuLoad;
    [ObservableProperty] private float _gpuTemp;
    [ObservableProperty] private string _gpuName = "Detecting...";
    [ObservableProperty] private float _gpuMemoryUsed;
    [ObservableProperty] private float _gpuMemoryTotal;
    [ObservableProperty] private float _gpuPower;
    [ObservableProperty] private float _gpuFanSpeed;
    [ObservableProperty] private bool _hasGpu;

    // RAM
    [ObservableProperty] private float _ramLoad;
    [ObservableProperty] private float _ramUsed;
    [ObservableProperty] private float _ramTotal;
    [ObservableProperty] private string _ramUsedFormatted = "--";
    [ObservableProperty] private string _ramTotalFormatted = "--";

    // Storage
    [ObservableProperty] private float _storageTemp;
    [ObservableProperty] private float _storageUsed;
    [ObservableProperty] private string _storageName = "Detecting...";
    [ObservableProperty] private float _storageReadRate;
    [ObservableProperty] private float _storageWriteRate;
    [ObservableProperty] private int _storageCount;
    [ObservableProperty] private bool _hasStorage;

    public ObservableCollection<StorageDriveInfo> StorageDrives { get; } = new();

    // Motherboard
    [ObservableProperty] private string _motherboardName = "Detecting...";
    [ObservableProperty] private float _motherboardTemp;
    [ObservableProperty] private float _motherboardVrmTemp;
    [ObservableProperty] private int _motherboardFanCount;
    [ObservableProperty] private bool _hasMotherboard;

    // Network
    [ObservableProperty] private float _networkUp;
    [ObservableProperty] private float _networkDown;
    [ObservableProperty] private int _networkAdapterCount;
    [ObservableProperty] private string _networkName = "Network";

    // Battery
    [ObservableProperty] private float _batteryLevel;
    [ObservableProperty] private float _batteryVoltage;
    [ObservableProperty] private string _batteryStatus = "";
    [ObservableProperty] private bool _hasBattery;
    [ObservableProperty] private float _batteryChargeRate;

    [ObservableProperty] private int _totalSensorCount;
    [ObservableProperty] private int _totalHardwareCount;
    [ObservableProperty] private string _tempUnit = "\u00B0C";

    public ObservableCollection<SensorViewModel> TopTemperatures { get; } = new();
    public ObservableCollection<SensorViewModel> TopFans { get; } = new();
    public ObservableCollection<SensorViewModel> TopPower { get; } = new();

    public DashboardViewModel(HardwareMonitorService service)
    {
        _service = service;
    }

    public void Update()
    {
        TempUnit = Converters.SensorUnitHelper.TempUnit;
        UpdateCpu();
        UpdateGpu();
        UpdateRam();
        UpdateStorage();
        UpdateMotherboard();
        UpdateNetwork();
        UpdateBattery();
        UpdateTopSensors();
        UpdateSummary();
    }

    private void UpdateCpu()
    {
        var cpus = _service.GetHardwareByType(HardwareType.Cpu).ToList();
        HasCpu = cpus.Count > 0;
        if (!HasCpu)
        {
            CpuName = "Not detected";
            return;
        }

        var cpu = cpus[0];
        CpuName = cpu.Name;

        var totalLoad = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total"));
        CpuLoad = totalLoad?.Value ?? 0;

        var temps = cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature).ToList();
        var packageTemp = temps.FirstOrDefault(t => t.Name.Contains("Package") || t.Name.Contains("Average"));
        float rawCpuTemp = packageTemp?.Value ?? (temps.Count > 0 ? temps.Max(t => t.Value ?? 0) : 0);
        CpuTemp = Converters.SensorUnitHelper.ConvertTemp(rawCpuTemp);

        var power = cpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power && (s.Name.Contains("Package") || s.Name.Contains("Total")));
        CpuPower = power?.Value ?? 0;

        var clocks = cpu.Sensors.Where(s => s.SensorType == SensorType.Clock && !s.Name.Contains("Bus")).ToList();
        CpuMaxClock = clocks.Count > 0 ? clocks.Max(c => c.Value ?? 0) : 0;
        CpuCoreCount = clocks.Count;
    }

    private void UpdateGpu()
    {
        var gpus = _service.Computer.Hardware
            .Where(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
            .ToList();

        HasGpu = gpus.Count > 0;
        if (!HasGpu)
        {
            GpuName = "Not detected";
            return;
        }
        var gpu = gpus[0];
        GpuName = gpu.Name;

        var coreLoad = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Core"));
        GpuLoad = coreLoad?.Value ?? 0;

        var temp = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name.Contains("Core"));
        float rawGpuTemp = temp?.Value ?? (gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value ?? 0);
        GpuTemp = Converters.SensorUnitHelper.ConvertTemp(rawGpuTemp);

        var memUsed = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name.Contains("Used"));
        GpuMemoryUsed = memUsed?.Value ?? 0;

        var memTotal = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && s.Name.Contains("Total"));
        GpuMemoryTotal = memTotal?.Value ?? 0;

        var power = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power);
        GpuPower = power?.Value ?? 0;

        var fan = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Fan);
        GpuFanSpeed = fan?.Value ?? 0;
    }

    private void UpdateRam()
    {
        var mems = _service.GetHardwareByType(HardwareType.Memory).ToList();
        if (mems.Count == 0) return;
        var mem = mems[0];

        var load = mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
        RamLoad = load?.Value ?? 0;

        var used = mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Used"));
        var available = mem.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Contains("Available"));

        float usedVal = used?.Value ?? 0;
        float availVal = available?.Value ?? 0;
        RamUsed = usedVal;
        RamTotal = usedVal + availVal;
        RamUsedFormatted = $"{usedVal:F1}";
        RamTotalFormatted = $"{(usedVal + availVal):F1}";
    }

    private static ISensor? GetRealTemperature(IHardware hw)
    {
        return hw.Sensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Temperature &&
            !s.Name.Contains("Warning") && !s.Name.Contains("Critical") &&
            !s.Name.Contains("Lifetime") && !s.Name.Contains("Threshold") &&
            !s.Name.Contains("Limit"));
    }

    private void UpdateStorage()
    {
        var drives = _service.GetHardwareByType(HardwareType.Storage).ToList();
        StorageCount = drives.Count;
        HasStorage = drives.Count > 0;
        if (!HasStorage)
        {
            StorageName = "Not detected";
            StorageDrives.Clear();
            return;
        }

        var drive = drives[0];
        StorageName = drive.Name;

        var temp = GetRealTemperature(drive);
        StorageTemp = Converters.SensorUnitHelper.ConvertTemp(temp?.Value ?? 0);

        var usedSpace = drive.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Used"));
        StorageUsed = usedSpace?.Value ?? 0;

        // Update all drives list
        for (int i = 0; i < drives.Count; i++)
        {
            var d = drives[i];
            var dTemp = GetRealTemperature(d);
            var dUsed = d.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Used"));
            float tVal = dTemp?.Value ?? 0;
            float uVal = dUsed?.Value ?? 0;
            bool hasT = dTemp?.Value != null;

            if (i < StorageDrives.Count)
            {
                StorageDrives[i].Name = d.Name;
                StorageDrives[i].Temperature = tVal;
                StorageDrives[i].UsedPercent = uVal;
                StorageDrives[i].HasTemp = hasT;
            }
            else
            {
                StorageDrives.Add(new StorageDriveInfo(d.Name, tVal, uVal, hasT));
            }
        }
        while (StorageDrives.Count > drives.Count)
            StorageDrives.RemoveAt(StorageDrives.Count - 1);

        // Aggregate read/write across all drives
        float totalRead = 0, totalWrite = 0;
        foreach (var d in drives)
        {
            var read = d.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Read"));
            var write = d.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Write"));
            totalRead += read?.Value ?? 0;
            totalWrite += write?.Value ?? 0;
        }
        StorageReadRate = totalRead;
        StorageWriteRate = totalWrite;
    }

    private void UpdateNetwork()
    {
        var allNics = _service.GetHardwareByType(HardwareType.Network).ToList();
        var physicalNics = allNics.Where(Services.NetworkAdapterFilter.IsPhysicalAdapter).ToList();

        // Fall back to all adapters if no physical ones found
        var nics = physicalNics.Count > 0 ? physicalNics : allNics;
        NetworkAdapterCount = nics.Count;

        // Pick the most active adapter for the name
        IHardware? activeNic = null;
        float maxThroughput = 0;
        float totalUp = 0, totalDown = 0;

        foreach (var nic in nics)
        {
            var up = nic.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Upload"));
            var down = nic.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Throughput && s.Name.Contains("Download"));
            float u = up?.Value ?? 0;
            float d = down?.Value ?? 0;
            totalUp += u;
            totalDown += d;

            float total = u + d;
            if (total > maxThroughput || activeNic == null)
            {
                maxThroughput = total;
                activeNic = nic;
            }
        }

        NetworkUp = totalUp;
        NetworkDown = totalDown;
        NetworkName = activeNic?.Name ?? "Network";
    }

    private void UpdateBattery()
    {
        var batteries = _service.GetHardwareByType(HardwareType.Battery).ToList();
        HasBattery = batteries.Count > 0;
        if (!HasBattery) return;

        var bat = batteries[0];
        var level = bat.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Level);
        BatteryLevel = level?.Value ?? 0;

        var voltage = bat.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Voltage);
        BatteryVoltage = voltage?.Value ?? 0;

        var chargeRate = bat.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Power);
        BatteryChargeRate = chargeRate?.Value ?? 0;

        var current = bat.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Current);
        BatteryStatus = (current?.Value ?? 0) > 0 ? "Charging" : "Discharging";
    }

    private void UpdateMotherboard()
    {
        var boards = _service.GetHardwareByType(HardwareType.Motherboard).ToList();
        HasMotherboard = boards.Count > 0;
        if (!HasMotherboard)
        {
            MotherboardName = "Not detected";
            return;
        }

        var board = boards[0];
        MotherboardName = board.Name;

        // Get chipset/system temp from SubHardware (SuperIO chips)
        var allSensors = board.Sensors
            .Concat(board.SubHardware.SelectMany(s => s.Sensors))
            .ToList();

        var sysTemp = allSensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Temperature &&
            (s.Name.Contains("System") || s.Name.Contains("Chipset") || s.Name.Contains("Temperature #1")));
        MotherboardTemp = Converters.SensorUnitHelper.ConvertTemp(sysTemp?.Value ?? 0);

        var vrmTemp = allSensors.FirstOrDefault(s =>
            s.SensorType == SensorType.Temperature &&
            (s.Name.Contains("VRM") || s.Name.Contains("MOS") || s.Name.Contains("Temperature #2")));
        MotherboardVrmTemp = Converters.SensorUnitHelper.ConvertTemp(vrmTemp?.Value ?? 0);

        MotherboardFanCount = allSensors.Count(s => s.SensorType == SensorType.Fan && s.Value.HasValue && s.Value > 0);
    }

    private void UpdateSummary()
    {
        TotalHardwareCount = _service.Computer.Hardware.Count
            + _service.Computer.Hardware.Sum(h => h.SubHardware.Length);
        TotalSensorCount = _service.GetAllSensors().Count(s => s.Value.HasValue);
    }

    private void UpdateTopSensors()
    {
        UpdateTopCollection(TopTemperatures, SensorType.Temperature);
        UpdateTopCollection(TopFans, SensorType.Fan);
        UpdateTopCollection(TopPower, SensorType.Power);
    }

    /// <summary>
    /// Sensor names that are reference/threshold values, not actual readings.
    /// </summary>
    private static bool IsReferenceSensor(ISensor sensor)
    {
        string n = sensor.Name;
        return n.Contains("Warning") || n.Contains("Critical") || n.Contains("Lifetime") ||
               n.Contains("Threshold") || n.Contains("Limit");
    }

    private void UpdateTopCollection(ObservableCollection<SensorViewModel> collection, SensorType type)
    {
        var topSensors = _service.GetSensorsByType(type)
            .Where(s => s.Value.HasValue && !IsReferenceSensor(s))
            .OrderByDescending(s => s.Value)
            .Take(6)
            .ToList();

        for (int i = 0; i < topSensors.Count; i++)
        {
            string id = topSensors[i].Identifier.ToString();
            if (i < collection.Count)
            {
                if (collection[i].Identifier != id)
                    collection[i] = new SensorViewModel(topSensors[i]);
                else
                    collection[i].Update();
            }
            else
            {
                collection.Add(new SensorViewModel(topSensors[i]));
            }
        }

        while (collection.Count > topSensors.Count)
            collection.RemoveAt(collection.Count - 1);
    }
}
