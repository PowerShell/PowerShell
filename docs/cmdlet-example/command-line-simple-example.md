# Creating a cross-platform binary module with the .NET Core command-line interface tools

This example uses the [.NET Core command-line interface tools][dotnet-cli], a.k.a the
`dotnet CLI`, to demonstrate how to create a binary module that is portable across operating
systems supported by `PowerShell Core` as well as `Windows PowerShell` version 3 and higher.

Because the binary module's assembly will be created as a `.NET Standard 2.0 class library`,
the same assembly can be imported into `Windows PowerShell` as well as `PowerShell Core`.
This means you do not have to build separate assemblies to target these two different
implementations of PowerShell.

## Prerequisites
* PowerShell Core and/or Windows PowerShell

  For this example, you can use any operating system that is supported by PowerShell Core.
  To see if your operating system is supported and to get instructions on how to install
  PowerShell Core on your operating system, see the [Get PowerShell][pscore-os] topic in
  the PowerShell repo's [README.md][readme] file.

  Note: On Windows 10 Anniversary Update or higher, you can use the [Windows Subsystem for
  Linux][wsl] (WSL) console to build the module. In order to import and use the module, you'll need
  to install `PowerShell Core` for the version of `Ubuntu` you're running.  You can get that
  version info by running the command `lsb_release -a` from the WSL console.

* .NET Core 2.0 SDK

  Download and install the [.NET Core 2.0 SDK][net-core-sdk] for your operating system.
  It is recommended that you use a package manager to install the SDK on Linux.
  See these [instructions][linux-install] on how to install the SDK on Linux.
  Be sure to pick your distribution of Linux e.g. RHEL, Debian, etc so you get the
  appropriate instructions for your platform.

## Create the .NET Standard 2.0 Binary Module

1. Verify you are running the 2.0.0 version of the dotnet CLI.

   ```
   dotnet --version
   ```

   This should output `2.0.0` or higher. If it returns 1.x.x, make sure you have installed
   the .NET Core 2.0 SDK and have re-started your shell to get this newer version of the SDK tools.

2. Use the `dotnet CLI` to create a starter `classlib` project based on `.NET Standard 2.0`
   (the default for classlib projects).

   ```powershell
   dotnet new classlib --name mymodule
   ```

3. Add a `global.json` file that specifies that the project requires the `2.0.0` version of
   the `.NET Core SDK`.  This is necessary to prevent issues if you have more than one
   version of the .NET Core SDK installed.

   ```
   cd mymodule
   dotnet new globaljson --sdk-version 2.0.0
   ```

4. Add the [PowerShell Standard Library][ps-stdlib] package to the project file.
   This package provides the `System.Management.Automation` assembly.

   Note: as newer versions of this library are released, update the version number
   in this command to match the latest version.

   ```
   dotnet add package PowerShellStandard.Library --version 3.0.0-preview-01
   ```

5. Add source code for a simple PowerShell command to the `Class1.cs` file by opening
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

6. Build the project.

   ```powershell
   dotnet build
   ```

7. Load the binary and invoke the new command.

   Note: the previous steps could have been performed in a different shell such as
   Bash if you're on Linux.  For this step, make sure you are running PowerShell Core.

   ```powershell
   cd 'bin/Debug/netstandard2.0'
   Import-Module ./mymodule.dll
   Write-TimestampedMessage "Test message."
   ```

## Running a .NET Standard 2.0 based module in Windows PowerShell
You may have heard that .NET class libraries compiled as a `.NET Standard 2.0 class
library` will load into both `.NET Core 2.0` applications such as PowerShell Core
and `.NET Framework 4.6.1` (or higher) applications such as Windows PowerShell.
This allows you to build a single, cross-platform binary module.

Unfortunately, this works best when the .NET Framework application has been compiled
against the .NET Standard 2.0 library.  The compiler can provide the appropriate
facade/shim assemblies so the library can find the required .NET Framework types.

For **existing** `Windows PowerShell` installations, this means your portable
binary module needs help to run correctly in Windows PowerShell.

First, let's see what happens when you use this module in `Windows PowerShell`.

1. Copy `mymodule.dll` to a folder on a Windows machine.

2. Import the module.

   ```powershell
   Import-Module .\mymodule.dll
   ```

   Note: the module should import without errors.

3. Execute the `Write-TimestampedMessage` command.

   ```powershell
   Write-TimestampedMessage "Test message."
   ```

   This will result in the following error:

   ```
   Write-TimestampedMessage : Could not load file or assembly 'netstandard, Version=2.0.0.0, Culture=neutral,
   PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The system cannot find the file specified.
   At line:1 char:1
   + Write-TimestampedMessage "Test message."
   + ~~~~~~~~~~~~~~~~~~~~~~~~
       + CategoryInfo          : NotSpecified: (:) [], FileNotFoundException
       + FullyQualifiedErrorId : System.IO.FileNotFoundException
   ```

This error indicates that the `mymodule.dll` assembly can't find the `netstandard.dll`
"implementation" assembly for the version of the `.NET Framework` that
Windows PowerShell is using.

If you install (or already have) the `.NET Core 2.0 SDK for Windows`, you can
find the `netstandard.dll` implementation assembly for .NET 4.6.1 in the following directory:
`C:\Program Files\dotnet\sdk\2.0.0\Microsoft\Microsoft.NET.Build.Extensions\net461\lib`.

If you copy `netstandard.dll` from this directory to the directory containing
`mymodule.dll`, the `Write-TimestampedMessage` command will work.  Let's try that.

1. Install the [.NET Core SDK 2.0 for Windows][net-core-sdk], if it isn't already installed.

2. Start a new `Windows PowerShell` console. Remember that once a binary assembly is
   loaded into PowerShell it can't be unloaded. Restarting PowerShell is necessary to
   get it to reload `mymodule.dll`.

3. Copy the `netstandard.dll` implementation assembly for .NET 4.6.1 to the module's directory.
   ```powershell
   cd 'path-to-where-you-copied-module.dll'
   Copy-Item 'C:\Program Files\dotnet\sdk\2.0.0\Microsoft\Microsoft.NET.Build.Extensions\net461\lib\netstandard.dll' .
   ```

4. Import the module and execute the command:
   ```powershell
   Import-Module .\mymodule.dll
   Write-TimestampedMessage "Test message."
   ```
   Now the command should succeed.

If you use additional libraries there may be more work involved. This approach has
been successfully tested using types from `System.Xml` and `System.Web`.

## .NET 4.7.1
Early testing indicates that Windows systems with `.NET 4.7.1` installed or Windows 10
Fall Creators Update and higher, will load the binary module without the need to have
`netstandard.dll` copied beside the module's assembly.

[dotnet-cli]:    https://docs.microsoft.com/en-us/dotnet/core/tools/?tabs=netcore2x
[net-core-sdk]:  https://www.microsoft.com/net/download/core
[pscore-os]:     https://github.com/powershell/powershell#get-powershell
[readme]:        ../../README.md
[linux-install]: https://www.microsoft.com/net/core#linuxubuntu
[ps-stdlib]:     https://www.nuget.org/packages/PowerShellStandard.Library/
[wsl]:           https://msdn.microsoft.com/commandline/wsl/about
