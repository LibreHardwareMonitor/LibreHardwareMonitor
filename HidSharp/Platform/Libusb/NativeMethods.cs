#region License
/* Copyright 2012 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Runtime.InteropServices;
using System.Text;

namespace HidSharp.Platform.Libusb
{
	static class NativeMethods
	{
		const string Libusb = "libusb-1.0";
		
		public struct DeviceDescriptor
		{
			public byte bLength, bDescriptorType;
			public ushort bcdUSB;
			public byte bDeviceClass, bDeviceSubClass, bDeviceProtocol, bMaxPacketSize0;
			public ushort idVendor, idProduct, bcdDevice;
			public byte iManufacturer, iProduct, iSerialNumber, bNumConfigurations;
		}
	
		public enum DeviceClass : byte
		{
			HID = 0x03,
			MassStorage = 0x08,
			VendorSpecific = 0xff
		}
		
		public enum DescriptorType : byte
		{
			Device = 0x01,
			Configuration = 0x02,
			String = 0x03,
			Interface = 0x04,
			Endpoint = 0x05,
			HID = 0x21,
			Report = 0x22,
			Physical = 0x23,
			Hub = 0x29
		}
		
		public enum EndpointDirection : byte
		{
			In = 0x80,
			Out = 0,
		}
		
		public enum Request : byte
		{
			GetDescriptor = 0x06
		}
		
		public enum RequestRecipient : byte
		{
			Device = 0,
			Interface = 1,
			Endpoint = 2,
			Other = 3
		}
		
		public enum RequestType : byte
		{
			Standard = 0x00,
			Class = 0x20,
			Vendor = 0x40
		}
		
		public enum TransferType : byte
		{
			Control = 0,
			Isochronous,
			Bulk,
			Interrupt	
		}
		
		public struct Version
		{
			public ushort Major, Minor, Micro, Nano;
		}
		
		public enum Error
		{
			None = 0,
			IO = -1,
			InvalidParameter = -2,
			AccessDenied = -3,
			NoDevice = -4,
			NotFound = -5,
			Busy = -6,
			Timeout = -7,
			Overflow = -8,
			Pipe = -9,
			Interrupted = -10,
			OutOfMemory = -11,
			NotSupported = -12
		}
		
		[DllImport(Libusb)]
		public static extern Error libusb_init(out IntPtr context);
		
		[DllImport(Libusb)]
		public static extern void libusb_set_debug(IntPtr context, int level);
		
		[DllImport(Libusb)]
		public static extern void libusb_exit(IntPtr context);
		
		[DllImport(Libusb)]
		public static extern IntPtr libusb_get_device_list(IntPtr context, out IntPtr list);
		
		[DllImport(Libusb)]
		public static extern void libusb_free_device_list(IntPtr context, IntPtr list);
		
		[DllImport(Libusb)]
		public static extern IntPtr libusb_ref_device(IntPtr device);
		
		[DllImport(Libusb)]
		public static extern void libusb_unref_device(IntPtr device);
		
		[DllImport(Libusb)]
		public static extern int libusb_get_max_packet_size(IntPtr device, byte endpoint);
		
		[DllImport(Libusb)]
		public static extern Error libusb_open(IntPtr device, out IntPtr deviceHandle);
		
		[DllImport(Libusb)]
		public static extern void libusb_close(IntPtr deviceHandle);
		
		[DllImport(Libusb)]
		public static extern Error libusb_get_configuration(IntPtr deviceHandle, out int configuration);
		
		[DllImport(Libusb)]
		public static extern Error libusb_set_configuration(IntPtr deviceHandle, int configuration);
		
		[DllImport(Libusb)]
		public static extern Error libusb_claim_interface(IntPtr deviceHandle, int @interface);
		
		[DllImport(Libusb)]
		public static extern Error libusb_release_interface(IntPtr deviceHandle, int @interface);
		
		[DllImport(Libusb)]
		public static extern Error libusb_set_interface_alt_setting(IntPtr deviceHandle, int @interface, int altSetting);
		
		[DllImport(Libusb)]
		public static extern Error libusb_clear_halt(IntPtr deviceHandle, byte endpoint);
		
		[DllImport(Libusb)]
		public static extern Error libusb_reset_device(IntPtr deviceHandle);
		
		[DllImport(Libusb)]
		public static extern Error libusb_kernel_driver_active(IntPtr deviceHandle, int @interface);
		
		[DllImport(Libusb)]
		public static extern Error libusb_detach_kernel_driver(IntPtr deviceHandle, int @interface);
		
		[DllImport(Libusb)]
		public static extern Error libusb_attach_kernel_driver(IntPtr deviceHandle, int @interface);
		
		[DllImport(Libusb)]
		public static extern IntPtr libusb_get_version();
				
		[DllImport(Libusb)]
		public static extern Error libusb_get_device_descriptor(IntPtr device, out DeviceDescriptor descriptor);
		
		[DllImport(Libusb)]
		public static extern Error libusb_get_active_config_descriptor(IntPtr device, out IntPtr configuration);

		[DllImport(Libusb)]
		public static extern Error libusb_get_config_descriptor_by_value(IntPtr device, byte index, out IntPtr configuration);
		
		[DllImport(Libusb)]
		public static extern void libusb_free_config_descriptor(IntPtr configuration);

		static Error libusb_get_descriptor_core(IntPtr deviceHandle, DescriptorType type, byte index, byte[] data, ushort wLength, ushort wIndex)
		{
			return libusb_control_transfer(deviceHandle,
			                               (byte)EndpointDirection.In, (byte)Request.GetDescriptor,
			                               (ushort)((byte)DescriptorType.String << 8 | index),
			                               wIndex, data, wLength, 1000);
		}

		public static Error libusb_get_descriptor(IntPtr deviceHandle, DescriptorType type, byte index, byte[] data, ushort wLength)
		{
			return libusb_get_descriptor_core(deviceHandle,
			                                  type, index, data, wLength, 0);
		}
		
		public static Error libusb_get_string_descriptor(IntPtr deviceHandle, DescriptorType type, byte index, ushort languageID, byte[] data, ushort wLength)
		{
			return libusb_get_descriptor_core(deviceHandle,
			                                  DescriptorType.String, index,
			                                  data, wLength, languageID);	
		}
		
		[DllImport(Libusb)]
		public static extern Error libusb_control_transfer(IntPtr deviceHandle,
		                                            	   byte bmRequestType, byte bRequest,
		                                            	   ushort wValue, ushort wIndex,
		                                            	   byte[] data, ushort wLength,
		                                            	   uint timeout);
		
		[DllImport(Libusb)]
		public static extern Error libusb_bulk_transfer(IntPtr deviceHandle,
		                                         	    byte endpoint,
		                                         	    byte[] data, int length,
		                                         	    out int transferred,
		                                         	    uint timeout);
		
		[DllImport(Libusb)]
		public static extern Error libusb_interrupt_transfer(IntPtr deviceHandle,
		                                         	  	     byte endpoint,
		                                         	  	     byte[] data, int length,
		                                         	  	     out int transferred,
		                                         	  	     uint timeout);
	}
}

