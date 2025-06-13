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
    internal enum OLSErrorPCI : uint
    {
        OLS_ERROR_PCI_BUS_NOT_EXIST = 0xE0000001,
        OLS_ERROR_PCI_NO_DEVICE     = 0xE0000002,
        OLS_ERROR_PCI_WRITE_CONFIG  = 0xE0000003,
        OLS_ERROR_PCI_READ_CONFIG   = 0xE0000004,
    }
}
