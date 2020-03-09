using OxyPlot;

namespace LibreHardwareMonitor.UI
{
    class ScaledPlotModel : PlotModel
    {
        public ScaledPlotModel(double dpiXscale, double dpiYscale) : base()
        {
            PlotMargins = new OxyThickness(PlotMargins.Left * dpiXscale,
                                                PlotMargins.Top * dpiYscale,
                                                PlotMargins.Right * dpiXscale,
                                                PlotMargins.Bottom * dpiYscale
                                               );
            Padding = new OxyThickness(Padding.Left * dpiXscale,
                                            Padding.Top * dpiYscale,
                                            Padding.Right * dpiXscale,
                                            Padding.Bottom * dpiYscale
                                           );
            TitlePadding *= dpiXscale;
            LegendSymbolLength *= dpiXscale;
            LegendSymbolMargin *= dpiXscale;
            LegendPadding *= dpiXscale;
            LegendColumnSpacing *= dpiXscale;
            LegendItemSpacing *= dpiXscale;
            LegendMargin *= dpiXscale;
        }
    }
}
