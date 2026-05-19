using System.Collections.Generic;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Rtss;

internal class RtssGroup : IGroup
{
    private readonly List<RtssHardware> _hardware = new List<RtssHardware>();

    public RtssGroup(ISettings settings)
    {
        // No implementation for FPS on Unix systems
        if (Software.OperatingSystem.IsUnix)
            return;

#if WINDOWS
        _hardware.Add(new RtssHardware(settings));
#endif
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public string GetReport()
    {
        var report = new StringBuilder();
        foreach (var hardware in _hardware)
        {
            report.Append(hardware.GetReport());
        }
        return report.ToString();
    }

    public void Close()
    {
        foreach (var hardware in _hardware)
        {
            hardware.Close();
        }
    }
}
