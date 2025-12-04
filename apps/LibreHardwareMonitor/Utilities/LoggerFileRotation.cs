using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Utilities
{
    public enum LoggerFileRotation
    {
        // Keep the same file for the entire record session
        PerSession = 0,

        // Create a new file every day
        Daily,
    }
}
