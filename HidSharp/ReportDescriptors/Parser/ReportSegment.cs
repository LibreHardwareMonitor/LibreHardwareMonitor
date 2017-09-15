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
using System.Collections.Generic;

namespace HidSharp.ReportDescriptors.Parser
{
    public class ReportSegment : ReportMainItem
    {
        Report _report;

        public int ConvertArbitraryRangeToValue(double quantity, double minimum, double maximum)
        {
            return Math.Max(LogicalMinimum, Math.Min(LogicalMaximum,
                (int)((quantity - minimum) * LogicalRange / (maximum - minimum))));
        }

        public int ConvertPhysicalQuantityToValue(double quantity)
        {
            double exp = Math.Pow(10, UnitExponent);
            return ConvertArbitraryRangeToValue(quantity,
                PhysicalMinimum * exp, PhysicalMaximum * exp);
        }

        public double ConvertValueToArbitraryRange(int value,
            double minimum, double maximum)
        {
            return minimum + (value - LogicalMinimum) * (maximum - minimum) / LogicalRange;
        }

        public double ConvertValueToPhysicalQuantity(int value)
        {
            double exp = Math.Pow(10, UnitExponent);
            return ConvertValueToArbitraryRange(value,
                PhysicalMinimum * exp, PhysicalRange * exp);
        }

        public int DecodeSigned(uint value)
        {
            uint signBit = 1u << (ElementSize - 1), mask = signBit - 1;
            return (value & signBit) != 0 ? (int)(value | ~mask) : (int)value;
        }

        public uint EncodeSigned(int value)
        {
            uint usValue = (uint)value;
            uint signBit = 1u << (ElementSize - 1), mask = signBit - 1;
            return (usValue & mask) | (value < 0 ? signBit : 0);
        }

        public bool IsValueOutOfRange(int value)
        {
            return LogicalIsSigned
                ? value < LogicalMinimum || value > LogicalMaximum
                : ((uint)value < (uint)LogicalMinimum || (uint)value > (uint)LogicalMaximum)
                ;
        }

        public int Read(byte[] buffer, int bitOffset, int element)
        {
            uint rawValue = ReadRaw(buffer, bitOffset, element);
            return LogicalIsSigned ? DecodeSigned(rawValue) : (int)rawValue;
        }

        public uint ReadRaw(byte[] buffer, int bitOffset, int element)
        {
            uint value = 0; int totalBits = Math.Min(ElementSize, 32);
            bitOffset += element * ElementSize;

            for (int i = 0; i < totalBits; i++, bitOffset ++)
            {
                int byteStart = bitOffset >> 3; byte bitStart = (byte)(1u << (bitOffset & 7));
                value |= (buffer[byteStart] & bitStart) != 0 ? (1u << i) : 0;
            }

            return value;
        }

        public void Write(byte[] buffer, int bitOffset, int element, int value)
        {
            WriteRaw(buffer, bitOffset, element,
                LogicalIsSigned ? EncodeSigned(value) : (uint)value);
        }

        public void WriteRaw(byte[] buffer, int bitOffset, int element, uint value)
        {
            int totalBits = Math.Min(ElementSize, 32);
            bitOffset += element * ElementSize;

            for (int i = 0; i < totalBits; i++, bitOffset++)
            {
                int byteStart = bitOffset >> 3; uint bitStart = 1u << (bitOffset & 7);
                if ((value & (1 << i)) != 0) { buffer[byteStart] |= (byte)bitStart; } else { buffer[byteStart] &= (byte)(~bitStart); }
            }
        }

        public int BitCount
        {
            get { return ElementCount * ElementSize; }
        }

        public int ElementCount
        {
            get;
            set;
        }

        public int ElementSize
        {
            get;
            set;
        }

        public DataMainItemFlags Flags
        {
            get;
            set;
        }

        public bool LogicalIsSigned
        {
            get;
            set;
        }

        public int LogicalMinimum
        {
            get;
            set;
        }

        public int LogicalMaximum
        {
            get;
            set;
        }

        public int LogicalRange
        {
            get { return LogicalMaximum - LogicalMinimum; }
        }

        public int PhysicalMinimum
        {
            get;
            set;
        }

        public int PhysicalMaximum
        {
            get;
            set;
        }

        public int PhysicalRange
        {
            get { return PhysicalMaximum - PhysicalMinimum; }
        }

        public Report Report
        {
            get { return _report; }
            set
            {
                if (_report == value) { return; }

                if (_report != null) { _report._segments.Remove(this); }
                _report = value;
                if (_report != null) { _report._segments.Add(this); }
            }
        }

        public double Resolution
        {
            get
            {
                return (double)LogicalRange /
                    ((double)PhysicalRange *
                    Math.Pow(10, UnitExponent));
            }
        }

        public Units.Unit Unit
        {
            get;
            set;
        }

        public int UnitExponent
        {
            get;
            set;
        }
    }
}
