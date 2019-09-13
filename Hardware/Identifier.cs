// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.Text;

namespace LibreHardwareMonitor.Hardware
{
    public class Identifier : IComparable<Identifier>
    {
        private readonly string _identifier;
        private const char Separator = '/';

        private static void CheckIdentifiers(IEnumerable<string> identifiers)
        {
            foreach (string s in identifiers)
            {
                if (s.Contains(" ") || s.Contains(Separator.ToString()))
                    throw new ArgumentException("Invalid identifier");
            }
        }

        public Identifier(params string[] identifiers)
        {
            CheckIdentifiers(identifiers);
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < identifiers.Length; i++)
            {
                s.Append(Separator);
                s.Append(identifiers[i]);
            }
            _identifier = s.ToString();
        }

        public Identifier(Identifier identifier, params string[] extensions)
        {
            CheckIdentifiers(extensions);
            StringBuilder s = new StringBuilder();
            s.Append(identifier.ToString());
            for (int i = 0; i < extensions.Length; i++)
            {
                s.Append(Separator);
                s.Append(extensions[i]);
            }
            _identifier = s.ToString();
        }

        public override string ToString()
        {
            return _identifier;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            Identifier id = obj as Identifier;
            if (id == null)
                return false;
            return (_identifier == id._identifier);
        }

        public override int GetHashCode()
        {
            return _identifier.GetHashCode();
        }

        public int CompareTo(Identifier other)
        {
            if (other == null)
                return 1;
            else
                return string.Compare(this._identifier, other._identifier,
                  StringComparison.Ordinal);
        }

        public static bool operator ==(Identifier id1, Identifier id2)
        {
            if (id1.Equals(null))
                return id2.Equals(null);
            else
                return id1.Equals(id2);
        }

        public static bool operator !=(Identifier id1, Identifier id2)
        {
            return !(id1 == id2);
        }

        public static bool operator <(Identifier id1, Identifier id2)
        {
            if (id1 == null)
                return id2 != null;
            else
                return (id1.CompareTo(id2) < 0);
        }

        public static bool operator >(Identifier id1, Identifier id2)
        {
            if (id1 == null)
                return false;
            else
                return (id1.CompareTo(id2) > 0);
        }
    }
}
