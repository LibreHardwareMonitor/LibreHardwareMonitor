namespace LibreHardwareMonitor.Hardware.Motherboard;

internal class Fan
{
    public readonly int Index;
    public readonly string Name;

    public Fan(string name, int index)
    {
        Name = name;
        Index = index;
    }
}