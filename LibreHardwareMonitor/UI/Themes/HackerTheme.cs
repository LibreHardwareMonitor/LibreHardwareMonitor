using System.Drawing;
using System.Linq;

namespace LibreHardwareMonitor.UI.Themes
{
    public class HackerTheme : LightTheme
    {
        private readonly Color[] _plotColorPalette;
        public override Color ForegroundColor => ColorTranslator.FromHtml("#00fd2c");
        public override Color BackgroundColor => ColorTranslator.FromHtml("#000000");
        public override Color HyperlinkColor => ColorTranslator.FromHtml("#74A2A4");
        public override Color SelectedForegroundColor => ForegroundColor;
        public override Color SelectedBackgroundColor => ColorTranslator.FromHtml("#003300");
        public override Color LineColor => ColorTranslator.FromHtml("#003410");
        public override Color StrongLineColor => ColorTranslator.FromHtml("#00441b");
        public override Color[] PlotColorPalette => _plotColorPalette;
        public override Color PlotGridMajorColor => LineColor;
        public override Color PlotGridMinorColor => StrongLineColor;
        public override bool WindowTitlebarFallbackToImmersiveDarkMode => true;

        public HackerTheme() : base("hacker", "Hacker")
        {
            string[] colors = {
                "#fcbba1",
                "#d7f9c0",
                "#fca272",
                "#fb6a4a",
                "#74d476",
                "#41bb5d",
                "#ef3b2c",
                "#cb181d",
                "#007d2c",
                "#a50f15",
                "#00541b",
                "#67000d"
            };

            _plotColorPalette = colors.Select(color => ColorTranslator.FromHtml(color)).ToArray();
        }
    }
}
