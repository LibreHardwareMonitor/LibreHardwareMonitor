// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace LibreHardwareMonitor.Windows.Forms.Localization;

internal static class UiCulture
{
    public const string SettingKey = "ui.language";
    public const string System = "system";
    public const string English = "en";
    public const string Russian = "ru";

    public static string CurrentCode { get; private set; } = System;

    public static void ApplyFromConfig()
    {
        string code = System;
        try
        {
            string fileName = Path.ChangeExtension(Application.ExecutablePath, ".config");
            if (File.Exists(fileName))
            {
                XmlDocument doc = new();
                doc.Load(fileName);
                XmlNodeList list = doc.SelectNodes("//appSettings/add");
                if (list != null)
                {
                    foreach (XmlNode node in list)
                    {
                        if (node.Attributes?["key"]?.Value == SettingKey)
                        {
                            code = node.Attributes["value"]?.Value ?? System;
                            break;
                        }
                    }
                }
            }
        }
        catch
        {
            // Keep system language if the config cannot be read yet.
        }

        Apply(code);
    }

    public static void Apply(string code)
    {
        CurrentCode = string.IsNullOrWhiteSpace(code) ? System : code;
        CultureInfo culture = Resolve(CurrentCode);
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    public static CultureInfo Resolve(string code)
    {
        if (string.Equals(code, English, StringComparison.OrdinalIgnoreCase))
            return CultureInfo.GetCultureInfo("en");
        if (string.Equals(code, Russian, StringComparison.OrdinalIgnoreCase))
            return CultureInfo.GetCultureInfo("ru");
        return CultureInfo.InstalledUICulture;
    }
}
