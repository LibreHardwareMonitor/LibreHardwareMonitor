#include "stdafx.h"
#include "hwinterfacedrv.h"

typedef BOOL (WINAPI *LPFN_ISWOW64PROCESS) (HANDLE, PBOOL);
typedef BOOL (WINAPI *LPFN_WOW64DISABLE) (PVOID*);
typedef BOOL (WINAPI *LPFN_WOW64REVERT) (PVOID);

LPFN_ISWOW64PROCESS	fnIsWow64Process = (LPFN_ISWOW64PROCESS)GetProcAddress(GetModuleHandle(_T("kernel32")),"IsWow64Process");
LPFN_WOW64DISABLE	fnWow64Disable	 = (LPFN_WOW64DISABLE)GetProcAddress(GetModuleHandle(_T("kernel32")),"Wow64DisableWow64FsRedirection");
LPFN_WOW64REVERT	fnWow64Revert	 = (LPFN_WOW64REVERT)GetProcAddress(GetModuleHandle(_T("kernel32")),"Wow64RevertWow64FsRedirection");

//Purpose: Return TRUE if we are running in WOW64 (i.e. a 32bit process on XP x64 edition)
BOOL _stdcall IsXP64Bit()
{
#ifdef _M_X64
	return TRUE;	//Urrr if its a x64 build of the DLL, we MUST be running on X64 nativly!
#endif

    BOOL bIsWow64 = FALSE;
     if (NULL != fnIsWow64Process)
    {
        if (!fnIsWow64Process(GetCurrentProcess(),&bIsWow64))
        {
            // handle error
        }
    }
    return bIsWow64;
}

BOOL DisableWOW64(PVOID* oldValue)
{
#ifdef _M_X64
	return TRUE;		// If were 64b under x64, we dont wanna do anything!
#endif
	return fnWow64Disable(oldValue);
}

BOOL RevertWOW64(PVOID* oldValue)
{
#ifdef _M_X64
	return TRUE;		// If were 64b under x64, we dont wanna do anything!
#endif
	return fnWow64Revert (oldValue);
}

int SystemVersion()
{   
	OSVERSIONINFOEX osvi;
	BOOL bOsVersionInfoEx;

	ZeroMemory(&osvi, sizeof(OSVERSIONINFOEX));
	osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);

	if( !(bOsVersionInfoEx = GetVersionEx ((OSVERSIONINFO *) &osvi)) )
	{
		osvi.dwOSVersionInfoSize = sizeof (OSVERSIONINFO);

		if (! GetVersionEx ( (OSVERSIONINFO *) &osvi) )
			return 0;  
	}

	switch (osvi.dwPlatformId)
	{      
	case VER_PLATFORM_WIN32_NT:

		return 2;		//WINNT

		break;

	case VER_PLATFORM_WIN32_WINDOWS:

		return 1;		//WIN9X

		break;

	}   
	return 0; 
}
