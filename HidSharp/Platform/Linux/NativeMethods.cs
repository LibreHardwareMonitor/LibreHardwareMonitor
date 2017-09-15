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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HidSharp.Platform.Linux
{
    static class NativeMethods
    {
		const string libc = "libc";
		const string libudev = "libudev.so.0";

		public enum error
		{
			OK = 0,
			EPERM = 1,
			EINTR = 4,
			EIO = 5,
			ENXIO = 6,
			EBADF = 9,
			EAGAIN = 11,
			EACCES = 13,
			EBUSY = 16,
			ENODEV = 19,
			EINVAL = 22
		}
			
		[Flags]
		public enum oflag
		{
			RDONLY = 0x000,
			WRONLY = 0x001,
			RDWR = 0x002,
			CREAT = 0x040,
			EXCL = 0x080,
			TRUNC = 0x200,
			APPEND = 0x400,
			NONBLOCK = 0x800
		}
	
		[Flags]
		public enum pollev : short
		{
			IN = 0x01,
			PRI = 0x02,
			OUT = 0x04,
			ERR = 0x08,
			HUP = 0x10,
			NVAL = 0x20
		}

		public struct pollfd
		{
			public int fd;
			public pollev events;
			public pollev revents;
		}

		public static int retry(Func<int> sysfunc)
		{
			while (true)
			{
                int ret = sysfunc(); var error = (error)Marshal.GetLastWin32Error();
                if (ret >= 0 || error != error.EINTR) { return ret; }
			}
		}

		public static IntPtr retry(Func<IntPtr> sysfunc)
		{
			while (true)
			{
                IntPtr ret = sysfunc(); var error = (error)Marshal.GetLastWin32Error();
                if ((long)ret >= 0 || error != error.EINTR) { return ret; }
			}
		}

		public static bool uname(out string sysname, out Version release, out string machine)
		{
			string releaseStr; release = null;
			if (!uname(out sysname, out releaseStr, out machine)) { return false; }
            releaseStr = new string(releaseStr.Trim().TakeWhile(ch => (ch >= '0' && ch <= '9') || ch == '.').ToArray());
			release = new Version(releaseStr);
			return true;
		}
		
        public static bool uname(out string sysname, out string release, out string machine)
        {
            sysname = null; release = null; machine = null;

            string syscallPath = "Mono.Unix.Native.Syscall, Mono.Posix, PublicKeyToken=0738eb9f132ed756";
            var syscall = Type.GetType(syscallPath);
            if (syscall == null) { return false; }

            var unameArgs = new object[1];
            int unameRet = (int)syscall.InvokeMember("uname",
                                                     BindingFlags.InvokeMethod | BindingFlags.Static, null, null, unameArgs,
                                                     CultureInfo.InvariantCulture);
            if (unameRet < 0) { return false; }

            var uname = unameArgs[0];
			Func<string, string> getMember = s => (string)uname.GetType().InvokeMember(s,
                                                                                       BindingFlags.GetField, null, uname, new object[0],
                                                                                       CultureInfo.InvariantCulture);
            sysname = getMember("sysname"); release = getMember("release"); machine = getMember("machine");
            return true;
        }
		
		[DllImport(libc, SetLastError = true)]
		public static extern int open(
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string filename,
			 oflag oflag);
		
		[DllImport(libc, SetLastError = true)]
		public static extern int close(int filedes);

		[DllImport(libc, SetLastError = true)]
		public static extern IntPtr read(int filedes, IntPtr buffer, IntPtr size);
		
		[DllImport(libc, SetLastError = true)]
		public static extern IntPtr write(int filedes, IntPtr buffer, IntPtr size);

		[DllImport(libc, SetLastError = true)]
		public static extern int poll(pollfd[] fds, IntPtr nfds, int timeout);
		
        [DllImport(libudev)]
        public static extern IntPtr udev_new();

        [DllImport(libudev)]
        public static extern IntPtr udev_ref(IntPtr udev);

        [DllImport(libudev)]
        public static extern void udev_unref(IntPtr udev);

        [DllImport(libudev)]
        public static extern IntPtr udev_enumerate_new(IntPtr udev);

        [DllImport(libudev)]
        public static extern IntPtr udev_enumerate_ref(IntPtr enumerate);

        [DllImport(libudev)]
        public static extern void udev_enumerate_unref(IntPtr enumerate);

        [DllImport(libudev)]
        public static extern int udev_enumerate_add_match_subsystem(IntPtr enumerate,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string subsystem);

        [DllImport(libudev)]
        public static extern int udev_enumerate_scan_devices(IntPtr enumerate);

        [DllImport(libudev)]
        public static extern IntPtr udev_enumerate_get_list_entry(IntPtr enumerate);

        [DllImport(libudev)]
        public static extern IntPtr udev_list_entry_get_next(IntPtr entry);

        [DllImport(libudev)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string udev_list_entry_get_name(IntPtr entry);

        [DllImport(libudev)]
        public static extern IntPtr udev_device_new_from_syspath(IntPtr udev,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string syspath);

        [DllImport(libudev)]
        public static extern IntPtr udev_device_ref(IntPtr device);

        [DllImport(libudev)]
        public static extern void udev_device_unref(IntPtr device);

        [DllImport(libudev)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string udev_device_get_devnode(IntPtr device);

        [DllImport(libudev)]
        public static extern IntPtr udev_device_get_parent_with_subsystem_devtype(IntPtr device,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string subsystem,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string devtype);

        [DllImport(libudev)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string udev_device_get_sysattr_value(IntPtr device,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string sysattr);

        public static bool TryParseHex(string hex, out int result)
        {
            return int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseVersion(string version, out int major, out int minor)
        {
            major = 0; minor = 0; if (version == null) { return false; }
            string[] parts = version.Split(new[] { '.' }, 2); if (parts.Length != 2) { return false; }
            return int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor);
        }

        #region ioctl
        // TODO: Linux changes these up depending on platform. Eventually we'll handle it.
        //       For now, x86 and ARM are safe with this definition.
        public const int IOC_NONE = 0;
        public const int IOC_WRITE = 1;
        public const int IOC_READ = 2;
        public const int IOC_NRBITS = 8;
        public const int IOC_TYPEBITS = 8;
        public const int IOC_SIZEBITS = 14;
        public const int IOC_DIRBITS = 2;
        public const int IOC_NRSHIFT = 0;
        public const int IOC_TYPESHIFT = IOC_NRSHIFT + IOC_NRBITS;
        public const int IOC_SIZESHIFT = IOC_TYPESHIFT + IOC_TYPEBITS;
        public const int IOC_DIRSHIFT = IOC_SIZESHIFT + IOC_SIZEBITS;

        public static int IOC(int dir, int type, int nr, int size)
        {
            return dir << IOC_DIRSHIFT | type << IOC_TYPESHIFT | nr << IOC_NRSHIFT | size << IOC_SIZESHIFT;
        }

        #region hidraw
        public const int HID_MAX_DESCRIPTOR_SIZE = 4096;
        public static readonly int HIDIOCGRDESCSIZE = IOC(IOC_READ, (byte)'H', 1, 4);
        public static readonly int HIDIOCGRDESC = IOC(IOC_READ, (byte)'H', 2, Marshal.SizeOf(typeof(hidraw_report_descriptor)));

        public struct hidraw_report_descriptor
        {
            public uint size;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = HID_MAX_DESCRIPTOR_SIZE)]
            public byte[] value;
        }

        [DllImport(libc, SetLastError = true)]
        public static extern int ioctl(int filedes, int command, out uint value);

        [DllImport(libc, SetLastError = true)]
        public static extern int ioctl(int filedes, int command, ref hidraw_report_descriptor value);
        #endregion
        #endregion
    }
}
