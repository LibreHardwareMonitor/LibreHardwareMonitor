using System;

namespace LibreHardwareMonitor.PawnIo;

public class LpcCrOSEc
{
    private readonly PawnIo _pawnIO = PawnIo. LoadModuleFromResource(typeof(AmdFamily0F).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.LpcCrOSEC.bin");

    public byte[] EcCommand(int version, int command, int outsize, int insize, byte[] data)
    {
        throw new NotImplementedException();
    }

    public byte[] ReadMemmap(byte offset, byte bytes)
    {
        long[] inArray = new long[2];
        inArray[0] = offset;
        inArray[1] = bytes;
        long[] outArray = _pawnIO.Execute("ioctl_ec_readmem", inArray, (int)Math.Ceiling((double)bytes / 8.0));
        byte[] retArray = new byte[bytes];
        Buffer.BlockCopy(outArray, 0, retArray, 0, bytes);
        return retArray;
    }

    public void Close() => _pawnIO.Close();
}
