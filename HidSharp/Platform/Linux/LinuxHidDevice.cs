#region License
/* Copyright 2012-2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

   Permission to use, copy, modify, and/or distribute this software for any
   purpose with or without fee is hereby granted, provided that the above
   copyright notice and this permission notice appear in all copies.

   THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
   WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
   MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
   ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
   WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
   ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
   OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE. */
#endregion

using System;

namespace HidSharp.Platform.Linux
{
    class LinuxHidDevice : HidDevice
    {
        string _manufacturer;
        string _productName;
        string _serialNumber;
        byte[] _reportDescriptor;
        int _vid, _pid, _version;
        int _maxInput, _maxOutput, _maxFeature;
        bool _reportsUseID;
        string _path;

        public LinuxHidDevice(string path)
        {
            _path = path;
        }

        public override HidStream Open()
        {
            var stream = new LinuxHidStream();
            try { stream.Init(_path, this); return stream; }
            catch { stream.Close(); throw; }
        }

        public override byte[] GetReportDescriptor()
        {
            return (byte[])_reportDescriptor.Clone();
        }

        static bool TryParseReportDescriptor(IntPtr device, out ReportDescriptors.Parser.ReportDescriptorParser parser, out byte[] reportDescriptor)
        {
            parser = null; reportDescriptor = null;
			string devnode = NativeMethods.udev_device_get_devnode(device);
            if (null == devnode) { return false; }
            
            int handle = NativeMethods.retry(() => NativeMethods.open
                                        (devnode, NativeMethods.oflag.NONBLOCK));
            if (handle < 0) { return false; }

            try
            {
                uint descsize;
                if (NativeMethods.ioctl(handle, NativeMethods.HIDIOCGRDESCSIZE, out descsize) < 0) { return false; }
                if (descsize > NativeMethods.HID_MAX_DESCRIPTOR_SIZE) { return false; }

                var desc = new NativeMethods.hidraw_report_descriptor() { size = descsize };
                if (NativeMethods.ioctl(handle, NativeMethods.HIDIOCGRDESC, ref desc) < 0) { return false; }

                Array.Resize(ref desc.value, (int)descsize);
                parser = new ReportDescriptors.Parser.ReportDescriptorParser();
                parser.Parse(desc.value); reportDescriptor = desc.value; return true;
            }
            finally
            {
                NativeMethods.retry(() => NativeMethods.close(handle));
            }
        }

        internal unsafe bool GetInfo()
        {
            IntPtr udev = NativeMethods.udev_new();
            if (IntPtr.Zero != udev)
            {
                try
                {
                    IntPtr device = NativeMethods.udev_device_new_from_syspath(udev, _path);
                    if (device != IntPtr.Zero)
                    {
                        try
                        {
                            IntPtr parent = NativeMethods.udev_device_get_parent_with_subsystem_devtype(device, "usb", "usb_device");
                            if (IntPtr.Zero != parent)
                            {
                                string manufacturer = NativeMethods.udev_device_get_sysattr_value(parent, "manufacturer") ?? "";
                                string productName = NativeMethods.udev_device_get_sysattr_value(parent, "product") ?? "";
                                string serialNumber = NativeMethods.udev_device_get_sysattr_value(parent, "serial") ?? "";
                                string idVendor = NativeMethods.udev_device_get_sysattr_value(parent, "idVendor");
                                string idProduct = NativeMethods.udev_device_get_sysattr_value(parent, "idProduct");
                                string version = NativeMethods.udev_device_get_sysattr_value(parent, "version");

                                int vid, pid, verMajor, verMinor;
                                if (NativeMethods.TryParseHex(idVendor, out vid) &&
                                    NativeMethods.TryParseHex(idProduct, out pid) &&
                                    NativeMethods.TryParseVersion(version, out verMajor, out verMinor))
                                {
                                    _vid = vid;
                                    _pid = pid;
                                    _version = verMajor << 8 | verMinor;
                                    _manufacturer = manufacturer;
                                    _productName = productName;
                                    _serialNumber = serialNumber;

                                    ReportDescriptors.Parser.ReportDescriptorParser parser;
                                    if (TryParseReportDescriptor(device, out parser, out _reportDescriptor))
                                    {
                                        // Follow the Windows convention: No Report ID? Report ID is 0.
                                        // So, it's always one byte above the parser's result.
                                        _maxInput = parser.InputReportMaxLength; if (_maxInput > 0) { _maxInput++; }
                                        _maxOutput = parser.OutputReportMaxLength; if (_maxOutput > 0) { _maxOutput++; }
                                        _maxFeature = parser.FeatureReportMaxLength; if (_maxFeature > 0) { _maxFeature++; }
                                        _reportsUseID = parser.ReportsUseID;
                                        return true;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            NativeMethods.udev_device_unref(device);
                        }
                    }
                }
                finally
                {
                    NativeMethods.udev_unref(udev);
                }
            }

            return false;
        }

        public override string DevicePath
        {
            get { return _path; }
        }

        public override int MaxInputReportLength
        {
            get { return _maxInput; }
        }

        public override int MaxOutputReportLength
        {
            get { return _maxOutput; }
        }

        public override int MaxFeatureReportLength
        {
            get { return _maxFeature; }
        }

        internal bool ReportsUseID
        {
            get { return _reportsUseID; }
        }

        public override string Manufacturer
        {
            get { return _manufacturer; }
        }

        public override int ProductID
        {
            get { return _pid; }
        }

        public override string ProductName
        {
            get { return _productName; }
        }

        public override int ProductVersion
        {
            get { return _version; }
        }

        public override string SerialNumber
        {
            get { return _serialNumber; }
        }

        public override int VendorID
        {
            get { return _vid; }
        }
    }
}
