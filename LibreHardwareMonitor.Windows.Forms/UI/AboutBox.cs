// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LibreHardwareMonitor.Windows.Forms.Localization;
using LibreHardwareMonitor.Windows.Forms.UI.Themes;

namespace LibreHardwareMonitor.Windows.Forms.UI;

public sealed partial class AboutBox : Form
{
    public AboutBox()
    {
        InitializeComponent();
        Font = SystemFonts.MessageBoxFont;
        Text = Strings.AboutTitle;
        okButton.Text = Strings.OK;
        label1.Text = Strings.AppTitle;
        label2.Text = Strings.AboutCopyright;
        label3.Text = string.Format(Strings.AboutVersion, Application.ProductVersion);
        projectLinkLabel.Text = Strings.ProjectWebsite;
        licenseLinkLabel.Text = Strings.LicensingInformation;
        projectLinkLabel.Links.Remove(projectLinkLabel.Links[0]);
        projectLinkLabel.Links.Add(0, projectLinkLabel.Text.Length, "https://github.com/LibreHardwareMonitor/LibreHardwareMonitor");
        licenseLinkLabel.Links.Remove(licenseLinkLabel.Links[0]);
        licenseLinkLabel.Links.Add(0, licenseLinkLabel.Text.Length, "https://www.mozilla.org/en-US/MPL/2.0/");
        Theme.Current.Apply(this);
    }

    private void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Link.LinkData.ToString()));
        }
        catch { }
    }
}
