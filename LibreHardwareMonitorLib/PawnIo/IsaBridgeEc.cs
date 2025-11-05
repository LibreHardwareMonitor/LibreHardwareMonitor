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

    public class IsaBridgeEc
    {
        private readonly PawnIo _pawnIO = PawnIo.LoadModuleFromResource(typeof(IsaBridgeEc).Assembly, $"{nameof(LibreHardwareMonitor)}.Resources.PawnIo.IsaBridgeEC.bin");

        // ioctl_find_superio_mmio

        // ioctl_map_superio_mmio

        // ioctl_unmap_superio_mmio

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

        // ioctl_iomem_mmio_set_state
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
