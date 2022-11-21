// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;
using LibreHardwareMonitor.Wmi;

namespace LibreHardwareMonitor.UI;

public sealed partial class MainForm : Form
{
    private readonly UserOption _autoStart;
    private readonly Computer _computer;
    private readonly SensorGadget _gadget;
    private readonly Logger _logger;
    private readonly UserRadioGroup _loggingInterval;
    private readonly UserRadioGroup _updateInterval;
    private readonly UserOption _logSensors;
    private readonly UserOption _minimizeOnClose;
    private readonly UserOption _minimizeToTray;
    private readonly Color[] _plotColorPalette;
    private readonly PlotPanel _plotPanel;
    private readonly UserOption _readBatterySensors;
    private readonly UserOption _readCpuSensors;
    private readonly UserOption _readFanControllersSensors;
    private readonly UserOption _readGpuSensors;
    private readonly UserOption _readHddSensors;
    private readonly UserOption _readMainboardSensors;
    private readonly UserOption _readNicSensors;
    private readonly UserOption _readPsuSensors;
    private readonly UserOption _readRamSensors;
    private readonly Node _root;
    private readonly UserOption _runWebServer;
    private readonly UserRadioGroup _sensorValuesTimeWindow;
    private readonly PersistentSettings _settings;
    private readonly UserOption _showGadget;
    private readonly StartupManager _startupManager = new();
    private readonly SystemTray _systemTray;
    private readonly UnitManager _unitManager;
    private readonly UpdateVisitor _updateVisitor = new();
    private readonly WmiProvider _wmiProvider;

    private int _delayCount;
    private Form _plotForm;
    private UserRadioGroup _plotLocation;
    private bool _selectionDragging;
    private IDictionary<ISensor, Color> _sensorPlotColors = new Dictionary<ISensor, Color>();
    private UserOption _showPlot;

    public MainForm()
    {
        InitializeComponent();

        _settings = new PersistentSettings();
        _settings.Load(Path.ChangeExtension(Application.ExecutablePath, ".config"));

        _unitManager = new UnitManager(_settings);

        // make sure the buffers used for double buffering are not disposed
        // after each draw call
        BufferedGraphicsManager.Current.MaximumBuffer = Screen.PrimaryScreen.Bounds.Size;

        // set the DockStyle here, to avoid conflicts with the MainMenu
        splitContainer.Dock = DockStyle.Fill;

        Font = SystemFonts.MessageBoxFont;
        treeView.Font = SystemFonts.MessageBoxFont;

        // Set the bounds immediately, so that our child components can be
        // properly placed.
        Bounds = new Rectangle
        {
            X = _settings.GetValue("mainForm.Location.X", Location.X),
            Y = _settings.GetValue("mainForm.Location.Y", Location.Y),
            Width = _settings.GetValue("mainForm.Width", 470),
            Height = _settings.GetValue("mainForm.Height", 640)
        };

        _plotPanel = new PlotPanel(_settings, _unitManager) { Font = SystemFonts.MessageBoxFont, Dock = DockStyle.Fill };

        nodeCheckBox.IsVisibleValueNeeded += NodeCheckBox_IsVisibleValueNeeded;
        nodeTextBoxText.DrawText += NodeTextBoxText_DrawText;
        nodeTextBoxValue.DrawText += NodeTextBoxText_DrawText;
        nodeTextBoxMin.DrawText += NodeTextBoxText_DrawText;
        nodeTextBoxMax.DrawText += NodeTextBoxText_DrawText;
        nodeTextBoxText.EditorShowing += NodeTextBoxText_EditorShowing;

        foreach (TreeColumn column in treeView.Columns)
            column.Width = Math.Max(20, Math.Min(400, _settings.GetValue("treeView.Columns." + column.Header + ".Width", column.Width)));

        TreeModel treeModel = new();
        _root = new Node(Environment.MachineName) { Image = EmbeddedResources.GetImage("computer.png") };

        treeModel.Nodes.Add(_root);
        treeView.Model = treeModel;

        _computer = new Computer(_settings);

        _systemTray = new SystemTray(_computer, _settings, _unitManager);
        _systemTray.HideShowCommand += HideShowClick;
        _systemTray.ExitCommand += ExitClick;

        if (Software.OperatingSystem.IsUnix)
        {
            // Unix
            treeView.RowHeight = Math.Max(treeView.RowHeight, 18);
            splitContainer.BorderStyle = BorderStyle.None;
            splitContainer.Border3DStyle = Border3DStyle.Adjust;
            splitContainer.SplitterWidth = 4;
            treeView.BorderStyle = BorderStyle.Fixed3D;
            _plotPanel.BorderStyle = BorderStyle.Fixed3D;
            gadgetMenuItem.Visible = false;
            minCloseMenuItem.Visible = false;
            minTrayMenuItem.Visible = false;
            startMinMenuItem.Visible = false;
        }
        else
        {
            // Windows
            treeView.RowHeight = Math.Max(treeView.Font.Height + 1, 18);
            _gadget = new SensorGadget(_computer, _settings, _unitManager);
            _gadget.HideShowCommand += HideShowClick;
            _wmiProvider = new WmiProvider(_computer);
        }

        treeView.ShowNodeToolTips = true;
        NodeToolTipProvider tooltipProvider = new();
        nodeTextBoxText.ToolTipProvider = tooltipProvider;
        nodeTextBoxValue.ToolTipProvider = tooltipProvider;
        _logger = new Logger(_computer);

        _plotColorPalette = new Color[13];
        _plotColorPalette[0] = Color.Blue;
        _plotColorPalette[1] = Color.OrangeRed;
        _plotColorPalette[2] = Color.Green;
        _plotColorPalette[3] = Color.LightSeaGreen;
        _plotColorPalette[4] = Color.Goldenrod;
        _plotColorPalette[5] = Color.DarkViolet;
        _plotColorPalette[6] = Color.YellowGreen;
        _plotColorPalette[7] = Color.SaddleBrown;
        _plotColorPalette[8] = Color.RoyalBlue;
        _plotColorPalette[9] = Color.DeepPink;
        _plotColorPalette[10] = Color.MediumSeaGreen;
        _plotColorPalette[11] = Color.Olive;
        _plotColorPalette[12] = Color.Firebrick;

        _computer.HardwareAdded += HardwareAdded;
        _computer.HardwareRemoved += HardwareRemoved;
        _computer.Open();

        backgroundUpdater.DoWork += BackgroundUpdater_DoWork;
        timer.Enabled = true;

        UserOption showHiddenSensors = new("hiddenMenuItem", false, hiddenMenuItem, _settings);
        showHiddenSensors.Changed += delegate { treeModel.ForceVisible = showHiddenSensors.Value; };

        UserOption showValue = new("valueMenuItem", true, valueMenuItem, _settings);
        showValue.Changed += delegate { treeView.Columns[1].IsVisible = showValue.Value; };

        UserOption showMin = new("minMenuItem", false, minMenuItem, _settings);
        showMin.Changed += delegate { treeView.Columns[2].IsVisible = showMin.Value; };

        UserOption showMax = new("maxMenuItem", true, maxMenuItem, _settings);
        showMax.Changed += delegate { treeView.Columns[3].IsVisible = showMax.Value; };

        var _ = new UserOption("startMinMenuItem", false, startMinMenuItem, _settings);
        _minimizeToTray = new UserOption("minTrayMenuItem", true, minTrayMenuItem, _settings);
        _minimizeToTray.Changed += delegate { _systemTray.IsMainIconEnabled = _minimizeToTray.Value; };

        _minimizeOnClose = new UserOption("minCloseMenuItem", false, minCloseMenuItem, _settings);

        _autoStart = new UserOption(null, _startupManager.Startup, startupMenuItem, _settings);
        _autoStart.Changed += delegate
        {
            try
            {
                _startupManager.Startup = _autoStart.Value;
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show("Updating the auto-startup option failed.",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                _autoStart.Value = _startupManager.Startup;
            }
        };

        _readMainboardSensors = new UserOption("mainboardMenuItem", true, mainboardMenuItem, _settings);
        _readMainboardSensors.Changed += delegate { _computer.IsMotherboardEnabled = _readMainboardSensors.Value; };

        _readCpuSensors = new UserOption("cpuMenuItem", true, cpuMenuItem, _settings);
        _readCpuSensors.Changed += delegate { _computer.IsCpuEnabled = _readCpuSensors.Value; };

        _readRamSensors = new UserOption("ramMenuItem", true, ramMenuItem, _settings);
        _readRamSensors.Changed += delegate { _computer.IsMemoryEnabled = _readRamSensors.Value; };

        _readGpuSensors = new UserOption("gpuMenuItem", true, gpuMenuItem, _settings);
        _readGpuSensors.Changed += delegate { _computer.IsGpuEnabled = _readGpuSensors.Value; };

        _readFanControllersSensors = new UserOption("fanControllerMenuItem", true, fanControllerMenuItem, _settings);
        _readFanControllersSensors.Changed += delegate { _computer.IsControllerEnabled = _readFanControllersSensors.Value; };

        _readHddSensors = new UserOption("hddMenuItem", true, hddMenuItem, _settings);
        _readHddSensors.Changed += delegate { _computer.IsStorageEnabled = _readHddSensors.Value; };

        _readNicSensors = new UserOption("nicMenuItem", true, nicMenuItem, _settings);
        _readNicSensors.Changed += delegate { _computer.IsNetworkEnabled = _readNicSensors.Value; };

        _readPsuSensors = new UserOption("psuMenuItem", true, psuMenuItem, _settings);
        _readPsuSensors.Changed += delegate { _computer.IsPsuEnabled = _readPsuSensors.Value; };

        _readBatterySensors = new UserOption("batteryMenuItem", true, batteryMenuItem, _settings);
        _readBatterySensors.Changed += delegate { _computer.IsBatteryEnabled = _readBatterySensors.Value; };

        _showGadget = new UserOption("gadgetMenuItem", false, gadgetMenuItem, _settings);
        _showGadget.Changed += delegate
        {
            if (_gadget != null)
                _gadget.Visible = _showGadget.Value;
        };

        celsiusMenuItem.Checked = _unitManager.TemperatureUnit == TemperatureUnit.Celsius;
        fahrenheitMenuItem.Checked = !celsiusMenuItem.Checked;

        Server = new HttpServer(_root,
                                _settings.GetValue("listenerPort", 8085),
                                _settings.GetValue("authenticationEnabled", false),
                                _settings.GetValue("authenticationUserName", ""),
                                _settings.GetValue("authenticationPassword", ""));

        if (Server.PlatformNotSupported)
        {
            webMenuItemSeparator.Visible = false;
            webMenuItem.Visible = false;
        }

        _runWebServer = new UserOption("runWebServerMenuItem", false, runWebServerMenuItem, _settings);
        _runWebServer.Changed += delegate
        {
            if (_runWebServer.Value)
                Server.StartHttpListener();
            else
                Server.StopHttpListener();
        };

        authWebServerMenuItem.Checked = _settings.GetValue("authenticationEnabled", false);

        _logSensors = new UserOption("logSensorsMenuItem", false, logSensorsMenuItem, _settings);

        _loggingInterval = new UserRadioGroup("loggingInterval",
                                              0,
                                              new[]
                                              {
                                                  log1sMenuItem,
                                                  log2sMenuItem,
                                                  log5sMenuItem,
                                                  log10sMenuItem,
                                                  log30sMenuItem,
                                                  log1minMenuItem,
                                                  log2minMenuItem,
                                                  log5minMenuItem,
                                                  log10minMenuItem,
                                                  log30minMenuItem,
                                                  log1hMenuItem,
                                                  log2hMenuItem,
                                                  log6hMenuItem
                                              },
                                              _settings);

        _loggingInterval.Changed += (sender, e) =>
        {
            switch (_loggingInterval.Value)
            {
                case 0:
                    _logger.LoggingInterval = new TimeSpan(0, 0, 1);
                    break;
                case 1:
                    _logger.LoggingInterval = new TimeSpan(0, 0, 2);
                    break;
                case 2:
                    _logger.LoggingInterval = new TimeSpan(0, 0, 5);
                    break;
                case 3:
                    _logger.LoggingInterval = new TimeSpan(0, 0, 10);
                    break;
                case 4:
                    _logger.LoggingInterval = new TimeSpan(0, 0, 30);
                    break;
                case 5:
                    _logger.LoggingInterval = new TimeSpan(0, 1, 0);
                    break;
                case 6:
                    _logger.LoggingInterval = new TimeSpan(0, 2, 0);
                    break;
                case 7:
                    _logger.LoggingInterval = new TimeSpan(0, 5, 0);
                    break;
                case 8:
                    _logger.LoggingInterval = new TimeSpan(0, 10, 0);
                    break;
                case 9:
                    _logger.LoggingInterval = new TimeSpan(0, 30, 0);
                    break;
                case 10:
                    _logger.LoggingInterval = new TimeSpan(1, 0, 0);
                    break;
                case 11:
                    _logger.LoggingInterval = new TimeSpan(2, 0, 0);
                    break;
                case 12:
                    _logger.LoggingInterval = new TimeSpan(6, 0, 0);
                    break;
            }
        };

        _updateInterval = new UserRadioGroup("updateIntervalMenuItem",
                                             2,
                                             new[]
                                             {
                                                 updateInterval250msMenuItem,
                                                 updateInterval500msMenuItem,
                                                 updateInterval1sMenuItem,
                                                 updateInterval2sMenuItem,
                                                 updateInterval5sMenuItem,
                                                 updateInterval10sMenuItem
                                             },
                                             _settings);

        _updateInterval.Changed += (sender, e) =>
        {
            switch (_updateInterval.Value)
            {
                case 0:
                    timer.Interval = 250;
                    break;
                case 1:
                    timer.Interval = 500;
                    break;
                case 2:
                    timer.Interval = 1000;
                    break;
                case 3:
                    timer.Interval = 2000;
                    break;
                case 4:
                    timer.Interval = 5000;
                    break;
                case 5:
                    timer.Interval = 10000;
                    break;
            }
        };

        _sensorValuesTimeWindow = new UserRadioGroup("sensorValuesTimeWindow",
                                                     10,
                                                     new[]
                                                     {
                                                         timeWindow30sMenuItem,
                                                         timeWindow1minMenuItem,
                                                         timeWindow2minMenuItem,
                                                         timeWindow5minMenuItem,
                                                         timeWindow10minMenuItem,
                                                         timeWindow30minMenuItem,
                                                         timeWindow1hMenuItem,
                                                         timeWindow2hMenuItem,
                                                         timeWindow6hMenuItem,
                                                         timeWindow12hMenuItem,
                                                         timeWindow24hMenuItem
                                                     },
                                                     _settings);

        _sensorValuesTimeWindow.Changed += (sender, e) =>
        {
            TimeSpan timeWindow = TimeSpan.Zero;
            switch (_sensorValuesTimeWindow.Value)
            {
                case 0:
                    timeWindow = new TimeSpan(0, 0, 30);
                    break;
                case 1:
                    timeWindow = new TimeSpan(0, 1, 0);
                    break;
                case 2:
                    timeWindow = new TimeSpan(0, 2, 0);
                    break;
                case 3:
                    timeWindow = new TimeSpan(0, 5, 0);
                    break;
                case 4:
                    timeWindow = new TimeSpan(0, 10, 0);
                    break;
                case 5:
                    timeWindow = new TimeSpan(0, 30, 0);
                    break;
                case 6:
                    timeWindow = new TimeSpan(1, 0, 0);
                    break;
                case 7:
                    timeWindow = new TimeSpan(2, 0, 0);
                    break;
                case 8:
                    timeWindow = new TimeSpan(6, 0, 0);
                    break;
                case 9:
                    timeWindow = new TimeSpan(12, 0, 0);
                    break;
                case 10:
                    timeWindow = new TimeSpan(24, 0, 0);
                    break;
            }

            _computer.Accept(new SensorVisitor(delegate(ISensor sensor) { sensor.ValuesTimeWindow = timeWindow; }));
        };

        InitializePlotForm();
        InitializeSplitter();

        startupMenuItem.Visible = _startupManager.IsAvailable;

        if (startMinMenuItem.Checked)
        {
            if (!minTrayMenuItem.Checked)
            {
                WindowState = FormWindowState.Minimized;
                Show();
            }
        }
        else
        {
            Show();
        }

        // Create a handle, otherwise calling Close() does not fire FormClosed

        // Make sure the settings are saved when the user logs off
        Microsoft.Win32.SystemEvents.SessionEnded += delegate
        {
            _computer.Close();
            SaveConfiguration();
            if (_runWebServer.Value)
                Server.Quit();
        };

        Microsoft.Win32.SystemEvents.PowerModeChanged += PowerModeChanged;
    }

    public bool AuthWebServerMenuItemChecked
    {
        get { return authWebServerMenuItem.Checked; }
        set { authWebServerMenuItem.Checked = value; }
    }

    public HttpServer Server { get; }

    private void BackgroundUpdater_DoWork(object sender, DoWorkEventArgs e)
    {
        _computer.Accept(_updateVisitor);

        if (_logSensors != null && _logSensors.Value && _delayCount >= 4)
            _logger.Log();

        if (_delayCount < 4)
            _delayCount++;

        _plotPanel.InvalidatePlot();
    }

    private void PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs eventArgs)
    {
        if (eventArgs.Mode == Microsoft.Win32.PowerModes.Resume)
        {
            _computer.Reset();
        }
    }

    private void InitializeSplitter()
    {
        splitContainer.SplitterDistance = _settings.GetValue("splitContainer.SplitterDistance", 400);
        splitContainer.SplitterMoved += delegate { _settings.SetValue("splitContainer.SplitterDistance", splitContainer.SplitterDistance); };
    }

    private void InitializePlotForm()
    {
        _plotForm = new Form { FormBorderStyle = FormBorderStyle.SizableToolWindow, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual };
        AddOwnedForm(_plotForm);
        _plotForm.Bounds = new Rectangle
        {
            X = _settings.GetValue("plotForm.Location.X", -100000),
            Y = _settings.GetValue("plotForm.Location.Y", 100),
            Width = _settings.GetValue("plotForm.Width", 600),
            Height = _settings.GetValue("plotForm.Height", 400)
        };

        _showPlot = new UserOption("plotMenuItem", false, plotMenuItem, _settings);
        _plotLocation = new UserRadioGroup("plotLocation", 0, new[] { plotWindowMenuItem, plotBottomMenuItem, plotRightMenuItem }, _settings);

        _showPlot.Changed += delegate
        {
            if (_plotLocation.Value == 0)
            {
                if (_showPlot.Value && Visible)
                    _plotForm.Show();
                else
                    _plotForm.Hide();
            }
            else
            {
                splitContainer.Panel2Collapsed = !_showPlot.Value;
            }

            treeView.Invalidate();
        };

        _plotLocation.Changed += delegate
        {
            switch (_plotLocation.Value)
            {
                case 0:
                    splitContainer.Panel2.Controls.Clear();
                    splitContainer.Panel2Collapsed = true;
                    _plotForm.Controls.Add(_plotPanel);
                    if (_showPlot.Value && Visible)
                        _plotForm.Show();

                    break;
                case 1:
                    _plotForm.Controls.Clear();
                    _plotForm.Hide();
                    splitContainer.Orientation = Orientation.Horizontal;
                    splitContainer.Panel2.Controls.Add(_plotPanel);
                    splitContainer.Panel2Collapsed = !_showPlot.Value;
                    break;
                case 2:
                    _plotForm.Controls.Clear();
                    _plotForm.Hide();
                    splitContainer.Orientation = Orientation.Vertical;
                    splitContainer.Panel2.Controls.Add(_plotPanel);
                    splitContainer.Panel2Collapsed = !_showPlot.Value;
                    break;
            }
        };

        _plotForm.FormClosing += delegate(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // just switch off the plotting when the user closes the form
                if (_plotLocation.Value == 0)
                {
                    _showPlot.Value = false;
                }

                e.Cancel = true;
            }
        };

        void MoveOrResizePlotForm(object sender, EventArgs e)
        {
            if (_plotForm.WindowState != FormWindowState.Minimized)
            {
                _settings.SetValue("plotForm.Location.X", _plotForm.Bounds.X);
                _settings.SetValue("plotForm.Location.Y", _plotForm.Bounds.Y);
                _settings.SetValue("plotForm.Width", _plotForm.Bounds.Width);
                _settings.SetValue("plotForm.Height", _plotForm.Bounds.Height);
            }
        }

        _plotForm.Move += MoveOrResizePlotForm;
        _plotForm.Resize += MoveOrResizePlotForm;

        _plotForm.VisibleChanged += delegate
        {
            Rectangle bounds = new(_plotForm.Location, _plotForm.Size);
            Screen screen = Screen.FromRectangle(bounds);
            Rectangle intersection = Rectangle.Intersect(screen.WorkingArea, bounds);
            if (intersection.Width < Math.Min(16, bounds.Width) ||
                intersection.Height < Math.Min(16, bounds.Height))
            {
                _plotForm.Location = new Point(screen.WorkingArea.Width / 2 - bounds.Width / 2,
                                               screen.WorkingArea.Height / 2 - bounds.Height / 2);
            }
        };

        VisibleChanged += delegate
        {
            if (Visible && _showPlot.Value && _plotLocation.Value == 0)
                _plotForm.Show();
            else
                _plotForm.Hide();
        };
    }

    private void InsertSorted(IList<Node> nodes, HardwareNode node)
    {
        int i = 0;
        while (i < nodes.Count && nodes[i] is HardwareNode && ((HardwareNode)nodes[i]).Hardware.HardwareType <= node.Hardware.HardwareType)
            i++;

        nodes.Insert(i, node);
    }

    private void SubHardwareAdded(IHardware hardware, Node node)
    {
        HardwareNode hardwareNode = new(hardware, _settings, _unitManager);
        hardwareNode.PlotSelectionChanged += PlotSelectionChanged;
        InsertSorted(node.Nodes, hardwareNode);
        foreach (IHardware subHardware in hardware.SubHardware)
            SubHardwareAdded(subHardware, hardwareNode);
    }

    private void HardwareAdded(IHardware hardware)
    {
        SubHardwareAdded(hardware, _root);
        PlotSelectionChanged(this, null);
    }

    private void HardwareRemoved(IHardware hardware)
    {
        List<HardwareNode> nodesToRemove = new();
        foreach (Node node in _root.Nodes)
        {
            if (node is HardwareNode hardwareNode && hardwareNode.Hardware == hardware)
                nodesToRemove.Add(hardwareNode);
        }

        foreach (HardwareNode hardwareNode in nodesToRemove)
        {
            _root.Nodes.Remove(hardwareNode);
            hardwareNode.PlotSelectionChanged -= PlotSelectionChanged;
        }

        PlotSelectionChanged(this, null);
    }

    private void NodeTextBoxText_DrawText(object sender, DrawEventArgs e)
    {
        if (e.Node.Tag is Node node)
        {
            if (node.IsVisible)
            {
                if (plotMenuItem.Checked && node is SensorNode sensorNode && _sensorPlotColors.TryGetValue(sensorNode.Sensor, out Color color))
                    e.TextColor = color;
            }
            else
                e.TextColor = Color.DarkGray;
        }
    }

    private void PlotSelectionChanged(object sender, EventArgs e)
    {
        List<ISensor> selected = new();
        IDictionary<ISensor, Color> colors = new Dictionary<ISensor, Color>();
        int colorIndex = 0;

        foreach (TreeNodeAdv node in treeView.AllNodes)
        {
            if (node.Tag is SensorNode sensorNode)
            {
                if (sensorNode.Plot)
                {
                    if (!sensorNode.PenColor.HasValue)
                    {
                        colors.Add(sensorNode.Sensor,
                                   _plotColorPalette[colorIndex % _plotColorPalette.Length]);
                    }

                    selected.Add(sensorNode.Sensor);
                }

                colorIndex++;
            }
        }

        // if a sensor is assigned a color that's already being used by another
        // sensor, try to assign it a new color. This is done only after the
        // previous loop sets an unchanging default color for all sensors, so that
        // colors jump around as little as possible as sensors get added/removed
        // from the plot
        var usedColors = new List<Color>();
        foreach (ISensor curSelectedSensor in selected)
        {
            if (!colors.ContainsKey(curSelectedSensor))
                continue;

            Color curColor = colors[curSelectedSensor];
            if (usedColors.Contains(curColor))
            {
                foreach (Color potentialNewColor in _plotColorPalette)
                {
                    if (!colors.Values.Contains(potentialNewColor))
                    {
                        colors[curSelectedSensor] = potentialNewColor;
                        usedColors.Add(potentialNewColor);
                        break;
                    }
                }
            }
            else
            {
                usedColors.Add(curColor);
            }
        }

        foreach (TreeNodeAdv node in treeView.AllNodes)
        {
            if (node.Tag is SensorNode sensorNode && sensorNode.Plot && sensorNode.PenColor.HasValue)
                colors.Add(sensorNode.Sensor, sensorNode.PenColor.Value);
        }

        _sensorPlotColors = colors;
        _plotPanel.SetSensors(selected, colors);
    }

    private void NodeTextBoxText_EditorShowing(object sender, CancelEventArgs e)
    {
        e.Cancel = !(treeView.CurrentNode != null && (treeView.CurrentNode.Tag is SensorNode || treeView.CurrentNode.Tag is HardwareNode));
    }

    private void NodeCheckBox_IsVisibleValueNeeded(object sender, NodeControlValueEventArgs e)
    {
        e.Value = e.Node.Tag is SensorNode && plotMenuItem.Checked;
    }

    private void ExitClick(object sender, EventArgs e)
    {
        CloseApplication();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        treeView.Invalidate();
        _systemTray.Redraw();
        _gadget?.Redraw();
        _wmiProvider?.Update();

        if (!backgroundUpdater.IsBusy)
            backgroundUpdater.RunWorkerAsync();

        RestoreCollapsedNodeState(treeView);
    }

    private void SaveConfiguration()
    {
        if (_plotPanel == null || _settings == null)
            return;

        _plotPanel.SetCurrentSettings();

        foreach (TreeColumn column in treeView.Columns)
            _settings.SetValue("treeView.Columns." + column.Header + ".Width", column.Width);

        _settings.SetValue("listenerPort", Server.ListenerPort);
        _settings.SetValue("authenticationEnabled", Server.AuthEnabled);
        _settings.SetValue("authenticationUserName", Server.UserName);
        _settings.SetValue("authenticationPassword", Server.Password);

        string fileName = Path.ChangeExtension(Application.ExecutablePath, ".config");

        try
        {
            _settings.Save(fileName);
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Access to the path '" +
                            fileName +
                            "' is denied. " +
                            "The current settings could not be saved.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
        }
        catch (IOException)
        {
            MessageBox.Show("The path '" +
                            fileName +
                            "' is not writeable. " +
                            "The current settings could not be saved.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
        }
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        Rectangle newBounds = new()
        {
            X = _settings.GetValue("mainForm.Location.X", Location.X),
            Y = _settings.GetValue("mainForm.Location.Y", Location.Y),
            Width = _settings.GetValue("mainForm.Width", 470),
            Height = _settings.GetValue("mainForm.Height", 640)
        };

        Rectangle fullWorkingArea = new(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);

        foreach (Screen screen in Screen.AllScreens)
            fullWorkingArea = Rectangle.Union(fullWorkingArea, screen.Bounds);

        Rectangle intersection = Rectangle.Intersect(fullWorkingArea, newBounds);
        if (intersection.Width < 20 || intersection.Height < 20 || !_settings.Contains("mainForm.Location.X"))
        {
            newBounds.X = (Screen.PrimaryScreen.WorkingArea.Width / 2) - (newBounds.Width / 2);
            newBounds.Y = (Screen.PrimaryScreen.WorkingArea.Height / 2) - (newBounds.Height / 2);
        }

        Bounds = newBounds;

        RestoreCollapsedNodeState(treeView);

        FormClosed += MainForm_FormClosed;
    }

    private void RestoreCollapsedNodeState(TreeViewAdv treeViewAdv)
    {
        var collapsedHwNodes = treeViewAdv.AllNodes
                                          .Where(n => n.IsExpanded && n.Tag is IExpandPersistNode expandPersistNode && !expandPersistNode.Expanded)
                                          .OrderByDescending(n => n.Level)
                                          .ToList();

        foreach (TreeNodeAdv node in collapsedHwNodes)
        {
            node.IsExpanded = false;
        }
    }

    private void CloseApplication()
    {
        FormClosed -= MainForm_FormClosed;

        Visible = false;
        _systemTray.IsMainIconEnabled = false;
        timer.Enabled = false;
        _computer.Close();
        SaveConfiguration();
        if (_runWebServer.Value)
            Server.Quit();

        _systemTray.Dispose();
        timer.Dispose();
        backgroundUpdater.Dispose();

        Application.Exit();
    }

    private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
    {
        CloseApplication();
    }

    private void AboutMenuItem_Click(object sender, EventArgs e)
    {
        _ = new AboutBox().ShowDialog();
    }

    private void TreeView_Click(object sender, EventArgs e)
    {
        if (!(e is MouseEventArgs m) || (m.Button != MouseButtons.Left && m.Button != MouseButtons.Right))
            return;

        NodeControlInfo info = treeView.GetNodeControlInfoAt(new Point(m.X, m.Y));
        if (m.Button == MouseButtons.Left && info.Node != null)
        {
            if (info.Node.Tag is IExpandPersistNode expandPersistNode)
            {
                expandPersistNode.Expanded = info.Node.IsExpanded;
            }

            return;
        }

        treeView.SelectedNode = info.Node;
        if (info.Node != null)
        {
            if (info.Node.Tag is SensorNode node && node.Sensor != null)
            {
                treeContextMenu.Items.Clear();
                if (node.Sensor.Parameters.Count > 0)
                {
                    ToolStripItem item = new ToolStripMenuItem("Parameters...");
                    item.Click += delegate { ShowParameterForm(node.Sensor); };
                    treeContextMenu.Items.Add(item);
                }

                if (nodeTextBoxText.EditEnabled)
                {
                    ToolStripItem item = new ToolStripMenuItem("Rename");
                    item.Click += delegate { nodeTextBoxText.BeginEdit(); };
                    treeContextMenu.Items.Add(item);
                }

                if (node.IsVisible)
                {
                    ToolStripItem item = new ToolStripMenuItem("Hide");
                    item.Click += delegate { node.IsVisible = false; };
                    treeContextMenu.Items.Add(item);
                }
                else
                {
                    ToolStripItem item = new ToolStripMenuItem("Unhide");
                    item.Click += delegate { node.IsVisible = true; };
                    treeContextMenu.Items.Add(item);
                }

                treeContextMenu.Items.Add(new ToolStripSeparator());
                {
                    ToolStripItem item = new ToolStripMenuItem("Pen Color...");
                    item.Click += delegate
                    {
                        ColorDialog dialog = new() { Color = node.PenColor.GetValueOrDefault() };
                        if (dialog.ShowDialog() == DialogResult.OK)
                            node.PenColor = dialog.Color;
                    };

                    treeContextMenu.Items.Add(item);
                }

                {
                    ToolStripItem item = new ToolStripMenuItem("Reset Pen Color");
                    item.Click += delegate { node.PenColor = null; };
                    treeContextMenu.Items.Add(item);
                }

                treeContextMenu.Items.Add(new ToolStripSeparator());
                {
                    ToolStripMenuItem item = new("Show in Tray") { Checked = _systemTray.Contains(node.Sensor) };
                    item.Click += delegate
                    {
                        if (item.Checked)
                            _systemTray.Remove(node.Sensor);
                        else
                            _systemTray.Add(node.Sensor, true);
                    };

                    treeContextMenu.Items.Add(item);
                }

                if (_gadget != null)
                {
                    ToolStripMenuItem item = new("Show in Gadget") { Checked = _gadget.Contains(node.Sensor) };
                    item.Click += delegate
                    {
                        if (item.Checked)
                        {
                            _gadget.Remove(node.Sensor);
                        }
                        else
                        {
                            _gadget.Add(node.Sensor);
                        }
                    };

                    treeContextMenu.Items.Add(item);
                }

                if (node.Sensor.Control != null)
                {
                    treeContextMenu.Items.Add(new ToolStripSeparator());
                    IControl control = node.Sensor.Control;
                    ToolStripMenuItem controlItem = new("Control");
                    ToolStripItem defaultItem = new ToolStripMenuItem("Default") { Checked = control.ControlMode == ControlMode.Default };
                    controlItem.DropDownItems.Add(defaultItem);
                    defaultItem.Click += delegate { control.SetDefault(); };
                    ToolStripMenuItem manualItem = new("Manual");
                    controlItem.DropDownItems.Add(manualItem);
                    manualItem.Checked = control.ControlMode == ControlMode.Software;
                    for (int i = 0; i <= 100; i += 5)
                    {
                        if (i <= control.MaxSoftwareValue &&
                            i >= control.MinSoftwareValue)
                        {
                            ToolStripMenuItem item = new ToolStripRadioButtonMenuItem(i + " %");
                            manualItem.DropDownItems.Add(item);
                            item.Checked = control.ControlMode == ControlMode.Software && Math.Round(control.SoftwareValue) == i;
                            int softwareValue = i;
                            item.Click += delegate { control.SetSoftware(softwareValue); };
                        }
                    }

                    treeContextMenu.Items.Add(controlItem);
                }

                treeContextMenu.Show(treeView, new Point(m.X, m.Y));
            }

            if (info.Node.Tag is HardwareNode hardwareNode && hardwareNode.Hardware != null)
            {
                treeContextMenu.Items.Clear();

                if (nodeTextBoxText.EditEnabled)
                {
                    ToolStripItem item = new ToolStripMenuItem("Rename");
                    item.Click += delegate { nodeTextBoxText.BeginEdit(); };
                    treeContextMenu.Items.Add(item);
                }

                treeContextMenu.Show(treeView, new Point(m.X, m.Y));
            }
        }
    }

    private void SaveReportMenuItem_Click(object sender, EventArgs e)
    {
        string report = _computer.GetReport();
        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            using (TextWriter w = new StreamWriter(saveFileDialog.FileName))
            {
                w.Write(report);
            }
        }
    }

    private void SysTrayHideShow()
    {
        Visible = !Visible;
        if (Visible)
            Activate();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_SYSCOMMAND = 0x112;
        const int SC_MINIMIZE = 0xF020;
        const int SC_CLOSE = 0xF060;

        if (_minimizeToTray.Value && m.Msg == WM_SYSCOMMAND && m.WParam.ToInt64() == SC_MINIMIZE)
        {
            SysTrayHideShow();
        }
        else if (_minimizeOnClose.Value && m.Msg == WM_SYSCOMMAND && m.WParam.ToInt64() == SC_CLOSE)
        {
            //Apparently the user wants to minimize rather than close
            //Now we still need to check if we're going to the tray or not
            //Note: the correct way to do this would be to send out SC_MINIMIZE,
            //but since the code here is so simple,
            //that would just be a waste of time.
            if (_minimizeToTray.Value)
                SysTrayHideShow();
            else
                WindowState = FormWindowState.Minimized;
        }
        else
        {
            base.WndProc(ref m);
        }
    }

    private void HideShowClick(object sender, EventArgs e)
    {
        SysTrayHideShow();
    }

    private void ShowParameterForm(ISensor sensorForm)
    {
        ParameterForm form = new() { Parameters = sensorForm.Parameters, captionLabel = { Text = sensorForm.Name } };
        form.ShowDialog();
    }

    private void TreeView_NodeMouseDoubleClick(object sender, TreeNodeAdvMouseEventArgs e)
    {
        if (e.Node.Tag is SensorNode node && node.Sensor != null && node.Sensor.Parameters.Count > 0)
            ShowParameterForm(node.Sensor);
    }

    private void CelsiusMenuItem_Click(object sender, EventArgs e)
    {
        celsiusMenuItem.Checked = true;
        fahrenheitMenuItem.Checked = false;
        _unitManager.TemperatureUnit = TemperatureUnit.Celsius;
    }

    private void FahrenheitMenuItem_Click(object sender, EventArgs e)
    {
        celsiusMenuItem.Checked = false;
        fahrenheitMenuItem.Checked = true;
        _unitManager.TemperatureUnit = TemperatureUnit.Fahrenheit;
    }

    private void ResetMinMaxMenuItem_Click(object sender, EventArgs e)
    {
        _computer.Accept(new SensorVisitor(delegate(ISensor sensorClick)
        {
            sensorClick.ResetMin();
            sensorClick.ResetMax();
        }));
    }

    private void resetPlotMenuItem_Click(object sender, EventArgs e)
    {
        _computer.Accept(new SensorVisitor(delegate (ISensor sensorClick)
        {
            sensorClick.ClearValues();
        }));
    }

    private void MainForm_MoveOrResize(object sender, EventArgs e)
    {
        if (WindowState != FormWindowState.Minimized)
        {
            _settings.SetValue("mainForm.Location.X", Bounds.X);
            _settings.SetValue("mainForm.Location.Y", Bounds.Y);
            _settings.SetValue("mainForm.Width", Bounds.Width);
            _settings.SetValue("mainForm.Height", Bounds.Height);
        }
    }

    private void ResetClick(object sender, EventArgs e)
    {
        // disable the fallback MainIcon during reset, otherwise icon visibility
        // might be lost
        _systemTray.IsMainIconEnabled = false;
        _computer.Reset();
        // restore the MainIcon setting
        _systemTray.IsMainIconEnabled = _minimizeToTray.Value;
    }

    private void TreeView_MouseMove(object sender, MouseEventArgs e)
    {
        _selectionDragging &= (e.Button & (MouseButtons.Left | MouseButtons.Right)) > 0;
        if (_selectionDragging)
            treeView.SelectedNode = treeView.GetNodeAt(e.Location);
    }

    private void TreeView_MouseDown(object sender, MouseEventArgs e)
    {
        _selectionDragging = true;
    }

    private void TreeView_MouseUp(object sender, MouseEventArgs e)
    {
        _selectionDragging = false;
    }

    private void ServerPortMenuItem_Click(object sender, EventArgs e)
    {
        new PortForm(this).ShowDialog();
    }

    private void AuthWebServerMenuItem_Click(object sender, EventArgs e)
    {
        new AuthForm(this).ShowDialog();
    }
}