// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI
{
    public partial class WebServerOptionsForm : Form
    {
        private readonly Computer _computer;
        private readonly PersistentSettings _settings;
        private const string WebServerSelectedSensorsSettingKey = "webServer.selectedSensors";

        public WebServerOptionsForm(Computer computer, PersistentSettings settings)
        {
            _computer = computer;
            _settings = settings;

            InitializeComponent();

            PopulateSensorTree();
            LoadSensorSelections();
        }

        private void PopulateSensorTree()
        {
            treeViewSensors.BeginUpdate();
            treeViewSensors.Nodes.Clear();

            foreach (IHardware hardware in _computer.Hardware)
            {
                TreeNode hardwareNode = new TreeNode(hardware.Name) { Tag = hardware };
                treeViewSensors.Nodes.Add(hardwareNode);
                AddSubHardwareAndSensors(hardwareNode, hardware);
                hardwareNode.Expand();
            }

            treeViewSensors.EndUpdate();
        }

        private void AddSubHardwareAndSensors(TreeNode parentNode, IHardware parentHardware)
        {
            foreach (IHardware subHardware in parentHardware.SubHardware)
            {
                TreeNode subHardwareNode = new TreeNode(subHardware.Name) { Tag = subHardware };
                parentNode.Nodes.Add(subHardwareNode);
                AddSubHardwareAndSensors(subHardwareNode, subHardware);
                // Optionally expand sub-hardware nodes too, or leave them collapsed by default
                // subHardwareNode.Expand(); 
            }

            foreach (ISensor sensor in parentHardware.Sensors)
            {
                TreeNode sensorNode = new TreeNode(sensor.Name) { Tag = sensor.Identifier.ToString() };
                parentNode.Nodes.Add(sensorNode);
            }
        }

        private void LoadSensorSelections()
        {
            string selectedSensorsString = _settings.GetValue(WebServerSelectedSensorsSettingKey, string.Empty);
            if (string.IsNullOrEmpty(selectedSensorsString))
                return;

            List<string> selectedSensorIds = new List<string>(selectedSensorsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            CheckSensorNodes(treeViewSensors.Nodes, selectedSensorIds);
        }

        private void CheckSensorNodes(TreeNodeCollection nodes, List<string> selectedIds)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is string sensorId && selectedIds.Contains(sensorId))
                {
                    node.Checked = true;
                }
                if (node.Nodes.Count > 0)
                {
                    CheckSensorNodes(node.Nodes, selectedIds);
                }
            }
        }

        private void SaveSensorSelections()
        {
            List<string> selectedSensorIds = new List<string>();
            GetCheckedSensorNodes(treeViewSensors.Nodes, selectedSensorIds);
            _settings.SetValue(WebServerSelectedSensorsSettingKey, string.Join(",", selectedSensorIds));
        }

        private void GetCheckedSensorNodes(TreeNodeCollection nodes, List<string> selectedIds)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Tag is string sensorId)
                {
                    selectedIds.Add(sensorId);
                }
                if (node.Nodes.Count > 0)
                {
                    GetCheckedSensorNodes(node.Nodes, selectedIds);
                }
            }
        }


        private void buttonOK_Click(object sender, EventArgs e)
        {
            SaveSensorSelections();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
