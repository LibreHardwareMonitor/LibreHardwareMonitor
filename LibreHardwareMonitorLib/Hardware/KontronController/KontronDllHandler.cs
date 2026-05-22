// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BlackSharp.Core.Interop.Windows.Native;
using HidSharp.Reports;
using LibreHardwareMonitor.Interop;
using Microsoft.Win32;
using Windows.Win32;

namespace LibreHardwareMonitor.Hardware.KontronDll;

static class KontronHWGroupActiveFlags
{
    public static bool _isSensorGroupActive = false;
    public static bool _isWatchdogGroupActive = false;
}

internal sealed class KontronDllHandler
{
    private static StringBuilder _report;

    private const string KontronKscDllName = "kscapi.dll";
    private static FreeLibrarySafeHandle _kontronKscDll;

    private delegate int KscApiInitDeInitFuncType();

    private delegate int KscApiGetDllDriverVersionFuncType(ulong versionDataAddr);
    private static KscApiDllAndDriverVersion _kscApiDllVersion;
    private static KscApiDllAndDriverVersion _kscApiDriverVersion;

    private delegate int KscApiGetDeviceInfoFuncType(ulong devInfoAddr, byte mode);
    private static KscApiDevInfo _kscDeviceInfoData;

    private static int _kscError = 0;

    public KontronDllHandler(StringBuilder report)
    {
        _report = report;
    }
    public FreeLibrarySafeHandle kontronKscDll { get { return _kontronKscDll; } }

    public bool CheckDllPrerequisites()
    {
        // This KSC add-on does not support UNIX systems.
        if (Software.OperatingSystem.IsUnix)
        {
            _report.AppendLine("> This Kontron KSC add-on does not support UNIX systems - failed!");
            _report.AppendLine();
            return false;
        }
        _report.AppendLine("- Operating system is Windows.");

        // KSC does not support Windows 32-Bit systems.
        if (!Software.OperatingSystem.Is64Bit)
        {
            _report.AppendLine("> Kontron KSC does not support Windows 32-Bit systems - failed!");
            _report.AppendLine();
            return false;
        }
        _report.AppendLine("- Windows architecture is x64.");

        return true;
    }

    public bool OpenKscDll(LhmKscHardwareGroup hwGroup)
    {
        // load KSC DLL

        _kontronKscDll = PInvoke.LoadLibrary(KontronKscDllName);
        if (_kontronKscDll.IsInvalid)
            return false;

        // get ~Init function address

        IntPtr initFuncPtr = PInvoke.GetProcAddress(_kontronKscDll, "KscApiInit");
        if (initFuncPtr == IntPtr.Zero)
            return false;
        KscApiInitDeInitFuncType kscApiInitFunc =
            (KscApiInitDeInitFuncType)Marshal.GetDelegateForFunctionPointer(initFuncPtr,
                                                                            typeof(KscApiInitDeInitFuncType));
        // call ~Init function

        int resultInt = kscApiInitFunc();
        if (resultInt != 0)
        {
            _kscError = resultInt;
            return false;
        }

        // set hwGroup active flags

        if( hwGroup == LhmKscHardwareGroup.LHM_KSC_HWGROUP_SENSOR)
            KontronHWGroupActiveFlags._isSensorGroupActive = true;
        if (hwGroup == LhmKscHardwareGroup.LHM_KSC_HWGROUP_WATCHDOG)
            KontronHWGroupActiveFlags._isWatchdogGroupActive = true;

        return true;
    }

    public bool CloseKscDll(LhmKscHardwareGroup hwGroup)
    {
        if (hwGroup == LhmKscHardwareGroup.LHM_KSC_HWGROUP_SENSOR)
            KontronHWGroupActiveFlags._isSensorGroupActive = false;
        if (hwGroup == LhmKscHardwareGroup.LHM_KSC_HWGROUP_WATCHDOG)
            KontronHWGroupActiveFlags._isWatchdogGroupActive = false;

        // If a Kontron hardware group is still active, the KSC DLL is not yet closed and unloaded.

        if (KontronHWGroupActiveFlags._isSensorGroupActive ||
            KontronHWGroupActiveFlags._isWatchdogGroupActive)
            return true;

        // get ~DeInit function address

        IntPtr deinitFuncPtr = PInvoke.GetProcAddress(_kontronKscDll, "KscApiDeInit");
        if (deinitFuncPtr == IntPtr.Zero)
            return false;
        KscApiInitDeInitFuncType kscApiDeInitFunc =
            (KscApiInitDeInitFuncType)Marshal.GetDelegateForFunctionPointer(deinitFuncPtr,
                                                                            typeof(KscApiInitDeInitFuncType));

        // call ~DeInit function 

        int resultInt = kscApiDeInitFunc();
        if (resultInt != 0)
        {
            _kscError = resultInt;
            return false;
        }

        return true;
    }

    public bool KscDllQueryVersions()
    {
        //_report.Append("- Query KSC DLL Version .. ");

        // get ~SpecVersion function address
        IntPtr specVersionFuncPtr = PInvoke.GetProcAddress(_kontronKscDll, "KscApiSpecVersion");
        if (specVersionFuncPtr == IntPtr.Zero)
        {
            _report.AppendLine("> Query KSC DLL Version - get ~SpecVersion function address - failed!");
            return false;
        }
        KscApiGetDllDriverVersionFuncType kscApiGetDllVersionFunc =
            (KscApiGetDllDriverVersionFuncType)Marshal.GetDelegateForFunctionPointer(specVersionFuncPtr,
                                                                                     typeof(KscApiGetDllDriverVersionFuncType));

        // build spec version data address
        ulong dllVersionDataAddress = 0;
        unsafe
        {
            fixed (KscApiDllAndDriverVersion* pKscDllVersionData = &_kscApiDllVersion)
            {
                dllVersionDataAddress = (ulong)pKscDllVersionData;
            }
        }

        // query KSC DLL version
        int result = kscApiGetDllVersionFunc(dllVersionDataAddress);
        if (result != 0)
        {
            _kscError = result;
            _report.AppendLine("> Query KSC DLL Version - get value - failed! Error = " + _kscError);
            _report.AppendLine("failed! Error = " + _kscError);
            return false;
        }

        _report.AppendLine("- KSC DLL version:            " + _kscApiDllVersion.MajorVer +
                           "." + _kscApiDllVersion.MinorVer +
                           "." + _kscApiDllVersion.PatchVer +
                           "." + _kscApiDllVersion.BuildVer);

        // ----------------

        // get ~DriverVersion function address
        IntPtr driverVersionFuncPtr = PInvoke.GetProcAddress(_kontronKscDll, "KscApiGetDriverVersion");
        if (driverVersionFuncPtr == IntPtr.Zero)
        {
            _report.AppendLine("Query KSC Driver Version - get ~DriverVersion function address - failed!");
            return false;
        }
        KscApiGetDllDriverVersionFuncType kscApiGetDriverVersionFunc =
            (KscApiGetDllDriverVersionFuncType)Marshal.GetDelegateForFunctionPointer(driverVersionFuncPtr,
                                                                                     typeof(KscApiGetDllDriverVersionFuncType));
        // build driver version data address
        ulong driverVersionDataAddress = 0;
        unsafe
        {
            fixed (KscApiDllAndDriverVersion* pKscDriverVersionData = &_kscApiDriverVersion)
            {
                driverVersionDataAddress = (ulong)pKscDriverVersionData;
            }
        }

        // query KSC DLL version
        result = kscApiGetDriverVersionFunc(driverVersionDataAddress);
        if (result != 0)
        {
            _kscError = result;
            _report.AppendLine("Query KSC Driver Version - get value - failed! Error = " + _kscError);
            return false;
        }

        _report.AppendLine("- KSC Driver version:         " + _kscApiDriverVersion.MajorVer +
                           "." + _kscApiDriverVersion.MinorVer +
                           "." + _kscApiDriverVersion.PatchVer +
                           "." + _kscApiDriverVersion.BuildVer);

        // ----------------

        // get ~DeviceInfo function address
        IntPtr deviceInfoFuncPtr = PInvoke.GetProcAddress(_kontronKscDll, "KscApiDeviceInfo");
        if (deviceInfoFuncPtr == IntPtr.Zero)
        {
            _report.AppendLine("Query KSC device info - get ~DeviceInfo function address - failed!");
            return false;
        }
        KscApiGetDeviceInfoFuncType kscApiDeviceInfoFunc =
            (KscApiGetDeviceInfoFuncType)Marshal.GetDelegateForFunctionPointer(deviceInfoFuncPtr,
                                                                               typeof(KscApiGetDeviceInfoFuncType));

        // build driver version data address
        ulong deviceInfoStructAddress = 0;
        unsafe
        {
            fixed (KscApiDevInfo* pKscDeviceInfoData = &_kscDeviceInfoData)
            {
                deviceInfoStructAddress = (ulong)pKscDeviceInfoData;
            }
        }

        // query KSC core firmware version
        result = kscApiDeviceInfoFunc(deviceInfoStructAddress,
                                      (byte)KscApiDevInfoCategory.eKscApiDevInfoCFver);
        if (result != 0)
        {
            _kscError = result;
            _report.AppendLine("Query KSC device info - get core firmware version - failed! Error = " + _kscError);
            return false;
        }

        // query KSC firmware spec version
        result = kscApiDeviceInfoFunc(deviceInfoStructAddress,
                                      (byte)KscApiDevInfoCategory.eKscApiDevInfoKSC);
        if (result != 0)
        {
            _kscError = result;
            _report.AppendLine("Query KSC device info - get firmware spec version - failed! Error = " + _kscError);
            return false;
        }

        // query KSC board firmware version
        result = kscApiDeviceInfoFunc(deviceInfoStructAddress,
                                      (byte)KscApiDevInfoCategory.eKscApiDevInfoBoardFirmVer);
        if (result != 0)
        {
            _kscError = result;
            _report.AppendLine("Query KSC device info - get board firmware version - failed! Error = " + _kscError);
            return false;
        }

        // query KSC board name
        result = kscApiDeviceInfoFunc(deviceInfoStructAddress,
                                      (byte)KscApiDevInfoCategory.eKscApiDevInfoBoardName);
        if (result != 0)
        {
            _kscError = result;
            _report.AppendLine("Query KSC device info - get board name - failed! Error = " + _kscError);
            return false;
        }

        _report.AppendLine("- KSC core firmware version:  " + _kscDeviceInfoData.CoreFirmwareVersion.MajorVer +
                           "." + _kscDeviceInfoData.CoreFirmwareVersion.MinorVer +
                           "." + _kscDeviceInfoData.CoreFirmwareVersion.PatchVer +
                           "." + _kscDeviceInfoData.CoreFirmwareVersion.ReleaseState);

        _report.AppendLine("- KSC firmware spec version:  " + _kscDeviceInfoData.SpecVersion.MajorVer +
                           "." + _kscDeviceInfoData.SpecVersion.MinorVer);

        _report.AppendLine("- KSC board firmware version: " + _kscDeviceInfoData.BoardFirmwareVersion.MajorVer +
                           "." + _kscDeviceInfoData.BoardFirmwareVersion.MinorVer +
                           "." + _kscDeviceInfoData.BoardFirmwareVersion.PatchVer +
                           "." + _kscDeviceInfoData.BoardFirmwareVersion.ReleaseState);

        {
            string boardName;
            unsafe
            {
                fixed (byte* pBoardName = _kscDeviceInfoData.BoardName)
                {
                    boardName = Encoding.ASCII.GetString(pBoardName, KSC_API_BOARDNAME_LEN).Trim('\0');
                }
            }
            _report.AppendLine("- KSC board name:             " + boardName);
        }
        return true;
    }

    // -------------------------------------------------------------------------------
    // consts, structs, enums, ...
    // -------------------------------------------------------------------------------

    public const int KSC_API_DEVICE_ID_LEN = 3;
    public const int KSC_API_LABEL_LEN = 24;
    public const int KSC_API_BOARDNAME_LEN = 13;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KscApiDevInfoCoreAndBoardFirmwareVersion
    {
        public byte MajorVer;
        public byte MinorVer;
        public byte PatchVer;
        public byte ReleaseState;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KscApiDevInfoSpec
    {
        public byte MajorVer;
        public byte MinorVer;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KscApiDevInfoPscFirmVer
    {
        public byte MajorVer;
        public byte MinorVer;
        public byte ReleaseState;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct KscApiDevInfo
    {
        public fixed byte DeviceId[KSC_API_DEVICE_ID_LEN + 1];
        public byte DeviceClassId;
        public KscApiDevInfoCoreAndBoardFirmwareVersion CoreFirmwareVersion;
        public KscApiDevInfoSpec SpecVersion;
        public fixed byte BoardName[KSC_API_BOARDNAME_LEN];
        public KscApiDevInfoCoreAndBoardFirmwareVersion BoardFirmwareVersion;
        public byte ReceiveDataSize;
        public byte TransmitDataSize;
        public uint ScmInfo;
        public uint G3PUCnt;
        public uint S5PUCnt;
        public uint PUTime_aG3;
        public uint OTCnt;
        public uint OPMode;
        public KscApiDevInfoPscFirmVer PSCFirmwareVersion;
        public uint PSCInfo;
    }
    internal enum KscApiDevInfoCategory
    {
        eKscApiDevInfoDToken = 0,
        eKscApiDevInfoCFver,
        eKscApiDevInfoKSC,
        eKscApiDevInfoBoardName,
        eKscApiDevInfoBoardFirmVer,
        eKscApiDevInfoHIC,
        eKscApiDevInfoScmInfo,
        eKscApiDevInfoG3Cnt,
        eKscApiDevInfoS5Cnt,
        eKscApiDevInfoPUpTime,
        eKscApiDevInfoOPTCnt,
        eKscApiDevInfoOM,
        eKscApiDevInfoPscFirmVer = 16,
        eKscApiDevInfoPscScmInfo = 17,
        eKscApiDevInfoFIELDCOUNT // current number of valid fields
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KscApiDllAndDriverVersion
    {
        public byte MajorVer;
        public byte MinorVer;
        public byte PatchVer;
        public byte BuildVer;
    }

    public enum LhmKscHardwareGroup
    {
        LHM_KSC_HWGROUP_SENSOR   = 0x01,
        LHM_KSC_HWGROUP_WATCHDOG = 0x02
    }
}
