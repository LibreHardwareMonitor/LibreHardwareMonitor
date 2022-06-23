// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.Hardware.Gpu
{
    public abstract class GenericGpu : Hardware
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericGpu" /> class.
        /// </summary>
        /// <param name="name">Component name.</param>
        /// <param name="identifier">Identifier that will be assigned to the device. Based on <see cref="Identifier" /></param>
        /// <param name="settings">Additional settings passed by the <see cref="IComputer" />.</param>
        protected GenericGpu(string name, Identifier identifier, ISettings settings) : base(name, identifier, settings)
        { }

        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        public abstract string DeviceId { get; }
    }
}
