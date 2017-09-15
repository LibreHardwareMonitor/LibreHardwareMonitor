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

namespace HidSharp.Platform.Unsupported
{
    class UnsupportedHidManager : HidManager
    {
        protected override object[] Refresh()
        {
            return new object[0];
        }

        protected override bool TryCreateDevice(object key, out HidDevice device, out object creationState)
        {
            throw new NotImplementedException();
        }

        protected override void CompleteDevice(object key, HidDevice device, object creationState)
        {
            throw new NotImplementedException();
        }

        public override bool IsSupported
        {
            get { return true; }
        }
    }
}
