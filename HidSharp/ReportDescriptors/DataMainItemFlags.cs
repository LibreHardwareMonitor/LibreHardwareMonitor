#region License
/* Copyright 2011 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.ReportDescriptors
{
    [Flags]
    public enum DataMainItemFlags : uint
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Constant values cannot be changed.
        /// </summary>
        Constant = 1 << 0,

        /// <summary>
        /// Each variable field corresponds to a particular value.
        /// The alternative is an array, where each field specifies an index.
        /// For example, with eight buttons, a variable field would have eight bits.
        /// An array would have an index of which button is pressed.
        /// </summary>
        Variable = 1 << 1,

        /// <summary>
        /// Mouse motion is in relative coordinates.
        /// Most sensors -- joysticks, accelerometers, etc. -- output absolute coordinates.
        /// </summary>
        Relative = 1 << 2,

        /// <summary>
        /// The value wraps around in a continuous manner.
        /// </summary>
        Wrap = 1 << 3,

        Nonlinear = 1 << 4,

        NoPreferred = 1 << 5,

        NullState = 1 << 6,

        Volatile = 1 << 7,

        BufferedBytes = 1 << 8
    }
}
