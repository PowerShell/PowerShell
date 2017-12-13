# Breaking Changes for PowerShell 6.0

### Skip null-element check for collections with a value-type element type [5432](https://github.com/PowerShell/PowerShell/issues/5432)
TODO


### Remove AddTypeCommandBase class [5407](https://github.com/PowerShell/PowerShell/issues/5407)
TODO


### Change $OutputEncoding to use iso-8859-1 encoding rather than ASCII [5361](https://github.com/PowerShell/PowerShell/issues/5361)
This is marked as a breaking change because the output of the pipeline is not altered as it was previously, anyone relying on ascii encoding (especially for extended ascii sequences) will no longer have that behavior.

TODO: This is closed in favor of a new PR which simply changes the output encoding to utf8nobom, the new PR is not referenced.  Need to find and see if it is still a breaking change.


### Invoke-RestMethod doesn't return useful info when no data is returned. [5320](https://github.com/PowerShell/PowerShell/issues/5320)
When an API returns just `null`, Invoke-RestMethod was serializing this as the string `"null"` instead of `$null`.  This change fixes the logic in `Invoke-RestMethod` to properly serialize a valid single value JSON `null` literal as `$null`.


### Remove DCOM support from *-Computer cmdlets (replacement for 5152) [5277](https://github.com/PowerShell/PowerShell/issues/5277)
DCOM is not supported in corefx, therefore the *-Computer cmdlets were removed.


### Remove AllScope from most default aliases [5268](https://github.com/PowerShell/PowerShell/issues/5268)
To speed up spope creation, AllScope was removed from most default aliases.  AllScope was left for a few frequently used aliases where the lookup was faster.


### ConvertFrom-Json silently throws away duplicated keys with identical casing [5199](https://github.com/PowerShell/PowerShell/issues/5199)
TODO: Still open


### fix get-item -literalpath a*b if `a*b` doesn't actually exist to return error [5197](https://github.com/PowerShell/PowerShell/issues/5197)
Previously, `-literalpath` given a wildcard will treat it the same as -path and if the wildcard found no files, it would silently exit. Correct behavior should be that -literalpath is literal so if the file doesn't exist, it should error. Fix is to also see if wildcard is being suppressed.


### don't insert linebreaks to output (except for tables) [5193](https://github.com/PowerShell/PowerShell/issues/5193)
TODO: Not sure what there is besides the title


### Don't wrap stderr as ErrorRecord [5190](https://github.com/PowerShell/PowerShell/issues/5190)
Keep stderr as string and don't wrap as ErrorRecord.  This means that errors from running pwsh.exe within pwsh are now just stderr strings even though they visually look like ErrorRecords so tests had to be changed.


### Passing a ScriptBlock (in syntax only) to native commands produces surprising results [5187](https://github.com/PowerShell/PowerShell/issues/5187)
TODO: still open


### Import-Csv Should apply PSTypeNames uppon import when Type Information is present in the CSV [5134](https://github.com/PowerShell/PowerShell/issues/5134)
Adds PSTypeName preservation on `Import-Csv` and `ConvertFrom-Csv`.


### -NoTypeInformation should be default on Export-Csv [5131](https://github.com/PowerShell/PowerShell/issues/5131)
Sets `-NoTypreInformation` as the default for `Export-Csv` and `ConvertTo-Csv`, hides the `-NoTypeInformation` parameter switch, adds `-IncludeTypeInformation` switch to `Export-Csv` and `ConvertTo-Csv` to enable legacy behavior, and provides a terminating error when both `-NoTypeInformation` and `-IncludeTypeInformation` are supplied.


### allow pscustomobject to have property with empty string name [5129](https://github.com/PowerShell/PowerShell/issues/5129)
TODO: Not taking the change, dup of [2820](https://github.com/PowerShell/PowerShell/pull/2820)


### StdErr/ErrorRecord should not be modified on output [5127](https://github.com/PowerShell/PowerShell/issues/5127)
TODO: Closed, not merged


### -Verbose should not override $ErrorActionPreference [5113](https://github.com/PowerShell/PowerShell/issues/5113)
`$ErrorActionPreference` is longer overwritten by the `-Verbose` or `-Debug` parameters.


### Web Cmdlets should warn when  legacy -Credential is sent over unencrypted connections [5112](https://github.com/PowerShell/PowerShell/issues/5112)
An error was added when a user tries to use `-Credential` (legacy usage without `-Authentication`) or `-UseDefaultCredentials` over a non-HTTPS URI. Users can bypass the error with `-AllowUnencryptedAuthentication`.


### Use InvariantCulture for implicit argument conversion [5106](https://github.com/PowerShell/PowerShell/issues/5106)
TODO: Closed, not merged


### Rename powershell.exe to pwsh.exe [5101](https://github.com/PowerShell/PowerShell/issues/5101)
TODO: (Not sure how to summarize this change further than the title)


### Remove '-ComputerName' from *-Service cmdlets [5090](https://github.com/PowerShell/PowerShell/issues/5094)
Although `-ComputerName` worked correctly, it was removed from `*-Service` cmdlets so that users consistently rely on PSRP and only have that port open.


### Unify cmdlets with parameter 'Encoding' to be of type System.Text.Encoding [5080](https://github.com/PowerShell/PowerShell/issues/5080)
The `-Encoding` Byte has been removed from the filesystem provider cmdlets. A new parameter -Byte is now the signal that a byte stream is required as input or output will be a stream of bytes.


### Add better error message for empty and null -UFormat arg [5055](https://github.com/PowerShell/PowerShell/issues/5055)
Previously, when passing an empty format string to `-UFormat`, a somewhat unhelpful error message would appear.  A more descriptive error has been added.


### Clean up console code [4995](https://github.com/PowerShell/PowerShell/issues/4995)
The following features were removed as they are not supported in PowerShell Core, and there are no plans to add support as they exist for legacy reasons for Windows PowerShell: psconsolefile switch and code, importsystemmodulues switch and code, and font changing code.


### removed RunspaceConfiguration support [4942](https://github.com/PowerShell/PowerShell/issues/4942)
In PSCore6 only InitialSessionState is supported.  Since the RunspaceConfiguration api was made internal, the code related to RunspaceConfiguration has been removed.  This means that some public apis have changed and it was decided that it was best to have a compile time error.


### Add Tests for *-computer cmdlets [4926](https://github.com/PowerShell/PowerShell/issues/4926)
TODO


### CommandInvocationIntrinsics.InvokeScript bind arguments to $input instead of $args [4923](https://github.com/PowerShell/PowerShell/issues/4923)
An incorrect position of a parameter resulted in the args passed as input instead of as args.


### remove unsupported -showwindow switch from get-help [4903](https://github.com/PowerShell/PowerShell/issues/4903)
`-showwindow` relies on WPF, which is not supported on CoreCLR.


### allow * to be used in registry path for remove-item [4866](https://github.com/PowerShell/PowerShell/issues/4866)
Using `-literalpath` with an asterisk and the filesystem provider will now return an error rather than returning quietly.


### fix set-service failing test [4802](https://github.com/PowerShell/PowerShell/issues/4802)
`New-Service` should throw on System `-StartupType`
TODO : Looks like this a change in test cases, not sure what the documentation process should be

### Enhance the -split operator with negative maximum token counts to split from the end [4765](https://github.com/PowerShell/PowerShell/issues/4765)
TODO


### Rename $IsOSX to $IsacOS [4700](https://github.com/PowerShell/PowerShell/issues/4700)
The naming in PowerShell should be consistent with our naming andd conform to Apple's use of macOS instead of OSX, however, for readability and consistently we are staying with Pascal casing.


### Prepare for BOM-less UTF-8 default character encoding with respect to $OutputEncoding and console code page  [4681](https://github.com/PowerShell/PowerShell/issues/4681)
TODO


### New-TemporaryFile should throw terminating error [4634](https://github.com/PowerShell/PowerShell/issues/4634)
TODO


### S.M.A.PowerShell.HadErrors and $? return false positives when errors are suppressed [4613](https://github.com/PowerShell/PowerShell/issues/4613)
TODO


### Make error message consistent when invalid script is passed to -File, better error when passed ambiguous arg [4573](https://github.com/PowerShell/PowerShell/issues/4573)
TODO (I believe the breaking change here is for "Enable -WindowStyle to work", changing the exit code.  Need to confirm)


### Fix Get-Date -UFormat '%V' week number output [4508](https://github.com/PowerShell/PowerShell/issues/4508)
TODO


### Improper usage of $input as a function parameter is silently ignored [4391](https://github.com/PowerShell/PowerShell/issues/4391)
TODO


### Due to the use of unsupported APIs, we must remove the Counter CmdLets in the Diagnostics Module until a better solution is found. [4303](https://github.com/PowerShell/PowerShell/issues/4303)
TODO (Not sure how to document more than the title)


###  Due to the use of unsupported APIs, we must remove the LocalAccounts module until a better solution is found. [4302](https://github.com/PowerShell/PowerShell/issues/4302)
TODO (Not sure how to document more than the title)


### Remove UTC and SQM telemetry code [4190](https://github.com/PowerShell/PowerShell/issues/4190)
TODO


### Unify file encoding when a cmdlet creates a file [4119](https://github.com/PowerShell/PowerShell/issues/4119)
TODO


### Executing powershell script with bool parameter doesnt work [4036](https://github.com/PowerShell/PowerShell/issues/4036)
TODO


### Remove ClrVersion property from $PSVersionTable [4027](https://github.com/PowerShell/PowerShell/issues/4027)
The ClrVersion property of `$PSVersionTable` is not useful with CoreCLR,  end users should not be using that value to determine compatibility.


### Command line arguments with a dollar sign [4024](https://github.com/PowerShell/PowerShell/issues/4024)
TODO


### Change positional parameter for powershell.exe from -Command to -File [4019](https://github.com/PowerShell/PowerShell/issues/4019)
This fixes the usage of `#!` in PowerShell scripts that are being executed from non-PowerShell shells on non-Windows platforms.
This also means that you can now do things like `powershell foo.ps1` or `powershell fooScript` without specifying `-File`. However, this change now requires that you explicitly specify `-c` or `-Command` when trying to do things like `powershell.exe Get-Command`.

### Implement Unicode escape parsing [3958](https://github.com/PowerShell/PowerShell/issues/3958)
TODO


### Change New-ModuleManifest encoding to UTF8NoBOM on non-Windows platforms [3940](https://github.com/PowerShell/PowerShell/issues/3940)
TODO


### Fix 'Get-Content -Delimiter' to not include the delimiter in the returned lines [3808](https://github.com/PowerShell/PowerShell/issues/3808)
TODO


### Prevent Get-ChildItem from recursing into symlinks (#1875). [3780](https://github.com/PowerShell/PowerShell/issues/3780)
TODO


### COM objects are not enumerating properly [3775](https://github.com/PowerShell/PowerShell/issues/3775)
TODO


### System.IO.DirectoryInfo and System.IO.FileInfo instances output by Get-Item / Get-ChildItem bind to -Path rather than -LiteralPath and sometimes by filename only [3772](https://github.com/PowerShell/PowerShell/issues/3772)
TODO


### Why do handled exceptions show in ErrorVariable? [3768](https://github.com/PowerShell/PowerShell/issues/3768)
TODO


### Align PowerShell's CLI / startup behavior with POSIX-like shells such as Bash - command-line arguments [3743](https://github.com/PowerShell/PowerShell/issues/3743)
TODO


### Get-Content -Delimiter unexpectedly keeps the delimiter in the lines returned [3706](https://github.com/PowerShell/PowerShell/issues/3706)
TODO


### Should we detect/Deny using $_ as a user defined variable? [3695](https://github.com/PowerShell/PowerShell/issues/3695)
TODO


### Added -i swtich to powershell for interactive [3558](https://github.com/PowerShell/PowerShell/issues/3558)
TODO


### PowerShell use different logic, when converting passed value to parameter type, for compiled and script cmdlets [3348](https://github.com/PowerShell/PowerShell/issues/3348)
TODO


### Implement Format-Hex in C# [3320](https://github.com/PowerShell/PowerShell/issues/3320)
TODO


### PowerShell as a default shell doesn't work with script command [3319](https://github.com/PowerShell/PowerShell/issues/3319)
TODO


### Completions for environment variables differ between platforms [3227](https://github.com/PowerShell/PowerShell/issues/3227)
TODO


### Typo fix in Get-ComputerInfo property name [3167](https://github.com/PowerShell/PowerShell/issues/3167)
`BiosSerialNumber` was misspelled as "BiosSeralNumber".


### -OutVariable doesn't unwrap single-object output and creates [System.Collections.ArrayList] values rather than [System.Object[]] [3154](https://github.com/PowerShell/PowerShell/issues/3154)
TODO


### Add Get-StringHash and Get-FileHash cmdlets [3024](https://github.com/PowerShell/PowerShell/issues/3024)
TODO


### Add validation on get-* cmdlets where passing $null returns all objects instead of error [2672](https://github.com/PowerShell/PowerShell/issues/2672)
TODO


### Get-Alias Name parameter allows Null value [2544](https://github.com/PowerShell/PowerShell/issues/2544)
TODO


### Add ValidateNullOrEmpty to -Name parameter of Get-Service [2542](https://github.com/PowerShell/PowerShell/issues/2542)
TODO


### Get-Service Name parameter allows Null value [2540](https://github.com/PowerShell/PowerShell/issues/2540)
TODO


### Add support W3C Extended Log File Format in Import-Csv [2482](https://github.com/PowerShell/PowerShell/issues/2482)
TODO


### Platform specific behavior for Split-Path [2301](https://github.com/PowerShell/PowerShell/issues/2301)
TODO


### -Verbose Changes  (downgrades) the Error Behaviour [2247](https://github.com/PowerShell/PowerShell/issues/2247)
TODO


### Parameter binding problem with ValueFromRemainingArguments in PS functions [2035](https://github.com/PowerShell/PowerShell/issues/2035)
TODO


### $input type in advanced functions [1563](https://github.com/PowerShell/PowerShell/issues/1563)
TODO


### BuildVersion should be removed from $PSVersionTable [1415](https://github.com/PowerShell/PowerShell/issues/1415)
Remove the `BuildVersion` property from `$PSVersionTable`. This property was strongly tied to the Windows build version. Instead, we recommend that you use `GitCommitId` to retrieve the exact build version of PowerShell Core.
