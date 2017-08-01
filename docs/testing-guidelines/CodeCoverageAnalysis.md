# Code coverage analysis for commit [2ae5d07](https://codecov.io/gh/PowerShell/PowerShell/tree/c7b959bd6e5356fbbd395f22ba0c6cba49f354f6/src)

Code coverage runs are enabled on daily Windows builds for PowerShell Core 6.0.
The results of the latest build are available at: [CodeCov.io](https://codecov.io/gh/PowerShell/PowerShell)

The goal of this analysis is to find the hot spots of missing coverage.
The metrics used for selection of these hot spots were: # missing lines and likelihood of code path usage.

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

- [ ] CDXML cmdlet coverage. It is 0% as CDXML is broken due to CoreCLR [issue](https://github.com/dotnet/corefx/issues/18877). 
- [ ] Add tests for *-Computer cmdlets [#4146](https://github.com/PowerShell/PowerShell/issues/4146)
- [ ] Add tests for *-Service cmdlets [#4147](https://github.com/PowerShell/PowerShell/issues/4147)
- [ ] Add tests for *-Item cmdlets. Especially for literal paths and error cases. [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Add tests for Get-Content -Tail. [#4150](https://github.com/PowerShell/PowerShell/issues/4150)
- [ ] Lots of resource strings not covered. Will probably get covered when coverage is added for error cases. [#4148](https://github.com/PowerShell/PowerShell/issues/4148)

### Microsoft.PowerShell.Commands.Utility

- [ ] Add tests for Trace-Command. Especially Trace-Command -Expression [#4151](https://github.com/PowerShell/PowerShell/issues/4151)
- [ ] Add tests for ConvertTo-XML serialization of PSObjects [#4152](https://github.com/PowerShell/PowerShell/issues/4152)
- [ ] Add tests for Debug-Runspace [#4153](https://github.com/PowerShell/PowerShell/issues/4153)
- [ ] Add tests for New-Object for ArgumentList, ComObject [#4154](https://github.com/PowerShell/PowerShell/issues/4154)

### Microsoft.PowerShell.ConsoleHost

- [ ] Various options, DebugHandler and hosting modes like server, namedpipe etc. [#4155](https://github.com/PowerShell/PowerShell/issues/4155)

### Microsoft.PowerShell.CoreCLR.Eventing

- [ ] Add tests for ETW events. [#4156](https://github.com/PowerShell/PowerShell/issues/4156)

### Microsoft.PowerShell.PSReadLine

- [ ] We need tests from PSReadline repo or ignore coverage data for this module. (This will be filtered out.)

### Microsoft.PowerShell.Security

- [ ] Add tests for *-Acl cmdlets. [4157] (https://github.com/PowerShell/PowerShell/issues/4157)
- [ ] Add tests for *-AuthenticodeSignature cmdlets. [4157] (https://github.com/PowerShell/PowerShell/issues/4157)
- [ ] Add coverage to various utility methods under src/Microsoft.PowerShell.Security/security/Utils.cs [4157] (https://github.com/PowerShell/PowerShell/issues/4157)

### Microsoft.WSMan.Management

- [ ] Add tests for WSMan provider [#4158](https://github.com/PowerShell/PowerShell/issues/4158)
- [ ] Add tests for WSMan cmdlets [#4158](https://github.com/PowerShell/PowerShell/issues/4158)
- [ ] Add tests for CredSSP [#4158](https://github.com/PowerShell/PowerShell/issues/4158)

### System.Management.Automation

#### CoreCLR

- [ ] Lots of non-windows code can be ifdef'ed out. Issue #[3565](https://github.com/PowerShell/PowerShell/issues/3565)

#### CIMSupport

- [ ] Missing coverage possibly due to: CoreCLR [issue](https://github.com/dotnet/corefx/issues/18877).
[4159](https://github.com/PowerShell/PowerShell/issues/4159)

#### Engine

- [ ] Add tests for COM. [#4154](https://github.com/PowerShell/PowerShell/issues/4154)
- [ ] Add tests for Tab Completion of various types of input. [#4160](https://github.com/PowerShell/PowerShell/issues/4160)
- [ ] Add tests for Import-Module / Get-Module over PSRP and CIMSession. [#4161](https://github.com/PowerShell/PowerShell/issues/4161)
- [ ] Add tests for debugging PS Jobs.[#4153](https://github.com/PowerShell/PowerShell/issues/4153)
- [ ] Add test for -is, -isnot, -contains, -notcontains and -like operators.[#4162](https://github.com/PowerShell/PowerShell/issues/4162)
- [ ] Remove Snapin code from CommandDiscovery. Issue #[4118](https://github.com/PowerShell/PowerShell/issues/4118)
- [ ] Add tests SessionStateItem, SessionStateContainer error cases, dynamic parameters. Coverage possibly added by *-Item, *-ChildItem error case tests. [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Add tests for Get-Command -ShowCommandInfo [#4163](https://github.com/PowerShell/PowerShell/issues/4163)
- [ ] Add tests for Proxy Commands [#4164](https://github.com/PowerShell/PowerShell/issues/4164)
- [ ] Add more tests using PSCredential [#4165](https://github.com/PowerShell/PowerShell/issues/4165)

#### Remoting

- [ ] Can PSProxyJobs be removed as it is for Workflows?
- [ ] Add more tests for PS Jobs. [#4166](https://github.com/PowerShell/PowerShell/issues/4166)
- [ ] Add more tests using -ThrottleLimit [#4166](https://github.com/PowerShell/PowerShell/issues/4166)
- [ ] Add tests for Register-PSSessionConfiguration [#4166](https://github.com/PowerShell/PowerShell/issues/4166)
- [ ] Add tests for Connect/Disconnect session [#4166](https://github.com/PowerShell/PowerShell/issues/4166)
- [ ] Add more tests for Start-Job's various options [#4166](https://github.com/PowerShell/PowerShell/issues/4166)

#### HelpSystem

- [ ] Add tests for Alias help [#4167](https://github.com/PowerShell/PowerShell/issues/4167)
- [ ] Add tests for Class help [#4167](https://github.com/PowerShell/PowerShell/issues/4167)
- [ ] Add tests for SaveHelp [#4167](https://github.com/PowerShell/PowerShell/issues/4167)
- [ ] Add tests for HelpProviderWithCache [#4167](https://github.com/PowerShell/PowerShell/issues/4167)
- [ ] HelpProviderWithFullCache, potential dead code. [#4167](https://github.com/PowerShell/PowerShell/issues/4167)

#### Security

- [ ] Add more tests under various ExecutionPolicy modes. [4168](https://github.com/PowerShell/PowerShell/issues/4168)

#### Utils

- [ ] Add more error case test to improve coverage of src/System.Management.Automation/utils [#4169](https://github.com/PowerShell/PowerShell/issues/4169)

#### Providers

##### FileSystemProvider

- [ ] Add tests for Mapped Network Drive [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Add tests for *-Item alternate stream [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Add tests for Get-ChildItem -path "file" [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Add tests for Rename-Item for a directory [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Add tests for Copy-Item over remote session [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Add tests for various error conditions [#4148](https://github.com/PowerShell/PowerShell/issues/4148)

##### RegistryProvider

- [ ] Add tests for *-Item [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Add tests for *-Acl [#4157](https://github.com/PowerShell/PowerShell/issues/4157)
- [ ] Add tests for error conditions [#4148](https://github.com/PowerShell/PowerShell/issues/4148)

##### FunctionProvider

- [ ] Add *-Item tests [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
