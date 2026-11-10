# Optional PresentMon FPS monitoring

PresentMon monitoring is **disabled by default**. It is independent of GPU sensor
monitoring and works with applications using NVIDIA, AMD, or Intel graphics.

## Setup

1. Install [Intel PresentMon 2.6.0](https://github.com/GameTechDev/PresentMon/releases/tag/v2.6.0)
   with its service. The integration requires public API **3.4**, accepts newer
   minor versions of API 3, and rejects incompatible major versions.
2. Use the **Windows x64** version of LHM with the x64 PresentMon middleware.
3. In the Windows Forms application, enable **File > Hardware > PresentMon FPS
   (foreground application)**.
4. Focus the game. The separate PresentMon node exposes **Presented FPS** and
   **Displayed FPS** as factor sensors, including through LHM's existing web/WMI
   interfaces. Its properties/report include the target PID, selected swap chain,
   and connection status.

No PresentMon UI or game overlay is needed. LHM does not install/start the service,
bundle binaries, collect GPU telemetry through PresentMon, or write frame logs.
The API DLL is loaded from the service installation's `sharedMiddlewarePath`
registry value, not from the current directory or a copied private DLL. Its
architecture must match the LHM process. An absent service, incompatible SDK,
or connection error leaves the FPS sensors unavailable and does not disable
other hardware monitoring. Connection failures are retried after five seconds.

The Avalonia frontend does not yet expose this option. Library consumers can
enable it on `Computer`:

```csharp
Computer computer = new()
{
    IsPresentMonEnabled = true,
    PresentMonProcessId = 1234 // 0 (default) follows the foreground window
};
computer.Open();
// Update hardware with the usual visitor, and call computer.Close() on shutdown.
```

## Measurement semantics

- Measurements use the SDK's one-second average window with a 200 ms offset.
  A new target gets a one-second warm-up period.
- **Presented FPS** is the SDK's `PM_METRIC_PRESENTED_FPS`; it is not labelled
  native/rendered FPS. **Displayed FPS** is `PM_METRIC_DISPLAYED_FPS`. Frame
  generation and presentation modes can make them differ; support depends on the
  game, driver, and PresentMon. LHM does not infer generated frames itself.
- For a process with multiple swap chains, choose the one with the highest valid
  presented FPS among the first 64 returned by the SDK. Do not sum unrelated
  windows. This is a heuristic, not detection of the main game window.
- Alt-tabbing changes the foreground target, including to a browser or launcher.
  Use `PresentMonProcessId` to pin a process from a library client.
- Min/max and history reset when the target PID or selected swap chain changes.
  Missing/invalid values are null. Frame timestamps older than two seconds
  invalidate cached FPS (including paused/exited applications).
- Disabling the option or closing LHM stops this client's tracking and releases
  its queries/session. It does not stop a service used by other clients.

## Maintenance and validation

The implementation uses the official public C API, not private PresentMon
internals or a port of its ETW logic. The managed binding follows
[`PresentMonAPI.h` at v2.6.0](https://github.com/GameTechDev/PresentMon/blob/v2.6.0/IntelPresentMon/PresentMonAPI2/PresentMonAPI.h).
Dynamic query records in that middleware are padded to 16 bytes. Returned field
offsets/sizes are validated before reading. The frame query supplies its record
size explicitly.

See [the regression harness](../Tests/PresentMon/README.md) for deterministic ABI,
freshness, cleanup, and opt-in lifecycle tests. Live verification should cover a
running game, alt-tab, pause/exit, and service restart. Compare performance with
monitoring enabled/disabled; integration does not eliminate ETW processing cost.

PresentMon API declarations are MIT licensed; see
[the notice](../Licenses/PresentMon.txt). LHM's implementation remains MPL-2.0.
