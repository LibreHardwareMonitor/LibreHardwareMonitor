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

namespace HidSharp.Platform
{
    sealed class Utf8Marshaler : ICustomMarshaler
    {
        bool _allocated; // workaround for Mono bug 4722

        public void CleanUpManagedData(object obj)
        {

        }

        public void CleanUpNativeData(IntPtr ptr)
        {
			if (IntPtr.Zero == ptr || !_allocated) { return; }
            Marshal.FreeHGlobal(ptr); _allocated = false;
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            string str = obj as string;
            if (str == null) { return IntPtr.Zero; }

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0);
            _allocated = true; return ptr;
        }

        public object MarshalNativeToManaged(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) { return null; }

            int length;
            for (length = 0; Marshal.ReadByte(ptr, length) != 0; length++) ;

            byte[] bytes = new byte[length];
            Marshal.Copy(ptr, bytes, 0, bytes.Length);
            string str = Encoding.UTF8.GetString(bytes);
            return str;
        }
		
		public static ICustomMarshaler GetInstance(string cookie)
		{
			return new Utf8Marshaler();
		}
    }
}
