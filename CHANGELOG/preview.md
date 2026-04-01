# Preview Changelog

## [7.7.0-preview.1]

### Engine Updates and Fixes

- Fix up default value for parameters with the `in` modifier (#26785) (Thanks @jborean93!)

### General Cmdlet Updates and Fixes

- Add verbose message to `Get-Service` when properties cannot be returned (#27109) (Thanks @reabr!)
- Fix `Remove-Item` confirmation message to use provider path instead (#27123) (Thanks @scuzqy!)
- PSStyle: validate background index against `BackgroundColorMap` (#27106) (Thanks @cuiweixie!)
- Update PowerShell Profile DSC resource manifests to allow null for content (#26929)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze</p>

</summary>

<ul>
<li>Fix <code>IDisposable</code> implementation in sealed classes (#26215) (Thanks @xtqqczze!)</li>
<li>Enable CA1852: Seal internal types (#25890) (Thanks @xtqqczze!)</li>
<li>Remove obsolete <code>CA2006</code> rule suppression (#25939) (Thanks @xtqqczze!)</li>
</ul>

</details>

### Tools

- Add GitOps policy to auto-label backport candidates when CL-BuildPackaging is added (#26881)

### Tests

- Fix the `PSNativeCommandArgumentPassing` test (#27057)
- Fix `Import-Module.Tests.ps1` to handle Arm32 platform (#26862)
- Add comprehensive PowerShell class tests for `ConvertTo-Json` (#26769) (Thanks @yotsuda!)
- Add comprehensive `PSCustomObject` tests for `ConvertTo-Json` (#26743) (Thanks @yotsuda!)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@powercode, @kasperk81, @xtqqczze</p>

</summary>

<ul>
<li>Separate Store Package Creation, Skip Polling for Store Publish, Clean up PDP-Media (#27024)</li>
<li>Revert &quot;Fetch latest ICU release version dynamically&quot; (#27127)</li>
<li>Update package references and move to .NET SDK 11.0-preview.2 (#27117)</li>
<li>Add comment-based help documentation to <code>build.psm1</code> functions (#27122) (Thanks @powercode!)</li>
<li>Bump github/codeql-action from 4.34.1 to 4.35.1 (#27120)</li>
<li>Select New MSIX Package Name (#27096)</li>
<li>Separate Official and NonOfficial templates for ADO pipelines (#26897)</li>
<li>Bump github/codeql-action from 4.32.6 to 4.34.1 (#27087)</li>
<li>Update the <code>PhoneProductId</code> to be the official LTS id used by Store (#27077)</li>
<li>release-upload-buildinfo: replace version-comparison channel gating with metadata flags (#27074)</li>
<li>Update build to create two msix's and msixbundles for LTS and Stable (#27056)</li>
<li>Update <code>metadata.json</code> for the v7.6.0 release (#27054)</li>
<li>Move <code>_GetDependencies</code> MSBuild target from dynamic generation in <code>build.psm1</code> into <code>Microsoft.PowerShell.SDK.csproj</code> (#27052)</li>
<li>Fix PMC repo URL for RHEL10  (#27059)</li>
<li>Create Linux LTS deb/rpm packages for LTS releases (#27049)</li>
<li>Create LTS pkg and non-LTS pkg for macOS for LTS releases (#27039)</li>
<li>Fix the container image for vPack, MSIX vPack and Package pipelines (#27015)</li>
<li>Update <code>Microsoft.PowerShell.PSResourceGet</code> version to 1.2.0 (#27003)</li>
<li>Fix ConvertFrom-ClearlyDefinedCoordinates to handle API object coordinates (#26893)</li>
<li>Bump actions/upload-artifact from 6 to 7 (#26914)</li>
<li>Bump actions/dependency-review-action from 4.8.3 to 4.9.0 (#26938)</li>
<li>Bump github/codeql-action from 4.32.4 to 4.32.6 (#26942)</li>
<li>Hardcode Official templates (#26928)</li>
<li>Add PMC packages for debian13 and rhel10 (#26912)</li>
<li>Split TPN manifest and Component Governance manifest (#26891)</li>
<li>Add version in description and pass store task on failure (#26885)</li>
<li>Bump actions/dependency-review-action from 4.8.2 to 4.8.3 (#26861)</li>
<li>Bump github/codeql-action from 4.32.3 to 4.32.4 (#26879)</li>
<li>Correct the package name for .deb and .rpm packages (#26877)</li>
<li>Fix a preview detection test for the packaging script (#26882)</li>
<li>Exclude .exe packages from publishing to GitHub (#26859)</li>
<li>Update metadata.json for v7.6.0-rc.1 (#26856)</li>
<li>Fetch latest ICU release version dynamically (#26827) (Thanks @kasperk81!)</li>
<li>Update <code>LangVersion</code> to <code>preview</code> (#26214) (Thanks @xtqqczze!)</li>
<li>Update to .NET 11 SDK and update dependencies (#26783)</li>
<li>Bump github/codeql-action from 4.32.2 to 4.32.3 (#26839)</li>
</ul>

</details>

### Documentation and Help Content

- Check in `7.6.md` after v7.6.0 release (#27063)
- Update changelog for release v7.5.5 (#27014)
- Add 7.4.14 changelog (#26998)
- Bring the `v7.6.0-rc.1` changelog to master (#26857)

[7.7.0-preview.1]: https://github.com/PowerShell/PowerShell/compare/8f457fc71...v7.7.0-preview.1

