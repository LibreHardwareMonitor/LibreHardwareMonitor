using System.Drawing;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI.Themes
{
    public class ThemedHScrollIndicator : Control
    {
        private readonly HScrollBar _scrollbar;
        private int _startValue = 0;
        private int _startPos = 0;
        private bool _isScrolling = false;

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
            this.MouseDown += OnMouseDown;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (_isScrolling)
                return;

            _isScrolling = true;

            //note: this.Capture is true when the control is clicked, no need to handle this

            _startPos = e.X;
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

            double scaleToPercent = totalRange / Bounds.Width;
            double scrollValue = _startValue + (e.X - _startPos) * scaleToPercent;

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
