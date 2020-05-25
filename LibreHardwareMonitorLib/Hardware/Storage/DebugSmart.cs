﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

#if DEBUG

using System;
using LibreHardwareMonitor.Interop;

namespace LibreHardwareMonitor.Hardware.Storage
{
    internal class DebugSmart : ISmart
    {
        private readonly Drive[] _drives = {
            new Drive("KINGSTON SNV425S264GB", null, 16,
                @" 01 000000000000 100 100      
                02 000000000000 100 100      
                03 000000000000 100 100      
                05 000000000000 100 100      
                07 000000000000 100 100      
                08 000000000000 100 100      
                09 821E00000000 100 100      
                0A 000000000000 100 100      
                0C 950200000000 100 100      
                A8 000000000000 100 100      
                AF 000000000000 100 100      
                C0 000000000000 100 100      
                C2 290014002B00 100 41       
                C5 000000000000 100 100      
                F0 000000000000 100 100      
                AA 07007B000000 100 100      
                AD 0E1E71304919 100 100"),
            new Drive("PLEXTOR  PX-128M2S", "1.03", 16,
                @" 01 000000000000 100 100 0   
                03 000000000000 100 100 0   
                04 000000000000 100 100 0   
                05 000000000000 100 100 0   
                09 250100000000 100 100 0   
                0A 000000000000 100 100 0   
                0C D10000000000 100 100 0   
                B2 000000000000 100 100 0   
                BB 000000000000 100 100 0   
                BE 000000000000 100 100 0   
                C0 000000000000 100 100 0   
                C1 000000000000 100 100 0   
                C2 000000000000 100 100 0   
                C3 000000000000 100 100 0   
                C5 000000000000 100 100 0   
                C6 000000000000 100 100 0   
                C7 000000000000 100 100 0"),
            new Drive("OCZ-VERTEX2", "1.25", 16,
                @" 01 DADAD5000000 100 106 50
                05 000000000000 100 100 3 
                09 DF0900004A2F 100 100 0 
                0C FC0100000000 100 100 0 
                AB 000000000000 0   0   0 
                AC 000000000000 0   0   0 
                AE 1F0000000000 0   0   0 
                B1 000000000000 0   0   0 
                B5 000000000000 0   0   0 
                B6 000000000000 0   0   0 
                BB 000000000000 100 100 0 
                C2 010081007F00 129 1   0 
                C3 DADAD5000000 100 106 0 
                C4 000000000000 100 100 0 
                E7 000000000000 100 100 10
                E9 800400000000 0   0   0 
                EA 000600000000 0   0   0 
                F1 000600000000 0   0   0 
                F2 801200000000 0   0   0"),
            new Drive("WDC WD5000AADS-00S9B0", null, 10,
                @" 1   000000000000 200 200         
                3   820D00000000 149 150         
                4   610800000000 98  98          
                5   000000000000 200 200         
                7   000000000000 253 100         
                9   0F1F00000000 90  90          
                10  000000000000 100 100         
                11  000000000000 100 100         
                12  880200000000 100 100         
                192 6B0000000000 200 200         
                193 E9CB03000000 118 118         
                194 280000000000 94  103         
                196 000000000000 200 200         
                197 000000000000 200 200         
                198 000000000000 200 200         
                199 000000000000 200 200         
                200 000000000000 200 200         
                130 7B0300010002 1   41          
                5   000000000000 0   0           
                1   000000000000 0   0"),
            new Drive("INTEL SSDSA2M080G2GC", null, 10,
                @" 3   000000000000 100 100         
                4   000000000000 100 100         
                5   010000000000 100 100         
                9   B10B00000000 100 100         
                12  DD0300000000 100 100         
                192 480000000000 100 100         
                225 89DB00000000 200 200         
                226 3D1B00000000 100 100         
                227 030000000000 100 100         
                228 7F85703C0000 100 100         
                232 000000000000 99  99          
                233 000000000000 98  98          
                184 000000000000 100 100         
                1   000000000000 0   0"),
            new Drive("OCZ-VERTEX", null, 10,
                @" 1   000000000000 0   8   
                9   000000000000 30  99  
                12  000000000000 0   15  
                184 000000000000 0   7   
                195 000000000000 0   0   
                196 000000000000 0   2   
                197 000000000000 0   0   
                198 B9ED00000000 214 176 
                199 352701000000 143 185 
                200 B10500000000 105 55  
                201 F40A00000000 238 194 
                202 020000000000 137 35  
                203 020000000000 125 63  
                204 000000000000 0   0   
                205 000000000000 19  136 
                206 000000000000 22  54  
                207 010000000000 113 226 
                208 000000000000 49  232 
                209 000000000000 0   98  
                211 000000000000 0   0   
                212 000000000000 0   0   
                213 000000000000 0   0"),
            new Drive("INTEL SSDSA2CW120G3", null, 16,
                @"03 000000000000 100 100 0
                04 000000000000 100 100 0
                05 000000000000 100 100 0
                09 830200000000 100 100 0
                0C 900100000000 100 100 0
                AA 000000000000 100 100 0
                AB 000000000000 100 100 0
                AC 000000000000 100 100 0
                B8 000000000000 100 100 0
                BB 000000000000 100 100 0
                C0 040000000000 100 100 0
                E1 FF4300000000 100 100 0
                E2 E57D14000000 100 100 0
                E3 000000000000 100 100 0
                E4 E39600000000 100 100 0
                E8 000000000000 100 100 0
                E9 000000000000 100 100 0
                F1 FF4300000000 100 100 0
                F2 264F00000000 100 100 0"),
            new Drive("CORSAIR CMFSSD-128GBG2D", "VBM19C1Q", 16,
                @"09 100900000000 99  99  0 
                0C 560200000000 99  99  0 
                AF 000000000000 100 100 10
                B0 000000000000 100 100 10
                B1 2A0000000000 99  99  17
                B2 180000000000 60  60  10
                B3 4B0000000000 98  98  10
                B4 B50E00000000 98  98  10
                B5 000000000000 100 100 10
                B6 000000000000 100 100 10
                B7 000000000000 100 100 10
                BB 000000000000 100 100 0 
                C3 000000000000 200 200 0 
                C6 000000000000 100 100 0 
                C7 810100000000 253 253 0 
                E8 240000000000 60  60  10
                E9 630594120000 92  92  0"),
            new Drive("Maxtor 6L300R0", null, 10,
                @"3   9E5500000000 183 193         
                4   0A0D00000000 252 252         
                5   010000000000 253 253         
                6   000000000000 253 253         
                7   000000000000 252 253         
                8   DFA700000000 224 245         
                9   CE5700000000 155 155         
                10  000000000000 252 253         
                11  000000000000 252 253         
                12  BA0400000000 250 250         
                192 000000000000 253 253         
                193 000000000000 253 253         
                194 3D0000000000 253 42          
                195 5D1F00000000 252 253         
                196 000000000000 253 253         
                197 010000000000 253 253         
                198 000000000000 253 253         
                199 030000000000 196 199         
                200 000000000000 252 253         
                201 000000000000 252 253         
                202 000000000000 252 253         
                203 000000000000 252 253         
                204 000000000000 252 253         
                205 000000000000 252 253         
                207 000000000000 252 253         
                208 000000000000 252 253         
                209 EA0000000000 234 234         
                210 000000000000 252 253         
                211 000000000000 252 253         
                212 000000000000 252 253         
                130 5B0300010002 1   9           
                59  FC3203030100 205 0           
                1   000000000000 0   0           
                144 000000000000 0   34 "),
            new Drive("M4-CT256M4SSD2", "0309", 16,
                @"01 000000000000 100 100 50     
                05 000000000000 100 100 10     
                09 AB0100000000 100 100 1      
                0C 6E0000000000 100 100 1      
                AA 000000000000 100 100 10     
                AB 000000000000 100 100 1      
                AC 000000000000 100 100 1      
                AD 060000000000 100 100 10     
                AE 000000000000 100 100 1      
                B5 79003D00B700 100 100 1      
                B7 000000000000 100 100 1      
                B8 000000000000 100 100 50     
                BB 000000000000 100 100 1      
                BC 000000000000 100 100 1      
                BD 5B0000000000 100 100 1      
                C2 000000000000 100 100 0      
                C3 000000000000 100 100 1      
                C4 000000000000 100 100 1      
                C5 000000000000 100 100 1      
                C6 000000000000 100 100 1      
                C7 000000000000 100 100 1      
                CA 000000000000 100 100 1      
                CE 000000000000 100 100 1 "),
            new Drive("C300-CTFDDAC256MAG", "0007", 16,
                @"01 000000000000 100 100 0  
                05 000000000000 100 100 0  
                09 4C0A00000000 100 100 0  
                0C 0F0100000000 100 100 0  
                AA 000000000000 100 100 0  
                AB 000000000000 100 100 0  
                AC 000000000000 100 100 0  
                AD 1B0000000000 100 100 0  
                AE 000000000000 100 100 0  
                B5 D30357012B05 100 100 0  
                B7 000000000000 100 100 0  
                B8 000000000000 100 100 0  
                BB 000000000000 100 100 0  
                BC 000000000000 100 100 0  
                BD C60100000000 100 100 0  
                C3 000000000000 100 100 0  
                C4 000000000000 100 100 0  
                C5 000000000000 100 100 0  
                C6 000000000000 100 100 0  
                C7 000000000000 100 100 0  
                CA 000000000000 100 100 0  
                CE 000000000000 100 100 0"),
            new Drive("M4-CT064M4SSD2", "0009", 16,
                @"01 000000000000 100 100 50
                05 000000000000 100 100 10
                09 260000000000 100 100 1 
                0C 5A0000000000 100 100 1 
                AA 000000000000 100 100 10
                AB 000000000000 100 100 1 
                AC 000000000000 100 100 1 
                AD 010000000000 100 100 10
                AE 000000000000 100 100 1 
                B5 2B000E003A00 100 100 1 
                B7 000000000000 100 100 1 
                B8 000000000000 100 100 50
                BB 000000000000 100 100 1 
                BC 000000000000 100 100 1 
                BD 310000000000 100 100 1 
                C2 000000000000 100 100 0 
                C3 000000000000 100 100 1 
                C4 000000000000 100 100 1 
                C5 000000000000 100 100 1 
                C6 000000000000 100 100 1 
                C7 000000000000 100 100 1 
                CA 000000000000 100 100 1 
                CE 000000000000 100 100 1"),
            new Drive("M4-CT128M4SSD2", "000F", 16,
                @"01 000000000000 100 100 50 
                05 000000000000 100 100 10 
                09 CA1400000000 100 100 1  
                0C A30200000000 100 100 1  
                AA 000000000000 100 100 10 
                AB 000000000000 100 100 1  
                AC 000000000000 100 100 1  
                AD 1F0000000000 99  99  10 
                AE 140000000000 100 100 1  
                B5 12037C028E05 100 100 1  
                B7 000000000000 100 100 1  
                B8 000000000000 100 100 50 
                BB 000000000000 100 100 1  
                BC 000000000000 100 100 1  
                BD 510000000000 100 100 1  
                C2 000000000000 100 100 0  
                C3 000000000000 100 100 1  
                C4 000000000000 100 100 1  
                C5 000000000000 100 100 1  
                C6 000000000000 100 100 1  
                C7 000000000000 100 100 1  
                CA 010000000000 99  99  1  
                CE 000000000000 100 100 1 "),
            new Drive("Samsung SSD 840 PRO Series", "DXM05B0Q", 16,
                @"05 000000000000 100 100 10 
                09 541200000000 99  99  0  
                0C 820500000000 98  98  0  
                B1 B90200000000 80  80  0  
                B3 000000000000 100 100 10 
                B5 000000000000 100 100 10 
                B6 000000000000 100 100 10 
                B7 000000000000 100 100 10 
                BB 000000000000 100 100 0  
                BE 1C0000000000 48  72  0  
                C3 000000000000 200 200 0  
                C7 020000000000 99  99  0  
                EB 690000000000 99  99  0  
                F1 A56AA1F60200 99  99  0")
        };

        private int _driveNumber;

        public DebugSmart(int driveNumber)
        {
            _driveNumber = driveNumber;
        }

        public bool IsValid => true;

        public void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool EnableSmart()
        {
            if (_driveNumber < 0)
                throw new ObjectDisposedException(nameof(DebugSmart));
            return true;
        }

        public Kernel32.SMART_ATTRIBUTE[] ReadSmartData()
        {
            if (_driveNumber < 0)
                throw new ObjectDisposedException(nameof(DebugSmart));
            return _drives[_driveNumber].DriveAttributeValues;
        }

        public Kernel32.SMART_THRESHOLD[] ReadSmartThresholds()
        {
            if (_driveNumber < 0)
                throw new ObjectDisposedException(nameof(DebugSmart));
            return _drives[_driveNumber].DriveThresholdValues;
        }

        public bool ReadNameAndFirmwareRevision(out string name, out string firmwareRevision)
        {
            if (_driveNumber < 0)
                throw new ObjectDisposedException(nameof(DebugSmart));
            name = _drives[_driveNumber].Name;
            firmwareRevision = _drives[_driveNumber].FirmwareVersion;
            return true;
        }

        public void Dispose()
        {
            Close();
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
                _driveNumber = -1;
        }

        private class Drive
        {
            public Drive(string name, string firmware, int idBase, string value)
            {
                Name = name;
                FirmwareVersion = firmware;

                string[] lines = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                DriveAttributeValues = new Kernel32.SMART_ATTRIBUTE[lines.Length];
                var thresholds = new System.Collections.Generic.List<Kernel32.SMART_THRESHOLD>();

                for (int i = 0; i < lines.Length; i++)
                {
                    string[] array = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (array.Length != 4 && array.Length != 5)
                        throw new Exception();

                    var v = new Kernel32.SMART_ATTRIBUTE { Id = Convert.ToByte(array[0], idBase), RawValue = new byte[6] };

                    for (int j = 0; j < 6; j++)
                    {
                        v.RawValue[j] = Convert.ToByte(array[1].Substring(2 * j, 2), 16);
                    }

                    v.WorstValue = Convert.ToByte(array[2], 10);
                    v.CurrentValue = Convert.ToByte(array[3], 10);

                    DriveAttributeValues[i] = v;

                    if (array.Length == 5)
                    {
                        var t = new Kernel32.SMART_THRESHOLD { Id = v.Id, Threshold = Convert.ToByte(array[4], 10) };
                        thresholds.Add(t);
                    }
                }

                DriveThresholdValues = thresholds.ToArray();
            }

            public Kernel32.SMART_ATTRIBUTE[] DriveAttributeValues { get; }

            public Kernel32.SMART_THRESHOLD[] DriveThresholdValues { get; }

            public string FirmwareVersion { get; }

            public string Name { get; }
        }
    }
}

#endif
