// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.WMI;
using OpenHardwareMonitor.Utilities;

namespace OpenHardwareMonitor.GUI
{
    public partial class MainForm : Form
    {
        private int _delayCount = 0;
        private PersistentSettings _settings;
        private UnitManager _unitManager;
        private Computer _computer;
        private Node _root;
        private TreeModel _treeModel;
        private IDictionary<ISensor, Color> _sensorPlotColors = new Dictionary<ISensor, Color>();
        private Color[] _plotColorPalette;
        private SystemTray _systemTray;
        private StartupManager _startupManager = new StartupManager();
        private UpdateVisitor _updateVisitor = new UpdateVisitor();
        private SensorGadget _gadget;
        private Form _plotForm;
        private PlotPanel _plotPanel;

        private UserOption _showHiddenSensors;
        private UserOption _showPlot;
        private UserOption _showValue;
        private UserOption _showMin;
        private UserOption _showMax;
        private UserOption _startMinimized;
        private UserOption _minimizeToTray;
        private UserOption _minimizeOnClose;
        private UserOption _autoStart;

        private UserOption _readMainboardSensors;
        private UserOption _readCpuSensors;
        private UserOption _readRamSensors;
        private UserOption _readGpuSensors;
        private UserOption _readFanControllersSensors;
        private UserOption _readHddSensors;
        private UserOption _readNicSensors;

        private UserOption _showGadget;
        private UserRadioGroup _plotLocation;
        private WmiProvider _wmiProvider;

        private UserOption _runWebServer;
        private HttpServer _server;

        private UserOption _logSensors;
        private UserRadioGroup _loggingInterval;
        private UserRadioGroup _sensorValuesTimeWindow;
        private Logger _logger;

        private bool _selectionDragging = false;

        public MainForm()
        {
            InitializeComponent();

            // check if the OpenHardwareMonitorLib assembly has the correct version
            if (Assembly.GetAssembly(typeof(Computer)).GetName().Version != Assembly.GetExecutingAssembly().GetName().Version)
            {
                MessageBox.Show(
                  "The version of the file OpenHardwareMonitorLib.dll is incompatible.",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

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

            _plotPanel = new PlotPanel(_settings, _unitManager);
            _plotPanel.Font = SystemFonts.MessageBoxFont;
            _plotPanel.Dock = DockStyle.Fill;

            nodeCheckBox.IsVisibleValueNeeded += NodeCheckBox_IsVisibleValueNeeded;
            nodeTextBoxText.DrawText += NodeTextBoxText_DrawText;
            nodeTextBoxValue.DrawText += NodeTextBoxText_DrawText;
            nodeTextBoxMin.DrawText += NodeTextBoxText_DrawText;
            nodeTextBoxMax.DrawText += NodeTextBoxText_DrawText;
            nodeTextBoxText.EditorShowing += NodeTextBoxText_EditorShowing;

            foreach (TreeColumn column in treeView.Columns)
            {
                column.Width = Math.Max(20, Math.Min(400, _settings.GetValue("treeView.Columns." + column.Header + ".Width", column.Width)));
            }

            _treeModel = new TreeModel();
            _root = new Node(System.Environment.MachineName);
            _root.Image = Utilities.EmbeddedResources.GetImage("computer.png");

            _treeModel.Nodes.Add(_root);
            treeView.Model = _treeModel;

            _computer = new Computer(_settings);

            _systemTray = new SystemTray(_computer, _settings, _unitManager);
            _systemTray.HideShowCommand += HideShowClick;
            _systemTray.ExitCommand += ExitClick;

            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 128))
            { // Unix
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
            { // Windows
                treeView.RowHeight = Math.Max(treeView.Font.Height + 1, 18);
                _gadget = new SensorGadget(_computer, _settings, _unitManager);
                _gadget.HideShowCommand += HideShowClick;
                _wmiProvider = new WmiProvider(_computer);
            }

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

            _computer.HardwareAdded += new HardwareEventHandler(HardwareAdded);
            _computer.HardwareRemoved += new HardwareEventHandler(HardwareRemoved);
            _computer.Open();

            timer.Enabled = true;

            _showHiddenSensors = new UserOption("hiddenMenuItem", false, hiddenMenuItem, _settings);
            _showHiddenSensors.Changed += delegate (object sender, EventArgs e)
            {
                _treeModel.ForceVisible = _showHiddenSensors.Value;
            };

            _showValue = new UserOption("valueMenuItem", true, valueMenuItem, _settings);
            _showValue.Changed += delegate (object sender, EventArgs e)
            {
                treeView.Columns[1].IsVisible = _showValue.Value;
            };

            _showMin = new UserOption("minMenuItem", false, minMenuItem, _settings);
            _showMin.Changed += delegate (object sender, EventArgs e)
            {
                treeView.Columns[2].IsVisible = _showMin.Value;
            };

            _showMax = new UserOption("maxMenuItem", true, maxMenuItem, _settings);
            _showMax.Changed += delegate (object sender, EventArgs e)
            {
                treeView.Columns[3].IsVisible = _showMax.Value;
            };

            _startMinimized = new UserOption("startMinMenuItem", false, startMinMenuItem, _settings);
            _minimizeToTray = new UserOption("minTrayMenuItem", true, minTrayMenuItem, _settings);
            _minimizeToTray.Changed += delegate (object sender, EventArgs e)
            {
                _systemTray.IsMainIconEnabled = _minimizeToTray.Value;
            };

            _minimizeOnClose = new UserOption("minCloseMenuItem", false, minCloseMenuItem, _settings);

            _autoStart = new UserOption(null, _startupManager.Startup, startupMenuItem, _settings);
            _autoStart.Changed += delegate (object sender, EventArgs e)
            {
                try
                {
                    _startupManager.Startup = _autoStart.Value;
                }
                catch (InvalidOperationException)
                {
                    MessageBox.Show("Updating the auto-startup option failed.", "Error",
                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _autoStart.Value = _startupManager.Startup;
                }
            };

            _readMainboardSensors = new UserOption("mainboardMenuItem", true, mainboardMenuItem, _settings);
            _readMainboardSensors.Changed += delegate (object sender, EventArgs e)
            {
                _computer.MainboardEnabled = _readMainboardSensors.Value;
            };

            _readCpuSensors = new UserOption("cpuMenuItem", true, cpuMenuItem, _settings);
            _readCpuSensors.Changed += delegate (object sender, EventArgs e)
            {
                _computer.CPUEnabled = _readCpuSensors.Value;
            };

            _readRamSensors = new UserOption("ramMenuItem", true, ramMenuItem, _settings);
            _readRamSensors.Changed += delegate (object sender, EventArgs e)
            {
                _computer.RAMEnabled = _readRamSensors.Value;
            };

            _readGpuSensors = new UserOption("gpuMenuItem", true, gpuMenuItem, _settings);
            _readGpuSensors.Changed += delegate (object sender, EventArgs e)
            {
                _computer.GPUEnabled = _readGpuSensors.Value;
            };

            _readFanControllersSensors = new UserOption("fanControllerMenuItem", true, fanControllerMenuItem, _settings);
            _readFanControllersSensors.Changed += delegate (object sender, EventArgs e)
            {
                _computer.FanControllerEnabled = _readFanControllersSensors.Value;
            };

            _readHddSensors = new UserOption("hddMenuItem", true, hddMenuItem, _settings);
            _readHddSensors.Changed += delegate (object sender, EventArgs e)
            {
                _computer.HDDEnabled = _readHddSensors.Value;
            };

            _readNicSensors = new UserOption("nicMenuItem", true, nicMenuItem, _settings);
            _readNicSensors.Changed += delegate (object sender, EventArgs e)
            {
                _computer.NICEnabled = _readNicSensors.Value;
            };

            _showGadget = new UserOption("gadgetMenuItem", false, gadgetMenuItem, _settings);
            _showGadget.Changed += delegate (object sender, EventArgs e)
            {
                if (_gadget != null)
                    _gadget.Visible = _showGadget.Value;
            };

            celsiusMenuItem.Checked = _unitManager.TemperatureUnit == TemperatureUnit.Celsius;
            fahrenheitMenuItem.Checked = !celsiusMenuItem.Checked;

            _server = new HttpServer(_root, _settings.GetValue("listenerPort", 8085));
            if (_server.PlatformNotSupported)
            {
                webMenuItemSeparator.Visible = false;
                webMenuItem.Visible = false;
            }

            _runWebServer = new UserOption("runWebServerMenuItem", false, runWebServerMenuItem, _settings);
            _runWebServer.Changed += delegate (object sender, EventArgs e)
            {
                if (_runWebServer.Value)
                    _server.StartHTTPListener();
                else
                    _server.StopHTTPListener();
            };

            _logSensors = new UserOption("logSensorsMenuItem", false, logSensorsMenuItem, _settings);

            _loggingInterval = new UserRadioGroup("loggingInterval", 0,
                new[] { log1sMenuItem, log2sMenuItem, log5sMenuItem, log10sMenuItem,
                log30sMenuItem, log1minMenuItem, log2minMenuItem, log5minMenuItem,
                log10minMenuItem, log30minMenuItem, log1hMenuItem, log2hMenuItem,
                log6hMenuItem}, _settings);
            _loggingInterval.Changed += (sender, e) =>
            {
                switch (_loggingInterval.Value)
                {
                    case 0: _logger.LoggingInterval = new TimeSpan(0, 0, 1); break;
                    case 1: _logger.LoggingInterval = new TimeSpan(0, 0, 2); break;
                    case 2: _logger.LoggingInterval = new TimeSpan(0, 0, 5); break;
                    case 3: _logger.LoggingInterval = new TimeSpan(0, 0, 10); break;
                    case 4: _logger.LoggingInterval = new TimeSpan(0, 0, 30); break;
                    case 5: _logger.LoggingInterval = new TimeSpan(0, 1, 0); break;
                    case 6: _logger.LoggingInterval = new TimeSpan(0, 2, 0); break;
                    case 7: _logger.LoggingInterval = new TimeSpan(0, 5, 0); break;
                    case 8: _logger.LoggingInterval = new TimeSpan(0, 10, 0); break;
                    case 9: _logger.LoggingInterval = new TimeSpan(0, 30, 0); break;
                    case 10: _logger.LoggingInterval = new TimeSpan(1, 0, 0); break;
                    case 11: _logger.LoggingInterval = new TimeSpan(2, 0, 0); break;
                    case 12: _logger.LoggingInterval = new TimeSpan(6, 0, 0); break;
                }
            };

            _sensorValuesTimeWindow = new UserRadioGroup("sensorValuesTimeWindow", 10,
                new[] { timeWindow30sMenuItem, timeWindow1minMenuItem, timeWindow2minMenuItem,
                timeWindow5minMenuItem, timeWindow10minMenuItem, timeWindow30minMenuItem,
                timeWindow1hMenuItem, timeWindow2hMenuItem, timeWindow6hMenuItem,
                timeWindow12hMenuItem, timeWindow24hMenuItem}, _settings);
            _sensorValuesTimeWindow.Changed += (sender, e) =>
            {
                TimeSpan timeWindow = TimeSpan.Zero;
                switch (_sensorValuesTimeWindow.Value)
                {
                    case 0: timeWindow = new TimeSpan(0, 0, 30); break;
                    case 1: timeWindow = new TimeSpan(0, 1, 0); break;
                    case 2: timeWindow = new TimeSpan(0, 2, 0); break;
                    case 3: timeWindow = new TimeSpan(0, 5, 0); break;
                    case 4: timeWindow = new TimeSpan(0, 10, 0); break;
                    case 5: timeWindow = new TimeSpan(0, 30, 0); break;
                    case 6: timeWindow = new TimeSpan(1, 0, 0); break;
                    case 7: timeWindow = new TimeSpan(2, 0, 0); break;
                    case 8: timeWindow = new TimeSpan(6, 0, 0); break;
                    case 9: timeWindow = new TimeSpan(12, 0, 0); break;
                    case 10: timeWindow = new TimeSpan(24, 0, 0); break;
                }

                _computer.Accept(new SensorVisitor(delegate (ISensor sensor)
                {
                    sensor.ValuesTimeWindow = timeWindow;
                }));
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
            IntPtr handle = Handle;

            // Make sure the settings are saved when the user logs off
            Microsoft.Win32.SystemEvents.SessionEnded += delegate
            {
                _computer.Close();
                SaveConfiguration();
                if (_runWebServer.Value)
                    _server.Quit();
            };
        }

        private void InitializeSplitter()
        {
            splitContainer.SplitterDistance = _settings.GetValue("splitContainer.SplitterDistance", 400);
            splitContainer.SplitterMoved += delegate (object sender, System.Windows.Forms.SplitterEventArgs e)
            {
                _settings.SetValue("splitContainer.SplitterDistance", splitContainer.SplitterDistance);
            };
        }

        private void InitializePlotForm()
        {
            _plotForm = new Form();
            _plotForm.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            _plotForm.ShowInTaskbar = false;
            _plotForm.StartPosition = FormStartPosition.Manual;
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

            _showPlot.Changed += delegate (object sender, EventArgs e)
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
            _plotLocation.Changed += delegate (object sender, EventArgs e)
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

            _plotForm.FormClosing += delegate (object sender, FormClosingEventArgs e)
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

            EventHandler moveOrResizePlotForm = delegate (object sender, EventArgs e)
            {
                if (_plotForm.WindowState != FormWindowState.Minimized)
                {
                    _settings.SetValue("plotForm.Location.X", _plotForm.Bounds.X);
                    _settings.SetValue("plotForm.Location.Y", _plotForm.Bounds.Y);
                    _settings.SetValue("plotForm.Width", _plotForm.Bounds.Width);
                    _settings.SetValue("plotForm.Height", _plotForm.Bounds.Height);
                }
            };
            _plotForm.Move += moveOrResizePlotForm;
            _plotForm.Resize += moveOrResizePlotForm;

            _plotForm.VisibleChanged += delegate (object sender, EventArgs e)
            {
                Rectangle bounds = new Rectangle(_plotForm.Location, _plotForm.Size);
                Screen screen = Screen.FromRectangle(bounds);
                Rectangle intersection = Rectangle.Intersect(screen.WorkingArea, bounds);
                if (intersection.Width < Math.Min(16, bounds.Width) ||
                    intersection.Height < Math.Min(16, bounds.Height))
                {
                    _plotForm.Location = new Point(
                      screen.WorkingArea.Width / 2 - bounds.Width / 2,
                      screen.WorkingArea.Height / 2 - bounds.Height / 2);
                }
            };

            VisibleChanged += delegate (object sender, EventArgs e)
            {
                if (Visible && _showPlot.Value && _plotLocation.Value == 0)
                    _plotForm.Show();
                else
                    _plotForm.Hide();
            };
        }

        private void InsertSorted(Collection<Node> nodes, HardwareNode node)
        {
            int i = 0;
            while (i < nodes.Count && nodes[i] is HardwareNode && ((HardwareNode)nodes[i]).Hardware.HardwareType < node.Hardware.HardwareType)
                i++;
            nodes.Insert(i, node);
        }

        private void SubHardwareAdded(IHardware hardware, Node node)
        {
            HardwareNode hardwareNode = new HardwareNode(hardware, _settings, _unitManager);
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
            List<HardwareNode> nodesToRemove = new List<HardwareNode>();
            foreach (Node node in _root.Nodes)
            {
                HardwareNode hardwareNode = node as HardwareNode;
                if (hardwareNode != null && hardwareNode.Hardware == hardware)
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
            Node node = e.Node.Tag as Node;
            if (node != null)
            {
                Color color;
                if (node.IsVisible)
                {
                    SensorNode sensorNode = node as SensorNode;
                    if (plotMenuItem.Checked && sensorNode != null && _sensorPlotColors.TryGetValue(sensorNode.Sensor, out color))
                        e.TextColor = color;
                }
                else
                {
                    e.TextColor = Color.DarkGray;
                }
            }
        }

        private void PlotSelectionChanged(object sender, EventArgs e)
        {
            List<ISensor> selected = new List<ISensor>();
            IDictionary<ISensor, Color> colors = new Dictionary<ISensor, Color>();
            int colorIndex = 0;
            foreach (TreeNodeAdv node in treeView.AllNodes)
            {
                SensorNode sensorNode = node.Tag as SensorNode;
                if (sensorNode != null)
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
            foreach (var curSelectedSensor in selected)
            {
                if (!colors.ContainsKey(curSelectedSensor))
                    continue;
                var curColor = colors[curSelectedSensor];
                if (usedColors.Contains(curColor))
                {
                    foreach (var potentialNewColor in _plotColorPalette)
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
                SensorNode sensorNode = node.Tag as SensorNode;
                if (sensorNode != null && sensorNode.Plot && sensorNode.PenColor.HasValue)
                    colors.Add(sensorNode.Sensor, sensorNode.PenColor.Value);
            }
            _sensorPlotColors = colors;
            _plotPanel.SetSensors(selected, colors);
        }

        private void NodeTextBoxText_EditorShowing(object sender, CancelEventArgs e)
        {
            e.Cancel = !(treeView.CurrentNode != null &&
              (treeView.CurrentNode.Tag is SensorNode ||
               treeView.CurrentNode.Tag is HardwareNode));
        }

        private void NodeCheckBox_IsVisibleValueNeeded(object sender, NodeControlValueEventArgs e)
        {
            SensorNode node = e.Node.Tag as SensorNode;
            e.Value = (node != null) && plotMenuItem.Checked;
        }

        private void ExitClick(object sender, EventArgs e)
        {
            Close();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _computer.Accept(_updateVisitor);
            treeView.Invalidate();
            _plotPanel.InvalidatePlot();
            _systemTray.Redraw();
            if (_gadget != null)
                _gadget.Redraw();
            if (_wmiProvider != null)
                _wmiProvider.Update();
            if (_logSensors != null && _logSensors.Value && _delayCount >= 4)
                _logger.Log();
            if (_delayCount < 4)
                _delayCount++;
        }

        private void SaveConfiguration()
        {
            _plotPanel.SetCurrentSettings();
            foreach (TreeColumn column in treeView.Columns)
            {
                _settings.SetValue("treeView.Columns." + column.Header + ".Width", column.Width);
            }

            _settings.SetValue("listenerPort", _server.ListenerPort);

            string fileName = Path.ChangeExtension(System.Windows.Forms.Application.ExecutablePath, ".config");
            try
            {
                _settings.Save(fileName);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Access to the path '" + fileName + "' is denied. " +
                  "The current settings could not be saved.",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException)
            {
                MessageBox.Show("The path '" + fileName + "' is not writeable. " +
                  "The current settings could not be saved.",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Rectangle newBounds = new Rectangle
            {
                X = _settings.GetValue("mainForm.Location.X", Location.X),
                Y = _settings.GetValue("mainForm.Location.Y", Location.Y),
                Width = _settings.GetValue("mainForm.Width", 470),
                Height = _settings.GetValue("mainForm.Height", 640)
            };

            Rectangle fullWorkingArea = new Rectangle(int.MaxValue, int.MaxValue,
              int.MinValue, int.MinValue);

            foreach (Screen screen in Screen.AllScreens)
                fullWorkingArea = Rectangle.Union(fullWorkingArea, screen.Bounds);

            Rectangle intersection = Rectangle.Intersect(fullWorkingArea, newBounds);
            if (intersection.Width < 20 || intersection.Height < 20 || !_settings.Contains("mainForm.Location.X"))
            {
                newBounds.X = (Screen.PrimaryScreen.WorkingArea.Width / 2) -
                              (newBounds.Width / 2);
                newBounds.Y = (Screen.PrimaryScreen.WorkingArea.Height / 2) -
                              (newBounds.Height / 2);
            }
            Bounds = newBounds;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Visible = false;
            _systemTray.IsMainIconEnabled = false;
            timer.Enabled = false;
            _computer.Close();
            SaveConfiguration();
            if (_runWebServer.Value)
                _server.Quit();
            _systemTray.Dispose();
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog();
        }

        private void TreeView_Click(object sender, EventArgs e)
        {

            MouseEventArgs m = e as MouseEventArgs;
            if (m == null || m.Button != MouseButtons.Right)
                return;

            NodeControlInfo info = treeView.GetNodeControlInfoAt(new Point(m.X, m.Y));
            treeView.SelectedNode = info.Node;
            if (info.Node != null)
            {
                SensorNode node = info.Node.Tag as SensorNode;
                if (node != null && node.Sensor != null)
                {
                    treeContextMenu.MenuItems.Clear();
                    if (node.Sensor.Parameters.Count > 0)
                    {
                        MenuItem item = new MenuItem("Parameters...");
                        item.Click += delegate (object obj, EventArgs args)
                        {
                            ShowParameterForm(node.Sensor);
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    if (nodeTextBoxText.EditEnabled)
                    {
                        MenuItem item = new MenuItem("Rename");
                        item.Click += delegate (object obj, EventArgs args)
                        {
                            nodeTextBoxText.BeginEdit();
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    if (node.IsVisible)
                    {
                        MenuItem item = new MenuItem("Hide");
                        item.Click += delegate (object obj, EventArgs args)
                        {
                            node.IsVisible = false;
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    else
                    {
                        MenuItem item = new MenuItem("Unhide");
                        item.Click += delegate (object obj, EventArgs args)
                        {
                            node.IsVisible = true;
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    treeContextMenu.MenuItems.Add(new MenuItem("-"));
                    {
                        MenuItem item = new MenuItem("Pen Color...");
                        item.Click += delegate (object obj, EventArgs args)
                        {
                            ColorDialog dialog = new ColorDialog();
                            dialog.Color = node.PenColor.GetValueOrDefault();
                            if (dialog.ShowDialog() == DialogResult.OK)
                                node.PenColor = dialog.Color;
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    {
                        MenuItem item = new MenuItem("Reset Pen Color");
                        item.Click += delegate (object obj, EventArgs args)
                        {
                            node.PenColor = null;
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    treeContextMenu.MenuItems.Add(new MenuItem("-"));
                    {
                        MenuItem item = new MenuItem("Show in Tray");
                        item.Checked = _systemTray.Contains(node.Sensor);
                        item.Click += delegate (object obj, EventArgs args)
                        {
                            if (item.Checked)
                                _systemTray.Remove(node.Sensor);
                            else
                                _systemTray.Add(node.Sensor, true);
                        };
                        treeContextMenu.MenuItems.Add(item);
                    }
                    if (_gadget != null)
                    {
                        MenuItem item = new MenuItem("Show in Gadget");
                        item.Checked = _gadget.Contains(node.Sensor);
                        item.Click += delegate (object obj, EventArgs args)
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
                        treeContextMenu.MenuItems.Add(item);
                    }
                    if (node.Sensor.Control != null)
                    {
                        treeContextMenu.MenuItems.Add(new MenuItem("-"));
                        IControl control = node.Sensor.Control;
                        MenuItem controlItem = new MenuItem("Control");
                        MenuItem defaultItem = new MenuItem("Default");
                        defaultItem.Checked = control.ControlMode == ControlMode.Default;
                        controlItem.MenuItems.Add(defaultItem);
                        defaultItem.Click += delegate (object obj, EventArgs args)
                        {
                            control.SetDefault();
                        };
                        MenuItem manualItem = new MenuItem("Manual");
                        controlItem.MenuItems.Add(manualItem);
                        manualItem.Checked = control.ControlMode == ControlMode.Software;
                        for (int i = 0; i <= 100; i += 5)
                        {
                            if (i <= control.MaxSoftwareValue &&
                                i >= control.MinSoftwareValue)
                            {
                                MenuItem item = new MenuItem(i + " %");
                                item.RadioCheck = true;
                                manualItem.MenuItems.Add(item);
                                item.Checked = control.ControlMode == ControlMode.Software &&
                                  Math.Round(control.SoftwareValue) == i;
                                int softwareValue = i;
                                item.Click += delegate (object obj, EventArgs args)
                                {
                                    control.SetSoftware(softwareValue);
                                };
                            }
                        }
                        treeContextMenu.MenuItems.Add(controlItem);
                    }

                    treeContextMenu.Show(treeView, new Point(m.X, m.Y));
                }

                HardwareNode hardwareNode = info.Node.Tag as HardwareNode;
                if (hardwareNode != null && hardwareNode.Hardware != null)
                {
                    treeContextMenu.MenuItems.Clear();

                    if (nodeTextBoxText.EditEnabled)
                    {
                        MenuItem item = new MenuItem("Rename");
                        item.Click += delegate (object obj, EventArgs args)
                        {
                            nodeTextBoxText.BeginEdit();
                        };
                        treeContextMenu.MenuItems.Add(item);
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

        private void ShowParameterForm(ISensor sensor)
        {
            ParameterForm form = new ParameterForm();
            form.Parameters = sensor.Parameters;
            form.captionLabel.Text = sensor.Name;
            form.ShowDialog();
        }

        private void TreeView_NodeMouseDoubleClick(object sender, TreeNodeAdvMouseEventArgs e)
        {
            SensorNode node = e.Node.Tag as SensorNode;
            if (node != null && node.Sensor != null && node.Sensor.Parameters.Count > 0)
            {
                ShowParameterForm(node.Sensor);
            }
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
            _computer.Accept(new SensorVisitor(delegate (ISensor sensor)
            {
                sensor.ResetMin();
                sensor.ResetMax();
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
            _computer.Close();
            _computer.Open();
            // restore the MainIcon setting
            _systemTray.IsMainIconEnabled = _minimizeToTray.Value;
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            _selectionDragging = _selectionDragging & (e.Button & (MouseButtons.Left | MouseButtons.Right)) > 0;
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

        public HttpServer Server
        {
            get { return _server; }
        }
    }
}
