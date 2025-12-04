using System.Drawing;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI.Themes
{
    public class ThemedToolStripRenderer : ToolStripRenderer
    {
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            if (e.Item is not ToolStripSeparator)
            {
                base.OnRenderSeparator(e);
                return;
            }

            Rectangle bounds = new(Point.Empty, e.Item.Size);
            using (Brush brush = new SolidBrush(Theme.Current.MenuBackgroundColor))
                e.Graphics.FillRectangle(brush, bounds);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item.Selected ? Theme.Current.MenuSelectedForegroundColor : Theme.Current.MenuForegroundColor;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            using (Pen pen = new Pen(e.Item.Selected ? Theme.Current.MenuSelectedForegroundColor : Theme.Current.MenuForegroundColor))
            {
                int x = 10;
                int y = 6;
                e.Graphics.DrawLine(pen, x, y + 3, x + 2, y + 5);
                e.Graphics.DrawLine(pen, x + 2, y + 5, x + 6, y + 1);
                e.Graphics.DrawLine(pen, x, y + 4, x + 2, y + 6);
                e.Graphics.DrawLine(pen, x + 2, y + 6, x + 6, y + 2);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Theme.Current.MenuSelectedForegroundColor : Theme.Current.MenuForegroundColor;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip.Parent is not Form)
            {
                Rectangle bounds = new(Point.Empty, new Size(e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
                using (Pen pen = new Pen(Theme.Current.MenuBorderColor))
                    e.Graphics.DrawRectangle(pen, bounds);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            Rectangle bounds = new(Point.Empty, e.ToolStrip.Size);
            using (Brush brush = new SolidBrush(Theme.Current.MenuBackgroundColor))
                e.Graphics.FillRectangle(brush, bounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle bounds = new(Point.Empty, e.Item.Size);

            using (Brush brush = new SolidBrush(e.Item.Selected ? Theme.Current.MenuSelectedBackgroundColor : Theme.Current.MenuBackgroundColor))
                e.Graphics.FillRectangle(brush, bounds);
        }
    }
}
