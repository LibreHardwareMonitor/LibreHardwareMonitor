using System;
using System.Threading;

namespace HidLibrary
{
    internal class HidDeviceEventMonitor
    {
        public event InsertedEventHandler Inserted;
        public event RemovedEventHandler Removed;

        public delegate void InsertedEventHandler();
        public delegate void RemovedEventHandler();

        private readonly HidDevice _device;
        private bool _wasConnected;

        public HidDeviceEventMonitor(HidDevice device)
        {
            _device = device;
        }

        public void Init()
        {
            var eventMonitor = new Action(DeviceEventMonitor);
            eventMonitor.BeginInvoke(DisposeDeviceEventMonitor, eventMonitor);
        }

        private void DeviceEventMonitor()
        {
            var isConnected = _device.IsConnected;

            if (isConnected != _wasConnected)
            {
                if (isConnected && Inserted != null) Inserted();
                else if (!isConnected && Removed != null) Removed();
                _wasConnected = isConnected;
            }

            Thread.Sleep(500);

            if (_device.MonitorDeviceEvents) Init();
        }

        private static void DisposeDeviceEventMonitor(IAsyncResult ar)
        {
            ((Action)ar.AsyncState).EndInvoke(ar);
        }
    }
}
