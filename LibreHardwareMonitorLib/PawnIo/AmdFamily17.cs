namespace LibreHardwareMonitor.PawnIo;

public class AmdFamily17
{
    private readonly PawnIo _pawnIo = PawnIo.LoadModuleFromResource(typeof(AmdFamily0F).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.AMDFamily17.bin");

    public uint ReadSmn(uint offset)
    {
        long[] result = _pawnIo.Execute("ioctl_read_smn", [offset], 1);
        return (uint)result[0];
    }

    public bool ReadMsr(uint index, out uint eax, out uint edx)
    {
        long[] inArray = new long[1];
        inArray[0] = index;
        eax = 0;
        edx = 0;
        try
        {
            long[] outArray = _pawnIo.Execute("ioctl_read_msr", inArray, 1);
            eax = (uint)outArray[0];
            edx = (uint)(outArray[0] >> 32);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public bool ReadMsr(uint index, out ulong eaxedx)
    {
        long[] inArray = new long[1];
        inArray[0] = index;
        eaxedx = 0;
        try
        {
            long[] outArray = _pawnIo.Execute("ioctl_read_msr", inArray, 1);
            eaxedx = (ulong)outArray[0];
        }
        catch
        {
            return false;
        }

        return true;
    }

    public void Close() => _pawnIo.Close();
}
