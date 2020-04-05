# Current preview release

## 7.1.0-preview.1 - 2020-03-26

### Breaking Changes

- Use invariant culture string conversion for `-replace` operator (#10954) (Thanks @iSazonov!)

### Engine Updates and Fixes

- Revert the PRs that made `DBNull.Value` and `NullString.Value` treated as `$null` (#11648)

### Experimental Features

- Use invariant culture string conversion for `-replace` operator (#10954) (Thanks @iSazonov!)

### General Cmdlet Updates and Fixes

- Fix an operator preference order issue in binder code (#12075) (Thanks @DamirAinullin!)
- Fix `NullReferenceException` when binding common parameters of type `ActionPreference` (#12124)
- Fix default formatting for deserialized `MatchInfo` (#11728) (Thanks @iSazonov!)
- Use asynchronous streams in `Invoke-RestMethod` (#11095) (Thanks @iSazonov!)
- Address UTF-8 Detection In `Get-Content -Tail` (#11899) (Thanks @NoMoreFood!)
- Handle the `IOException` in `Get-FileHash` (#11944) (Thanks @iSazonov!)
- Change 'PowerShell Core' to 'PowerShell' in a resource string (#11928) (Thanks @alexandair!)
- Bring back `MainWindowTitle` in `PSHostProcessInfo` (#11885) (Thanks @iSazonov!)
- Miscellaneous minor updates to Windows Compatibility (#11980)
- Fix `ConciseView` to split `PositionMessage` using `[Environment]::NewLine` (#12010)
- Remove network hop restriction for interactive sessions (#11920)
- Fix `NullReferenceException` in `SuspendStoppingPipeline()` and `RestoreStoppingPipeline()` (#11870) (Thanks @iSazonov!)
- Generate GUID for `FormatViewDefinition` `InstanceId` if not provided (#11896)
- Fix `ConciseView` where error message is wider than window width and doesn't have whitespace (#11880)
- Allow cross-platform `CAPI-compatible` remote key exchange (#11185) (Thanks @silijon!)
- Fix error message (#11862) (Thanks @NextTurn!)
- Fix `ConciseView` to handle case where there isn't a console to obtain the width (#11784)
- Update `CmsCommands` to use Store vs certificate provider (#11643) (Thanks @mikeTWC1984!)
- Enable `pwsh` to work on Windows systems where `mpr.dll` and STA is not available (#11748)
- Refactor and implement `Restart-Computer` for `Un*x` and macOS (#11319)
- Add an implementation of `Stop-Computer` for Linux and macOS (#11151)
- Fix `help` function to check if `less` is available before using (#11737)
- Update `PSPath` in `certificate_format_ps1.xml` (#11603) (Thanks @xtqqczze!)
- Change regular expression to match relation-types without quotes in Link header (#11711) (Thanks @Marusyk!)
- Fix error message during symbolic link deletion (#11331)
- Add custom 'Selected.*' type to `PSCustomObject` in `Select-Object` only once (#11548) (Thanks @iSazonov!)
- Add `-AsUTC` to the `Get-Date` cmdlet (#11611)
- Fix grouping behavior with Boolean values in `Format-Hex` (#11587) (Thanks @vexx32!)
- Make `Test-Connection` always use the default synchronization context for sending ping requests (#11517)
- Correct startup error messages (#11473) (Thanks @iSazonov!)
- Ignore headers with null values in web cmdlets (#11424) (Thanks @iSazonov!)
- Re-add check for `Invoke-Command` job dispose. (#11388)
- Revert "Update formatter to not write newlines if content is empty (#11193)" (#11342) (Thanks @iSazonov!)
- Allow `CompleteInput` to return results from `ArgumentCompleter` when `AST` or Script has matching function definition (#10574) (Thanks @M1kep!)
- Update formatter to not write new lines if content is empty (#11193)

### Code Cleanup

<details>

<ul>
<li>Use span-based overloads (#11884) (Thanks @iSazonov!)</li>
<li>Use new <code>string.Split()</code> overloads (#11867) (Thanks @iSazonov!)</li>
<li>Remove unreachable DSC code (#12076) (Thanks @DamirAinullin!)</li>
<li>Remove old dead code from FullCLR (#11886) (Thanks @iSazonov!)</li>
<li>Use <code>Dictionary.TryAdd()</code> where possible (#11767) (Thanks @iSazonov!)</li>
<li>Use <code>Environment.NewLine</code> instead of hard-coded linefeed in <code>ParseError.ToString</code> (#11746)</li>
<li>Fix <code>FileSystem</code> provider error message (#11741) (Thanks @iSazonov!)</li>
<li>Reformat code according to <code>EditorConfig</code> rules (#11681) (Thanks @xtqqczze!)</li>
<li>Replace use of throw <code>GetExceptionForHR</code> with <code>ThrowExceptionForHR</code> (#11640) (Thanks @xtqqczze!)</li>
<li>Refactor delegate types to lambda expressions (#11690) (Thanks @xtqqczze!)</li>
<li>Remove Unicode BOM from text files (#11546) (Thanks @xtqqczze!)</li>
<li>Fix Typo in <code>Get-ComputerInfo</code> cmdlet description (#11321) (Thanks @doctordns!)</li>
<li>Fix typo in description for <code>Get-ExperimentalFeature</code> <code>PSWindowsPowerShellCompatibility</code> (#11282) (Thanks @alvarodelvalle!)</li>
<li>Cleanups in command discovery (#10815) (Thanks @iSazonov!)</li>
<li>Review <code>currentculture</code> (#11044) (Thanks @iSazonov!)</li>
</ul>

</details>

### Tools

- Change recommended VS Code extension name from `ms-vscode.csharp` to `ms-dotnettools.csharp` (#12083) (Thanks @devlead!)
- Specify `csharp_preferred_modifier_order` in `EditorConfig` (#11775) (Thanks @xtqqczze!)
- Update `.editorconfig` (#11675) (Thanks @xtqqczze!)
- Enable `EditorConfig` support in `OmniSharp` (#11627) (Thanks @xtqqczze!)
- Specify charset in `.editorconfig` as `utf-8` (no BOM) (#11654) (Thanks @xtqqczze!)
- Configure the issue label bot (#11527)
- Avoid variable names that conflict with automatic variables (#11392) (Thanks @xtqqczze!)

### Tests

- Add empty `preview.md` file to fix broken link (#12041)
- Add helper functions for SSH remoting tests (#11955)
- Add new tests for `Get-ChildItem` for `FileSystemProvider` (#11602) (Thanks @iSazonov!)
- Ensure that types referenced by `PowerShellStandard` are present (#10634)
- Check state and report reason if it's not "opened" (#11574)
- Fixes for running tests on Raspbian (#11661)
- Unify pester test syntax for the arguments of `-BeOfType`  (#11558) (Thanks @xtqqczze!)
- Correct casing for automatic variables (#11568) (Thanks @iSazonov!)
- Avoid variable names that conflict with automatic variables part 2 (#11559) (Thanks @xtqqczze!)
- Update pester syntax to v4 (#11544) (Thanks @xtqqczze!)
- Allow error 504 (Gateway Timeout) in `markdown-link` tests (#11439) (Thanks @xtqqczze!)
- Re-balance CI tests (#11420) (Thanks @iSazonov!)
- Include URL in the markdown-links test error message (#11438) (Thanks @xtqqczze!)
- Use CIM cmdlets instead of WMI cmdlets in tests (#11423) (Thanks @xtqqczze!)

### Build and Packaging Improvements

<details>

<ul>
<li>Put symbols in separate package (#12169)</li>
<li>Disable <code>x86</code> PDB generation (#12167)</li>
<li>Bump <code>NJsonSchema</code> from <code>10.1.5</code> to <code>10.1.11</code> (#12050) (#12088) (#12166)</li>
<li>Create <code>crossgen</code> symbols for Windows <code>x64</code> and <code>x86</code> (#12157)</li>
<li>Move to <code>.NET 5 preview.1</code> (#12140)</li>
<li>Bump <code>Microsoft.CodeAnalysis.CSharp</code> from <code>3.4.0</code> to <code>3.5.0</code> (#12136)</li>
<li>Move to standard internal pool for building (#12119)</li>
<li>Fix package syncing to private Module Feed  (#11841)</li>
<li>Add Ubuntu SSH remoting tests CI (#12033)</li>
<li>Bump <code>Markdig.Signed</code> from <code>0.18.1</code> to <code>0.18.3</code> (#12078)</li>
<li>Fix MSIX packaging to determine if a Preview release by inspecting the semantic version string (#11991)</li>
<li>Ignore last exit code in the build step as <code>dotnet</code> may return error when SDK is not installed (#11972)</li>
<li>Fix daily package build (#11882)</li>
<li>Fix package sorting for syncing to private Module Feed (#11838)</li>
<li>Set <code>StrictMode</code> version <code>3.0</code> (#11563) (Thanks @xtqqczze!)</li>
<li>Bump <code>.devcontainer</code> version to dotnet <code>3.1.101</code> (#11707) (Thanks @Jawz84!)</li>
<li>Move to version 3 of <code>AzFileCopy</code> (#11697)</li>
<li>Update <code>README.md</code> and <code>metadata.json</code> for next release (#11664)</li>
<li>Code Cleanup for environment data gathering in <code>build.psm1</code> (#11572) (Thanks @xtqqczze!)</li>
<li>Update Debian Install Script To Support Debian 10 (#11540) (Thanks @RandomNoun7!)</li>
<li>Update <code>ADOPTERS.md</code> (#11261) (Thanks @edyoung!)</li>
<li>Change back to use powershell.exe in 'SetVersionVariables.yml' to unblock daily build (#11207)</li>
<li>Change to use pwsh to have consistent JSON conversion for <code>DateTime</code> (#11126)</li>
</ul>

</details>

### Documentation and Help Content

- Replace `VSCode` link in `CONTRIBUTING.md` (#11475) (Thanks @stevend811!)
- Remove the version number of PowerShell from LICENSE (#12019)
- Add the 7.0 change log link to `CHANGELOG/README.md` (#12062) (Thanks @LabhanshAgrawal!)
- Improvements to the contribution guide (#12086) (Thanks @ShaydeNofziger!)
- Update the doc about debugging dotnet core in VSCode (#11969)
- Update `README.md` and `metadata.json` for the next release (#11918) (#11992)
- Update `Adopters.md` to include info on Azure Pipelines and GitHub Actions (#11888) (Thanks @alepauly!)
- Add information about how Amazon AWS uses PowerShell. (#11365) (Thanks @bpayette!)
- Add link to .NET CLI version in build documentation (#11725) (Thanks @joeltankam!)
- Added info about DeploymentScripts in ADOPTERS.md (#11703)
- Update `CHANGELOG.md` for `6.2.4` release (#11699)
- Update `README.md` and `metadata.json` for next release (#11597)
- Update the breaking change definition (#11516)
- Adding System Frontier to the PowerShell Core adopters list `ADOPTERS.md` (#11480) (Thanks @OneScripter!)
- Update `ChangeLog`, `README.md` and `metadata.json` for `7.0.0-rc.1` release (#11363)
- Add `AzFunctions` to `ADOPTERS.md` (#11311) (Thanks @Francisco-Gamino!)
- Add `Universal Dashboard` to `ADOPTERS.md` (#11283) (Thanks @adamdriscoll!)
- Add `config.yml` for `ISSUE_TEMPLATE` so that Doc, Security, Support, and Windows PowerShell issues go to URLs (#11153)
- Add `Adopters.md` file (#11256)
- Update `Readme.md` for `preview.6` release (#11108)
- Update `SUPPORT.md` (#11101) (Thanks @mklement0!)
- Update `README.md` (#11100) (Thanks @mklement0!)
