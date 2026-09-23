// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.Windows.Forms.Localization;

/// <summary>
/// Minimal localization helper.
///
/// The neutral resource <see cref="Strings"/> (English) is embedded in the main assembly, culture
/// specific resources (currently zh-CN) are deployed as satellite resource assemblies. The
/// canonical English strings in the code (designer texts, hard coded sensor group names) stay
/// untouched, so the default experience and all API behavior remain English. Localization is only
/// applied as a display layer on top of these defaults.
/// </summary>
public static class LocalizationManager
{
    private const string ResourceBaseName = "LibreHardwareMonitor.Windows.Forms.Localization.Strings";
    private static readonly ResourceManager Resources = new(ResourceBaseName, typeof(LocalizationManager).Assembly);

    private const string DefaultLanguage = "en";

    // A few ToolStripMenuItems in MainForm.Designer.cs have a legacy "Name" that does not match
    // their field name. The resource keys always use the field name, so translate the names here.
    private static readonly Dictionary<string, string> MenuItemNameAliases = new()
    {
        { "menuItem5", "menuItemFileHardware" },
        { "attachedPlotPanelScalingMenuItem", "splitPlotPanelScalingMenuItem" },
        { "attachedPlotPanelPercentageScalingMenuItem", "splitPanelPercentageScalingMenuItem" },
        { "attachedBottomMenuItem", "splitPanelFixedPlotScalingMenuItem" },
        { "attachedRightMenuItem", "splitPanelFixedSensorScalingMenuItem" }
    };

    public static string Language { get; private set; } = DefaultLanguage;

    /// <summary>
    /// Selects the UI language and applies it to the current thread so that
    /// <see cref="ResourceManager"/> resolves the right satellite resource set.
    /// </summary>
    public static void SetLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            language = DefaultLanguage;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            language = DefaultLanguage;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
        }

        Language = language;
    }

    /// <summary>
    /// Returns the localized string for the current UI language, or null when no translation
    /// exists (in which case callers keep their English default).
    /// </summary>
    public static string Get(string key)
    {
        return Resources.GetString(key);
    }

    /// <summary>
    /// Like <see cref="Get(string)"/> but resolves the resource for a specific language,
    /// independent of the current UI culture.
    /// </summary>
    public static string GetFor(string key, string language)
    {
        try
        {
            return Resources.GetString(key, CultureInfo.GetCultureInfo(language));
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Localizes the header of a sensor type group (for example "Temperatures").
    /// </summary>
    public static string GetGroupText(SensorType sensorType, string fallback)
    {
        return Get("SensorGroup." + sensorType) ?? fallback;
    }

    /// <summary>
    /// Walks a <see cref="MenuStrip"/> and replaces the text of every item for which a
    /// translation exists. Items without a translation keep their original (English) text.
    /// </summary>
    public static void ApplyToMenu(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is ToolStripMenuItem menuItem)
            {
                if (!string.IsNullOrEmpty(menuItem.Text))
                {
                    string key = MenuItemKey(menuItem.Name);
                    if (key != null)
                    {
                        string localized = Resources.GetString(key);
                        if (localized != null)
                            menuItem.Text = localized;
                    }
                }

                if (menuItem.HasDropDownItems)
                    ApplyToMenu(menuItem.DropDownItems);
            }
        }
    }

    private static string MenuItemKey(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        return "Menu." + (MenuItemNameAliases.TryGetValue(itemName, out string canonical) ? canonical : itemName);
    }
}
