/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2010-2012 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using OpenHardwareMonitor.Hardware;

namespace OpenHardwareMonitor.GUI {
  public class SensorGadget : Gadget {

    private UnitManager unitManager;

    private Image back = Utilities.EmbeddedResources.GetImage("gadget.png");
    private Image image = null;
    private Image fore = null;
    private Image barBack = Utilities.EmbeddedResources.GetImage("barback.png");
    private Image barFore = Utilities.EmbeddedResources.GetImage("barblue.png");
    private const int topBorder = 6;
    private const int bottomBorder = 7;
    private const int leftBorder = 6;
    private const int rightBorder = 7;
    private Image background = new Bitmap(1, 1);

    private readonly float scale;
    private float fontSize;
    private int iconSize;
    private int hardwareLineHeight;
    private int sensorLineHeight;
    private int rightMargin;
    private int leftMargin;
    private int topMargin;
    private int bottomMargin;
    private int progressWidth;

    private IDictionary<IHardware, IList<ISensor>> sensors =
      new SortedDictionary<IHardware, IList<ISensor>>(new HardwareComparer());

    private PersistentSettings settings;
    private UserOption hardwareNames;
    private UserOption alwaysOnTop;
    private UserOption lockPositionAndSize;

    private Font largeFont;
    private Font smallFont;
    private Brush darkWhite;
    private StringFormat stringFormat;
    private StringFormat trimStringFormat;
    private StringFormat alignRightStringFormat;

    public SensorGadget(IComputer computer, PersistentSettings settings, 
      UnitManager unitManager) 
    {
      this.unitManager = unitManager;
      this.settings = settings;
      computer.HardwareAdded += new HardwareEventHandler(HardwareAdded);
      computer.HardwareRemoved += new HardwareEventHandler(HardwareRemoved);      

      this.darkWhite = new SolidBrush(Color.FromArgb(0xF0, 0xF0, 0xF0));

      this.stringFormat = new StringFormat();
      this.stringFormat.FormatFlags = StringFormatFlags.NoWrap;

      this.trimStringFormat = new StringFormat();
      this.trimStringFormat.Trimming = StringTrimming.EllipsisCharacter;
      this.trimStringFormat.FormatFlags = StringFormatFlags.NoWrap;

      this.alignRightStringFormat = new StringFormat();
      this.alignRightStringFormat.Alignment = StringAlignment.Far;
      this.alignRightStringFormat.FormatFlags = StringFormatFlags.NoWrap;

      if (File.Exists("gadget_background.png")) {
        try { 
          Image newBack = new Bitmap("gadget_background.png");
          back.Dispose();
          back = newBack;
        } catch { }
      }

      if (File.Exists("gadget_image.png")) {
        try {
          image = new Bitmap("gadget_image.png"); 
        } catch {}
      }

      if (File.Exists("gadget_foreground.png")) {
        try {
          fore = new Bitmap("gadget_foreground.png"); 
        } catch { }
      }

      if (File.Exists("gadget_bar_background.png")) {
        try {
          Image newBarBack = new Bitmap("gadget_bar_background.png");
          barBack.Dispose();
          barBack = newBarBack;
        } catch { }
      }

      if (File.Exists("gadget_bar_foreground.png")) {
        try {
          Image newBarColor = new Bitmap("gadget_bar_foreground.png");
          barFore.Dispose();
          barFore = newBarColor;
        } catch { }
      }

      this.Location = new Point(
        settings.GetValue("sensorGadget.Location.X", 100),
        settings.GetValue("sensorGadget.Location.Y", 100)); 
      LocationChanged += delegate(object sender, EventArgs e) {
        settings.SetValue("sensorGadget.Location.X", Location.X);
        settings.SetValue("sensorGadget.Location.Y", Location.Y);
      };

      // get the custom to default dpi ratio
      using (Bitmap b = new Bitmap(1, 1)) {
        scale = b.HorizontalResolution / 96.0f;
      }

      SetFontSize(settings.GetValue("sensorGadget.FontSize", 7.5f));
      Resize(settings.GetValue("sensorGadget.Width", Size.Width));
      
      ContextMenu contextMenu = new ContextMenu();
      MenuItem hardwareNamesItem = new MenuItem("Hardware Names");
      contextMenu.MenuItems.Add(hardwareNamesItem);
      MenuItem fontSizeMenu = new MenuItem("Font Size");
      for (int i = 0; i < 4; i++) {
        float size;
        string name;
        switch (i) {
          case 0: size = 6.5f; name = "Small"; break;
          case 1: size = 7.5f; name = "Medium"; break;
          case 2: size = 9f; name = "Large"; break;
          case 3: size = 11f; name = "Very Large"; break;
          default: throw new NotImplementedException();
        }
        MenuItem item = new MenuItem(name);
        item.Checked = fontSize == size;
        item.Click += delegate(object sender, EventArgs e) {
          SetFontSize(size);
          settings.SetValue("sensorGadget.FontSize", size);
          foreach (MenuItem mi in fontSizeMenu.MenuItems)
            mi.Checked = mi == item;
        };
        fontSizeMenu.MenuItems.Add(item);
      }
      contextMenu.MenuItems.Add(fontSizeMenu);
      contextMenu.MenuItems.Add(new MenuItem("-"));
      MenuItem lockItem = new MenuItem("Lock Position and Size");
      contextMenu.MenuItems.Add(lockItem);
      contextMenu.MenuItems.Add(new MenuItem("-"));
      MenuItem alwaysOnTopItem = new MenuItem("Always on Top");
      contextMenu.MenuItems.Add(alwaysOnTopItem);
      MenuItem opacityMenu = new MenuItem("Opacity");
      contextMenu.MenuItems.Add(opacityMenu);
      Opacity = (byte)settings.GetValue("sensorGadget.Opacity", 255);      
      for (int i = 0; i < 5; i++) {
        MenuItem item = new MenuItem((20 * (i + 1)).ToString() + " %");
        byte o = (byte)(51 * (i + 1));
        item.Checked = Opacity == o;
        item.Click += delegate(object sender, EventArgs e) {
          Opacity = o;
          settings.SetValue("sensorGadget.Opacity", Opacity);
          foreach (MenuItem mi in opacityMenu.MenuItems)
            mi.Checked = mi == item;          
        };
        opacityMenu.MenuItems.Add(item);
      }
      this.ContextMenu = contextMenu;

      hardwareNames = new UserOption("sensorGadget.Hardwarenames", true,
        hardwareNamesItem, settings);
      hardwareNames.Changed += delegate(object sender, EventArgs e) {
        Resize();
      };

      alwaysOnTop = new UserOption("sensorGadget.AlwaysOnTop", false, 
        alwaysOnTopItem, settings);
      alwaysOnTop.Changed += delegate(object sender, EventArgs e) {
        this.AlwaysOnTop = alwaysOnTop.Value;
      };
      lockPositionAndSize = new UserOption("sensorGadget.LockPositionAndSize", 
        false, lockItem, settings);
      lockPositionAndSize.Changed += delegate(object sender, EventArgs e) {
        this.LockPositionAndSize = lockPositionAndSize.Value;
      };

      HitTest += delegate(object sender, HitTestEventArgs e) {
        if (lockPositionAndSize.Value)
          return;

        if (e.Location.X < leftBorder) {
          e.HitResult = HitResult.Left;
          return;
        }
        if (e.Location.X > Size.Width - 1 - rightBorder) {
          e.HitResult = HitResult.Right;
          return;
        }
      };

      SizeChanged += delegate(object sender, EventArgs e) {
        settings.SetValue("sensorGadget.Width", Size.Width);
        Redraw();
      };

      VisibleChanged += delegate(object sender, EventArgs e) {
        Rectangle bounds = new Rectangle(Location, Size);
        Screen screen = Screen.FromRectangle(bounds);
        Rectangle intersection = 
          Rectangle.Intersect(screen.WorkingArea, bounds);
        if (intersection.Width < Math.Min(16, bounds.Width) || 
            intersection.Height < Math.Min(16, bounds.Height)) 
        {
          Location = new Point(
            screen.WorkingArea.Width / 2 - bounds.Width / 2, 
            screen.WorkingArea.Height / 2 - bounds.Height / 2);
        }
      };

      MouseDoubleClick += delegate(object obj, MouseEventArgs args) {
        SendHideShowCommand();
      };
    }

    public override void Dispose() {

      largeFont.Dispose();
      largeFont = null;

      smallFont.Dispose();
      smallFont = null;

      darkWhite.Dispose();
      darkWhite = null;

      stringFormat.Dispose();
      stringFormat = null;

      trimStringFormat.Dispose();
      trimStringFormat = null;

      alignRightStringFormat.Dispose();
      alignRightStringFormat = null;     
 
      back.Dispose();
      back = null;

      barFore.Dispose();
      barFore = null;

      barBack.Dispose();
      barBack = null;

      background.Dispose();
      background = null;

      if (image != null) {
        image.Dispose();
        image = null;
      }

      if (fore != null) {
        fore.Dispose();
        fore = null;
      }

      base.Dispose();
    }

    private void HardwareRemoved(IHardware hardware) {
      hardware.SensorAdded -= new SensorEventHandler(SensorAdded);
      hardware.SensorRemoved -= new SensorEventHandler(SensorRemoved);
      foreach (ISensor sensor in hardware.Sensors)
        SensorRemoved(sensor);
      foreach (IHardware subHardware in hardware.SubHardware)
        HardwareRemoved(subHardware);
    }

    private void HardwareAdded(IHardware hardware) {
      foreach (ISensor sensor in hardware.Sensors)
        SensorAdded(sensor);
      hardware.SensorAdded += new SensorEventHandler(SensorAdded);
      hardware.SensorRemoved += new SensorEventHandler(SensorRemoved);
      foreach (IHardware subHardware in hardware.SubHardware)
        HardwareAdded(subHardware);
    }

    private void SensorAdded(ISensor sensor) {
      if (settings.GetValue(new Identifier(sensor.Identifier,
        "gadget").ToString(), false)) 
        Add(sensor);
    }

    private void SensorRemoved(ISensor sensor) {
      if (Contains(sensor))
        Remove(sensor, false);
    }

    public bool Contains(ISensor sensor) {
      foreach (IList<ISensor> list in sensors.Values)
        if (list.Contains(sensor))
          return true;
      return false;
    }

    public void Add(ISensor sensor) {
      if (Contains(sensor)) {
        return;
      } else {
        // get the right hardware
        IHardware hardware = sensor.Hardware;
        while (hardware.Parent != null)
          hardware = hardware.Parent;

        // get the sensor list associated with the hardware
        IList<ISensor> list;
        if (!sensors.TryGetValue(hardware, out list)) {
          list = new List<ISensor>();
          sensors.Add(hardware, list);
        }

        // insert the sensor at the right position
        int i = 0;
        while (i < list.Count && (list[i].SensorType < sensor.SensorType || 
          (list[i].SensorType == sensor.SensorType && 
           list[i].Index < sensor.Index))) i++;
        list.Insert(i, sensor);

        settings.SetValue(
          new Identifier(sensor.Identifier, "gadget").ToString(), true);
        
        Resize();
      }
    }

    public void Remove(ISensor sensor) {
      Remove(sensor, true);
    }

    private void Remove(ISensor sensor, bool deleteConfig) {
      if (deleteConfig) 
        settings.Remove(new Identifier(sensor.Identifier, "gadget").ToString());

      foreach (KeyValuePair<IHardware, IList<ISensor>> keyValue in sensors)
        if (keyValue.Value.Contains(sensor)) {
          keyValue.Value.Remove(sensor);          
          if (keyValue.Value.Count == 0) {
            sensors.Remove(keyValue.Key);
            break;
          }
        }
      Resize();
    }

    public event EventHandler HideShowCommand;

    public void SendHideShowCommand() {
      if (HideShowCommand != null)
        HideShowCommand(this, null);
    }

    private Font CreateFont(float size, FontStyle style) {
      try {
        return new Font(SystemFonts.MessageBoxFont.FontFamily, size, style);
      } catch (ArgumentException) {
        // if the style is not supported, fall back to the original one
        return new Font(SystemFonts.MessageBoxFont.FontFamily, size, 
          SystemFonts.MessageBoxFont.Style);
      }
    }

    private void SetFontSize(float size) {
      fontSize = size;
      largeFont = CreateFont(fontSize, FontStyle.Bold);
      smallFont = CreateFont(fontSize, FontStyle.Regular);
      
      double scaledFontSize = fontSize * scale;
      iconSize = (int)Math.Round(1.5 * scaledFontSize);
      hardwareLineHeight = (int)Math.Round(1.66 * scaledFontSize);
      sensorLineHeight = (int)Math.Round(1.33 * scaledFontSize);
      leftMargin = leftBorder + (int)Math.Round(0.3 * scaledFontSize);
      rightMargin = rightBorder + (int)Math.Round(0.3 * scaledFontSize);
      topMargin = topBorder;
      bottomMargin = bottomBorder + (int)Math.Round(0.3 * scaledFontSize);
      progressWidth = (int)Math.Round(5.3 * scaledFontSize);

      Resize((int)Math.Round(17.3 * scaledFontSize));
    }

    private void Resize() {
      Resize(this.Size.Width);
    }

    private void Resize(int width) {
      int y = topMargin;      
      foreach (KeyValuePair<IHardware, IList<ISensor>> pair in sensors) {
        if (hardwareNames.Value) {
          if (y > topMargin)
            y += hardwareLineHeight - sensorLineHeight;
          y += hardwareLineHeight;
        }
        y += pair.Value.Count * sensorLineHeight;
      }      
      if (sensors.Count == 0)
        y += 4 * sensorLineHeight + hardwareLineHeight;
      y += bottomMargin;
      this.Size = new Size(width, y);
    }

    private void DrawImageWidthBorder(Graphics g, int width, int height, 
      Image back, int t, int b, int l, int r) 
    {
      GraphicsUnit u = GraphicsUnit.Pixel;

      g.DrawImage(back, new Rectangle(0, 0, l, t),
            new Rectangle(0, 0, l, t), u);
      g.DrawImage(back, new Rectangle(l, 0, width - l - r, t),
        new Rectangle(l, 0, back.Width - l - r, t), u);
      g.DrawImage(back, new Rectangle(width - r, 0, r, t),
        new Rectangle(back.Width - r, 0, r, t), u);

      g.DrawImage(back, new Rectangle(0, t, l, height - t - b),
        new Rectangle(0, t, l, back.Height - t - b), u);
      g.DrawImage(back, new Rectangle(l, t, width - l - r, height - t - b),
        new Rectangle(l, t, back.Width - l - r, back.Height - t - b), u);
      g.DrawImage(back, new Rectangle(width - r, t, r, height - t - b),
        new Rectangle(back.Width - r, t, r, back.Height - t - b), u);

      g.DrawImage(back, new Rectangle(0, height - b, l, b),
        new Rectangle(0, back.Height - b, l, b), u);
      g.DrawImage(back, new Rectangle(l, height - b, width - l - r, b),
        new Rectangle(l, back.Height - b, back.Width - l - r, b), u);
      g.DrawImage(back, new Rectangle(width - r, height - b, r, b),
        new Rectangle(back.Width - r, back.Height - b, r, b), u);
    }

    private void DrawBackground(Graphics g) {
      int w = Size.Width;
      int h = Size.Height;      

      if (w != background.Width || h != background.Height) {

        background.Dispose();
        background = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
        using (Graphics graphics = Graphics.FromImage(background)) {

          DrawImageWidthBorder(graphics, w, h, back, topBorder, bottomBorder, 
            leftBorder, rightBorder);    
      
          if (fore != null)
            DrawImageWidthBorder(graphics, w, h, fore, topBorder, bottomBorder,
            leftBorder, rightBorder);

          if (image != null) {
            int width = w - leftBorder - rightBorder;
            int height = h - topBorder - bottomBorder;
            float xRatio = width / (float)image.Width;
            float yRatio = height / (float)image.Height;
            float destWidth, destHeight;
            float xOffset, yOffset;
            if (xRatio < yRatio) {
              destWidth = width;
              destHeight = image.Height * xRatio;
              xOffset = 0;
              yOffset = 0.5f * (height - destHeight);
            } else {
              destWidth = image.Width * yRatio;
              destHeight = height;
              xOffset = 0.5f * (width - destWidth);
              yOffset = 0;
            }

            graphics.DrawImage(image,
              new RectangleF(leftBorder + xOffset, topBorder + yOffset, 
                destWidth, destHeight));
          }
        }
      }

      g.DrawImageUnscaled(background, 0, 0);
    }

    private void DrawProgress(Graphics g, float x, float y, 
      float width, float height, float progress) 
    {
      g.DrawImage(barBack, 
        new RectangleF(x + width * progress, y, width * (1 - progress), height), 
        new RectangleF(barBack.Width * progress, 0, 
          (1 - progress) * barBack.Width, barBack.Height), 
        GraphicsUnit.Pixel);
      g.DrawImage(barFore,
        new RectangleF(x, y, width * progress, height),
        new RectangleF(0, 0, progress * barFore.Width, barFore.Height),
        GraphicsUnit.Pixel);
    }

    protected override void OnPaint(PaintEventArgs e) {
      Graphics g = e.Graphics;
      int w = Size.Width;

      g.Clear(Color.Transparent);
      
      DrawBackground(g);

      int x;
      int y = topMargin;

      if (sensors.Count == 0) {
        x = leftBorder + 1;
        g.DrawString("Right-click on a sensor in the main window and select " + 
          "\"Show in Gadget\" to show the sensor here.", 
          smallFont, Brushes.White,
          new Rectangle(x, y - 1, w - rightBorder - x, 0));
      }

      foreach (KeyValuePair<IHardware, IList<ISensor>> pair in sensors) {
        if (hardwareNames.Value) {
          if (y > topMargin)
            y += hardwareLineHeight - sensorLineHeight;
          x = leftBorder + 1;
          g.DrawImage(HardwareTypeImage.Instance.GetImage(pair.Key.HardwareType),
            new Rectangle(x, y + 1, iconSize, iconSize));
          x += iconSize + 1;
          g.DrawString(pair.Key.Name, largeFont, Brushes.White,
            new Rectangle(x, y - 1, w - rightBorder - x, 0), 
            stringFormat);
          y += hardwareLineHeight;
        }

        foreach (ISensor sensor in pair.Value) {
          int remainingWidth;


          if ((sensor.SensorType != SensorType.Load &&
               sensor.SensorType != SensorType.Control &&
               sensor.SensorType != SensorType.Level) || !sensor.Value.HasValue) 
          {
            string formatted;

            if (sensor.Value.HasValue) {
              string format = "";
              switch (sensor.SensorType) {
                case SensorType.Voltage:
                  format = "{0:F3} V";
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
                case SensorType.SmallData:                  format = "{0:F0} MB";
                  break;
                case SensorType.Factor:
                  format = "{0:F3}";
                  break;
              }

              if (sensor.SensorType == SensorType.Temperature &&
                unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit) {
                formatted = string.Format("{0:F1} °F",
                  UnitManager.CelsiusToFahrenheit(sensor.Value));
              } else if(sensor.SensorType == SensorType.Throughput)
                            {
                string result = "-";
                switch (sensor.Name){ 
                  case "Connection Speed": {
                    switch (sensor.Value){ 
                      case 100000000:
                          result = "100Mbps";
                          break;
                      case 1000000000:
                          result = "1Gbps";
                          break;
                      default: {
                        if (sensor.Value < 1024)
                            result = string.Format("{0:F0} bps", sensor.Value);
                        else if(sensor.Value < 1048576)
                            result = string.Format("{0:F1} Kbps", sensor.Value / 1024);
                        else if(sensor.Value < 1073741824)
                            result = string.Format("{0:F1} Mbps", sensor.Value / 1048576);
                        else
                            result = string.Format("{0:F1} Gbps", sensor.Value / 1073741824);
                      }
                      break;
                    }
                  }
                  break;
                  default:{
                    if (sensor.Value < 1048576)
                        result = string.Format("{0:F1} KB/s", sensor.Value / 1024);
                    else
                        result = string.Format("{0:F1} MB/s", sensor.Value / 1048576);
                  }
                  break;
                }
                formatted =  result;
              } else {
                formatted = string.Format(format, sensor.Value);
              }
            } else {
              formatted = "-";
            }

            g.DrawString(formatted, smallFont, darkWhite,
              new RectangleF(-1, y - 1, w - rightMargin + 3, 0),
              alignRightStringFormat);

            remainingWidth = w - (int)Math.Floor(g.MeasureString(formatted,
              smallFont, w, StringFormat.GenericTypographic).Width) -
              rightMargin;
          } else {
            DrawProgress(g, w - progressWidth - rightMargin,
              y + 0.35f * sensorLineHeight, progressWidth,
              0.6f * sensorLineHeight, 0.01f * sensor.Value.Value);

            remainingWidth = w - progressWidth - rightMargin;
          }
           
          remainingWidth -= leftMargin + 2;
          if (remainingWidth > 0) {
            g.DrawString(sensor.Name, smallFont, darkWhite,
              new RectangleF(leftMargin - 1, y - 1, remainingWidth, 0), 
              trimStringFormat);
          }

          y += sensorLineHeight;
        }
      }
    }

    private class HardwareComparer : IComparer<IHardware> {
      public int Compare(IHardware x, IHardware y) {
        if (x == null && y == null)
          return 0;
        if (x == null)
          return -1;
        if (y == null)
          return 1;

        if (x.HardwareType != y.HardwareType)
          return x.HardwareType.CompareTo(y.HardwareType);

        return x.Identifier.CompareTo(y.Identifier);
      }
    }
  }
}

