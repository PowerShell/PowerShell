# Code coverage analysis for commit [de5f69c](https://codecov.io/gh/PowerShell/PowerShell/tree/de5f69cf942a85839c907f11a29cf9c09f9de8b4/src)

Code coverage runs are enabled on daily Windows builds for PowerShell Core 6.
The results of the latest build are available at [codecov.io](https://codecov.io/gh/PowerShell/PowerShell)

The goal of this analysis is to find the hot spots of missing coverage.
The metrics used for selection of these hot spots were: # missing lines and likelihood of code path usage.

## Coverage Status

The following table shows the status for the above commit, dated 2018-11-28

| Assembly | Hit % |
| -------- |:-----:|
| Microsoft.Management.Infrastructure.CimCmdlets | 48.18% |
| Microsoft.PowerShell.Commands.Diagnostics | 47.58% |
| Microsoft.PowerShell.Commands.Management | 61.06% |
| Microsoft.PowerShell.Commands.Utility | 70.76% |
| Microsoft.PowerShell.ConsoleHost | 46.39% |
| Microsoft.PowerShell.CoreCLR.Eventing | 37.84% |
| Microsoft.PowerShell.MarkdownRender | 70.68% |
| Microsoft.PowerShell.Security | 49.36% |
| Microsoft.WSMan.Management | 62.36% |
| System.Management.Automation | 63.35% |
| Microsoft.WSMan.Runtime/WSManSessionOption.cs | 100.00% |
| powershell/Program.cs | 100.00% |

## Hot Spots with missing coverage

### Microsoft.PowerShell.Commands.Management

- [ ] Add tests for *-Item cmdlets. Especially for literal paths and error cases. [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Lots of resource strings not covered. Will probably get covered when coverage is added for error cases. [#4148](https://github.com/PowerShell/PowerShell/issues/4148)

### Microsoft.PowerShell.Commands.Utility

- [ ] Add tests for Debug-Runspace [#4153](https://github.com/PowerShell/PowerShell/issues/4153)

### Microsoft.PowerShell.ConsoleHost

- [ ] Various options, DebugHandler and hosting modes like server, namedpipe etc. [#4155](https://github.com/PowerShell/PowerShell/issues/4155)

### Microsoft.PowerShell.CoreCLR.Eventing

- [ ] Add tests for ETW events. [#4156](https://github.com/PowerShell/PowerShell/issues/4156)

### Microsoft.PowerShell.Security

- [ ] Add tests for *-Acl cmdlets. [#4157](https://github.com/PowerShell/PowerShell/issues/4157)
- [ ] Add tests for *-AuthenticodeSignature cmdlets. [#4157](https://github.com/PowerShell/PowerShell/issues/4157)
- [ ] Add coverage to various utility methods under src/Microsoft.PowerShell.Security/security/Utils.cs [#4157](https://github.com/PowerShell/PowerShell/issues/4157)

### Microsoft.WSMan.Management

- [ ] Add tests for WSMan provider [#4158](https://github.com/PowerShell/PowerShell/issues/4158)
- [ ] Add tests for WSMan cmdlets [#4158](https://github.com/PowerShell/PowerShell/issues/4158)
- [ ] Add tests for CredSSP [#4158](https://github.com/PowerShell/PowerShell/issues/4158)

### System.Management.Automation

#### CoreCLR

- [ ] Lots of non-windows code can be ifdef'ed out. [#3565](https://github.com/PowerShell/PowerShell/issues/3565)

#### Engine

- [ ] Add tests for Tab Completion of various types of input. [#4160](https://github.com/PowerShell/PowerShell/issues/4160)
- [ ] Add tests for debugging PS Jobs. [#4153](https://github.com/PowerShell/PowerShell/issues/4153)
- [ ] Remove Snapin code from CommandDiscovery. [#4118](https://github.com/PowerShell/PowerShell/issues/4118)
- [ ] Add tests SessionStateItem, SessionStateContainer error cases, dynamic parameters. Coverage possibly added by *-Item, *-ChildItem error case tests. [#4148](https://github.com/PowerShell/PowerShell/issues/4148)
- [ ] Add more tests using PSCredential [#4165](https://github.com/PowerShell/PowerShell/issues/4165)

#### Remoting

- [ ] Can PSProxyJobs be removed as it is for Workflows?
- [ ] Add more tests for PS Jobs. [#4166](https://github.com/PowerShell/PowerShell/issues/4166)
- [ ] Add more tests using -ThrottleLimit [#4166](https://github.com/PowerShell/PowerShell/issues/4166)
- [ ] Add tests for Register-PSSessionConfiguration [#4166](https://github.com/PowerShell/PowerShell/issues/4166)
- [ ] Add tests for Connect/Disconnect session [#4166](https://github.com/PowerShell/PowerShell/issues/4166)
- [ ] Add more tests for Start-Job's various options [#4166](https://github.com/PowerShell/PowerShell/issues/4166)

#### Security

- [ ] Add more tests under various ExecutionPolicy modes. [#4168](https://github.com/PowerShell/PowerShell/issues/4168)

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
