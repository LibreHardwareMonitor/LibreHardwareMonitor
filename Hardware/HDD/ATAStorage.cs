// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) 2009-2015 Michael Möller <mmoeller @openhardwaremonitor.org>
// Copyright (C) 2010 Paul Werelds
// Copyright (C) 2011 Roland Reinl <roland-reinl @gmx.de>
// Copyright (C) 2016-2019 Sebastian Grams <https://github.com/sebastian-dev>
// Copyright (C) 2016-2019 Aqua Computer <https://github.com/aquacomputer, info@aqua-computer.de>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;

namespace OpenHardwareMonitor.Hardware.HDD {
  public abstract class ATAStorage : AbstractStorage {

    // array of all harddrive types, matching type is searched in this order
    private static Type[] hddTypes = {
      typeof(SSDPlextor),
      typeof(SSDIntel),
      typeof(SSDSandforce),
      typeof(SSDIndilinx),
      typeof(SSDSamsung),
      typeof(SSDMicron),
      typeof(GenericHarddisk)
    };

    private ISmart smart { get; }
    private IList<SmartAttribute> smartAttributes { get; set; }
    private IDictionary<SmartAttribute, Sensor> sensors { get; set; }

    public ATAStorage(StorageInfo _storageInfo, ISmart smart, string name, string firmwareRevision, string id, int index, IEnumerable<SmartAttribute> smartAttributes, ISettings settings)
      : base(_storageInfo, name, firmwareRevision, id, index, settings) {
      this.smart = smart;
      if (smart.IsValid)
        smart.EnableSmart();
      this.smartAttributes = new List<SmartAttribute>(smartAttributes);
      CreateSensors();
    }

    public static AbstractStorage CreateInstance(StorageInfo info, ISettings settings) {
      ISmart smart = new WindowsSmart(info.Index);
      string name = null;
      string firmwareRevision = null;
      Interop.DriveAttributeValue[] values = { };

      if (smart.IsValid) {
        bool nameValid = smart.ReadNameAndFirmwareRevision(out name, out firmwareRevision);
        bool smartEnabled = smart.EnableSmart();

        if (smartEnabled)
          values = smart.ReadSmartData();

        if (!nameValid) {
          name = null;
          firmwareRevision = null;
        }
      }
      else {
        string[] logicalDrives = WindowsStorage.GetLogicalDrives(info.Index);
        if (logicalDrives == null || logicalDrives.Length == 0) {
          smart.Close();
          return null;
        }

        bool hasNonZeroSizeDrive = false;
        foreach (string logicalDrive in logicalDrives) {
          try {
            DriveInfo di = new DriveInfo(logicalDrive);
            if (di.TotalSize > 0) {
              hasNonZeroSizeDrive = true;
              break;
            }
          }
          catch (ArgumentException) {
          }
          catch (IOException) {
          }
          catch (UnauthorizedAccessException) {
          }
        }

        if (!hasNonZeroSizeDrive) {
          smart.Close();
          return null;
        }
      }

      if (string.IsNullOrEmpty(name))
        name = string.IsNullOrEmpty(info.Name) ? "Generic Hard Disk" : info.Name;

      if (string.IsNullOrEmpty(firmwareRevision))
        firmwareRevision = string.IsNullOrEmpty(info.Revision) ? "Unknown" : info.Revision;

      foreach (Type type in hddTypes) {
        // get the array of name prefixes for the current type
        NamePrefixAttribute[] namePrefixes = type.GetCustomAttributes(
          typeof(NamePrefixAttribute), true) as NamePrefixAttribute[];

        // get the array of the required SMART attributes for the current type
        RequireSmartAttribute[] requiredAttributes = type.GetCustomAttributes(
          typeof(RequireSmartAttribute), true) as RequireSmartAttribute[];

        // check if all required attributes are present
        bool allRequiredAttributesFound = true;
        foreach (var requireAttribute in requiredAttributes) {
          bool adttributeFound = false;
          foreach (Interop.DriveAttributeValue value in values) {
            if (value.Identifier == requireAttribute.AttributeId) {
              adttributeFound = true;
              break;
            }
          }
          if (!adttributeFound) {
            allRequiredAttributesFound = false;
            break;
          }
        }

        // if an attribute is missing, then try the next type
        if (!allRequiredAttributesFound)
          continue;

        // check if there is a matching name prefix for this type
        foreach (NamePrefixAttribute prefix in namePrefixes) {
          if (name.StartsWith(prefix.Prefix, StringComparison.InvariantCulture))
            return Activator.CreateInstance(type, info, smart, name, firmwareRevision, info.Index, settings) as ATAStorage;
        }
      }

      // no matching type has been found
      smart.Close();
      return null;
    }


    protected override sealed void CreateSensors() {
      sensors = new Dictionary<SmartAttribute, Sensor>();

      if (smart.IsValid) {

        var smartIds = smart.ReadSmartData()
                            .Select(attrValue => attrValue.Identifier);

        // unique attributes by SensorType and SensorChannel.
        var uniqueAtrributes = smartAttributes
            .Where(a => a.SensorType.HasValue)
            .Where(a => smartIds.Contains(a.Identifier))
            .GroupBy(a => new { a.SensorType.Value, a.SensorChannel })
            .Select(g => g.First());

        sensors = uniqueAtrributes.ToDictionary(attr => attr,
            attr => new Sensor(attr.SensorName,
              attr.SensorChannel, attr.DefaultHiddenSensor,
              attr.SensorType.Value, this, attr.ParameterDescriptions,
              settings));

        foreach (var sensor in sensors) {
          ActivateSensor(sensor.Value);
        }
      }

      base.CreateSensors();
    }

    public virtual void UpdateAdditionalSensors(Interop.DriveAttributeValue[] values) { }

    protected override void UpdateSensors() {
      if (smart.IsValid) {
        Interop.DriveAttributeValue[] values = smart.ReadSmartData();

        foreach (KeyValuePair<SmartAttribute, Sensor> keyValuePair in sensors) {
          SmartAttribute attribute = keyValuePair.Key;
          foreach (Interop.DriveAttributeValue value in values) {
            if (value.Identifier == attribute.Identifier) {
              Sensor sensor = keyValuePair.Value;
              sensor.Value = attribute.ConvertValue(value, sensor.Parameters);
            }
          }
        }

        UpdateAdditionalSensors(values);
      }
    }

    protected override void GetReport(StringBuilder r) {
      if (smart.IsValid) {
        Interop.DriveAttributeValue[] values = smart.ReadSmartData();
        Interop.DriveThresholdValue[] thresholds = smart.ReadSmartThresholds();

        if (values.Length > 0) {
          r.AppendFormat(CultureInfo.InvariantCulture,
            " {0}{1}{2}{3}{4}{5}{6}{7}",
            ("ID").PadRight(3),
            ("Description").PadRight(35),
            ("Raw Value").PadRight(13),
            ("Worst").PadRight(6),
            ("Value").PadRight(6),
            ("Thres").PadRight(6),
            ("Physical").PadRight(8),
            Environment.NewLine);

          foreach (Interop.DriveAttributeValue value in values) {
            if (value.Identifier == 0x00)
              break;

            byte? threshold = null;
            foreach (Interop.DriveThresholdValue t in thresholds) {
              if (t.Identifier == value.Identifier) {
                threshold = t.Threshold;
              }
            }

            string description = "Unknown";
            float? physical = null;
            foreach (SmartAttribute a in smartAttributes) {
              if (a.Identifier == value.Identifier) {
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
              value.Identifier.ToString("X2").PadRight(3),
              description.PadRight(35),
              raw.Replace("-", "").PadRight(13),
              value.WorstValue.ToString(CultureInfo.InvariantCulture).PadRight(6),
              value.AttrValue.ToString(CultureInfo.InvariantCulture).PadRight(6),
              (threshold.HasValue ? threshold.Value.ToString(
                CultureInfo.InvariantCulture) : "-").PadRight(6),
              (physical.HasValue ? physical.Value.ToString(CultureInfo.InvariantCulture) : "-").PadRight(8), Environment.NewLine);
          }
          r.AppendLine();
        }
      }
    }

    protected static float RawToInt(byte[] raw, byte value, IReadOnlyList<IParameter> parameters) {
      return (raw[3] << 24) | (raw[2] << 16) | (raw[1] << 8) | raw[0];
    }

    public override void Close() {
      smart.Close();

      base.Close();
    }
  }
}