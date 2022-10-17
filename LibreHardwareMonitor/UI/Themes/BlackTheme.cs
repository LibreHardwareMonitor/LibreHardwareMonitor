using System.Drawing;

namespace LibreHardwareMonitor.UI.Themes
{
    public class BlackTheme : DarkTheme
    {
        public BlackTheme() : base("black", "Black") { }
        public override Color BackgroundColor => Color.FromArgb(0, 0, 0);
        public override Color LineColor => Color.FromArgb(15, 15, 15);
    }
}
