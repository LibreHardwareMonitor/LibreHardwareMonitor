using System.Collections.Generic;
using System.Linq;

namespace HidLibrary
{
    public class HidFastReadEnumerator : IHidEnumerator
    {
        public bool IsConnected(string devicePath)
        {
            return HidDevices.IsConnected(devicePath);
        }

        public IHidDevice GetDevice(string devicePath)
        {
            return Enumerate(devicePath).FirstOrDefault() as IHidDevice;
        }

        public IEnumerable<IHidDevice> Enumerate()
        {
            return HidDevices.EnumerateDevices().
                Select(d => new HidFastReadDevice(d.Path, d.Description) as IHidDevice);
        }

        public IEnumerable<IHidDevice> Enumerate(string devicePath)
        {
            return HidDevices.EnumerateDevices().Where(x => x.Path == devicePath).
                Select(d => new HidFastReadDevice(d.Path, d.Description) as IHidDevice);
        }

        public IEnumerable<IHidDevice> Enumerate(int vendorId, params int[] productIds)
        {
            return HidDevices.EnumerateDevices().Select(d => new HidFastReadDevice(d.Path, d.Description)).
                Where(f => f.Attributes.VendorId == vendorId && productIds.Contains(f.Attributes.ProductId)).
                Select(d => d as IHidDevice);
        }

        public IEnumerable<IHidDevice> Enumerate(int vendorId)
        {
            return HidDevices.EnumerateDevices().Select(d => new HidFastReadDevice(d.Path, d.Description)).
                Where(f => f.Attributes.VendorId == vendorId).
                Select(d => d as IHidDevice);
        }
    }
}