// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.Utilities;

public class PersistentSettings : ISettings
{
    private readonly IDictionary<string, string> _settings = new Dictionary<string, string>();

    public void Load(string fileName)
    {
        XmlDocument doc = new XmlDocument();
        try
        {
            doc.Load(fileName);
        }
        catch
        {
            try
            {
                File.Delete(fileName);
            }
            catch { }

            string backupFileName = fileName + ".backup";
            try
            {
                doc.Load(backupFileName);
            }
            catch
            {
                try
                {
                    File.Delete(backupFileName);
                }
                catch { }

                return;
            }
        }

        XmlNodeList list = doc.GetElementsByTagName("appSettings");
        foreach (XmlNode node in list)
        {
            XmlNode parent = node.ParentNode;
            if (parent != null && parent.Name == "configuration" && parent.ParentNode is XmlDocument)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name == "add")
                    {
                        XmlAttributeCollection attributes = child.Attributes;
                        XmlAttribute keyAttribute = attributes["key"];
                        XmlAttribute valueAttribute = attributes["value"];
                        if (keyAttribute != null && valueAttribute != null && keyAttribute.Value != null)
                        {
                            _settings.Add(keyAttribute.Value, valueAttribute.Value);
                        }
                    }
                }
            }
        }
    }

    public void Save(string fileName)
    {
        XmlDocument doc = new XmlDocument();
        doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
        XmlElement configuration = doc.CreateElement("configuration");
        doc.AppendChild(configuration);
        XmlElement appSettings = doc.CreateElement("appSettings");
        configuration.AppendChild(appSettings);
        foreach (KeyValuePair<string, string> keyValuePair in _settings)
        {
            XmlElement add = doc.CreateElement("add");
            add.SetAttribute("key", keyValuePair.Key);
            add.SetAttribute("value", keyValuePair.Value);
            appSettings.AppendChild(add);
        }

        byte[] file;
        using (var memory = new MemoryStream())
        {
            using (var writer = new StreamWriter(memory, Encoding.UTF8))
            {
                doc.Save(writer);
            }
            file = memory.ToArray();
        }

        string backupFileName = fileName + ".backup";
        if (File.Exists(fileName))
        {
            try
            {
                File.Delete(backupFileName);
            }
            catch { }
            try
            {
                File.Move(fileName, backupFileName);
            }
            catch { }
        }

        using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        {
            stream.Write(file, 0, file.Length);
        }

        try
        {
            File.Delete(backupFileName);
        }
        catch { }
    }

    public bool Contains(string name)
    {
        return _settings.ContainsKey(name);
    }

    public void SetValue(string name, string value)
    {
        _settings[name] = value;
    }

    public string GetValue(string name, string value)
    {
        if (_settings.TryGetValue(name, out string result))
            return result;


        return value;
    }

    public void Remove(string name)
    {
        _settings.Remove(name);
    }

    public void SetValue(string name, int value)
    {
        _settings[name] = value.ToString();
    }

    public int GetValue(string name, int value)
    {
        if (_settings.TryGetValue(name, out string str))
        {
            if (int.TryParse(str, out int parsedValue))
                return parsedValue;


            return value;
        }

        return value;
    }

    public void SetValue(string name, float value)
    {
        _settings[name] = value.ToString(CultureInfo.InvariantCulture);
    }

    public float GetValue(string name, float value)
    {
        if (_settings.TryGetValue(name, out string str))
        {
            if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue))
                return parsedValue;
        }

        return value;

    }

    public double GetValue(string name, double value)
    {
        if (_settings.TryGetValue(name, out string str))
        {
            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue))
                return parsedValue;
        }

        return value;
    }
        
    public void SetValue(string name, bool value)
    {
        _settings[name] = value ? "true" : "false";
    }

    public bool GetValue(string name, bool value)
    {
        if (_settings.TryGetValue(name, out string str))
        {
            return str == "true";
        }

        return value;
    }

    public void SetValue(string name, Color color)
    {
        _settings[name] = color.ToArgb().ToString("X8");
    }

    public Color GetValue(string name, Color value)
    {
        if (_settings.TryGetValue(name, out string str))
        {
            if (int.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int parsedValue))
                return Color.FromArgb(parsedValue);
        }

        return value;
    }
}