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
    public class IndexList : IndexBase
    {
        public IndexList()
        {
            Indices = new List<IList<uint>>();
        }

        public override bool IndexFromValue(uint value, out int index)
        {
            for (int i = 0; i < Indices.Count; i ++)
            {
                foreach (uint thisValue in Indices[i])
                {
                    if (thisValue == value) { index = i; return true; }
                }
            }

            return base.IndexFromValue(value, out index);
        }

        public override IEnumerable<uint> ValuesFromIndex(int index)
        {
            if (index < 0 || Indices.Count == 0) { yield break; }
            foreach (uint value in Indices[Math.Min(Count - 1, index)])
                { yield return value; }
        }

        public override int Count
        {
            get { return Indices.Count; }
        }

        public IList<IList<uint>> Indices
        {
            get;
            private set;
        }
    }
}
