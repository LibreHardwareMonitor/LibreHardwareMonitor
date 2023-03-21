namespace LibreHardwareMonitor.Hardware.Motherboard;

internal class Control
{
    public readonly int Index;
    public readonly string Name;

    public Control(string name, int index)
    {
        Name = name;
        Index = index;
    }
}
