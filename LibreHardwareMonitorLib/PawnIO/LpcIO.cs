using System;

namespace LibreHardwareMonitor.PawnIo;

internal class LpcIO
{
    readonly long[] _singleArgArray = new long[1];
    readonly long[] _doubleArgArray = new long[2];
    readonly PawnIo _pawnIO;

    public LpcIO()
    {
        _pawnIO = PawnIo.LoadModuleFromResource(typeof(LpcIO).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIO.LpcIO.bin");
    }

    public void SelectSlot(int slot)
    {
        _singleArgArray[0] = slot;
        _pawnIO.Execute("ioctl_select_slot", _singleArgArray, 0);
    }

    public void FindBars()
    {
        _pawnIO.Execute("ioctl_find_bars", [], 0);
    }

    public byte ReadPort(ushort port)
    {
        _singleArgArray[0] = port;
        return (byte)_pawnIO.Execute("ioctl_pio_inb", _singleArgArray, 1)[0];
    }

    public void WritePort(ushort port, byte value)
    {
        _doubleArgArray[0] = port;
        _doubleArgArray[1] = value;
        _pawnIO.Execute("ioctl_pio_outb", _doubleArgArray, 0);
    }

    public byte ReadByte(byte register)
    {
        _singleArgArray[0] = register;
        return (byte)_pawnIO.Execute("ioctl_superio_inb", _singleArgArray, 1)[0];
    }

    public ushort ReadWord(byte register)
    {
        _singleArgArray[0] = register;
        return (ushort)_pawnIO.Execute("ioctl_superio_inw", _singleArgArray, 1)[0];
    }

    public void WriteByte(byte register, byte value)
    {
        _doubleArgArray[0] = register;
        _doubleArgArray[1] = value;
        _pawnIO.Execute("ioctl_superio_outb", _doubleArgArray, 0);
    }

    public void Close() => _pawnIO.Close();
}
