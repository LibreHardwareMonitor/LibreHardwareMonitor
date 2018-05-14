using System;
using System.Threading.Tasks;

namespace HidLibrary
{
    public delegate void InsertedEventHandler();
    public delegate void RemovedEventHandler();

    public enum DeviceMode
    {
        NonOverlapped = 0,
        Overlapped = 1
    }

    [Flags]
    public enum ShareMode
    {
        Exclusive = 0,
        ShareRead = NativeMethods.FILE_SHARE_READ,
        ShareWrite = NativeMethods.FILE_SHARE_WRITE
    }

    public delegate void ReadCallback(HidDeviceData data);
    public delegate void ReadReportCallback(HidReport report);
    public delegate void WriteCallback(bool success);

    public interface IHidDevice : IDisposable
    {
        event InsertedEventHandler Inserted;
        event RemovedEventHandler Removed;

        IntPtr Handle { get; }
        bool IsOpen { get; }
        bool IsConnected { get; }
        string Description { get; }
        HidDeviceCapabilities Capabilities { get; }
        HidDeviceAttributes Attributes { get;  }
        string DevicePath { get; }

        bool MonitorDeviceEvents { get; set; }

        void OpenDevice();

        void OpenDevice(DeviceMode readMode, DeviceMode writeMode, ShareMode shareMode);
        
        void CloseDevice();

        HidDeviceData Read();

        void Read(ReadCallback callback);

        void Read(ReadCallback callback, int timeout);

        Task<HidDeviceData> ReadAsync(int timeout = 0);

        HidDeviceData Read(int timeout);

        void ReadReport(ReadReportCallback callback);

        void ReadReport(ReadReportCallback callback, int timeout);

        Task<HidReport> ReadReportAsync(int timeout = 0);

        HidReport ReadReport(int timeout);
        HidReport ReadReport();

        bool ReadFeatureData(out byte[] data, byte reportId = 0);

        bool ReadProduct(out byte[] data);

        bool ReadManufacturer(out byte[] data);

        bool ReadSerialNumber(out byte[] data);

        void Write(byte[] data, WriteCallback callback);

        bool Write(byte[] data);

        bool Write(byte[] data, int timeout);

        void Write(byte[] data, WriteCallback callback, int timeout);

        Task<bool> WriteAsync(byte[] data, int timeout = 0);

        void WriteReport(HidReport report, WriteCallback callback);

        bool WriteReport(HidReport report);

        bool WriteReport(HidReport report, int timeout);

        void WriteReport(HidReport report, WriteCallback callback, int timeout);

        Task<bool> WriteReportAsync(HidReport report, int timeout = 0);

        HidReport CreateReport();

        bool WriteFeatureData(byte[] data);
    }
}
