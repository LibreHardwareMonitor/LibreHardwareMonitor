﻿/*

  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.

  Copyright (C) 2012 Michael Möller <mmoeller@openhardwaremonitor.org>

*/

namespace OpenHardwareMonitor.Hardware.Mainboard {
  internal class Identification {

    public static Manufacturer GetManufacturer(string name) {
      switch (name) {
        case "Alienware":
          return Manufacturer.Alienware;
        case "Apple Inc.":
          return Manufacturer.Apple;
        case "ASRock":
          return Manufacturer.ASRock;
        case "ASUSTeK Computer INC.":
        case "ASUSTeK COMPUTER INC.":
          return Manufacturer.ASUS;
        case "Dell Inc.":
          return Manufacturer.Dell;
        case "DFI":
        case "DFI Inc.":
          return Manufacturer.DFI;
        case "ECS":
          return Manufacturer.ECS;
        case "EPoX COMPUTER CO., LTD":
          return Manufacturer.EPoX;
        case "EVGA":
          return Manufacturer.EVGA;
        case "First International Computer, Inc.":
          return Manufacturer.FIC;
        case "FUJITSU":
        case "FUJITSU SIEMENS":
          return Manufacturer.Fujitsu;
        case "Gigabyte Technology Co., Ltd.":
          return Manufacturer.Gigabyte;
        case "Hewlett-Packard":
          return Manufacturer.HP;
        case "IBM":
          return Manufacturer.IBM;
        case "Intel":
        case "Intel Corp.":
        case "Intel Corporation":
        case "INTEL Corporation":
          return Manufacturer.Intel;
        case "Lenovo":
        case "LENOVO":
          return Manufacturer.Lenovo;
        case "Micro-Star International":
        case "Micro-Star International Co., Ltd":
        case "MICRO-STAR INTERNATIONAL CO., LTD":
        case "MICRO-STAR INTERNATIONAL CO.,LTD":
        case "MSI":
          return Manufacturer.MSI;
        case "Shuttle":
          return Manufacturer.Shuttle;
        case "Supermicro":
          return Manufacturer.Supermicro;
        case "TOSHIBA":
          return Manufacturer.Toshiba;
        case "XFX":
          return Manufacturer.XFX;
        case "To be filled by O.E.M.":
          return Manufacturer.Unknown;
        default:
          return Manufacturer.Unknown;
      }
    }

    public static Model GetModel(string name) {
      switch (name) {
        case "880GMH/USB3":
          return Model._880GMH_USB3;
        case "B85M-DGS":
          return Model.B85M_DGS;
        case "ASRock AOD790GX/128M":
          return Model.AOD790GX_128M;
        case "AB350 Pro4":
          return Model.AB350_PRO4;
        case "AB350M Pro4":
          return Model.AB350M_PRO4;
        case "AB350M":
          return Model.AB350M;
        case "Fatal1ty AB350 Gaming K4":
          return Model.Fatal1ty_AB350_Gaming_K4;
        case "AB350M-HDV":
          return Model.AB350M_HDV;
        case "X399 Phantom Gaming 6":
          return Model.PHANTOM_GAMING_6;
        case "A320M-HDV":
          return Model.A320M_HDV;
        case "P55 Deluxe":
          return Model.P55_Deluxe;
        case "Crosshair III Formula":
          return Model.Crosshair_III_Formula;
        case "M2N-SLI DELUXE":
          return Model.M2N_SLI_DELUXE;
        case "M4A79XTD EVO":
          return Model.M4A79XTD_EVO;
        case "P5W DH Deluxe":
          return Model.P5W_DH_Deluxe;
        case "P6T":
          return Model.P6T;
        case "P6X58D-E":
          return Model.P6X58D_E;
        case "P8P67":
        case "P8P67 REV 3.1":
          return Model.P8P67;
        case "P8P67 EVO":
          return Model.P8P67_EVO;
        case "P8P67 PRO":
          return Model.P8P67_PRO;
        case "P8P67-M PRO":
          return Model.P8P67_M_PRO;
        case "P8Z77-V":
          return Model.P8Z77_V;
        case "P9X79":
          return Model.P9X79;
        case "Rampage Extreme":
          return Model.Rampage_Extreme;
        case "Rampage II GENE":
          return Model.Rampage_II_GENE;
        case "LP BI P45-T2RS Elite":
          return Model.LP_BI_P45_T2RS_Elite;
        case "LP DK P55-T3eH9":
          return Model.LP_DK_P55_T3eH9;
        case "A890GXM-A":
          return Model.A890GXM_A;
        case "X58 SLI Classified":
          return Model.X58_SLI_Classified;
        case "965P-S3":
          return Model._965P_S3;
        case "EP45-DS3R":
          return Model.EP45_DS3R;
        case "EP45-UD3R":
          return Model.EP45_UD3R;
        case "EX58-EXTREME":
          return Model.EX58_EXTREME;
        case "EX58-UD3R":
          return Model.EX58_UD3R;
        case "G41M-Combo":
          return Model.G41M_Combo;
        case "G41MT-S2":
          return Model.G41MT_S2;
        case "G41MT-S2P":
          return Model.G41MT_S2P;
        case "GA-970A-UD3":
          return Model._970A_UD3;
        case "GA-MA770T-UD3":
          return Model.MA770T_UD3;
        case "GA-MA770T-UD3P":
          return Model.MA770T_UD3P;
        case "GA-MA785GM-US2H":
          return Model.MA785GM_US2H;
        case "GA-MA785GMT-UD2H":
          return Model.MA785GMT_UD2H;
        case "GA-MA78LM-S2H":
          return Model.MA78LM_S2H;
        case "GA-MA790X-UD3P":
          return Model.MA790X_UD3P;
        case "H55-USB3":
          return Model.H55_USB3;
        case "H55N-USB3":
          return Model.H55N_USB3;
        case "H61M-DS2 REV 1.2":
          return Model.H61M_DS2_REV_1_2;
        case "H61M-USB3-B3 REV 2.0":
          return Model.H61M_USB3_B3_REV_2_0;
        case "H67A-UD3H-B3":
          return Model.H67A_UD3H_B3;
        case "H67A-USB3-B3":
          return Model.H67A_USB3_B3;
        case "P35-DS3":
          return Model.P35_DS3;
        case "P35-DS3L":
          return Model.P35_DS3L;
        case "P55-UD4":
          return Model.P55_UD4;
        case "P55A-UD3":
          return Model.P55A_UD3;
        case "P55M-UD4":
          return Model.P55M_UD4;
        case "P67A-UD3-B3":
          return Model.P67A_UD3_B3;
        case "P67A-UD3R-B3":
          return Model.P67A_UD3R_B3;
        case "P67A-UD4-B3":
          return Model.P67A_UD4_B3;
        case "P8Z68-V PRO":
          return Model.P8Z68_V_PRO;
        case "X38-DS5":
          return Model.X38_DS5;
        case "X58A-UD3R":
          return Model.X58A_UD3R;
        case "Z270 PC MATE":
        case "Z270 PC MATE (MS-7A72)":
          return Model.Z270_PC_MATE;
        case "X79-UD3":
          return Model.X79_UD3;
        case "Z68A-D3H-B3":
          return Model.Z68A_D3H_B3;
        case "Z68AP-D3":
          return Model.Z68AP_D3;
        case "Z68X-UD3H-B3":
          return Model.Z68X_UD3H_B3;
        case "Z68X-UD7-B3":
          return Model.Z68X_UD7_B3;
        case "Z390 M GAMING-CF":
          return Model.Z390_M_GAMING;
        case "Z390 AORUS ULTRA":
          return Model.Z390_AORUS_ULTRA;
        case "Z390 UD":
          return Model.Z390_UD;
        case "FH67":
          return Model.FH67;
        case "AX370-Gaming K7":
          return Model.AX370_K7;
        case "PRIME X370-PRO":
          return Model.PRIME_X370_PRO;
        case "ROG MAXIMUS X APEX":
          return Model.ROG_MAXIMUS_APEX;
        case "AB350-Gaming 3-CF":
          return Model.AB350_Gaming_3_CF;
        case "X399 AORUS Gaming 7":
          return Model.X399_AOURUS_GAMING7;
        case "ROG ZENITH EXTREME":
          return Model.ROG_ZENITH_EXTREME;
        case "Z170-A":
          return Model.Z170_A;
        case "Z77 Pro4-M":
          return Model.Z77PRO4M;
        case "AX370-Gaming 5":
          return Model.AX370_Gaming_5;
        case "TUF X470-PLUS GAMING":
          return Model.TUF_X470_PLUS_GAMING;
        case "B360M PRO-VDH (MS-7B24)":
          return Model.B360M_PRO_VDH;
        case "B450-A PRO (MS-7B86)":
          return Model.B450A_PRO;
        case "Base Board Product Name":
        case "To be filled by O.E.M.":
          return Model.Unknown;
        default:
          return Model.Unknown;
      }
    }
  }
}