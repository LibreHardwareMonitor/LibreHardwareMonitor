// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Threading.Tasks;

namespace LibreHardwareMonitor.Hardware.Storage.StorageSpaces;

/// <summary>
/// Owns the shared <see cref="StorageSpacesSnapshot" /> and refreshes it off the update thread.
/// </summary>
/// <remarks>
/// A snapshot costs several hundred milliseconds -- measured at ~600 ms for a four disk pool, of
/// which ~70 ms is a reliability counter query per disk -- so it is far too slow to take on the
/// update thread, however infrequently. Updates therefore only ever read the last completed
/// snapshot while a refresh runs in the background.
/// </remarks>
internal static class StorageSpacesData
{
    private static readonly object _lock = new();

    private static StorageSpacesSnapshot _snapshot;
    private static Task _refresh;
    private static DateTime _lastRefresh = DateTime.MinValue;
    private static bool _closed;

    /// <summary>
    /// Gets or sets how long to wait between refreshes. Health and capacity change slowly, and a
    /// refresh is expensive, so this is deliberately much longer than the update interval.
    /// </summary>
    public static TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the most recently completed snapshot, or <see langword="null" /> if there is none.
    /// </summary>
    public static StorageSpacesSnapshot Snapshot
    {
        get
        {
            lock (_lock)
                return _snapshot;
        }
    }

    /// <summary>
    /// Reads a snapshot synchronously. Used while building the hardware tree, which cannot be
    /// populated from a snapshot that does not exist yet.
    /// </summary>
    public static StorageSpacesSnapshot Initialize()
    {
        StorageSpacesSnapshot snapshot = StorageSpacesSnapshot.Create();

        lock (_lock)
        {
            _closed = false;
            _snapshot = snapshot;
            _lastRefresh = DateTime.UtcNow;
        }

        return snapshot;
    }

    /// <summary>
    /// Starts a background refresh if one is due. Never blocks.
    /// </summary>
    public static void Update()
    {
        lock (_lock)
        {
            if (_closed || (_refresh != null && !_refresh.IsCompleted))
                return;

            if (DateTime.UtcNow - _lastRefresh < UpdateInterval)
                return;

            _refresh = Task.Run(Refresh);
        }
    }

    public static void Close()
    {
        lock (_lock)
        {
            _closed = true;
            _snapshot = null;
        }
    }

    private static void Refresh()
    {
        StorageSpacesSnapshot snapshot = StorageSpacesSnapshot.Create();

        lock (_lock)
        {
            // Discard a refresh that finished after the group was closed.
            if (_closed)
                return;

            _snapshot = snapshot;
            _lastRefresh = DateTime.UtcNow;
        }
    }
}
