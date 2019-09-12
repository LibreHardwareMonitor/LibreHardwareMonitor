// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.Utilities;

namespace OpenHardwareMonitor.GUI
{
    public class SensorNotifyIcon : IDisposable
    {
        private UnitManager unitManager;
        private ISensor sensor;
        private NotifyIconAdv notifyIcon;
        private Bitmap bitmap;
        private Graphics graphics;
        private Color color;
        private Color darkColor;
        private Brush brush;
        private Brush darkBrush;
        private Pen pen;
        private Font font;
        private Font smallFont;

        public SensorNotifyIcon(SystemTray sensorSystemTray, ISensor sensor, bool balloonTip, PersistentSettings settings, UnitManager unitManager)
        {
            this.unitManager = unitManager;
            this.sensor = sensor;
            this.notifyIcon = new NotifyIconAdv();

            Color defaultColor = Color.White;
            if (sensor.SensorType == SensorType.Load ||
                sensor.SensorType == SensorType.Control ||
                sensor.SensorType == SensorType.Level)
            {
                defaultColor = Color.FromArgb(0xff, 0x70, 0x8c, 0xf1);
            }
            Color = settings.GetValue(new Identifier(sensor.Identifier, "traycolor").ToString(), defaultColor);

            this.pen = new Pen(Color.FromArgb(96, Color.Black));
            ContextMenu contextMenu = new ContextMenu();
            MenuItem hideShowItem = new MenuItem("Hide/Show");
            hideShowItem.Click += delegate (object obj, EventArgs args)
            {
                sensorSystemTray.SendHideShowCommand();
            };
            contextMenu.MenuItems.Add(hideShowItem);
            contextMenu.MenuItems.Add(new MenuItem("-"));
            MenuItem removeItem = new MenuItem("Remove Sensor");
            removeItem.Click += delegate (object obj, EventArgs args)
            {
                sensorSystemTray.Remove(this.sensor);
            };
            contextMenu.MenuItems.Add(removeItem);
            MenuItem colorItem = new MenuItem("Change Color...");
            colorItem.Click += delegate (object obj, EventArgs args)
            {
                ColorDialog dialog = new ColorDialog();
                dialog.Color = Color;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Color = dialog.Color;
                    settings.SetValue(new Identifier(sensor.Identifier,
                      "traycolor").ToString(), Color);
                }
            };
            contextMenu.MenuItems.Add(colorItem);
            contextMenu.MenuItems.Add(new MenuItem("-"));
            MenuItem exitItem = new MenuItem("Exit");
            exitItem.Click += delegate (object obj, EventArgs args)
            {
                sensorSystemTray.SendExitCommand();
            };
            contextMenu.MenuItems.Add(exitItem);
            this.notifyIcon.ContextMenu = contextMenu;
            this.notifyIcon.DoubleClick += delegate (object obj, EventArgs args)
            {
                sensorSystemTray.SendHideShowCommand();
            };

            // get the default dpi to create an icon with the correct size
            float dpiX, dpiY;
            using (Bitmap b = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
            {
                dpiX = b.HorizontalResolution;
                dpiY = b.VerticalResolution;
            }

            // adjust the size of the icon to current dpi (default is 16x16 at 96 dpi)
            int width = (int)Math.Round(16 * dpiX / 96);
            int height = (int)Math.Round(16 * dpiY / 96);

            // make sure it does never get smaller than 16x16
            width = width < 16 ? 16 : width;
            height = height < 16 ? 16 : height;

            // adjust the font size to the icon size
            FontFamily family = SystemFonts.MessageBoxFont.FontFamily;
            float baseSize;
            switch (family.Name)
            {
                case "Segoe UI": baseSize = 12; break;
                case "Tahoma": baseSize = 11; break;
                default: baseSize = 12; break;
            }

            this.font = new Font(family, baseSize * width / 16.0f, GraphicsUnit.Pixel);
            this.smallFont = new Font(family, 0.75f * baseSize * width / 16.0f, GraphicsUnit.Pixel);

            this.bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            this.graphics = Graphics.FromImage(this.bitmap);
            if (Environment.OSVersion.Version.Major > 5)
            {
                this.graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                this.graphics.SmoothingMode = SmoothingMode.HighQuality;
            }
        }

        public ISensor Sensor
        {
            get { return sensor; }
        }

        public Color Color
        {
            get { return color; }
            set
            {
                this.color = value;
                this.darkColor = Color.FromArgb(255, this.color.R / 3, this.color.G / 3, this.color.B / 3);
                Brush brush = this.brush;
                this.brush = new SolidBrush(this.color);
                if (brush != null)
                    brush.Dispose();
                Brush darkBrush = this.darkBrush;
                this.darkBrush = new SolidBrush(this.darkColor);
                if (darkBrush != null)
                    darkBrush.Dispose();
            }
        }

        public void Dispose()
        {
            Icon icon = notifyIcon.Icon;
            notifyIcon.Icon = null;
            if (icon != null)
                icon.Dispose();
            notifyIcon.Dispose();

            if (brush != null)
                brush.Dispose();
            if (darkBrush != null)
                darkBrush.Dispose();
            pen.Dispose();
            graphics.Dispose();
            bitmap.Dispose();
            font.Dispose();
            smallFont.Dispose();
        }

        private string GetString()
        {
            if (!sensor.Value.HasValue)
                return "-";

            switch (sensor.SensorType)
            {
                case SensorType.Voltage:
                    return string.Format("{0:F1}", sensor.Value);
                case SensorType.Clock:
                    return string.Format("{0:F1}", 1e-3f * sensor.Value);
                case SensorType.Load:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Temperature:
                    if (unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
                        return string.Format("{0:F0}",
                          UnitManager.CelsiusToFahrenheit(sensor.Value));
                    else
                        return string.Format("{0:F0}", sensor.Value);
                case SensorType.Fan:
                    return string.Format("{0:F1}", 1e-3f * sensor.Value);
                case SensorType.Flow:
                    return string.Format("{0:F1}", 1e-3f * sensor.Value);
                case SensorType.Control:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Level:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Power:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Data:
                    return string.Format("{0:F0}", sensor.Value);
                case SensorType.Factor:
                    return string.Format("{0:F1}", sensor.Value);
            }
            return "-";
        }

        private Icon CreateTransparentIcon()
        {
            string text = GetString();
            int count = 0;
            for (int i = 0; i < text.Length; i++)
                if ((text[i] >= '0' && text[i] <= '9') || text[i] == '-')
                    count++;
            bool small = count > 2;

            graphics.Clear(Color.Black);
            TextRenderer.DrawText(graphics, text, small ? smallFont : font, new Point(-2, small ? 1 : 0), Color.White, Color.Black);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            IntPtr Scan0 = data.Scan0;

            int numBytes = bitmap.Width * bitmap.Height * 4;
            byte[] bytes = new byte[numBytes];
            Marshal.Copy(Scan0, bytes, 0, numBytes);
            bitmap.UnlockBits(data);

            byte red, green, blue;
            for (int i = 0; i < bytes.Length; i += 4)
            {
                blue = bytes[i];
                green = bytes[i + 1];
                red = bytes[i + 2];

                bytes[i] = color.B;
                bytes[i + 1] = color.G;
                bytes[i + 2] = color.R;
                bytes[i + 3] = (byte)(0.3 * red + 0.59 * green + 0.11 * blue);
            }

            return IconFactory.Create(bytes, bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
        }

        private Icon CreatePercentageIcon()
        {
            try
            {
                graphics.Clear(Color.Transparent);
            }
            catch (ArgumentException)
            {
                graphics.Clear(Color.Black);
            }
            graphics.FillRectangle(darkBrush, 0.5f, -0.5f, bitmap.Width - 2, bitmap.Height);
            float value = sensor.Value.GetValueOrDefault();
            float y = 0.16f * (100 - value);
            graphics.FillRectangle(brush, 0.5f, -0.5f + y, bitmap.Width - 2, bitmap.Height - y);
            graphics.DrawRectangle(pen, 1, 0, bitmap.Width - 3, bitmap.Height - 1);

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] bytes = new byte[bitmap.Width * bitmap.Height * 4];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);

            return IconFactory.Create(bytes, bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
        }

        public void Update()
        {
            Icon icon = notifyIcon.Icon;

            switch (sensor.SensorType)
            {
                case SensorType.Load:
                case SensorType.Control:
                case SensorType.Level:
                    notifyIcon.Icon = CreatePercentageIcon();
                    break;
                default:
                    notifyIcon.Icon = CreateTransparentIcon();
                    break;
            }

            if (icon != null)
                icon.Dispose();

            string format = "";
            switch (sensor.SensorType)
            {
                case SensorType.Voltage: format = "\n{0}: {1:F2} V"; break;
                case SensorType.Clock: format = "\n{0}: {1:F0} MHz"; break;
                case SensorType.Load: format = "\n{0}: {1:F1} %"; break;
                case SensorType.Temperature: format = "\n{0}: {1:F1} °C"; break;
                case SensorType.Fan: format = "\n{0}: {1:F0} RPM"; break;
                case SensorType.Flow: format = "\n{0}: {1:F0} L/h"; break;
                case SensorType.Control: format = "\n{0}: {1:F1} %"; break;
                case SensorType.Level: format = "\n{0}: {1:F1} %"; break;
                case SensorType.Power: format = "\n{0}: {1:F0} W"; break;
                case SensorType.Data: format = "\n{0}: {1:F0} GB"; break;
                case SensorType.Factor: format = "\n{0}: {1:F3} GB"; break;
            }
            string formattedValue = string.Format(format, sensor.Name, sensor.Value);

            if (sensor.SensorType == SensorType.Temperature && unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
            {
                format = "\n{0}: {1:F1} °F";
                formattedValue = string.Format(format, sensor.Name, UnitManager.CelsiusToFahrenheit(sensor.Value));
            }

            string hardwareName = sensor.Hardware.Name;
            hardwareName = hardwareName.Substring(0, Math.Min(63 - formattedValue.Length, hardwareName.Length));
            string text = hardwareName + formattedValue;
            if (text.Length > 63)
                text = null;

            notifyIcon.Text = text;
            notifyIcon.Visible = true;
        }
    }
}
