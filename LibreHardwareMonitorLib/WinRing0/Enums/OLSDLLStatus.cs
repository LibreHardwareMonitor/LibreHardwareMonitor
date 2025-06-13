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
    internal enum OLSDLLStatus
    {
        OLS_DLL_NO_ERROR                     = 0,
        OLS_DLL_UNSUPPORTED_PLATFORM         = 1,
        OLS_DLL_DRIVER_NOT_LOADED            = 2,
        OLS_DLL_DRIVER_NOT_FOUND             = 3,
        OLS_DLL_DRIVER_UNLOADED              = 4,
        OLS_DLL_DRIVER_NOT_LOADED_ON_NETWORK = 5,
        OLS_DLL_UNKNOWN_ERROR                = 9,
    }
}
