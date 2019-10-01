using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreHardwareMonitorReport
{
    class Program
    {
        static void Main(string[] args)
        {
            Computer computer = new Computer();

            computer.IsCpuEnabled = true;
            computer.IsGpuEnabled = true;
            computer.IsStorageEnabled = true;
            computer.IsMotherboardEnabled = true;
            computer.IsMemoryEnabled = true;
            computer.IsControllerEnabled = true;
            computer.IsNetworkEnabled = true;

            computer.Open();

            computer.Accept(new UpdateVisitor());

            Console.Out.Write(computer.GetReport());

            computer.Close();

        }
    }
    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware)
                subHardware.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }

}
