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

namespace HidSharp.Platform.Linux
{
    class LinuxHidManager : HidManager
    {
        protected override object[] Refresh()
        {
            var paths = new List<string>();

	        IntPtr udev = NativeMethods.udev_new();
            if (IntPtr.Zero != udev)
            {
                try
                {
                    IntPtr enumerate = NativeMethods.udev_enumerate_new(udev);
                    if (IntPtr.Zero != enumerate)
                    {
                        try
                        {
                            if (0 == NativeMethods.udev_enumerate_add_match_subsystem(enumerate, "hidraw") &&
                                0 == NativeMethods.udev_enumerate_scan_devices(enumerate))
                            {
                                IntPtr entry;
                                for (entry = NativeMethods.udev_enumerate_get_list_entry(enumerate); entry != IntPtr.Zero;
                                     entry = NativeMethods.udev_list_entry_get_next(entry))
                                {
                                    string syspath = NativeMethods.udev_list_entry_get_name(entry);
                                    if (syspath != null) { paths.Add(syspath); }
                                }
                            }
                        }
                        finally
                        {
                            NativeMethods.udev_enumerate_unref(enumerate);
                        }
                    }
                }
                finally
                {
                    NativeMethods.udev_unref(udev);
                }
            }

            return paths.Cast<object>().ToArray();
        }

        protected override bool TryCreateDevice(object key, out HidDevice device, out object creationState)
        {
            creationState = null;
            string syspath = (string)key; var hidDevice = new LinuxHidDevice(syspath);
            if (!hidDevice.GetInfo()) { device = null; return false; }
            device = hidDevice; return true;
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
					string sysname; Version release; string machine;
					if (NativeMethods.uname(out sysname, out release, out machine))
					{
						IntPtr udev = NativeMethods.udev_new();
						if (IntPtr.Zero != udev)
						{
							NativeMethods.udev_unref(udev);
							return sysname == "Linux" && release >= new Version(2, 6, 36);
						}
					}
                }
				catch
				{
					
				}
                finally
                {

                }

                return false;
            }
        }
    }
}
