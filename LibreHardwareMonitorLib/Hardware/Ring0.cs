// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

//SecurityIdentifier

namespace LibreHardwareMonitor.Hardware
{
    internal static class Ring0
    {
        private static KernelDriver _driver;
        private static string _fileName;
        private static Mutex _isaBusMutex;
        private static Mutex _pciBusMutex;

        private static readonly StringBuilder _report = new StringBuilder();
        
        public static bool IsOpen
        {
            get { return _driver != null; }
        }

        private static Assembly GetAssembly()
        {
            return typeof(Ring0).Assembly;
        }

        private static string GetTempFileName()
        {
            // try to create one in the application folder
            string location = GetAssembly().Location;
            if (!string.IsNullOrEmpty(location))
            {
                try
                {
                    string fileName = Path.ChangeExtension(location, ".sys");

                    using (File.Create(fileName))
                        return fileName;
                }
                catch (Exception)
                { }
            }

            // if this failed, try to get a file in the temporary folder
            try
            {
                return Path.GetTempFileName();
            }
            catch (IOException)
            {
                // some I/O exception
            }
            catch (UnauthorizedAccessException)
            {
                // we do not have the right to create a file in the temp folder
            }
            catch (NotSupportedException)
            {
                // invalid path format of the TMP system environment variable
            }

            return null;
        }

        private static bool ExtractDriver(string fileName)
        {
            string resourceName = nameof(LibreHardwareMonitor) + "." + nameof(Hardware) + "." + (Software.OperatingSystem.Is64Bit ? "WinRing0x64.sys" : "WinRing0.sys");

            string[] names = GetAssembly().GetManifestResourceNames();
            byte[] buffer = null;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].Replace('\\', '.') == resourceName)
                {
                    using (Stream stream = GetAssembly().GetManifestResourceStream(names[i]))
                    {
                        if (stream != null)
                        {
                            buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, buffer.Length);
                        }
                    }
                }
            }

            if (buffer == null)
                return false;


            try
            {
                using (FileStream target = new FileStream(fileName, FileMode.Create))
                {
                    target.Write(buffer, 0, buffer.Length);
                    target.Flush();
                }
            }
            catch (IOException)
            {
                // for example there is not enough space on the disk
                return false;
            }

            // make sure the file is actually written to the file system
            for (int i = 0; i < 20; i++)
            {
                try
                {
                    if (File.Exists(fileName) &&
                        new FileInfo(fileName).Length == buffer.Length)
                    {
                        return true;
                    }

                    Thread.Sleep(100);
                }
                catch (IOException)
                {
                    Thread.Sleep(10);
                }
            }

            // file still has not the right size, something is wrong
            return false;
        }

        public static void Open()
        {
            // no implementation for unix systems
            if (Software.OperatingSystem.IsUnix)
                return;

            if (_driver != null)
                return;


            // clear the current report
            _report.Length = 0;

            _driver = new KernelDriver("WinRing0_1_2_0");
            _driver.Open();

            if (!_driver.IsOpen)
            {
                // driver is not loaded, try to install and open
                _fileName = GetTempFileName();
                if (_fileName != null && ExtractDriver(_fileName))
                {
                    if (_driver.Install(_fileName, out string installError))
                    {
                        _driver.Open();

                        if (!_driver.IsOpen)
                        {
                            _driver.Delete();
                            _report.AppendLine("Status: Opening driver failed after install");
                        }
                    }
                    else
                    {
                        string errorFirstInstall = installError;

                        // install failed, try to delete and reinstall
                        _driver.Delete();

                        // wait a short moment to give the OS a chance to remove the driver
                        Thread.Sleep(2000);

                        if (_driver.Install(_fileName, out string errorSecondInstall))
                        {
                            _driver.Open();

                            if (!_driver.IsOpen)
                            {
                                _driver.Delete();
                                _report.AppendLine("Status: Opening driver failed after reinstall");
                            }
                        }
                        else
                        {
                            _report.AppendLine("Status: Installing driver \"" + _fileName + "\" failed" + (File.Exists(_fileName) ? " and file exists" : string.Empty));
                            _report.AppendLine("First Exception: " + errorFirstInstall);
                            _report.AppendLine("Second Exception: " + errorSecondInstall);
                        }
                    }
                }
                else
                {
                    _report.AppendLine("Status: Extracting driver failed");
                }

                try
                {
                    // try to delete the driver file
                    if (File.Exists(_fileName) && _fileName != null)
                        File.Delete(_fileName);

                    _fileName = null;
                }
                catch (IOException)
                { }
                catch (UnauthorizedAccessException)
                { }
            }

            if (!_driver.IsOpen)
                _driver = null;

            const string isaMutexName = "Global\\Access_ISABUS.HTP.Method";

            try
            {
#if NETSTANDARD2_0
                _isaBusMutex = new Mutex(false, isaMutexName);
#else
                //mutex permissions set to everyone to allow other software to access the hardware
                //otherwise other monitoring software cant access
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);
                _isaBusMutex = new Mutex(false, isaMutexName, out _, securitySettings);
#endif
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
#if NETSTANDARD2_0
                    _isaBusMutex = Mutex.OpenExisting(isaMutexName);
#else
                    _isaBusMutex = Mutex.OpenExisting(isaMutexName, MutexRights.Synchronize);
#endif
                }
                catch
                { }
            }

            const string pciMutexName = "Global\\Access_PCI";

            try
            {
                _pciBusMutex = new Mutex(false, pciMutexName);
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
#if NETSTANDARD2_0
                    _pciBusMutex = Mutex.OpenExisting(pciMutexName);
#else
                    _pciBusMutex = Mutex.OpenExisting(pciMutexName, MutexRights.Synchronize);
#endif
                }
                catch
                { }
            }
        }

        public static void Close()
        {
            if (_driver != null)
            {
                uint refCount = 0;
                _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_GET_REFCOUNT, null, ref refCount);
                _driver.Close();

                if (refCount <= 1)
                    _driver.Delete();

                _driver = null;
            }

            if (_isaBusMutex != null)
            {
                _isaBusMutex.Close();
                _isaBusMutex = null;
            }

            if (_pciBusMutex != null)
            {
                _pciBusMutex.Close();
                _pciBusMutex = null;
            }

            // try to delete temporary driver file again if failed during open
            if (_fileName != null && File.Exists(_fileName))
            {
                try
                {
                    File.Delete(_fileName);
                    _fileName = null;
                }
                catch (IOException)
                { }
                catch (UnauthorizedAccessException)
                { }
            }
        }

        public static string GetReport()
        {
            if (_report.Length > 0)
            {
                StringBuilder r = new StringBuilder();
                r.AppendLine("Ring0");
                r.AppendLine();
                r.Append(_report);
                r.AppendLine();
                return r.ToString();
            }

            return null;
        }

        public static bool WaitIsaBusMutex(int millisecondsTimeout)
        {
            if (_isaBusMutex == null)
                return true;


            try
            {
                return _isaBusMutex.WaitOne(millisecondsTimeout, false);
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static void ReleaseIsaBusMutex()
        {
            _isaBusMutex?.ReleaseMutex();
        }

        public static bool WaitPciBusMutex(int millisecondsTimeout)
        {
            if (_pciBusMutex == null)
                return true;


            try
            {
                return _pciBusMutex.WaitOne(millisecondsTimeout, false);
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public static void ReleasePciBusMutex()
        {
            _pciBusMutex?.ReleaseMutex();
        }

        public static bool ReadMsr(uint index, out uint eax, out uint edx)
        {
            if (_driver == null)
            {
                eax = 0;
                edx = 0;
                return false;
            }

            ulong buffer = 0;
            bool result = _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_READ_MSR, index, ref buffer);
            edx = (uint)((buffer >> 32) & 0xFFFFFFFF);
            eax = (uint)(buffer & 0xFFFFFFFF);
            return result;
        }

        public static bool ReadMsr(uint index, out uint eax, out uint edx, GroupAffinity affinity)
        {
            GroupAffinity previousAffinity = ThreadAffinity.Set(affinity);
            bool result = ReadMsr(index, out eax, out edx);
            ThreadAffinity.Set(previousAffinity);
            return result;
        }

        public static bool WriteMsr(uint index, uint eax, uint edx)
        {
            if (_driver == null)
                return false;


            WriteMsrInput input = new WriteMsrInput { Register = index, Value = ((ulong)edx << 32) | eax };
            return _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_WRITE_MSR, input);
        }

        public static byte ReadIoPort(uint port)
        {
            if (_driver == null)
                return 0;


            uint value = 0;
            _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_READ_IO_PORT_BYTE, port, ref value);
            return (byte)(value & 0xFF);
        }

        public static void WriteIoPort(uint port, byte value)
        {
            if (_driver == null)
                return;


            WriteIoPortInput input = new WriteIoPortInput { PortNumber = port, Value = value };
            _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_WRITE_IO_PORT_BYTE, input);
        }

        public static uint GetPciAddress(byte bus, byte device, byte function)
        {
            return (uint)(((bus & 0xFF) << 8) | ((device & 0x1F) << 3) | (function & 7));
        }

        public static bool ReadPciConfig(uint pciAddress, uint regAddress, out uint value)
        {
            if (_driver == null || (regAddress & 3) != 0)
            {
                value = 0;
                return false;
            }

            ReadPciConfigInput input = new ReadPciConfigInput { PciAddress = pciAddress, RegAddress = regAddress };

            value = 0;
            return _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_READ_PCI_CONFIG, input, ref value);
        }

        public static bool WritePciConfig(uint pciAddress, uint regAddress, uint value)
        {
            if (_driver == null || (regAddress & 3) != 0)
                return false;


            WritePciConfigInput input = new WritePciConfigInput { PciAddress = pciAddress, RegAddress = regAddress, Value = value };
            return _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_WRITE_PCI_CONFIG, input);
        }

        public static bool ReadMemory<T>(ulong address, ref T buffer)
        {
            if (_driver == null)
                return false;


            ReadMemoryInput input = new ReadMemoryInput { Address = address, UnitSize = 1, Count = (uint)Marshal.SizeOf(buffer) };
            return _driver.DeviceIOControl(Interop.Ring0.IOCTL_OLS_READ_MEMORY, input, ref buffer);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WriteMsrInput
        {
            public uint Register;
            public ulong Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WriteIoPortInput
        {
            public uint PortNumber;
            public byte Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ReadPciConfigInput
        {
            public uint PciAddress;
            public uint RegAddress;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WritePciConfigInput
        {
            public uint PciAddress;
            public uint RegAddress;
            public uint Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ReadMemoryInput
        {
            public ulong Address;
            public uint UnitSize;
            public uint Count;
        }
    }
}
