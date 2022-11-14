// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Storage;

public abstract class AtaStorage : AbstractStorage
{
    // array of all hard drive types, matching type is searched in this order
    private static readonly Type[] _hddTypes = { typeof(SsdPlextor), typeof(SsdIntel), typeof(SsdSandforce), typeof(SsdIndilinx), typeof(SsdSamsung), typeof(SsdMicron), typeof(GenericHardDisk) };

    private IDictionary<SmartAttribute, Sensor> _sensors;

    /// <summary>
    /// Gets the SMART data.
    /// </summary>
    public ISmart Smart { get; }

    /// <summary>
    /// Gets the SMART attributes.
    /// </summary>
    public IReadOnlyList<SmartAttribute> SmartAttributes { get; }

    internal AtaStorage(StorageInfo storageInfo, ISmart smart, string name, string firmwareRevision, string id, int index, IReadOnlyList<SmartAttribute> smartAttributes, ISettings settings)
        : base(storageInfo, name, firmwareRevision, id, index, settings)
    {
        Smart = smart;
        if (smart.IsValid)
            smart.EnableSmart();
            
        SmartAttributes = smartAttributes;
        CreateSensors();
    }

    internal static AbstractStorage CreateInstance(StorageInfo storageInfo, ISettings settings)
    {
        ISmart smart = new WindowsSmart(storageInfo.Index);
        string name = null;
        string firmwareRevision = null;
        Kernel32.SMART_ATTRIBUTE[] smartAttributes = { };

        if (smart.IsValid)
        {
            bool nameValid = smart.ReadNameAndFirmwareRevision(out name, out firmwareRevision);
            bool smartEnabled = smart.EnableSmart();

            if (smartEnabled)
                smartAttributes = smart.ReadSmartData();
                
            if (!nameValid)
            {
                name = null;
                firmwareRevision = null;
            }
        }
        else
        {
            string[] logicalDrives = WindowsStorage.GetLogicalDrives(storageInfo.Index);
            if (logicalDrives == null || logicalDrives.Length == 0)
            {
                smart.Close();
                return null;
            }

            bool hasNonZeroSizeDrive = false;
            foreach (string logicalDrive in logicalDrives)
            {
                try
                {
                    var driveInfo = new DriveInfo(logicalDrive);
                    if (driveInfo.TotalSize > 0)
                    {
                        hasNonZeroSizeDrive = true;
                        break;
                    }
                }
                catch (ArgumentException) { }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            if (!hasNonZeroSizeDrive)
            {
                smart.Close();
                return null;
            }
        }

        if (string.IsNullOrEmpty(name))
            name = string.IsNullOrEmpty(storageInfo.Name) ? "Generic Hard Disk" : storageInfo.Name;

        if (string.IsNullOrEmpty(firmwareRevision))
            firmwareRevision = string.IsNullOrEmpty(storageInfo.Revision) ? "Unknown" : storageInfo.Revision;

        foreach (Type type in _hddTypes)
        {
            // get the array of the required SMART attributes for the current type

            // check if all required attributes are present
            bool allAttributesFound = true;

            if (type.GetCustomAttributes(typeof(RequireSmartAttribute), true) is RequireSmartAttribute[] requiredAttributes)
            {
                foreach (RequireSmartAttribute requireAttribute in requiredAttributes)
                {
                    bool attributeFound = false;

                    foreach (Kernel32.SMART_ATTRIBUTE value in smartAttributes)
                    {
                        if (value.Id == requireAttribute.AttributeId)
                        {
                            attributeFound = true;
                            break;
                        }
                    }

                    if (!attributeFound)
                    {
                        allAttributesFound = false;
                        break;
                    }
                }
            }

            // if an attribute is missing, then try the next type
            if (!allAttributesFound)
                continue;

            // check if there is a matching name prefix for this type
            if (type.GetCustomAttributes(typeof(NamePrefixAttribute), true) is NamePrefixAttribute[] namePrefixes)
            {
                foreach (NamePrefixAttribute prefix in namePrefixes)
                {
                    if (name.StartsWith(prefix.Prefix, StringComparison.InvariantCulture))
                    {
                        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

                        return Activator.CreateInstance(type, flags, null, new object[] { storageInfo, smart, name, firmwareRevision, storageInfo.Index, settings }, null) as AtaStorage;
                    }
                }
            }
        }

        // no matching type has been found
        smart.Close();
        return null;
    }

    protected sealed override void CreateSensors()
    {
        _sensors = new Dictionary<SmartAttribute, Sensor>();

        if (Smart.IsValid)
        {
            byte[] smartIds = Smart.ReadSmartData().Select(x => x.Id).ToArray();

            // unique attributes by SensorType and SensorChannel.
            IEnumerable<SmartAttribute> smartAttributes = SmartAttributes
                                                         .Where(x => x.SensorType.HasValue && smartIds.Contains(x.Id))
                                                         .GroupBy(x => new { x.SensorType.Value, x.SensorChannel })
                                                         .Select(x => x.First());

            _sensors = smartAttributes.ToDictionary(attribute => attribute,
                                                    attribute => new Sensor(attribute.SensorName,
                                                                            attribute.SensorChannel,
                                                                            attribute.DefaultHiddenSensor,
                                                                            attribute.SensorType.GetValueOrDefault(),
                                                                            this,
                                                                            attribute.ParameterDescriptions,
                                                                            _settings));

            foreach (KeyValuePair<SmartAttribute, Sensor> sensor in _sensors)
                ActivateSensor(sensor.Value);
        }

        base.CreateSensors();
    }

    protected virtual void UpdateAdditionalSensors(Kernel32.SMART_ATTRIBUTE[] values) { }

    protected override void UpdateSensors()
    {
        if (Smart.IsValid)
        {
            Kernel32.SMART_ATTRIBUTE[] smartAttributes = Smart.ReadSmartData();

            foreach (KeyValuePair<SmartAttribute, Sensor> keyValuePair in _sensors)
            {
                SmartAttribute attribute = keyValuePair.Key;
                foreach (Kernel32.SMART_ATTRIBUTE value in smartAttributes)
                {
                    if (value.Id == attribute.Id)
                    {
                        Sensor sensor = keyValuePair.Value;
                        sensor.Value = attribute.ConvertValue(value, sensor.Parameters);
                    }
                }
            }

            UpdateAdditionalSensors(smartAttributes);
        }
    }

    protected override void GetReport(StringBuilder r)
    {
        if (Smart.IsValid)
        {
            Kernel32.SMART_ATTRIBUTE[] values = Smart.ReadSmartData();
            Kernel32.SMART_THRESHOLD[] thresholds = Smart.ReadSmartThresholds();
            if (values.Length > 0)
            {
                r.AppendFormat(CultureInfo.InvariantCulture,
                               " {0}{1}{2}{3}{4}{5}{6}{7}",
                               "Id".PadRight(3),
                               "Description".PadRight(35),
                               "Raw Value".PadRight(13),
                               "Worst".PadRight(6),
                               "Value".PadRight(6),
                               "Threshold".PadRight(6),
                               "Physical".PadRight(8),
                               Environment.NewLine);

                foreach (Kernel32.SMART_ATTRIBUTE value in values)
                {
                    if (value.Id == 0x00)
                        break;

                    byte? threshold = null;
                    foreach (Kernel32.SMART_THRESHOLD t in thresholds)
                    {
                        if (t.Id == value.Id)
                        {
                            threshold = t.Threshold;
                        }
                    }

                    string description = "Unknown";
                    float? physical = null;
                    foreach (SmartAttribute a in SmartAttributes)
                    {
                        if (a.Id == value.Id)
                        {
                            description = a.Name;
                            if (a.HasRawValueConversion | a.SensorType.HasValue)
                                physical = a.ConvertValue(value, null);
                            else
                                physical = null;
                        }
                    }

                    string raw = BitConverter.ToString(value.RawValue);
                    r.AppendFormat(CultureInfo.InvariantCulture,
                                   " {0}{1}{2}{3}{4}{5}{6}{7}",
                                   value.Id.ToString("X2").PadRight(3),
                                   description.PadRight(35),
                                   raw.Replace("-", string.Empty).PadRight(13),
                                   value.WorstValue.ToString(CultureInfo.InvariantCulture).PadRight(6),
                                   value.CurrentValue.ToString(CultureInfo.InvariantCulture).PadRight(6),
                                   (threshold.HasValue
                                       ? threshold.Value.ToString(CultureInfo.InvariantCulture)
                                       : "-").PadRight(6),
                                   (physical.HasValue ? physical.Value.ToString(CultureInfo.InvariantCulture) : "-").PadRight(8),
                                   Environment.NewLine);
                }

                r.AppendLine();
            }
        }
    }

    protected static float RawToInt(byte[] raw, byte value, IReadOnlyList<IParameter> parameters)
    {
        return (raw[3] << 24) | (raw[2] << 16) | (raw[1] << 8) | raw[0];
    }

    public override void Close()
    {
        Smart.Close();
        base.Close();
    }
}