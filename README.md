# LibreHardwareMonitor
[![GitHub license](https://img.shields.io/github/license/LibreHardwareMonitor/LibreHardwareMonitor)](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/blob/master/LICENSE) [![Nuget](https://img.shields.io/nuget/v/LibreHardwareMonitorLib)](https://www.nuget.org/packages/LibreHardwareMonitorLib/) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/LibreHardwareMonitorLib?label=nuget-pre)](https://www.nuget.org/packages/LibreHardwareMonitorLib/) [![Nuget](https://img.shields.io/nuget/dt/LibreHardwareMonitorLib?label=nuget-downloads)](https://www.nuget.org/packages/LibreHardwareMonitorLib/)

Libre Hardware Monitor, a fork of Open Hardware Monitor, is free software that can monitor the temperature sensors, fan speeds, voltages, load and clock speeds of your computer. 

## What's included?
| Name| .NET | Build Status |
| --- | --- | --- | 
| **LibreHardwareMonitor** <br /> Windows Forms based application that presents all data in a graphical interface | .NET Framework 4.7.2 | [![Build status](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/workflows/CI/badge.svg)](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/actions) | 
| **LibreHardwareMonitorLib** <br /> Library that allows you to use all features in your own application | .NET Framework 4.7.2, .NET 6.0, and .NET 7.0 | [![Build status](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/workflows/CI/badge.svg)](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/actions) | 

## What can it do?
You can read information from devices such as:
- Motherboards
- Intel and AMD processors
- NVIDIA and AMD graphics cards
- HDD, SSD and NVMe hard drives
- Network cards

## Where can I download it?
You can download the latest release [here](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/releases).

### Nightly builds
If you have a **GitHub** account, you can download nightly builds [here](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/actions). Otherwise, you can download the *latest* nightly build [here](https://nightly.link/LibreHardwareMonitor/LibreHardwareMonitor/workflows/master/master/LibreHardwareMonitor-net472.zip).

## How can I help improve it?
The LibreHardwareMonitor team welcomes feedback and contributions!<br/>
You can check if it works properly on your motherboard. For many manufacturers, the way of reading data differs a bit, so if you notice any inaccuracies, please send us a pull request. If you have any suggestions or improvements, don't hesitate to create an issue.

## Developer information
**Integrate the library in own application**
1. Add the [LibreHardwareMonitorLib](https://www.nuget.org/packages/LibreHardwareMonitorLib/) NuGet package to your application.
2. Use the sample code below.


**Sample code**
```c#
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

**Administrator rights**

Some sensors require administrator privileges to access the data. Restart your IDE with admin privileges, or add an [app.manifest](https://learn.microsoft.com/en-us/windows/win32/sbscs/application-manifests) file to your project with requestedExecutionLevel on requireAdministrator.


## License
LibreHardwareMonitor is free and open source software licensed under MPL 2.0. You can use it in private and commercial projects. Keep in mind that you must include a copy of the license in your project.
