InpOut32Drv Driver Interface DLL
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Modified for x64 compatibility and built by Phillip Gibbons (Phil@highrez.co.uk).
See http://www.highrez.co.uk/Downloads/InpOut32 or the Highrez Forums (http://forums.highrez.co.uk) for information.
Many thanks to Red Fox UK for supporting the community and providing Driver signatures allowing Vista/7 x64 compatibility.



Based on the original written by Logix4U (www.logix4u.net).


Notes:

	The InpOut32 device driver supports writing to "old fashioned" hardware port addresses. 
	It does NOT support USB devices such as USB Parallel ports or even PCI parallel ports (as I am lead to believe).


	The device driver is installed at runtime. To do this however needs administrator privileges.
	On Vista & later, using UAC, you can run the InstallDriver.exe in the \Win32 folder to install the driver 
	appropriate for your OS. Doing so will request elevation and ask for your permission (or for the administrator 
	password). Once the driver is installed for the first time, it can then be used by any user *without* 
	administrator privileges

