/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2013 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

namespace OpenHardwareMonitor.GUI {
  partial class MainForm {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      this.components = new System.ComponentModel.Container();
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
      this.sensor = new Aga.Controls.Tree.TreeColumn();
      this.value = new Aga.Controls.Tree.TreeColumn();
      this.min = new Aga.Controls.Tree.TreeColumn();
      this.max = new Aga.Controls.Tree.TreeColumn();
      this.nodeImage = new Aga.Controls.Tree.NodeControls.NodeIcon();
      this.nodeCheckBox = new Aga.Controls.Tree.NodeControls.NodeCheckBox();
      this.nodeTextBoxText = new Aga.Controls.Tree.NodeControls.NodeTextBox();
      this.nodeTextBoxValue = new Aga.Controls.Tree.NodeControls.NodeTextBox();
      this.nodeTextBoxMin = new Aga.Controls.Tree.NodeControls.NodeTextBox();
      this.nodeTextBoxMax = new Aga.Controls.Tree.NodeControls.NodeTextBox();
      this.mainMenu = new System.Windows.Forms.MainMenu(this.components);
      this.fileMenuItem = new System.Windows.Forms.MenuItem();
      this.saveReportMenuItem = new System.Windows.Forms.MenuItem();
      this.sumbitReportMenuItem = new System.Windows.Forms.MenuItem();
      this.MenuItem2 = new System.Windows.Forms.MenuItem();
      this.resetMenuItem = new System.Windows.Forms.MenuItem();
      this.menuItem5 = new System.Windows.Forms.MenuItem();
      this.mainboardMenuItem = new System.Windows.Forms.MenuItem();
      this.cpuMenuItem = new System.Windows.Forms.MenuItem();
      this.ramMenuItem = new System.Windows.Forms.MenuItem();
      this.gpuMenuItem = new System.Windows.Forms.MenuItem();
      this.fanControllerMenuItem = new System.Windows.Forms.MenuItem();
      this.hddMenuItem = new System.Windows.Forms.MenuItem();
      this.menuItem6 = new System.Windows.Forms.MenuItem();
      this.exitMenuItem = new System.Windows.Forms.MenuItem();
      this.viewMenuItem = new System.Windows.Forms.MenuItem();
      this.resetMinMaxMenuItem = new System.Windows.Forms.MenuItem();
      this.MenuItem3 = new System.Windows.Forms.MenuItem();
      this.hiddenMenuItem = new System.Windows.Forms.MenuItem();
      this.plotMenuItem = new System.Windows.Forms.MenuItem();
      this.gadgetMenuItem = new System.Windows.Forms.MenuItem();
      this.MenuItem1 = new System.Windows.Forms.MenuItem();
      this.columnsMenuItem = new System.Windows.Forms.MenuItem();
      this.valueMenuItem = new System.Windows.Forms.MenuItem();
      this.minMenuItem = new System.Windows.Forms.MenuItem();
      this.maxMenuItem = new System.Windows.Forms.MenuItem();
      this.optionsMenuItem = new System.Windows.Forms.MenuItem();
      this.startMinMenuItem = new System.Windows.Forms.MenuItem();
      this.minTrayMenuItem = new System.Windows.Forms.MenuItem();
      this.minCloseMenuItem = new System.Windows.Forms.MenuItem();
      this.startupMenuItem = new System.Windows.Forms.MenuItem();
      this.separatorMenuItem = new System.Windows.Forms.MenuItem();
      this.temperatureUnitsMenuItem = new System.Windows.Forms.MenuItem();
      this.celsiusMenuItem = new System.Windows.Forms.MenuItem();
      this.fahrenheitMenuItem = new System.Windows.Forms.MenuItem();
      this.plotLocationMenuItem = new System.Windows.Forms.MenuItem();
      this.plotWindowMenuItem = new System.Windows.Forms.MenuItem();
      this.plotBottomMenuItem = new System.Windows.Forms.MenuItem();
      this.plotRightMenuItem = new System.Windows.Forms.MenuItem();
      this.webMenuItemSeparator = new System.Windows.Forms.MenuItem();
      this.webMenuItem = new System.Windows.Forms.MenuItem();
      this.runWebServerMenuItem = new System.Windows.Forms.MenuItem();
      this.serverPortMenuItem = new System.Windows.Forms.MenuItem();
      this.logSensorsMenuItem = new System.Windows.Forms.MenuItem();
      this.helpMenuItem = new System.Windows.Forms.MenuItem();
      this.aboutMenuItem = new System.Windows.Forms.MenuItem();
      this.treeContextMenu = new System.Windows.Forms.ContextMenu();
      this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
      this.timer = new System.Windows.Forms.Timer(this.components);
      this.splitContainer = new OpenHardwareMonitor.GUI.SplitContainerAdv();
      this.treeView = new Aga.Controls.Tree.TreeViewAdv();
      this.nicMenuItem = new System.Windows.Forms.MenuItem();
      this.logSeparatorMenuItem = new System.Windows.Forms.MenuItem();
      this.loggingIntervalMenuItem = new System.Windows.Forms.MenuItem();
      this.log1sMenuItem = new System.Windows.Forms.MenuItem();
      this.log5sMenuItem = new System.Windows.Forms.MenuItem();
      this.log10sMenuItem = new System.Windows.Forms.MenuItem();
      this.log30sMenuItem = new System.Windows.Forms.MenuItem();
      this.log1minMenuItem = new System.Windows.Forms.MenuItem();
      this.log5minMenuItem = new System.Windows.Forms.MenuItem();
      this.log10minMenuItem = new System.Windows.Forms.MenuItem();
      this.log30minMenuItem = new System.Windows.Forms.MenuItem();
      this.log2sMenuItem = new System.Windows.Forms.MenuItem();
      this.log2minMenuItem = new System.Windows.Forms.MenuItem();
      this.log1hMenuItem = new System.Windows.Forms.MenuItem();
      this.log2hMenuItem = new System.Windows.Forms.MenuItem();
      this.log6hMenuItem = new System.Windows.Forms.MenuItem();
      this.sensorValuesTimeWindowMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow30sMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow2minMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow5minMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow10minMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow30minMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow1hMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow2hMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow6hMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow12hMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow24hMenuItem = new System.Windows.Forms.MenuItem();
      this.timeWindow1minMenuItem = new System.Windows.Forms.MenuItem();
      this.splitContainer.Panel1.SuspendLayout();
      this.splitContainer.SuspendLayout();
      this.SuspendLayout();
      // 
      // sensor
      // 
      this.sensor.Header = "Sensor";
      this.sensor.SortOrder = System.Windows.Forms.SortOrder.None;
      this.sensor.TooltipText = null;
      this.sensor.Width = 250;
      // 
      // value
      // 
      this.value.Header = "Value";
      this.value.SortOrder = System.Windows.Forms.SortOrder.None;
      this.value.TooltipText = null;
      this.value.Width = 100;
      // 
      // min
      // 
      this.min.Header = "Min";
      this.min.SortOrder = System.Windows.Forms.SortOrder.None;
      this.min.TooltipText = null;
      this.min.Width = 100;
      // 
      // max
      // 
      this.max.Header = "Max";
      this.max.SortOrder = System.Windows.Forms.SortOrder.None;
      this.max.TooltipText = null;
      this.max.Width = 100;
      // 
      // nodeImage
      // 
      this.nodeImage.DataPropertyName = "Image";
      this.nodeImage.LeftMargin = 1;
      this.nodeImage.ParentColumn = this.sensor;
      this.nodeImage.ScaleMode = Aga.Controls.Tree.ImageScaleMode.Fit;
      // 
      // nodeCheckBox
      // 
      this.nodeCheckBox.DataPropertyName = "Plot";
      this.nodeCheckBox.EditEnabled = true;
      this.nodeCheckBox.LeftMargin = 3;
      this.nodeCheckBox.ParentColumn = this.sensor;
      // 
      // nodeTextBoxText
      // 
      this.nodeTextBoxText.DataPropertyName = "Text";
      this.nodeTextBoxText.EditEnabled = true;
      this.nodeTextBoxText.IncrementalSearchEnabled = true;
      this.nodeTextBoxText.LeftMargin = 3;
      this.nodeTextBoxText.ParentColumn = this.sensor;
      this.nodeTextBoxText.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
      this.nodeTextBoxText.UseCompatibleTextRendering = true;
      // 
      // nodeTextBoxValue
      // 
      this.nodeTextBoxValue.DataPropertyName = "Value";
      this.nodeTextBoxValue.IncrementalSearchEnabled = true;
      this.nodeTextBoxValue.LeftMargin = 3;
      this.nodeTextBoxValue.ParentColumn = this.value;
      this.nodeTextBoxValue.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
      this.nodeTextBoxValue.UseCompatibleTextRendering = true;
      // 
      // nodeTextBoxMin
      // 
      this.nodeTextBoxMin.DataPropertyName = "Min";
      this.nodeTextBoxMin.IncrementalSearchEnabled = true;
      this.nodeTextBoxMin.LeftMargin = 3;
      this.nodeTextBoxMin.ParentColumn = this.min;
      this.nodeTextBoxMin.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
      this.nodeTextBoxMin.UseCompatibleTextRendering = true;
      // 
      // nodeTextBoxMax
      // 
      this.nodeTextBoxMax.DataPropertyName = "Max";
      this.nodeTextBoxMax.IncrementalSearchEnabled = true;
      this.nodeTextBoxMax.LeftMargin = 3;
      this.nodeTextBoxMax.ParentColumn = this.max;
      this.nodeTextBoxMax.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
      this.nodeTextBoxMax.UseCompatibleTextRendering = true;
      // 
      // mainMenu
      // 
      this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.fileMenuItem,
            this.viewMenuItem,
            this.optionsMenuItem,
            this.helpMenuItem});
      // 
      // fileMenuItem
      // 
      this.fileMenuItem.Index = 0;
      this.fileMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.saveReportMenuItem,
            this.sumbitReportMenuItem,
            this.MenuItem2,
            this.resetMenuItem,
            this.menuItem5,
            this.menuItem6,
            this.exitMenuItem});
      this.fileMenuItem.Text = "File";
      // 
      // saveReportMenuItem
      // 
      this.saveReportMenuItem.Index = 0;
      this.saveReportMenuItem.Text = "Save Report...";
      this.saveReportMenuItem.Click += new System.EventHandler(this.saveReportMenuItem_Click);
      // 
      // sumbitReportMenuItem
      // 
      this.sumbitReportMenuItem.Index = 1;
      this.sumbitReportMenuItem.Text = "Submit Report...";
      this.sumbitReportMenuItem.Click += new System.EventHandler(this.sumbitReportMenuItem_Click);
      // 
      // MenuItem2
      // 
      this.MenuItem2.Index = 2;
      this.MenuItem2.Text = "-";
      // 
      // resetMenuItem
      // 
      this.resetMenuItem.Index = 3;
      this.resetMenuItem.Text = "Reset";
      this.resetMenuItem.Click += new System.EventHandler(this.resetClick);
      // 
      // menuItem5
      // 
      this.menuItem5.Index = 4;
      this.menuItem5.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.mainboardMenuItem,
            this.cpuMenuItem,
            this.ramMenuItem,
            this.gpuMenuItem,
            this.fanControllerMenuItem,
            this.hddMenuItem,
            this.nicMenuItem});
      this.menuItem5.Text = "Hardware";
      // 
      // mainboardMenuItem
      // 
      this.mainboardMenuItem.Index = 0;
      this.mainboardMenuItem.Text = "Mainboard";
      // 
      // cpuMenuItem
      // 
      this.cpuMenuItem.Index = 1;
      this.cpuMenuItem.Text = "CPU";
      // 
      // ramMenuItem
      // 
      this.ramMenuItem.Index = 2;
      this.ramMenuItem.Text = "RAM";
      // 
      // gpuMenuItem
      // 
      this.gpuMenuItem.Index = 3;
      this.gpuMenuItem.Text = "GPU";
      // 
      // fanControllerMenuItem
      // 
      this.fanControllerMenuItem.Index = 4;
      this.fanControllerMenuItem.Text = "Fan Controllers";
      // 
      // hddMenuItem
      // 
      this.hddMenuItem.Index = 5;
      this.hddMenuItem.Text = "Hard Disk Drives";
      // 
      // nicItem
      // 
      this.nicMenuItem.Index = 6;
      this.nicMenuItem.Text = "Network";
      // 
      // menuItem6
      // 
      this.menuItem6.Index = 5;
      this.menuItem6.Text = "-";
      // 
      // exitMenuItem
      // 
      this.exitMenuItem.Index = 6;
      this.exitMenuItem.Text = "Exit";
      this.exitMenuItem.Click += new System.EventHandler(this.exitClick);
      // 
      // viewMenuItem
      // 
      this.viewMenuItem.Index = 1;
      this.viewMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.resetMinMaxMenuItem,
            this.MenuItem3,
            this.hiddenMenuItem,
            this.plotMenuItem,
            this.gadgetMenuItem,
            this.MenuItem1,
            this.columnsMenuItem});
      this.viewMenuItem.Text = "View";
      // 
      // resetMinMaxMenuItem
      // 
      this.resetMinMaxMenuItem.Index = 0;
      this.resetMinMaxMenuItem.Text = "Reset Min/Max";
      this.resetMinMaxMenuItem.Click += new System.EventHandler(this.resetMinMaxMenuItem_Click);
      // 
      // MenuItem3
      // 
      this.MenuItem3.Index = 1;
      this.MenuItem3.Text = "-";
      // 
      // hiddenMenuItem
      // 
      this.hiddenMenuItem.Index = 2;
      this.hiddenMenuItem.Text = "Show Hidden Sensors";
      // 
      // plotMenuItem
      // 
      this.plotMenuItem.Index = 3;
      this.plotMenuItem.Text = "Show Plot";
      // 
      // gadgetMenuItem
      // 
      this.gadgetMenuItem.Index = 4;
      this.gadgetMenuItem.Text = "Show Gadget";
      // 
      // MenuItem1
      // 
      this.MenuItem1.Index = 5;
      this.MenuItem1.Text = "-";
      // 
      // columnsMenuItem
      // 
      this.columnsMenuItem.Index = 6;
      this.columnsMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.valueMenuItem,
            this.minMenuItem,
            this.maxMenuItem});
      this.columnsMenuItem.Text = "Columns";
      // 
      // valueMenuItem
      // 
      this.valueMenuItem.Index = 0;
      this.valueMenuItem.Text = "Value";
      // 
      // minMenuItem
      // 
      this.minMenuItem.Index = 1;
      this.minMenuItem.Text = "Min";
      // 
      // maxMenuItem
      // 
      this.maxMenuItem.Index = 2;
      this.maxMenuItem.Text = "Max";
      // 
      // optionsMenuItem
      // 
      this.optionsMenuItem.Index = 2;
      this.optionsMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.startMinMenuItem,
            this.minTrayMenuItem,
            this.minCloseMenuItem,
            this.startupMenuItem,
            this.separatorMenuItem,
            this.temperatureUnitsMenuItem,
            this.plotLocationMenuItem,
            this.logSeparatorMenuItem,
            this.logSensorsMenuItem,
            this.loggingIntervalMenuItem,
            this.sensorValuesTimeWindowMenuItem,
            this.webMenuItemSeparator,
            this.webMenuItem});
      this.optionsMenuItem.Text = "Options";
      // 
      // startMinMenuItem
      // 
      this.startMinMenuItem.Index = 0;
      this.startMinMenuItem.Text = "Start Minimized";
      // 
      // minTrayMenuItem
      // 
      this.minTrayMenuItem.Index = 1;
      this.minTrayMenuItem.Text = "Minimize To Tray";
      // 
      // minCloseMenuItem
      // 
      this.minCloseMenuItem.Index = 2;
      this.minCloseMenuItem.Text = "Minimize On Close";
      // 
      // startupMenuItem
      // 
      this.startupMenuItem.Index = 3;
      this.startupMenuItem.Text = "Run On Windows Startup";
      // 
      // separatorMenuItem
      // 
      this.separatorMenuItem.Index = 4;
      this.separatorMenuItem.Text = "-";
      // 
      // temperatureUnitsMenuItem
      // 
      this.temperatureUnitsMenuItem.Index = 5;
      this.temperatureUnitsMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.celsiusMenuItem,
            this.fahrenheitMenuItem});
      this.temperatureUnitsMenuItem.Text = "Temperature Unit";
      // 
      // celsiusMenuItem
      // 
      this.celsiusMenuItem.Index = 0;
      this.celsiusMenuItem.RadioCheck = true;
      this.celsiusMenuItem.Text = "Celsius";
      this.celsiusMenuItem.Click += new System.EventHandler(this.celsiusMenuItem_Click);
      // 
      // fahrenheitMenuItem
      // 
      this.fahrenheitMenuItem.Index = 1;
      this.fahrenheitMenuItem.RadioCheck = true;
      this.fahrenheitMenuItem.Text = "Fahrenheit";
      this.fahrenheitMenuItem.Click += new System.EventHandler(this.fahrenheitMenuItem_Click);
      // 
      // plotLocationMenuItem
      // 
      this.plotLocationMenuItem.Index = 6;
      this.plotLocationMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.plotWindowMenuItem,
            this.plotBottomMenuItem,
            this.plotRightMenuItem});
      this.plotLocationMenuItem.Text = "Plot Location";
      // 
      // plotWindowMenuItem
      // 
      this.plotWindowMenuItem.Index = 0;
      this.plotWindowMenuItem.RadioCheck = true;
      this.plotWindowMenuItem.Text = "Window";
      // 
      // plotBottomMenuItem
      // 
      this.plotBottomMenuItem.Index = 1;
      this.plotBottomMenuItem.RadioCheck = true;
      this.plotBottomMenuItem.Text = "Bottom";
      // 
      // plotRightMenuItem
      // 
      this.plotRightMenuItem.Index = 2;
      this.plotRightMenuItem.RadioCheck = true;
      this.plotRightMenuItem.Text = "Right";
      // 
      // webMenuItemSeparator
      // 
      this.webMenuItemSeparator.Index = 11;
      this.webMenuItemSeparator.Text = "-";
      // 
      // webMenuItem
      // 
      this.webMenuItem.Index = 12;
      this.webMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.runWebServerMenuItem,
            this.serverPortMenuItem});
      this.webMenuItem.Text = "Remote Web Server";
      // 
      // runWebServerMenuItem
      // 
      this.runWebServerMenuItem.Index = 0;
      this.runWebServerMenuItem.Text = "Run";
      // 
      // serverPortMenuItem
      // 
      this.serverPortMenuItem.Index = 1;
      this.serverPortMenuItem.Text = "Port";
      this.serverPortMenuItem.Click += new System.EventHandler(this.serverPortMenuItem_Click);
      // 
      // logSensorsMenuItem
      // 
      this.logSensorsMenuItem.Index = 8;
      this.logSensorsMenuItem.Text = "Log Sensors";
      // 
      // helpMenuItem
      // 
      this.helpMenuItem.Index = 3;
      this.helpMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.aboutMenuItem});
      this.helpMenuItem.Text = "Help";
      // 
      // aboutMenuItem
      // 
      this.aboutMenuItem.Index = 0;
      this.aboutMenuItem.Text = "About";
      this.aboutMenuItem.Click += new System.EventHandler(this.aboutMenuItem_Click);
      // 
      // saveFileDialog
      // 
      this.saveFileDialog.DefaultExt = "txt";
      this.saveFileDialog.FileName = "OpenHardwareMonitor.Report.txt";
      this.saveFileDialog.Filter = "Text Documents|*.txt|All Files|*.*";
      this.saveFileDialog.RestoreDirectory = true;
      this.saveFileDialog.Title = "Save Report As";
      // 
      // timer
      // 
      this.timer.Interval = 1000;
      this.timer.Tick += new System.EventHandler(this.timer_Tick);
      // 
      // splitContainer
      // 
      this.splitContainer.Border3DStyle = System.Windows.Forms.Border3DStyle.Raised;
      this.splitContainer.Color = System.Drawing.SystemColors.Control;
      this.splitContainer.Cursor = System.Windows.Forms.Cursors.Default;
      this.splitContainer.Location = new System.Drawing.Point(12, 12);
      this.splitContainer.Name = "splitContainer";
      this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
      // 
      // splitContainer.Panel1
      // 
      this.splitContainer.Panel1.Controls.Add(this.treeView);
      // 
      // splitContainer.Panel2
      // 
      this.splitContainer.Panel2.Cursor = System.Windows.Forms.Cursors.Default;
      this.splitContainer.Size = new System.Drawing.Size(386, 483);
      this.splitContainer.SplitterDistance = 354;
      this.splitContainer.SplitterWidth = 5;
      this.splitContainer.TabIndex = 3;
      // 
      // treeView
      // 
      this.treeView.BackColor = System.Drawing.SystemColors.Window;
      this.treeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.treeView.Columns.Add(this.sensor);
      this.treeView.Columns.Add(this.value);
      this.treeView.Columns.Add(this.min);
      this.treeView.Columns.Add(this.max);
      this.treeView.DefaultToolTipProvider = null;
      this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
      this.treeView.DragDropMarkColor = System.Drawing.Color.Black;
      this.treeView.FullRowSelect = true;
      this.treeView.GridLineStyle = Aga.Controls.Tree.GridLineStyle.Horizontal;
      this.treeView.LineColor = System.Drawing.SystemColors.ControlDark;
      this.treeView.Location = new System.Drawing.Point(0, 0);
      this.treeView.Model = null;
      this.treeView.Name = "treeView";
      this.treeView.NodeControls.Add(this.nodeImage);
      this.treeView.NodeControls.Add(this.nodeCheckBox);
      this.treeView.NodeControls.Add(this.nodeTextBoxText);
      this.treeView.NodeControls.Add(this.nodeTextBoxValue);
      this.treeView.NodeControls.Add(this.nodeTextBoxMin);
      this.treeView.NodeControls.Add(this.nodeTextBoxMax);
      this.treeView.SelectedNode = null;
      this.treeView.Size = new System.Drawing.Size(386, 354);
      this.treeView.TabIndex = 0;
      this.treeView.Text = "treeView";
      this.treeView.UseColumns = true;
      this.treeView.NodeMouseDoubleClick += new System.EventHandler<Aga.Controls.Tree.TreeNodeAdvMouseEventArgs>(this.treeView_NodeMouseDoubleClick);
      this.treeView.Click += new System.EventHandler(this.treeView_Click);
      this.treeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.treeView_MouseDown);
      this.treeView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.treeView_MouseMove);
      this.treeView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.treeView_MouseUp);
      // 
      // logSeparatorMenuItem
      // 
      this.logSeparatorMenuItem.Index = 7;
      this.logSeparatorMenuItem.Text = "-";
      // 
      // loggingIntervalMenuItem
      // 
      this.loggingIntervalMenuItem.Index = 9;
      this.loggingIntervalMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.log1sMenuItem,
            this.log2sMenuItem,
            this.log5sMenuItem,
            this.log10sMenuItem,
            this.log30sMenuItem,
            this.log1minMenuItem,
            this.log2minMenuItem,
            this.log5minMenuItem,
            this.log10minMenuItem,
            this.log30minMenuItem,
            this.log1hMenuItem,
            this.log2hMenuItem,
            this.log6hMenuItem});
      this.loggingIntervalMenuItem.Text = "Logging Interval";
      // 
      // log1sMenuItem
      // 
      this.log1sMenuItem.Index = 0;
      this.log1sMenuItem.RadioCheck = true;
      this.log1sMenuItem.Text = "1s";
      // 
      // log5sMenuItem
      // 
      this.log5sMenuItem.Index = 2;
      this.log5sMenuItem.RadioCheck = true;
      this.log5sMenuItem.Text = "5s";
      // 
      // log10sMenuItem
      // 
      this.log10sMenuItem.Index = 3;
      this.log10sMenuItem.RadioCheck = true;
      this.log10sMenuItem.Text = "10s";
      // 
      // log30sMenuItem
      // 
      this.log30sMenuItem.Index = 4;
      this.log30sMenuItem.RadioCheck = true;
      this.log30sMenuItem.Text = "30s";
      // 
      // log1minMenuItem
      // 
      this.log1minMenuItem.Index = 5;
      this.log1minMenuItem.RadioCheck = true;
      this.log1minMenuItem.Text = "1min";
      // 
      // log5minMenuItem
      // 
      this.log5minMenuItem.Index = 7;
      this.log5minMenuItem.RadioCheck = true;
      this.log5minMenuItem.Text = "5min";
      // 
      // log10minMenuItem
      // 
      this.log10minMenuItem.Index = 8;
      this.log10minMenuItem.RadioCheck = true;
      this.log10minMenuItem.Text = "10min";
      // 
      // log30minMenuItem
      // 
      this.log30minMenuItem.Index = 9;
      this.log30minMenuItem.RadioCheck = true;
      this.log30minMenuItem.Text = "30min";
      // 
      // log2sMenuItem
      // 
      this.log2sMenuItem.Index = 1;
      this.log2sMenuItem.RadioCheck = true;
      this.log2sMenuItem.Text = "2s";
      // 
      // log2minMenuItem
      // 
      this.log2minMenuItem.Index = 6;
      this.log2minMenuItem.RadioCheck = true;
      this.log2minMenuItem.Text = "2min";
      // 
      // log1hMenuItem
      // 
      this.log1hMenuItem.Index = 10;
      this.log1hMenuItem.RadioCheck = true;
      this.log1hMenuItem.Text = "1h";
      // 
      // log2hMenuItem
      // 
      this.log2hMenuItem.Index = 11;
      this.log2hMenuItem.RadioCheck = true;
      this.log2hMenuItem.Text = "2h";
      // 
      // log6hMenuItem
      // 
      this.log6hMenuItem.Index = 12;
      this.log6hMenuItem.RadioCheck = true;
      this.log6hMenuItem.Text = "6h";
      // 
      // 
      // sensorValuesTimeWindowMenuItem
      // 
      this.sensorValuesTimeWindowMenuItem.Index = 10;
      this.sensorValuesTimeWindowMenuItem.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
      this.timeWindow30sMenuItem,
      this.timeWindow1minMenuItem,
      this.timeWindow2minMenuItem,
      this.timeWindow5minMenuItem,
      this.timeWindow10minMenuItem,
      this.timeWindow30minMenuItem,
      this.timeWindow1hMenuItem,
      this.timeWindow2hMenuItem,
      this.timeWindow6hMenuItem,
      this.timeWindow12hMenuItem,
      this.timeWindow24hMenuItem});
      this.sensorValuesTimeWindowMenuItem.Text = "Sensor Values Time Window";
      // 
      // timeWindow30sMenuItem
      // 
      this.timeWindow30sMenuItem.Index = 0;
      this.timeWindow30sMenuItem.RadioCheck = true;
      this.timeWindow30sMenuItem.Text = "30s";
      // 
      // timeWindow2minMenuItem
      // 
      this.timeWindow2minMenuItem.Index = 2;
      this.timeWindow2minMenuItem.RadioCheck = true;
      this.timeWindow2minMenuItem.Text = "2min";
      // 
      // timeWindow5minMenuItem
      // 
      this.timeWindow5minMenuItem.Index = 3;
      this.timeWindow5minMenuItem.RadioCheck = true;
      this.timeWindow5minMenuItem.Text = "5min";
      // 
      // timeWindow10minMenuItem
      // 
      this.timeWindow10minMenuItem.Index = 4;
      this.timeWindow10minMenuItem.RadioCheck = true;
      this.timeWindow10minMenuItem.Text = "10min";
      // 
      // timeWindow30minMenuItem
      // 
      this.timeWindow30minMenuItem.Index = 5;
      this.timeWindow30minMenuItem.RadioCheck = true;
      this.timeWindow30minMenuItem.Text = "30min";
      // 
      // timeWindow1hMenuItem
      // 
      this.timeWindow1hMenuItem.Index = 6;
      this.timeWindow1hMenuItem.RadioCheck = true;
      this.timeWindow1hMenuItem.Text = "1h";
      // 
      // timeWindow2hMenuItem
      // 
      this.timeWindow2hMenuItem.Index = 7;
      this.timeWindow2hMenuItem.RadioCheck = true;
      this.timeWindow2hMenuItem.Text = "2n";
      // 
      // timeWindow6hMenuItem
      // 
      this.timeWindow6hMenuItem.Index = 8;
      this.timeWindow6hMenuItem.RadioCheck = true;
      this.timeWindow6hMenuItem.Text = "6h";
      // 
      // timeWindow12hMenuItem
      // 
      this.timeWindow12hMenuItem.Index = 9;
      this.timeWindow12hMenuItem.RadioCheck = true;
      this.timeWindow12hMenuItem.Text = "12h";
      // 
      // timeWindow24hMenuItem
      // 
      this.timeWindow24hMenuItem.Index = 10;
      this.timeWindow24hMenuItem.RadioCheck = true;
      this.timeWindow24hMenuItem.Text = "24h";
      // 
      // timeWindow1minMenuItem
      // 
      this.timeWindow1minMenuItem.Index = 1;
      this.timeWindow1minMenuItem.RadioCheck = true;
      this.timeWindow1minMenuItem.Text = "1min";
      // 
      // MainForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(418, 554);
      this.Controls.Add(this.splitContainer);
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Menu = this.mainMenu;
      this.Name = "MainForm";
      this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
      this.Text = "Open Hardware Monitor";
      this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
      this.Load += new System.EventHandler(this.MainForm_Load);
      this.ResizeEnd += new System.EventHandler(this.MainForm_MoveOrResize);
      this.Move += new System.EventHandler(this.MainForm_MoveOrResize);
      this.splitContainer.Panel1.ResumeLayout(false);
      this.splitContainer.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private Aga.Controls.Tree.TreeViewAdv treeView;
    private System.Windows.Forms.MainMenu mainMenu;
    private System.Windows.Forms.MenuItem fileMenuItem;
    private System.Windows.Forms.MenuItem exitMenuItem;
    private Aga.Controls.Tree.TreeColumn sensor;
    private Aga.Controls.Tree.TreeColumn value;
    private Aga.Controls.Tree.TreeColumn min;
    private Aga.Controls.Tree.TreeColumn max;
    private Aga.Controls.Tree.NodeControls.NodeIcon nodeImage;
    private Aga.Controls.Tree.NodeControls.NodeTextBox nodeTextBoxText;
    private Aga.Controls.Tree.NodeControls.NodeTextBox nodeTextBoxValue;
    private Aga.Controls.Tree.NodeControls.NodeTextBox nodeTextBoxMin;
    private Aga.Controls.Tree.NodeControls.NodeTextBox nodeTextBoxMax;
    private SplitContainerAdv splitContainer;
    private System.Windows.Forms.MenuItem viewMenuItem;
    private System.Windows.Forms.MenuItem plotMenuItem;
    private Aga.Controls.Tree.NodeControls.NodeCheckBox nodeCheckBox;
    private System.Windows.Forms.MenuItem helpMenuItem;
    private System.Windows.Forms.MenuItem aboutMenuItem;
    private System.Windows.Forms.MenuItem saveReportMenuItem;
    private System.Windows.Forms.MenuItem optionsMenuItem;
    private System.Windows.Forms.MenuItem hddMenuItem;
    private System.Windows.Forms.MenuItem minTrayMenuItem;
    private System.Windows.Forms.MenuItem separatorMenuItem;
    private System.Windows.Forms.ContextMenu treeContextMenu;
    private System.Windows.Forms.MenuItem startMinMenuItem;
    private System.Windows.Forms.MenuItem startupMenuItem;
    private System.Windows.Forms.SaveFileDialog saveFileDialog;
    private System.Windows.Forms.Timer timer;
    private System.Windows.Forms.MenuItem hiddenMenuItem;
    private System.Windows.Forms.MenuItem MenuItem1;
    private System.Windows.Forms.MenuItem columnsMenuItem;
    private System.Windows.Forms.MenuItem valueMenuItem;
    private System.Windows.Forms.MenuItem minMenuItem;
    private System.Windows.Forms.MenuItem maxMenuItem;
    private System.Windows.Forms.MenuItem temperatureUnitsMenuItem;
    private System.Windows.Forms.MenuItem webMenuItemSeparator;
    private System.Windows.Forms.MenuItem celsiusMenuItem;
    private System.Windows.Forms.MenuItem fahrenheitMenuItem;
    private System.Windows.Forms.MenuItem sumbitReportMenuItem;
    private System.Windows.Forms.MenuItem MenuItem2;
    private System.Windows.Forms.MenuItem resetMinMaxMenuItem;
    private System.Windows.Forms.MenuItem MenuItem3;
    private System.Windows.Forms.MenuItem gadgetMenuItem;
    private System.Windows.Forms.MenuItem minCloseMenuItem;
    private System.Windows.Forms.MenuItem resetMenuItem;
    private System.Windows.Forms.MenuItem menuItem6;
    private System.Windows.Forms.MenuItem plotLocationMenuItem;
    private System.Windows.Forms.MenuItem plotWindowMenuItem;
    private System.Windows.Forms.MenuItem plotBottomMenuItem;
    private System.Windows.Forms.MenuItem plotRightMenuItem;
		private System.Windows.Forms.MenuItem webMenuItem;
    private System.Windows.Forms.MenuItem runWebServerMenuItem;
    private System.Windows.Forms.MenuItem serverPortMenuItem;
    private System.Windows.Forms.MenuItem menuItem5;
    private System.Windows.Forms.MenuItem mainboardMenuItem;
    private System.Windows.Forms.MenuItem cpuMenuItem;
    private System.Windows.Forms.MenuItem gpuMenuItem;
    private System.Windows.Forms.MenuItem fanControllerMenuItem;
    private System.Windows.Forms.MenuItem ramMenuItem;
    private System.Windows.Forms.MenuItem logSensorsMenuItem;
    private System.Windows.Forms.MenuItem logSeparatorMenuItem;
    private System.Windows.Forms.MenuItem loggingIntervalMenuItem;
    private System.Windows.Forms.MenuItem log1sMenuItem;
    private System.Windows.Forms.MenuItem log2sMenuItem;
    private System.Windows.Forms.MenuItem log5sMenuItem;
    private System.Windows.Forms.MenuItem log10sMenuItem;
    private System.Windows.Forms.MenuItem log30sMenuItem;
    private System.Windows.Forms.MenuItem log1minMenuItem;
    private System.Windows.Forms.MenuItem log2minMenuItem;
    private System.Windows.Forms.MenuItem log5minMenuItem;
    private System.Windows.Forms.MenuItem log10minMenuItem;
    private System.Windows.Forms.MenuItem log30minMenuItem;
    private System.Windows.Forms.MenuItem log1hMenuItem;
    private System.Windows.Forms.MenuItem log2hMenuItem;
    private System.Windows.Forms.MenuItem log6hMenuItem;
    private System.Windows.Forms.MenuItem nicMenuItem;
    private System.Windows.Forms.MenuItem sensorValuesTimeWindowMenuItem;
    private System.Windows.Forms.MenuItem timeWindow30sMenuItem;
    private System.Windows.Forms.MenuItem timeWindow1minMenuItem;
    private System.Windows.Forms.MenuItem timeWindow2minMenuItem;
    private System.Windows.Forms.MenuItem timeWindow5minMenuItem;
    private System.Windows.Forms.MenuItem timeWindow10minMenuItem;
    private System.Windows.Forms.MenuItem timeWindow30minMenuItem;
    private System.Windows.Forms.MenuItem timeWindow1hMenuItem;
    private System.Windows.Forms.MenuItem timeWindow2hMenuItem;
    private System.Windows.Forms.MenuItem timeWindow6hMenuItem;
    private System.Windows.Forms.MenuItem timeWindow12hMenuItem;
    private System.Windows.Forms.MenuItem timeWindow24hMenuItem;
  }
}

