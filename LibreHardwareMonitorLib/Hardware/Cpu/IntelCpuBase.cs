namespace LibreHardwareMonitor.Hardware.Cpu;

internal abstract class IntelCpu : GenericCpu
{
    protected IntelCpu(int processorIndex, CpuId[][] cpuId, ISettings settings)
        : base(processorIndex, cpuId, settings)
    {
    }

    public virtual float EnergyUnitsMultiplier => 0;
}
