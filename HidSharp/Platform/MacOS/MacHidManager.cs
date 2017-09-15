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
using System.Linq;

namespace HidSharp.Platform.MacOS
{
    class MacHidManager : HidManager
    {
        protected override object[] Refresh()
        {
            var paths = new List<NativeMethods.io_string_t>();

            var matching = NativeMethods.IOServiceMatching("IOHIDDevice").ToCFType(); // Consumed by IOServiceGetMatchingServices, so DON'T Dispose().
            if (matching.IsSet)
            {
                int iteratorObj;
                if (NativeMethods.IOReturn.Success == NativeMethods.IOServiceGetMatchingServices(0, matching, out iteratorObj))
                {
                    using (var iterator = iteratorObj.ToIOObject())
                    {
                        while (true)
                        {
                            using (var handle = NativeMethods.IOIteratorNext(iterator).ToIOObject())
                            {
                                if (!handle.IsSet) { break; }

                                NativeMethods.io_string_t path;
                                if (NativeMethods.IOReturn.Success == NativeMethods.IORegistryEntryGetPath(handle, "IOService", out path))
                                {
                                    paths.Add(path);
                                }
                            }
                        }
                    }
                }
            }

            return paths.Cast<object>().ToArray();
        }

        protected override bool TryCreateDevice(object key, out HidDevice device, out object creationState)
        {
            creationState = null;
            var path = (NativeMethods.io_string_t)key; var hidDevice = new MacHidDevice(path);
            using (var handle = NativeMethods.IORegistryEntryFromPath(0, ref path).ToIOObject())
            {
                if (!handle.IsSet || !hidDevice.GetInfo(handle)) { device = null; return false; }
                device = hidDevice; return true;
            }
        }

        protected override void CompleteDevice(object key, HidDevice device, object creationState)
        {
            
        }

        public override bool IsSupported
        {
            get
            {
                try
                {
                    IntPtr major; NativeMethods.OSErr majorErr = NativeMethods.Gestalt(NativeMethods.OSType.gestaltSystemVersionMajor, out major);
                    IntPtr minor; NativeMethods.OSErr minorErr = NativeMethods.Gestalt(NativeMethods.OSType.gestaltSystemVersionMinor, out minor);
                    if (majorErr == NativeMethods.OSErr.noErr && minorErr == NativeMethods.OSErr.noErr)
                    {
                        return (long)major >= 10 || ((long)major == 10 && (long)minor >= 5);
                    }
                }
                catch
                {

                }

                return false;
            }
        }
    }
}
