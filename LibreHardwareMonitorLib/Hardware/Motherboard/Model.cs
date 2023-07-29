// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

// ReSharper disable InconsistentNaming

using System.Diagnostics.CodeAnalysis;

namespace LibreHardwareMonitor.Hardware.Motherboard;

[SuppressMessage("ReSharper", "IdentifierTypo")]
[SuppressMessage("ReSharper", "CommentTypo")]
public enum Model
{
    // ASRock
    _880GMH_USB3,
    A320M_HDV,
    AB350_Pro4,
    AB350M,
    AB350M_HDV,
    AB350M_Pro4,
    AOD790GX_128M,
    B450_Pro4,
    B450_Steel_Legend,
    B450M_Pro4,
    B450M_Steel_Legend,
    B85M_DGS,
    Fatal1ty_AB350_Gaming_K4,
    P55_Deluxe,
    X399_Phantom_Gaming_6,
    Z77Pro4M,
    X570_Pro4,
    X570_Taichi,
    X570_Phantom_Gaming_ITX,
    Z790_Taichi,

    // ASUS
    CROSSHAIR_III_FORMULA,
    ROG_CROSSHAIR_VIII_HERO,
    ROG_CROSSHAIR_VIII_HERO_WIFI,
    ROG_CROSSHAIR_VIII_DARK_HERO,
    ROG_CROSSHAIR_VIII_FORMULA,
    ROG_CROSSHAIR_VIII_IMPACT,
    ROG_STRIX_X470_I,
    ROG_CROSSHAIR_X670E_EXTREME,
    ROG_CROSSHAIR_X670E_GENE,
    ROG_STRIX_X570_E_GAMING,
    ROG_STRIX_X570_F_GAMING,
    ROG_STRIX_X570_I_GAMING,
    ROG_STRIX_B550_E_GAMING,
    ROG_STRIX_B550_F_GAMING_WIFI,
    ROG_STRIX_B550_I_GAMING,
    ROG_STRIX_Z390_E_GAMING,
    ROG_STRIX_Z390_F_GAMING,
    ROG_STRIX_Z390_I_GAMING,
    ROG_STRIX_Z690_A_GAMING_WIFI_D4,
    ROG_MAXIMUS_X_HERO_WIFI_AC,
    ROG_MAXIMUS_Z690_FORMULA,
    ROG_MAXIMUS_Z690_HERO,
    ROG_MAXIMUS_Z690_EXTREME_GLACIAL,
    ROG_STRIX_Z790_I_GAMING_WIFI,
    M2N_SLI_Deluxe,
    M4A79XTD_EVO,
    P5W_DH_Deluxe,
    P6T,
    P6X58D_E,
    P8P67,
    P8P67_EVO,
    P8P67_M_PRO,
    P8P67_PRO,
    P8Z77_V,
    P9X79,
    PRIME_X370_PRO,
    PRIME_X470_PRO,
    PRIME_X570_PRO,
    PROART_X570_CREATOR_WIFI,
    PRO_WS_X570_ACE,
    RAMPAGE_EXTREME,
    RAMPAGE_II_GENE,
    ROG_MAXIMUS_X_APEX,
    ROG_ZENITH_EXTREME,
    ROG_ZENITH_II_EXTREME,
    TUF_X470_PLUS_GAMING,
    Z170_A,
    TUF_GAMING_B550M_PLUS_WIFI,
    ROG_MAXIMUS_Z790_HERO,
    PRIME_Z690_A,

    //BIOSTAR
    B660GTN,
    X670E_Valkyrie,

    // DFI
    LP_BI_P45_T2RS_Elite,
    LP_DK_P55_T3EH9,

    // ECS
    A890GXM_A,

    // MSI
    B350_Gaming_Plus,
    B360M_PRO_VDH,
    B450A_PRO,
    Z270_PC_MATE,
    Z77_MS7751,

    // EVGA
    X58_SLI_Classified,
    X58_3X_SLI,

    // Gigabyte
    _965P_S3,
    _970A_UD3,
    AB350_Gaming_3,
    AX370_Gaming_5,
    AX370_Gaming_K7,
    B360_AORUS_GAMING_3_WIFI_CF,
    B550_AORUS_PRO,
    B560M_AORUS_ELITE,
    B560M_AORUS_PRO,
    B560M_AORUS_PRO_AX,
    B660M_DS3H_AX_DDR4,
    EP45_DS3R,
    EP45_UD3R,
    EX58_EXTREME,
    EX58_UD3R,
    G41M_COMBO,
    G41MT_S2,
    G41MT_S2P,
    H55_USB3,
    H55N_USB3,
    H61M_DS2_REV_1_2,
    H61M_USB3_B3_REV_2_0,
    H67A_UD3H_B3,
    H67A_USB3_B3,
    H81M_HD3,
    B75M_D3H,
    MA770T_UD3,
    MA770T_UD3P,
    MA785GM_US2H,
    MA785GMT_UD2H,
    MA78LM_S2H,
    MA790X_UD3P,
    P35_DS3,
    P35_DS3L,
    P55_UD4,
    P55A_UD3,
    P55M_UD4,
    P67A_UD3_B3,
    P67A_UD3R_B3,
    P67A_UD4_B3,
    P8Z68_V_PRO,
    X38_DS5,
    X399_AORUS_Gaming_7,
    X58A_UD3R,
    X79_UD3,
    Z390_AORUS_ULTRA,
    Z390_AORUS_PRO,
    Z390_M_GAMING,
    Z390_UD,
    Z68A_D3H_B3,
    Z68AP_D3,
    Z68X_UD3H_B3,
    Z68X_UD7_B3,
    Z68XP_UD3R,
    Z690_AORUS_PRO,
    Z690_AORUS_ULTRA,
    Z690_GAMING_X_DDR4,
    Z170N_WIFI,
    X470_AORUS_GAMING_7_WIFI,
    X570_AORUS_MASTER,
    X570_GAMING_X,
    X570_AORUS_ULTRA,

    // Shuttle
    FH67,

    // Unknown
    Unknown
}
