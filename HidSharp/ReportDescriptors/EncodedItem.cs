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
using System.Collections.Generic;

namespace HidSharp.ReportDescriptors
{
    public class EncodedItem
    {
        public EncodedItem()
        {
            Data = new List<byte>();
        }

        public void Clear()
        {
            Data.Clear(); Tag = 0; Type = ItemType.Main;
        }

        public byte DataAt(int index)
        {
            return index >= 0 && index < Data.Count ? Data[index] : (byte)0;
        }

        static byte GetByte(IList<byte> buffer, ref int offset, ref int count)
        {
            if (count <= 0) { return 0; } else { count--; }
            return offset >= 0 && offset < buffer.Count ? buffer[offset++] : (byte)0;
        }

        public int Decode(IList<byte> buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);

            Clear(); int startCount = count;
            byte header = GetByte(buffer, ref offset, ref count);

            int size = header & 0x3; if (size == 3) { size = 4; }
            Type = (ItemType)((header >> 2) & 0x3); Tag = (byte)(header >> 4);
            for (int i = 0; i < size; i++) { Data.Add(GetByte(buffer, ref offset, ref count)); }
            return startCount - count;
        }

        public static IEnumerable<EncodedItem> DecodeRaw(IList<byte> buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);

            while (count > 0)
            {
                EncodedItem item = new EncodedItem();
                int bytes = item.Decode(buffer, offset, count);
                offset += bytes; count -= bytes;
                yield return item;
            }
        }

        public static IEnumerable<EncodedItem> DecodeHIDDT(IList<byte> buffer, int offset, int count)
        {
            Throw.If.OutOfRange(buffer, offset, count);

            while (count > 34)
            {
                EncodedItem item = new EncodedItem();
                int bytes = item.Decode(buffer, offset + 34, count - 34);
                offset += 10; count -= 10;
                yield return item;
            }
        }

        public void Encode(IList<byte> buffer)
        {
            Throw.If.Null(buffer, "buffer");

            if (buffer == null) { throw new ArgumentNullException("buffer"); }
            if (!IsShortTag) { return; }

            byte size = DataSize;
            buffer.Add((byte)((size == 4 ? (byte)3 : size) | (byte)Type << 2 | Tag << 4));
            foreach (byte @byte in Data) { buffer.Add(@byte); }
        }

        public static void EncodeRaw(IList<byte> buffer, IEnumerable<EncodedItem> items)
        {
            Throw.If.Null(buffer, "buffer").Null(items, "items");
            foreach (EncodedItem item in items) { item.Encode(buffer); }
        }

        public IList<byte> Data
        {
            get;
            private set;
        }

        public byte DataSize
        {
            get { return (byte)(IsShortTag ? Data.Count : 0); }
        }

        public uint DataValue
        {
            get
            {
                if (!IsShortTag) { return 0; }
                return (uint)(DataAt(0) | DataAt(1) << 8 | DataAt(2) << 16 | DataAt(3) << 24);
            }

            set
            {
                Data.Clear();
                Data.Add((byte)value);
                if (value > 0xff) { Data.Add((byte)(value >> 8)); }
                if (value > 0xffff) { Data.Add((byte)(value >> 16)); Data.Add((byte)(value >> 24)); }
            }
        }

        public bool DataValueMayBeNegative
        {
            get { return IsShortTag && Data.Count > 0 && (Data[Data.Count - 1] & 0x80) != 0; }
        }

        public int DataValueSigned
        {
            get
            {
                if (!IsShortTag) { return 0; }
                return Data.Count == 4 ? (int)DataValue :
                    Data.Count == 2 ? (short)DataValue :
                    Data.Count == 1 ? (sbyte)DataValue : (sbyte)0;
            }

            set
            {
                if (value == 0)
                    { DataValue = (uint)value; }
                else if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                    { DataValue = (uint)(sbyte)value; if (value < 0) { Data.Add(0); } }
                else if (value >= short.MinValue && value <= short.MaxValue)
                    { DataValue = (uint)(short)value; if (value < 0) { Data.Add(0); Data.Add(0); } }
                else
                    { DataValue = (uint)value; }
            }
        }

        public int EncodedSize
        {
            get { return IsShortTag ? (DataSize + 1) : 0; }
        }

        public byte Tag
        {
            get;
            set;
        }

        public GlobalItemTag TagForGlobal
        {
            get { return (GlobalItemTag)Tag; }
        }

        public LocalItemTag TagForLocal
        {
            get { return (LocalItemTag)Tag; }
        }

        public MainItemTag TagForMain
        {
            get { return (MainItemTag)Tag; }
        }

        public ItemType Type
        {
            get;
            set;
        }

        public bool IsShortTag
        {
            get
            {
                return !IsLongTag &&
                    (Data.Count == 0 || Data.Count == 1 || Data.Count == 2 || Data.Count == 4);
            }
        }

        public bool IsLongTag
        {
            get
            {
                return Tag == 15 && Type == ItemType.Reserved && Data.Count >= 2;
            }
        }
    }
}
