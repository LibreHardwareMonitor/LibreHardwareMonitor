// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using System.Text;
using System.Security.Principal;        //SecurityIdentifier

namespace LibreHardwareMonitor.Hardware
{
    internal static class Ring0
    {
        private static KernelDriver _driver;
        private static string _fileName;
        private static Mutex _isaBusMutex;
        private static readonly StringBuilder _report = new StringBuilder();

        private const uint OLS_TYPE = 40000;
        private static Interop.Kernel32.IOControlCode
            IOCTL_OLS_GET_REFCOUNT = new Interop.Kernel32.IOControlCode(OLS_TYPE, 0x801, Interop.Kernel32.IOControlCode.Access.Any),
            IOCTL_OLS_GET_DRIVER_VERSION = new Interop.Kernel32.IOControlCode(OLS_TYPE, 0x800, Interop.Kernel32.IOControlCode.Access.Any),
            IOCTL_OLS_READ_MSR = new Interop.Kernel32.IOControlCode(OLS_TYPE, 0x821, Interop.Kernel32.IOControlCode.Access.Any),
            IOCTL_OLS_WRITE_MSR = new Interop.Kernel32.IOControlCode(OLS_TYPE, 0x822, Interop.Kernel32.IOControlCode.Access.Any),
            IOCTL_OLS_READ_IO_PORT_BYTE = new Interop.Kernel32.IOControlCode(OLS_TYPE, 0x833, Interop.Kernel32.IOControlCode.Access.Read),
            IOCTL_OLS_WRITE_IO_PORT_BYTE = new Interop.Kernel32.IOControlCode(OLS_TYPE, 0x836, Interop.Kernel32.IOControlCode.Access.Write),
            IOCTL_OLS_READ_PCI_CONFIG = new Interop.Kernel32.IOControlCode(OLS_TYPE, 0x851, Interop.Kernel32.IOControlCode.Access.Read),
            IOCTL_OLS_WRITE_PCI_CONFIG = new Interop.Kernel32.IOControlCode(OLS_TYPE, 0x852, Interop.Kernel32.IOControlCode.Access.Write),
            IOCTL_OLS_READ_MEMORY = new Interop.Kernel32.IOControlCode(OLS_TYPE, 0x841, Interop.Kernel32.IOControlCode.Access.Read);

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
                    using (FileStream stream = File.Create(fileName))
                    {
                        return fileName;
                    }
                }
                catch (Exception) { }
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
            string resourceName = "LibreHardwareMonitor.Hardware." + (Software.OperatingSystem.Is64Bit ? "WinRing0x64.sys" : "WinRing0.sys");

            string[] names = GetAssembly().GetManifestResourceNames();
            byte[] buffer = null;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].Replace('\\', '.') == resourceName)
                {
                    using (Stream stream = GetAssembly().GetManifestResourceStream(names[i]))
                    {
                        buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
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

            // make sure the file is actually writen to the file system
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
            if (Software.OperatingSystem.IsLinux)
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
                    string installError;
                    if (_driver.Install(_fileName, out installError))
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

                        string errorSecondInstall;
                        if (_driver.Install(_fileName, out errorSecondInstall))
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
                            _report.AppendLine("Status: Installing driver \"" + _fileName + "\" failed" + (File.Exists(_fileName) ? " and file exists" : ""));
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
                    // try to delte the driver file
                    if (File.Exists(_fileName))
                        File.Delete(_fileName);
                    _fileName = null;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            if (!_driver.IsOpen)
                _driver = null;

            string mutexName = "Global\\Access_ISABUS.HTP.Method";
            try
            {
#if NETSTANDARD2_0
        _isaBusMutex = new Mutex(false, mutexName);
#else
                //mutex permissions set to everyone to allow other software to access the hardware
                //otherwise other monitoring software cant access
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);
                _isaBusMutex = new Mutex(false, mutexName, out _, securitySettings);
#endif
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
#if NETSTANDARD2_0
                    _isaBusMutex = Mutex.OpenExisting(mutexName);
#else
                    _isaBusMutex = Mutex.OpenExisting(mutexName, MutexRights.Synchronize);
#endif
                }
                catch { }
            }
        }

        public static bool IsOpen
        {
            get { return _driver != null; }
        }

        public static void Close()
        {
            if (_driver != null)
            {
                uint refCount = 0;
                _driver.DeviceIOControl(IOCTL_OLS_GET_REFCOUNT, null, ref refCount);
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

            // try to delete temporary driver file again if failed during open
            if (_fileName != null && File.Exists(_fileName))
            {
                try
                {
                    File.Delete(_fileName);
                    _fileName = null;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        public static ulong ThreadAffinitySet(ulong mask)
        {
            return ThreadAffinity.Set(mask);
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
            else
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
            catch (AbandonedMutexException) { return true; }
            catch (InvalidOperationException) { return false; }
        }

        public static void ReleaseIsaBusMutex()
        {
            if (_isaBusMutex == null)
                return;
            _isaBusMutex.ReleaseMutex();
        }

        public static bool Rdmsr(uint index, out uint eax, out uint edx)
        {
            if (_driver == null)
            {
                eax = 0;
                edx = 0;
                return false;
            }

            ulong buffer = 0;
            bool result = _driver.DeviceIOControl(IOCTL_OLS_READ_MSR, index, ref buffer);
            edx = (uint)((buffer >> 32) & 0xFFFFFFFF);
            eax = (uint)(buffer & 0xFFFFFFFF);
            return result;
        }

        public static bool RdmsrTx(uint index, out uint eax, out uint edx, ulong threadAffinityMask)
        {
            ulong mask = ThreadAffinity.Set(threadAffinityMask);
            bool result = Rdmsr(index, out eax, out edx);
            ThreadAffinity.Set(mask);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WrmsrInput
        {
            public uint Register;
            public ulong Value;
        }

        public static bool Wrmsr(uint index, uint eax, uint edx)
        {
            if (_driver == null)
                return false;

            WrmsrInput input = new WrmsrInput();
            input.Register = index;
            input.Value = ((ulong)edx << 32) | eax;
            return _driver.DeviceIOControl(IOCTL_OLS_WRITE_MSR, input);
        }

        public static byte ReadIoPort(uint port)
        {
            if (_driver == null)
                return 0;

            uint value = 0;
            _driver.DeviceIOControl(IOCTL_OLS_READ_IO_PORT_BYTE, port, ref value);
            return (byte)(value & 0xFF);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WriteIoPortInput
        {
            public uint PortNumber;
            public byte Value;
        }

        public static void WriteIoPort(uint port, byte value)
        {
            if (_driver == null)
                return;

            WriteIoPortInput input = new WriteIoPortInput();
            input.PortNumber = port;
            input.Value = value;
            _driver.DeviceIOControl(IOCTL_OLS_WRITE_IO_PORT_BYTE, input);
        }

        public const uint InvalidPciAddress = 0xFFFFFFFF;

        public static uint GetPciAddress(byte bus, byte device, byte function)
        {
            return (uint)(((bus & 0xFF) << 8) | ((device & 0x1F) << 3) | (function & 7));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ReadPciConfigInput
        {
            public uint PciAddress;
            public uint RegAddress;
        }

        public static bool ReadPciConfig(uint pciAddress, uint regAddress, out uint value)
        {
            if (_driver == null || (regAddress & 3) != 0)
            {
                value = 0;
                return false;
            }

            ReadPciConfigInput input = new ReadPciConfigInput();
            input.PciAddress = pciAddress;
            input.RegAddress = regAddress;

            value = 0;
            return _driver.DeviceIOControl(IOCTL_OLS_READ_PCI_CONFIG, input, ref value);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct WritePciConfigInput
        {
            public uint PciAddress;
            public uint RegAddress;
            public uint Value;
        }

        public static bool WritePciConfig(uint pciAddress, uint regAddress, uint value)
        {
            if (_driver == null || (regAddress & 3) != 0)
                return false;

            WritePciConfigInput input = new WritePciConfigInput();
            input.PciAddress = pciAddress;
            input.RegAddress = regAddress;
            input.Value = value;
            return _driver.DeviceIOControl(IOCTL_OLS_WRITE_PCI_CONFIG, input);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ReadMemoryInput
        {
            public ulong address;
            public uint unitSize;
            public uint count;
        }

        public static bool ReadMemory<T>(ulong address, ref T buffer)
        {
            if (_driver == null)
                return false;

            ReadMemoryInput input = new ReadMemoryInput();
            input.address = address;
            input.unitSize = 1;
            input.count = (uint)Marshal.SizeOf(buffer);
            return _driver.DeviceIOControl(IOCTL_OLS_READ_MEMORY, input, ref buffer);
        }
    }
}
