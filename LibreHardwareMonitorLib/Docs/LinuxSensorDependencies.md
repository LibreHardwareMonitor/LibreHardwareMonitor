# Linux Sensor Dependencies

This document clarifies runtime and package dependencies for Linux sensor paths used by LibreHardwareMonitor in this repository.

## Scope

- LMSensors motherboard path: `Hardware/Motherboard/Lpc/LMSensors.cs`
- Intel CPU core temperature path: `Hardware/Cpu/LinuxIntelCpu.cs`

## Dependency Matrix

| Component | Runtime Linux Requirements | NuGet/Package Requirements | Optional Tools |
|---|---|---|---|
| LMSensors (`LMSensors.cs`) | `/sys/class/hwmon` is present; supported Super I/O kernel drivers loaded; access to `in*_input`, `temp*_input`, `fan*_input`, `pwm*` files | None specific to LMSensors | `lm-sensors` userspace tools (`sensors`, `sensors-detect`) are useful for diagnostics only |
| Intel coretemp (`LinuxIntelCpu.cs`) | Intel `coretemp` driver loaded; hwmon node exports `temp*_input` and `temp*_label`; read access to these files | None specific to LinuxIntelCpu | `lm-sensors` userspace tools are useful for diagnostics only |

## Important Clarifications

1. The implementation reads sysfs directly. It does not execute `sensors`.
2. If `sensors` works but the app does not, check file permissions and path discovery assumptions under `/sys/class/hwmon`.
3. Fan control writes (LMSensors PWM paths) require write permissions and chip mode support.

## Non-root Write Permissions (LMSensors `SetControl`)

In `LMSensors.cs`, write operations happen in `LMChip.SetControl` and target sysfs nodes such as `pwm*` and `pwm*_enable`.

For non-root operation, the running user must have write access to those nodes.

Recommended Linux-side setup:

1. Use a dedicated group for hwmon fan control users.
2. Apply udev rules so matching `pwm*` and `pwm*_enable` files are assigned to that group with `g+rw`.
3. Ensure the process user is in that group and has refreshed group membership.

Without this setup, read-only telemetry can still work while `SetControl` writes fail with permission errors.

## Related Project Packages

The project contains general dependencies in `LibreHardwareMonitorLib.csproj`, but there is no dedicated NuGet dependency that powers only LMSensors or Linux Intel coretemp reading.
