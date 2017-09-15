#region License
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

using System;

namespace HidSharp.ReportDescriptors.Units
{
    /// <summary>
    /// Describes the units of a report value.
    /// </summary>
    public class Unit
    {
        uint _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Unit"/> class.
        /// </summary>
        /// <param name="value">The raw HID value describing the units.</param>
        public Unit(uint value)
        {
            _value = value;
        }

        uint Element(int index)
        {
            return (Value >> (index << 2)) & 0xf;
        }

        int Exponent(int index)
        {
            return DecodeExponent(Element(index));
        }

        /// <summary>
        /// Decodes an encoded HID unit exponent.
        /// </summary>
        /// <param name="value">The encoded exponent.</param>
        /// <returns>The exponent.</returns>
        public static int DecodeExponent(uint value)
        {
            if (value > 15) { throw new ArgumentOutOfRangeException("value", "Value range is [0, 15]."); }
            return value >= 8 ? (int)value - 16 : (int)value;
        }

        void Element(int index, uint value)
        {
            Value &= 0xfu << (index << 2); Value |= (value & 0xfu) << (index << 2);
        }

        /// <summary>
        /// Encodes an exponent in HID unit form.
        /// </summary>
        /// <param name="value">The exponent.</param>
        /// <returns>The encoded exponent.</returns>
        public static uint EncodeExponent(int value)
        {
            if (value < -8 || value > 7)
                { throw new ArgumentOutOfRangeException("value", "Exponent range is [-8, 7]."); }
            return (uint)(value < 0 ? value + 16 : value);
        }

        void Exponent(int index, int value)
        {
            Element(index, EncodeExponent(value));
        }

        /// <summary>
        /// Gets or sets the unit system.
        /// </summary>
        public UnitSystem System
        {
            get { return (UnitSystem)Element(0); }
            set { Element(0, (uint)value); }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of length.
        /// </summary>
        public int LengthExponent
        {
            get { return Exponent(1); }
            set { Exponent(1, value); }
        }

        /// <summary>
        /// Gets the units of length corresponding to <see cref="System"/>.
        /// </summary>
        public LengthUnit LengthUnit
        {
            get
            {
                switch (System)
                {
                    case UnitSystem.SILinear: return LengthUnit.Centimeter;
                    case UnitSystem.SIRotation: return LengthUnit.Radians;
                    case UnitSystem.EnglishLinear: return LengthUnit.Inch;
                    case UnitSystem.EnglishRotation: return LengthUnit.Degrees;
                    default: return LengthUnit.None;
                }
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of mass.
        /// </summary>
        public int MassExponent
        {
            get { return Exponent(2); }
            set { Exponent(2, value); }
        }

        /// <summary>
        /// Gets the units of mass corresponding to <see cref="System"/>.
        /// </summary>
        public MassUnit MassUnit
        {
            get
            {
                switch (System)
                {
                    case UnitSystem.SILinear:
                    case UnitSystem.SIRotation: return MassUnit.Gram;
                    case UnitSystem.EnglishLinear:
                    case UnitSystem.EnglishRotation: return MassUnit.Slug;
                    default: return MassUnit.None;
                }
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of time.
        /// </summary>
        public int TimeExponent
        {
            get { return Exponent(3); }
            set { Exponent(3, value); }
        }

        /// <summary>
        /// Gets the units of time corresponding to <see cref="System"/>.
        /// </summary>
        public TimeUnit TimeUnit
        {
            get
            {
                return System != UnitSystem.None
                    ? TimeUnit.Seconds : TimeUnit.None;
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of temperature.
        /// </summary>
        public int TemperatureExponent
        {
            get { return Exponent(4); }
            set { Exponent(4, value); }
        }

        /// <summary>
        /// Gets the units of temperature corresponding to <see cref="System"/>.
        /// </summary>
        public TemperatureUnit TemperatureUnit
        {
            get
            {
                switch (System)
                {
                    case UnitSystem.SILinear:
                    case UnitSystem.SIRotation: return TemperatureUnit.Kelvin;
                    case UnitSystem.EnglishLinear:
                    case UnitSystem.EnglishRotation: return TemperatureUnit.Fahrenheit;
                    default: return TemperatureUnit.None;
                }
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of current.
        /// </summary>
        public int CurrentExponent
        {
            get { return Exponent(5); }
            set { Exponent(5, value); }
        }

        /// <summary>
        /// Gets the units of current corresponding to <see cref="System"/>.
        /// </summary>
        public CurrentUnit CurrentUnit
        {
            get
            {
                return System != UnitSystem.None
                    ? CurrentUnit.Ampere : CurrentUnit.None;
            }
        }

        /// <summary>
        /// Gets or sets the exponent of the report value's units of luminous intensity.
        /// </summary>
        public int LuminousIntensityExponent
        {
            get { return Exponent(6); }
            set { Exponent(6, value); }
        }

        /// <summary>
        /// Gets the units of luminous intensity corresponding to <see cref="System"/>.
        /// </summary>
        public LuminousIntensityUnit LuminousIntensityUnit
        {
            get
            {
                return System != UnitSystem.None
                    ? LuminousIntensityUnit.Candela : LuminousIntensityUnit.None;
            }
        }

        /// <summary>
        /// Gets or sets the raw HID value describing the units.
        /// </summary>
        public uint Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }
}
