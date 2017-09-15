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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HidSharp.Platform.MacOS
{
    static class NativeMethods
    {
        const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        const string CoreServices = "/System/Library/Frameworks/CoreServices.framework/CoreServices";
        const string IOKit = "/System/Library/Frameworks/IOKit.framework/IOKit";

        public static readonly IntPtr kCFRunLoopDefaultMode = CFStringCreateWithCharacters("kCFRunLoopDefaultMode");
        public static readonly IntPtr kIOHIDVendorIDKey = CFStringCreateWithCharacters("VendorID");
        public static readonly IntPtr kIOHIDProductIDKey = CFStringCreateWithCharacters("ProductID");
        public static readonly IntPtr kIOHIDVersionNumberKey = CFStringCreateWithCharacters("VersionNumber");
        public static readonly IntPtr kIOHIDManufacturerKey = CFStringCreateWithCharacters("Manufacturer");
        public static readonly IntPtr kIOHIDProductKey = CFStringCreateWithCharacters("Product");
        public static readonly IntPtr kIOHIDSerialNumberKey = CFStringCreateWithCharacters("SerialNumber");
        public static readonly IntPtr kIOHIDLocationIDKey = CFStringCreateWithCharacters("LocationID");
        public static readonly IntPtr kIOHIDMaxInputReportSizeKey = CFStringCreateWithCharacters("MaxInputReportSize");
        public static readonly IntPtr kIOHIDMaxOutputReportSizeKey = CFStringCreateWithCharacters("MaxOutputReportSize");
        public static readonly IntPtr kIOHIDMaxFeatureReportSizeKey = CFStringCreateWithCharacters("MaxFeatureReportSize");

        public delegate void IOHIDCallback(IntPtr context, IOReturn result, IntPtr sender);
        public delegate void IOHIDDeviceCallback(IntPtr context, IOReturn result, IntPtr sender, IntPtr device);
        public delegate void IOHIDReportCallback(IntPtr context, IOReturn result, IntPtr sender,
                                                 IOHIDReportType type, uint reportID, IntPtr report, IntPtr reportLength);

        public enum OSErr : short
        {
            noErr = 0,
            gestaltUnknownErr = -5550,
            gestaltUndefSelectorErr = -5551,
            gestaltDupSelectorErr = -5552,
            gestaltLocationErr = -5553
        }

        public enum OSType : uint
        {
            gestaltSystemVersion = (byte)'s' << 24 | (byte)'y' << 16 | (byte)'s' << 8 | (byte)'v' << 0,
            gestaltSystemVersionMajor = (byte)'s' << 24 | (byte)'y' << 16 | (byte)'s' << 8 | (byte)'1' << 0,
            gestaltSystemVersionMinor = (byte)'s' << 24 | (byte)'y' << 16 | (byte)'s' << 8 | (byte)'2' << 0,
            gestaltSystemVersionBugFix = (byte)'s' << 24 | (byte)'y' << 16 | (byte)'s' << 8 | (byte)'3' << 0
        }

        public enum IOOptionBits
        {
            None = 0
        }

        public enum IOHIDReportType
        {
            Input = 0,
            Output,
            Feature
        }

        public enum IOReturn
        {
            Success = 0
        }

        public struct io_string_t
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
            public byte[] Value;

            public override bool Equals(object obj)
            {
                return obj is io_string_t && this == (io_string_t)obj;
            }

            public override int GetHashCode()
            {
                return Value.Length >= 1 ? Value[0] : -1;
            }

            public override string ToString()
            {
                return Encoding.UTF8.GetString(Value.TakeWhile(ch => ch != 0).ToArray());
            }

            public static bool operator ==(io_string_t io1, io_string_t io2)
            {
                return io1.Value.SequenceEqual(io2.Value);
            }

            public static bool operator !=(io_string_t io1, io_string_t io2)
            {
                return !(io1 == io2);
            }
        }

        public enum CFNumberType
        {
            Int = 9
        }

        public struct CFRange
        {
            public IntPtr Start, Length;
        }

        public struct IOObject : IDisposable
        {
            public int Handle { get; set; }
            public bool IsSet { get { return Handle != 0; } }

            void IDisposable.Dispose()
            {
                if (IsSet) { IOObjectRelease(Handle); Handle = 0; }
            }

            public static implicit operator int(IOObject self)
            {
                return self.Handle;
            }
        }

        public struct CFType : IDisposable
        {
            public IntPtr Handle { get; set; }
            public bool IsSet { get { return Handle != IntPtr.Zero; } }

            void IDisposable.Dispose()
            {
                if (IsSet) { CFRelease(Handle); Handle = IntPtr.Zero; }
            }

            public static implicit operator IntPtr(CFType self)
            {
                return self.Handle;
            }
        }

        public static CFType ToCFType(this IntPtr handle)
        {
            return new CFType() { Handle = handle };
        }

        public static IOObject ToIOObject(this int handle)
        {
            return new IOObject() { Handle = handle };
        }

        [DllImport(CoreServices)]
        public static extern OSErr Gestalt(OSType selector, out IntPtr response);
		
		[DllImport(CoreFoundation)]
		public static extern uint CFGetTypeID(IntPtr type);
		
		[DllImport(CoreFoundation)]
		public static extern uint CFNumberGetTypeID();
		
		[DllImport(CoreFoundation)]
		public static extern uint CFStringGetTypeID();
		
        [DllImport(CoreFoundation)]
        public static extern IntPtr CFDictionaryCreateMutable(IntPtr allocator, IntPtr capacity,
                                                       		  IntPtr keyCallbacks, IntPtr valueCallbacks);

        public static IntPtr CFDictionaryCreateMutable()
        {
            return CFDictionaryCreateMutable(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }
		
        [DllImport(CoreFoundation)]
        public static extern void CFDictionarySetValue(IntPtr dict, IntPtr key, IntPtr value);
		
        [DllImport(CoreFoundation)]
        public static extern IntPtr CFNumberCreate(IntPtr allocator, CFNumberType type, ref int value);

        public static IntPtr CFNumberCreate(int value)
        {
            return CFNumberCreate(IntPtr.Zero, CFNumberType.Int, ref value);
        }

        [DllImport(CoreFoundation)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool CFNumberGetValue(IntPtr number, CFNumberType type, out int value);

        public static int? CFNumberGetValue(IntPtr number)
        {
            int value;
            return number != IntPtr.Zero && CFGetTypeID(number) == CFNumberGetTypeID() &&
				CFNumberGetValue(number, CFNumberType.Int, out value) ? (int?)value : null;
        }

        [DllImport(CoreFoundation, CharSet=CharSet.Unicode)]
        public static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator, char[] buffer, IntPtr length);

        public static IntPtr CFStringCreateWithCharacters(string str)
        {
            return CFStringCreateWithCharacters(IntPtr.Zero, str.ToCharArray(), (IntPtr)str.Length);
        }

        [DllImport(CoreFoundation, CharSet=CharSet.Unicode)]
        public static extern void CFStringGetCharacters(IntPtr str, CFRange range, char[] buffer);

        public static string CFStringGetCharacters(IntPtr str)
        {
            if (str == IntPtr.Zero || CFGetTypeID(str) != CFStringGetTypeID()) { return null; }
            char[] buffer = new char[(int)CFStringGetLength(str)];
            CFStringGetCharacters(str, new CFRange() { Start = (IntPtr)0, Length = (IntPtr)buffer.Length }, buffer);
            return new string(buffer);
        }

        [DllImport(CoreFoundation)]
        public static extern IntPtr CFStringGetLength(IntPtr str);

        [DllImport(CoreFoundation)]
        public static extern void CFRunLoopRun();

        [DllImport(CoreFoundation)]
        public static extern IntPtr CFRunLoopGetCurrent();

        [DllImport(CoreFoundation)]
        public static extern void CFRunLoopStop(IntPtr runLoop);

        [DllImport(CoreFoundation)]
        public static extern void CFRelease(IntPtr obj);

        [DllImport(CoreFoundation)]
        public static extern void CFRetain(IntPtr obj);

        [DllImport(CoreFoundation)]
        public static extern IntPtr CFSetGetCount(IntPtr set);

        [DllImport(CoreFoundation)]
        public static extern void CFSetGetValues(IntPtr set, IntPtr[] values);

        [DllImport(IOKit)]
        public static extern IntPtr IOHIDDeviceCreate(IntPtr allocator, int service);

        [DllImport(IOKit)]
        public static extern IOReturn IOHIDDeviceOpen(IntPtr device, IOOptionBits options = IOOptionBits.None);

        [DllImport(IOKit)]
        public static extern void IOHIDDeviceRegisterInputReportCallback(IntPtr device, IntPtr report, IntPtr reportLength,
                                                                         IOHIDReportCallback callback, IntPtr context);

        [DllImport(IOKit)]
        public static extern void IOHIDDeviceRegisterRemovalCallback(IntPtr device, IOHIDCallback callback, IntPtr context);

        [DllImport(IOKit)]
        public static extern IOReturn IOHIDDeviceGetReport(IntPtr device, IOHIDReportType type, IntPtr reportID, IntPtr report, ref IntPtr reportLength);

        [DllImport(IOKit)]
        public static extern IOReturn IOHIDDeviceSetReport(IntPtr device, IOHIDReportType type, IntPtr reportID, IntPtr report, IntPtr reportLength);

        [DllImport(IOKit)]
        public static extern void IOHIDDeviceScheduleWithRunLoop(IntPtr device, IntPtr runLoop, IntPtr runLoopMode);

        [DllImport(IOKit)]
        public static extern void IOHIDDeviceUnscheduleFromRunLoop(IntPtr device, IntPtr runLoop, IntPtr runLoopMode);

        [DllImport(IOKit)]
        public static extern IOReturn IOHIDDeviceClose(IntPtr device, IOOptionBits options = IOOptionBits.None);

        [DllImport(IOKit)]
        public static extern int IOIteratorNext(int iterator);

        [DllImport(IOKit)]
        public static extern IOReturn IOObjectRetain(int @object);

        [DllImport(IOKit)]
        public static extern IOReturn IOObjectRelease(int @object);

        [DllImport(IOKit)]
        public static extern IntPtr IORegistryEntryCreateCFProperty(int entry, IntPtr strKey, IntPtr allocator, IOOptionBits options = IOOptionBits.None);

        public static int? IORegistryEntryGetCFProperty_Int(int entry, IntPtr strKey)
        {
            using (var property = IORegistryEntryCreateCFProperty(entry, strKey, IntPtr.Zero).ToCFType())
            {
                return CFNumberGetValue(property);
            }
        }

        public static string IORegistryEntryGetCFProperty_String(int entry, IntPtr strKey)
        {
            using (var property = IORegistryEntryCreateCFProperty(entry, strKey, IntPtr.Zero).ToCFType())
            {
                return CFStringGetCharacters(property);
            }
        }

        [DllImport(IOKit)]
        public static extern int IORegistryEntryFromPath(int masterPort, ref io_string_t path);

        [DllImport(IOKit)] // plane = IOService
        public static extern IOReturn IORegistryEntryGetPath(int entry, [MarshalAs(UnmanagedType.LPStr)] string plane, out io_string_t path);

        [DllImport(IOKit)]
        public static extern IOReturn IOServiceGetMatchingServices(int masterPort, IntPtr matching, out int iterator);

        [DllImport(IOKit)] // name = IOHIDDevice
        public static extern IntPtr IOServiceMatching([MarshalAs(UnmanagedType.LPStr)] string name);
    }
}
