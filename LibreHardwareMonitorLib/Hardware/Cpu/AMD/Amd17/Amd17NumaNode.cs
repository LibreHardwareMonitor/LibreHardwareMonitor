using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Cpu.AMD.Amd17
{
    /// <summary>
    /// AMD 17 Numa Node
    /// </summary>
    internal class Amd17NumaNode
    {
        private readonly Amd17Cpu _cpu;

        public List<Amd17Core> Cores { get; }

        public int NodeId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Amd17NumaNode"/> class.
        /// </summary>
        /// <param name="cpu">The cpu.</param>
        /// <param name="id">The identifier.</param>
        public Amd17NumaNode(Amd17Cpu cpu, int id)
        {
            Cores = new List<Amd17Core>();
            NodeId = id;
            _cpu = cpu;
        }

        /// <summary>
        /// Appends the thread.
        /// </summary>
        /// <param name="thread">The thread.</param>
        /// <param name="coreId">The core identifier.</param>
        public void AppendThread(CpuId thread, int coreId)
        {
            Amd17Core core = null;
            foreach (Amd17Core c in Cores)
            {
                if (c.CoreId == coreId)
                    core = c;
            }

            if (core == null)
            {
                core = new Amd17Core(_cpu, coreId);
                Cores.Add(core);
            }

            if (thread != null)
                core.Threads.Add(thread);
        }

        /// <summary>
        /// Updates the sensors.
        /// </summary>
        public static void UpdateSensors() { }
    }
}
