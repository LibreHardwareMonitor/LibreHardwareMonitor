using System.Drawing;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI.Themes
{
    public class ThemedVScrollIndicator : Control
    {
        private readonly VScrollBar _scrollbar;
        private int _startValue = 0;
        private int _startPos = 0;
        private bool _isScrolling = false;

        public static void AddToControl(Control control)
        {
            foreach (Control child in control.Controls)
            {
                if (child is VScrollBar scrollbar)
                {
                    control.Controls.Add(new ThemedVScrollIndicator(scrollbar));
                    return;
                }
            }
        }

        public ThemedVScrollIndicator(VScrollBar scrollBar)
        {
            _scrollbar = scrollBar;

            Width = 8;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            Left = scrollBar.Parent.Width - Width;
            Top = 0;
            Size = new Size(Width, scrollBar.Parent.Height);
            Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            Visible = scrollBar.Visible;

            scrollBar.VisibleChanged += (s, e) => Visible = (s as ScrollBar).Visible;
            scrollBar.Scroll += (s, e) => Invalidate();
            scrollBar.ValueChanged += (s, e) => Invalidate();

            scrollBar.Width = 0;
            this.MouseDown += OnMouseDown;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (_isScrolling)
                return;

            _isScrolling = true;

            //note: this.Capture is true when the control is clicked, no need to handle this

            _startPos = e.Y;
            _startValue = _scrollbar.Value;

            this.MouseUp += OnMouseUp;
            this.MouseMove += OnMouseMove;
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _isScrolling = false;
            this.MouseUp -= OnMouseUp;
            this.MouseMove -= OnMouseMove;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isScrolling)
                return;

            //pixel to range scaling:
            double totalRange = _scrollbar.Maximum - _scrollbar.Minimum;

            if (totalRange <= 0)
                return;

            double scaleToPercent = totalRange / Bounds.Height;
            double scrollValue = _startValue + (e.Y - _startPos) * scaleToPercent;

            if (scrollValue < _scrollbar.Minimum)
                scrollValue = _scrollbar.Minimum;

            if (scrollValue > (_scrollbar.Maximum - _scrollbar.LargeChange))
                scrollValue = _scrollbar.Maximum - _scrollbar.LargeChange;

            _scrollbar.Value = (int)scrollValue;
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            using (SolidBrush brush = new SolidBrush(Theme.Current.ScrollbarBackground))
                g.FillRectangle(brush, new Rectangle(0, 0, Bounds.Width, Bounds.Height));

            int height = Bounds.Height;
            int range = _scrollbar.Maximum - _scrollbar.Minimum;

            if (range > 0)
            {
                int start = height * (_scrollbar.Value - _scrollbar.Minimum) / range;
                int end = height * (_scrollbar.Value - _scrollbar.Minimum + _scrollbar.LargeChange) / range;
                using (SolidBrush brush = new SolidBrush(Theme.Current.ScrollbarTrack))
                    g.FillRectangle(brush, new Rectangle(2, start, Bounds.Width - 4, end - start));
            }
        }
    }
}
