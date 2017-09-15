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
    public delegate void ReportScanCallback
        (byte[] buffer, int bitOffset, ReportSegment segment);

    /// <summary>
    /// Reads and writes HID reports.
    /// </summary>
    public class Report
    {
        internal List<ReportSegment> _segments;

        /// <summary>
        /// Initializes a new instance of the <see cref="Report"/> class.
        /// </summary>
        public Report()
        {
            _segments = new List<ReportSegment>();
        }

        /// <summary>
        /// Resets the instance to its initial state.
        /// </summary>
        public void Clear()
        {
            List<ReportSegment> segments = new List<ReportSegment>(_segments);
            foreach (ReportSegment segment in segments) { segment.Report = null; }
            ID = 0; Type = 0;
        }

        /// <summary>
        /// Reads a HID report, calling back a provided function for each segment.
        /// </summary>
        /// <param name="buffer">The buffer containing the report.</param>
        /// <param name="offset">The offset to begin reading the report at.</param>
        /// <param name="callback">
        ///     This callback will be called for each report segment.
        ///     Use this to read every value you need.
        /// </param>
        public void Scan(byte[] buffer, int offset, ReportScanCallback callback)
        {
            int bitOffset = offset * 8;

            foreach (ReportSegment segment in Segments)
            {
                callback(buffer, bitOffset, segment);
                bitOffset += segment.BitCount;
            }
        }

        /// <summary>
        /// Writes a HID report, calling back a provided function for each segment.
        /// </summary>
        /// <param name="callback">
        ///     This callback will be called for each report segment.
        ///     Write to each segment to write a complete HID report.
        /// </param>
        public byte[] Write(ReportScanCallback callback)
        {
            byte[] buffer = new byte[1 + Length];
            buffer[0] = ID; Scan(buffer, 1, callback);
            return buffer;
        }

        /// <summary>
        /// The Report ID.
        /// </summary>
        public byte ID
        {
            get;
            set;
        }

        /// <summary>
        /// The length of this particular report.
        /// The Report ID is not included in this length.
        /// </summary>
        public int Length
        {
            get
            {
                int bits = 0;
                foreach (ReportSegment segment in _segments) { bits += segment.BitCount; }
                return (bits + 7) / 8;
            }
        }

        public IEnumerable<ReportSegment> Segments
        {
            get { foreach (ReportSegment segment in _segments) { yield return segment; } }
        }

        public ReportType Type
        {
            get;
            set;
        }
    }
}
