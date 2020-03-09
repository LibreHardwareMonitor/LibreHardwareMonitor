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
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI
{
    public class SensorNotifyIcon : IDisposable
    {
        private readonly UnitManager _unitManager;
        private readonly NotifyIconAdv _notifyIcon;
        private readonly Bitmap _bitmap;
        private readonly Graphics _graphics;
        private Color _color;
        private Color _darkColor;
        private Brush _brush;
        private Brush _darkBrush;
        private readonly Pen _pen;
        private readonly Font _font;
        private readonly Font _smallFont;

        public SensorNotifyIcon(SystemTray sensorSystemTray, ISensor sensor, PersistentSettings settings, UnitManager unitManager)
        {
            _unitManager = unitManager;
            Sensor = sensor;
            _notifyIcon = new NotifyIconAdv();

            Color defaultColor = Color.White;
            if (sensor.SensorType == SensorType.Load || sensor.SensorType == SensorType.Control || sensor.SensorType == SensorType.Level)
                defaultColor = Color.FromArgb(0xff, 0x70, 0x8c, 0xf1);

            Color = settings.GetValue(new Identifier(sensor.Identifier, "traycolor").ToString(), defaultColor);

            _pen = new Pen(Color.FromArgb(96, Color.Black));
            ContextMenu contextMenu = new ContextMenu();
            MenuItem hideShowItem = new MenuItem("Hide/Show");
            hideShowItem.Click += delegate
            {
                sensorSystemTray.SendHideShowCommand();
            };
            contextMenu.MenuItems.Add(hideShowItem);
            contextMenu.MenuItems.Add(new MenuItem("-"));
            MenuItem removeItem = new MenuItem("Remove Sensor");
            removeItem.Click += delegate
            {
                sensorSystemTray.Remove(Sensor);
            };
            contextMenu.MenuItems.Add(removeItem);
            MenuItem colorItem = new MenuItem("Change Color...");
            colorItem.Click += delegate
            {
                ColorDialog dialog = new ColorDialog { Color = Color };
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
            exitItem.Click += delegate
            {
                sensorSystemTray.SendExitCommand();
            };
            contextMenu.MenuItems.Add(exitItem);
            _notifyIcon.ContextMenu = contextMenu;
            _notifyIcon.DoubleClick += delegate
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

            _font = new Font(family, baseSize * width / 16.0f, GraphicsUnit.Pixel);
            _smallFont = new Font(family, 0.75f * baseSize * width / 16.0f, GraphicsUnit.Pixel);

            _bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            _graphics = Graphics.FromImage(_bitmap);
            if (Environment.OSVersion.Version.Major > 5)
            {
                _graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                _graphics.SmoothingMode = SmoothingMode.HighQuality;
            }
        }

        public ISensor Sensor { get; }

        public Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                _darkColor = Color.FromArgb(255, _color.R / 3, _color.G / 3, _color.B / 3);
                Brush brush = _brush;
                _brush = new SolidBrush(_color);
                brush?.Dispose();
                Brush darkBrush = _darkBrush;
                _darkBrush = new SolidBrush(_darkColor);
                darkBrush?.Dispose();
            }
        }

        public void Dispose()
        {
            Icon icon = _notifyIcon.Icon;
            _notifyIcon.Icon = null;
            icon?.Dispose();
            _notifyIcon.Dispose();

            _brush?.Dispose();
            _darkBrush?.Dispose();
            _pen.Dispose();
            _graphics.Dispose();
            _bitmap.Dispose();
            _font.Dispose();
            _smallFont.Dispose();
        }

        private string GetString()
        {
            if (!Sensor.Value.HasValue)
                return "-";

            switch (Sensor.SensorType)
            {
                case SensorType.Voltage:
                    return $"{Sensor.Value:F1}";
                case SensorType.Clock:
                    return $"{1e-3f * Sensor.Value:F1}";
                case SensorType.Load:
                    return $"{Sensor.Value:F0}";
                case SensorType.Temperature:
                    return _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit ? $"{UnitManager.CelsiusToFahrenheit(Sensor.Value):F0}" : $"{Sensor.Value:F0}";
                case SensorType.Fan:
                    return $"{1e-3f * Sensor.Value:F1}";
                case SensorType.Flow:
                    return $"{1e-3f * Sensor.Value:F1}";
                case SensorType.Control:
                    return $"{Sensor.Value:F0}";
                case SensorType.Level:
                    return $"{Sensor.Value:F0}";
                case SensorType.Power:
                    return $"{Sensor.Value:F0}";
                case SensorType.Data:
                    return $"{Sensor.Value:F0}";
                case SensorType.Factor:
                    return $"{Sensor.Value:F1}";
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
            IntPtr scan0 = data.Scan0;

            int numBytes = _bitmap.Width * _bitmap.Height * 4;
            byte[] bytes = new byte[numBytes];
            Marshal.Copy(scan0, bytes, 0, numBytes);
            _bitmap.UnlockBits(data);

            for (int i = 0; i < bytes.Length; i += 4)
            {
                byte blue = bytes[i];
                byte green = bytes[i + 1];
                byte red = bytes[i + 2];

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
            float value = Sensor.Value.GetValueOrDefault();
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

            switch (Sensor.SensorType)
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

            icon?.Dispose();

            string format = "";
            switch (Sensor.SensorType)
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
            string formattedValue = string.Format(format, Sensor.Name, Sensor.Value);

            if (Sensor.SensorType == SensorType.Temperature && _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
            {
                format = "\n{0}: {1:F1} °F";
                formattedValue = string.Format(format, Sensor.Name, UnitManager.CelsiusToFahrenheit(Sensor.Value));
            }

            string hardwareName = Sensor.Hardware.Name;
            hardwareName = hardwareName.Substring(0, Math.Min(63 - formattedValue.Length, hardwareName.Length));
            string text = hardwareName + formattedValue;
            if (text.Length > 63)
                text = null;

            _notifyIcon.Text = text;
            _notifyIcon.Visible = true;
        }
    }
}
