//#define ISA_BRIDGE_EC_DEBUG

using System.Diagnostics;

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
        public bool FindSuperIoMMIO(out MMIOMapping firstMmio, out MMIOMapping secondMmio)
        {
            long[] outArray = new long[6];
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_find_superio_mmio", [], 0, outArray, 6, out uint returnSize);

            Log($"FindSuperIoMMIO statusCode: {ntStatusCode}");

            if (ntStatusCode != 0)
            {
                firstMmio = default;
                secondMmio = default;
                return false;
            }

            firstMmio = new MMIOMapping
            {
                Index = 0,
                BaseAddress = outArray[0],
                SuperIoSize = outArray[1],
                ChipId = outArray[2]
            };

            secondMmio = new MMIOMapping
            {
                Index = 1,
                BaseAddress = outArray[3],
                SuperIoSize = outArray[4],
                ChipId = outArray[5]
            };

            Log($"First MMIO - BaseAddress: 0x{firstMmio.BaseAddress:X}, SuperIoSize: 0x{firstMmio.SuperIoSize:X}, ChipId: 0x{firstMmio.ChipId:X}");
            Log($"Second MMIO - BaseAddress: 0x{secondMmio.BaseAddress:X}, SuperIoSize: 0x{secondMmio.SuperIoSize:X}, ChipId: 0x{secondMmio.ChipId:X}");

            return firstMmio.BaseAddress != 0 || secondMmio.BaseAddress != 0;
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

            Log($"ReadMmio statusCode: {ntStatusCode}, Read Value: 0x{outarray[0]:X} at SuperIoIndex {superIoIndex}, offset {offset}, size {size}");

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

            Log($"WriteMmio statusCode: {ntStatusCode}, Written Value: 0x{value:X} at SuperIoIndex {superIoIndex}, offset {offset}, size {size}");

            return ntStatusCode == 0;
        }

        // ioctl_map_superio_mmio
        public bool Map()
        {
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_map_superio_mmio", [], 0, [], 0, out uint returnSize);

            Log($"Map statusCode: {ntStatusCode}");

            return ntStatusCode == 0;
        }

        // ioctl_unmap_superio_mmio
        public bool Unmap()
        {
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_unmap_superio_mmio", [], 0, [], 0, out uint returnSize);

            Log($"Unmap statusCode: {ntStatusCode}");

            return ntStatusCode == 0;
        }

        // ioctl_access_superio_mmio
        public bool GetOriginalState(out MMIOState state)
        {
            state = MMIOState.Unknown;
            long[] outArray = new long[1];
            int ntStatusCode = _pawnIO.ExecuteHr("ioctl_iomem_mmio_get_org_state", [], 0, outArray, 1, out uint returnSize);

            Log($"GetOriginalState statusCode: {ntStatusCode}");


            if (ntStatusCode != 0)
                return false;

            Log($"Original MMIO State: {(MMIOState)outArray[0]}");

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

            Log($"TrySetState to {state} statusCode: {ntStatusCode}");

            if (ntStatusCode != 0)
                return false;

            return true;
        }

        public void Close() => _pawnIO.Close();

        /// <summary>
        /// Writes a debug message to both the output window and a log file when ISA_BRIDGE_EC_DEBUG is defined.
        /// </summary>
        /// <remarks>The log entry is timestamped and appended to the file
        /// 'PawnIo_IsaBridgeEc_DebugLog.txt' in the application's working directory. This method only produces output
        /// when compiled with the ISA_BRIDGE_EC_DEBUG symbol defined.</remarks>
        /// <param name="message">The message to log. This should provide relevant information for debugging purposes.</param>
        [Conditional("DEBUG_LOG"), Conditional("ISA_BRIDGE_EC_DEBUG")]
        private static void Log(string message)
        {
            Debug.WriteLine(message);
        }
    }
}
