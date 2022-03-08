# PowerShell NamedPipeConnection module

This module implements a PowerShell custom remote connection based on named pipes.
It will connect to any local process that is running PowerShell which has a named pipe listener.  

This module is intended for use in PowerShell testing, and as an example of how to use the new publicly available PowerShell APIs for creating custom connection and transport objects as outlined in this [RFC document](https://github.com/PowerShell/PowerShell-RFC/blob/master/Archive/Draft/RFC0063-Custom-Remote-Connections.md).

## Supported PowerShell versions

This module only works with PowerShell version 7.3 and greater.  

## New-NamedPipeSession

This command attempts to connect to a local process running PowerShell via a provided process Id, and return a `PSSession` object.
The `PSSession` object can be used with PowerShell core cmdlets to enter into an interactive session or invoke commands and script on the remote session.

```powershell
Import-Module -Name Microsoft.PowerShell.NamedPipeConnection
$session = New-NamedPipeSession -ProcessId 27536 -ConnectingTimeout 10 -Name MyConnect
$session

 Id Name            Transport ComputerName    ComputerType    State         ConfigurationName     Availability
 -- ----            --------- ------------    ------------    -----         -----------------     ------------
  1 MyConnect       PSNPTest  LocalMachine:2… RemoteMachine   Opened                                 Available

Enter-PSSession -Session $session
[LocalMachine:27536]: PS C:\> $PID
27536
[LocalMachine:27536]: PS C:\>
```
