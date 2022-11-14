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

namespace LibreHardwareMonitor.UI;

public partial class PortForm : Form
{
    private readonly MainForm _parent;
    private readonly string _localIP;
    public PortForm(MainForm m)
    {
        InitializeComponent();
        _parent = m;
        _localIP = GetLocalIP();
    }

    private void PortTextBox_TextChanged(object sender, EventArgs e)
    {

    }

    private string GetLocalIP()
    {
        IPHostEntry host;
        string localIP = "?";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                localIP = ip.ToString();
        }
        return localIP;
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
        Close();
    }

    private void PortCancelButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void PortForm_Load(object sender, EventArgs e)
    {
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
}