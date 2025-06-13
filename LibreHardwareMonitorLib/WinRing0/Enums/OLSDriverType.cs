//-----------------------------------------------------------------------------
//     Author : hiyohiyo
//       Mail : hiyohiyo@crystalmark.info
//        Web : http://openlibsys.org/
//    License : The modified BSD license
//
//                     Copyright 2007-2009 OpenLibSys.org. All rights reserved.
//-----------------------------------------------------------------------------
// This is support library for WinRing0 1.3.x.

namespace LibreHardwareMonitor.WinRing0.Enums
{
    //For WinRing0
    internal enum OLSDriverType
    {
        OLS_DRIVER_TYPE_UNKNOWN     = 0,
        OLS_DRIVER_TYPE_WIN_9X      = 1,
        OLS_DRIVER_TYPE_WIN_NT      = 2,
        OLS_DRIVER_TYPE_WIN_NT4     = 3,    // Obsolete
        OLS_DRIVER_TYPE_WIN_NT_X64  = 4,
        OLS_DRIVER_TYPE_WIN_NT_IA64 = 5,
    }
}
