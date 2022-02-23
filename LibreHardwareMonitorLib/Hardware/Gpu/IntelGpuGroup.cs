using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibreHardwareMonitor.Hardware.Gpu
{
    internal class IntelGpuGroup : IGroup
    {
        private readonly List<Hardware> _hardware = new();
        private readonly StringBuilder _report = new();

        public IntelGpuGroup(CPU.IntelCpu intelCpu, ISettings settings)
        {
            if (!Software.OperatingSystem.IsUnix && intelCpu != null)
            {
                _report.AppendLine("Intel GPU (D3D)");
                _report.AppendLine();

                var ids = D3DDisplayDevice.GetDeviceIdentifiers();
                
                _report.Append("Number of adapters: ");
                _report.AppendLine(ids.Length.ToString(CultureInfo.InvariantCulture));
                _report.AppendLine();

                for (var i = 0; i < ids.Length; i++)
                {
                    var deviceId = ids[i];
                    var isIntel = deviceId.IndexOf("VEN_8086") != -1;

                    _report.Append("AdapterIndex: ");
                    _report.AppendLine(i.ToString(CultureInfo.InvariantCulture));
                    _report.Append("DeviceId: ");
                    _report.AppendLine(deviceId);
                    _report.Append("IsIntel: ");
                    _report.AppendLine(isIntel.ToString(CultureInfo.InvariantCulture));

                    if (isIntel)
                    {
                        if (D3DDisplayDevice.GetDeviceInfoByIdentifier(deviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
                        {
                            _report.Append("GpuSharedLimit: ");
                            _report.AppendLine(deviceInfo.GpuSharedLimit.ToString(CultureInfo.InvariantCulture));
                            _report.Append("GpuSharedUsed: ");
                            _report.AppendLine(deviceInfo.GpuSharedUsed.ToString(CultureInfo.InvariantCulture));
                            _report.Append("GpuSharedMax: ");
                            _report.AppendLine(deviceInfo.GpuSharedMax.ToString(CultureInfo.InvariantCulture));
                            _report.Append("GpuDedicatedLimit: ");
                            _report.AppendLine(deviceInfo.GpuDedicatedLimit.ToString(CultureInfo.InvariantCulture));
                            _report.Append("GpuDedicatedUsed: ");
                            _report.AppendLine(deviceInfo.GpuDedicatedUsed.ToString(CultureInfo.InvariantCulture));
                            _report.Append("GpuDedicatedMax: ");
                            _report.AppendLine(deviceInfo.GpuDedicatedMax.ToString(CultureInfo.InvariantCulture));
                            _report.Append("Integrated: ");
                            _report.AppendLine(deviceInfo.Integrated.ToString(CultureInfo.InvariantCulture));

                            if (deviceInfo.Integrated)
                            {
                                _hardware.Add(new IntelIntegratedGpu(intelCpu, deviceId, deviceInfo, settings));
                            }
                        }
                    }

                    _report.AppendLine();
                }
            }
        }

        public IReadOnlyList<IHardware> Hardware => _hardware;

        public string GetReport()
        {
            return _report.ToString();
        }

        public void Close()
        {
            foreach (Hardware gpu in _hardware)
                gpu.Close();
        }
    }
}
