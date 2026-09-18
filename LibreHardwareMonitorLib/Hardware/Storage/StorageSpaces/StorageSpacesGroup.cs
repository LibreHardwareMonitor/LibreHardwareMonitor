// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace LibreHardwareMonitor.Hardware.Storage.StorageSpaces;

/// <summary>
/// Contributes a node per Storage Spaces pool. Empty when the machine has no pool, so it needs no
/// enable flag of its own.
/// </summary>
internal class StorageSpacesGroup : IGroup
{
    private readonly List<StorageSpacesPool> _hardware = new();

    public StorageSpacesGroup(ISettings settings)
    {
        StorageSpacesSnapshot snapshot = StorageSpacesData.Initialize();
        if (snapshot == null)
            return;

        for (int i = 0; i < snapshot.Pools.Count; i++)
            _hardware.Add(new StorageSpacesPool(snapshot.Pools[i], i, settings));
    }

    public IReadOnlyList<IHardware> Hardware => _hardware;

    public void Close()
    {
        foreach (StorageSpacesPool pool in _hardware)
            pool.Close();

        StorageSpacesData.Close();
    }

    public string GetReport() => null;
}
