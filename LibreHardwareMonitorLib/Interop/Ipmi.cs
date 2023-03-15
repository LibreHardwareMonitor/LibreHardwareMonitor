using System;
using System.Runtime.InteropServices;

namespace LibreHardwareMonitor.Interop
{
    internal static class Ipmi
    {
        // Ported from ipmiutil
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        internal unsafe struct Sdr
        {
            [MarshalAs(UnmanagedType.U2)]
            public ushort recid;
            [MarshalAs(UnmanagedType.U1)]
            public byte sdrver;
            [MarshalAs(UnmanagedType.U1)]
            public byte rectype;
            [MarshalAs(UnmanagedType.U1)]
            public byte reclen;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_ownid;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_ownlun;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_num;
            [MarshalAs(UnmanagedType.U1)]
            public byte entity_id;
            [MarshalAs(UnmanagedType.U1)]
            public byte entity_inst;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_init;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_capab;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_type;
            [MarshalAs(UnmanagedType.U1)]
            public byte ev_type;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 6)]
            public string data1;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_units;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_base;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_mod;
            [MarshalAs(UnmanagedType.U1)]
            public byte linear;
            [MarshalAs(UnmanagedType.U1)]
            public byte m;
            [MarshalAs(UnmanagedType.U1)]
            public byte m_t;
            [MarshalAs(UnmanagedType.U1)]
            public byte b;
            [MarshalAs(UnmanagedType.U1)]
            public byte b_a;
            [MarshalAs(UnmanagedType.U1)]
            public byte a_ax;
            [MarshalAs(UnmanagedType.U1)]
            public byte rx_bx;
            [MarshalAs(UnmanagedType.U1)]
            public byte flags;
            [MarshalAs(UnmanagedType.U1)]
            public byte nom_reading;
            [MarshalAs(UnmanagedType.U1)]
            public byte norm_max;
            [MarshalAs(UnmanagedType.U1)]
            public byte norm_min;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_max_reading;
            [MarshalAs(UnmanagedType.U1)]
            public byte sens_min_reading;
            [MarshalAs(UnmanagedType.U1)]
            public byte unr_threshold;
            [MarshalAs(UnmanagedType.U1)]
            public byte ucr_threshold;
            [MarshalAs(UnmanagedType.U1)]
            public byte unc_threshold;
            [MarshalAs(UnmanagedType.U1)]
            public byte lnr_threshold;
            [MarshalAs(UnmanagedType.U1)]
            public byte lcr_threshold;
            [MarshalAs(UnmanagedType.U1)]
            public byte lnc_threshold;
            [MarshalAs(UnmanagedType.U1)]
            public byte pos_hysteresis;
            [MarshalAs(UnmanagedType.U1)]
            public byte neg_hysteresis;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)]
            public string data3;
            [MarshalAs(UnmanagedType.U1)]
            public byte id_strlen;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string id_string;
        }
    }
}
