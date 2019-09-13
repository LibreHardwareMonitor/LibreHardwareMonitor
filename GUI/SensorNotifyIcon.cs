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
        private UnitManager _unitManager;
        private ISensor _sensor;
        private NotifyIconAdv _notifyIcon;
        private Bitmap _bitmap;
        private Graphics _graphics;
        private Color _color;
        private Color _darkColor;
        private Brush _brush;
        private Brush _darkBrush;
        private Pen _pen;
        private Font _font;
        private Font _smallFont;

        public SensorNotifyIcon(SystemTray sensorSystemTray, ISensor sensor, bool balloonTip, PersistentSettings settings, UnitManager unitManager)
        {
            this._unitManager = unitManager;
            this._sensor = sensor;
            this._notifyIcon = new NotifyIconAdv();

            Color defaultColor = Color.White;
            if (sensor.SensorType == SensorType.Load ||
                sensor.SensorType == SensorType.Control ||
                sensor.SensorType == SensorType.Level)
            {
                defaultColor = Color.FromArgb(0xff, 0x70, 0x8c, 0xf1);
            }
            Color = settings.GetValue(new Identifier(sensor.Identifier, "traycolor").ToString(), defaultColor);

            this._pen = new Pen(Color.FromArgb(96, Color.Black));
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
                sensorSystemTray.Remove(this._sensor);
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
            this._notifyIcon.ContextMenu = contextMenu;
            this._notifyIcon.DoubleClick += delegate (object obj, EventArgs args)
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

            this._font = new Font(family, baseSize * width / 16.0f, GraphicsUnit.Pixel);
            this._smallFont = new Font(family, 0.75f * baseSize * width / 16.0f, GraphicsUnit.Pixel);

            this._bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            this._graphics = Graphics.FromImage(this._bitmap);
            if (Environment.OSVersion.Version.Major > 5)
            {
                this._graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                this._graphics.SmoothingMode = SmoothingMode.HighQuality;
            }
        }

        public ISensor Sensor
        {
            get { return _sensor; }
        }

        public Color Color
        {
            get { return _color; }
            set
            {
                this._color = value;
                this._darkColor = Color.FromArgb(255, this._color.R / 3, this._color.G / 3, this._color.B / 3);
                Brush brush = this._brush;
                this._brush = new SolidBrush(this._color);
                if (brush != null)
                    brush.Dispose();
                Brush darkBrush = this._darkBrush;
                this._darkBrush = new SolidBrush(this._darkColor);
                if (darkBrush != null)
                    darkBrush.Dispose();
            }
        }

        public void Dispose()
        {
            Icon icon = _notifyIcon.Icon;
            _notifyIcon.Icon = null;
            if (icon != null)
                icon.Dispose();
            _notifyIcon.Dispose();

            if (_brush != null)
                _brush.Dispose();
            if (_darkBrush != null)
                _darkBrush.Dispose();
            _pen.Dispose();
            _graphics.Dispose();
            _bitmap.Dispose();
            _font.Dispose();
            _smallFont.Dispose();
        }

        private string GetString()
        {
            if (!_sensor.Value.HasValue)
                return "-";

            switch (_sensor.SensorType)
            {
                case SensorType.Voltage:
                    return string.Format("{0:F1}", _sensor.Value);
                case SensorType.Clock:
                    return string.Format("{0:F1}", 1e-3f * _sensor.Value);
                case SensorType.Load:
                    return string.Format("{0:F0}", _sensor.Value);
                case SensorType.Temperature:
                    if (_unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
                        return string.Format("{0:F0}",
                          UnitManager.CelsiusToFahrenheit(_sensor.Value));
                    else
                        return string.Format("{0:F0}", _sensor.Value);
                case SensorType.Fan:
                    return string.Format("{0:F1}", 1e-3f * _sensor.Value);
                case SensorType.Flow:
                    return string.Format("{0:F1}", 1e-3f * _sensor.Value);
                case SensorType.Control:
                    return string.Format("{0:F0}", _sensor.Value);
                case SensorType.Level:
                    return string.Format("{0:F0}", _sensor.Value);
                case SensorType.Power:
                    return string.Format("{0:F0}", _sensor.Value);
                case SensorType.Data:
                    return string.Format("{0:F0}", _sensor.Value);
                case SensorType.Factor:
                    return string.Format("{0:F1}", _sensor.Value);
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

            _graphics.Clear(Color.Black);
            TextRenderer.DrawText(_graphics, text, small ? _smallFont : _font, new Point(-2, small ? 1 : 0), Color.White, Color.Black);
            BitmapData data = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            IntPtr Scan0 = data.Scan0;

            int numBytes = _bitmap.Width * _bitmap.Height * 4;
            byte[] bytes = new byte[numBytes];
            Marshal.Copy(Scan0, bytes, 0, numBytes);
            _bitmap.UnlockBits(data);

            byte red, green, blue;
            for (int i = 0; i < bytes.Length; i += 4)
            {
                blue = bytes[i];
                green = bytes[i + 1];
                red = bytes[i + 2];

                bytes[i] = _color.B;
                bytes[i + 1] = _color.G;
                bytes[i + 2] = _color.R;
                bytes[i + 3] = (byte)(0.3 * red + 0.59 * green + 0.11 * blue);
            }

            return IconFactory.Create(bytes, _bitmap.Width, _bitmap.Height, PixelFormat.Format32bppArgb);
        }

        private Icon CreatePercentageIcon()
        {
            try
            {
                _graphics.Clear(Color.Transparent);
            }
            catch (ArgumentException)
            {
                _graphics.Clear(Color.Black);
            }
            _graphics.FillRectangle(_darkBrush, 0.5f, -0.5f, _bitmap.Width - 2, _bitmap.Height);
            float value = _sensor.Value.GetValueOrDefault();
            float y = 0.16f * (100 - value);
            _graphics.FillRectangle(_brush, 0.5f, -0.5f + y, _bitmap.Width - 2, _bitmap.Height - y);
            _graphics.DrawRectangle(_pen, 1, 0, _bitmap.Width - 3, _bitmap.Height - 1);

            BitmapData data = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            byte[] bytes = new byte[_bitmap.Width * _bitmap.Height * 4];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            _bitmap.UnlockBits(data);

            return IconFactory.Create(bytes, _bitmap.Width, _bitmap.Height, PixelFormat.Format32bppArgb);
        }

        public void Update()
        {
            Icon icon = _notifyIcon.Icon;

            switch (_sensor.SensorType)
            {
                case SensorType.Load:
                case SensorType.Control:
                case SensorType.Level:
                    _notifyIcon.Icon = CreatePercentageIcon();
                    break;
                default:
                    _notifyIcon.Icon = CreateTransparentIcon();
                    break;
            }

            if (icon != null)
                icon.Dispose();

            string format = "";
            switch (_sensor.SensorType)
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
            string formattedValue = string.Format(format, _sensor.Name, _sensor.Value);

            if (_sensor.SensorType == SensorType.Temperature && _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
            {
                format = "\n{0}: {1:F1} °F";
                formattedValue = string.Format(format, _sensor.Name, UnitManager.CelsiusToFahrenheit(_sensor.Value));
            }

            string hardwareName = _sensor.Hardware.Name;
            hardwareName = hardwareName.Substring(0, Math.Min(63 - formattedValue.Length, hardwareName.Length));
            string text = hardwareName + formattedValue;
            if (text.Length > 63)
                text = null;

            _notifyIcon.Text = text;
            _notifyIcon.Visible = true;
        }
    }
}
