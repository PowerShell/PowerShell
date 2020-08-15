# Creating a cross-platform binary module with the .NET Core command-line interface tools

This example uses the [.NET Core command-line interface tools][dotnet-cli] (aka
`dotnet` CLI) to demonstrate how to create a binary module that is portable across operating
systems supported by **PowerShell Core** as well as **Windows PowerShell** version 3 and higher.

Because the binary module's assembly will be created as a .NET Standard 2.0 class library,
the same assembly can be imported into both PowerShell Core and Windows PowerShell.
This means you do not have to build and distribute separate assemblies that target these two
different implementations of PowerShell.

## Prerequisites

* PowerShell Core and/or Windows PowerShell

  For this example, you can use any operating system that is supported by PowerShell Core.
  To see if your operating system is supported and to get instructions on how to install
  PowerShell Core on your operating system, see the [Get PowerShell][pscore-os] topic in
  the PowerShell repo's [README.md][readme] file.

  Note: On Windows 10 Anniversary Update or higher, you can use the [Windows Subsystem for
  Linux][wsl] (WSL) console to build the module. In order to import and use the module, you'll need
  to install PowerShell Core for the distribution and version of Linux you're running.
  You can get that version info by running the command `lsb_release -a` from the WSL console.

* .NET Core 2.x SDK

  Download and install the [.NET Core 2.x SDK][net-core-sdk] for your operating system.
  It is recommended that you use a package manager to install the SDK on Linux.
  See these [instructions][linux-install] on how to install the SDK on Linux.
  Be sure to pick your distribution of Linux e.g. RHEL, Debian, etc to get the
  appropriate instructions for your platform.

## Create the .NET Standard 2.0 Binary Module

1. Verify you are running the 2.0.0 version of the `dotnet` CLI.

   ```powershell
   dotnet --version
   ```

   This should output `2.0.0` or higher. If it returns a major version of 1, make sure you have
   installed the .NET Core 2.x SDK and have restarted your shell to get the newer version of
   the SDK tools.

1. Use the `dotnet` CLI to create a starter `classlib` project based on .NET Standard 2.0
   (the default for classlib projects).

   ```powershell
   dotnet new classlib --name MyModule
   ```

1. Add a `global.json` file that specifies that the project requires the `2.0.0` version of
   the .NET Core SDK.  This is necessary to prevent issues if you have more than one
   version of the .NET Core SDK installed.

   ```powershell
   cd MyModule
   dotnet new globaljson --sdk-version 2.0.0
   ```

1. Add the [PowerShell Standard Library][ps-stdlib] package to the project file.
   This package provides the `System.Management.Automation` assembly.

   Note: As newer versions of this library are released, update the version number
   in this command to match the latest version.

   ```powershell
   dotnet add package PowerShellStandard.Library --version 3.0.0-preview-01
   ```

1. Add source code for a simple PowerShell command to the `Class1.cs` file by opening
   that file in an editor and replacing the existing code with the following code.

   ```csharp
   using System;
   using System.Management.Automation;

   namespace MyModule
   {
       [Cmdlet(VerbsCommunications.Write, "TimestampedMessage")]
       public class WriteTimestampedMessageCommand : PSCmdlet
       {
           [Parameter(Position=1)]
           public string Message { get; set; } = string.Empty;

           protected override void EndProcessing()
           {
               string timestamp = DateTime.Now.ToString("u");
               this.WriteObject($"[{timestamp}] - {this.Message}");
               base.EndProcessing();
           }
       }
   }
   ```

1. Build the project.

   ```powershell
   dotnet build
   ```

1. Import the binary module and invoke the new command.

   Note: The previous steps could have been performed in a different shell such as
   Bash if you're on Linux.  For this step, make sure you are running PowerShell Core.

   ```powershell
   cd 'bin/Debug/netstandard2.0'
   Import-Module ./MyModule.dll
   Write-TimestampedMessage "Test message."
   ```

## Using a .NET Standard 2.0 based binary module in Windows PowerShell

You may have heard that a .NET assembly compiled as a .NET Standard 2.0 class library
will load into both .NET Core 2.x applications such as PowerShell Core and
.NET Framework 4.6.1 (or higher) applications such as Windows PowerShell.
This allows you to build a single, cross-platform binary module.

Unfortunately, this works best when the .NET Framework application, in this case
Windows PowerShell, has either been compiled against a .NET Standard 2.0 library or with
support declared for .NET Standard libraries.  In which case, the build system can provide the
appropriate binding redirects and facade and shim assemblies so that the .NET Standard 2.0
library can find the .NET Framework types it needs within the context of the running
application.

Fortunately, this has been fixed in .NET Framework 4.7.1 and in the Windows 10 Fall
Creators Update. This version of the .NET Framework allows existing applications to
"just work" without the need to modify and/or re-compile them. On these systems, a
.NET Standard 2.0 based binary module will work in Windows PowerShell.

However, for Windows systems that have not been updated to .NET Framework 4.7.1 such a
binary module will not run correctly in Windows PowerShell.

Let's see what happens when you attempt to use this module in **Windows PowerShell** on
Windows 10 CU (1703 or lower) without .NET Framework 4.7.1 installed.

1. Copy `MyModule.dll` to a folder on a Windows machine.

1. Import the module.

   ```powershell
   Import-Module .\MyModule.dll
   ```

   Note: The module should import without errors.

1. Execute the `Write-TimestampedMessage` command.

   ```powershell
   Write-TimestampedMessage "Test message."
   ```

   This will result in the following error:

   ```text
   Write-TimestampedMessage : Could not load file or assembly 'netstandard, Version=2.0.0.0, Culture=neutral,
   PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The system cannot find the file specified.
   At line:1 char:1
   + Write-TimestampedMessage "Test message."
   + ~~~~~~~~~~~~~~~~~~~~~~~~
       + CategoryInfo          : NotSpecified: (:) [], FileNotFoundException
       + FullyQualifiedErrorId : System.IO.FileNotFoundException
   ```

If the command worked, congratulations! Your system was probably updated to
.NET Framework 4.7.1.  Otherwise, this error indicates that the `MyModule.dll` assembly
can't find the `netstandard.dll` "implementation" assembly for the version of the
.NET Framework that Windows PowerShell is using.

### The fix for missing netstandard.dll

If you install (or already have) the .NET Core SDK for Windows, you can
find the `netstandard.dll` implementation assembly for .NET 4.6.1 in the following directory:
`C:\Program Files\dotnet\sdk\<version-number>\Microsoft\Microsoft.NET.Build.Extensions\net461\lib`.
Note that, the version number in the path may vary depending on the installed SDK.

If you copy `netstandard.dll` from this directory to the directory containing
`MyModule.dll`, the `Write-TimestampedMessage` command will work.  Let's try that.

1. Install [.NET Core SDK for Windows][net-core-sdk], if it isn't already installed.

1. Start a new Windows PowerShell console. Remember that once a binary assembly is
   loaded into PowerShell it can't be unloaded. Restarting PowerShell is necessary to
   get it to reload `MyModule.dll`.

1. Copy the `netstandard.dll` implementation assembly for .NET 4.6.1 to the module's directory.

   ```powershell
   cd 'path-to-where-you-copied-module.dll'
   Copy-Item 'C:\Program Files\dotnet\sdk\<version-number>\Microsoft\Microsoft.NET.Build.Extensions\net461\lib\netstandard.dll' .
   ```

1. Import the module and execute the command:

   ```powershell
   Import-Module .\MyModule.dll
   Write-TimestampedMessage "Test message."
   ```

   Now the command should succeed.

   Note: If it fails, restart Windows PowerShell to make sure
   you don't have a previously loaded version of the assembly in the session and repeat
   step 4.

If you use additional libraries there may be more work involved. This approach has
been successfully tested using types from `System.Xml` and `System.Web`.

## Wrap-up

In a few steps, we have built a PowerShell binary module using a .NET Standard 2.0
class library that will run in PowerShell Core on multiple operating systems.
It will also run in Windows PowerShell on Windows systems that have been updated to
.NET Framework 4.7.1 as well as the Windows 10 Fall Creators Update which comes with that
version pre-installed.  Furthermore, this binary module can be built on Linux
and macOS as well as Windows using the .NET Core 2.x SDK command-line tools.

For more information on .NET Standard, check out the [documentation][net-std-docs]
and the [.NET Standard YouTube channel][net-std-chan].

[dotnet-cli]:    https://docs.microsoft.com/dotnet/core/tools/
[net-core-sdk]:  https://www.microsoft.com/net/download/core
[net-std-docs]:  https://docs.microsoft.com/dotnet/standard/net-standard
[net-std-chan]:  https://www.youtube.com/playlist?list=PLRAdsfhKI4OWx321A_pr-7HhRNk7wOLLY
[pscore-os]:     https://github.com/powershell/powershell#get-powershell
[readme]:        ../../README.md
[linux-install]: https://www.microsoft.com/net/core#linuxubuntu
[ps-stdlib]:     https://www.nuget.org/packages/PowerShellStandard.Library/
[wsl]:           https://msdn.microsoft.com/commandline/wsl/about
