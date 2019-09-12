// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace OpenHardwareMonitor.GUI
{
    public partial class PortForm : Form
    {
        private MainForm parent;
        private string localIP;
        public PortForm(MainForm m)
        {
            InitializeComponent();
            parent = m;
            localIP = getLocalIP();
        }

        private void portTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private string getLocalIP()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        private void portNumericUpDn_ValueChanged(object sender, EventArgs e)
        {
            string url = "http://" + localIP + ":" + portNumericUpDn.Value + "/";
            webServerLinkLabel.Text = url;
            webServerLinkLabel.Links.Remove(webServerLinkLabel.Links[0]);
            webServerLinkLabel.Links.Add(0, webServerLinkLabel.Text.Length, url);
        }

        private void portOKButton_Click(object sender, EventArgs e)
        {
            parent.Server.ListenerPort = (int)portNumericUpDn.Value;
            this.Close();
        }

        private void portCancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void PortForm_Load(object sender, EventArgs e)
        {
            portNumericUpDn.Value = parent.Server.ListenerPort;
            portNumericUpDn_ValueChanged(null, null);
        }

        private void webServerLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Link.LinkData.ToString()));
            }
            catch { }
        }

    }
}
