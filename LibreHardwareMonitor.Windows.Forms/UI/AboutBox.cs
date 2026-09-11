// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LibreHardwareMonitor.Windows.Forms.UI.Themes;

namespace LibreHardwareMonitor.Windows.Forms.UI;

public sealed partial class AboutBox : Form
{
    public AboutBox()
    {
        InitializeComponent();
        Font = SystemFonts.MessageBoxFont;
        Text = LibreHardwareMonitor.Windows.Forms.Localization.LocalizationManager.Get("About.title") ?? "About";
        okButton.Text = LibreHardwareMonitor.Windows.Forms.Localization.LocalizationManager.Get("About.ok") ?? "OK";
        label3.Text = (LibreHardwareMonitor.Windows.Forms.Localization.LocalizationManager.Get("About.versionPrefix") ?? "Version ") + Application.ProductVersion;
        projectLinkLabel.Text = LibreHardwareMonitor.Windows.Forms.Localization.LocalizationManager.Get("About.projectWebsite") ?? "Project Website";
        licenseLinkLabel.Text = LibreHardwareMonitor.Windows.Forms.Localization.LocalizationManager.Get("About.licensing") ?? "Licensing Information";
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
