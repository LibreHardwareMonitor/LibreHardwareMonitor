using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.Utilities
{
    public class SqlLogger
    {
        public IComputer Computer { get; private set; }

        public SqlLogger(IComputer computer)
        {
            Computer = computer;
            //Computer.HardwareAdded += HardwareAdded;
            //Computer.HardwareRemoved += HardwareRemoved;
        }
    }
}
