#region License
/* Copyright 2010, 2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

#pragma warning disable 618

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.Windows
{
    sealed class WinHidDevice : HidDevice
    {
        string _path;
        string _manufacturer;
        string _productName;
        string _serialNumber;
        int _vid, _pid, _version;
        int _maxInput, _maxOutput, _maxFeature;

        object _completeSync = new object();
        volatile bool _complete;

        internal WinHidDevice(string path)
        {
            _path = path;
        }

        void WaitForCompletion()
        {
            lock (_completeSync)
            {
                while (!_complete) { Monitor.Wait(_completeSync); }
            }
        }

        public override HidStream Open()
        {
            WaitForCompletion();
            var stream = new WinHidStream();
            try { stream.Init(_path, this); return stream; }
            catch { stream.Close(); throw; }
        }

        internal bool GetInfo(IntPtr handle)
        {
            NativeMethods.HIDD_ATTRIBUTES attributes = new NativeMethods.HIDD_ATTRIBUTES();
            attributes.Size = Marshal.SizeOf(attributes);
            if (!NativeMethods.HidD_GetAttributes(handle, ref attributes)) { return false; }
            
            _pid = attributes.ProductID;
            _vid = attributes.VendorID;
            _version = attributes.VersionNumber;
            return true;
        }

        internal void GetInfoComplete(IntPtr handle)
        {
            try
            {
                char[] buffer = new char[128];

                _manufacturer = NativeMethods.HidD_GetManufacturerString(handle, buffer, 256) ? NativeMethods.NTString(buffer) : "";
                _productName = NativeMethods.HidD_GetProductString(handle, buffer, 256) ? NativeMethods.NTString(buffer) : "";
                _serialNumber = NativeMethods.HidD_GetSerialNumberString(handle, buffer, 256) ? NativeMethods.NTString(buffer) : "";

                IntPtr preparsed;
                if (NativeMethods.HidD_GetPreparsedData(handle, out preparsed))
                {
                    NativeMethods.HIDP_CAPS caps;
                    int statusCaps = NativeMethods.HidP_GetCaps(preparsed, out caps);
                    if (statusCaps == NativeMethods.HIDP_STATUS_SUCCESS)
                    {
                        _maxInput = caps.InputReportByteLength;
                        _maxOutput = caps.OutputReportByteLength;
                        _maxFeature = caps.FeatureReportByteLength;
                    }
                    NativeMethods.HidD_FreePreparsedData(preparsed);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(handle);
            }

            lock (_completeSync) { _complete = true; Monitor.PulseAll(_completeSync); }
        }

        public override string DevicePath
        {
            get { return _path; }
        }

        public override int MaxInputReportLength
        {
            get { WaitForCompletion(); return _maxInput; }
        }

        public override int MaxOutputReportLength
        {
            get { WaitForCompletion(); return _maxOutput; }
        }

        public override int MaxFeatureReportLength
        {
            get { WaitForCompletion(); return _maxFeature; }
        }

        public override string Manufacturer
        {
            get { WaitForCompletion(); return _manufacturer; }
        }

        public override int ProductID
        {
            get { return _pid; }
        }

        public override string ProductName
        {
            get { WaitForCompletion(); return _productName; }
        }

        public override int ProductVersion
        {
            get { return _version; }
        }

        public override string SerialNumber
        {
            get { WaitForCompletion(); return _serialNumber; }
        }

        public override int VendorID
        {
            get { return _vid; }
        }
    }
}
