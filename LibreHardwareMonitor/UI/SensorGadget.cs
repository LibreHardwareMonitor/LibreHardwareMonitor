// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI.Themes;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI;

public class SensorGadget : Gadget
{
    private const int TopBorder = 6;
    private const int BottomBorder = 7;
    private const int LeftBorder = 6;
    private const int RightBorder = 7;

    private readonly UnitManager _unitManager;
    private Image _back = Utilities.EmbeddedResources.GetImage("gadget.png");
    private Image _image;
    private Image _fore;
    private Image _barBack = Utilities.EmbeddedResources.GetImage("barback.png");
    private Image _barFore = Utilities.EmbeddedResources.GetImage("bar.png");
    private Image _backTinted;
    private Image _barForeTinted;
    private Image _background = new Bitmap(1, 1);
    private bool _backgroundDirty = true;
    private readonly float _scale;
    private float _fontSize;
    private int _iconSize;
    private int _hardwareLineHeight;
    private int _sensorLineHeight;
    private int _rightMargin;
    private int _leftMargin;
    private int _topMargin;
    private int _bottomMargin;
    private int _progressWidth;

    private readonly IDictionary<IHardware, IList<ISensor>> _sensors = new SortedDictionary<IHardware, IList<ISensor>>(new HardwareComparer());
    private readonly PersistentSettings _settings;
    private readonly UserOption _hardwareNames;

    private Font _largeFont;
    private Font _smallFont;
    private Brush _textBrush;
    private StringFormat _stringFormat;
    private StringFormat _trimStringFormat;
    private StringFormat _alignRightStringFormat;
    private Color _backgroundColor;

    public SensorGadget(IComputer computer, PersistentSettings settings, UnitManager unitManager)
    {
        _unitManager = unitManager;
        _settings = settings;
        computer.HardwareAdded += HardwareAdded;
        computer.HardwareRemoved += HardwareRemoved;

        _stringFormat = new StringFormat { FormatFlags = StringFormatFlags.NoWrap };
        _trimStringFormat = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        _alignRightStringFormat = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.NoWrap };

        if (File.Exists("gadget_background.png"))
        {
            try
            {
                Image newBack = new Bitmap("gadget_background.png");
                _back.Dispose();
                _back = newBack;
            }
            catch { }
        }

        if (File.Exists("gadget_image.png"))
        {
            try
            {
                _image = new Bitmap("gadget_image.png");
            }
            catch { }
        }

        if (File.Exists("gadget_foreground.png"))
        {
            try
            {
                _fore = new Bitmap("gadget_foreground.png");
            }
            catch { }
        }

        if (File.Exists("gadget_bar_background.png"))
        {
            try
            {
                Image newBarBack = new Bitmap("gadget_bar_background.png");
                _barBack.Dispose();
                _barBack = newBarBack;
            }
            catch { }
        }

        if (File.Exists("gadget_bar_foreground.png"))
        {
            try
            {
                Image newBarColor = new Bitmap("gadget_bar_foreground.png");
                _barFore.Dispose();
                _barFore = newBarColor;
            }
            catch { }
        }

        Location = new Point(settings.GetValue("sensorGadget.Location.X", 100), settings.GetValue("sensorGadget.Location.Y", 100));
        LocationChanged += delegate
        {
            settings.SetValue("sensorGadget.Location.X", Location.X);
            settings.SetValue("sensorGadget.Location.Y", Location.Y);
        };

        // get the custom to default dpi ratio
        using (Bitmap b = new Bitmap(1, 1))
        {
            _scale = b.HorizontalResolution / 96.0f;
        }

        SetFontSize(settings.GetValue("sensorGadget.FontSize", 7.5f));
        Resize(settings.GetValue("sensorGadget.Width", Size.Width));

        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        ToolStripMenuItem hardwareNamesItem = new ToolStripMenuItem("Hardware Names");
        contextMenuStrip.Items.Add(hardwareNamesItem);
        ToolStripMenuItem fontSizeMenu = new ToolStripMenuItem("Font Size");
        for (int i = 0; i < 5; i++)
        {
            float size;
            string name;
            switch (i)
            {
                case 0: size = 6.5f; name = "Small"; break;
                case 1: size = 7.5f; name = "Medium"; break;
                case 2: size = 9f; name = "Large"; break;
                case 3: size = 11f; name = "Very Large"; break;
                case 4: size = 22f; name = "Extremely Large"; break;
                default: throw new NotImplementedException();
            }

            ToolStripItem item = new ToolStripMenuItem(name) { Checked = _fontSize == size };
            item.Click += delegate
            {
                SetFontSize(size);
                settings.SetValue("sensorGadget.FontSize", size);
                foreach (ToolStripMenuItem mi in fontSizeMenu.DropDownItems)
                    mi.Checked = mi == item;
            };
            fontSizeMenu.DropDownItems.Add(item);
        }
        contextMenuStrip.Items.Add(fontSizeMenu);

        Color fontColor = settings.GetValue("sensorGadget.FontColor", Color.White);
        SetFontColor(fontColor);

        ToolStripMenuItem fontColorMenu = new ToolStripMenuItem("Font Color");
        ToolStripItem chooseFontColorItem = new ToolStripMenuItem("Choose...");
        chooseFontColorItem.Click += delegate
        {
            if (TrySelectColor(fontColor, out Color selectedColor))
            {
                fontColor = selectedColor;
                SetFontColor(fontColor);
                settings.SetValue("sensorGadget.FontColor", fontColor);
                Redraw();
            }
        };
        fontColorMenu.DropDownItems.Add(chooseFontColorItem);

        ToolStripItem defaultFontColorItem = new ToolStripMenuItem("Default");
        defaultFontColorItem.Click += delegate
        {
            fontColor = Color.White;
            SetFontColor(fontColor);
            settings.Remove("sensorGadget.FontColor");
            Redraw();
        };
        fontColorMenu.DropDownItems.Add(defaultFontColorItem);
        contextMenuStrip.Items.Add(fontColorMenu);

        Color backgroundColor = settings.GetValue("sensorGadget.BackgroundColor", Color.FromArgb(0));
        SetBackgroundColor(backgroundColor);

        ToolStripMenuItem backgroundColorMenu = new ToolStripMenuItem("Background Color");
        ToolStripItem chooseBackgroundItem = new ToolStripMenuItem("Choose...");
        chooseBackgroundItem.Click += delegate
        {
            if (TrySelectColor(backgroundColor.A == 0 ? Color.White : backgroundColor, out Color selectedColor))
            {
                backgroundColor = selectedColor;
                SetBackgroundColor(backgroundColor);
                settings.SetValue("sensorGadget.BackgroundColor", backgroundColor);
            }
        };
        backgroundColorMenu.DropDownItems.Add(chooseBackgroundItem);

        ToolStripItem defaultBackgroundItem = new ToolStripMenuItem("Default");
        defaultBackgroundItem.Click += delegate
        {
            Color color = Color.FromArgb(0);
            backgroundColor = color;
            SetBackgroundColor(color);
            settings.SetValue("sensorGadget.BackgroundColor", color);
        };
        backgroundColorMenu.DropDownItems.Add(defaultBackgroundItem);
        contextMenuStrip.Items.Add(backgroundColorMenu);
        contextMenuStrip.Items.Add(new ToolStripSeparator());
        ToolStripMenuItem lockItem = new ToolStripMenuItem("Lock Position and Size");
        contextMenuStrip.Items.Add(lockItem);
        contextMenuStrip.Items.Add(new ToolStripSeparator());
        ToolStripMenuItem alwaysOnTopItem = new ToolStripMenuItem("Always on Top");
        contextMenuStrip.Items.Add(alwaysOnTopItem);
        ToolStripMenuItem opacityMenu = new ToolStripMenuItem("Opacity");
        contextMenuStrip.Items.Add(opacityMenu);
        Opacity = (byte)settings.GetValue("sensorGadget.Opacity", 255);

        for (int i = 0; i < 5; i++)
        {
            ToolStripMenuItem item = new ToolStripMenuItem((20 * (i + 1)).ToString() + " %");
            byte o = (byte)(51 * (i + 1));
            item.Checked = Opacity == o;
            item.Click += delegate
            {
                Opacity = o;
                settings.SetValue("sensorGadget.Opacity", Opacity);
                foreach (ToolStripMenuItem mi in opacityMenu.DropDownItems)
                    mi.Checked = mi == item;
            };
            opacityMenu.DropDownItems.Add(item);
        }

        contextMenuStrip.Items.Add(new ToolStripSeparator());
        ToolStripMenuItem hideShowItem = new ToolStripMenuItem("Hide/Show Main Window");
        contextMenuStrip.Items.Add(hideShowItem);

        ContextMenuStrip = contextMenuStrip;

        _hardwareNames = new UserOption("sensorGadget.Hardwarenames", true, hardwareNamesItem, settings);
        _hardwareNames.Changed += delegate
        {
            Resize();
        };

        UserOption alwaysOnTop = new UserOption("sensorGadget.AlwaysOnTop", false, alwaysOnTopItem, settings);
        alwaysOnTop.Changed += delegate
        {
            AlwaysOnTop = alwaysOnTop.Value;
        };
        UserOption lockPositionAndSize = new UserOption("sensorGadget.LockPositionAndSize", false, lockItem, settings);
        lockPositionAndSize.Changed += delegate
        {
            LockPositionAndSize = lockPositionAndSize.Value;
        };

        hideShowItem.Click += delegate
        {
            SendHideShowCommand();
        };

        HitTest += delegate (object sender, HitTestEventArgs e)
        {
            if (lockPositionAndSize.Value)
                return;

            if (e.Location.X < LeftBorder)
            {
                e.HitResult = HitResult.Left;
                return;
            }
            if (e.Location.X > Size.Width - 1 - RightBorder)
            {
                e.HitResult = HitResult.Right;
            }
        };

        SizeChanged += delegate
        {
            settings.SetValue("sensorGadget.Width", Size.Width);
            Redraw();
        };

        VisibleChanged += delegate
        {
            Rectangle bounds = new Rectangle(Location, Size);
            Screen screen = Screen.FromRectangle(bounds);
            Rectangle intersection = Rectangle.Intersect(screen.WorkingArea, bounds);
            if (intersection.Width < Math.Min(16, bounds.Width) || intersection.Height < Math.Min(16, bounds.Height))
            {
                Location = new Point(screen.WorkingArea.Width / 2 - bounds.Width / 2, screen.WorkingArea.Height / 2 - bounds.Height / 2);
            }
        };

        MouseDoubleClick += delegate
        {
            SendHideShowCommand();
        };
    }

    public override void Dispose()
    {

        _largeFont.Dispose();
        _largeFont = null;

        _smallFont.Dispose();
        _smallFont = null;

        _textBrush.Dispose();
        _textBrush = null;

        _stringFormat.Dispose();
        _stringFormat = null;

        _trimStringFormat.Dispose();
        _trimStringFormat = null;

        _alignRightStringFormat.Dispose();
        _alignRightStringFormat = null;

        _back.Dispose();
        _back = null;

        _barFore.Dispose();
        _barFore = null;

        _barBack.Dispose();
        _barBack = null;

        if (_backTinted != null)
        {
            _backTinted.Dispose();
            _backTinted = null;
        }
        if (_barForeTinted != null)
        {
            _barForeTinted.Dispose();
            _barForeTinted = null;
        }

        _background.Dispose();
        _background = null;

        if (_image != null)
        {
            _image.Dispose();
            _image = null;
        }

        if (_fore != null)
        {
            _fore.Dispose();
            _fore = null;
        }

        base.Dispose();
    }

    private void HardwareRemoved(IHardware hardware)
    {
        hardware.SensorAdded -= SensorAdded;
        hardware.SensorRemoved -= SensorRemoved;

        foreach (ISensor sensor in hardware.Sensors)
            SensorRemoved(sensor);

        foreach (IHardware subHardware in hardware.SubHardware)
            HardwareRemoved(subHardware);
    }

    private void HardwareAdded(IHardware hardware)
    {
        foreach (ISensor sensor in hardware.Sensors)
            SensorAdded(sensor);

        hardware.SensorAdded += SensorAdded;
        hardware.SensorRemoved += SensorRemoved;

        foreach (IHardware subHardware in hardware.SubHardware)
            HardwareAdded(subHardware);
    }

    private void SensorAdded(ISensor sensor)
    {
        if (_settings.GetValue(new Identifier(sensor.Identifier, "gadget").ToString(), false))
            Add(sensor);
    }

    private void SensorRemoved(ISensor sensor)
    {
        if (Contains(sensor))
            Remove(sensor, false);
    }

    public bool Contains(ISensor sensor)
    {
        return _sensors.Values.Any(list => list.Contains(sensor));
    }

    public void Add(ISensor sensor)
    {
        if (Contains(sensor))
            return;


        // get the right hardware
        IHardware hardware = sensor.Hardware;
        while (hardware.Parent != null)
            hardware = hardware.Parent;

        // get the sensor list associated with the hardware
        if (!_sensors.TryGetValue(hardware, out IList<ISensor> list))
        {
            list = new List<ISensor>();
            _sensors.Add(hardware, list);
        }

        // insert the sensor at the right position
        int i = 0;
        while (i < list.Count && (list[i].SensorType < sensor.SensorType || (list[i].SensorType == sensor.SensorType && list[i].Index < sensor.Index)))
            i++;

        list.Insert(i, sensor);

        _settings.SetValue(new Identifier(sensor.Identifier, "gadget").ToString(), true);
        Resize();
    }

    public void Remove(ISensor sensor)
    {
        Remove(sensor, true);
    }

    private void Remove(ISensor sensor, bool deleteConfig)
    {
        if (deleteConfig)
            _settings.Remove(new Identifier(sensor.Identifier, "gadget").ToString());

        foreach (KeyValuePair<IHardware, IList<ISensor>> keyValue in _sensors)
        {
            if (keyValue.Value.Contains(sensor))
            {
                keyValue.Value.Remove(sensor);
                if (keyValue.Value.Count == 0)
                {
                    _sensors.Remove(keyValue.Key);
                    break;
                }
            }
        }
        Resize();
    }

    public event EventHandler HideShowCommand;

    public void SendHideShowCommand()
    {
        HideShowCommand?.Invoke(this, null);
    }

    private Font CreateFont(float size, FontStyle style)
    {
        try
        {
            return new Font(SystemFonts.MessageBoxFont.FontFamily, size, style);
        }
        catch (ArgumentException)
        {
            // if the style is not supported, fall back to the original one
            return new Font(SystemFonts.MessageBoxFont.FontFamily, size,
                            SystemFonts.MessageBoxFont.Style);
        }
    }

    private void SetFontSize(float size)
    {
        _fontSize = size;
        _largeFont = CreateFont(_fontSize, FontStyle.Bold);
        _smallFont = CreateFont(_fontSize, FontStyle.Regular);

        double scaledFontSize = _fontSize * _scale;
        _iconSize = (int)Math.Round(1.5 * scaledFontSize);
        _hardwareLineHeight = (int)Math.Round(1.66 * scaledFontSize);
        _sensorLineHeight = (int)Math.Round(1.33 * scaledFontSize);
        _leftMargin = LeftBorder + (int)Math.Round(0.3 * scaledFontSize);
        _rightMargin = RightBorder + (int)Math.Round(0.3 * scaledFontSize);
        _topMargin = TopBorder;
        _bottomMargin = BottomBorder + (int)Math.Round(0.3 * scaledFontSize);
        _progressWidth = (int)Math.Round(5.3 * scaledFontSize);

        Resize((int)Math.Round(17.3 * scaledFontSize));
    }

    private void SetFontColor(Color color)
    {
        _textBrush?.Dispose();
        _textBrush = new SolidBrush(color);
    }

    private void SetBackgroundColor(Color color)
    {
        _backgroundColor = color;
        _backTinted?.Dispose();
        _backTinted = null;
        _barForeTinted?.Dispose();
        _barForeTinted = null;

        // Transparent means "Default" and keeps the embedded/custom image as-is.
        if (_backgroundColor.A > 0)
        {
            _backTinted = CreateBackgroundTint(_back, _backgroundColor);
            _barForeTinted = CreateBarTint(_barFore, _backgroundColor);
        }

        _backgroundDirty = true;
        Redraw();
    }

    private static Image CreateBackgroundTint(Image source, Color targetColor)
    {
        Bitmap sourceBitmap = new Bitmap(source);
        Bitmap result = new Bitmap(sourceBitmap.Width, sourceBitmap.Height, PixelFormat.Format32bppPArgb);
        float targetHue = targetColor.GetHue() / 360f;
        float targetSaturation = targetColor.GetSaturation();

        for (int y = 0; y < sourceBitmap.Height; y++)
        {
            for (int x = 0; x < sourceBitmap.Width; x++)
            {
                Color c = sourceBitmap.GetPixel(x, y);
                if (c.A == 0)
                {
                    result.SetPixel(x, y, c);
                    continue;
                }

                // Keep original luminance/alpha so texture depth remains intact while
                // shifting hue to the selected background color.
                float saturation = Math.Min(1f, Math.Max(c.GetSaturation() * 0.35f, targetSaturation * 0.75f));
                Color tinted = ColorFromHsv(targetHue, saturation, c.GetBrightness(), c.A);

                result.SetPixel(x, y, tinted);
            }
        }

        sourceBitmap.Dispose();
        return result;
    }

    private static Image CreateBarTint(Image source, Color targetColor)
    {
        Bitmap sourceBitmap = new Bitmap(source);
        Bitmap result = new Bitmap(sourceBitmap.Width, sourceBitmap.Height, PixelFormat.Format32bppPArgb);
        float targetHue = targetColor.GetHue() / 360f;
        float targetSaturation = Math.Max(0.35f, targetColor.GetSaturation());

        for (int y = 0; y < sourceBitmap.Height; y++)
        {
            for (int x = 0; x < sourceBitmap.Width; x++)
            {
                Color c = sourceBitmap.GetPixel(x, y);
                if (c.A == 0)
                {
                    result.SetPixel(x, y, c);
                    continue;
                }

                float saturation = Math.Min(1f, Math.Max(c.GetSaturation() * 0.4f, targetSaturation));
                Color tinted = ColorFromHsv(targetHue, saturation, c.GetBrightness(), c.A);
                result.SetPixel(x, y, tinted);
            }
        }

        sourceBitmap.Dispose();
        return result;
    }

    private static Color ColorFromHsv(float hue, float saturation, float value, int alpha)
    {
        if (saturation <= 0)
        {
            int v = (int)Math.Round(value * 255);
            return Color.FromArgb(alpha, v, v, v);
        }

        float h = (hue % 1.0f + 1.0f) % 1.0f * 6.0f;
        int i = (int)Math.Floor(h);
        float f = h - i;
        float p = value * (1 - saturation);
        float q = value * (1 - saturation * f);
        float t = value * (1 - saturation * (1 - f));

        (float r, float g, float b) = i switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q)
        };

        return Color.FromArgb(alpha,
                              (int)Math.Round(r * 255),
                              (int)Math.Round(g * 255),
                              (int)Math.Round(b * 255));
    }

    private static bool TrySelectColor(Color initialColor, out Color selectedColor)
    {
        using Form form = new Form
        {
            Text = "Select Color",
            FormBorderStyle = FormBorderStyle.Sizable,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(460, 340),
            MinimumSize = new Size(360, 280)
        };
        form.Icon = EmbeddedResources.GetIcon("icon.ico");

        Panel svBox = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Cross
        };
        Panel hueBox = new Panel
        {
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Cross
        };
        SetDoubleBuffered(svBox);
        SetDoubleBuffered(hueBox);
        SetDoubleBuffered(form);

        float currentHue = initialColor.GetHue() / 360f;
        float currentSaturation = initialColor.GetSaturation();
        float currentValue = GetColorValue(initialColor);
        Color current = ColorFromHsv(currentHue, currentSaturation, currentValue, 255);

        int svSelectorX = 0;
        int svSelectorY = 0;
        int hueSelectorY = 0;

        Panel preview = new Panel
        {
            Size = new Size(34, 34),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = current
        };

        Label selectedLabel = new Label
        {
            AutoSize = true,
            Location = new Point(0, 0),
            Text = $"#{current.R:X2}{current.G:X2}{current.B:X2}"
        };

        Button okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 70 };
        Button cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 70 };

        void LayoutControls()
        {
            const int padding = 12;
            const int bottomRowHeight = 40;
            const int hueBarWidth = 24;
            const int hueGap = 8;

            svBox.Location = new Point(padding, padding);
            svBox.Size = new Size(
                Math.Max(200, form.ClientSize.Width - (2 * padding) - hueBarWidth - hueGap),
                Math.Max(120, form.ClientSize.Height - (3 * padding) - bottomRowHeight));
            hueBox.Location = new Point(svBox.Right + hueGap, padding);
            hueBox.Size = new Size(hueBarWidth, svBox.Height);

            preview.Location = new Point(padding, form.ClientSize.Height - padding - preview.Height);
            selectedLabel.Location = new Point(preview.Right + 10, preview.Top + 9);

            cancelButton.Location = new Point(form.ClientSize.Width - padding - cancelButton.Width, form.ClientSize.Height - padding - cancelButton.Height);
            okButton.Location = new Point(cancelButton.Left - 8 - okButton.Width, cancelButton.Top);
        }

        void SyncSelectorsFromCurrent()
        {
            svSelectorX = Math.Min(Math.Max(0, (int)Math.Round(currentSaturation * Math.Max(1, svBox.ClientSize.Width - 1))), Math.Max(0, svBox.ClientSize.Width - 1));
            svSelectorY = Math.Min(Math.Max(0, (int)Math.Round((1f - currentValue) * Math.Max(1, svBox.ClientSize.Height - 1))), Math.Max(0, svBox.ClientSize.Height - 1));
            hueSelectorY = Math.Min(Math.Max(0, (int)Math.Round(currentHue * Math.Max(1, hueBox.ClientSize.Height - 1))), Math.Max(0, hueBox.ClientSize.Height - 1));
        }

        void UpdateCurrentColor()
        {
            current = ColorFromHsv(currentHue, currentSaturation, currentValue, 255);
            preview.BackColor = current;
            selectedLabel.Text = $"#{current.R:X2}{current.G:X2}{current.B:X2}";
        }

        void UpdateSvFromPoint(Point p)
        {
            svSelectorX = Math.Min(Math.Max(0, p.X), Math.Max(0, svBox.ClientSize.Width - 1));
            svSelectorY = Math.Min(Math.Max(0, p.Y), Math.Max(0, svBox.ClientSize.Height - 1));
            currentSaturation = svSelectorX / (float)Math.Max(1, svBox.ClientSize.Width - 1);
            currentValue = 1f - svSelectorY / (float)Math.Max(1, svBox.ClientSize.Height - 1);
            UpdateCurrentColor();
            svBox.Invalidate();
        }

        void UpdateHueFromPoint(Point p)
        {
            int newHueSelectorY = Math.Min(Math.Max(0, p.Y), Math.Max(0, hueBox.ClientSize.Height - 1));
            if (newHueSelectorY == hueSelectorY)
                return;

            hueSelectorY = newHueSelectorY;
            currentHue = hueSelectorY / (float)Math.Max(1, hueBox.ClientSize.Height - 1);
            UpdateCurrentColor();
            svBox.Invalidate();
            hueBox.Invalidate();
        }

        bool draggingSv = false;
        bool draggingHue = false;
        svBox.MouseDown += delegate(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            draggingSv = true;
            UpdateSvFromPoint(e.Location);
        };
        svBox.MouseMove += delegate(object sender, MouseEventArgs e)
        {
            if (draggingSv)
                UpdateSvFromPoint(e.Location);
        };
        svBox.MouseUp += delegate { draggingSv = false; };

        hueBox.MouseDown += delegate(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            draggingHue = true;
            UpdateHueFromPoint(e.Location);
        };
        hueBox.MouseMove += delegate(object sender, MouseEventArgs e)
        {
            if (draggingHue)
                UpdateHueFromPoint(e.Location);
        };
        hueBox.MouseUp += delegate { draggingHue = false; };

        svBox.Paint += delegate(object sender, PaintEventArgs e)
        {
            Rectangle rect = svBox.ClientRectangle;
            if (rect.Width <= 1 || rect.Height <= 1)
                return;

            rect.Width -= 1;
            rect.Height -= 1;
            e.Graphics.SmoothingMode = SmoothingMode.None;

            using (SolidBrush hueBrush = new SolidBrush(ColorFromHsv(currentHue, 1f, 1f, 255)))
                e.Graphics.FillRectangle(hueBrush, rect);

            using (LinearGradientBrush satBrush = new LinearGradientBrush(rect, Color.White, Color.Transparent, LinearGradientMode.Horizontal))
            {
                ColorBlend satBlend = new ColorBlend
                {
                    Positions = new[] { 0f, 1f },
                    Colors = new[] { Color.FromArgb(255, 255, 255, 255), Color.FromArgb(0, 255, 255, 255) }
                };
                satBrush.InterpolationColors = satBlend;
                e.Graphics.FillRectangle(satBrush, rect);
            }

            using (LinearGradientBrush valueBrush = new LinearGradientBrush(rect, Color.Transparent, Color.Black, LinearGradientMode.Vertical))
            {
                ColorBlend valueBlend = new ColorBlend
                {
                    Positions = new[] { 0f, 1f },
                    Colors = new[] { Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 0, 0) }
                };
                valueBrush.InterpolationColors = valueBlend;
                e.Graphics.FillRectangle(valueBrush, rect);
            }

            Rectangle marker = new Rectangle(svSelectorX - 4, svSelectorY - 4, 8, 8);
            using Pen outer = new Pen(Color.Black, 2f);
            using Pen inner = new Pen(Color.White, 1f);
            e.Graphics.DrawEllipse(outer, marker);
            e.Graphics.DrawEllipse(inner, marker);
        };
        hueBox.Paint += delegate(object sender, PaintEventArgs e)
        {
            Rectangle rect = hueBox.ClientRectangle;
            if (rect.Width <= 1 || rect.Height <= 1)
                return;

            rect.Width -= 1;
            rect.Height -= 1;

            using (LinearGradientBrush hueBrush = new LinearGradientBrush(rect, Color.Red, Color.Red, LinearGradientMode.Vertical))
            {
                hueBrush.InterpolationColors = new ColorBlend
                {
                    Positions = new[] { 0f, 1f / 6f, 2f / 6f, 3f / 6f, 4f / 6f, 5f / 6f, 1f },
                    Colors = new[] { Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta, Color.Red }
                };
                e.Graphics.FillRectangle(hueBrush, rect);
            }

            Rectangle marker = new Rectangle(0, hueSelectorY - 2, hueBox.ClientSize.Width - 1, 4);
            using Pen outer = new Pen(Color.Black, 2f);
            using Pen inner = new Pen(Color.White, 1f);
            e.Graphics.DrawRectangle(outer, marker);
            e.Graphics.DrawRectangle(inner, marker);
        };

        form.Controls.Add(svBox);
        form.Controls.Add(hueBox);
        form.Controls.Add(preview);
        form.Controls.Add(selectedLabel);
        form.Controls.Add(okButton);
        form.Controls.Add(cancelButton);
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;
        Theme.Current.Apply(form);
        form.Resize += delegate
        {
            LayoutControls();
            SyncSelectorsFromCurrent();
            UpdateCurrentColor();
            svBox.Invalidate();
            hueBox.Invalidate();
        };
        LayoutControls();
        SyncSelectorsFromCurrent();
        UpdateCurrentColor();

        if (form.ShowDialog() == DialogResult.OK)
        {
            selectedColor = current;
            return true;
        }

        selectedColor = initialColor;
        return false;
    }

    private static void SetDoubleBuffered(Control control)
    {
        typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(control, true, null);
    }

    private static float GetColorValue(Color color)
    {
        return Math.Max(color.R, Math.Max(color.G, color.B)) / 255f;
    }

    private void Resize()
    {
        Resize(Size.Width);
    }

    private void Resize(int width)
    {
        int y = _topMargin;

        foreach (KeyValuePair<IHardware, IList<ISensor>> pair in _sensors)
        {
            if (_hardwareNames.Value)
            {
                if (y > _topMargin)
                    y += _hardwareLineHeight - _sensorLineHeight;
                y += _hardwareLineHeight;
            }
            y += pair.Value.Count * _sensorLineHeight;
        }

        if (_sensors.Count == 0)
            y += 4 * _sensorLineHeight + _hardwareLineHeight;

        y += _bottomMargin;
        Size = new Size(width, y);
    }

    private void DrawImageWidthBorder(Graphics g, int width, int height, Image back, int t, int b, int l, int r)
    {
        GraphicsUnit u = GraphicsUnit.Pixel;

        g.DrawImage(back, new Rectangle(0, 0, l, t), new Rectangle(0, 0, l, t), u);
        g.DrawImage(back, new Rectangle(l, 0, width - l - r, t), new Rectangle(l, 0, back.Width - l - r, t), u);
        g.DrawImage(back, new Rectangle(width - r, 0, r, t), new Rectangle(back.Width - r, 0, r, t), u);

        g.DrawImage(back, new Rectangle(0, t, l, height - t - b), new Rectangle(0, t, l, back.Height - t - b), u);
        g.DrawImage(back, new Rectangle(l, t, width - l - r, height - t - b), new Rectangle(l, t, back.Width - l - r, back.Height - t - b), u);
        g.DrawImage(back, new Rectangle(width - r, t, r, height - t - b), new Rectangle(back.Width - r, t, r, back.Height - t - b), u);

        g.DrawImage(back, new Rectangle(0, height - b, l, b), new Rectangle(0, back.Height - b, l, b), u);
        g.DrawImage(back, new Rectangle(l, height - b, width - l - r, b), new Rectangle(l, back.Height - b, back.Width - l - r, b), u);
        g.DrawImage(back, new Rectangle(width - r, height - b, r, b), new Rectangle(back.Width - r, back.Height - b, r, b), u);
    }

    private void DrawBackground(Graphics g)
    {
        int w = Size.Width;
        int h = Size.Height;

        if (_backgroundDirty || w != _background.Width || h != _background.Height)
        {
            _background.Dispose();
            _background = new Bitmap(w, h, PixelFormat.Format32bppPArgb);

            using (Graphics graphics = Graphics.FromImage(_background))
            {
                DrawImageWidthBorder(graphics, w, h, _backTinted ?? _back, TopBorder, BottomBorder, LeftBorder, RightBorder);

                if (_fore != null)
                    DrawImageWidthBorder(graphics, w, h, _fore, TopBorder, BottomBorder, LeftBorder, RightBorder);

                if (_image != null)
                {
                    int width = w - LeftBorder - RightBorder;
                    int height = h - TopBorder - BottomBorder;
                    float xRatio = width / (float)_image.Width;
                    float yRatio = height / (float)_image.Height;
                    float destWidth, destHeight;
                    float xOffset, yOffset;

                    if (xRatio < yRatio)
                    {
                        destWidth = width;
                        destHeight = _image.Height * xRatio;
                        xOffset = 0;
                        yOffset = 0.5f * (height - destHeight);
                    }
                    else
                    {
                        destWidth = _image.Width * yRatio;
                        destHeight = height;
                        xOffset = 0.5f * (width - destWidth);
                        yOffset = 0;
                    }

                    graphics.DrawImage(_image, new RectangleF(LeftBorder + xOffset, TopBorder + yOffset, destWidth, destHeight));
                }
            }

            _backgroundDirty = false;
        }

        g.DrawImageUnscaled(_background, 0, 0);
    }

    private void DrawProgress(Graphics g, float x, float y, float width, float height, float progress)
    {
        Image barFore = _barForeTinted ?? _barFore;
        g.DrawImage(_barBack,
                    new RectangleF(x + width * progress, y, width * (1 - progress), height),
                    new RectangleF(_barBack.Width * progress, 0, (1 - progress) * _barBack.Width, _barBack.Height),
                    GraphicsUnit.Pixel);
        g.DrawImage(barFore,
                    new RectangleF(x, y, width * progress, height),
                    new RectangleF(0, 0, progress * barFore.Width, barFore.Height), GraphicsUnit.Pixel);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        try
        {
            Graphics g = e.Graphics;
            int w = Size.Width;
    
            g.Clear(Color.Transparent);
            DrawBackground(g);
    
            int x;
            int y = _topMargin;
    
            if (_sensors.Count == 0)
            {
                x = LeftBorder + 1;
                g.DrawString("Right-click on a sensor in the main window and select " +
                             "\"Show in Gadget\" to show the sensor here.",
                             _smallFont, _textBrush,
                             new Rectangle(x, y - 1, w - RightBorder - x, 0));
            }
    
            foreach (KeyValuePair<IHardware, IList<ISensor>> pair in _sensors)
            {
                if (_hardwareNames.Value)
                {
                    if (y > _topMargin)
                        y += _hardwareLineHeight - _sensorLineHeight;

                    x = LeftBorder + 1;
                    g.DrawImage(HardwareTypeImage.Instance.GetImage(pair.Key.HardwareType), new Rectangle(x, y + 1, _iconSize, _iconSize));
                    x += _iconSize + 1;
                    g.DrawString(pair.Key.Name, _largeFont, _textBrush, new Rectangle(x, y - 1, w - RightBorder - x, 0), _stringFormat);
                    y += _hardwareLineHeight;
                }
    
                foreach (ISensor sensor in pair.Value)
                {
                    int remainingWidth;    
    
                    if ((sensor.SensorType != SensorType.Load &&
                         sensor.SensorType != SensorType.Control &&
                         sensor.SensorType != SensorType.Level &&
                         sensor.SensorType != SensorType.Humidity) || !sensor.Value.HasValue)
                    {
                        string formatted;
    
                        if (sensor.Value.HasValue)
                        {
                            string format = "";
                            switch (sensor.SensorType)
                            {
                                case SensorType.Voltage:
                                    format = "{0:F3} V";
                                    break;
                                case SensorType.Current:
                                    format = "{0:F3} A";
                                    break;
                                case SensorType.Clock:
                                    format = "{0:F0} MHz";
                                    break;
                                case SensorType.Frequency:
                                    format = "{0:F0} Hz";
                                    break;
                                case SensorType.Temperature:
                                    format = "{0:F1} °C";
                                    break;
                                case SensorType.Fan:
                                    format = "{0:F0} RPM";
                                    break;
                                case SensorType.Flow:
                                    format = "{0:F0} L/h";
                                    break;
                                case SensorType.Power:
                                    format = "{0:F1} W";
                                    break;
                                case SensorType.Data:
                                    format = "{0:F1} GB";
                                    break;
                                case SensorType.SmallData:
                                    format = "{0:F0} MB";
                                    break;
                                case SensorType.Factor:
                                    format = "{0:F3}";
                                    break;
                                case SensorType.TimeSpan:
                                    format = "{0:g}";
                                    break;
                                case SensorType.Timing:
                                    format = "{0:F3} ns";
                                    break;
                                case SensorType.Energy:
                                    format = "{0:F0} mWh";
                                    break;
                                case SensorType.Noise:
                                    format = "{0:F0} dBA";
                                    break;
                                case SensorType.Conductivity:
                                    format = "{0:F1} µS/cm";
                                    break;
                            }
    
                            if (sensor.SensorType == SensorType.Temperature && _unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit)
                            {
                                formatted = $"{UnitManager.CelsiusToFahrenheit(sensor.Value):F1} °F";
                            }
                            else if (sensor.SensorType == SensorType.Throughput)
                            {
                                string result;
                                switch (sensor.Name)
                                {
                                    case "Connection Speed":
                                        {
                                            switch (sensor.Value)
                                            {
                                                case 100000000:
                                                    result = "100Mbps";
                                                    break;
                                                case 1000000000:
                                                    result = "1Gbps";
                                                    break;
                                                default:
                                                    {
                                                        if (sensor.Value < 1024)
                                                            result = $"{sensor.Value:F0} bps";
                                                        else if (sensor.Value < 1048576)
                                                            result = $"{sensor.Value / 1024:F1} Kbps";
                                                        else if (sensor.Value < 1073741824)
                                                            result = $"{sensor.Value / 1048576:F1} Mbps";
                                                        else
                                                            result = $"{sensor.Value / 1073741824:F1} Gbps";
                                                    }
                                                    break;
                                            }
                                        }
                                        break;
                                    default:
                                        {
                                            if (sensor.Value < 1048576)
                                                result = $"{sensor.Value / 1024:F1} KB/s";
                                            else
                                                result = $"{sensor.Value / 1048576:F1} MB/s";
                                        }
                                        break;
                                }
                                formatted = result;
                            }
                            else if (sensor.SensorType == SensorType.TimeSpan)
                            {
                                formatted = string.Format(format, TimeSpan.FromSeconds(sensor.Value.Value));
                            }
                            else
                            {
                                formatted = string.Format(format, sensor.Value);
                            }
                        }
                        else
                        {
                            formatted = "-";
                        }
    
                        g.DrawString(formatted, _smallFont, _textBrush, new RectangleF(-1, y - 1, w - _rightMargin + 3, 0), _alignRightStringFormat);
    
                        remainingWidth = w - (int)Math.Floor(g.MeasureString(formatted, _smallFont, w, StringFormat.GenericTypographic).Width) - _rightMargin;
                    }
                    else
                    {
                        DrawProgress(g, w - _progressWidth - _rightMargin, y + 0.35f * _sensorLineHeight, _progressWidth, 0.6f * _sensorLineHeight, 0.01f * sensor.Value.Value);
                        remainingWidth = w - _progressWidth - _rightMargin;
                    }
    
                    remainingWidth -= _leftMargin + 2;
                    if (remainingWidth > 0)
                    {
                        g.DrawString(sensor.Name, _smallFont, _textBrush, new RectangleF(_leftMargin - 1, y - 1, remainingWidth, 0), _trimStringFormat);
                    }
                    y += _sensorLineHeight;
                }
            }
        }
        catch (ArgumentException)
        {
            // #1425.
        }
    }

    private class HardwareComparer : IComparer<IHardware>
    {
        public int Compare(IHardware x, IHardware y)
        {
            switch (x)
            {
                case null when y == null:
                    return 0;
                case null:
                    return -1;
            }

            if (y == null)
                return 1;

            if (x.HardwareType != y.HardwareType)
                return x.HardwareType.CompareTo(y.HardwareType);

            return x.Identifier.CompareTo(y.Identifier);
        }
    }
}
