namespace LibreHardwareMonitor.PawnIo;

public class IntelMchbar
{
    private readonly long[] _inArray = new long[1];
    private readonly long[] _outArray = new long[1];
    private readonly PawnIo _pawnIO = PawnIo.LoadModuleFromResource(typeof(IntelMchbar).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.IntelMCHBAR.bin");

    public bool ReadDword(uint offset, out uint value)
    {
        value = 0;

        if (!_pawnIO.IsLoaded)
        {
            return false;
        }

        _inArray[0] = offset;

        int hr = _pawnIO.ExecuteHr("ioctl_read_dword", _inArray, 1, _outArray, 1, out uint returnSize);
        if (hr != 0 || returnSize != 1)
        {
            return false;
        }

        value = (uint)_outArray[0];

        return true;
    }

    public void Close() => _pawnIO.Close();
}
