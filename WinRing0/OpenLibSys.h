//-----------------------------------------------------------------------------
//     Author : hiyohiyo
//       Mail : hiyohiyo@crystalmark.info
//        Web : http://openlibsys.org/
//    License : The modified BSD license
//
//                     Copyright 2007-2008 OpenLibSys.org. All rights reserved.
//-----------------------------------------------------------------------------

#include <ntddk.h>
#include <devioctl.h>
#include "OlsIoctl.h"

//-----------------------------------------------------------------------------
//
// Device Name
//
//-----------------------------------------------------------------------------

#define NT_DEVICE_NAME	L"\\Device\\WinRing0_1_2_0"
#define DOS_DEVICE_NAME	L"\\DosDevices\\WinRing0_1_2_0"

//-----------------------------------------------------------------------------
//
// Function Prototypes
//
//-----------------------------------------------------------------------------

NTSTATUS	DriverEntry(
				IN PDRIVER_OBJECT DriverObject,
				IN PUNICODE_STRING RegistryPath
			);

NTSTATUS	OlsDispatch(
				IN PDEVICE_OBJECT pDO,
				IN PIRP pIrp
			);

VOID		Unload(
				IN PDRIVER_OBJECT DriverObject
			);

//-----------------------------------------------------------------------------
//
// Function Prototypes for Control Code
//
//-----------------------------------------------------------------------------

NTSTATUS	ReadMsr(
				void *lpInBuffer, 
				ULONG nInBufferSize, 
				void *lpOutBuffer, 
				ULONG nOutBufferSize, 
				ULONG *lpBytesReturned
			);

NTSTATUS	WriteMsr(
				void *lpInBuffer, 
				ULONG nInBufferSize, 
				void *lpOutBuffer, 
				ULONG nOutBufferSize, 
				ULONG *lpBytesReturned
			);
			
NTSTATUS	ReadPmc(
				void *lpInBuffer, 
				ULONG nInBufferSize, 
				void *lpOutBuffer, 
				ULONG nOutBufferSize, 
				ULONG *lpBytesReturned
			);

NTSTATUS	ReadIoPort(
				ULONG ioControlCode,
				void *lpInBuffer, 
				ULONG nInBufferSize, 
				void *lpOutBuffer, 
				ULONG nOutBufferSize, 
				ULONG *lpBytesReturned
			);

NTSTATUS	WriteIoPort(
				ULONG ioControlCode,
				void *lpInBuffer, 
				ULONG nInBufferSize, 
				void *lpOutBuffer, 
				ULONG nOutBufferSize, 
				ULONG *lpBytesReturned
			);

NTSTATUS	ReadPciConfig(
				void *lpInBuffer, 
				ULONG nInBufferSize, 
				void *lpOutBuffer, 
				ULONG nOutBufferSize, 
				ULONG *lpBytesReturned
			);

NTSTATUS	WritePciConfig(
				void *lpInBuffer, 
				ULONG nInBufferSize, 
				void *lpOutBuffer, 
				ULONG nOutBufferSize, 
				ULONG *lpBytesReturned
			);
			
NTSTATUS	ReadMemory(
				void *lpInBuffer, 
				ULONG nInBufferSize, 
				void *lpOutBuffer, 
				ULONG nOutBufferSize, 
				ULONG *lpBytesReturned
			);

NTSTATUS	WriteMemory(
				void *lpInBuffer, 
				ULONG nInBufferSize, 
				void *lpOutBuffer, 
				ULONG nOutBufferSize, 
				ULONG *lpBytesReturned
			);


//-----------------------------------------------------------------------------
//
// Support Function Prototypes
//
//-----------------------------------------------------------------------------

NTSTATUS pciConfigRead(ULONG pciAddress, ULONG offset, void *data, int length);
NTSTATUS pciConfigWrite(ULONG pciAddress, ULONG offset, void *data, int length);
