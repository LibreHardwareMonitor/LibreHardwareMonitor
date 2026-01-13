// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using System.Threading;
using HidSharp;

namespace LibreHardwareMonitor.Hardware.Controller.Arctic
{
    internal class ArcticFanController : Hardware
    {
        private const int CHANNEL_COUNT = 10;
        private const int PACKET_SIZE = 32;
        private const int CONTROL_VALUE_MIN = 0;
        private const int CONTROL_VALUE_MAX = 100;
        private const int CONTROL_RESET_VALUE = 30;
        private const int TIMEOUT_MS = 1000;

        private HidStream _hidStream;
        private readonly object _hidLock = new();
        private readonly object _controlLock = new();
        private readonly Thread _thread;
        private readonly float[] _requestedFanSpeedsPercent = new float[CHANNEL_COUNT];
        private readonly float[] _currentFanRpms = new float[CHANNEL_COUNT];
        private readonly float[] _currentDevicePwmValues = new float[CHANNEL_COUNT]; // Current PWM values from device
        private bool _sendPwmRequested;
        private bool _pwmValuesInitialized = false; // Track if we've read initial PWM values from device
        private readonly System.Collections.Generic.List<Sensor> _rpmSensors = new();
        private readonly System.Collections.Generic.List<Sensor> _controlSensors = new();

        public ArcticFanController(HidDevice dev, ISettings settings) : base("Arctic Fan Controller", new Identifier(dev), settings)
        {
            if (dev.TryOpen(out HidStream hidStream))
            {
                // Create fan sensors (RPM monitoring) - all 10 fans have RPM feedback
                for (int i = 1; i <= CHANNEL_COUNT; i++)
                {
                    var fanSensor = new Sensor($"Arctic Controller Fan {i}", i, SensorType.Fan, this, settings);
                    ActivateSensor(fanSensor);
                    _rpmSensors.Add(fanSensor);
                }

                // Create control sensors - all 10 fans can be controlled
                for (int i = 1; i <= CHANNEL_COUNT; i++)
                {
                    var controlSensor = new Sensor($"Arctic Controller Fan Control {i}", i, SensorType.Control, this, settings);
                    Control control = new(controlSensor, settings, CONTROL_VALUE_MIN, CONTROL_VALUE_MAX);
                    control.ControlModeChanged += Control_ControlModeChanged;
                    control.SoftwareControlValueChanged += Control_SoftwareControlValueChanged;

                    controlSensor.Control = control;
                    ActivateSensor(controlSensor); // Activate the control sensor so it appears in the UI
                    _controlSensors.Add(controlSensor);
                }

                _hidStream = hidStream;
                _hidStream.ReadTimeout = TIMEOUT_MS;
                _hidStream.WriteTimeout = TIMEOUT_MS;

                // create thread
                _thread = new Thread(ThreadHidLoop);
                _thread.IsBackground = true; // Allow app to close even if thread is running
                _thread.Start(); // Start the thread to read RPM data
            }
        }

        private void ThreadHidLoop()
        {
            while (_hidStream != null)
            {
                lock (_hidLock)
                {
                    if (_hidStream == null) return;

                    UpdateRpmAndPwmValues();
                    SendPWMUpdateIfRequired();
                }

                Thread.Sleep(500);
            }
        }

        private void Control_SoftwareControlValueChanged(Control control)
        {
            // need PWM update
            lock (_controlLock)
            {
                var value = control.ControlMode switch
                {
                    ControlMode.Software => Math.Max(Math.Min(control.SoftwareValue, CONTROL_VALUE_MAX), CONTROL_VALUE_MIN),
                    _ => CONTROL_RESET_VALUE,
                };

                _requestedFanSpeedsPercent[control.Sensor.Index - 1] = value;
                _sendPwmRequested = true;
            }

            // update the sensor value
            (control.Sensor as Sensor)?.Value = control.ControlMode == ControlMode.Software ? control.SoftwareValue : null;
        }

        private void Control_ControlModeChanged(Control control)
        {
            Control_SoftwareControlValueChanged(control);
        }

        public override HardwareType HardwareType { get; } = HardwareType.EmbeddedController;

        public override void Update()
        {
            lock (_hidLock)
            {
                foreach (Sensor sensor in _rpmSensors)
                {
                    sensor.Value = GetRPM(sensor.Index);
                }

                // Update control sensor values to reflect current device PWM values
                // This ensures the UI shows the actual current values from the device quickly
                foreach (Sensor sensor in _controlSensors)
                {
                    int idx = sensor.Index - 1;
                    if (idx >= 0 && idx < CHANNEL_COUNT &&
                        // Only update if sensor doesn't have a manual value set (to avoid overwriting user input)
                        sensor.Control?.ControlMode != ControlMode.Software)
                    {
                        // Use current device PWM values for immediate display, fallback to requested if not initialized yet
                        float displayValue = _pwmValuesInitialized ? _currentDevicePwmValues[idx] : _requestedFanSpeedsPercent[idx];
                        sensor.Value = displayValue;
                    }

                }
            }
        }

        public override void Close()
        {
            lock (_hidLock)
            {
                try
                {
                    lock (_controlLock)
                    {
                        // Set all fans to 30% before closing (like JS does)
                        for (int i = 0; i < CHANNEL_COUNT; i++)
                        {
                            _requestedFanSpeedsPercent[i] = CONTROL_RESET_VALUE;
                        }

                        _sendPwmRequested = true;
                    }

                    SendPWMUpdateIfRequired();

                }
                catch { }

                try
                {
                    _hidStream?.Close();
                    _hidStream?.Dispose();

                }
                catch { }
                // make sure stream is null so the thread can exit
                finally
                {
                    _hidStream = null;
                }
            }

            // wait for thread to finish
            _thread?.Join(1000);

            base.Close();
        }

        private void UpdateRpmAndPwmValues()
        {
            if (_hidStream is null)
            {
                return;
            }

            if (_hidStream is null)
            {
                return;
            }

            try
            {
                // Try to read available data (device sends periodically)
                byte[] response = null;
                int attempts = 0;
                const int maxAttempts = 3;

                while (attempts < maxAttempts)
                {
                    try
                    {
                        // Set short timeout to check if data is available
                        var originalTimeout = _hidStream.ReadTimeout;
                        _hidStream.ReadTimeout = 200;

                        response = new byte[PACKET_SIZE];
                        int bytesRead = _hidStream.Read(response, 0, PACKET_SIZE);

                        _hidStream.ReadTimeout = originalTimeout;

                        if (bytesRead >= PACKET_SIZE && response[0] == 0x01)
                        {
                            // Valid response with Report ID 0x01
                            break;
                        }
                    }
                    catch (TimeoutException)
                    {
                        // No data available yet, continue trying
                    }
                    catch
                    {
                        // Retry on other errors
                    }

                    attempts++;
                    if (attempts < maxAttempts) Thread.Sleep(50);
                }

                ProcessResponse(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RPM update failed: {ex.Message}");
            }
        }

        private void ProcessResponse(byte[] response)
        {
            if (response == null || response.Length < PACKET_SIZE || response[0] != 0x01)
            {
                return;
            }

            // Parse current PWM values from bytes 1-10 (sent by device)
            // Format: [Report ID=0x01, PWM[1-10] (bytes 1-10), RPM[1-10] (bytes 11-30, 2 bytes each), padding]
            for (int i = 0; i < CHANNEL_COUNT; i++)
            {
                if (1 + i < response.Length)
                {
                    _currentDevicePwmValues[i] = response[1 + i];
                }
            }

            // Initialize requested PWM values with current device values on first read
            // This prevents other fans from resetting to 0% when one fan is set to manual
            if (!_pwmValuesInitialized)
            {
                lock (_controlLock)
                {
                    for (int i = 0; i < CHANNEL_COUNT; i++)
                    {
                        _requestedFanSpeedsPercent[i] = _currentDevicePwmValues[i];
                    }
                    _pwmValuesInitialized = true;
                }
            }

            // Parse RPM values from bytes 11-30 (10 RPM values as uint16 little-endian)
            for (int i = 0; i < CHANNEL_COUNT; i++)
            {
                int rpmIndex = 11 + i * 2;
                if (rpmIndex + 1 < response.Length)
                {
                    int rpmLow = response[rpmIndex];
                    int rpmHigh = response[rpmIndex + 1];
                    _currentFanRpms[i] = rpmLow | (rpmHigh << 8);
                }
            }
        }

        private void SendPWMUpdateIfRequired()
        {
            float[] values = null;
            lock (_controlLock)
            {
                if (!_sendPwmRequested)
                {
                    return;
                }

                _sendPwmRequested = false;
                values = _requestedFanSpeedsPercent.ToArray();
            }

            try
            {
                // New format: [Report ID=0x01, PWM[0-9] (10 bytes), padding (21 bytes)]
                byte[] pwmPacket = new byte[PACKET_SIZE];
                pwmPacket[0] = 0x01; // Report ID

                // Set all fan speeds (bytes 1-10)
                for (int i = 0; i < CHANNEL_COUNT; i++)
                {
                    pwmPacket[1 + i] = (byte)Math.Round(values[i]);
                }
                // Rest are zeros (already initialized)

                _hidStream.Write(pwmPacket);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PWM update failed: {ex.Message}");
            }
        }

        private float GetRPM(int fanIndex)
        {
            if (fanIndex < 1 || fanIndex > CHANNEL_COUNT)
            {
                return 0;
            }

            return _currentFanRpms[fanIndex - 1];
        }
    }
}
