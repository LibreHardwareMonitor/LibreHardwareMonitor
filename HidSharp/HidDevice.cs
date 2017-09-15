#region License
/* Copyright 2010-2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace HidSharp
{
    /// <summary>
    /// Represents a USB HID class device.
    /// </summary>
    [ComVisible(true), Guid("4D8A9A1A-D5CC-414e-8356-5A025EDA098D")]
    public abstract class HidDevice
    {
        /// <summary>
        /// Makes a connection to the USB HID class device, or throws an exception if the connection cannot be made.
        /// </summary>
        /// <returns>The stream to use to communicate with the device.</returns>
        public abstract HidStream Open();

        /// <summary>
        /// Returns the raw report descriptor of the USB device.
        /// Currently this is only supported on Linux.
        /// </summary>
        /// <returns>The report descriptor.</returns>
        public virtual byte[] GetReportDescriptor()
        {
            throw new NotSupportedException(); // Windows without libusb can't... Linux can.
        }

        /// <summary>
        /// Tries to make a connection to the USB HID class device.
        /// </summary>
        /// <param name="stream">The stream to use to communicate with the device.</param>
        /// <returns>True if the connetion was successful.</returns>
        public bool TryOpen(out HidStream stream)
        {
            try
			{
				stream = Open();
				return true;
			}
            catch (Exception e)
			{
#if DEBUG
				Console.WriteLine(e);
#endif
				stream = null; return false;
			}
		}

        /// <summary>
        /// The operating system's name for the device.
        /// 
        /// If you have multiple devices with the same Vendor ID, Product ID, Serial Number. etc.,
        /// this may be useful for differentiating them.
        /// </summary>
        public abstract string DevicePath
        {
            get;
        }

        /// <summary>
        /// The maximum input report length, including the Report ID byte.
        /// If the device does not use Report IDs, the first byte will always be 0.
        /// </summary>
        public abstract int MaxInputReportLength { get; }

        /// <summary>
        /// The maximum output report length, including the Report ID byte.
        /// If the device does not use Report IDs, use 0 for the first byte.
        /// </summary>
        public abstract int MaxOutputReportLength { get; }

        /// <summary>
        /// The maximum feature report length, including the Report ID byte.
        /// If the device does not use Report IDs, use 0 for the first byte.
        /// </summary>
        public abstract int MaxFeatureReportLength { get; }

        /// <summary>
        /// The manufacturer name.
        /// </summary>
        public abstract string Manufacturer
        {
            get;
        }

        /// <summary>
        /// The USB product ID. These are listed at: http://usb-ids.gowdy.us
        /// </summary>
        public abstract int ProductID
        {
            get;
        }

        /// <summary>
        /// The product name.
        /// </summary>
        public abstract string ProductName
        {
            get;
        }

        /// <summary>
        /// The product version.
        /// This is a 16-bit number encoding the major and minor versions in the upper and lower 8 bits, respectively.
        /// </summary>
        public abstract int ProductVersion
        {
            get;
        }

        /// <summary>
        /// The device serial number.
        /// </summary>
        public abstract string SerialNumber
        {
            get;
        }

        /// <summary>
        /// The USB vendor ID. These are listed at: http://usb-ids.gowdy.us
        /// </summary>
        public abstract int VendorID
        {
            get;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} ({1}VID {2}, PID {3}, version {4})",
                Manufacturer.Length > 0 || ProductName.Length > 0 ? Manufacturer.Trim() + " " + ProductName.Trim() : "(unnamed)",
                SerialNumber.Length > 0 ? "serial " + SerialNumber.Trim() + ", " : "", VendorID, ProductID, ProductVersion);
        }
    }
}
