namespace LibreHardwareMonitor.Hardware.Motherboard;

internal class Voltage
{
    public readonly bool Hidden;
    public readonly int Index;
    public readonly string Name;
    public readonly float Rf;
    public readonly float Ri;
    public readonly float Vf;

    public Voltage(string name, int index, bool hidden = false) : this(name, index, 0, 1, 0, hidden)
    { }

    public Voltage(string name, int index, float ri, float rf, float vf = 0, bool hidden = false)
    {
        Name = name;
        Index = index;
        Ri = ri;
        Rf = rf;
        Vf = vf;
        Hidden = hidden;
    }
}