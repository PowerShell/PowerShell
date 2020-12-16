# Current preview release

## [7.2.0-preview.1] - 2020-11-17

### Engine Updates and Fixes

- Change the default fallback encoding for `GetEncoding` in `Start-Transcript` to be `UTF8` without a BOM (#13732) (Thanks @Gimly!)

### General Cmdlet Updates and Fixes

- Update `pwsh -?` output to match docs (#13748)
- Fix `NullReferenceException` in `Test-Json` (#12942) (Thanks @iSazonov!)
- Make `Dispose` in `TranscriptionOption` idempotent (#13839) (Thanks @krishnayalavarthi!)
- Add additional Microsoft PowerShell modules to the tracked modules list (#12183)
- Relax further `SSL` verification checks for `WSMan` on non-Windows hosts with verification available (#13786) (Thanks @jborean93!)
- Add the `OutputTypeAttribute` to `Get-ExperimentalFeature` (#13738) (Thanks @ThomasNieto!)
- Fix blocking wait when starting file associated with a Windows application (#13750)
- Emit warning if `ConvertTo-Json` exceeds `-Depth` value (#13692)

### Code Cleanup

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@xtqqczze, @mkswd, @ThomasNieto, @PatLeong, @paul-cheung, @georgettica</p>

</summary>

<ul>
<li>Fix RCS1049: Simplify boolean comparison (#13994) (Thanks @xtqqczze!)</li>
<li>Enable IDE0062: Make local function static (#14044) (Thanks @xtqqczze!)</li>
<li>Enable CA2207: Initialize value type static fields inline (#14068) (Thanks @xtqqczze!)</li>
<li>Enable CA1837: Use <code>ProcessId</code> and <code>CurrentManagedThreadId</code> from <code>System.Environment</code> (#14063) (Thanks @xtqqczze and @PatLeong!)</li>
<li>Remove unnecessary using directives (#14014, #14017, #14021, #14050, #14065, #14066, #13863, #13860, #13861, #13814) (Thanks @xtqqczze and @ThomasNieto!)</li>
<li>Remove unnecessary usage of LINQ <code>Count</code> method (#13545) (Thanks @xtqqczze!)</li>
<li>Fix SA1518: The code must not contain extra blank lines at the end of the file (#13574) (Thanks @xtqqczze!)</li>
<li>Enable CA1829: Use the <code>Length</code> or <code>Count</code> property instead of <code>Count()</code> (#13925) (Thanks @xtqqczze!)</li>
<li>Enable CA1827: Do not use <code>Count()</code> or <code>LongCount()</code> when <code>Any()</code> can be used (#13923) (Thanks @xtqqczze!)</li>
<li>Enable or fix nullable usage in a few files (#13793, #13805, #13808, #14018, #13804) (Thanks @mkswd and @georgettica!)</li>
<li>Enable IDE0040: Add accessibility modifiers (#13962, #13874) (Thanks @xtqqczze!)</li>
<li>Make applicable private Guid fields readonly (#14000) (Thanks @xtqqczze!)</li>
<li>Fix CA1003: Use generic event handler instances (#13937) (Thanks @xtqqczze!)</li>
<li>Simplify delegate creation (#13578) (Thanks @xtqqczze!)</li>
<li>Fix RCS1033: Remove redundant boolean literal (#13454) (Thanks @xtqqczze!)</li>
<li>Fix RCS1221: Use pattern matching instead of combination of <code>as</code> operator and null check (#13333) (Thanks @xtqqczze!)</li>
<li>Use <code>is not</code> syntax (#13338) (Thanks @xtqqczze!)</li>
<li>Replace magic number with constant in PDH (#13536) (Thanks @xtqqczze!)</li>
<li>Fix accessor order (#13538) (Thanks @xtqqczze!)</li>
<li>Enable IDE0054: Use compound assignment (#13546) (Thanks @xtqqczze!)</li>
<li>Fix RCS1098: Constant values should be on right side of comparisons (#13833) (Thanks @xtqqczze!)</li>
<li>Enable CA1068: <code>CancellationToken</code> parameters must come last (#13867) (Thanks @xtqqczze!)</li>
<li>Enable CA10XX rules with suggestion severity (#13870, #13928, #13924) (Thanks @xtqqczze!)</li>
<li>Enable IDE0064: Make Struct fields writable (#13945) (Thanks @xtqqczze!)</li>
<li>Run <code>dotnet-format</code> to improve formatting of source code (#13503) (Thanks @xtqqczze!)</li>
<li>Enable CA1825: Avoid zero-length array allocations (#13961) (Thanks @xtqqczze!)</li>
<li>Add IDE analyzer rule IDs to comments (#13960) (Thanks @xtqqczze!)</li>
<li>Enable CA1830: Prefer strongly-typed <code>Append</code> and <code>Insert</code> method overloads on <code>StringBuilder</code> (#13926) (Thanks @xtqqczze!)</li>
<li>Enforce code style in build (#13957) (Thanks @xtqqczze!)</li>
<li>Enable CA1836: Prefer <code>IsEmpty</code> over <code>Count</code> when available (#13877) (Thanks @xtqqczze!)</li>
<li>Enable CA1834: Consider using <code>StringBuilder.Append(char)</code> when applicable (#13878) (Thanks @xtqqczze!)</li>
<li>Fix IDE0044: Make field readonly (#13884, #13885, #13888, #13892, #13889, #13886, #13890, #13891, #13887, #13893, #13969, #13967, #13968, #13970, #13971, #13966, #14012) (Thanks @xtqqczze!)</li>
<li>Enable IDE0048: Add required parentheses (#13896) (Thanks @xtqqczze!)</li>
<li>Enable IDE1005: Invoke delegate with conditional access (#13911) (Thanks @xtqqczze!)</li>
<li>Enable IDE0036: Enable the check on the order of modifiers (#13958, #13881) (Thanks @xtqqczze!)</li>
<li>Use span-based <code>String.Concat</code> instead of <code>String.Substring</code> (#13500) (Thanks @xtqqczze!)</li>
<li>Enable CA1050: Declare types in namespace (#13872) (Thanks @xtqqczze!)</li>
<li>Fix minor keyword typo in C# code comment (#13811) (Thanks @paul-cheung!)</li>
</ul>

</details>

### Tools

- Enable `CodeQL` Security scanning (#13894)
- Add global `AnalyzerConfig` with default configuration (#13835) (Thanks @xtqqczze!)

### Build and Packaging Improvements

<details>

<summary>

<p>We thank the following contributors!</p>
<p>@mkswd, @xtqqczze</p>

</summary>

<ul>
<li>Bump <code>Microsoft.NET.Test.Sdk</code> to <code>16.8.0</code> (#14020)</li>
<li>Bump <code>Microsoft.CodeAnalysis.CSharp</code> to <code>3.8.0</code> (#14075)</li>
<li>Remove workarounds for .NET 5 RTM builds (#14038)</li>
<li>Migrate 3rd party signing to ESRP (#14010)</li>
<li>Fixes to release pipeline for GA release (#14034)</li>
<li>Don't do a shallow checkout (#13992)</li>
<li>Add validation and dependencies for Ubuntu 20.04 distribution to packaging script (#13993)</li>
<li>Add .NET install workaround for RTM (#13991)</li>
<li>Move to ESRP signing for Windows files (#13988)</li>
<li>Update <code>PSReadLine</code> version to <code>2.1.0</code> (#13975)</li>
<li>Bump .NET to version <code>5.0.100-rtm.20526.5</code> (#13920)</li>
<li>Update script to use .NET RTM feeds (#13927)</li>
<li>Add checkout step to release build templates (#13840)</li>
<li>Turn on <code>/features:strict</code> for all projects (#13383) (Thanks @xtqqczze!)</li>
<li>Bump <code>NJsonSchema</code> to <code>10.2.2</code> (#13722, #13751)</li>
<li>Add flag to make Linux script publish to production repo (#13714)</li>
<li>Bump <code>Markdig.Signed</code> to <code>0.22.0</code> (#13741)</li>
<li>Use new release script for Linux packages (#13705)</li>
</ul>

</details>

### Documentation and Help Content

- Fix links to LTS versions for Windows (#14070)
- Fix `crontab` formatting in example doc (#13712) (Thanks @dgoldman-msft!)

[7.2.0-preview.1]: https://github.com/PowerShell/PowerShell/compare/v7.1.0...v7.2.0-preview.1
