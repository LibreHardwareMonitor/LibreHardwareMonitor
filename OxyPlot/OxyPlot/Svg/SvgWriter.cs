// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SvgWriter.cs" company="OxyPlot">
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
//   Scalable Vector Graphics writer.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Represents a writer that provides easy generation of Scalable Vector Graphics files.
    /// </summary>
    public class SvgWriter : XmlWriterBase
    {
        /// <summary>
        /// The end is written.
        /// </summary>
        private bool endIsWritten;

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgWriter"/> class.
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="isDocument">
        /// if set to <c>true</c>, the writer will write the xml headers (?xml and !DOCTYPE).
        /// </param>
        public SvgWriter(Stream stream, double width, double height, bool isDocument = true)
            : base(stream)
        {
            this.IsDocument = isDocument;
            this.NumberFormat = "0.####";
            this.WriteHeader(width, height);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this writer should produce a stand-alone document.
        /// </summary>
        public bool IsDocument { get; set; }

        /// <summary>
        /// Gets or sets the number format.
        /// </summary>
        /// <value>The number format.</value>
        public string NumberFormat { get; set; }

        /// <summary>
        /// Closes the svg document.
        /// </summary>
        public override void Close()
        {
            if (!this.endIsWritten)
            {
                this.Complete();
            }

            base.Close();
        }

        /// <summary>
        /// Writes the end of the document.
        /// </summary>
        public void Complete()
        {
            this.WriteEndElement();
            if (this.IsDocument)
            {
                this.WriteEndDocument();
            }

            this.endIsWritten = true;
        }

        /// <summary>
        /// Creates a style.
        /// </summary>
        /// <param name="fill">
        /// The fill color.
        /// </param>
        /// <param name="stroke">
        /// The stroke color.
        /// </param>
        /// <param name="thickness">
        /// The stroke thickness.
        /// </param>
        /// <param name="dashArray">
        /// The line dash array.
        /// </param>
        /// <param name="lineJoin">
        /// The line join type.
        /// </param>
        /// <returns>
        /// A style string.
        /// </returns>
        public string CreateStyle(
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            double[] dashArray = null,
            OxyPenLineJoin lineJoin = OxyPenLineJoin.Miter)
        {
            // http://oreilly.com/catalog/svgess/chapter/ch03.html
            var style = new StringBuilder();
            if (fill == null)
            {
                style.AppendFormat("fill:none;");
            }
            else
            {
                style.AppendFormat("fill:{0};", this.ColorToString(fill));
                if (fill.A != 0xFF)
                {
                    style.AppendFormat(CultureInfo.InvariantCulture, "fill-opacity:{0};", fill.A / 255.0);
                }
            }

            if (stroke == null)
            {
                style.AppendFormat("stroke:none;");
            }
            else
            {
                string formatString = "stroke:{0};stroke-width:{1:" + this.NumberFormat + "}";
                style.AppendFormat(formatString, this.ColorToString(stroke), thickness);
                switch (lineJoin)
                {
                    case OxyPenLineJoin.Round:
                        style.AppendFormat(";stroke-linejoin:round");
                        break;
                    case OxyPenLineJoin.Bevel:
                        style.AppendFormat(";stroke-linejoin:bevel");
                        break;
                }

                if (stroke.A != 0xFF)
                {
                    style.AppendFormat(CultureInfo.InvariantCulture, ";stroke-opacity:{0}", stroke.A / 255.0);
                }

                if (dashArray != null && dashArray.Length > 0)
                {
                    style.Append(";stroke-dasharray:");
                    for (int i = 0; i < dashArray.Length; i++)
                    {
                        style.AppendFormat(
                            CultureInfo.InvariantCulture, "{0}{1}", i > 0 ? "," : string.Empty, dashArray[i]);
                    }
                }
            }

            return style.ToString();
        }

        /// <summary>
        /// Writes an ellipse.
        /// </summary>
        /// <param name="x">
        /// The x coordinate of the center.
        /// </param>
        /// <param name="y">
        /// The y coordinate of the center.
        /// </param>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="style">
        /// The style.
        /// </param>
        public void WriteEllipse(double x, double y, double width, double height, string style)
        {
            // http://www.w3.org/TR/SVG/shapes.html#EllipseElement
            this.WriteStartElement("ellipse");
            this.WriteAttributeString("cx", x + (width / 2));
            this.WriteAttributeString("cy", y + (height / 2));
            this.WriteAttributeString("rx", width / 2);
            this.WriteAttributeString("ry", height / 2);
            this.WriteAttributeString("style", style);
            this.WriteEndElement();
        }

        /// <summary>
        /// Writes a line.
        /// </summary>
        /// <param name="p1">
        /// The first point.
        /// </param>
        /// <param name="p2">
        /// The second point.
        /// </param>
        /// <param name="style">
        /// The style.
        /// </param>
        public void WriteLine(ScreenPoint p1, ScreenPoint p2, string style)
        {
            // http://www.w3.org/TR/SVG/shapes.html#LineElement
            // http://www.w3schools.com/svg/svg_line.asp
            this.WriteStartElement("line");
            this.WriteAttributeString("x1", p1.X);
            this.WriteAttributeString("y1", p1.Y);
            this.WriteAttributeString("x2", p2.X);
            this.WriteAttributeString("y2", p2.Y);
            this.WriteAttributeString("style", style);
            this.WriteEndElement();
        }

        /// <summary>
        /// Writes a polygon.
        /// </summary>
        /// <param name="points">
        /// The points.
        /// </param>
        /// <param name="style">
        /// The style.
        /// </param>
        public void WritePolygon(IEnumerable<ScreenPoint> points, string style)
        {
            // http://www.w3.org/TR/SVG/shapes.html#PolygonElement
            this.WriteStartElement("polygon");
            this.WriteAttributeString("points", this.PointsToString(points));
            this.WriteAttributeString("style", style);
            this.WriteEndElement();
        }

        /// <summary>
        /// Writes a polyline.
        /// </summary>
        /// <param name="pts">
        /// The points.
        /// </param>
        /// <param name="style">
        /// The style.
        /// </param>
        public void WritePolyline(IEnumerable<ScreenPoint> pts, string style)
        {
            // http://www.w3.org/TR/SVG/shapes.html#PolylineElement
            this.WriteStartElement("polyline");
            this.WriteAttributeString("points", this.PointsToString(pts));
            this.WriteAttributeString("style", style);
            this.WriteEndElement();
        }

        /// <summary>
        /// Writes a rectangle.
        /// </summary>
        /// <param name="x">
        /// The x coordinate.
        /// </param>
        /// <param name="y">
        /// The y coordinate.
        /// </param>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        /// <param name="style">
        /// The style.
        /// </param>
        public void WriteRectangle(double x, double y, double width, double height, string style)
        {
            // http://www.w3.org/TR/SVG/shapes.html#RectangleElement
            this.WriteStartElement("rect");
            this.WriteAttributeString("x", x);
            this.WriteAttributeString("y", y);
            this.WriteAttributeString("width", width);
            this.WriteAttributeString("height", height);
            this.WriteAttributeString("style", style);
            this.WriteEndElement();
        }

        /// <summary>
        /// Writes text.
        /// </summary>
        /// <param name="position">
        /// The position.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="fill">
        /// The text color.
        /// </param>
        /// <param name="fontFamily">
        /// The font family.
        /// </param>
        /// <param name="fontSize">
        /// The font size.
        /// </param>
        /// <param name="fontWeight">
        /// The font weight.
        /// </param>
        /// <param name="rotate">
        /// The rotation angle.
        /// </param>
        /// <param name="halign">
        /// The horizontal alignment.
        /// </param>
        /// <param name="valign">
        /// The vertical alignment.
        /// </param>
        public void WriteText(
            ScreenPoint position,
            string text,
            OxyColor fill,
            string fontFamily = null,
            double fontSize = 10,
            double fontWeight = FontWeights.Normal,
            double rotate = 0,
            HorizontalAlignment halign = HorizontalAlignment.Left,
            VerticalAlignment valign = VerticalAlignment.Top)
        {
            // http://www.w3.org/TR/SVG/text.html
            this.WriteStartElement("text");

            // WriteAttributeString("x", position.X);
            // WriteAttributeString("y", position.Y);
            string baselineAlignment = "hanging";
            if (valign == VerticalAlignment.Middle)
            {
                baselineAlignment = "middle";
            }

            if (valign == VerticalAlignment.Bottom)
            {
                baselineAlignment = "baseline";
            }

            this.WriteAttributeString("dominant-baseline", baselineAlignment);

            string textAnchor = "start";
            if (halign == HorizontalAlignment.Center)
            {
                textAnchor = "middle";
            }

            if (halign == HorizontalAlignment.Right)
            {
                textAnchor = "end";
            }

            this.WriteAttributeString("text-anchor", textAnchor);

            string fmt = "translate({0:" + this.NumberFormat + "},{1:" + this.NumberFormat + "})";
            string transform = string.Format(CultureInfo.InvariantCulture, fmt, position.X, position.Y);
            if (Math.Abs(rotate) > 0)
            {
                transform += string.Format(CultureInfo.InvariantCulture, " rotate({0})", rotate);
            }

            this.WriteAttributeString("transform", transform);

            if (fontFamily != null)
            {
                this.WriteAttributeString("font-family", fontFamily);
            }

            if (fontSize > 0)
            {
                this.WriteAttributeString("font-size", fontSize);
            }

            if (fontWeight > 0)
            {
                this.WriteAttributeString("font-weight", fontWeight);
            }

            this.WriteAttributeString("fill", this.ColorToString(fill));

            // WriteAttributeString("style", style);
            this.WriteString(text);
            this.WriteEndElement();
        }

        /// <summary>
        /// Converts a color to a svg color string.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>The color string.</returns>
        protected string ColorToString(OxyColor color)
        {
            if (color == OxyColors.Black)
            {
                return "black";
            }

            var formatString = "rgb({0:" + this.NumberFormat + "},{1:" + this.NumberFormat + "},{2:" + this.NumberFormat + "})";
            return string.Format(formatString, color.R, color.G, color.B);
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
        protected void WriteAttributeString(string name, double value)
        {
            this.WriteAttributeString(name, value.ToString(this.NumberFormat, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Converts a value to a string or to the specified "auto" string if the value is NaN.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="auto">The string to return if value is NaN.</param>
        /// <returns>A string.</returns>
        private string GetAutoValue(double value, string auto)
        {
            if (double.IsNaN(value))
            {
                return auto;
            }

            return value.ToString(this.NumberFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a list of points to a string.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A string.</returns>
        private string PointsToString(IEnumerable<ScreenPoint> points)
        {
            var sb = new StringBuilder();
            string fmt = "{0:" + this.NumberFormat + "},{1:" + this.NumberFormat + "} ";
            foreach (var p in points)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, fmt, p.X, p.Y);
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// The write header.
        /// </summary>
        /// <param name="width">
        /// The width.
        /// </param>
        /// <param name="height">
        /// The height.
        /// </param>
        private void WriteHeader(double width, double height)
        {
            // http://www.w3.org/TR/SVG/struct.html#SVGElement
            if (this.IsDocument)
            {
                this.WriteStartDocument(false);
                this.WriteDocType(
                    "svg", "-//W3C//DTD SVG 1.1//EN", "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd", null);
            }

            this.WriteStartElement("svg", "http://www.w3.org/2000/svg");
            this.WriteAttributeString("width", this.GetAutoValue(width, "100%"));
            this.WriteAttributeString("height", this.GetAutoValue(height, "100%"));
            this.WriteAttributeString("version", "1.1");
        }

    }
}