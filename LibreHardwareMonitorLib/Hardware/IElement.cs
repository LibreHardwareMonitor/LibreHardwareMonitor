// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware
{
    public interface IElement
    {
        // accept visitor on this element
        void Accept(IVisitor visitor);

        // call accept(visitor) on all child elements (called only from visitors)
        void Traverse(IVisitor visitor);
    }
}
