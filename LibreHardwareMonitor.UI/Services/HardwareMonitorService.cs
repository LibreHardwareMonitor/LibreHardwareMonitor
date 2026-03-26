using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI.Services;

public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _visitor = new();
    private readonly Timer _timer;
    private bool _isRunning;

    public event Action? Updated;

    public Computer Computer => _computer;
    public bool IsRunning => _isRunning;

    public double UpdateIntervalMs
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsNetworkEnabled = true,
            IsStorageEnabled = true,
            IsBatteryEnabled = true,
            IsPsuEnabled = true
        };

        _timer = new Timer(1000);
        _timer.Elapsed += OnTimerElapsed;
    }

    public void Start()
    {
        if (_isRunning) return;
        _computer.Open();
        _computer.Accept(_visitor);
        _isRunning = true;
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _isRunning = false;
    }

    public void Update()
    {
        if (!_isRunning) return;
        _computer.Accept(_visitor);
        Updated?.Invoke();
    }

    public IEnumerable<IHardware> GetHardwareByType(HardwareType type)
    {
        return _computer.Hardware
            .Where(h => h.HardwareType == type)
            .Concat(_computer.Hardware.SelectMany(h => h.SubHardware).Where(s => s.HardwareType == type));
    }

    public IEnumerable<ISensor> GetAllSensors()
    {
        return _computer.Hardware
            .SelectMany(h => h.Sensors.Concat(h.SubHardware.SelectMany(s => s.Sensors)));
    }

    public IEnumerable<ISensor> GetSensorsByType(SensorType type)
    {
        return GetAllSensors().Where(s => s.SensorType == type);
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            Update();
        }
        catch
        {
            // Swallow exceptions during background updates
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        if (_isRunning)
        {
            _computer.Close();
            _isRunning = false;
        }
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware sub in hardware.SubHardware)
                sub.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
