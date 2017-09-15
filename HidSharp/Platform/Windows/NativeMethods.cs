#region License
/* Copyright 2010-2012 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

#pragma warning disable 169, 649

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.Windows
{
    unsafe static class NativeMethods
    {
        // For constants, see PInvoke.Net,
        //  http://doxygen.reactos.org/de/d2a/hidclass_8h_source.html
        //  http://www.rpi.edu/dept/cis/software/g77-mingw32/include/winioctl.h
        // and Google.
        public const int ERROR_HANDLE_EOF = 38;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int ERROR_OPERATION_ABORTED = 995;
        public const int ERROR_IO_PENDING = 997;
        public const uint FILE_ANY_ACCESS = 0;
        public const uint FILE_DEVICE_KEYBOARD = 11;
        public const uint METHOD_NEITHER = 3;
        public const uint WAIT_OBJECT_0 = 0;
        public const uint WAIT_OBJECT_1 = 1;
        public const uint WAIT_TIMEOUT = 258;

        public static uint CTL_CODE(uint devType, uint func, uint method, uint access)
        {
            return devType << 16 | access << 14 | func << 2 | method;
        }

        public static uint HID_CTL_CODE(uint id)
        {
            return CTL_CODE(FILE_DEVICE_KEYBOARD, id, METHOD_NEITHER, FILE_ANY_ACCESS);
        }

        public static readonly uint IOCTL_HID_GET_REPORT_DESCRIPTOR = HID_CTL_CODE(1);

        public static int HIDP_ERROR_CODES(int sev, ushort code)
        {
            return sev << 28 | 0x11 << 16 | code;
        }

        public static readonly int HIDP_STATUS_SUCCESS = HIDP_ERROR_CODES(0, 0);
        public static readonly int HIDP_STATUS_INVALID_PREPARSED_DATA = HIDP_ERROR_CODES(12, 1);

        [Flags]
        public enum EFileAccess : uint
        {
            None = 0,
            Read = 0x80000000,
            Write = 0x40000000,
            Execute = 0x20000000,
            All = 0x10000000
        }

        [Flags]
        public enum EFileShare : uint
        {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004,
            All = Read | Write | Delete
        }

        public enum ECreationDisposition : uint
        {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5
        }

        [Flags]
        public enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Writethrough = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        [Flags]
        public enum DIGCF
        {
            None = 0,
            Default = 1,
            Present = 2,
            AllClasses = 4,
            Profile = 8,
            DeviceInterface = 16
        }

        [Flags]
        public enum SPINT
        {
            None = 0,
            Active = 1,
            Default = 2,
            Removed = 4
        }

        public struct HDEVINFO
        {
            IntPtr Value;

            public void Invalidate()
            {
                Value = (IntPtr)(-1);
            }

            public bool IsValid
            {
                get { return Value != (IntPtr)(-1); }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct OSVERSIONINFO
        {
            public int OSVersionInfoSize;
            public uint MajorVersion, MinorVersion, BuildNumber, PlatformID;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string CSDVersion;
        }

        public struct SP_DEVINFO_DATA
        {
            public int Size;
            public Guid ClassGuid;
            public uint DevInst;
            IntPtr Reserved;
        }

        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int Size;
            public Guid InterfaceClassGuid;
            public SPINT Flags;
            IntPtr Reserved;
        }

        [Obfuscation(Feature = "preserve-name-binding")]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] public string DevicePath;
        }

        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID, ProductID, VersionNumber;
        }

        public unsafe struct HIDP_CAPS
        {
            public ushort Usage, UsagePage;
            public ushort InputReportByteLength, OutputReportByteLength, FeatureReportByteLength;
            fixed ushort Reserved[17];
            public ushort NumberLinkCollectionNodes,
                NumberInputButtonCaps, NumberInputValueCaps, NumberInputDataIndices,
                NumberOutputButtonCaps, NumberOutputValueCaps, NumberOutputDataIndices,
                NumberFeatureButtonCaps, NumberFeatureValueCaps, NumberFeatureDataIndices;
        }

        public static IntPtr CreateManualResetEventOrThrow()
        {
            IntPtr @event = NativeMethods.CreateEvent(IntPtr.Zero, true, false, IntPtr.Zero);
            if (@event == IntPtr.Zero) { throw new IOException("Event creation failed."); }
            return @event;
        }

        public unsafe static void OverlappedOperation(IntPtr ioHandle,
            IntPtr eventHandle, int eventTimeout, IntPtr closeEventHandle,
            bool overlapResult,
            NativeOverlapped* overlapped, out uint bytesTransferred)
        {
            bool closed = false;

            if (!overlapResult)
            {
                int win32Error = Marshal.GetLastWin32Error();
                if (win32Error != NativeMethods.ERROR_IO_PENDING)
                {
                    throw new IOException("Operation failed early.", new Win32Exception());
                }

                IntPtr* handles = stackalloc IntPtr[2];
                handles[0] = eventHandle; handles[1] = closeEventHandle;
                uint timeout = eventTimeout < 0 ? ~(uint)0 : (uint)eventTimeout;
                uint waitResult = NativeMethods.WaitForMultipleObjects(2, handles, false, timeout);
                switch (waitResult)
                {
                    case NativeMethods.WAIT_OBJECT_0: break;
                    case NativeMethods.WAIT_OBJECT_1: closed = true; goto default;
                    default: CancelIo(ioHandle); break;
                }
            }

            if (!NativeMethods.GetOverlappedResult(ioHandle, overlapped, out bytesTransferred, true))
            {
                int win32Error = Marshal.GetLastWin32Error();
                if (win32Error != NativeMethods.ERROR_HANDLE_EOF)
                {
                    if (closed)
                    {
                        throw new IOException("Connection closed.");
                    }

                    if (win32Error == NativeMethods.ERROR_OPERATION_ABORTED)
                    {
                        throw new TimeoutException("Operation timed out.");
                    }

                    throw new IOException("Operation failed after some time.", new Win32Exception());
                }

                bytesTransferred = 0;
            }
        }
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVersionEx(ref OSVERSIONINFO version);
         
        [DllImport("hid.dll")]
        public static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetAttributes(IntPtr handle, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetManufacturerString(IntPtr handle, char[] buffer, int bufferLengthInBytes);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetProductString(IntPtr handle, char[] buffer, int bufferLengthInBytes);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetSerialNumberString(IntPtr handle, char[] buffer, int bufferLengthInBytes);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool HidD_GetFeature(IntPtr handle, byte* buffer, int bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool HidD_SetFeature(IntPtr handle, byte* buffer, int bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool HidD_GetPreparsedData(IntPtr handle, out IntPtr preparsed);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.U1)]
        public unsafe static extern bool HidD_FreePreparsedData(IntPtr preparsed);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public unsafe static extern int HidP_GetCaps(IntPtr preparsed, out HIDP_CAPS caps);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern HDEVINFO SetupDiGetClassDevs
            ([MarshalAs(UnmanagedType.LPStruct)] Guid classGuid, string enumerator, IntPtr hwndParent, DIGCF flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiDestroyDeviceInfoList(HDEVINFO deviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiEnumDeviceInterfaces(HDEVINFO deviceInfoSet, IntPtr deviceInfoData,
            [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceClassGuid, int memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(HDEVINFO deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize, IntPtr requiredSize, IntPtr deviceInfoData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateEvent(IntPtr eventAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool manualReset,
            [MarshalAs(UnmanagedType.Bool)] bool initialState,
            IntPtr name);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFile(string filename, EFileAccess desiredAccess, EFileShare shareMode, IntPtr securityAttributes,
            ECreationDisposition creationDisposition, EFileAttributes attributes, IntPtr template);

        public static IntPtr CreateFileFromDevice(string filename, EFileAccess desiredAccess, EFileShare shareMode)
        {
            return CreateFile(filename, desiredAccess, shareMode, IntPtr.Zero,
                ECreationDisposition.OpenExisting,
                EFileAttributes.Device | EFileAttributes.Overlapped,
                IntPtr.Zero);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

		public static bool CloseHandle(ref IntPtr handle)
		{
			if (!CloseHandle(handle)) { return false; }
			handle = IntPtr.Zero; return true;
		}
		
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public unsafe static extern bool ReadFile(IntPtr handle, byte* buffer, int bytesToRead,
            IntPtr bytesRead, NativeOverlapped* overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public unsafe static extern bool WriteFile(IntPtr handle, byte* buffer, int bytesToWrite,
            IntPtr bytesWritten, NativeOverlapped* overlapped);

        public static string NTString(char[] buffer)
        {
            int index = Array.IndexOf(buffer, '\0');
            return new string(buffer, 0, index >= 0 ? index : buffer.Length);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CancelIo(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public unsafe static extern bool DeviceIoControl(IntPtr handle,
            uint ioControlCode, byte* inBuffer, uint inBufferSize, byte* outBuffer, uint outBufferSize,
            IntPtr bytesReturned, NativeOverlapped* overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetOverlappedResult(IntPtr handle,
            NativeOverlapped* overlapped, out uint bytesTransferred,
            [MarshalAs(UnmanagedType.Bool)] bool wait);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ResetEvent(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetEvent(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public unsafe static extern uint WaitForMultipleObjects(uint count, IntPtr* handles,
            [MarshalAs(UnmanagedType.Bool)] bool waitAll, uint milliseconds);
    }
}
