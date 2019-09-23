// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotRenderer.cs" company="OxyPlot">
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
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace OxyPlot
{
    public class PlotRenderer
    {
        protected readonly PlotModel plot;
        protected readonly IRenderContext rc;

        public PlotRenderer(IRenderContext rc, PlotModel p)
        {
            this.rc = rc;
            plot = p;
        }

        public void RenderTitle(string title, string subtitle)
        {
            OxySize size1 = rc.MeasureText(title, plot.TitleFont, plot.TitleFontSize, plot.TitleFontWeight);
            OxySize size2 = rc.MeasureText(subtitle, plot.TitleFont, plot.TitleFontSize, plot.TitleFontWeight);
            double height = size1.Height + size2.Height;
            double dy = (plot.AxisMargins.Top - height) * 0.5;
            double dx = (plot.Bounds.Left + plot.Bounds.Right) * 0.5;

            if (!String.IsNullOrEmpty(title))
                rc.DrawText(
                    new ScreenPoint(dx, dy), title, plot.TextColor,
                    plot.TitleFont, plot.TitleFontSize, plot.TitleFontWeight,
                    0,
                    HorizontalTextAlign.Center, VerticalTextAlign.Top);
            if (!String.IsNullOrEmpty(subtitle))
                rc.DrawText(new ScreenPoint(dx, dy + size1.Height), subtitle, plot.TextColor,
                            plot.TitleFont, plot.SubtitleFontSize, plot.SubtitleFontWeight, 0,
                            HorizontalTextAlign.Center, VerticalTextAlign.Top);
        }

        public void RenderRect(OxyRect bounds, OxyColor fill, OxyColor borderColor, double borderThickness)
        {
            var border = new[]
                             {
                                 new ScreenPoint(bounds.Left, bounds.Top), new ScreenPoint(bounds.Right, bounds.Top),
                                 new ScreenPoint(bounds.Right, bounds.Bottom), new ScreenPoint(bounds.Left, bounds.Bottom),
                                 new ScreenPoint(bounds.Left, bounds.Top)
                             };

            rc.DrawPolygon(border, fill, borderColor, borderThickness, null, true);
        }

        private static readonly double LEGEND_PADDING = 8;

        public void RenderLegends()
        {
            double maxWidth = 0;
            double maxHeight = 0;
            double totalHeight = 0;

            // Measure
            foreach (var s in plot.Series)
            {
                if (String.IsNullOrEmpty(s.Title))
                    continue;
                var oxySize = rc.MeasureText(s.Title, plot.LegendFont, plot.LegendFontSize);
                if (oxySize.Width > maxWidth) maxWidth = oxySize.Width;
                if (oxySize.Height > maxHeight) maxHeight = oxySize.Height;
                totalHeight += oxySize.Height;
            }

            double lineLength = plot.LegendSymbolLength;

            // Arrange
            double x0 = double.NaN, x1 = double.NaN, y0 = double.NaN;

            //   padding          padding
            //          lineLength
            // y0       -----o----       seriesName
            //          x0               x1

            double sign = 1;
            if (plot.IsLegendOutsidePlotArea)
                sign = -1;

            // Horizontal alignment
            HorizontalTextAlign ha = HorizontalTextAlign.Left;
            switch (plot.LegendPosition)
            {
                case LegendPosition.TopRight:
                case LegendPosition.BottomRight:
                    x0 = plot.Bounds.Right - LEGEND_PADDING * sign;
                    x1 = x0 - lineLength * sign - LEGEND_PADDING * sign;
                    ha = sign == 1 ? HorizontalTextAlign.Right : HorizontalTextAlign.Left;
                    break;
                case LegendPosition.TopLeft:
                case LegendPosition.BottomLeft:
                    x0 = plot.Bounds.Left + LEGEND_PADDING * sign;
                    x1 = x0 + lineLength * sign + LEGEND_PADDING * sign;
                    ha = sign == 1 ? HorizontalTextAlign.Left : HorizontalTextAlign.Right;
                    break;
            }

            // Vertical alignment
            VerticalTextAlign va = VerticalTextAlign.Middle;
            switch (plot.LegendPosition)
            {
                case LegendPosition.TopRight:
                case LegendPosition.TopLeft:
                    y0 = plot.Bounds.Top + LEGEND_PADDING + maxHeight / 2;
                    break;
                case LegendPosition.BottomRight:
                case LegendPosition.BottomLeft:
                    y0 = plot.Bounds.Bottom - maxHeight + LEGEND_PADDING;
                    break;
            }

            foreach (var s in plot.Series)
            {
                if (String.IsNullOrEmpty(s.Title))
                    continue;
                rc.DrawText(new ScreenPoint(x1, y0),
                            s.Title, plot.TextColor,
                            plot.LegendFont, plot.LegendFontSize, 500, 0,
                            ha, va);
                OxyRect rect = new OxyRect(x0 - lineLength, y0 - maxHeight / 2, lineLength, maxHeight);
                if (ha == HorizontalTextAlign.Left)
                    rect = new OxyRect(x0, y0 - maxHeight / 2, lineLength, maxHeight);

                s.RenderLegend(rc, rect);
                if (plot.LegendPosition == LegendPosition.TopLeft || plot.LegendPosition == LegendPosition.TopRight)
                    y0 += maxHeight;
                else
                    y0 -= maxHeight;
            }
        }
    }
}