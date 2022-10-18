using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.Utilities
{
    public interface ILogger
    {
        IComputer Computer { get; }
        DateTime LastLoggedTime { get; }

        TimeSpan LoggingInterval { get; set; }

        //void Log();

        void Log(bool selecetiveLogging = false, List<string> Identifiers = null);
    }
}
