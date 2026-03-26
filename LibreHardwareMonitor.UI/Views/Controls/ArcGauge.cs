using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LibreHardwareMonitor.UI.Views.Controls;

public class ArcGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ArcGauge, double>(nameof(Value));

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ArcGauge, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ArcGauge, double>(nameof(Maximum), 100);

    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<ArcGauge, double>(nameof(StrokeWidth), 8);

    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<ArcGauge, IBrush?>(nameof(TrackBrush));

    public static readonly StyledProperty<IBrush?> ValueBrushProperty =
        AvaloniaProperty.Register<ArcGauge, IBrush?>(nameof(ValueBrush));

    private Pen? _cachedTrackPen;
    private Pen? _cachedValuePen;
    private IBrush? _lastTrackBrush;
    private IBrush? _lastValueBrush;
    private double _lastStrokeWidth;

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public IBrush? ValueBrush
    {
        get => GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    static ArcGauge()
    {
        AffectsRender<ArcGauge>(ValueProperty, MinimumProperty, MaximumProperty,
            StrokeWidthProperty, TrackBrushProperty, ValueBrushProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0) return;

        double radius = (size - StrokeWidth) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        double startAngle = 135;
        double sweepAngle = 270;

        // Cache pens — only recreate when brush or stroke width changes
        IBrush trackBrush = TrackBrush ?? Brushes.Gray;
        IBrush valueBrush = ValueBrush ?? Brushes.DodgerBlue;
        double strokeWidth = StrokeWidth;

        if (_cachedTrackPen is null || !ReferenceEquals(_lastTrackBrush, trackBrush) || _lastStrokeWidth != strokeWidth)
        {
            _cachedTrackPen = new Pen(trackBrush, strokeWidth, lineCap: PenLineCap.Round);
            _lastTrackBrush = trackBrush;
        }

        if (_cachedValuePen is null || !ReferenceEquals(_lastValueBrush, valueBrush) || _lastStrokeWidth != strokeWidth)
        {
            _cachedValuePen = new Pen(valueBrush, strokeWidth, lineCap: PenLineCap.Round);
            _lastValueBrush = valueBrush;
        }

        _lastStrokeWidth = strokeWidth;

        // Draw track arc
        DrawArc(context, center, radius, startAngle, sweepAngle, _cachedTrackPen);

        // Draw value arc
        double range = Maximum - Minimum;
        double normalizedValue = range > 0 ? Math.Clamp((Value - Minimum) / range, 0, 1) : 0;
        double valueSweep = normalizedValue * sweepAngle;

        if (valueSweep > 0.5)
        {
            DrawArc(context, center, radius, startAngle, valueSweep, _cachedValuePen);
        }
    }

    private static void DrawArc(DrawingContext context, Point center, double radius,
        double startAngleDeg, double sweepAngleDeg, Pen pen)
    {
        double startRad = startAngleDeg * Math.PI / 180;
        double endRad = (startAngleDeg + sweepAngleDeg) * Math.PI / 180;

        var startPoint = new Point(
            center.X + radius * Math.Cos(startRad),
            center.Y + radius * Math.Sin(startRad));
        var endPoint = new Point(
            center.X + radius * Math.Cos(endRad),
            center.Y + radius * Math.Sin(endRad));

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(startPoint, false);
            ctx.ArcTo(endPoint, new Size(radius, radius), 0,
                sweepAngleDeg > 180, SweepDirection.Clockwise);
        }

        context.DrawGeometry(null, pen, geometry);
    }
}
