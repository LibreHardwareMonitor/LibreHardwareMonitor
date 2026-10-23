// SPDX-License-Identifier: MPL-2.0

using System.Globalization;
using BlackSharp.Core.Globalization;
using LanguageResources = LibreHardwareMonitor.Avalonia.Localization.Resources.Resources;

namespace LibreHardwareMonitor.Avalonia.Localization.Services;

/// <summary>
/// Selects an available user interface culture and applies it to resources and formatting.
/// </summary>
public static class AppCulture
{
    /// <summary>
    /// Gets the culture used when the system language is not supported.
    /// </summary>
    public const string DefaultName = "en-US";

    /// <summary>
    /// Lists the cultures for which application resources are available.
    /// </summary>
    private static readonly IReadOnlyList<string> _supportedCultureNames = Array.AsReadOnly(new[]
    {
        DefaultName,
        "de-DE"
    });

    /// <summary>
    /// Gets the supported user interface culture names.
    /// </summary>
    public static IReadOnlyList<string> SupportedCultureNames => _supportedCultureNames;

    /// <summary>
    /// Selects the supported language matching the current system user interface culture.
    /// </summary>
    /// <returns>The name of the selected culture.</returns>
    public static string GetSystemDefaultName()
    {
        CultureInfo systemCulture = CultureInfo.CurrentUICulture;

        if (TryGetSupportedName(systemCulture.Name, out string supportedName))
        {
            return supportedName;
        }

        foreach (string name in _supportedCultureNames)
        {
            CultureInfo supportedCulture = CultureInfo.GetCultureInfo(name);

            if (string.Equals(
                supportedCulture.TwoLetterISOLanguageName,
                systemCulture.TwoLetterISOLanguageName,
                StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
        }

        return DefaultName;
    }

    /// <summary>
    /// Checks whether a culture has application resources.
    /// </summary>
    /// <param name="cultureName">The culture name to check.</param>
    /// <returns><see langword="true"/> when the culture is supported.</returns>
    public static bool IsSupported(string cultureName)
    {
        return TryGetSupportedName(cultureName, out _);
    }

    /// <summary>
    /// Resolves a supported culture name or falls back to the default language.
    /// </summary>
    /// <param name="cultureName">The requested culture name.</param>
    /// <returns>The supported culture name.</returns>
    public static string Normalize(string cultureName)
    {
        return TryGetSupportedName(cultureName, out string supportedName)
            ? supportedName
            : DefaultName;
    }

    /// <summary>
    /// Applies a supported culture to the current and future threads and to the resource class.
    /// </summary>
    /// <param name="cultureName">The requested culture name.</param>
    public static void SetCurrent(string cultureName)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(Normalize(cultureName));

        LanguageResources.Culture = culture;

        CultureUtilities.SetCurrent(culture);
    }

    /// <summary>
    /// Finds the canonical name of a supported culture.
    /// </summary>
    /// <param name="cultureName">The culture name to search for.</param>
    /// <param name="supportedName">The matching supported culture name, when found.</param>
    /// <returns><see langword="true"/> when an exact supported culture name exists.</returns>
    private static bool TryGetSupportedName(string cultureName, out string supportedName)
    {
        foreach (string name in _supportedCultureNames)
        {
            if (string.Equals(name, cultureName, StringComparison.OrdinalIgnoreCase))
            {
                supportedName = name;
                return true;
            }
        }

        supportedName = null;
        return false;
    }
}
