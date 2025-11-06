using System.Drawing;
using System.Linq;

namespace LibreHardwareMonitor.PawnIo
{
    public enum MMIOState
    {
        Unknown = -999,

        MMIO_Original = -1,
        MMIO_Disabled = 0,
        MMIO_Enabled2E = 1,
        MMIO_Enabled4E = 2,
        MMIO_EnabledBoth = 3
    };

    public struct MMIOMapping
    {
        public int Index;
        public long BaseAddress;
        public long SuperIoSize;
        public long ChipId;
    }

    public class IsaBridgeEc
    {
        private readonly PawnIo _pawnIO = PawnIo.LoadModuleFromResource(typeof(IsaBridgeEc).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.IsaBridgeEC.bin");

        // ioctl_find_superio_mmio
        public bool FindSuperIoMMIO(out MMIOMapping mmio)
        {
            long[] outArray = new long[6];
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_find_superio_mmio", [], 0, outArray, 6, out uint returnSize);

            if (ntStatusCode != 0)
            {
                mmio = new MMIOMapping();
                return false;
            }

            // 2 possible mmio mapping, return the first valid one
            for (int i = 0; i < 2; i++)
            {
                mmio = new MMIOMapping()
                {
                    Index = i,
                    BaseAddress = outArray[0 * i],
                    SuperIoSize = outArray[1 * i],
                    ChipId = outArray[2 * i]
                };

                if (mmio.BaseAddress != 0)
                {
                    return true;
                }
            }

            mmio = new MMIOMapping();
            return false;
        }

        //ioctl_access_superio_mmio
        public bool ReadMmio(long superIoIndex, long offset, long size, out byte value)
        {
            long[] inArray = new long[5] {
                superIoIndex, // superio index
                offset, // offset
                size, // size
                0, // is write?
                0 // value (ignored for read)
            };

            long[] outarray = new long[1];

            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_access_superio_mmio", inArray, 5, outarray, 1, out uint returnSize);

            value = (byte)outarray[0];

            return ntStatusCode == 0;
        }

        //ioctl_access_superio_mmio
        public bool WriteMmio(long superIoIndex, long offset, long size, byte value)
        {
            long[] inArray = new long[5] {
                superIoIndex, // superio index
                offset, // offset
                size, // size
                1, // is write?
                value // value
            };

            long[] outarray = new long[1];

            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_access_superio_mmio", inArray, 5, outarray, 1, out uint returnSize);
            return ntStatusCode == 0;
        }

        // ioctl_map_superio_mmio
        public bool Map()
        {
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_map_superio_mmio", [], 0, [], 0, out uint returnSize);
            return ntStatusCode == 0;
        }

        // ioctl_unmap_superio_mmio
        public bool Unmap()
        {
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_unmap_superio_mmio", [], 0, [], 0, out uint returnSize);
            return ntStatusCode == 0;
        }

        // ioctl_access_superio_mmio

        public bool GetOriginalState(out MMIOState state)
        {
            state = MMIOState.Unknown;
            long[] outArray = new long[1];
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_iomem_mmio_get_org_state", [], 0, outArray, 1, out uint returnSize);

            if (ntStatusCode != 0)
                return false;

            state = (MMIOState)outArray[0];
            return true;
        }

        public bool TryGetCurrentState(out MMIOState state)
        {
            state = MMIOState.Unknown;
            long[] outArray = new long[1];
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_iomem_mmio_get_cur_state", [], 0, outArray, 1, out uint returnSize);

            if (ntStatusCode != 0)
                return false;

            state = (MMIOState)outArray[0];
            return true;
        }

        public bool TrySetState(MMIOState state)
        {
            long[] inArray = new long[1];
            inArray[0] = (long)state;
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_iomem_mmio_set_state", inArray, 1, [], 0, out uint returnSize);
            if (ntStatusCode != 0)
                return false;

            return true;
        }

        public void Close() => _pawnIO.Close();


    }
}
