using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.PawnIo;

public class AmdFamily0F
{
    private readonly PawnIo _pawnIO = PawnIo.LoadModuleFromResource(typeof(AmdFamily0F).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.AMDFamily0F.bin");

    public bool ReadMsr(uint index, out uint eax, out uint edx)
    {
        long[] inArray = new long[1];
        inArray[0] = index;
        eax = 0;
        edx = 0;
        try
        {
            long[] outArray = _pawnIO.Execute("ioctl_read_msr", inArray, 1);
            eax = (uint)outArray[0];
            edx = (uint)(outArray[0] >> 32);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public bool ReadMsr(uint index, out uint eax, out uint edx, GroupAffinity affinity)
    {
        GroupAffinity previousAffinity = ThreadAffinity.Set(affinity);
        bool result = ReadMsr(index, out eax, out edx);
        ThreadAffinity.Set(previousAffinity);
        return result;
    }

    public uint GetThermtrip(int cpuIndex, uint coreIndex)
    {
        long[] inArray = new long[2];
        inArray[0] = cpuIndex;
        inArray[1] = coreIndex;
        long[] outArray = _pawnIO.Execute("ioctl_get_thermtrip", inArray, 1);
        return (uint)outArray[0];
    }

    public void Close() => _pawnIO.Close();
}
