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

namespace HidSharp.Platform.MacOS
{
    class MacHidDevice : HidDevice
    {
        string _manufacturer;
        string _productName;
        string _serialNumber;
        int _vid, _pid, _version;
        int _maxInput, _maxOutput, _maxFeature;
        NativeMethods.io_string_t _path;

        internal MacHidDevice(NativeMethods.io_string_t path)
        {
            _path = path;
        }

        public override HidStream Open()
        {
            var stream = new MacHidStream();
            try { stream.Init(_path, this); return stream; }
            catch { stream.Close(); throw; }
        }

        internal bool GetInfo(int handle)
        {
            int? vid = NativeMethods.IORegistryEntryGetCFProperty_Int(handle, NativeMethods.kIOHIDVendorIDKey);
            int? pid = NativeMethods.IORegistryEntryGetCFProperty_Int(handle, NativeMethods.kIOHIDProductIDKey);
            int? version = NativeMethods.IORegistryEntryGetCFProperty_Int(handle, NativeMethods.kIOHIDVersionNumberKey);
            if (vid == null || pid == null || version == null) { return false; }

            _vid = (int)vid;
            _pid = (int)pid;
            _version = (int)version;
            _maxInput = NativeMethods.IORegistryEntryGetCFProperty_Int(handle, NativeMethods.kIOHIDMaxInputReportSizeKey) ?? 0;
            _maxOutput = NativeMethods.IORegistryEntryGetCFProperty_Int(handle, NativeMethods.kIOHIDMaxOutputReportSizeKey) ?? 0;
            _maxFeature = NativeMethods.IORegistryEntryGetCFProperty_Int(handle, NativeMethods.kIOHIDMaxFeatureReportSizeKey) ?? 0;
            _manufacturer = NativeMethods.IORegistryEntryGetCFProperty_String(handle, NativeMethods.kIOHIDManufacturerKey) ?? "";
            _productName = NativeMethods.IORegistryEntryGetCFProperty_String(handle, NativeMethods.kIOHIDProductKey) ?? "";
            _serialNumber = NativeMethods.IORegistryEntryGetCFProperty_String(handle, NativeMethods.kIOHIDSerialNumberKey) ?? "";
            return true;
        }

        public override string DevicePath
        {
            get { return _path.ToString(); }
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
