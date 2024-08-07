using System.Drawing;
using System.Linq;

namespace LibreHardwareMonitor.UI.Themes
{
    public class DarkTheme : LightTheme
    {
        private readonly Color[] _plotColorPalette;
        public override Color ForegroundColor => Color.FromArgb(233, 233, 233);
        public override Color BackgroundColor => Color.FromArgb(30, 30, 30);
        public override Color HyperlinkColor => Color.FromArgb(144, 220, 232);
        public override Color SelectedForegroundColor => ForegroundColor;
        public override Color SelectedBackgroundColor => Color.FromArgb(45, 45, 45);
        public override Color LineColor => Color.FromArgb(38, 38, 38);
        public override Color StrongLineColor => Color.FromArgb(53, 53, 53);
        public override Color[] PlotColorPalette => _plotColorPalette;
        public override Color PlotGridMajorColor => Color.FromArgb(93, 93, 93);
        public override Color PlotGridMinorColor => Color.FromArgb(53, 53, 53);
        public override bool WindowTitlebarFallbackToImmersiveDarkMode => true;

        public DarkTheme() : base("dark", "Dark")
        {
            string[] colors = {
                "#F07178",
                "#82AAFF",
                "#C3E88D",
                "#FFCB6B",
                "#009688",
                "#89DDF3",
                "#FFE082",
                "#7986CB",
                "#C792EA",
                "#FF5370",
                "#73d1c8",
                "#F78C6A"
            };

            _plotColorPalette = colors.Select(color => ColorTranslator.FromHtml(color)).ToArray();
        }
    }
}
