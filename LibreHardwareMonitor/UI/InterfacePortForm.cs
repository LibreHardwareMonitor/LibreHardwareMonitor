// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Net.NetworkInformation;

namespace LibreHardwareMonitor.UI;

public partial class InterfacePortForm : Form
{
    private readonly MainForm _parent;
    private string _localIP;
    
    public InterfacePortForm(MainForm m)
    {
        InitializeComponent();
        _parent = m;
        _localIP = LoadNetworkInterfaces(_parent.Server.ListenerIp);
    }

    private string LoadNetworkInterfaces(string selectedListenerIp)
    {
        IPHostEntry host;
        interfaceComboBox.Items.Clear();
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                interfaceComboBox.Items.Add(ip.ToString());
        }
        // select the last one by default to match the existing behavior
        if (interfaceComboBox.Items.Count > 0)
        {
            interfaceComboBox.SelectedIndex = interfaceComboBox.Items.Count - 1;
        } else
        {
            // default to ? just like previous version
            interfaceComboBox.Items.Add("?");
            interfaceComboBox.SelectedIndex = 0;
        }
        // check to see if the selected listener IP is in our list.
        if (interfaceComboBox.Items.Contains(selectedListenerIp))
        {
            // default it to the previously selected IP.
            interfaceComboBox.SelectedItem = selectedListenerIp;
        }
        return interfaceComboBox.SelectedItem as string;
    }

    private void PortNumericUpDn_ValueChanged(object sender, EventArgs e)
    {
        string url = "http://" + _localIP + ":" + portNumericUpDn.Value + "/";
        webServerLinkLabel.Text = url;
        webServerLinkLabel.Links.Remove(webServerLinkLabel.Links[0]);
        webServerLinkLabel.Links.Add(0, webServerLinkLabel.Text.Length, url);
    }

    private void PortOKButton_Click(object sender, EventArgs e)
    {
        _parent.Server.ListenerPort = (int)portNumericUpDn.Value;
        _parent.Server.ListenerIp = _localIP;
        Close();
    }

    private void PortCancelButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void PortForm_Load(object sender, EventArgs e)
    {
        interfaceComboBox.SelectedValue = _parent.Server.ListenerIp;
        portNumericUpDn.Value = _parent.Server.ListenerPort;
        PortNumericUpDn_ValueChanged(null, null);
    }

    private void WebServerLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Link.LinkData.ToString()));
        }
        catch { }
    }

    private void PortNumericUpDn_KeyUp(object sender, KeyEventArgs e)
    {
        PortNumericUpDn_ValueChanged(null, null);
    }

    private void interfaceComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {

        _localIP = interfaceComboBox.SelectedItem as string;
        PortNumericUpDn_ValueChanged(null, null);
    }
}
