# LibreHardwareMonitor
Libre Hardware Monitor, a fork of Open Hardware Monitor, is free software that can monitor the temperature sensors, fan speeds, voltages, load and clock speeds of your computer. 

## What's included?
| Name| .NET | Build Status |
| --- | --- | --- | 
| **LibreHardwareMonitor** <br /> Windows Forms based application that presents all data in a graphical interface | .NET Framework 4.5.2 | [![Build status](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/workflows/CI/badge.svg)](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/actions) | 
| **LibreHardwareMonitorLib** <br /> Library that allows you to use all features in your own application | .NET Framework 4.5.2,<br />.NET Standard 2.0,<br />.NET 5.0.0 | [![Build status](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/workflows/CI/badge.svg)](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/actions) | 

## What can you do?
With the help of LibreHardwareMonitor you can read information from devices such as:
- Motherboards
- Intel and AMD processors
- NVIDIA and AMD graphics cards
- HDD, SSD and NVMe hard drives
- Network cards

## Where can I download it?
You can download the latest release [here](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/releases).<br/>
If you're **signed in to GitHub**, you can also download the latest builds [here](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/actions). Click on a result and download **Binaries** under **Artifacts**.

## How can I help improve it?
The LibreHardwareMonitor team welcomes feedback and contributions!<br/>
You can check if it works properly on your motherboard. For many manufacturers, the way of reading data differs a bit, so if you notice any inaccuracies, please send us a pull request. If you have any suggestions or improvements, don't hesitate to create an issue.

## What's the easiest way to start?
**LibreHardwareMonitor application:**
1. Download the repository and compile 'LibreHardwareMonitor'.
2. You can start the application immediately.

**Sample code:**
1. Download the repository and compile 'LibreHardwareMonitorLib'.
2. Add references to 'LibreHardwareMonitorLib.dll' and 'HidSharp.dll' in your project.
3. In your main file, add namespace 'using LibreHardwareMonitor.Hardware;'
4. Now you can read most of the data from your devices.

```c#
/*
 * Example for .NET Framework
 */
public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }
    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
    }
    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}

public void Monitor()
{
    Computer computer = new Computer
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsControllerEnabled = true,
        IsNetworkEnabled = true,
        IsStorageEnabled = true
    };

    computer.Open();
    computer.Accept(new UpdateVisitor());

    foreach (IHardware hardware in computer.Hardware)
    {
        Console.WriteLine("Hardware: {0}", hardware.Name);
        
        foreach (IHardware subhardware in hardware.SubHardware)
        {
            Console.WriteLine("\tSubhardware: {0}", subhardware.Name);
            
            foreach (ISensor sensor in subhardware.Sensors)
            {
                Console.WriteLine("\t\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
            }
        }

        foreach (ISensor sensor in hardware.Sensors)
        {
            Console.WriteLine("\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
        }
    }
    
    computer.Close();
}
```

## What does the library contain?
1. Namespaces:
```c#
LibreHardwareMonitor.Hardware
LibreHardwareMonitor.Interop
LibreHardwareMonitor.Software
```

## License
LibreHardwareMonitor is free and open source software licensed under MPL 2.0. You can use it in private and commercial projects. Keep in mind that you must include a copy of the license in your project.
