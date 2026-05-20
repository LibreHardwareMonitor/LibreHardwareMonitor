// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using LibreHardwareMonitor.Hardware.Rtss;

namespace LibreHardwareMonitor.Hardware.Gpu;

public abstract class GenericGpu : Hardware
{
    /// <summary>
    /// Sensors for RivaTuner Statistics Server (RTSS) by process id.
    /// </summary>
    private readonly IDictionary<uint, Sensor> _rtssSensorsByProcessId = new Dictionary<uint, Sensor>();

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericGpu" /> class.
    /// </summary>
    /// <param name="name">Component name.</param>
    /// <param name="identifier">Identifier that will be assigned to the device. Based on <see cref="Identifier" /></param>
    /// <param name="settings">Additional settings passed by the <see cref="IComputer" />.</param>
    protected GenericGpu(string name, Identifier identifier, ISettings settings) : base(name, identifier, settings)
    {
    }

    /// <summary>
    /// Gets the device identifier.
    /// </summary>
    public abstract string DeviceId { get; }

    public override void Update()
    {
        // No RTSS on Unix systems
        if (Software.OperatingSystem.IsUnix)
            return;

#if WINDOWS
        var processIds = new HashSet<uint>();
        try
        {
            using (var mmf = MemoryMappedFile.OpenExisting(RtssHelpers.MMF_NAME))
            using (var accessor = mmf.CreateViewAccessor())
            {
                accessor.Read(0, out RTSS_SHARED_MEMORY_HEADER header);

                if (header.Signature == RtssHelpers.RTSS_SIGNATURE && header.Version >= RtssHelpers.GenerateVersion(2, 1))
                {
                    for (uint i = 0; i < header.AppArrSize; i++)
                    {
                        uint offset = header.AppArrOffset + (i * header.AppEntrySize);
                        accessor.Read(offset, out RTSS_SHARED_MEMORY_APP_ENTRY entry);

                        if (entry.ProcessID is not 0 && entry.Time1 > entry.Time0)
                        {
                            Sensor sensor;
                            if (!_rtssSensorsByProcessId.TryGetValue(entry.ProcessID, out sensor))
                            {
                                var textParts = new List<string>
                                {
                                    RtssHelpers.EntryToDisplayName(entry),
                                    RtssHelpers.FlagsToDisplayName((RtssAppFlags)entry.Flags),
                                    "FPS",
                                }.Where(text => text != null);
                                sensor = new Sensor(string.Join(" ", textParts), 0, SensorType.Factor, this, _settings);
                                base.ActivateSensor(sensor);
                                _rtssSensorsByProcessId.Add(entry.ProcessID, sensor);
                            }

                            float fps = 1000.0f * entry.Frames / (entry.Time1 - entry.Time0);
                            sensor.Value = (float)Math.Round(fps, 1);
                        }

                        processIds.Add(entry.ProcessID);
                    }
                }
            }
        }
        catch (FileNotFoundException)
        {
            // RTSS not started
        }

        foreach (var kv in _rtssSensorsByProcessId.Where(kv => !processIds.Contains(kv.Key)).ToList())
        {
            DeactivateSensor(kv.Value);
            _rtssSensorsByProcessId.Remove(kv.Key);
        }
#endif
    }

    public override void Close()
    {
        base.Close();
    }
}
