using System;

namespace OpenHardwareMonitor.Hardware.Nvidia {
  internal class NvidiaNVMLGPU : NvidiaGPU {
    private readonly NVML nvml;
    private readonly NvmlDevice? device;
    private readonly Sensor powerUsage;

    public NvidiaNVMLGPU(int adapterIndex, NvPhysicalGpuHandle handle,
      NvDisplayHandle? displayHandle, ISettings settings, NVML nvml) 
	: base(adapterIndex, handle, displayHandle, settings) {
        if (nvml == null)
            throw new ArgumentNullException(nameof(nvml));

        this.nvml = nvml;
	
	    if (nvml.Initialised) {
	        device = nvml.NvmlDeviceGetHandleByIndex(adapterIndex);
            if (device.HasValue) {
                powerUsage = new Sensor("GPU Package", 0, SensorType.Power, this, settings);
	        }
        }
    }

    public override void Update() {
        base.Update();

        if (nvml.Initialised && device.HasValue) {
            var result = nvml.NvmlDeviceGetPowerUsage(device.Value);
            if (result.HasValue) {
                powerUsage.Value = (float)result.Value / 1000;
                ActivateSensor(powerUsage);
            }
        }
    }
  }
}
