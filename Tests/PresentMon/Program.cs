// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.

using System.Reflection;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;

if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
    throw new PlatformNotSupportedException("Run these native ABI tests on Windows x64.");

// Optional live SDK probe: --live <absolute middleware DLL path> <target PID>.
if (args[0] == "--live")
{
    Type api = typeof(Computer).Assembly.GetType("LibreHardwareMonitor.Interop.PresentMon", true)!;
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    using IDisposable client = (IDisposable)Activator.CreateInstance(api, flags, null, new object[] { Path.GetFullPath(args[1]), args.Length > 3 ? args[3] : null }, null)!;
    if (args.Length > 3)
    {
        IntPtr native = NativeLibrary.Load(Path.GetFullPath(args[1]));
        try
        {
            IntPtr session = (IntPtr)api.GetField("_session", flags)!.GetValue(client)!;
            int status = Marshal.GetDelegateForFunctionPointer<PlaybackDelegate>(NativeLibrary.GetExport(native, "pmStartPlaybackTracking"))(session, uint.Parse(args[2]), 0);
            if (status != 0) throw new Exception($"Playback status {status}");
            api.GetField("_processId", flags)!.SetValue(client, uint.Parse(args[2]));
        }
        finally { NativeLibrary.Free(native); }
    }
    else
        api.GetMethod("Track", flags)!.Invoke(client, new object[] { uint.Parse(args[2]) });
    for (int i = 0; i < 8; i++)
    {
        Thread.Sleep(1000);
        Console.WriteLine(api.GetMethod("Poll", flags)!.Invoke(client, null));
        Console.WriteLine($"Last frame QPC: {api.GetField("_lastFrameTimestamp", flags)!.GetValue(client)}, now: {System.Diagnostics.Stopwatch.GetTimestamp()}");
    }
    return;
}

string path = Path.GetFullPath(args[0]);
IntPtr module = NativeLibrary.Load(path);
Action<int> configure = Marshal.GetDelegateForFunctionPointer<ConfigureDelegate>(NativeLibrary.GetExport(module, "Configure")).Invoke;
Func<int> resources = Marshal.GetDelegateForFunctionPointer<ResourcesDelegate>(NativeLibrary.GetExport(module, "Resources")).Invoke;
Type adapter = typeof(Computer).Assembly.GetType("LibreHardwareMonitor.Interop.PresentMon", true)!;
const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
int passed = 0;

object Create() => Activator.CreateInstance(adapter, Flags, null, new object[] { path, null }, null)!;
void Track(object client, uint pid) => adapter.GetMethod("Track", Flags)!.Invoke(client, new object[] { pid });
(float?, float?, ulong) Poll(object client) => ((float?, float?, ulong))adapter.GetMethod("Poll", Flags)!.Invoke(client, null)!;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
void Test(string name, Action action)
{
    configure(0);
    action();
    Check(resources() == 0, name + ": leaked native resources");
    Console.WriteLine("PASS " + name);
    passed++;
}
void Fails(Action action)
{
    try { action(); }
    catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { return; }
    throw new Exception("Expected an SDK failure.");
}

try
{
    Test("query ABI, returned offsets, padded stride and busiest swap chain", () =>
    {
        using IDisposable client = (IDisposable)Create();
        Check(Poll(client) == default, "Must not poll without a target");
        Track(client, 123);
        Check(Poll(client) == (144f, 288f, 0x1001UL), "Incorrect FPS / swap chain");
        Track(client, 456);
        Check(resources() == 4, "Old process tracking was not released");
        Track(client, 0);
        Check(Poll(client) == default, "Stopped target still has FPS");
        client.Dispose(); // idempotent even though using disposes again
    });
    Test("stale frame timestamps invalidate cached FPS", () =>
    {
        configure(1);
        using IDisposable client = (IDisposable)Create();
        Track(client, 123);
        Check(Poll(client) == default, "Stale FPS was returned");
    });
    foreach (int mode in new[] { 2, 7 })
    {
        Test("missing/invalid samples " + mode, () =>
        {
            configure(mode);
            using IDisposable client = (IDisposable)Create();
            Track(client, 123);
            Check(Poll(client) == default, "Missing samples must not become zero FPS");
        });
    }
    Test("non-finite displayed FPS does not invalidate presented FPS", () =>
    {
        configure(8);
        using IDisposable client = (IDisposable)Create();
        Track(client, 123);
        Check(Poll(client) == (144f, null, 0x1001UL), "Invalid displayed FPS was accepted");
    });
    foreach (int mode in new[] { 4, 5, 6 })
        Test("initialization failure releases partial resources " + mode, () => { configure(mode); Fails(() => Create()); });
    foreach (int mode in new[] { 3, 9 })
    {
        Test("service error / corrupt count " + mode, () =>
        {
            using IDisposable client = (IDisposable)Create();
            Track(client, 123);
            configure(mode);
            Fails(() => Poll(client));
        });
    }
    Test("Computer opt-in lifecycle", () =>
    {
        Computer computer = new();
        Check(!computer.IsPresentMonEnabled, "FPS must be disabled by default");
        computer.Open();
        try
        {
            Check(!computer.Hardware.Any(h => h.HardwareType == HardwareType.PresentMon), "Unexpected FPS hardware");
            computer.IsPresentMonEnabled = true;
            IHardware hardware = computer.Hardware.Single(h => h.HardwareType == HardwareType.PresentMon);
            Check(hardware.Sensors.Length == 2 && hardware.Sensors.All(s => s.Value == null), "Sensors must start unavailable");
            computer.IsPresentMonEnabled = false;
            Check(!computer.Hardware.Any(h => h.HardwareType == HardwareType.PresentMon), "FPS hardware not removed");
        }
        finally { computer.Close(); }
    });
    foreach (bool switchTarget in new[] { false, true })
    {
        Test(switchTarget ? "target switch clears history" : "SDK errors leave hardware available and FPS null", () =>
        {
            Computer computer = new() { IsPresentMonEnabled = true, PresentMonProcessId = 123 };
            computer.Open();
            try
            {
                IHardware hardware = computer.Hardware.Single();
                Type type = hardware.GetType();
                object client = Create();
                Track(client, 123);
                type.GetField("_client", Flags)!.SetValue(hardware, client);
                type.GetField("_processId", Flags)!.SetValue(hardware, 123u);
                hardware.Update();
                Check(hardware.Sensors.Single(s => s.Index == 0).Value == 144f, "Hardware did not expose SDK FPS");
                if (switchTarget)
                {
                    // Prevent a real service connection after switching away from the stub.
                    type.GetField("_nextAttempt", Flags)!.SetValue(hardware, long.MaxValue);
                    computer.PresentMonProcessId = 456;
                }
                else
                    configure(3);

                hardware.Update();
                Check(hardware.Sensors.All(s => s.Value == null), "Previous FPS remained visible");
                Check(resources() == 0, "Native client was not released");
                if (switchTarget)
                    Check(hardware.Sensors.All(s => s.Min == null && s.Max == null && !s.Values.Any()), "Previous application's history survived");
                else
                    Check(hardware.Properties["Status"].Contains("PM_STATUS 13"), "Missing error diagnostic");
            }
            finally { computer.Close(); }
        });
    }
    Console.WriteLine($"{passed} regression cases passed.");
}
finally { NativeLibrary.Free(module); }

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void ConfigureDelegate(int mode);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int ResourcesDelegate();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int PlaybackDelegate(IntPtr session, uint processId, uint backpressured);
