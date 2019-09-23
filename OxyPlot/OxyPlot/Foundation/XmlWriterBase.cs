// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlWriterBase.cs" company="OxyPlot">
//   The MIT License (MIT)
//
//   Copyright (c) 2012 Oystein Bjorke
//
//   Permission is hereby granted, free of charge, to any person obtaining a
//   copy of this software and associated documentation files (the
//   "Software"), to deal in the Software without restriction, including
//   without limitation the rights to use, copy, modify, merge, publish,
//   distribute, sublicense, and/or sell copies of the Software, and to
//   permit persons to whom the Software is furnished to do so, subject to
//   the following conditions:
//
//   The above copyright notice and this permission notice shall be included
//   in all copies or substantial portions of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//   CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//   TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary>
//   Abstract base class for exporters that write xml.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Provides an abstract base class for exporters that write xml.
    /// </summary>
    public abstract class XmlWriterBase : IDisposable
    {
        /// <summary>
        /// The xml writer.
        /// </summary>
        private XmlWriter w;

        /// <summary>
        /// The disposed flag.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref = "XmlWriterBase" /> class.
        /// </summary>
        protected XmlWriterBase()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlWriterBase"/> class.
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        protected XmlWriterBase(Stream stream)
        {
            this.w = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public virtual void Close()
        {
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Flushes this instance.
        /// </summary>
        public void Flush()
        {
            this.w.Flush();
        }

        /// <summary>
        /// The write attribute string.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        protected void WriteAttributeString(string name, string value)
        {
            this.w.WriteAttributeString(name, value);
        }

        /// <summary>
        /// The write doc type.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="pubid">
        /// The pubid.
        /// </param>
        /// <param name="sysid">
        /// The sysid.
        /// </param>
        /// <param name="subset">
        /// The subset.
        /// </param>
        protected void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            this.w.WriteDocType(name, pubid, sysid, subset);
        }

        /// <summary>
        /// The write element string.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        protected void WriteElementString(string name, string text)
        {
            this.w.WriteElementString(name, text);
        }

        /// <summary>
        /// The write end document.
        /// </summary>
        protected void WriteEndDocument()
        {
            this.w.WriteEndDocument();
        }

        /// <summary>
        /// The write end element.
        /// </summary>
        protected void WriteEndElement()
        {
            this.w.WriteEndElement();
        }

        /// <summary>
        /// The write raw.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        protected void WriteRaw(string text)
        {
            this.w.WriteRaw(text);
        }

        /// <summary>
        /// The write start document.
        /// </summary>
        /// <param name="standalone">
        /// The standalone.
        /// </param>
        protected void WriteStartDocument(bool standalone)
        {
            this.w.WriteStartDocument(standalone);
        }

        /// <summary>
        /// The write start element.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        protected void WriteStartElement(string name)
        {
            this.w.WriteStartElement(name);
        }

        /// <summary>
        /// The write start element.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="ns">
        /// The ns.
        /// </param>
        protected void WriteStartElement(string name, string ns)
        {
            this.w.WriteStartElement(name, ns);
        }

        /// <summary>
        /// The write string.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        protected void WriteString(string text)
        {
            this.w.WriteString(text);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Close();
                }
            }

            this.disposed = true;
        }
    }
}