﻿#region License
/* Copyright 2011, 2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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

namespace HidSharp.ReportDescriptors.Units
{
    /// <summary>
    /// Defines the possible units of temperature.
    /// </summary>
    public enum TemperatureUnit
    {
        /// <summary>
        /// The unit system has no unit of temperature.
        /// </summary>
        None,

        /// <summary>
        /// The unit of temperature is Kelvin (occurs in SI Linear and Rotation unit systems).
        /// </summary>
        Kelvin,

        /// <summary>
        /// The unit of temperature is Fahrenheit (occurs in English Linear and Rotation unit systems).
        /// </summary>
        Fahrenheit
    }
}
