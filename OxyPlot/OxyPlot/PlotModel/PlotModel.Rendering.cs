// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotModel.Rendering.cs" company="OxyPlot">
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
//   Partial PlotModel class - this file contains rendering methods.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace OxyPlot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;

    public partial class PlotModel
    {
        /// <summary>
        /// Renders the plot with the specified rendering context.
        /// </summary>
        /// <param name="rc">The rendering context.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public void Render(IRenderContext rc, double width, double height)
        {
            lock (this.syncRoot)
            {
                if (width <= 0 || height <= 0)
                {
                    return;
                }
                
                this.Width = width;
                this.Height = height;

                this.ActualPlotMargins = this.PlotMargins;
                this.EnsureLegendProperties();

                while (true)
                {
                    this.UpdatePlotArea(rc);
                    this.UpdateAxisTransforms();
                    this.UpdateIntervals();
                    if (!this.AutoAdjustPlotMargins)
                    {
                        break;
                    }

                    if (!this.AdjustPlotMargins(rc))
                    {
                        break;
                    }
                }

                if (this.PlotType == PlotType.Cartesian)
                {
                    this.EnforceCartesianTransforms();
                    this.UpdateIntervals();
                }

                this.RenderBackgrounds(rc);
                this.RenderAnnotations(rc, AnnotationLayer.BelowAxes);
                this.RenderAxes(rc, AxisLayer.BelowSeries);
                this.RenderAnnotations(rc, AnnotationLayer.BelowSeries);
                this.RenderSeries(rc);
                this.RenderAnnotations(rc, AnnotationLayer.AboveSeries);
                this.RenderTitle(rc);
                this.RenderBox(rc);
                this.RenderAxes(rc, AxisLayer.AboveSeries);

                if (this.IsLegendVisible)
                {
                    this.RenderLegends(rc, this.LegendArea);
                }
            }
        }

        /// <summary>
        /// Calculates the maximum size of the specified axes.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="axesOfPositionTier">
        /// The axes of position tier.
        /// </param>
        /// <returns>
        /// The maximum size.
        /// </returns>
        private static double MaxSizeOfPositionTier(IRenderContext rc, IEnumerable<Axis> axesOfPositionTier)
        {
            double maxSizeOfPositionTier = 0;
            foreach (var axis in axesOfPositionTier)
            {
                OxySize size = axis.Measure(rc);
                if (axis.IsHorizontal())
                {
                    if (size.Height > maxSizeOfPositionTier)
                    {
                        maxSizeOfPositionTier = size.Height;
                    }
                }
                else
                {
                    if (size.Width > maxSizeOfPositionTier)
                    {
                        maxSizeOfPositionTier = size.Width;
                    }
                }
            }

            return maxSizeOfPositionTier;
        }

        /// <summary>
        /// Adjust the plot margins.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <returns>
        /// The adjust plot margins.
        /// </returns>
        private bool AdjustPlotMargins(IRenderContext rc)
        {
            bool isAdjusted = false;
            var newPlotMargins = new Dictionary<AxisPosition, double>
                {
                    { AxisPosition.Left, this.ActualPlotMargins.Left },
                    { AxisPosition.Top, this.ActualPlotMargins.Top },
                    { AxisPosition.Right, this.ActualPlotMargins.Right },
                    { AxisPosition.Bottom, this.ActualPlotMargins.Bottom }
                };

            for (var position = AxisPosition.Left; position <= AxisPosition.Bottom; position++)
            {
                double maxValueOfPositionTier = 0;
                var axesOfPosition = this.Axes.Where(a => a.Position == position).ToList();
                foreach (var positionTier in axesOfPosition.Select(a => a.PositionTier).Distinct().OrderBy(l => l))
                {
                    var axesOfPositionTier = axesOfPosition.Where(a => a.PositionTier == positionTier).ToList();
                    double maxSizeOfPositionTier = MaxSizeOfPositionTier(rc, axesOfPositionTier);
                    double minValueOfPositionTier = maxValueOfPositionTier;

                    if (Math.Abs(maxValueOfPositionTier) > 1e-5)
                    {
                        maxValueOfPositionTier += this.AxisTierDistance;
                    }

                    maxValueOfPositionTier += maxSizeOfPositionTier;

                    foreach (Axis axis in axesOfPositionTier)
                    {
                        axis.PositionTierSize = maxSizeOfPositionTier;
                        axis.PositionTierMinShift = minValueOfPositionTier;
                        axis.PositionTierMaxShift = maxValueOfPositionTier;
                    }
                }

                if (maxValueOfPositionTier > newPlotMargins[position])
                {
                    newPlotMargins[position] = maxValueOfPositionTier;
                    isAdjusted = true;
                }
            }

            if (isAdjusted)
            {
                this.ActualPlotMargins = new OxyThickness(
                    newPlotMargins[AxisPosition.Left],
                    newPlotMargins[AxisPosition.Top],
                    newPlotMargins[AxisPosition.Right],
                    newPlotMargins[AxisPosition.Bottom]);
            }

            return isAdjusted;
        }

        /// <summary>
        /// Measures the size of the title and subtitle.
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        /// <returns>
        /// Size of the titles.
        /// </returns>
        private OxySize MeasureTitles(IRenderContext rc)
        {
            OxySize size1 = rc.MeasureText(this.Title, this.ActualTitleFont, this.TitleFontSize, this.TitleFontWeight);
            OxySize size2 = rc.MeasureText(
                this.Subtitle, this.SubtitleFont ?? this.ActualSubtitleFont, this.SubtitleFontSize, this.SubtitleFontWeight);
            double height = size1.Height + size2.Height;
            double width = Math.Max(size1.Width, size2.Width);
            return new OxySize(width, height);
        }

        /// <summary>
        /// Renders the annotations.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="layer">
        /// The layer.
        /// </param>
        private void RenderAnnotations(IRenderContext rc, AnnotationLayer layer)
        {
            foreach (var a in this.Annotations.Where(a => a.Layer == layer))
            {
                a.Render(rc, this);
            }
        }

        /// <summary>
        /// Renders the axes.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        /// <param name="layer">
        /// The layer.
        /// </param>
        private void RenderAxes(IRenderContext rc, AxisLayer layer)
        {
            for (int i = 0; i < 2; i++)
            {
                foreach (var a in this.Axes)
                {
                    if (a.IsAxisVisible && a.Layer == layer)
                    {
                        a.Render(rc, this, layer, i);
                    }
                }
            }
        }

        /// <summary>
        /// Renders the series backgrounds.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        private void RenderBackgrounds(IRenderContext rc)
        {
            // Render the main background of the plot area (only if there are axes)
            // The border is rendered by DrawRectangleAsPolygon to ensure that it is pixel aligned with the tick marks.
            if (this.Axes.Count > 0 && this.PlotAreaBackground != null)
            {
                rc.DrawRectangleAsPolygon(this.PlotArea, this.PlotAreaBackground, null, 0);
            }

            foreach (var s in this.VisibleSeries)
            {
                var s2 = s as XYAxisSeries;
                if (s2 == null || s2.Background == null)
                {
                    continue;
                }

                rc.DrawRectangle(s2.GetScreenRectangle(), s2.Background, null, 0);
            }
        }

        /// <summary>
        /// Renders the border around the plot area.
        /// </summary>
        /// <remarks>
        /// The border will only by rendered if there are axes in the plot.
        /// </remarks>
        /// <param name="rc">
        /// The render context.
        /// </param>
        private void RenderBox(IRenderContext rc)
        {
            // The border is rendered by DrawBox to ensure that it is pixel aligned with the tick marks (cannot use DrawRectangle here).
            if (this.Axes.Count > 0)
            {
                rc.DrawRectangleAsPolygon(this.PlotArea, null, this.PlotAreaBorderColor, this.PlotAreaBorderThickness);

                foreach (var axis in this.Axes) 
                {
                    if (!axis.IsAxisVisible)
                        continue;

                    if (axis.IsHorizontal()) 
                    {
                        var start = this.PlotArea.Left + 
                            this.PlotArea.Width * axis.StartPosition;
                        if (axis.StartPosition < 1 && axis.StartPosition > 0)
                            rc.DrawLine(new[] {
                                new ScreenPoint(start, this.PlotArea.Top),
                                new ScreenPoint(start, this.PlotArea.Bottom) },
                                    this.PlotAreaBorderColor, this.PlotAreaBorderThickness,
                                null, OxyPenLineJoin.Miter, true);

                        var end = this.PlotArea.Left +
                            this.PlotArea.Width * axis.EndPosition;
                        if (axis.EndPosition < 1 && axis.EndPosition > 0)
                            rc.DrawLine(new[] {
                                new ScreenPoint(end, this.PlotArea.Top),
                                new ScreenPoint(end, this.PlotArea.Bottom) },
                                    this.PlotAreaBorderColor, this.PlotAreaBorderThickness,
                                null, OxyPenLineJoin.Miter, true);
                    } 
                    else 
                    {
                        var start = this.PlotArea.Bottom - 
                            this.PlotArea.Height * axis.StartPosition;
                        if (axis.StartPosition < 1 && axis.StartPosition > 0)
                            rc.DrawLine(new[] { 
                                new ScreenPoint(this.PlotArea.Left, start),
                                new ScreenPoint(this.PlotArea.Right, start) },
                                    this.PlotAreaBorderColor, this.PlotAreaBorderThickness,
                                null, OxyPenLineJoin.Miter, true);

                        var end = this.PlotArea.Bottom -
                            this.PlotArea.Height * axis.EndPosition;
                        if (axis.EndPosition < 1 && axis.EndPosition > 0)
                            rc.DrawLine(new[] { 
                                new ScreenPoint(this.PlotArea.Left, end),
                                new ScreenPoint(this.PlotArea.Right, end) },
                                    this.PlotAreaBorderColor, this.PlotAreaBorderThickness,
                                null, OxyPenLineJoin.Miter, true);
                    }
                }

            }
        }

        /// <summary>
        /// Renders the series.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        private void RenderSeries(IRenderContext rc)
        {
            // Update undefined colors
            this.ResetDefaultColor();
            foreach (var s in this.VisibleSeries)
            {
                s.SetDefaultValues(this);
            }

            foreach (var s in this.VisibleSeries)
            {
                s.Render(rc, this);
            }
        }

        /// <summary>
        /// Renders the title and subtitle.
        /// </summary>
        /// <param name="rc">
        /// The render context.
        /// </param>
        private void RenderTitle(IRenderContext rc)
        {
            OxySize size1 = rc.MeasureText(this.Title, this.ActualTitleFont, this.TitleFontSize, this.TitleFontWeight);
            rc.MeasureText(
                this.Subtitle, this.SubtitleFont ?? this.ActualSubtitleFont, this.SubtitleFontSize, this.SubtitleFontWeight);

            // double height = size1.Height + size2.Height;
            // double dy = (TitleArea.Top+TitleArea.Bottom-height)*0.5;
            double dy = this.TitleArea.Top;
            double dx = (this.TitleArea.Left + this.TitleArea.Right) * 0.5;

            if (!string.IsNullOrEmpty(this.Title))
            {
                rc.DrawMathText(
                    new ScreenPoint(dx, dy),
                    this.Title,
                    this.TitleColor ?? this.TextColor,
                    this.ActualTitleFont,
                    this.TitleFontSize,
                    this.TitleFontWeight,
                    0,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Top);
                dy += size1.Height;
            }

            if (!string.IsNullOrEmpty(this.Subtitle))
            {
                rc.DrawMathText(
                    new ScreenPoint(dx, dy),
                    this.Subtitle,
                    this.SubtitleColor ?? this.TextColor,
                    this.ActualSubtitleFont,
                    this.SubtitleFontSize,
                    this.SubtitleFontWeight,
                    0,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Top);
            }
        }

        /// <summary>
        /// Calculates the plot area (subtract padding, title size and outside legends)
        /// </summary>
        /// <param name="rc">
        /// The rendering context.
        /// </param>
        private void UpdatePlotArea(IRenderContext rc)
        {
            var plotArea = new OxyRect(
                this.Padding.Left,
                this.Padding.Top,
                this.Width - this.Padding.Left - this.Padding.Right,
                this.Height - this.Padding.Top - this.Padding.Bottom);

            var titleSize = this.MeasureTitles(rc);

            if (titleSize.Height > 0)
            {
                double titleHeight = titleSize.Height + this.TitlePadding;
                plotArea.Height -= titleHeight;
                plotArea.Top += titleHeight;
            }

            plotArea.Top += this.ActualPlotMargins.Top;
            plotArea.Height -= this.ActualPlotMargins.Top;

            plotArea.Height -= this.ActualPlotMargins.Bottom;

            plotArea.Left += this.ActualPlotMargins.Left;
            plotArea.Width -= this.ActualPlotMargins.Left;

            plotArea.Width -= this.ActualPlotMargins.Right;

            // Find the available size for the legend box
            double availableLegendWidth = plotArea.Width;
            double availableLegendHeight = plotArea.Height;
            if (this.LegendPlacement == LegendPlacement.Inside)
            {
                availableLegendWidth -= this.LegendMargin * 2;
                availableLegendHeight -= this.LegendMargin * 2;
            }

            if (availableLegendWidth < 0)
            {
                availableLegendWidth = 0;
            }

            if (availableLegendHeight < 0)
            {
                availableLegendHeight = 0;
            }

            // Calculate the size of the legend box
            var legendSize = this.MeasureLegends(rc, new OxySize(availableLegendWidth, availableLegendHeight));

            // Adjust the plot area after the size of the legend box has been calculated
            if (this.IsLegendVisible && this.LegendPlacement == LegendPlacement.Outside)
            {
                switch (this.LegendPosition)
                {
                    case LegendPosition.LeftTop:
                    case LegendPosition.LeftMiddle:
                    case LegendPosition.LeftBottom:
                        plotArea.Left += legendSize.Width + this.LegendMargin;
                        plotArea.Width -= legendSize.Width + this.LegendMargin;
                        break;
                    case LegendPosition.RightTop:
                    case LegendPosition.RightMiddle:
                    case LegendPosition.RightBottom:
                        plotArea.Width -= legendSize.Width + this.LegendMargin;
                        break;
                    case LegendPosition.TopLeft:
                    case LegendPosition.TopCenter:
                    case LegendPosition.TopRight:
                        plotArea.Top += legendSize.Height + this.LegendMargin;
                        plotArea.Height -= legendSize.Height + this.LegendMargin;
                        break;
                    case LegendPosition.BottomLeft:
                    case LegendPosition.BottomCenter:
                    case LegendPosition.BottomRight:
                        plotArea.Height -= legendSize.Height + this.LegendMargin;
                        break;
                }
            }

            // Ensure the plot area is valid
            if (plotArea.Height < 0)
            {
                plotArea.Bottom = plotArea.Top + 1;
            }

            if (plotArea.Width < 0)
            {
                plotArea.Right = plotArea.Left + 1;
            }

            this.PlotArea = plotArea;
            this.PlotAndAxisArea = new OxyRect(
                plotArea.Left - this.ActualPlotMargins.Left,
                plotArea.Top - this.ActualPlotMargins.Top,
                plotArea.Width + this.ActualPlotMargins.Left + this.ActualPlotMargins.Right,
                plotArea.Height + this.ActualPlotMargins.Top + this.ActualPlotMargins.Bottom);
            this.TitleArea = new OxyRect(this.PlotArea.Left, this.Padding.Top, this.PlotArea.Width, titleSize.Height + (this.TitlePadding * 2));
            this.LegendArea = this.GetLegendRectangle(legendSize);
        }
    }
}