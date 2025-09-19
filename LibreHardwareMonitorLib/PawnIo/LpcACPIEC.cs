namespace LibreHardwareMonitor.PawnIo;

public class LpcAcpiEc
{
    private readonly PawnIo _pawnIO = PawnIo. LoadModuleFromResource(typeof(AmdFamily0F).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.LpcACPIEC.bin");

    public byte ReadPort(byte port)
    {
        long[] inArray = new long[1];
        inArray[0] = port;
        long[] outArray = _pawnIO.Execute("ioctl_pio_read", inArray, 1);
        return (byte)outArray[0];
    }

    public void WritePort(byte port, byte value)
    {
        long[] inArray = new long[2];
        inArray[0] = port;
        inArray[1] = value;
        _pawnIO.Execute("ioctl_pio_write", inArray, 0);
    }

    public void Close() => _pawnIO.Close();
}
