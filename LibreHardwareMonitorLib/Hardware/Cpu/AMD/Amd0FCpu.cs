// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Text;
using System.Threading;

namespace LibreHardwareMonitor.Hardware.Cpu.AMD;

/// <summary>
/// AMD 0F CPU
/// </summary>
/// <seealso cref="AmdCpuBase" />
internal sealed class Amd0FCpu : AmdCpuBase
{
    #region Fields

    // ReSharper disable InconsistentNaming
    private const uint FIDVID_STATUS = 0xC0010042;
    private const ushort MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1103;
    private const byte MISCELLANEOUS_CONTROL_FUNCTION = 3;
    private const uint THERMTRIP_STATUS_REGISTER = 0xE4;
    // ReSharper restore InconsistentNaming

    private readonly uint _miscellaneousControlAddress;

    private Sensor _busClock;
    private Sensor[] _coreClocks;
    private Sensor[] _coreTemperatures;

    private byte _thermSenseCoreSelCpu0;
    private byte _thermSenseCoreSelCpu1;

    #endregion

    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Amd0FCpu"/> class.
    /// </summary>
    /// <param name="processorIndex">Index of the processor.</param>
    /// <param name="cpuId">The cpu identifier.</param>
    /// <param name="settings">The settings.</param>
    public Amd0FCpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        // Set Misc Control Address
        _miscellaneousControlAddress = GetPciAddress(MISCELLANEOUS_CONTROL_FUNCTION, MISCELLANEOUS_CONTROL_DEVICE_ID);

        // Sensors
        CreateTemperatureSensors();
        CreateClockSensors();

        // Initialize
        Initialize();

        // Update
        Update();
    }

    #endregion

    #region Methods
    
    /// <summary>
    /// Prints the data to a report.
    /// </summary>
    /// <returns></returns>
    /// <inheritdoc />
    public override string GetReport()
    {
        StringBuilder r = new();
        r.Append(base.GetReport());
        r.Append("Miscellaneous Control Address: 0x");
        r.AppendLine(_miscellaneousControlAddress.ToString("X", CultureInfo.InvariantCulture));
        r.AppendLine();
        return r.ToString();
    }

    /// <summary>
    /// Updates all sensors.
    /// </summary>
    /// <inheritdoc />
    public override void Update()
    {
        // Update Generic CPU
        base.Update();

        // Sensors
        UpdateTemperatureSensors();
        UpdateClockSensors();
    }

    /// <summary>
    /// Gets the MSRS.
    /// </summary>
    /// <returns></returns>
    protected override uint[] GetMsrs() => [FIDVID_STATUS];

    /// <summary>
    /// Create CPU temperature sensors.
    /// </summary>
    /// <returns></returns>
    private void CreateTemperatureSensors()
    {
        uint[,] cpu0ExtData = Cpu0.ExtData;
        float offset = -49.0f;

        // AM2+ 65nm +21 offset
        uint model = Cpu0.Model;
        if (model is >= 0x69 and not 0xc1 and not 0x6c and not 0x7c)
        {
            offset += 21;
        }

        // AMD Athlon 64 Processors
        if (model < 40)
        {
            _thermSenseCoreSelCpu0 = 0x0;
            _thermSenseCoreSelCpu1 = 0x4;
        }
        else
        {
            // AMD NPT Family 0Fh Revision F, G have the core selection swapped
            _thermSenseCoreSelCpu0 = 0x4;
            _thermSenseCoreSelCpu1 = 0x0;
        }

        // Check if processor supports a digital thermal sensor
        if (cpu0ExtData.GetLength(0) > 7 && (cpu0ExtData[7, 3] & 1) != 0)
        {
            _coreTemperatures = new Sensor[CoreCount];
            for (int i = 0; i < CoreCount; i++)
            {
                _coreTemperatures[i] = new Sensor("Core #" + (i + 1),
                    i,
                    SensorType.Temperature,
                    this,
                    [
                        new ParameterDescription("Offset [°C]",
                            "Temperature offset of the thermal sensor.\nTemperature = Value + Offset.", offset)
                    ],
                    Settings);
            }
        }
        else
        {
            _coreTemperatures = [];
        }
    }

    /// <summary>
    /// Updates the temperature sensors.
    /// </summary>
    private void UpdateTemperatureSensors()
    {
        // Block
        if (!Mutexes.WaitPciBus(10)) return;

        // Evaluate
        if (_miscellaneousControlAddress != Interop.Ring0.INVALID_PCI_ADDRESS)
        {
            for (uint i = 0; i < _coreTemperatures.Length; i++)
            {
                if (!Ring0.WritePciConfig(_miscellaneousControlAddress,
                        THERMTRIP_STATUS_REGISTER,
                        i > 0 ? _thermSenseCoreSelCpu1 : _thermSenseCoreSelCpu0))
                {
                    continue;
                }

                if (Ring0.ReadPciConfig(_miscellaneousControlAddress, THERMTRIP_STATUS_REGISTER, out uint value))
                {
                    _coreTemperatures[i].Value = ((value >> 16) & 0xFF) + _coreTemperatures[i].Parameters[0].Value;
                    ActivateSensor(_coreTemperatures[i]);
                }
                else
                {
                    DeactivateSensor(_coreTemperatures[i]);
                }
            }
        }

        // Release
        Mutexes.ReleasePciBus();
    }

    /// <summary>
    /// Create CPU clock sensors.
    /// </summary>
    /// <returns></returns>
    private void CreateClockSensors()
    {
        _busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, Settings);
        _coreClocks = new Sensor[CoreCount];
        for (int i = 0; i < _coreClocks.Length; i++)
        {
            _coreClocks[i] = new Sensor(SetCoreName(i), i + 1, SensorType.Clock, this, Settings);
            if (!HasTimeStampCounter) continue;
            ActivateSensor(_coreClocks[i]);
        }
    }

    /// <summary>
    /// Update CPU clock sensors.
    /// </summary>
    /// <returns></returns>
    private void UpdateClockSensors()
    {
        if (!HasTimeStampCounter) return;

         // Bus Clock
        double newBusClock = 0;
        for (int i = 0; i < _coreClocks.Length; i++)
        {
            Thread.Sleep(1);

            if (Ring0.ReadMsr(FIDVID_STATUS, out uint eax, out uint _, CpuId[i][0].Affinity))
            {
                // CurrFID can be found in eax bits 0-5, MaxFID in 16-21
                // 8-13 hold StartFID, we don't use that here.
                double curMp = 0.5 * ((eax & 0x3F) + 8);
                double maxMp = 0.5 * ((eax >> 16 & 0x3F) + 8);
                _coreClocks[i].Value = (float)(curMp * TimeStampCounterFrequency / maxMp);
                newBusClock = (float)(TimeStampCounterFrequency / maxMp);
            }
            else
            {
                // Fail-safe value - if the code above fails, we'll use this instead
                _coreClocks[i].Value = (float)TimeStampCounterFrequency;
            }
        }

        // Set sensor
        if (!(newBusClock > 0)) return;
        _busClock.Value = (float)newBusClock;
        ActivateSensor(_busClock);
    }

    #endregion
}
