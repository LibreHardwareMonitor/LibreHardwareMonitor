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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.Linux
{
    class LinuxHidStream : HidStream
    {
		Queue<byte[]> _inputQueue;
		Queue<CommonOutputReport> _outputQueue;
		
		LinuxHidDevice _device;
		int _handle;
		Thread _readThread, _writeThread;
		volatile bool _shutdown;
		
        internal LinuxHidStream()
        {
			_inputQueue = new Queue<byte[]>();
			_outputQueue = new Queue<CommonOutputReport>();
			_handle = -1;
			_readThread = new Thread(ReadThread);
			_readThread.IsBackground = true;
			_writeThread = new Thread(WriteThread);
			_writeThread.IsBackground = true;
        }
		
		static int DeviceHandleFromPath(string path)
		{
			IntPtr udev = NativeMethods.udev_new();
			if (IntPtr.Zero != udev)
			{
				try
				{
					IntPtr device = NativeMethods.udev_device_new_from_syspath(udev, path);
					if (IntPtr.Zero != device)
					{
						try
						{
							string devnode = NativeMethods.udev_device_get_devnode(device);
							if (devnode != null)
							{
								int handle = NativeMethods.retry(() => NativeMethods.open
								                            (devnode, NativeMethods.oflag.RDWR | NativeMethods.oflag.NONBLOCK));
								if (handle < 0)
								{
									var error = (NativeMethods.error)Marshal.GetLastWin32Error();
									if (error == NativeMethods.error.EACCES)
									{
										throw new UnauthorizedAccessException("Not permitted to open HID class device at " + devnode + ".");
									}
									else
									{
										throw new IOException("Unable to open HID class device (" + error.ToString() + ").");
									}
								}
								return handle;
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
			
			throw new FileNotFoundException("HID class device not found.");
		}
		
        internal void Init(string path, LinuxHidDevice device)
        {
			int handle;
			handle = DeviceHandleFromPath(path);
			
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
            try { lock (_inputQueue) { Monitor.PulseAll(_inputQueue); } } catch { }
			try { lock (_outputQueue) { Monitor.PulseAll(_outputQueue); } } catch { }

            try { _readThread.Join(); } catch { }
            try { _writeThread.Join(); } catch { }

			HandleRelease();
		}
		
		internal override void HandleFree()
		{
			NativeMethods.retry(() => NativeMethods.close(_handle)); _handle = -1;
		}
		
		unsafe void ReadThread()
		{
			if (!HandleAcquire()) { return; }
			
			try
			{
				lock (_inputQueue)
				{
					while (true)
					{
						var fds = new NativeMethods.pollfd[1];
						fds[0].fd = _handle;
						fds[0].events = NativeMethods.pollev.IN;
						
						while (!_shutdown)
						{
						tryReadAgain:
							int ret;
							Monitor.Exit(_inputQueue);
							try { ret = NativeMethods.retry(() => NativeMethods.poll(fds, (IntPtr)1, 250)); }
							finally { Monitor.Enter(_inputQueue); }
							if (ret != 1) { continue; }
							
							if (0 != (fds[0].revents & (NativeMethods.pollev.ERR | NativeMethods.pollev.HUP))) { break; }
							if (0 != (fds[0].revents & NativeMethods.pollev.IN))
							{
                                // Linux doesn't provide a Report ID if the device doesn't use one.
                                int inputLength = _device.MaxInputReportLength;
                                if (inputLength > 0 && !_device.ReportsUseID) { inputLength--; }

                                byte[] inputReport = new byte[inputLength];
								fixed (byte* inputBytes = inputReport)
								{
                                    var inputBytesPtr = (IntPtr)inputBytes;
									IntPtr length = NativeMethods.retry(() => NativeMethods.read
									                               (_handle, inputBytesPtr, (IntPtr)inputReport.Length));
									if ((long)length < 0)
									{
                                        var error = (NativeMethods.error)Marshal.GetLastWin32Error();
										if (error != NativeMethods.error.EAGAIN) { break; }
										goto tryReadAgain;
									}

									Array.Resize(ref inputReport, (int)length); // No Report ID? First byte becomes Report ID 0.
                                    if (!_device.ReportsUseID) { inputReport = new byte[1].Concat(inputReport).ToArray(); }
									_inputQueue.Enqueue(inputReport); Monitor.PulseAll(_inputQueue);
								}
							}
						}
						while (!_shutdown && _inputQueue.Count == 0) { Monitor.Wait(_inputQueue); }
						if (_shutdown) { break; }
						
						_inputQueue.Dequeue();
					}
				}
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

        public override void GetFeature(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(); // TODO
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

						CommonOutputReport outputReport = _outputQueue.Peek();

                        // Linux doesn't expect a Report ID if the device doesn't use one.
                        byte[] outputBytesRaw = outputReport.Bytes;
                        if (!_device.ReportsUseID && outputBytesRaw.Length > 0) { outputBytesRaw = outputBytesRaw.Skip(1).ToArray(); }

						try
						{
							fixed (byte* outputBytes = outputBytesRaw)
							{
								// hidraw is apparently blocking for output, even when O_NONBLOCK is used.
								// See for yourself at drivers/hid/hidraw.c...
                                IntPtr length;
                                Monitor.Exit(_outputQueue);
                                try
                                {
                                    var outputBytesPtr = (IntPtr)outputBytes;
                                    length = NativeMethods.retry(() => NativeMethods.write
                                                            (_handle, outputBytesPtr, (IntPtr)outputBytesRaw.Length));
                                    if ((long)length == outputBytesRaw.Length) { outputReport.DoneOK = true; }
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
            throw new NotSupportedException(); // TODO
        }

        public override HidDevice Device
        {
            get { return _device; }
        }
    }
}
