using System.Drawing;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI.Themes
{
    public class ThemedHScrollIndicator : Control
    {
        private readonly HScrollBar _scrollbar;

        public static void AddToControl(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (child is HScrollBar scrollbar)
                {
                    control.Controls.Add(new ThemedHScrollIndicator(scrollbar));
                    return;
                }
            }
        }

        public ThemedHScrollIndicator(HScrollBar scrollBar)
        {
            _scrollbar = scrollBar;

            Height = 8;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            Left = 0;
            Top = scrollBar.Parent.Height - Height;
            Size = new Size(scrollBar.Parent.Width, Height);
            Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom;
            Visible = scrollBar.Visible;

            scrollBar.VisibleChanged += (s, e) => Visible = (s as ScrollBar).Visible;
            scrollBar.Scroll += (s, e) => Invalidate();
            scrollBar.ValueChanged += (s, e) => Invalidate();

            scrollBar.Height = 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            using (SolidBrush brush = new SolidBrush(Theme.Current.ScrollbarBackground))
                g.FillRectangle(brush, new Rectangle(0, 0, Bounds.Width, Bounds.Height));

            int width = Bounds.Width;
            int range = _scrollbar.Maximum - _scrollbar.Minimum;

            if (range > 0)
            {
                int start = width * (_scrollbar.Value - _scrollbar.Minimum) / range;
                int end = width * (_scrollbar.Value - _scrollbar.Minimum + _scrollbar.LargeChange) / range;
                using (SolidBrush brush = new SolidBrush(Theme.Current.ScrollbarTrack))
                    g.FillRectangle(brush, new Rectangle(start, 2, end - start, Bounds.Height - 4));
            }
        }
    }
}
