// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;

namespace LibreHardwareMonitor.Hardware.Motherboard
{
    internal class Identification
    {
        public static Manufacturer GetManufacturer(string name)
        {
            switch (name)
            {
                case var _ when name.IndexOf("abit.com.tw", StringComparison.OrdinalIgnoreCase) > -1:
                    return Manufacturer.Acer;
                case var _ when name.StartsWith("Acer", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Acer;
                case var _ when name.StartsWith("AMD", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.AMD;
                case var _ when name.Equals("Alienware", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Alienware;
                case var _ when name.StartsWith("AOpen", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.AOpen;
                case var _ when name.StartsWith("Apple", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Apple;
                case var _ when name.Equals("ASRock", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.ASRock;
                case var _ when name.StartsWith("ASUSTeK", StringComparison.OrdinalIgnoreCase):
                case var _ when name.StartsWith("ASUS ", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.ASUS;
                case var _ when name.StartsWith("Biostar", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Biostar;
                case var _ when name.StartsWith("Clevo", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Clevo;
                case var _ when name.StartsWith("Dell", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Dell;
                case var _ when name.Equals("DFI", StringComparison.OrdinalIgnoreCase):
                case var _ when name.StartsWith("DFI Inc", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.DFI;
                case var _ when name.Equals("ECS", StringComparison.OrdinalIgnoreCase):
                case var _ when name.StartsWith("ELITEGROUP", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.ECS;
                case var _ when name.Equals("EPoX COMPUTER CO., LTD", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.EPoX;
                case var _ when name.StartsWith("EVGA", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.EVGA;
                case var _ when name.Equals("FIC", StringComparison.OrdinalIgnoreCase):
                case var _ when name.StartsWith("First International Computer", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.FIC;
                case var _ when name.Equals("Foxconn", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Foxconn;
                case var _ when name.StartsWith("Fujitsu", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Fujitsu;
                case var _ when name.StartsWith("Gigabyte", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Gigabyte;
                case var _ when name.StartsWith("Hewlett-Packard", StringComparison.OrdinalIgnoreCase):
                case var _ when name.Equals("HP", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.HP;
                case var _ when name.Equals("IBM", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.IBM;
                case var _ when name.Equals("Intel", StringComparison.OrdinalIgnoreCase):
                case var _ when name.StartsWith("Intel Corp", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Intel;
                case var _ when name.StartsWith("Jetway", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Jetway;
                case var _ when name.StartsWith("Lenovo", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Lenovo;
                case var _ when name.Equals("LattePanda", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.LattePanda;
                case var _ when name.StartsWith("Medion", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Medion;
                case var _ when name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Microsoft;
                case var _ when name.StartsWith("Micro-Star International", StringComparison.OrdinalIgnoreCase):
                case var _ when name.Equals("MSI", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.MSI;
                case var _ when name.StartsWith("NEC ", StringComparison.OrdinalIgnoreCase):
                case var _ when name.Equals("NEC", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.NEC;
                case var _ when name.StartsWith("Pegatron", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Pegatron;
                case var _ when name.StartsWith("Samsung", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Samsung;
                case var _ when name.StartsWith("Sapphire", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Sapphire;
                case var _ when name.StartsWith("Shuttle", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Shuttle;
                case var _ when name.StartsWith("Sony", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Sony;
                case var _ when name.StartsWith("Supermicro", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Supermicro;
                case var _ when name.StartsWith("Toshiba", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Toshiba;
                case var _ when name.Equals("XFX", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.XFX;
                case var _ when name.StartsWith("Zotac", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Zotac;
                case var _ when name.Equals("To be filled by O.E.M.", StringComparison.OrdinalIgnoreCase):
                    return Manufacturer.Unknown;
                default:
                    return Manufacturer.Unknown;
            }
        }

        public static Model GetModel(string name)
        {
            switch (name)
            {
                case var _ when name.Equals("880GMH/USB3", StringComparison.OrdinalIgnoreCase):
                    return Model._880GMH_USB3;
                case var _ when name.Equals("B85M-DGS", StringComparison.OrdinalIgnoreCase):
                    return Model.B85M_DGS;
                case var _ when name.Equals("ASRock AOD790GX/128M", StringComparison.OrdinalIgnoreCase):
                    return Model.AOD790GX_128M;
                case var _ when name.Equals("AB350 Pro4", StringComparison.OrdinalIgnoreCase):
                    return Model.AB350_Pro4;
                case var _ when name.Equals("AB350M Pro4", StringComparison.OrdinalIgnoreCase):
                    return Model.AB350M_Pro4;
                case var _ when name.Equals("AB350M", StringComparison.OrdinalIgnoreCase):
                    return Model.AB350M;
                case var _ when name.Equals("B450 Steel Legend", StringComparison.OrdinalIgnoreCase):
                    return Model.B450_Steel_Legend;
                case var _ when name.Equals("B450M Steel Legend", StringComparison.OrdinalIgnoreCase):
                    return Model.B450M_Steel_Legend;
                case var _ when name.Equals("B450 Pro4", StringComparison.OrdinalIgnoreCase):
                    return Model.B450_Pro4;
                case var _ when name.Equals("B450M Pro4", StringComparison.OrdinalIgnoreCase):
                    return Model.B450M_Pro4;
                case var _ when name.Equals("Fatal1ty AB350 Gaming K4", StringComparison.OrdinalIgnoreCase):
                    return Model.Fatal1ty_AB350_Gaming_K4;
                case var _ when name.Equals("AB350M-HDV", StringComparison.OrdinalIgnoreCase):
                    return Model.AB350M_HDV;
                case var _ when name.Equals("X399 Phantom Gaming 6", StringComparison.OrdinalIgnoreCase):
                    return Model.X399_Phantom_Gaming_6;
                case var _ when name.Equals("A320M-HDV", StringComparison.OrdinalIgnoreCase):
                    return Model.A320M_HDV;
                case var _ when name.Equals("P55 Deluxe", StringComparison.OrdinalIgnoreCase):
                    return Model.P55_Deluxe;
                case var _ when name.Equals("Crosshair III Formula", StringComparison.OrdinalIgnoreCase):
                    return Model.CROSSHAIR_III_FORMULA;
                case var _ when name.Equals("ROG CROSSHAIR VIII HERO", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_CROSSHAIR_VIII_HERO;
                case var _ when name.Equals("ROG CROSSHAIR VIII HERO (WI-FI)", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_CROSSHAIR_VIII_HERO_WIFI;
                case var _ when name.Equals("ROG CROSSHAIR VIII DARK HERO", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_CROSSHAIR_VIII_DARK_HERO;
                case var _ when name.Equals("ROG CROSSHAIR VIII FORMULA", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_CROSSHAIR_VIII_FORMULA;
                case var _ when name.Equals("ROG CROSSHAIR VIII IMPACT", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_CROSSHAIR_VIII_IMPACT;
                case var _ when name.Equals("M2N-SLI DELUXE", StringComparison.OrdinalIgnoreCase):
                    return Model.M2N_SLI_Deluxe;
                case var _ when name.Equals("M4A79XTD EVO", StringComparison.OrdinalIgnoreCase):
                    return Model.M4A79XTD_EVO;
                case var _ when name.Equals("P5W DH Deluxe", StringComparison.OrdinalIgnoreCase):
                    return Model.P5W_DH_Deluxe;
                case var _ when name.Equals("P6T", StringComparison.OrdinalIgnoreCase):
                    return Model.P6T;
                case var _ when name.Equals("P6X58D-E", StringComparison.OrdinalIgnoreCase):
                    return Model.P6X58D_E;
                case var _ when name.Equals("P8P67", StringComparison.OrdinalIgnoreCase):
                case var _ when name.Equals("P8P67 REV 3.1", StringComparison.OrdinalIgnoreCase):
                    return Model.P8P67;
                case var _ when name.Equals("P8P67 EVO", StringComparison.OrdinalIgnoreCase):
                    return Model.P8P67_EVO;
                case var _ when name.Equals("P8P67 PRO", StringComparison.OrdinalIgnoreCase):
                    return Model.P8P67_PRO;
                case var _ when name.Equals("P8P67-M PRO", StringComparison.OrdinalIgnoreCase):
                    return Model.P8P67_M_PRO;
                case var _ when name.Equals("P8Z77-V", StringComparison.OrdinalIgnoreCase):
                    return Model.P8Z77_V;
                case var _ when name.Equals("P9X79", StringComparison.OrdinalIgnoreCase):
                    return Model.P9X79;
                case var _ when name.Equals("Rampage Extreme", StringComparison.OrdinalIgnoreCase):
                    return Model.RAMPAGE_EXTREME;
                case var _ when name.Equals("Rampage II GENE", StringComparison.OrdinalIgnoreCase):
                    return Model.RAMPAGE_II_GENE;
                case var _ when name.Equals("LP BI P45-T2RS Elite", StringComparison.OrdinalIgnoreCase):
                    return Model.LP_BI_P45_T2RS_Elite;
                case var _ when name.Equals("ROG STRIX B550-F GAMING (WI-FI)", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_STRIX_B550_F_GAMING_WIFI;
                case var _ when name.Equals("ROG STRIX X470-I GAMING", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_STRIX_X470_I;
                case var _ when name.Equals("ROG STRIX B550-E GAMING", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_STRIX_B550_E_GAMING;
                case var _ when name.Equals("ROG STRIX B550-I GAMING", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_STRIX_B550_I_GAMING;
                case var _ when name.Equals("ROG STRIX X570-E GAMING", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_STRIX_X570_E_GAMING;
                case var _ when name.Equals("ROG STRIX X570-I GAMING", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_STRIX_X570_I_GAMING;
                case var _ when name.Equals("ROG STRIX X570-F GAMING", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_STRIX_X570_F_GAMING;
                case var _ when name.Equals("LP DK P55-T3eH9", StringComparison.OrdinalIgnoreCase):
                    return Model.LP_DK_P55_T3EH9;
                case var _ when name.Equals("A890GXM-A", StringComparison.OrdinalIgnoreCase):
                    return Model.A890GXM_A;
                case var _ when name.Equals("X58 SLI Classified", StringComparison.OrdinalIgnoreCase):
                    return Model.X58_SLI_Classified;
                case var _ when name.Equals("965P-S3", StringComparison.OrdinalIgnoreCase):
                    return Model._965P_S3;
                case var _ when name.Equals("EP45-DS3R", StringComparison.OrdinalIgnoreCase):
                    return Model.EP45_DS3R;
                case var _ when name.Equals("EP45-UD3R", StringComparison.OrdinalIgnoreCase):
                    return Model.EP45_UD3R;
                case var _ when name.Equals("EX58-EXTREME", StringComparison.OrdinalIgnoreCase):
                    return Model.EX58_EXTREME;
                case var _ when name.Equals("EX58-UD3R", StringComparison.OrdinalIgnoreCase):
                    return Model.EX58_UD3R;
                case var _ when name.Equals("G41M-Combo", StringComparison.OrdinalIgnoreCase):
                    return Model.G41M_COMBO;
                case var _ when name.Equals("G41MT-S2", StringComparison.OrdinalIgnoreCase):
                    return Model.G41MT_S2;
                case var _ when name.Equals("G41MT-S2P", StringComparison.OrdinalIgnoreCase):
                    return Model.G41MT_S2P;
                case var _ when name.Equals("GA-970A-UD3", StringComparison.OrdinalIgnoreCase):
                    return Model._970A_UD3;
                case var _ when name.Equals("GA-MA770T-UD3", StringComparison.OrdinalIgnoreCase):
                    return Model.MA770T_UD3;
                case var _ when name.Equals("GA-MA770T-UD3P", StringComparison.OrdinalIgnoreCase):
                    return Model.MA770T_UD3P;
                case var _ when name.Equals("GA-MA785GM-US2H", StringComparison.OrdinalIgnoreCase):
                    return Model.MA785GM_US2H;
                case var _ when name.Equals("GA-MA785GMT-UD2H", StringComparison.OrdinalIgnoreCase):
                    return Model.MA785GMT_UD2H;
                case var _ when name.Equals("GA-MA78LM-S2H", StringComparison.OrdinalIgnoreCase):
                    return Model.MA78LM_S2H;
                case var _ when name.Equals("GA-MA790X-UD3P", StringComparison.OrdinalIgnoreCase):
                    return Model.MA790X_UD3P;
                case var _ when name.Equals("H55-USB3", StringComparison.OrdinalIgnoreCase):
                    return Model.H55_USB3;
                case var _ when name.Equals("H55N-USB3", StringComparison.OrdinalIgnoreCase):
                    return Model.H55N_USB3;
                case var _ when name.Equals("H61M-DS2 REV 1.2", StringComparison.OrdinalIgnoreCase):
                    return Model.H61M_DS2_REV_1_2;
                case var _ when name.Equals("H61M-USB3-B3 REV 2.0", StringComparison.OrdinalIgnoreCase):
                    return Model.H61M_USB3_B3_REV_2_0;
                case var _ when name.Equals("H67A-UD3H-B3", StringComparison.OrdinalIgnoreCase):
                    return Model.H67A_UD3H_B3;
                case var _ when name.Equals("H67A-USB3-B3", StringComparison.OrdinalIgnoreCase):
                    return Model.H67A_USB3_B3;
                case var _ when name.Equals("H81M-HD3", StringComparison.OrdinalIgnoreCase):
                    return Model.H81M_HD3;
                case var _ when name.Equals("P35-DS3", StringComparison.OrdinalIgnoreCase):
                    return Model.P35_DS3;
                case var _ when name.Equals("P35-DS3L", StringComparison.OrdinalIgnoreCase):
                    return Model.P35_DS3L;
                case var _ when name.Equals("P55-UD4", StringComparison.OrdinalIgnoreCase):
                    return Model.P55_UD4;
                case var _ when name.Equals("P55A-UD3", StringComparison.OrdinalIgnoreCase):
                    return Model.P55A_UD3;
                case var _ when name.Equals("P55M-UD4", StringComparison.OrdinalIgnoreCase):
                    return Model.P55M_UD4;
                case var _ when name.Equals("P67A-UD3-B3", StringComparison.OrdinalIgnoreCase):
                    return Model.P67A_UD3_B3;
                case var _ when name.Equals("P67A-UD3R-B3", StringComparison.OrdinalIgnoreCase):
                    return Model.P67A_UD3R_B3;
                case var _ when name.Equals("P67A-UD4-B3", StringComparison.OrdinalIgnoreCase):
                    return Model.P67A_UD4_B3;
                case var _ when name.Equals("P8Z68-V PRO", StringComparison.OrdinalIgnoreCase):
                    return Model.P8Z68_V_PRO;
                case var _ when name.Equals("X38-DS5", StringComparison.OrdinalIgnoreCase):
                    return Model.X38_DS5;
                case var _ when name.Equals("X58A-UD3R", StringComparison.OrdinalIgnoreCase):
                    return Model.X58A_UD3R;
                case var _ when name.Equals("Z270 PC MATE", StringComparison.OrdinalIgnoreCase):
                case var _ when name.Equals("Z270 PC MATE (MS-7A72)", StringComparison.OrdinalIgnoreCase):
                    return Model.Z270_PC_MATE;
                case var _ when name.Equals("X79-UD3", StringComparison.OrdinalIgnoreCase):
                    return Model.X79_UD3;
                case var _ when name.Equals("Z68A-D3H-B3", StringComparison.OrdinalIgnoreCase):
                    return Model.Z68A_D3H_B3;
                case var _ when name.Equals("Z68AP-D3", StringComparison.OrdinalIgnoreCase):
                    return Model.Z68AP_D3;
                case var _ when name.Equals("Z68X-UD3H-B3", StringComparison.OrdinalIgnoreCase):
                    return Model.Z68X_UD3H_B3;
                case var _ when name.Equals("Z68X-UD7-B3", StringComparison.OrdinalIgnoreCase):
                    return Model.Z68X_UD7_B3;
                case var _ when name.Equals("Z68XP-UD3R", StringComparison.OrdinalIgnoreCase):
                    return Model.Z68XP_UD3R;
                case var _ when name.Equals("Z170N-WIFI-CF", StringComparison.OrdinalIgnoreCase):
                    return Model.Z170N_WIFI;
                case var _ when name.Equals("Z390 M GAMING-CF", StringComparison.OrdinalIgnoreCase):
                    return Model.Z390_M_GAMING;
                case var _ when name.Equals("Z390 AORUS ULTRA", StringComparison.OrdinalIgnoreCase):
                    return Model.Z390_AORUS_ULTRA;
                case var _ when name.Equals("Z390 AORUS PRO-CF", StringComparison.OrdinalIgnoreCase):
                    return Model.Z390_AORUS_PRO;
                case var _ when name.Equals("Z390 UD", StringComparison.OrdinalIgnoreCase):
                    return Model.Z390_UD;
                case var _ when name.Equals("Z690 AORUS PRO", StringComparison.OrdinalIgnoreCase):
                    return Model.Z690_AORUS_PRO;
                case var _ when name.Equals("Z690 GAMING X DDR4", StringComparison.OrdinalIgnoreCase):
                    return Model.Z690_GAMING_X_DDR4;
                case var _ when name.Equals("FH67", StringComparison.OrdinalIgnoreCase):
                    return Model.FH67;
                case var _ when name.Equals("AX370-Gaming K7", StringComparison.OrdinalIgnoreCase):
                    return Model.AX370_Gaming_K7;
                case var _ when name.Equals("PRIME X370-PRO", StringComparison.OrdinalIgnoreCase):
                    return Model.PRIME_X370_PRO;
                case var _ when name.Equals("PRIME X470-PRO", StringComparison.OrdinalIgnoreCase):
                    return Model.PRIME_X470_PRO;
                case var _ when name.Equals("PRIME X570-PRO", StringComparison.OrdinalIgnoreCase):
                    return Model.PRIME_X570_PRO;
                case var _ when name.Equals("ProArt X570-CREATOR WIFI", StringComparison.OrdinalIgnoreCase):
                    return Model.PROART_X570_CREATOR_WIFI;
                case var _ when name.Equals("Pro WS X570-ACE", StringComparison.OrdinalIgnoreCase):
                    return Model.PRO_WS_X570_ACE;
                case var _ when name.Equals("ROG MAXIMUS X APEX", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_MAXIMUS_X_APEX;
                case var _ when name.Equals("AB350-Gaming 3-CF", StringComparison.OrdinalIgnoreCase):
                    return Model.AB350_Gaming_3;
                case var _ when name.Equals("X399 AORUS Gaming 7", StringComparison.OrdinalIgnoreCase):
                    return Model.X399_AORUS_Gaming_7;
                case var _ when name.Equals("ROG ZENITH EXTREME", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_ZENITH_EXTREME;
                case var _ when name.Equals("Z170-A", StringComparison.OrdinalIgnoreCase):
                    return Model.Z170_A;
                case var _ when name.Equals("Z77 Pro4-M", StringComparison.OrdinalIgnoreCase):
                    return Model.Z77Pro4M;
                case var _ when name.Equals("X570 Pro4", StringComparison.OrdinalIgnoreCase):
                    return Model.X570_Pro4;
                case var _ when name.Equals("X570 Taichi", StringComparison.OrdinalIgnoreCase):
                    return Model.X570_Taichi;
                case var _ when name.Equals("X570 Phantom Gaming-ITX/TB3", StringComparison.OrdinalIgnoreCase):
                    return Model.X570_Phantom_Gaming_ITX;
                case var _ when name.Equals("AX370-Gaming 5", StringComparison.OrdinalIgnoreCase):
                    return Model.AX370_Gaming_5;
                case var _ when name.Equals("TUF X470-PLUS GAMING", StringComparison.OrdinalIgnoreCase):
                    return Model.TUF_X470_PLUS_GAMING;
                case var _ when name.Equals("B360M PRO-VDH (MS-7B24)", StringComparison.OrdinalIgnoreCase):
                    return Model.B360M_PRO_VDH;
                case var _ when name.Equals("B450-A PRO (MS-7B86)", StringComparison.OrdinalIgnoreCase):
                    return Model.B450A_PRO;
                case var _ when name.Equals("B350 GAMING PLUS (MS-7A34)", StringComparison.OrdinalIgnoreCase):
                    return Model.B350_Gaming_Plus;
                case var _ when name.Equals("X470 AORUS GAMING 7 WIFI-CF", StringComparison.OrdinalIgnoreCase):
                    return Model.X470_AORUS_GAMING_7_WIFI;
                case var _ when name.Equals("X570 AORUS MASTER", StringComparison.OrdinalIgnoreCase):
                    return Model.X570_AORUS_MASTER;
                case var _ when name.Equals("X570 AORUS ULTRA", StringComparison.OrdinalIgnoreCase):
                    return Model.X570_AORUS_ULTRA;
                case var _ when name.Equals("X570 GAMING X", StringComparison.OrdinalIgnoreCase):
                    return Model.X570_GAMING_X;
                case var _ when name.Equals("TUF GAMING B550M-PLUS (WI-FI)", StringComparison.OrdinalIgnoreCase):
                    return Model.TUF_GAMING_B550M_PLUS_WIFI;
                case var _ when name.Equals("B360 AORUS GAMING 3 WIFI-CF", StringComparison.OrdinalIgnoreCase):
                return Model.B360_AORUS_GAMING_3_WIFI_CF;
                case var _ when name.Equals("B560M AORUS ELITE", StringComparison.OrdinalIgnoreCase):
                    return Model.B560M_AORUS_ELITE;
                case var _ when name.Equals("B560M AORUS PRO", StringComparison.OrdinalIgnoreCase):
                    return Model.B560M_AORUS_PRO;
                case var _ when name.Equals("B560M AORUS PRO AX", StringComparison.OrdinalIgnoreCase):
                    return Model.B560M_AORUS_PRO_AX;
                case var _ when name.Equals("ROG STRIX Z690-A GAMING WIFI D4", StringComparison.OrdinalIgnoreCase):
                    return Model.ROG_STRIX_Z690_A_GAMING_WIFI_D4;
                case var _ when name.Equals("B660GTN", StringComparison.OrdinalIgnoreCase):
                    return Model.B660GTN;
                case var _ when name.Equals("Base Board Product Name", StringComparison.OrdinalIgnoreCase):
                case var _ when name.Equals("To be filled by O.E.M.", StringComparison.OrdinalIgnoreCase):
                    return Model.Unknown;
                default:
                    return Model.Unknown;
            }
        }
    }
}
