namespace HidLibrary
{
    public class HidDeviceData
    {
        public enum ReadStatus
        {
            Success = 0,
            WaitTimedOut = 1,
            WaitFail = 2,
            NoDataRead = 3,
            ReadError = 4,
            NotConnected = 5
        }

        public HidDeviceData(ReadStatus status)
	    {
		    Data = new byte[] {};
		    Status = status;
	    }

        public HidDeviceData(byte[] data, ReadStatus status)
        {
            Data = data;
            Status = status;
        }

        public byte[] Data { get; private set; }
        public ReadStatus Status { get; private set; }
    }
}
