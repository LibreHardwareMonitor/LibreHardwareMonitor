using System.Drawing;

namespace LibreHardwareMonitor.UI.Themes
{
    public class DarkTheme : Theme
    {
        private readonly Color[] _plotColorPalette = new Color[] {
            Color.FromArgb(27, 170, 153),
            Color.FromArgb(255, 99, 97),
            Color.FromArgb(254, 54, 72),
            Color.FromArgb(139, 181, 57),
            Color.FromArgb(28, 156, 75),
            Color.FromArgb(238, 231, 147),
            Color.FromArgb(19, 130, 209),
            Color.FromArgb(0, 167, 166),
            Color.FromArgb(255, 226, 71),
            Color.FromArgb(253, 100, 55),
            Color.FromArgb(216, 46, 27),
            Color.FromArgb(16, 86, 150)
        };

        public DarkTheme(string id, string displayName) : base(id, displayName) { }
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

        public DarkTheme() : this("dark", "Dark") { }
    }
}
