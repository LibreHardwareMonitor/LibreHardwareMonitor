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

namespace HidSharp.ReportDescriptors.Parser
{
    public class ReportMainItem
    {
        ReportCollection _parent;

        public ReportMainItem()
        {
            Indexes = new LocalIndexes();
        }

        public LocalIndexes Indexes
        {
            get;
            private set;
        }

        public ReportCollection Parent
        {
            get { return _parent; }
            set
            {
                if (_parent == value) { return; }

                ReportCollection check = value;
                while (check != null && check != this) { check = check.Parent; }
                if (check == this) { throw new ArgumentException("Can't set up a loop."); }

                if (_parent != null) { _parent._children.Remove(this); }
                _parent = value;
                if (_parent != null) { _parent._children.Add(this); }
            }
        }
    }
}
