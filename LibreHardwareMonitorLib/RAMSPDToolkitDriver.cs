using RAMSPDToolkit.I2CSMBus.Interop.PawnIO;
using RAMSPDToolkit.Windows.Driver.Interfaces;

namespace LibreHardwareMonitor;

internal class RAMSPDToolkitDriver : IPawnIODriver
{
    const string I801ModuleFilename = "SmbusI801.bin";
    const string Piix4ModuleFilename = "SmbusPIIX4.bin";
    const string Nct6793ModuleFilename = "SmbusNCT6793.bin";

    private PawnIo.PawnIo _pawnIO;

    public bool IsOpen => true;

    public int Execute(string name, long[] inBuffer, uint inSize, long[] outBuffer, uint outSize, out uint returnSize)
    {
        return _pawnIO.ExecuteHr(name, inBuffer, inSize, outBuffer, outSize, out returnSize);
    }

    public bool Load()
    {
        //Not required
        return true;
    }

    public bool LoadModule(PawnIOSMBusIdentifier pawnIOSMBusIdentifier)
    {
        string moduleResourceFilename = pawnIOSMBusIdentifier switch
        {
            PawnIOSMBusIdentifier.I801 => I801ModuleFilename,
            PawnIOSMBusIdentifier.Piix4 => Piix4ModuleFilename,
            PawnIOSMBusIdentifier.NCT6793 => Nct6793ModuleFilename,
            _ => null
        };

        if (moduleResourceFilename == null)
        {
            return false;
        }

        try
        {
            _pawnIO = PawnIo.PawnIo.LoadModuleFromResource(typeof(RAMSPDToolkitDriver).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIO.{moduleResourceFilename}");
        }
        catch
        {
            return false;
        }

        return _pawnIO != null;
    }

    public void Unload()
    {
        _pawnIO.Close();
    }
}
