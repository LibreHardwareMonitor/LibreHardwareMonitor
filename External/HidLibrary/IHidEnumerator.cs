using System;
using System.Collections.Generic;
using System.Linq;

namespace HidLibrary
{
    public interface IHidEnumerator
    {
        bool IsConnected(string devicePath);
        IHidDevice GetDevice(string devicePath);
        IEnumerable<IHidDevice> Enumerate();
        IEnumerable<IHidDevice> Enumerate(string devicePath);
        IEnumerable<IHidDevice> Enumerate(int vendorId, params int[] productIds);
        IEnumerable<IHidDevice> Enumerate(int vendorId);
    }

    // Instance class that wraps HidDevices
    // The purpose of this is to allow consumer classes to create
    // their own enumeration abstractions, either for testing or
    // for comparing different implementations
    public class HidEnumerator : IHidEnumerator
    {
        public bool IsConnected(string devicePath)
        {
            return HidDevices.IsConnected(devicePath);
        }

        public IHidDevice GetDevice(string devicePath)
        {
            return HidDevices.GetDevice(devicePath) as IHidDevice;
        }

        public IEnumerable<IHidDevice> Enumerate()
        {
            return HidDevices.Enumerate().
                Select(d => d as IHidDevice);
        }

        public IEnumerable<IHidDevice> Enumerate(string devicePath)
        {
            return HidDevices.Enumerate(devicePath).
                Select(d => d as IHidDevice);
        }

        public IEnumerable<IHidDevice> Enumerate(int vendorId, params int[] productIds)
        {
            return HidDevices.Enumerate(vendorId, productIds).
                Select(d => d as IHidDevice);
        }

        public IEnumerable<IHidDevice> Enumerate(int vendorId)
        {
            return HidDevices.Enumerate(vendorId).
                Select(d => d as IHidDevice);
        }
    }
}
