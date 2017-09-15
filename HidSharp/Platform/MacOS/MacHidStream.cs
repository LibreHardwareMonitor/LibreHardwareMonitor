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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.MacOS
{
    class MacHidStream : HidStream
    {
        Queue<byte[]> _inputQueue;
        Queue<CommonOutputReport> _outputQueue;

        MacHidDevice _device;
        IntPtr _handle;
        IntPtr _readRunLoop;
        Thread _readThread, _writeThread;
        volatile bool _shutdown;

        internal MacHidStream()
        {
            _inputQueue = new Queue<byte[]>();
            _outputQueue = new Queue<CommonOutputReport>();
            _readThread = new Thread(ReadThread);
			_readThread.IsBackground = true;
            _writeThread = new Thread(WriteThread);
			_writeThread.IsBackground = true;
        }
		
		internal void Init(NativeMethods.io_string_t path, MacHidDevice device)
		{
            IntPtr handle;
            using (var service = NativeMethods.IORegistryEntryFromPath(0, ref path).ToIOObject())
            {
                handle = NativeMethods.IOHIDDeviceCreate(IntPtr.Zero, service);
                if (handle == IntPtr.Zero) { throw new IOException("HID class device not found."); }

                if (NativeMethods.IOReturn.Success != NativeMethods.IOHIDDeviceOpen(handle)) { NativeMethods.CFRelease(handle); throw new IOException("Unable to open HID class device."); }
            }
            _device = device;
            _handle = handle;
			HandleInitAndOpen();

            _readThread.Start();
            _writeThread.Start();
		}
		
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
			if (!HandleClose()) { return; }
			
            _shutdown = true;
            try { lock (_outputQueue) { Monitor.PulseAll(_outputQueue); } } catch { }

            NativeMethods.CFRunLoopStop(_readRunLoop);
            try { _readThread.Join(); } catch { }
            try { _writeThread.Join(); } catch { }
			
			HandleRelease();
        }

		internal override void HandleFree()
		{
			NativeMethods.CFRelease(_handle); _handle = IntPtr.Zero;
		}
		
        static void ReadThreadEnqueue(Queue<byte[]> queue, byte[] report)
        {
            lock (queue)
            {
                if (queue.Count < 100) { queue.Enqueue(report); Monitor.PulseAll(queue); }
            }
        }

        void ReadThreadCallback(IntPtr context, NativeMethods.IOReturn result, IntPtr sender,
                                	   NativeMethods.IOHIDReportType type,
		                               uint reportID, IntPtr report, IntPtr reportLength)
        {
            byte[] reportBytes = new byte[(int)reportLength];
            Marshal.Copy(report, reportBytes, 0, reportBytes.Length);

            if (result == NativeMethods.IOReturn.Success && reportLength != IntPtr.Zero)
            {
                if (type == NativeMethods.IOHIDReportType.Input)
                {
                    ReadThreadEnqueue(_inputQueue, reportBytes);
                }
            }
        }

        unsafe void ReadThread()
        {			
			if (!HandleAcquire()) { return; }
			_readRunLoop = NativeMethods.CFRunLoopGetCurrent();
			
            try
            {
				var callback = new NativeMethods.IOHIDReportCallback(ReadThreadCallback);

                byte[] inputReport = new byte[_device.MaxInputReportLength];
                fixed (byte* inputReportBytes = inputReport)
                {
                    NativeMethods.IOHIDDeviceRegisterInputReportCallback(_handle,
                                                                  (IntPtr)inputReportBytes, (IntPtr)inputReport.Length,
                                                                  callback, IntPtr.Zero);
                    NativeMethods.IOHIDDeviceScheduleWithRunLoop(_handle, _readRunLoop, NativeMethods.kCFRunLoopDefaultMode);
                    NativeMethods.CFRunLoopRun();
                    NativeMethods.IOHIDDeviceUnscheduleFromRunLoop(_handle, _readRunLoop, NativeMethods.kCFRunLoopDefaultMode);
                }
				
				GC.KeepAlive(this);
				GC.KeepAlive(callback);
                GC.KeepAlive(_inputQueue);
            }
            finally
            {
                HandleRelease();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return CommonRead(buffer, offset, count, _inputQueue);
        }

        public unsafe override void GetFeature(byte[] buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            fixed (byte* bufferBytes = buffer)
	            {
	                IntPtr reportLength = (IntPtr)count;
	                if (NativeMethods.IOReturn.Success != NativeMethods.IOHIDDeviceGetReport(_handle, NativeMethods.IOHIDReportType.Feature,
	                                                                           (IntPtr)buffer[offset],
	                                                                           (IntPtr)(bufferBytes + offset),
	                                                                           ref reportLength))
	
	                {
	                    throw new IOException("GetFeature failed.");
	                }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        unsafe void WriteThread()
        {
			if (!HandleAcquire()) { return; }
			
			try
	        {	
				lock (_outputQueue)
				{								
	                while (true)
	                {
	                    while (!_shutdown && _outputQueue.Count == 0) { Monitor.Wait(_outputQueue); }
						if (_shutdown) { break; }
	
						NativeMethods.IOReturn ret;
	                    CommonOutputReport outputReport = _outputQueue.Peek();
	                    try
	                    {
	                        fixed (byte* outputReportBytes = outputReport.Bytes)
	                        {
	                            Monitor.Exit(_outputQueue);
	
	                            try
	                            {
	                                ret = NativeMethods.IOHIDDeviceSetReport(_handle,
									                                  outputReport.Feature ? NativeMethods.IOHIDReportType.Feature : NativeMethods.IOHIDReportType.Output,
	                                                                  (IntPtr)outputReport.Bytes[0],
	                                                                  (IntPtr)outputReportBytes,
	                                                                  (IntPtr)outputReport.Bytes.Length);
	                                if (ret == NativeMethods.IOReturn.Success) { outputReport.DoneOK = true; }
	                            }
	                            finally
	                            {
	                                Monitor.Enter(_outputQueue);
	                            }
	                        }
	                    }
	                    finally
	                    {
							_outputQueue.Dequeue();
	                        outputReport.Done = true;
	                        Monitor.PulseAll(_outputQueue);
	                    }
	                }
	            }
			}
            finally
            {
                HandleRelease();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CommonWrite(buffer, offset, count, _outputQueue, false, _device.MaxOutputReportLength);
        }

        public override void SetFeature(byte[] buffer, int offset, int count)
        {
            CommonWrite(buffer, offset, count, _outputQueue, true, _device.MaxFeatureReportLength);
        }

        public override HidDevice Device
        {
            get { return _device; }
        }
    }
}
