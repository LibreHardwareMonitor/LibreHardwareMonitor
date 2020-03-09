// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IRenderContext.cs" company="OxyPlot">
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
//   Render context interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines rendering functionality.
    /// </summary>
    public interface IRenderContext
    {
        /// <summary>
        /// Gets a value indicating whether the context renders to screen.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the context renders to screen; otherwise, <c>false</c>.
        /// </value>
        bool RendersToScreen { get; }

        /// <summary>
        /// Draws an ellipse.
        /// </summary>
        /// <param name="rect">
        /// The rectangle.
        /// </param>
        /// <param name="fill">
        /// The fill color.
        /// </param>
        /// <param name="stroke">
        /// The stroke color.
        /// </param>
        /// <param name="thickness">
        /// The thickness.
        /// </param>
        void DrawEllipse(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness = 1.0);

        /// <summary>
        /// Draws the collection of ellipses, where all have the same stroke and fill.
        /// This performs better than calling DrawEllipse multiple times.
        /// </summary>
        /// <param name="rectangles">
        /// The rectangles.
        /// </param>
        /// <param name="fill">
        /// The fill color.
        /// </param>
        /// <param name="stroke">
        /// The stroke color.
        /// </param>
        /// <param name="thickness">
        /// The stroke thickness.
        /// </param>
        void DrawEllipses(IList<OxyRect> rectangles, OxyColor fill, OxyColor stroke, double thickness = 1.0);

        /// <summary>
        /// Draws the polyline from the specified points.
        /// </summary>
        /// <param name="points">
        /// The points.
        /// </param>
        /// <param name="stroke">
        /// The stroke color.
        /// </param>
        /// <param name="thickness">
        /// The stroke thickness.
        /// </param>
        /// <param name="dashArray">
        /// The dash array.
        /// </param>
        /// <param name="lineJoin">
        /// The line join type.
        /// </param>
        /// <param name="aliased">
        /// if set to <c>true</c> the shape will be aliased.
        /// </param>
        void DrawLine(
            IList<ScreenPoint> points,
            OxyColor stroke,
            double thickness = 1.0,
            double[] dashArray = null,
            OxyPenLineJoin lineJoin = OxyPenLineJoin.Miter,
            bool aliased = false);

        /// <summary>
        /// Draws the multiple line segments defined by points (0,1) (2,3) (4,5) etc.
        /// This should have better performance than calling DrawLine for each segment.
        /// </summary>
        /// <param name="points">
        /// The points.
        /// </param>
        /// <param name="stroke">
        /// The stroke color.
        /// </param>
        /// <param name="thickness">
        /// The stroke thickness.
        /// </param>
        /// <param name="dashArray">
        /// The dash array.
        /// </param>
        /// <param name="lineJoin">
        /// The line join type.
        /// </param>
        /// <param name="aliased">
        /// if set to <c>true</c> the shape will be aliased.
        /// </param>
        void DrawLineSegments(
            IList<ScreenPoint> points,
            OxyColor stroke,
            double thickness = 1.0,
            double[] dashArray = null,
            OxyPenLineJoin lineJoin = OxyPenLineJoin.Miter,
            bool aliased = false);

        /// <summary>
        /// Draws the polygon from the specified points. The polygon can have stroke and/or fill.
        /// </summary>
        /// <param name="points">
        /// The points.
        /// </param>
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
        /// The dash array.
        /// </param>
        /// <param name="lineJoin">
        /// The line join type.
        /// </param>
        /// <param name="aliased">
        /// if set to <c>true</c> the shape will be aliased.
        /// </param>
        void DrawPolygon(
            IList<ScreenPoint> points,
            OxyColor fill,
            OxyColor stroke,
            double thickness = 1.0,
            double[] dashArray = null,
            OxyPenLineJoin lineJoin = OxyPenLineJoin.Miter,
            bool aliased = false);

        /// <summary>
        /// Draws a collection of polygons, where all polygons have the same stroke and fill.
        /// This performs better than calling DrawPolygon multiple times.
        /// </summary>
        /// <param name="polygons">
        /// The polygons.
        /// </param>
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
        /// The dash array.
        /// </param>
        /// <param name="lineJoin">
        /// The line join type.
        /// </param>
        /// <param name="aliased">
        /// if set to <c>true</c> the shape will be aliased.
        /// </param>
        void DrawPolygons(
            IList<IList<ScreenPoint>> polygons,
            OxyColor fill,
            OxyColor stroke,
            double thickness = 1.0,
            double[] dashArray = null,
            OxyPenLineJoin lineJoin = OxyPenLineJoin.Miter,
            bool aliased = false);

        /// <summary>
        /// Draws the rectangle.
        /// </summary>
        /// <param name="rect">
        /// The rectangle.
        /// </param>
        /// <param name="fill">
        /// The fill color.
        /// </param>
        /// <param name="stroke">
        /// The stroke color.
        /// </param>
        /// <param name="thickness">
        /// The stroke thickness.
        /// </param>
        void DrawRectangle(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness = 1.0);

        /// <summary>
        /// Draws a collection of rectangles, where all have the same stroke and fill.
        /// This performs better than calling DrawRectangle multiple times.
        /// </summary>
        /// <param name="rectangles">
        /// The rectangles.
        /// </param>
        /// <param name="fill">
        /// The fill color.
        /// </param>
        /// <param name="stroke">
        /// The stroke color.
        /// </param>
        /// <param name="thickness">
        /// The stroke thickness.
        /// </param>
        void DrawRectangles(IList<OxyRect> rectangles, OxyColor fill, OxyColor stroke, double thickness = 1.0);

        /// <summary>
        /// Draws the text.
        /// </summary>
        /// <param name="p">
        /// The position.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="fill">
        /// The fill color.
        /// </param>
        /// <param name="fontFamily">
        /// The font family.
        /// </param>
        /// <param name="fontSize">
        /// Size of the font.
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
        /// <param name="maxSize">
        /// The maximum size of the text.
        /// </param>
        void DrawText(
            ScreenPoint p,
            string text,
            OxyColor fill,
            string fontFamily = null,
            double fontSize = 10,
            double fontWeight = 500,
            double rotate = 0,
            HorizontalAlignment halign = HorizontalAlignment.Left,
            VerticalAlignment valign = VerticalAlignment.Top,
            OxySize? maxSize = null);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="fontFamily">
        /// The font family.
        /// </param>
        /// <param name="fontSize">
        /// Size of the font.
        /// </param>
        /// <param name="fontWeight">
        /// The font weight.
        /// </param>
        /// <returns>
        /// The text size.
        /// </returns>
        OxySize MeasureText(string text, string fontFamily = null, double fontSize = 10, double fontWeight = 500);

        /// <summary>
        /// Sets the tool tip for the following items.
        /// </summary>
        /// <params>
        /// This is only used in the plot controls.
        /// </params>
        /// <param name="text">
        /// The text in the tooltip.
        /// </param>
        void SetToolTip(string text);

        /// <summary>
        /// Cleans up resources not in use.
        /// </summary>
        /// <remarks>
        /// This method is called at the end of each rendering.
        /// </remarks>
        void CleanUp();

        /// <summary>
        /// Gets the size of the specified image.
        /// </summary>
        /// <param name="source">The image source.</param>
        /// <returns>The image info.</returns>
        OxyImageInfo GetImageInfo(OxyImage source);

        /// <summary>
        /// Draws the specified portion of the specified <see cref="OxyImage"/> at the specified location and with the specified size.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="srcX">The x-coordinate of the upper-left corner of the portion of the source image to draw.</param>
        /// <param name="srcY">The y-coordinate of the upper-left corner of the portion of the source image to draw.</param>
        /// <param name="srcWidth">Width of the portion of the source image to draw.</param>
        /// <param name="srcHeight">Height of the portion of the source image to draw.</param>
        /// <param name="destX">The x-coordinate of the upper-left corner of drawn image.</param>
        /// <param name="destY">The y-coordinate of the upper-left corner of drawn image.</param>
        /// <param name="destWidth">The width of the drawn image.</param>
        /// <param name="destHeight">The height of the drawn image.</param>
        /// <param name="opacity">The opacity.</param>
        /// <param name="interpolate">interpolate if set to <c>true</c>.</param>
        void DrawImage(OxyImage source, uint srcX, uint srcY, uint srcWidth, uint srcHeight, double destX, double destY, double destWidth, double destHeight, double opacity, bool interpolate);

        /// <summary>
        /// Sets the clip rectangle.
        /// </summary>
        /// <param name="rect">The clip rectangle.</param>
        /// <returns>True if the clip rectangle was set.</returns>
        bool SetClip(OxyRect rect);

        /// <summary>
        /// Resets the clip rectangle.
        /// </summary>
        void ResetClip();
    }

    /// <summary>
    /// Provides information about the size of an image.
    /// </summary>
    public class OxyImageInfo
    {
        /// <summary>
        /// Gets or sets the width in pixels.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        public uint Width { get; set; }

        /// <summary>
        /// Gets or sets the height in pixels.
        /// </summary>
        /// <value>
        /// The height.
        /// </value>
        public uint Height { get; set; }

        /// <summary>
        /// Gets or sets the horizontal resolution in dpi.
        /// </summary>
        /// <value>
        /// The dpi X.
        /// </value>
        public double DpiX { get; set; }

        /// <summary>
        /// Gets or sets the vertical resolution in dpi.
        /// </summary>
        /// <value>
        /// The dpi Y.
        /// </value>
        public double DpiY { get; set; }
    }
}