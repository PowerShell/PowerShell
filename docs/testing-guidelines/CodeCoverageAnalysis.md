# Code coverage analysis for commit [2ae5d07](https://codecov.io/gh/PowerShell/PowerShell/tree/c7b959bd6e5356fbbd395f22ba0c6cba49f354f6/src)

Code coverage runs are enabled on daily Windows builds for PowerShell 6.0. The results of the latest build are available at: [CodeCov.io](https://codecov.io/gh/PowerShell/PowerShell)

The goal of this analysis is to find the hot spots of missing coverage. The metrics used for selection of these hot spots were: # missing lines and likelihood of code path usage.

## Coverage Status

The following table shows the status for the above commit, dated 06/18/2017

| Assembly | Hit % |
| -------- |:-----:|
| Microsoft.PowerShell.Commands.Diagnostics | 58.01% |
| Microsoft.PowerShell.Commands.Management  | 32.02% |
| Microsoft.PowerShell.Commands.Utility | 67.55% | 
| Microsoft.PowerShell.ConsoleHost | 41.15% |
| Microsoft.PowerShell.CoreCLR.AssemblyLoadContext | 97.65% |
| Microsoft.PowerShell.CoreCLR.Eventing | 29.91% |
| Microsoft.PowerShell.LocalAccounts | 86.35% |
| Microsoft.PowerShell.PSReadLine | 10.18% |
| Microsoft.PowerShell.Security | 44.44% |
| Microsoft.WSMan.Management | 4.91% |
| System.Management.Automation | 50.42% |
| Microsoft.WSMan.Runtime/WSManSessionOption.cs | 100% |
| powershell/Program.cs | 100% |

## Hot Spots with missing coverage

### Microsoft.PowerShell.Commands.Management

- CDXML cmdlet coverage. It is 0% as CDXML is broken due to CoreCLR [issue](https://github.com/dotnet/corefx/issues/18877). 
- Add tests for *-Computer cmdlets
- Add tests for *-Service cmdlets
- Add tests for *-Item cmdlets. Especially for literal paths and error cases.
- Add tests for Get-Content -Tail.
- Lots of resource strings not covered. Will probably get covered when coverage is added for error cases.

### Microsoft.PowerShell.Commands.Utility

- Add tests for Trace-Command. Especially Trace-Command -Expression
- Add tests for ConvertTo-XML serialization of PSObjects
- Add tests for Debug-Runspace
- Add tests for New-Object for ArgumentList, ComObject

### Microsoft.PowerShell.ConsoleHost

- Various options, DebugHandler and hosting modes like server, namedpipe etc.

### Microsoft.PowerShell.CoreCLR.Eventing

- Add tests for ETW events.

### Microsoft.PowerShell.PSReadLine

- We need tests from PSReadline repo or ignore coverage data for this module.

### Microsoft.PowerShell.Security

- Add tests for *-Acl cmdlets.
- Add tests for *-AuthenticodeSignature cmdlets.
- Add coverage to various utility methods under src/Microsoft.PowerShell.Security/security/Utils.cs

### Microsoft.WSMan.Management

- Add tests for WSMan provider
- Add tests for WSMan cmdlets
- Add tests for CredSSP

### System.Management.Automation

#### CoreCLR

- Lots of non-windows code can be ifdef'ed out.

#### CIMSupport

- Missing coverage possibly due to: CoreCLR [issue](https://github.com/dotnet/corefx/issues/18877).

#### Engine

- Add tests for COM.
- Add tests for Tab Completion of various types of input.
- Add tests for Import-Module / Get-Module over PSRP and CIMSession.
- Add tests for debugging PS Jobs.
- Add test for -is, -isnot, -contains, -notcontains and -like operators.
- Remove Snapin code from CommandDiscovery
- Add tests SessionStateItem, SessionStateContainer error cases, dynamic parameters. Coverage possibly added by *-Item, *-ChildItem error case tests.
- Add tests for Get-Command -ShowCommandInfo
- Add tests for Proxy Commands
- Add more tests using PSCredential

#### Remoting

- Can PSProxyJobs be removed as it is for Workflows?
- Add more tests for PS Jobs.
- Add more tests using -ThrottleLimit
- Add tests for Register-PSSessionConfiguration
- Add tests for Connect/Disconnect session
- Add more tests for Start-Job's various options

#### HelpSystem

- Add tests for Alias help
- Add tests for Class help
- Add tests for SaveHelp
- Add tests for HelpProviderWithCache
- HelpProviderWithFullCache, potential dead code.

#### Security

- Add more tests under various ExecutionPolicy modes.

#### Utils

- Add more error case test to improve coverage of src/System.Management.Automation/utils

#### Providers

##### FileSystemProvider

- Add tests for Mapped Network Drive
- Add tests for *-Item alternate stream
- Add tests for Get-ChildItem -path "file"
- Add tests for Rename-Item for a directory
- Add tests for Copy-Item over remote session
- Add tests for various error conditions

##### RegistryProvider

- Add tests for *-Item
- Add tests for *-Acl
- Add tests for error conditions

##### FunctionProvider

- Add *-Item tests