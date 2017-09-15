#region License
/* Copyright 2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp
{
    sealed class Throw
    {
        Throw()
        {

        }

        public static Throw If
        {
            get { return null; }
        }
    }

    static class ThrowExtensions
    {
        public static Throw Null<T>(this Throw self, T value, string paramName)
        {
            if (value == null) { throw new ArgumentNullException(paramName); }
            return null;
        }

        public static Throw OutOfRange<T>(this Throw self, IList<T> buffer, int offset, int count)
        {
            Throw.If.Null(buffer, "buffer");
            if (offset < 0 || offset > buffer.Count) { throw new ArgumentOutOfRangeException("offset"); }
            if (count < 0 || count > buffer.Count - offset) { throw new ArgumentOutOfRangeException("count"); }
            return null;
        }
    }
}
