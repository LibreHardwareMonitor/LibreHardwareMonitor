// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SliderAction.cs" company="OxyPlot">
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
namespace OxyPlot
{
    // todo: use screen coordinates instead of original points (problem on log axes)
    public class SliderAction : MouseAction
    {
        public SliderAction(IPlot pc)
            : base(pc)
        {
        }

        private DataSeries currentSeries;

        public override void OnMouseDown(ScreenPoint pt, OxyMouseButton button, int clickCount, bool control, bool shift)
        {
            base.OnMouseDown(pt, button, clickCount, control, shift);

            if (button != OxyMouseButton.Left)
                return;

            // Middle button double click adds an annotation
            if (clickCount == 2)
            {
                // pc.Annotations.
                pc.Refresh();
            }

            currentSeries = pc.GetSeriesFromPoint(pt) as DataSeries;

            OnMouseMove(pt, control, shift);

            //pc.CaptureMouse();
            // pc.Cursor = Cursors.Cross;
        }

        public override void OnMouseMove(ScreenPoint pt, bool control, bool shift)
        {
            if (currentSeries == null)
                return;

            var current = GetNearestPoint(currentSeries, pt, !control, shift);
            if (current != null)
                pc.ShowSlider(currentSeries, current.Value);
        }

        private static DataPoint? GetNearestPoint(ISeries s, ScreenPoint point, bool snap, bool pointsOnly)
        {
            if (s == null)
                return null;

            if (snap || pointsOnly)
            {
                ScreenPoint spn;
                DataPoint dpn;
                if (s.GetNearestPoint(point, out dpn, out spn) && snap)
                {
                    if (spn.DistanceTo(point) < 20)
                        return dpn;
                }
            }

            ScreenPoint sp;
            DataPoint dp;

            if (!pointsOnly)
                if (s.GetNearestInterpolatedPoint(point, out dp, out sp))
                    return dp;

            return null;
        }

        public override void OnMouseUp()
        {
            base.OnMouseUp();
            if (currentSeries == null)
                return;
            currentSeries = null;
            pc.HideSlider();
        }
    }
}