// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RenderContextBase.cs" company="OxyPlot">
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
//   The abstract render context base class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides an abstract base class for rendering contexts.
    /// </summary>
    public abstract class RenderContextBase : IRenderContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderContextBase" /> class.
        /// </summary>
        protected RenderContextBase()
        {
            this.RendersToScreen = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the context renders to screen.
        /// </summary>
        /// <value>
        /// <c>true</c> if the context renders to screen; otherwise, <c>false</c>.
        /// </value>
        public bool RendersToScreen { get; set; }

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
        public abstract void DrawEllipse(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness);

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
        public virtual void DrawEllipses(IList<OxyRect> rectangles, OxyColor fill, OxyColor stroke, double thickness)
        {
            foreach (var r in rectangles)
            {
                this.DrawEllipse(r, fill, stroke, thickness);
            }
        }

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
        public abstract void DrawLine(
            IList<ScreenPoint> points,
            OxyColor stroke,
            double thickness,
            double[] dashArray,
            OxyPenLineJoin lineJoin,
            bool aliased);

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
        public virtual void DrawLineSegments(
            IList<ScreenPoint> points,
            OxyColor stroke,
            double thickness,
            double[] dashArray,
            OxyPenLineJoin lineJoin,
            bool aliased)
        {
            for (int i = 0; i + 1 < points.Count; i += 2)
            {
                this.DrawLine(new[] { points[i], points[i + 1] }, stroke, thickness, dashArray, lineJoin, aliased);
            }
        }

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
        public abstract void DrawPolygon(
            IList<ScreenPoint> points,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            double[] dashArray,
            OxyPenLineJoin lineJoin,
            bool aliased);

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
        public virtual void DrawPolygons(
            IList<IList<ScreenPoint>> polygons,
            OxyColor fill,
            OxyColor stroke,
            double thickness,
            double[] dashArray,
            OxyPenLineJoin lineJoin,
            bool aliased)
        {
            foreach (var polygon in polygons)
            {
                this.DrawPolygon(polygon, fill, stroke, thickness, dashArray, lineJoin, aliased);
            }
        }

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
        public abstract void DrawRectangle(OxyRect rect, OxyColor fill, OxyColor stroke, double thickness);

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
        public virtual void DrawRectangles(IList<OxyRect> rectangles, OxyColor fill, OxyColor stroke, double thickness)
        {
            foreach (var r in rectangles)
            {
                this.DrawRectangle(r, fill, stroke, thickness);
            }
        }

        /// <summary>
        /// Draws the text.
        /// </summary>
        /// <param name="p">
        /// The p.
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
        public abstract void DrawText(
            ScreenPoint p,
            string text,
            OxyColor fill,
            string fontFamily,
            double fontSize,
            double fontWeight,
            double rotate,
            HorizontalAlignment halign,
            VerticalAlignment valign,
            OxySize? maxSize);

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
        public abstract OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight);

        /// <summary>
        /// Sets the tool tip for the following items.
        /// </summary>
        /// <param name="text">
        /// The text in the tooltip.
        /// </param>
        /// <params>
        /// This is only used in the plot controls.
        /// </params>
        public virtual void SetToolTip(string text)
        {
        }

        /// <summary>
        /// Cleans up resources not in use.
        /// </summary>
        /// <remarks>
        /// This method is called at the end of each rendering.
        /// </remarks>
        public virtual void CleanUp()
        {
        }

        /// <summary>
        /// Gets the size of the specified image.
        /// </summary>
        /// <param name="source">The image source.</param>
        /// <returns>
        /// The image info.
        /// </returns>
        public virtual OxyImageInfo GetImageInfo(OxyImage source)
        {
            return null;
        }

        /// <summary>
        /// Draws the image.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="srcX">The SRC X.</param>
        /// <param name="srcY">The SRC Y.</param>
        /// <param name="srcWidth">Width of the SRC.</param>
        /// <param name="srcHeight">Height of the SRC.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="w">The w.</param>
        /// <param name="h">The h.</param>
        /// <param name="opacity">The opacity.</param>
        /// <param name="interpolate">interpolate if set to <c>true</c>.</param>
        public virtual void DrawImage(
            OxyImage source,
            uint srcX,
            uint srcY,
            uint srcWidth,
            uint srcHeight,
            double x,
            double y,
            double w,
            double h,
            double opacity,
            bool interpolate)
        {
        }

        /// <summary>
        /// Sets the clip rectangle.
        /// </summary>
        /// <param name="rect">The clip rectangle.</param>
        /// <returns>
        /// True if the clip rectangle was set.
        /// </returns>
        public virtual bool SetClip(OxyRect rect)
        {
            return false;
        }

        /// <summary>
        /// Resets the clip rectangle.
        /// </summary>
        public virtual void ResetClip()
        {
        }
    }
}