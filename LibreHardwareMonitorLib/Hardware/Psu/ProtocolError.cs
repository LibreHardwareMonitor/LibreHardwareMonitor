using System;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Psu;

/// <summary>
/// Represents an error that occurs during communication with a PSU controller over USB.
/// </summary>
/// <param name="device">The HID device associated with the communication error. Cannot be null.</param>
/// <param name="message">The error message that describes the nature of the communication failure.</param>
public class ProtocolError(HidDevice device, string message) : ApplicationException($"Error communicating with the PSU controller at {device.DevicePath}: {message}");
