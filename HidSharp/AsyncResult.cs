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
using System.Threading;

namespace HidSharp
{
    class AsyncResult<T> : IAsyncResult
    {
        volatile bool _isCompleted;
        ManualResetEvent _waitHandle;

        AsyncResult(AsyncCallback callback, object state)
        {
            AsyncCallback = callback; AsyncState = state;
        }

        void Complete()
        {
            lock (this)
            {
                if (_isCompleted) { return; } _isCompleted = true;
                if (_waitHandle != null) { _waitHandle.Set(); }
            }

            if (AsyncCallback != null) { AsyncCallback(this); }
        }

        internal delegate T OperationCallback();

        internal static IAsyncResult BeginOperation(OperationCallback operation,
            AsyncCallback callback, object state)
        {
            var ar = new AsyncResult<T>(callback, state);
            ThreadPool.QueueUserWorkItem(delegate(object self)
            {
                try { ar.Result = operation(); }
                catch (Exception e) { ar.Exception = e; }
                ar.Complete();
            }, ar);
            return ar;
        }

        internal T EndOperation()
        {
            while (true)
            {
                if (IsCompleted)
                {
                    if (Exception != null) { throw Exception; }
                    return Result;
                }
                AsyncWaitHandle.WaitOne();
            }
        }

        public AsyncCallback AsyncCallback
        {
            get;
            private set;
        }

        public object AsyncState
        {
            get;
            private set;
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (this)
                {
                    if (_waitHandle == null)
                    {
                        _waitHandle = new ManualResetEvent(_isCompleted);
                    }
                }

                return _waitHandle;
            }
        }

        public bool CompletedSynchronously
        {
            get { return false; }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
        }

        Exception Exception
        {
            get;
            set;
        }

        T Result
        {
            get;
            set;
        }
    }
}