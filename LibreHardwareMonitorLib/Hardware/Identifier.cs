// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace LibreHardwareMonitor.Hardware;

/// <summary>
/// Represents a unique <see cref="ISensor" />/<see cref="IHardware" /> identifier in text format with a / separator.
/// </summary>
public class Identifier : IComparable<Identifier>
{
    private const char Separator = '/';
    private readonly string _identifier;

    public Identifier(params string[] identifiers)
    {
        CheckIdentifiers(identifiers);
        StringBuilder s = new();
        for (int i = 0; i < identifiers.Length; i++)
        {
            s.Append(Separator);
            s.Append(identifiers[i]);
        }

        _identifier = s.ToString();
    }

    /// <summary>
    /// Creates a new identifier instance based on the base <see cref="Identifier" /> and additional elements.
    /// </summary>
    /// <param name="identifier">Base identifier being the beginning of the new one.</param>
    /// <param name="extensions">Additional parts by which the base <see cref="Identifier" /> will be extended.</param>
    public Identifier(Identifier identifier, params string[] extensions)
    {
        CheckIdentifiers(extensions);
        StringBuilder s = new();
        s.Append(identifier);
        for (int i = 0; i < extensions.Length; i++)
        {
            s.Append(Separator);
            s.Append(extensions[i]);
        }

        _identifier = s.ToString();
    }

    /// <inheritdoc />
    public int CompareTo(Identifier other)
    {
        if (other == null)
            return 1;

        return string.Compare(_identifier,
                              other._identifier,
                              StringComparison.Ordinal);
    }

    private static void CheckIdentifiers(IEnumerable<string> identifiers)
    {
        foreach (string s in identifiers)
        {
            if (s.Contains(" ") || s.Contains(Separator.ToString()))
                throw new ArgumentException("Invalid identifier");
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _identifier;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;

        Identifier id = obj as Identifier;
        if (id == null)
            return false;

        return _identifier == id._identifier;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _identifier.GetHashCode();
    }

    public static bool operator ==(Identifier id1, Identifier id2)
    {
        if (id1 is null && id2 is null)
            return true;

        return id1 is not null && id1.Equals(id2);
    }

    public static bool operator !=(Identifier id1, Identifier id2)
    {
        return !(id1 == id2);
    }

    public static bool operator <(Identifier id1, Identifier id2)
    {
        if (id1 == null)
            return id2 != null;

        return id1.CompareTo(id2) < 0;
    }

    public static bool operator >(Identifier id1, Identifier id2)
    {
        if (id1 == null)
            return false;

        return id1.CompareTo(id2) > 0;
    }
}