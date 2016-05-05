OneGet Experimental Build.
---------------------------

Unpack into a folder somewhere. 

DO NOT OVERWRITE A ONEGET MODULE INSTALLED WITH WMF 5.

Run the RunToUnblock.cmd script to unblock the files. 
This also sets the executionpolicy to unrestricted. 

Open a powershell session and import the module using the path:

	PS > ipmo c:\path\to\module\oneget.psd1 


Release Notes
---------------------------
This build has the return of the Chocolatey Provider (Yay!)
It's still experimental, but is getting better every day.

The NuGet provider is hidden by default, you can access it using 
	-provider NuGet
on most commands.

There is also an MSI provider, which will install and uninstall apps using 
MSI files.

The ARP provider can show applications listed in the ADD/REMOVE programs. Does
not yet uninstall.

It's now built using .NET 4.0 -- this may run on Windows 7 
and Server 2008 R2. on WMF 3. Please test it :D


Changelog
--------------
==LOG==


Not Functional Yet:
---------------------------
	
	The MSU provider is not yet complete
	The VSIX provider is not yet complete
	The SWIDTAG provider is not yet complete
