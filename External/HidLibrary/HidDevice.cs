using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HidLibrary
{
    public class HidDevice : IHidDevice
    {
        public event InsertedEventHandler Inserted;
        public event RemovedEventHandler Removed;

        private readonly string _description;
        private readonly string _devicePath;
        private readonly HidDeviceAttributes _deviceAttributes;

        private readonly HidDeviceCapabilities _deviceCapabilities;
        private DeviceMode _deviceReadMode = DeviceMode.NonOverlapped;
        private DeviceMode _deviceWriteMode = DeviceMode.NonOverlapped;
        private ShareMode _deviceShareMode = ShareMode.ShareRead | ShareMode.ShareWrite;

        private readonly HidDeviceEventMonitor _deviceEventMonitor;

        private bool _monitorDeviceEvents;
        protected delegate HidDeviceData ReadDelegate(int timeout);
        protected delegate HidReport ReadReportDelegate(int timeout);
        private delegate bool WriteDelegate(byte[] data, int timeout);
        private delegate bool WriteReportDelegate(HidReport report, int timeout);

        internal HidDevice(string devicePath, string description = null)
        {
            _deviceEventMonitor = new HidDeviceEventMonitor(this);
            _deviceEventMonitor.Inserted += DeviceEventMonitorInserted;
            _deviceEventMonitor.Removed += DeviceEventMonitorRemoved;

            _devicePath = devicePath;
            _description = description;

            try
            {
                var hidHandle = OpenDeviceIO(_devicePath, NativeMethods.ACCESS_NONE);

                _deviceAttributes = GetDeviceAttributes(hidHandle);
                _deviceCapabilities = GetDeviceCapabilities(hidHandle);

                CloseDeviceIO(hidHandle);
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Error querying HID device '{0}'.", devicePath), exception);
            }
        }

        public IntPtr Handle { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsConnected { get { return HidDevices.IsConnected(_devicePath); } }
        public string Description { get { return _description; } }
        public HidDeviceCapabilities Capabilities { get { return _deviceCapabilities; } }
        public HidDeviceAttributes Attributes { get { return _deviceAttributes; } }
        public string DevicePath { get { return _devicePath; } }

        public bool MonitorDeviceEvents
        {
            get { return _monitorDeviceEvents; }
            set
            {
                if (value & _monitorDeviceEvents == false) _deviceEventMonitor.Init();
                _monitorDeviceEvents = value;
            }
        }

        public override string ToString()
        {
            return string.Format("VendorID={0}, ProductID={1}, Version={2}, DevicePath={3}",
                                _deviceAttributes.VendorHexId,
                                _deviceAttributes.ProductHexId,
                                _deviceAttributes.Version,
                                _devicePath);
        }

        public void OpenDevice()
        {
            OpenDevice(DeviceMode.NonOverlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
        }

        public void OpenDevice(DeviceMode readMode, DeviceMode writeMode, ShareMode shareMode)
        {
            if (IsOpen) return;

            _deviceReadMode = readMode;
            _deviceWriteMode = writeMode;
            _deviceShareMode = shareMode;

            try
            {
                Handle = OpenDeviceIO(_devicePath, readMode, NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE, shareMode);
            }
            catch (Exception exception)
            {
                IsOpen = false;
                throw new Exception("Error opening HID device.", exception);
            }

            IsOpen = Handle.ToInt32() != NativeMethods.INVALID_HANDLE_VALUE;
        }


        public void CloseDevice()
        {
            if (!IsOpen) return;
            CloseDeviceIO(Handle);
            IsOpen = false;
        }

        public HidDeviceData Read()
        {
            return Read(0);
        }

        public HidDeviceData Read(int timeout)
        {
            if (IsConnected)
            {
                if (IsOpen == false) OpenDevice(_deviceReadMode, _deviceWriteMode, _deviceShareMode);
                try
                {
                    return ReadData(timeout);
                }
                catch
                {
                    return new HidDeviceData(HidDeviceData.ReadStatus.ReadError);
                }

            }
            return new HidDeviceData(HidDeviceData.ReadStatus.NotConnected);
        }

        public void Read(ReadCallback callback)
        {
            Read(callback, 0);
        }

        public void Read(ReadCallback callback, int timeout)
        {
            var readDelegate = new ReadDelegate(Read);
            var asyncState = new HidAsyncState(readDelegate, callback);
            readDelegate.BeginInvoke(timeout, EndRead, asyncState);
        }

        public async Task<HidDeviceData> ReadAsync(int timeout = 0)
        {
            var readDelegate = new ReadDelegate(Read);
            return await Task<HidDeviceData>.Factory.FromAsync(readDelegate.BeginInvoke, readDelegate.EndInvoke, timeout, null);
        }

        public HidReport ReadReport()
        {
            return ReadReport(0);
        }

        public HidReport ReadReport(int timeout)
        {
            return new HidReport(Capabilities.InputReportByteLength, Read(timeout));
        }

        public void ReadReport(ReadReportCallback callback)
        {
            ReadReport(callback, 0);
        }

        public void ReadReport(ReadReportCallback callback, int timeout)
        {
            var readReportDelegate = new ReadReportDelegate(ReadReport);
            var asyncState = new HidAsyncState(readReportDelegate, callback);
            readReportDelegate.BeginInvoke(timeout, EndReadReport, asyncState);
        }

        public async Task<HidReport> ReadReportAsync(int timeout = 0)
        {
            var readReportDelegate = new ReadReportDelegate(ReadReport);
            return await Task<HidReport>.Factory.FromAsync(readReportDelegate.BeginInvoke, readReportDelegate.EndInvoke, timeout, null);
        }

        /// <summary>
        /// Reads an input report from the Control channel.  This method provides access to report data for devices that 
        /// do not use the interrupt channel to communicate for specific usages.
        /// </summary>
        /// <param name="reportId">The report ID to read from the device</param>
        /// <returns>The HID report that is read.  The report will contain the success status of the read request</returns>
        /// 
        public HidReport ReadReportSync(byte reportId)
        {
            byte[] cmdBuffer = new byte[Capabilities.InputReportByteLength];
            cmdBuffer[0] = reportId;
            bool bSuccess = NativeMethods.HidD_GetInputReport(Handle, cmdBuffer, cmdBuffer.Length);
            HidDeviceData deviceData = new HidDeviceData(cmdBuffer, bSuccess ? HidDeviceData.ReadStatus.Success : HidDeviceData.ReadStatus.NoDataRead);
            return new HidReport(Capabilities.InputReportByteLength, deviceData);
        }

        public bool ReadFeatureData(out byte[] data, byte reportId = 0)
        {
            if (_deviceCapabilities.FeatureReportByteLength <= 0)
            {
                data = new byte[0];
                return false;
            }

            data = new byte[_deviceCapabilities.FeatureReportByteLength];

            var buffer = CreateFeatureOutputBuffer();
            buffer[0] = reportId;

            IntPtr hidHandle = IntPtr.Zero;
            bool success = false;
            try
            {
                if (IsOpen)
                    hidHandle = Handle;
                else
                    hidHandle = OpenDeviceIO(_devicePath, NativeMethods.ACCESS_NONE);

                success = NativeMethods.HidD_GetFeature(hidHandle, buffer, buffer.Length);

                if (success)
                {
                    Array.Copy(buffer, 0, data, 0, Math.Min(data.Length, _deviceCapabilities.FeatureReportByteLength));
                }
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Error accessing HID device '{0}'.", _devicePath), exception);
            }
            finally
            {
                if (hidHandle != IntPtr.Zero && hidHandle != Handle)
                    CloseDeviceIO(hidHandle);
            }

            return success;
        }

        public string ProductName
        {
            get {
                byte[] data;
                if (ReadProduct(out data))
                {
                    var str = Encoding.ASCII.GetString(data).Replace("\0", string.Empty).TrimEnd(' ');
                    return str.Substring(0,1).ToUpper() + str.Substring(1);;
                }

                return "";
            }
        }

        public bool ReadProduct(out byte[] data)
        {
            data = new byte[254];
            IntPtr hidHandle = IntPtr.Zero;
            bool success = false;
            try
            {
                if (IsOpen)
                    hidHandle = Handle;
                else
                    hidHandle = OpenDeviceIO(_devicePath, NativeMethods.ACCESS_NONE);

                success = NativeMethods.HidD_GetProductString(hidHandle, ref data[0], data.Length);
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Error accessing HID device '{0}'.", _devicePath), exception);
            }
            finally
            {
                if (hidHandle != IntPtr.Zero && hidHandle != Handle)
                    CloseDeviceIO(hidHandle);
            }

            return success;
        }

        public bool ReadManufacturer(out byte[] data)
        {
            data = new byte[254];
            IntPtr hidHandle = IntPtr.Zero;
            bool success = false;
            try
            {
                if (IsOpen)
                    hidHandle = Handle;
                else
                    hidHandle = OpenDeviceIO(_devicePath, NativeMethods.ACCESS_NONE);

                success = NativeMethods.HidD_GetManufacturerString(hidHandle, ref data[0], data.Length);
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Error accessing HID device '{0}'.", _devicePath), exception);
            }
            finally
            {
                if (hidHandle != IntPtr.Zero && hidHandle != Handle)
                    CloseDeviceIO(hidHandle);
            }

            return success;
        }

        public bool ReadSerialNumber(out byte[] data)
        {
            data = new byte[254];
            IntPtr hidHandle = IntPtr.Zero;
            bool success = false;
            try
            {
                if (IsOpen)
                    hidHandle = Handle;
                else
                    hidHandle = OpenDeviceIO(_devicePath, NativeMethods.ACCESS_NONE);

                success = NativeMethods.HidD_GetSerialNumberString(hidHandle, ref data[0], data.Length);
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Error accessing HID device '{0}'.", _devicePath), exception);
            }
            finally
            {
                if (hidHandle != IntPtr.Zero && hidHandle != Handle)
                    CloseDeviceIO(hidHandle);
            }

            return success;
        }

        public bool Write(byte[] data)
        {
            return Write(data, 0);
        }

        public bool Write(byte[] data, int timeout)
        {
            if (IsConnected)
            {
                if (IsOpen == false) OpenDevice(_deviceReadMode, _deviceWriteMode, _deviceShareMode);
                try
                {
                    return WriteData(data, timeout);
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public void Write(byte[] data, WriteCallback callback)
        {
            Write(data, callback, 0);
        }

        public void Write(byte[] data, WriteCallback callback, int timeout)
        {
            var writeDelegate = new WriteDelegate(Write);
            var asyncState = new HidAsyncState(writeDelegate, callback);
            writeDelegate.BeginInvoke(data, timeout, EndWrite, asyncState);
        }

        public async Task<bool> WriteAsync(byte[] data, int timeout = 0)
        {
            var writeDelegate = new WriteDelegate(Write);
            return await Task<bool>.Factory.FromAsync(writeDelegate.BeginInvoke, writeDelegate.EndInvoke, data, timeout, null);
        }

        public bool WriteReport(HidReport report)
        {
            return WriteReport(report, 0);
        }

        public bool WriteReport(HidReport report, int timeout)
        {
            return Write(report.GetBytes(), timeout);
        }

        public void WriteReport(HidReport report, WriteCallback callback)
        {
            WriteReport(report, callback, 0);
        }

        public void WriteReport(HidReport report, WriteCallback callback, int timeout)
        {
            var writeReportDelegate = new WriteReportDelegate(WriteReport);
            var asyncState = new HidAsyncState(writeReportDelegate, callback);
            writeReportDelegate.BeginInvoke(report, timeout, EndWriteReport, asyncState);
        }

        /// <summary>
        /// Handle data transfers on the control channel.  This method places data on the control channel for devices
        /// that do not support the interupt transfers
        /// </summary>
        /// <param name="report">The outbound HID report</param>
        /// <returns>The result of the tranfer request: true if successful otherwise false</returns>
        /// 
        public bool WriteReportSync(HidReport report)
        {

            if (null != report)
            {
                byte[] buffer = report.GetBytes();
                return (NativeMethods.HidD_SetOutputReport(Handle, buffer, buffer.Length));
            }
            else
                throw new ArgumentException("The output report is null, it must be allocated before you call this method", "report");
        }

        public async Task<bool> WriteReportAsync(HidReport report, int timeout = 0)
        {
            var writeReportDelegate = new WriteReportDelegate(WriteReport);
            return await Task<bool>.Factory.FromAsync(writeReportDelegate.BeginInvoke, writeReportDelegate.EndInvoke, report, timeout, null);
        }

        public HidReport CreateReport()
        {
            return new HidReport(Capabilities.OutputReportByteLength);
        }

        public bool WriteFeatureData(byte[] data)
        {
            if (_deviceCapabilities.FeatureReportByteLength <= 0) return false;

            var buffer = CreateFeatureOutputBuffer();

            Array.Copy(data, 0, buffer, 0, Math.Min(data.Length, _deviceCapabilities.FeatureReportByteLength));


            IntPtr hidHandle = IntPtr.Zero;
            bool success = false;
            try
            {
                if (IsOpen)
                    hidHandle = Handle;
                else
                    hidHandle = OpenDeviceIO(_devicePath, NativeMethods.ACCESS_NONE);

                //var overlapped = new NativeOverlapped();
                success = NativeMethods.HidD_SetFeature(hidHandle, buffer, buffer.Length);
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Error accessing HID device '{0}'.", _devicePath), exception);
            }
            finally
            {
                if (hidHandle != IntPtr.Zero && hidHandle != Handle)
                    CloseDeviceIO(hidHandle);
            }
            return success;
        }

        protected static void EndRead(IAsyncResult ar)
        {
            var hidAsyncState = (HidAsyncState)ar.AsyncState;
            var callerDelegate = (ReadDelegate)hidAsyncState.CallerDelegate;
            var callbackDelegate = (ReadCallback)hidAsyncState.CallbackDelegate;
            var data = callerDelegate.EndInvoke(ar);

            if ((callbackDelegate != null)) callbackDelegate.Invoke(data);
        }

        protected static void EndReadReport(IAsyncResult ar)
        {
            var hidAsyncState = (HidAsyncState)ar.AsyncState;
            var callerDelegate = (ReadReportDelegate)hidAsyncState.CallerDelegate;
            var callbackDelegate = (ReadReportCallback)hidAsyncState.CallbackDelegate;
            var report = callerDelegate.EndInvoke(ar);

            if ((callbackDelegate != null)) callbackDelegate.Invoke(report);
        }

        private static void EndWrite(IAsyncResult ar)
        {
            var hidAsyncState = (HidAsyncState)ar.AsyncState;
            var callerDelegate = (WriteDelegate)hidAsyncState.CallerDelegate;
            var callbackDelegate = (WriteCallback)hidAsyncState.CallbackDelegate;
            var result = callerDelegate.EndInvoke(ar);

            if ((callbackDelegate != null)) callbackDelegate.Invoke(result);
        }

        private static void EndWriteReport(IAsyncResult ar)
        {
            var hidAsyncState = (HidAsyncState)ar.AsyncState;
            var callerDelegate = (WriteReportDelegate)hidAsyncState.CallerDelegate;
            var callbackDelegate = (WriteCallback)hidAsyncState.CallbackDelegate;
            var result = callerDelegate.EndInvoke(ar);

            if ((callbackDelegate != null)) callbackDelegate.Invoke(result);
        }

        private byte[] CreateInputBuffer()
        {
            return CreateBuffer(Capabilities.InputReportByteLength - 1);
        }

        private byte[] CreateOutputBuffer()
        {
            return CreateBuffer(Capabilities.OutputReportByteLength - 1);
        }

        private byte[] CreateFeatureOutputBuffer()
        {
            return CreateBuffer(Capabilities.FeatureReportByteLength - 1);
        }

        private static byte[] CreateBuffer(int length)
        {
            byte[] buffer = null;
            Array.Resize(ref buffer, length + 1);
            return buffer;
        }

        private static HidDeviceAttributes GetDeviceAttributes(IntPtr hidHandle)
        {
            var deviceAttributes = default(NativeMethods.HIDD_ATTRIBUTES);
            deviceAttributes.Size = Marshal.SizeOf(deviceAttributes);
            NativeMethods.HidD_GetAttributes(hidHandle, ref deviceAttributes);
            return new HidDeviceAttributes(deviceAttributes);
        }

        private static HidDeviceCapabilities GetDeviceCapabilities(IntPtr hidHandle)
        {
            var capabilities = default(NativeMethods.HIDP_CAPS);
            var preparsedDataPointer = default(IntPtr);

            if (NativeMethods.HidD_GetPreparsedData(hidHandle, ref preparsedDataPointer))
            {
                NativeMethods.HidP_GetCaps(preparsedDataPointer, ref capabilities);
                NativeMethods.HidD_FreePreparsedData(preparsedDataPointer);
            }
            return new HidDeviceCapabilities(capabilities);
        }

        private bool WriteData(byte[] data, int timeout)
        {
            if (_deviceCapabilities.OutputReportByteLength <= 0) return false;

            var buffer = CreateOutputBuffer();
            uint bytesWritten = 0;

            Array.Copy(data, 0, buffer, 0, Math.Min(data.Length, _deviceCapabilities.OutputReportByteLength));

            if (_deviceWriteMode == DeviceMode.Overlapped)
            {
                var security = new NativeMethods.SECURITY_ATTRIBUTES();
                var overlapped = new NativeOverlapped();

                var overlapTimeout = timeout <= 0 ? NativeMethods.WAIT_INFINITE : timeout;

                security.lpSecurityDescriptor = IntPtr.Zero;
                security.bInheritHandle = true;
                security.nLength = Marshal.SizeOf(security);

                overlapped.OffsetLow = 0;
                overlapped.OffsetHigh = 0;
                overlapped.EventHandle = NativeMethods.CreateEvent(ref security, Convert.ToInt32(false), Convert.ToInt32(true), "");

                try
                {
                    NativeMethods.WriteFile(Handle, buffer, (uint)buffer.Length, out bytesWritten, ref overlapped);
                }
                catch { return false; }

                var result = NativeMethods.WaitForSingleObject(overlapped.EventHandle, overlapTimeout);

                switch (result)
                {
                    case NativeMethods.WAIT_OBJECT_0:
                        return true;
                    case NativeMethods.WAIT_TIMEOUT:
                        return false;
                    case NativeMethods.WAIT_FAILED:
                        return false;
                    default:
                        return false;
                }
            }
            else
            {
                try
                {
                    var overlapped = new NativeOverlapped();
                    return NativeMethods.WriteFile(Handle, buffer, (uint)buffer.Length, out bytesWritten, ref overlapped);
                }
                catch { return false; }
            }
        }

        protected HidDeviceData ReadData(int timeout)
        {
            var buffer = new byte[] { };
            var status = HidDeviceData.ReadStatus.NoDataRead;
            IntPtr nonManagedBuffer;

            if (_deviceCapabilities.InputReportByteLength > 0)
            {
                uint bytesRead = 0;

                buffer = CreateInputBuffer();
                nonManagedBuffer = Marshal.AllocHGlobal(buffer.Length);

                if (_deviceReadMode == DeviceMode.Overlapped)
                {
                    var security = new NativeMethods.SECURITY_ATTRIBUTES();
                    var overlapped = new NativeOverlapped();
                    var overlapTimeout = timeout <= 0 ? NativeMethods.WAIT_INFINITE : timeout;

                    security.lpSecurityDescriptor = IntPtr.Zero;
                    security.bInheritHandle = true;
                    security.nLength = Marshal.SizeOf(security);

                    overlapped.OffsetLow = 0;
                    overlapped.OffsetHigh = 0;
                    overlapped.EventHandle = NativeMethods.CreateEvent(ref security, Convert.ToInt32(false), Convert.ToInt32(true), string.Empty);

                    try
                    {
                        var success = NativeMethods.ReadFile(Handle, nonManagedBuffer, (uint)buffer.Length, out bytesRead, ref overlapped);

                        if (!success) {
                            var result = NativeMethods.WaitForSingleObject(overlapped.EventHandle, overlapTimeout);

                            switch (result) 
                            {
                                case NativeMethods.WAIT_OBJECT_0:
                                    status = HidDeviceData.ReadStatus.Success;
                                    NativeMethods.GetOverlappedResult(Handle, ref overlapped, out bytesRead, false);
                                    break;
                                case NativeMethods.WAIT_TIMEOUT:
                                    status = HidDeviceData.ReadStatus.WaitTimedOut;
                                    buffer = new byte[] { };
                                    break;
                            case NativeMethods.WAIT_FAILED:
                                    status = HidDeviceData.ReadStatus.WaitFail;
                                    buffer = new byte[] { };
                                    break;
                                default:
                                    status = HidDeviceData.ReadStatus.NoDataRead;
                                    buffer = new byte[] { };
                                    break;
                            }
                        }
                        Marshal.Copy(nonManagedBuffer, buffer, 0, (int)bytesRead);
                    }
                    catch { status = HidDeviceData.ReadStatus.ReadError; }
                    finally {
                        CloseDeviceIO(overlapped.EventHandle);
                        Marshal.FreeHGlobal(nonManagedBuffer);
                    }
                }
                else
                {
                    try
                    {
                        var overlapped = new NativeOverlapped();

                        NativeMethods.ReadFile(Handle, nonManagedBuffer, (uint)buffer.Length, out bytesRead, ref overlapped);
                        status = HidDeviceData.ReadStatus.Success;
                        Marshal.Copy(nonManagedBuffer, buffer, 0, (int)bytesRead);
                    }
                    catch { status = HidDeviceData.ReadStatus.ReadError; }
                    finally { Marshal.FreeHGlobal(nonManagedBuffer); }
                }
            }
            return new HidDeviceData(buffer, status);
        }

        private static IntPtr OpenDeviceIO(string devicePath, uint deviceAccess)
        {
            return OpenDeviceIO(devicePath, DeviceMode.NonOverlapped, deviceAccess, ShareMode.ShareRead | ShareMode.ShareWrite);
        }

        private static IntPtr OpenDeviceIO(string devicePath, DeviceMode deviceMode, uint deviceAccess, ShareMode shareMode)
        {
            var security = new NativeMethods.SECURITY_ATTRIBUTES();
            var flags = 0;

            if (deviceMode == DeviceMode.Overlapped) flags = NativeMethods.FILE_FLAG_OVERLAPPED;

            security.lpSecurityDescriptor = IntPtr.Zero;
            security.bInheritHandle = true;
            security.nLength = Marshal.SizeOf(security);

            return NativeMethods.CreateFile(devicePath, deviceAccess, (int)shareMode, ref security, NativeMethods.OPEN_EXISTING, flags, 0);
        }

        private static void CloseDeviceIO(IntPtr handle)
        {
            if (Environment.OSVersion.Version.Major > 5)
            {
                NativeMethods.CancelIoEx(handle, IntPtr.Zero);
            }
            NativeMethods.CloseHandle(handle);
        }

        private void DeviceEventMonitorInserted()
        {
            if (!IsOpen) OpenDevice(_deviceReadMode, _deviceWriteMode, _deviceShareMode);
            if (Inserted != null) Inserted();
        }

        private void DeviceEventMonitorRemoved()
        {
            if (IsOpen) CloseDevice();
            if (Removed != null) Removed();
        }

        public void Dispose()
        {
            if (MonitorDeviceEvents) MonitorDeviceEvents = false;
            if (IsOpen) CloseDevice();
        }
    }
}
