# Creating a Simple Cross-Platform Binary Module with the Dotnet CLI

This example shows how you can create a simple, binary module using the dotnet
CLI (command line interface).  For this example, you can use any OS that the
.NET Core 2.0 SDK installs on.  The Windows for Subsystem Linux console works
well for this.

Because the module's assembly will be created as a .NET Standard 2.0 class library,
the same assembly can be loaded in Windows PowerShell as well as PowerShell Core.
This means you don't have to build separate assemblies to target these two different
implementations of PowerShell.

## Prerequisites
* Download and install the [.NET Core 2.0 SDK][net-core-sdk] for your operating system.
  On Linux, I recommend using a package manager to install the SDK.
  See these [instructions][linux-install] on how to install the SDK on Linux.

## Create the NET Standard 2.0 Binary Module
1. Use the dotnet CLI to create a starter classlib project based on .NET Standard 2.0:
   ```
   dotnet new classlib --name mymodule
   ```

2. Add the PowerShell Standard Library package. This package provides the System.Management.Automation assembly.
   ```
   cd mymodule
   dotnet add package PowerShellStandard.Library --version 3.0.0-preview-01
   ```

3. Add the source code for a PowerShell command by opening the Class1.cs file in an editor and adding the following code:
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

4. Build the project:
   ```
   dotnet build
   ```

5. Load the binary and invoke the new command:
   ```
   cd bin/Debug/netstandard2.0
   Import-Module ./mymodule.dll
   Write-TimestampedMessage "Test message"
   ```

## Running a .NET Standard 2.0 based module in Windows PowerShell
You may have heard that .NET class libraries compiled as .NET Standard 2.0 class
libraries will load into both .NET Core 2.0 applications such as PowerShell Core
and .NET Framework 4.6.1 (or higher) applications such as Windows PowerShell.
This allows you to build a single, portable binary module.

Unfortunately, this works best when the .NET Framework application has been compiled
against the .NET Standard 2.0 library.  The compiler can provide the appropriate
facade/shim assemblies so the library can find .NET Framework types.

For existing Windows PowerShell installations, this means your single, portable
binary module needs help to run correctly in Windows PowerShell.

Let's see what happens when you use your module in Windows PowerShell.
1. Copy `mymodule.dll` to a folder on a Windows machine.

2. Import the module:
   ```
   Import-Module .\mymodule.dd
   ```
   Note: the module should load without errors.

3. Execute the Write-TimestampedMessage command:
   ```
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

This error indicates that the mymodule.dll assembly can't find the implementation version of the
netstandard.dll for the .NET Framework.

If you install the .NET Core 2.0 SDK on Windows, you
can find the implementation version of netstandard.dll for .NET 4.6.1 in the following directory:
`C:\Program Files\dotnet\sdk\2.0.0\Microsoft\Microsoft.NET.Build.Extensions\net461\lib`.
If you copy the netstandard.dll from this directory to the directory containing
mymodule.dll, the `Write-TimestampedMessage` command will work.  Let's try that.

1. Start a new Windows PowerShell console. Remember that once a binary assembly is loaded into
   PowerShell it can't be unloaded. Restarting PowerShell is necessary to get it to reload mymodule.dll.

2. Copy the implementation version of netstandard.dll for .NET 4.6.1 to our module directory:
   ```
   Copy-Item 'C:\Program Files\dotnet\sdk\2.0.0\Microsoft\Microsoft.NET.Build.Extensions\net461\lib\netstandard.dll' .
   ```

3. Import the module and execute the command:
   ```
   Import-Module .\mymodule.dll
   Write-TimestampedMessage "Test message."
   ```
   This time the command should succeed.

If you use additional libraries there may be more work involved. This approach has
been successfully tested using types from System.Xml and System.Web.

## .NET 4.7.1
Early testing indicates that Windows systems with .NET 4.7.1 installed or Windows 10
Fall Creators Update and higher, will load the binary module without the need to have
netstandard.dll copied beside the module's assembly.

[net-core-sdk]: https://www.microsoft.com/net/download/core
[linux-install]: https://www.microsoft.com/net/core#linuxredhat
