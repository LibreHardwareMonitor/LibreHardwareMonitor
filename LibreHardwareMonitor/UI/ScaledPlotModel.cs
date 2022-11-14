using OxyPlot;
using OxyPlot.Legends;

namespace LibreHardwareMonitor.UI;

class ScaledPlotModel : PlotModel
{
    public ScaledPlotModel(double dpiXscale, double dpiYscale)
    {
        PlotMargins = new OxyThickness(PlotMargins.Left * dpiXscale,
                                       PlotMargins.Top * dpiYscale,
                                       PlotMargins.Right * dpiXscale,
                                       PlotMargins.Bottom * dpiYscale);

        Padding = new OxyThickness(Padding.Left * dpiXscale,
                                   Padding.Top * dpiYscale,
                                   Padding.Right * dpiXscale,
                                   Padding.Bottom * dpiYscale);

        TitlePadding *= dpiXscale;

        Legend legend = new();

        legend.LegendSymbolLength *= dpiXscale;
        legend.LegendSymbolMargin *= dpiXscale;
        legend.LegendPadding *= dpiXscale;
        legend.LegendColumnSpacing *= dpiXscale;
        legend.LegendItemSpacing *= dpiXscale;
        legend.LegendMargin *= dpiXscale;

        Legends.Add(legend);
    }
}