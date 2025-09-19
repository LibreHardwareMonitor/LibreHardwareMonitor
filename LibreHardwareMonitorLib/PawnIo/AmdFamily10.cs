using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.PawnIo;

public class AmdFamily10
{
    private readonly PawnIo _pawnIo = PawnIo.LoadModuleFromResource(typeof(AmdFamily0F).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.AMDFamily10.bin");

    public void MeasureTscMultiplier(out long ctrPerTick, out long cofVid)
    {
        long[] result = _pawnIo.Execute("ioctl_measure_tsc_multiplier", [], 2);
        ctrPerTick = result[0];
        cofVid = result[1];
    }

    public bool HaveCstateResidencyInfo()
    {
        try
        {
            ReadCstateResidency();
            return true;
        }
        catch
        {
            // ignored
        }

        return false;
    }

    public byte[] ReadCstateResidency()
    {
        long[] result = _pawnIo.Execute("ioctl_read_cstate_residency", [], 2);
        return [(byte)result[0], (byte)result[1]];
    }

    public uint ReadMiscCtl(int cpu, uint offset)
    {
        long[] result = _pawnIo.Execute("ioctl_read_miscctl", [cpu, offset], 1);
        return (uint)result[0];
    }

    public uint ReadSmu(uint offset)
    {
        long[] result = _pawnIo.Execute("ioctl_read_smu", [offset], 1);
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

    public bool ReadMsr(uint index, out uint eax, out uint edx, GroupAffinity affinity)
    {
        GroupAffinity previousAffinity = ThreadAffinity.Set(affinity);
        bool result = ReadMsr(index, out eax, out edx);
        ThreadAffinity.Set(previousAffinity);
        return result;
    }

    public void Close() => _pawnIo.Close();
}
