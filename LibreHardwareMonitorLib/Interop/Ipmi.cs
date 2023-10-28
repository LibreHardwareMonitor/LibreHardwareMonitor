using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop;

internal static class Ipmi
{
    // Ported from ipmiutil
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct Sdr
    {
        public ushort recid;

        public byte sdrver;

        public byte rectype;

        public byte reclen;

        public byte sens_ownid;

        public byte sens_ownlun;

        public byte sens_num;

        public byte entity_id;

        public byte entity_inst;

        public byte sens_init;

        public byte sens_capab;

        public byte sens_type;

        public byte ev_type;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 6)]
        public string data1;

        public byte sens_units;

        public byte sens_base;

        public byte sens_mod;

        public byte linear;

        public byte m;

        public byte m_t;

        public byte b;

        public byte b_a;

        public byte a_ax;

        public byte rx_bx;

        public byte flags;

        public byte nom_reading;

        public byte norm_max;

        public byte norm_min;

        public byte sens_max_reading;

        public byte sens_min_reading;

        public byte unr_threshold;

        public byte ucr_threshold;

        public byte unc_threshold;

        public byte lnr_threshold;

        public byte lcr_threshold;

        public byte lnc_threshold;

        public byte pos_hysteresis;

        public byte neg_hysteresis;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)]
        public string data3;

        public byte id_strlen;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string id_string;
    }
}
