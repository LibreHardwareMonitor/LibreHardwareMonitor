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

namespace HidSharp.ReportDescriptors
{
    /// <summary>
    /// Describes the manner in which an item affects the descriptor.
    /// </summary>
    public enum ItemType : byte
    {
        /// <summary>
        /// Main items determine the report being described.
        /// For example, a main item switches between Input and Output reports.
        /// </summary>
        Main = 0,

        /// <summary>
        /// Global items affect all reports later in the descriptor.
        /// </summary>
        Global,

        /// <summary>
        /// Local items only affect the current report.
        /// </summary>
        Local,

        /// <summary>
        /// Long items use this type.
        /// </summary>
        Reserved
    }
}