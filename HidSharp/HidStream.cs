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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

#pragma warning disable 420

namespace HidSharp
{
    /// <summary>
    /// Communicates with a USB HID class device.
    /// </summary>
    [ComVisible(true), Guid("0C263D05-0D58-4c6c-AEA7-EB9E0C5338A2")]
    public abstract class HidStream : Stream
    {
		int _opened, _closed;
		volatile int _refCount;
		
        internal class CommonOutputReport
        {
            public byte[] Bytes;
			public bool DoneOK, Feature;
            public volatile bool Done;
        }
		
        internal HidStream()
        {
            ReadTimeout = 3000;
            WriteTimeout = 3000;
        }
		
		internal static int GetTimeout(int startTime, int timeout)
		{
			return Math.Min(timeout, Math.Max(0, startTime + timeout - Environment.TickCount));
		}
		
        internal int CommonRead(byte[] buffer, int offset, int count, Queue<byte[]> queue)
        {
            Throw.If.OutOfRange(buffer, offset, count);
            if (count == 0) { return 0; }

            int readTimeout = ReadTimeout;
            int startTime = Environment.TickCount;
            int timeout;

			HandleAcquireIfOpenOrFail();
			try
			{
	            lock (queue)
	            {
	                while (true)
	                {
	                    if (queue.Count > 0)
	                    {
	                        byte[] packet = queue.Dequeue();
	                        count = Math.Min(count, packet.Length);
	                        Array.Copy(packet, 0, buffer, offset, count);
	                        return count;
	                    }
	
	                    timeout = GetTimeout(startTime, readTimeout);
	                    if (!Monitor.Wait(queue, timeout)) { throw new TimeoutException(); }
	                }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        internal void CommonWrite(byte[] buffer, int offset, int count,
		                          Queue<CommonOutputReport> queue,
		                          bool feature, int maxOutputReportLength)
        {
            Throw.If.OutOfRange(buffer, offset, count);
            count = Math.Min(count, maxOutputReportLength);
            if (count == 0) { return; }

            int writeTimeout = WriteTimeout;
            int startTime = Environment.TickCount;
            int timeout;
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            lock (queue)
	            {
	                while (true)
	                {
	                    if (queue.Count == 0)
	                    {
	                        byte[] packet = new byte[count];
	                        Array.Copy(buffer, offset, packet, 0, count);
	                        var outputReport = new CommonOutputReport() { Bytes = packet, Feature = feature };
	                        queue.Enqueue(outputReport);
	                        Monitor.PulseAll(queue);
	
	                        while (true)
	                        {
	                            if (outputReport.Done)
	                            {
	                                if (!outputReport.DoneOK) { throw new IOException(); }
	                                return;
	                            }
	
	                            timeout = GetTimeout(startTime, writeTimeout);
	                            if (!Monitor.Wait(queue, timeout)) { throw new TimeoutException(); }
	                        }
	                    }
	
	                    timeout = GetTimeout(startTime, writeTimeout);
	                    if (!Monitor.Wait(queue, timeout)) { throw new TimeoutException(); }
	                }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        /// <exclude />
        public override void Flush()
        {
            
        }

        /// <summary>
        /// Sends a Get Feature setup request.
        /// </summary>
        /// <param name="buffer">The buffer to fill. Place the Report ID in the first byte.</param>
        public void GetFeature(byte[] buffer)
        {
            Throw.If.Null(buffer, "buffer");
            GetFeature(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Sends a Get Feature setup request.
        /// </summary>
        /// <param name="buffer">The buffer to fill. Place the Report ID in the byte at index <paramref name="offset"/>.</param>
        /// <param name="offset">The index in the buffer to begin filling with data.</param>
        /// <param name="count">The number of bytes in the feature request.</param>
        public abstract void GetFeature(byte[] buffer, int offset, int count);

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Throw.If.OutOfRange(buffer, offset, count);
            return AsyncResult<int>.BeginOperation(delegate()
            {
                return Read(buffer, offset, count);
            }, callback, state);
        }

        /// <summary>
        /// Reads HID Input Reports.
        /// </summary>
        /// <returns>The data read.</returns>
        public byte[] Read()
        {
            byte[] buffer = new byte[Device.MaxInputReportLength];
            int bytes = Read(buffer); Array.Resize(ref buffer, bytes);
            return buffer;
        }

        /// <summary>
        /// Reads HID Input Reports.
        /// </summary>
        /// <param name="buffer">The buffer to place the reports into.</param>
        /// <returns>The number of bytes read.</returns>
        public int Read(byte[] buffer)
        {
            Throw.If.Null(buffer, "buffer");
            return Read(buffer, 0, buffer.Length);
        }

        /// <inheritdoc />
        public override int EndRead(IAsyncResult asyncResult)
        {
            Throw.If.Null(asyncResult, "asyncResult");
            return ((AsyncResult<int>)asyncResult).EndOperation();
        }

        /// <exclude />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sends a Set Feature setup request.
        /// </summary>
        /// <param name="buffer">The buffer of data to send. Place the Report ID in the first byte.</param>
        public void SetFeature(byte[] buffer)
        {
            Throw.If.Null(buffer, "buffer");
            SetFeature(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Sends a Set Feature setup request.
        /// </summary>
        /// <param name="buffer">The buffer of data to send. Place the Report ID in the byte at index <paramref name="offset"/>.</param>
        /// <param name="offset">The index in the buffer to start the write from.</param>
        /// <param name="count">The number of bytes in the feature request.</param>
        public abstract void SetFeature(byte[] buffer, int offset, int count);

        /// <exclude />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Throw.If.OutOfRange(buffer, offset, count);
            return AsyncResult<int>.BeginOperation(delegate()
            {
                Write(buffer, offset, count); return 0;
            }, callback, state);
        }
		
		internal void HandleInitAndOpen()
		{
			_opened = 1; _refCount = 1;
		}
		
		internal bool HandleClose()
		{
			return 0 == Interlocked.CompareExchange(ref _closed, 1, 0) && _opened != 0;
		}
		
		internal bool HandleAcquire()
		{
			while (true)
			{
				int refCount = _refCount;
				if (refCount == 0) { return false; }
				
				if (refCount == Interlocked.CompareExchange
				    (ref _refCount, refCount + 1, refCount))
				{
					return true;
				}
			}
		}
		
		internal void HandleAcquireIfOpenOrFail()
		{
			if (_closed != 0 || !HandleAcquire()) { throw new IOException("Closed."); }
		}
		
		internal void HandleRelease()
		{
			if (0 == Interlocked.Decrement(ref _refCount))
			{
				if (_opened != 0) { HandleFree(); }
			}
		}
		
		internal abstract void HandleFree();
		
        /// <summary>
        /// Writes an HID Output Report to the device.
        /// </summary>
        /// <param name="buffer">The buffer containing the report. Place the Report ID in the first byte.</param>
        public void Write(byte[] buffer)
        {
            Throw.If.Null(buffer, "buffer");
            Write(buffer, 0, buffer.Length);
        }

        /// <inheritdoc />
        public override void EndWrite(IAsyncResult asyncResult)
        {
            Throw.If.Null(asyncResult, "asyncResult");
            ((AsyncResult<int>)asyncResult).EndOperation();
        }

        /// <exclude />
        public override bool CanRead
        {
            get { return true; }
        }

        /// <exclude />
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <exclude />
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <exclude />
        public override bool CanTimeout
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the <see cref="HidDevice"/> associated with this stream.
        /// </summary>
        public abstract HidDevice Device
        {
            get;
        }

        /// <exclude />
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        /// <exclude />
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// The maximum amount of time, in milliseconds, to wait for to receive a HID report.
        /// 
        /// The default is 3000 milliseconds.
        /// To disable the timeout, set this to <see cref="Timeout.Infinite"/>.
        /// </summary>
        public sealed override int ReadTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum amount of time, in milliseconds, to wait for the device to acknowledge a HID report.
        /// 
        /// The default is 3000 milliseconds.
        /// To disable the timeout, set this to <see cref="Timeout.Infinite"/>.
        /// </summary>
        public sealed override int WriteTimeout
        {
            get;
            set;
        }
    }
}
