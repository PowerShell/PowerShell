# Current preview release

## [7.3.0-preview.1] - 2021-12-16

### Breaking Changes

- Add `clean` block to script block as a peer to `begin`, `process`, and `end` to allow easy resource cleanup (#15177)
- Change default for `$PSStyle.OutputRendering` to `Ansi` (Internal 18449)

### Engine Updates and Fixes

- Remove duplicate remote server mediator code (#16027)
- Fix `PSVersion` parameter version checks and error messages for PowerShell 7 remoting (#16228)
- Use the same temporary home directory when `HOME` env variable is not set (#16263)
- Fix parser to generate error when array has more than 32 dimensions (#16276)

### Performance

- Avoid validation for built-in file extension and color VT sequences (#16320) (Thanks @iSazonov!)

### General Cmdlet Updates and Fixes

- Update `README.md` and `metadata.json` for next preview release (#16107)
- Use `PlainText` when writing to a host that doesn't support VT (#16092)
- Remove support for `AppExeCLinks` to retrieve target (#16044)
- Move `GetOuputString()` and `GetFormatStyleString()` to `PSHostUserInterface` as public API (#16075)
- Fix `ConvertTo-SecureString` with key regression due to .NET breaking change (#16068)
- Fix regression in `Move-Item` to only fallback to `copy and delete` in specific cases (#16029)
- Set `$?` correctly for command expression with redirections (#16046)
- Use `CurrentCulture` when handling conversions to `DateTime` in `Add-History` (#16005) (Thanks @vexx32!)
- Fix link header parsing to handle unquoted `rel` types (#15973) (Thanks @StevenLiekens!)
- Fix a casting error when using `$PSNativeCommandUsesErrorActionPreference` (#15993)
- Format-Wide: Fix `NullReferenceException` (#15990) (Thanks @DarylGraves!)
- Make the native command error handling optionally honor `ErrorActionPreference` (#15897)
- Remove declaration of experimental features in Utility module manifest as they are stable (#16460)
- Fix race condition between `DisconnectAsync` and `Dispose` (#16536) (Thanks @i3arnon!)
- Fix the `Max_PATH` condition check to handle long path correctly (#16487) (Thanks @Shriram0908!)
- Update `HelpInfoUri` for 7.2 release (#16456)
- Fix tab completion within the script block specified for the `ValidateScriptAttribute`. (#14550) (Thanks @MartinGC94!)
- Update `README.md` to specify gathered telemetry (#16379)
- Fix typo for "privacy" in MSI installer (#16407)
- Remove unneeded call to `File.ResolveLinkTarget` from `IsWindowsApplication` (#16371) (Thanks @iSazonov!)
- Add `-HttpVersion` parameter to web cmdlets (#15853) (Thanks @hayhay27!)
- Add support to web cmdlets for open-ended input tags (#16193) (Thanks @farmerau!)
- Add more tests to `Tee-Object -Encoding` (#14539) (Thanks @rpolley!)
- Don't throw exception when trying to resolve a possible link path (#16310)
- Fix `ConvertTo-Json -Depth` to allow 100 at maximum (#16197) (Thanks @KevRitchie!)
- Fix for SSH remoting when banner is enabled on SSHD endpoint (#16205)
- Disallow all COM for AppLocker system lock down (#16268)
- Configure `ApplicationInsights` to not send cloud role name (#16246)
- Disallow `Add-Type` in NoLanguage mode on a locked down machine (#16245)
- Specify the executable path as `TargetObect` for non-zero exit code `ErrorRecord` (#16108) (Thanks @rkeithhill!)
- Don't allow `Move-Item` with FileSystemProvider to move a directory into itself (#16198)
- Make property names for the color VT sequences consistent with documentations (#16212)
- Fix `PipelineVariable` to set variable in the right scope (#16199)
- Invoke-Command: improve handling of variables with $using: expression (#16113) (Thanks @dwtaber!)
- Change `Target` from a `CodeProperty` to be an `AliasProperty` that points to `FileSystemInfo.LinkTarget` (#16165)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @eltociear, @iSazonov</p>

</summary>

<ul>
<li>Improve <code>CommandInvocationIntrinsics</code> API documentation and style (#14369)</li>
<li>Use <code>bool?.GetValueOrDefault()</code> in <code>FormatWideCommand</code> (#15988) (Thanks @xtqqczze!)</li>
<li>Remove 4 assertions which cause debug build test runs to fail (#15963)</li>
<li>Fix typo in `Job.cs` (#16454) (Thanks @eltociear!)</li>
<li>Remove unnecessary call to `ToArray` (#16307) (Thanks @iSazonov!)</li>
<li>Remove the unused `FollowSymLink` function (#16231)</li>
<li>Fix typo in `TypeTable.cs` (#16220) (Thanks @eltociear!)</li>
<li>Fixes #16176 - replace snippet tag with code tag in comments (#16177)</li>
</ul>

</details>

### Tools

- Fix typo in build.psm1 (#16038) (Thanks @eltociear!)
- Add `.stylecop` to `filetypexml` and format it (#16025)
- Enable sending Teams notification when workflow fails (#15982)
- Use `Convert-Path` for unknown drive in `Build.psm1` (#16416) (Thanks @matt9ucci!)

### Tests

- Add benchmark to test compiler performance (#16083)
- Enable two previously disabled `Get-Process` tests (#15845) (Thanks @iSazonov!)
- Set clean state before testing `UseMU` in the MSI (#16543)
- Fix global tool and SDK tests in release pipeline (#16342)
- Remove the outdated test (#16269)
- Removed old not-used-anymore docker-based tests for PS release packages (#16224)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@github-actions[bot], @kondratyev-nv</p>

</summary>

<ul>
<li>fix issue with hash file getting created before we have finished get-childitem (#16170)</li>
<li>Add sha256 hashes to release (#16147)</li>
<li>Change path for Component Governance for build to the path we actually use to build (#16137)</li>
<li>Update <code>Microsoft.CodeAnalysis.CSharp</code> version (#16138)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#16070)</li>
<li>Update .NET to <code>6.0.100-rc.1.21458.32</code> (#16066)</li>
<li>Update minimum required OS version for macOS (#16088)</li>
<li>Set locale correctly on Linux CI (#16073)</li>
<li>Ensure locale is set correctly on Ubuntu 20.04 in CI (#16067)</li>
<li>Bump <code>Microsoft.CodeAnalysis.NetAnalyzers</code> (#16045)</li>
<li>Update .NET SDK version from `6.0.100-rc.1.21430.44` to `6.0.100-rc.1.21455.2` (#16041) (Thanks @github-actions[bot]!)</li>
<li>Fix the GitHub Action for updating .NET daily builds (#16042)</li>
<li>Bump Microsoft.CodeAnalysis.CSharp from 4.0.0-3.final to 4.0.0-4.21430.4 (#16036)</li>
<li>Bump .NET to `6.0.100-rc.1.21430.44` (#16028)</li>
<li>Move from <code>PkgES</code> hosted agents to 1ES hosted agents (#16023)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#16021)</li>
<li>Update Ubuntu images to use Ubuntu 20.04 (#15906)</li>
<li>Fix the mac build by updating the pool image name (#16010)</li>
<li>Use Alpine 3.12 for building PowerShell for alpine (#16008)</li>
<li>Update .NET SDK version from `6.0.100-preview.6.21355.2` to `6.0.100-rc.1.21426.1` (#15648) (Thanks @github-actions[bot]!)</li>
<li>Ignore error from <code>Find-Package</code> (#15999)</li>
<li>Find packages separately for each source in <code>UpdateDotnetRuntime.ps1</code> script (#15998)</li>
<li>Update metadata to start using .NET 6 RC1 builds (#15981)</li>
<li>Bump <code>Microsoft.CodeAnalysis.NetAnalyzers</code> (#15985)</li>
<li>Merge the v7.2.0-preview.9 release branch back to GitHub master (#15983)</li>
<li>Publish global tool package for stable releases (#15961)</li>
<li>Bump <code>Microsoft.CodeAnalysis.NetAnalyzers</code> to newer version (#15962)</li>
<li>Disabled Yarn cache in markdown.yml (#16599)</li>
<li>Update cgmanifest (#16600)</li>
<li>Fix broken links in markdown (#16598)</li>
<li>Add explicit job name for approval tasks in Snap stage (#16579)</li>
<li>Bring back <code>pwsh.exe</code> for framework dependent packages to support Start-Job (#16535)</li>
<li>Fix NuGet package generation in release build (#16509)</li>
<li>Add `Microsoft.PowerShell.Commands.SetStrictModeCommand.ArgumentToPSVersionTransformationAttribute` to list of patterns to remove for generated ref assembly (#16489)</li>
<li>Bump Microsoft.CodeAnalysis.CSharp from `4.0.0-6.final` to `4.0.1` (#16423)</li>
<li>use different containers for different branches (#16434)</li>
<li>Add import so we can use common GitHub workflow function. (#16433)</li>
<li>Remove pre-release .NET 6 build sources (#16418)</li>
<li>Update release instructions with link to new build (#16419)</li>
<li>Bump <code>Microsoft.ApplicationInsights</code> from <code>2.18.0</code> to <code>2.19.0</code> (#16413)</li>
<li>Update <code>metadata.json</code> to make 7.2.0 the latest LTS (#16417)</li>
<li>Make static CI a matrix (#16397)</li>
<li>Update <code>metadata.json</code> in preparation on <code>7.3.0-preview.1</code> release (#16406)</li>
<li>Update cgmanifest (#16405)</li>
<li>Add diagnostics used to take corrective action when releasing `buildInfoJson` (#16404)</li>
<li>`vPack` release should use `buildInfoJson` new to 7.2 (#16402)</li>
<li>Update the usage of metadata.json for getting LTS information (#16381)</li>
<li>Add checkout to build json stage to get `ci.psm1` (#16399)</li>
<li>Update CgManifest.json for 6.0.0 .NET packages (#16398)</li>
<li>Add current folder to the beginning of the module import (#16353)</li>
<li>Increment RC MSI build number by 100 (#16354)</li>
<li>Bump <code>XunitXml.TestLogger</code> from 3.0.66 to 3.0.70 (#16356)</li>
<li>Move PR Quantifier config to subfolder (#16352)</li>
<li>Release build info json when it is preview (#16335)</li>
<li>Add an approval for releasing build-info json (#16351)</li>
<li>Generate manifest with latest public version of the packages (#16337)</li>
<li>Update to the latest notices file (#16339) (Thanks @github-actions[bot]!)</li>
<li>Use notice task to generate license assuming cgmanifest contains all components (#16340)</li>
<li>Refactor cgmanifest generator to include all components (#16326)</li>
<li>Fix issues in release build (#16332)</li>
<li>Update feed and analyzer dependency (#16327)</li>
<li>Bump <code>Microsoft.NET.Test.Sdk</code> from 16.11.0 to 17.0.0 (#16312)</li>
<li>Update license and cgmanifest (#16325) (Thanks @github-actions[bot]!)</li>
<li>Fix condition in cgmanifest logic (#16324)</li>
<li>Add GitHub Workflow to keep notices up to date (#16284)</li>
<li>Update to latest .NET 6 GA build <code> 6.0.100-rtm.21527.11</code> (#16309)</li>
<li>Create compliance build (#16286)</li>
<li>Move mapping file into product repo and add Debian 11  (#16316)</li>
<li>Add a major-minor build info JSON file (#16301)</li>
<li>Clean up <code>crossgen</code> related build scripts also generate native symbols for <code>R2R</code> images (#16297)</li>
<li>Fix Windows build ZIP packaging (#16299) (Thanks @kondratyev-nv!)</li>
<li>Revert &quot;Update to use .NET 6 GA build (#16296)&quot; (#16308)</li>
<li>Add <code>wget</code> as a dependency for Bootstrap script (#16303) (Thanks @kondratyev-nv!)</li>
<li>Fix issues reported by code signing verification tool (#16291)</li>
<li>Update to use .NET 6 GA build (#16296)</li>
<li>Revert &quot;add GH workflow to keep the cgmanifest up to date.&quot; (#16294)</li>
<li>Update ChangeLog for 7.2.0-rc.1 and also fix RPM packaging (#16290)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#16271)</li>
<li>add GH workflow to keep the cgmanifest up to date.</li>
<li>Update <code>ThirdPartyNotices.txt</code> (#16283)</li>
<li>Update `testartifacts.yml` to use <code>ubuntu-latest</code> image (#16279)</li>
<li>Update version of <code>Microsoft.PowerShell.Native</code> and <code>Microsoft.PowerShell.MarkdownRender</code> packages (#16277)</li>
<li>Add script to generate <code>cgmanifest.json</code> (#16278)</li>
<li>Add <code>cgmanifest.json</code> for generating correct third party notice file (#16266)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers from `6.0.0-rtm.21504.2` to `6.0.0-rtm.21516.1` (#16264)</li>
<li>Only upload stable <code>buildinfo</code> for stable releases (#16251)</li>
<li>Make RPM license recognized (#16189)</li>
<li>Don't upload dep or tar.gz for RPM because there are none. (#16230)</li>
<li>Add condition to generate release files in local dev build only (#16259)</li>
<li>Update .NET 6 to version 6.0.100-rc.2.21505.57 (#16249)</li>
<li>change order of try-catch-finally and split out arm runs (#16252)</li>
<li>Ensure <code>psoptions.json</code> and <code>manifest.spdx.json</code> files always exist in packages (#16258)</li>
<li>Update to vPack task version to 12 (#16250)</li>
<li>Remove unneeded `NuGetConfigFile` resource string (#16232)</li>
<li>Add Software Bill of Materials to the main packages (#16202)</li>
<li>Sign third party exes (#16229)</li>
<li>Upgrade <code>set-value</code> package for markdown test (#16196)</li>
<li>Use Ubuntu 20.04 for SSH remoting test (#16225)</li>
<li>Bump Microsoft.CodeAnalysis.NetAnalyzers (#16194)</li>
<li>Bump `Microsoft.CodeAnalysis.NetAnalyzers` from `6.0.0-rc2.21458.5` to `6.0.0-rtm.21480.8` (#16183)</li>
<li>Move vPack build to 1ES Pool (#16169)</li>
<li>Fix Microsoft update spelling issue. (#16178)</li>
</ul>

</details>

### Documentation and Help Content

- Update Windows PowerShell issues link (#16105) (Thanks @andschwa!)
- Remove Joey from Committee and WG membership (#16119)
- Update more docs for `net6.0` TFM (#16102) (Thanks @xtqqczze!)
- Change `snippet` tag to `code` tag in XML comments (#16106)
- Update build documentation to reflect .NET 6 (#15751) (Thanks @Kellen-Stuart!)
- Update `README.md` about the change logs (#16471) (Thanks @powershellpr0mpt!)
- Update change log for 7.2.0 (#16401)
- Update `metadata.json` and `README.md` for 7.2.0 release (#16395)
- Update `README.md` and `metadata.json` files for `v7.2.0-rc.1` release (#16285)
- Update the change logs for `v7.0.8` and `v7.1.5` releases (#16248)

[7.3.0-preview.1]: https://github.com/PowerShell/PowerShell/compare/v7.2.0-preview.10...v7.3.0-preview.1
