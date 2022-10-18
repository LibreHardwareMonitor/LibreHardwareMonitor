using System.Drawing;
using System.Linq;

namespace LibreHardwareMonitor.UI.Themes
{
    public class CyberTheme : LightTheme
    {
        private readonly Color[] _plotColorPalette;
        public override Color ForegroundColor => ColorTranslator.FromHtml("#ff0055");
        public override Color BackgroundColor => ColorTranslator.FromHtml("#120b10");
        public override Color HyperlinkColor => ColorTranslator.FromHtml("#76C1FF");
        public override Color SelectedForegroundColor => ForegroundColor;
        public override Color SelectedBackgroundColor => Color.FromArgb(45, 45, 45);
        public override Color LineColor => Color.FromArgb(33, 33, 33);
        public override Color StrongLineColor => Color.FromArgb(53, 53, 53);
        public override Color[] PlotColorPalette => _plotColorPalette;
        public override Color PlotGridMajorColor => Color.FromArgb(93, 93, 93);
        public override Color PlotGridMinorColor => Color.FromArgb(53, 53, 53);
        public override bool WindowTitlebarFallbackToImmersiveDarkMode => true;

        public CyberTheme() : base("cyber", "Cyber")
        {
            string[] colors = {
                "#ff0055",
                "#EDF37E",
                "#6766b3",
                "#76C1FF",
                "#d57bff",
                "#EEFFFF",
                "#00FF9C",
                "#fffc58",
                "#009550",
                "#ff3270",
                "#FF4081",
                "#00FFC8"
            };

            _plotColorPalette = colors.Select(color => ColorTranslator.FromHtml(color)).ToArray();
        }
    }
}
