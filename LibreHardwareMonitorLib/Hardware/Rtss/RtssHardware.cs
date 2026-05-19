using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Rtss;

internal class RtssHardware : Hardware
{
    private readonly IDictionary<uint, Sensor> _sensorsByProcessId = new Dictionary<uint, Sensor>();
    private readonly Sensor _emptySensor;
    private const uint RTSS_SIGNATURE = 0x52545353; // 0x52 = 'R', 0x54 = 'T', 0x53 = 'S', 0x53 = 'S'
    private const string MMF_NAME = "RTSSSharedMemoryV2";

    public RtssHardware(ISettings settings)
        : base("RivaTuner Statistics Server", new Identifier("rtss"), settings)
    {
        _emptySensor = new Sensor("(No running app found)", 0, SensorType.Factor, this, _settings);
    }

    public override HardwareType HardwareType => HardwareType.Rtss; 

    public override void Update()
    {
        var processIds = new HashSet<uint>();
        try
        {
            using (var mmf = MemoryMappedFile.OpenExisting(MMF_NAME))
            using (var accessor = mmf.CreateViewAccessor())
            {
                accessor.Read(0, out RTSS_SHARED_MEMORY_HEADER header);

                if (header.Signature == RTSS_SIGNATURE && header.Version >= GenerateVersion(2, 0))
                {
                    for (uint i = 0; i < header.AppArrSize; i++)
                    {
                        uint offset = header.AppArrOffset + (i * header.AppEntrySize);
                        accessor.Read(offset, out RTSS_SHARED_MEMORY_APP_ENTRY entry);

                        if (((RtssAppFlags)entry.Flags & RtssAppFlags.MASK) is not RtssAppFlags.None &&
                            entry.ProcessID is not 0 &&
                            entry.Time1 > entry.Time0)
                        {
                            Sensor sensor;
                            if (!_sensorsByProcessId.TryGetValue(entry.ProcessID, out sensor))
                            {
                                var textParts = new List<string>
                                {
                                    EntryToDisplayName(entry),
                                    FlagsToDisplayName((RtssAppFlags)entry.Flags),
                                    "FPS",
                                }.Where(text => text != null);
                                sensor = new Sensor(string.Join(" ", textParts), 0, SensorType.Factor, this, _settings);
                                ActivateSensor(sensor);
                                _sensorsByProcessId.Add(entry.ProcessID, sensor);
                            }

                            float fps = 1000.0f * entry.Frames / (entry.Time1 - entry.Time0);
                            sensor.Value = (float)Math.Round(fps, 1);
                        }

                        processIds.Add(entry.ProcessID);
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        foreach (var kv in _sensorsByProcessId.Where(kv => !processIds.Contains(kv.Key)).ToList())
        {
            DeactivateSensor(kv.Value);
            _sensorsByProcessId.Remove(kv.Key);
        }

        if (_sensorsByProcessId.Any())
        {
            DeactivateSensor(_emptySensor);
        }
        else
        {
            ActivateSensor(_emptySensor);
        }
    }

    private static uint GenerateVersion(ushort major, ushort minor)
    {
        return ((uint)major << 16) + minor;
    }

    private static string ExtractName(RTSS_SHARED_MEMORY_APP_ENTRY entry)
    {
        unsafe
        {
            int length = 0;
            while (length < 256 && entry.Name[length] != 0)
            {
                length++;
            }

            return Encoding.Default.GetString(entry.Name, length);
        }
    }

    private static string EntryToDisplayName(RTSS_SHARED_MEMORY_APP_ENTRY entry)
    {
        string name = ExtractName(entry);
        return string.IsNullOrWhiteSpace(name) ? entry.ProcessID.ToString() : Path.GetFileNameWithoutExtension(name);
    }

    private static string FlagsToDisplayName(RtssAppFlags flags)
    {
        if (flags.HasFlag(RtssAppFlags.OpenGL))
        {
            return "OpenGL";
        }

        if (flags.HasFlag(RtssAppFlags.DirectDraw))
        {
            return "DirectDraw";
        }

        if (flags.HasFlag(RtssAppFlags.Direct3D8))
        {
            return "Direct3D 8";
        }

        if (flags.HasFlag(RtssAppFlags.Direct3D9) || flags.HasFlag(RtssAppFlags.Direct3D9Ex))
        {
            return "Direct3D 9";
        }

        if (flags.HasFlag(RtssAppFlags.Direct3D10))
        {
            return "Direct3D 10";
        }

        if (flags.HasFlag(RtssAppFlags.Direct3D11))
        {
            return "Direct3D 11";
        }

        return null;
    }
}
