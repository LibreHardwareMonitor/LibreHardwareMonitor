// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.PawnIo;

namespace LibreHardwareMonitorCli;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Monitor();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex}");
            return 1;
        }
    }

    private static void Monitor()
    {
        Computer computer = new Computer
        {
            IsStorageEnabled = true,
            IsPsuEnabled = true,
            IsNetworkEnabled = true,
            IsMotherboardEnabled = true,
            IsMemoryEnabled = true,
            IsPowerMonitorEnabled = true,
            IsGpuEnabled = true,
            IsCpuEnabled = true,
            IsControllerEnabled = true,
            IsBatteryEnabled = true,
        };

        if (!PawnIo.IsInstalled || PawnIo.Version < new Version(2, 2, 0, 0))
        {
            InstallPawnIO();
        }

        try
        {
            computer.Open();
            computer.Accept(new UpdateVisitor());

            foreach (IHardware hardware in computer.Hardware)
        {
            Console.WriteLine("{0}: {1}", hardware.HardwareType, hardware.Name);

            foreach (IHardware subhardware in hardware.SubHardware)
            {
                Console.WriteLine("\tSubhardware: {0}", subhardware.Name);

                foreach (ISensor sensor in subhardware.Sensors)
                {
                    Console.WriteLine("\t{0}: {1} {2}", sensor.Name, sensor.Value, sensor.SensorType);
                }
            }

            foreach (ISensor sensor in hardware.Sensors)
            {
                Console.WriteLine("\t{0}: {1} {2}", sensor.Name, sensor.Value, sensor.SensorType);
            }
        }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error while monitoring hardware: {ex.Message}");
        }
        finally
        {
            try
            {
                computer.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error while closing computer: {ex.Message}");
            }
        }
    }

    private static void InstallPawnIO()
    {
        string path = ExtractPawnIO();
        if (string.IsNullOrEmpty(path))
        {
            Console.Error.WriteLine("PawnIO installer resource not found or extraction failed.");
            return;
        }

        if (!IsUserAdministrator())
        {
            Console.Error.WriteLine("Warning: running PawnIO installer without administrative privileges may fail.");
        }

        var psi = new ProcessStartInfo(path, "-install -silent")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process? process = null;
        try
        {
            process = Process.Start(psi);
            if (process is not null)
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine(output);
                if (!string.IsNullOrEmpty(error))
                    Console.Error.WriteLine(error);

                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine($"PawnIO installer exited with code {process.ExitCode}.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to run PawnIO installer: {ex.Message}");
        }
        finally
        {
            try
            {
                if (process is not null)
                {
                    process.Dispose();
                }

                File.Delete(path);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to delete extracted installer: {ex.Message}");
            }
        }
    }

    private static string? ExtractPawnIO()
    {
        string? resourceName = Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("PawnIO_setup.exe", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return null;

        string destination = Path.Combine(Path.GetTempPath(), $"PawnIO_setup_{Guid.NewGuid():N}.exe");

        try
        {
            using Stream? resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (resourceStream is null)
                return null;

            using FileStream fileStream = new(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            resourceStream.CopyTo(fileStream);

            return destination;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to extract PawnIO installer: {ex.Message}");
            return null;
        }
    }

    private static bool IsUserAdministrator()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
