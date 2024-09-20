// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI;

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
        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        ToolStripItem hideShowItem = new ToolStripMenuItem("Hide/Show");
        hideShowItem.Click += delegate
        {
            sensorSystemTray.SendHideShowCommand();
        };
        contextMenuStrip.Items.Add(hideShowItem);
        contextMenuStrip.Items.Add(new ToolStripSeparator());
        ToolStripItem removeItem = new ToolStripMenuItem("Remove Sensor");
        removeItem.Click += delegate
        {
            sensorSystemTray.Remove(Sensor);
        };
        contextMenuStrip.Items.Add(removeItem);
        ToolStripItem colorItem = new ToolStripMenuItem("Change Color...");
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
        contextMenuStrip.Items.Add(colorItem);
        contextMenuStrip.Items.Add(new ToolStripSeparator());
        ToolStripItem exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += delegate
        {
            sensorSystemTray.SendExitCommand();
        };
        contextMenuStrip.Items.Add(exitItem);
        _notifyIcon.ContextMenuStrip = contextMenuStrip;
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
        icon?.Destroy();
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
            case SensorType.Temperature:
                return _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit ? $"{UnitManager.CelsiusToFahrenheit(Sensor.Value):F0}" : $"{Sensor.Value:F0}";
            case SensorType.TimeSpan:
                return $"{TimeSpan.FromSeconds(Sensor.Value.Value):g}";
            case SensorType.Clock:
            case SensorType.Fan:
            case SensorType.Flow:
                return $"{1e-3f * Sensor.Value:F1}";
            case SensorType.Voltage:
            case SensorType.Current:
            case SensorType.SmallData:
            case SensorType.Factor:
            case SensorType.Throughput:
            case SensorType.Conductivity:
                return $"{Sensor.Value:F1}";
            case SensorType.Control:
            case SensorType.Frequency:
            case SensorType.Level:
            case SensorType.Power:
            case SensorType.Data:
            case SensorType.Load:
            case SensorType.Energy:
            case SensorType.Noise:
            case SensorType.Humidity:
                return $"{Sensor.Value:F0}";
            default:
                return "-";
        }
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
        Rectangle bounds = new Rectangle(Point.Empty, _bitmap.Size);
        TextRenderer.DrawText(_graphics, text, small ? _smallFont : _font,
            bounds, Color.White, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        
        BitmapData data = _bitmap.LockBits(
            new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

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

        return IconFactory.Create(bytes, _bitmap.Width, _bitmap.Height,
            PixelFormat.Format32bppArgb);
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
        float y = (float)(_bitmap.Height * 0.01f) * (100 - value);
        _graphics.FillRectangle(_brush, 0.5f, -0.5f + y, _bitmap.Width - 2, _bitmap.Height - y);
        _graphics.DrawRectangle(_pen, 1, 0, _bitmap.Width - 3, _bitmap.Height - 1);

        return IconFactory.Create(_bitmap);
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

        icon?.Destroy();

        string format = "";
        switch (Sensor.SensorType)
        {
            case SensorType.Voltage: format = "\n{0}: {1:F2} V"; break;
            case SensorType.Current: format = "\n{0}: {1:F2} A"; break;
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
            case SensorType.Energy: format = "\n{0}: {0:F0} mWh"; break;
            case SensorType.Noise: format = "\n{0}: {0:F0} dBA"; break;
            case SensorType.Conductivity: format = "\n{0}: {0:F1} µS/cm"; break;
            case SensorType.Humidity: format = "\n{0}: {0:F0} %"; break;
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
