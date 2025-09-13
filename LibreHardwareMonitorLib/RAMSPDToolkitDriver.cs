using System;
using LibreHardwareMonitor.PawnIo;
using RAMSPDToolkit.I2CSMBus.Interop.PawnIO;
using RAMSPDToolkit.Windows.Driver.Interfaces;

namespace LibreHardwareMonitor
{
    internal class RAMSPDToolkitDriver : IPawnIODriver
    {
        const string I801ModuleFilename = "SmbusI801.bin";
        const string Piix4ModuleFilename = "SmbusPIIX4.bin";
        const string NCT6793ModuleFilename = "SmbusNCT6793.bin";

        private PawnIO _pawnIO;

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
            string moduleResourceFilename = null;

            switch (pawnIOSMBusIdentifier)
            {
                case PawnIOSMBusIdentifier.I801:
                    moduleResourceFilename = I801ModuleFilename;
                    break;
                case PawnIOSMBusIdentifier.Piix4:
                    moduleResourceFilename = Piix4ModuleFilename;
                    break;
                case PawnIOSMBusIdentifier.NCT6793:
                    moduleResourceFilename = NCT6793ModuleFilename;
                    break;
                default:
                    break;
            }

            if (moduleResourceFilename == null)
            {
                return false;
            }

            try
            {
                _pawnIO = PawnIO.LoadModuleFromResource(typeof(RAMSPDToolkitDriver).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIO.{moduleResourceFilename}");
            }
            catch (Exception)
            {
                return false;
            }

            return _pawnIO != null;
        }

        public void Unload()
        {
            //Empty
        }
    }
}
