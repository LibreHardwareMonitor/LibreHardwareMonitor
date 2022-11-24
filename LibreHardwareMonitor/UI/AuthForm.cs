// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.Remoting.Channels;
using System.Windows.Forms;

using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI;

public partial class AuthForm : Form
{
    private readonly MainForm _parent;

    public AuthForm(MainForm m)
    {
        InitializeComponent();
        _parent = m;
    }

    private void AuthForm_Load(object sender, EventArgs e)
    {
        httpAuthUsernameTextBox.Enabled = httpAuthPasswordTextBox.Enabled = enableHTTPAuthCheckBox.Checked = _parent.Server.AuthEnabled;
        httpAuthUsernameTextBox.Text = _parent.Server.UserName;
        if (_parent.Server.Password.Length != 64)
        {
            _parent.Server.Password = httpAuthPasswordTextBox.Text = "librehm";
            httpAuthPasswordTextBox.UseSystemPasswordChar = false;
        }
        else
            httpAuthPasswordTextBox.Text = _parent.Server.Password;
    }

    private void HttpAuthCancelButton_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void HttpAuthOkButton_Click(object sender, EventArgs e)
    {
        _parent.Server.UserName = httpAuthUsernameTextBox.Text;
        _parent.Server.Password = httpAuthPasswordTextBox.Text;
        _parent.Server.AuthEnabled = enableHTTPAuthCheckBox.Checked;
        _parent.AuthWebServerMenuItemChecked = _parent.Server.AuthEnabled;
        if(_parent.Server.RestartRequired && _parent.Server.PendingRestarts.Count > 0)
        {
            string txt = "Server restart required in order for changes to take effect.\nDo you want to restart it now?\nPending changes:";
            foreach (HttpServer.PendingRestartReason reason in _parent.Server.PendingRestarts)
            {
                txt += "\n - ";
                txt += reason switch
                {
                    HttpServer.PendingRestartReason.UserNameChanged => "Username: " + _parent.Server.UserName,
                    HttpServer.PendingRestartReason.PasswordChanged => "Password: <hidden>",
                    HttpServer.PendingRestartReason.ListenerPortChanged => "Port: " + _parent.Server.ListenerPort,
                    HttpServer.PendingRestartReason.AuthEnabledChanged => "Authentification: " + (_parent.Server.AuthEnabled ? "enabled" : "disabled"),
                    HttpServer.PendingRestartReason.Other => "Not specified",
                    _ => "Error while reading changes!",
                };
            }
            DialogResult result = MessageBox.Show(txt, "Pending server restart", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
            if(result == DialogResult.Yes)
            {
                _parent.Server.Restart();
            }
        }
        Close();
    }

    private void EnableHTTPAuthCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        httpAuthUsernameTextBox.Enabled = httpAuthPasswordTextBox.Enabled = enableHTTPAuthCheckBox.Checked;
    }

    private void httpAuthUsernameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SelectNextControl(httpAuthUsernameTextBox, true, true, true, true);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void httpAuthPasswordTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SelectNextControl(httpAuthPasswordTextBox, true, true, true, true);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }
}
