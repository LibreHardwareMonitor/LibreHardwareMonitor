using System.Drawing;
using System.Linq;

namespace LibreHardwareMonitor.UI.Themes
{
    public class BlackTheme : LightTheme
    {
        private readonly Color[] _plotColorPalette;
        public override Color ForegroundColor => Color.FromArgb(218, 218, 218);
        public override Color BackgroundColor => Color.FromArgb(0, 0, 0);
        public override Color HyperlinkColor => Color.FromArgb(144, 220, 232);
        public override Color SelectedForegroundColor => ForegroundColor;
        public override Color SelectedBackgroundColor => ColorTranslator.FromHtml("#090A17");
        public override Color LineColor => ColorTranslator.FromHtml("#070A12");
        public override Color StrongLineColor => ColorTranslator.FromHtml("#091217");
        public override Color[] PlotColorPalette => _plotColorPalette;
        public override Color PlotGridMajorColor => Color.FromArgb(73, 73, 73);
        public override Color PlotGridMinorColor => Color.FromArgb(33, 33, 33);
        public override bool WindowTitlebarFallbackToImmersiveDarkMode => true;

        public BlackTheme() : base("black", "Black")
        {
            string[] colors = {
                "#FF2525",
                "#1200FF",
                "#00FF5B",
                "#FFE53B",
                "#00FFFF",
                "#FF0A6C",
                "#2D27FF",
                "#FF2CDF",
                "#00E1FD",
                "#0A5057"
            };

            _plotColorPalette = colors.Select(color => ColorTranslator.FromHtml(color)).ToArray();
        }
    }
}
