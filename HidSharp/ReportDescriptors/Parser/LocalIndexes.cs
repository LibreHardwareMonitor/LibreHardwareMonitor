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

using System.Collections.Generic;

namespace HidSharp.ReportDescriptors.Parser
{
    public class LocalIndexes
    {
        IndexBase _designator, _string, _usage;

        public void Clear()
        {
            Designator = null; String = null; Usage = null;
        }

        public IndexBase Designator
        {
            get { return _designator ?? IndexBase.Unset; }
            set { _designator = value; }
        }

        public IndexBase String
        {
            get { return _string ?? IndexBase.Unset; }
            set { _string = value; }
        }

        public IndexBase Usage
        {
            get { return _usage ?? IndexBase.Unset; }
            set { _usage = value; }
        }
    }
}
