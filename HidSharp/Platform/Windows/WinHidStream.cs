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
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.Windows
{
    class WinHidStream : HidStream
    {
        object _readSync = new object(), _writeSync = new object();
        byte[] _readBuffer, _writeBuffer;
        IntPtr _handle, _closeEventHandle;
        WinHidDevice _device;

        internal WinHidStream()
        {
            _closeEventHandle = NativeMethods.CreateManualResetEventOrThrow();
        }

        ~WinHidStream()
        {
			Close();
            NativeMethods.CloseHandle(_closeEventHandle);
        }

        internal void Init(string path, WinHidDevice device)
        {
            IntPtr handle = NativeMethods.CreateFileFromDevice(path, NativeMethods.EFileAccess.Read | NativeMethods.EFileAccess.Write, NativeMethods.EFileShare.All);
            if (handle == (IntPtr)(-1)) { throw new IOException("Unable to open HID class device."); }

            _device = device;
			_handle = handle;
			HandleInitAndOpen();
        }
		
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
			if (!HandleClose()) { return; }
			
			NativeMethods.SetEvent(_closeEventHandle);
			HandleRelease();
		}
		
		internal override void HandleFree()
		{
			NativeMethods.CloseHandle(ref _handle);
			NativeMethods.CloseHandle(ref _closeEventHandle);
		}

        public unsafe override void GetFeature(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            fixed (byte* ptr = buffer)
	            {
	                if (!NativeMethods.HidD_GetFeature(_handle, ptr + offset, count))
	                    { throw new IOException("GetFeature failed.", new Win32Exception()); }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        // Buffer needs to be big enough for the largest report, plus a byte
        // for the Report ID.
        public unsafe override int Read(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count); uint bytesTransferred;
            IntPtr @event = NativeMethods.CreateManualResetEventOrThrow();
			
			HandleAcquireIfOpenOrFail();
            try
            {
				lock (_readSync)
				{
	                int maxIn = _device.MaxInputReportLength;
	                Array.Resize(ref _readBuffer, maxIn); if (count > maxIn) { count = maxIn; }
	
	                fixed (byte* ptr = _readBuffer)
	                {
                        var overlapped = stackalloc NativeOverlapped[1];
                        overlapped[0].EventHandle = @event;

                        NativeMethods.OverlappedOperation(_handle, @event, ReadTimeout, _closeEventHandle,
                            NativeMethods.ReadFile(_handle, ptr, maxIn, IntPtr.Zero, overlapped),
                            overlapped, out bytesTransferred);

	                    if (count > (int)bytesTransferred) { count = (int)bytesTransferred; }
	                    Array.Copy(_readBuffer, 0, buffer, offset, count);
	                    return count;
	                }
				}
            }
            finally
            {
				HandleRelease();
                NativeMethods.CloseHandle(@event);
            }
        }

        public unsafe override void SetFeature(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            fixed (byte* ptr = buffer)
	            {
	                if (!NativeMethods.HidD_SetFeature(_handle, ptr + offset, count))
	                    { throw new IOException("SetFeature failed.", new Win32Exception()); }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        public unsafe override void Write(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count); uint bytesTransferred;
            IntPtr @event = NativeMethods.CreateManualResetEventOrThrow();

			HandleAcquireIfOpenOrFail();
            try
            {
				lock (_writeSync)
				{
	                int maxOut = _device.MaxOutputReportLength;
	                Array.Resize(ref _writeBuffer, maxOut); if (count > maxOut) { count = maxOut; }
	                Array.Copy(buffer, offset, _writeBuffer, 0, count); count = maxOut;
	
	                fixed (byte* ptr = _writeBuffer)
	                {
	                    int offset0 = 0;
	                    while (count > 0)
	                    {
                            var overlapped = stackalloc NativeOverlapped[1];
                            overlapped[0].EventHandle = @event;

                            NativeMethods.OverlappedOperation(_handle, @event, WriteTimeout, _closeEventHandle,
	                            NativeMethods.WriteFile(_handle, ptr + offset0, count, IntPtr.Zero, overlapped),
	                            overlapped, out bytesTransferred);
	                        count -= (int)bytesTransferred; offset0 += (int)bytesTransferred;
	                    }
	                }
				}
            }
            finally
            {
				HandleRelease();
                NativeMethods.CloseHandle(@event);
            }
        }

        public override HidDevice Device
        {
            get { return _device; }
        }
    }
}
