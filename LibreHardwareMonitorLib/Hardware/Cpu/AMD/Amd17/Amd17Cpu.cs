// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Cpu.AMD.Amd17;

/// <summary>
/// AMD 17 CPU
/// </summary>
/// <seealso cref="AmdCpuBase" />
internal sealed class Amd17Cpu : AmdCpuBase
{
    #region Properties

    /// <summary>
    /// Gets the processor.
    /// </summary>
    /// <value>
    /// The processor.
    /// </value>
    public Amd17Processor Processor { get; }

    /// <summary>
    /// Gets the index of the sensor type.
    /// </summary>
    /// <value>
    /// The index of the sensor type.
    /// </value>
    public Dictionary<SensorType, int> SensorTypeIndex { get; }

    /// <summary>
    /// Gets the SMU.
    /// </summary>
    /// <value>
    /// The smu.
    /// </value>
    public RyzenSMU SMU { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="Amd17Cpu"/> class.
    /// </summary>
    /// <param name="processorIndex">Index of the processor.</param>
    /// <param name="cpuId">The cpu identifier.</param>
    /// <param name="settings">The settings.</param>
    public Amd17Cpu(int processorIndex, CpuId[][] cpuId, ISettings settings) : base(processorIndex, cpuId, settings)
    {
        SensorTypeIndex = new Dictionary<SensorType, int>();
        foreach (SensorType type in Enum.GetValues(typeof(SensorType)))
        {
            SensorTypeIndex.Add(type, 0);
        }

        SensorTypeIndex[SensorType.Load] = _active.Count(x => x.SensorType == SensorType.Load);

        SMU = new RyzenSMU(Family, Model, PackageType);

        // Add all numa nodes.
        // Register ..1E_2, [10:8] + 1
        Processor = new Amd17Processor(this);

        // Add all numa nodes.
        int coreId = 0;
        int lastCoreId = -1; // Invalid id.

        // Ryzen 3000's skip some core ids.
        // So start at 1 and count upwards when the read core changes.
        foreach (CpuId[] cpu in cpuId.OrderBy(x => x[0].ExtData[0x1e, 1] & 0xFF))
        {
            CpuId thread = cpu[0];

            // CPUID_Fn8000001E_EBX, Register ..1E_1, [7:0]
            // threads per core =  CPUID_Fn8000001E_EBX[15:8] + 1
            // CoreId: core ID =  CPUID_Fn8000001E_EBX[7:0]
            int coreIdRead = (int)(thread.ExtData[0x1e, 1] & 0xff);

            // CPUID_Fn8000001E_ECX, Node Identifiers, Register ..1E_2
            // NodesPerProcessor =  CPUID_Fn8000001E_ECX[10:8]
            // nodeID =  CPUID_Fn8000001E_ECX[7:0]
            int nodeId = (int)(thread.ExtData[0x1e, 2] & 0xff);

            if (coreIdRead != lastCoreId)
            {
                coreId++;
            }
            lastCoreId = coreIdRead;

            Processor.AppendThread(thread, nodeId, coreId);
        }

        // Initialize
        Initialize();

        // Update
        Update();
    }

    #endregion

    /// <summary>
    /// Gets the MSRS.
    /// </summary>
    /// <returns></returns>
    protected override uint[] GetMsrs() =>
    [
        Amd17Constants.PERF_CTL_0,
        Amd17Constants.PERF_CTR_0,
        Amd17Constants.HWCR,
        Amd17Constants.MSR_PSTATE_0,
        Amd17Constants.COFVID_STATUS
    ];

    /// <summary>
    /// Prints the data to a report.
    /// </summary>
    /// <returns></returns>
    /// <inheritdoc />
    public override string GetReport()
    {
        StringBuilder r = new();
        r.Append(base.GetReport());
        r.Append(SMU.GetReport());
        return r.ToString();
    }

    /// <summary>
    /// Updates all sensors.
    /// </summary>
    /// <inheritdoc />
    public override void Update()
    {
        base.Update();

        // Update processor sensors
        Processor.UpdateSensors();

        // Evaluate Numa Nodes
        foreach (Amd17NumaNode node in Processor.Nodes)
        {
            // Update Numa Node
            Amd17NumaNode.UpdateSensors();
            foreach (Amd17Core c in node.Cores)
            {
                c.UpdateSensors();
            }
        }
    }


}
