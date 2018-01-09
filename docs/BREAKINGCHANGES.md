# Breaking Changes for PowerShell 6.0

## Explict Removal of Support

### Workflow

### Custom Snap-Ins

### WMI v1 cmdlets
(Get-WmiObject, invoke-WmiMethod, etc.)

### RunspaceConfiguration

## Engine/language changes

### Rename `powershell.exe` to `pwsh.exe` [#5101](https://github.com/PowerShell/PowerShell/issues/5101)

In order to give users a deterministic way to call PowerShell Core on Windows (as opposed to Windows PowerShell),
the PowerShell Core binary was changed to `pwsh.exe` on Windows and `pwsh` on non-Windows platforms.

Additionally, this was done to reduce the typing time to open PowerShell,
especially for non-Windows users who are used to three or four letter shell names.

In the future, some kind of alias or symbolic link for `powershell` may be added to non-Windows platforms.

### Don't insert line breaks to output (except for tables) [#5193](https://github.com/PowerShell/PowerShell/issues/5193)
Previously, output was aligned to the width of the console and linebreak were added at the end width of the console, meaning the output doesn't get reformatted as expected if the terminal is resized.
This change was not applied to tables, as the linebreaks are necessary to keep the columns aligned.

### Skip null-element check for collections with a value-type element type [#5432](https://github.com/PowerShell/PowerShell/issues/5432)
For the `Mandatory` parameter and `ValidateNotNull` and `ValidateNotNullOrEmpty` attributes, skip the null-element check if the collection's element type is value type.  

### Change `$OutputEncoding` to use `iso-8859-1` encoding rather than ASCII [#5369](https://github.com/PowerShell/PowerShell/issues/5369)
The previous encoding, ASCII (7-bit), would result in incorrect alteration of the output in some cases.  This change is to make UTF-8 NoBOM default, which preserves unicode output with an encoding supported by most tools and operating systems.

### Remove `AllScope` from most default aliases [#5268](https://github.com/PowerShell/PowerShell/issues/5268)
To speed up scope creation, `AllScope` was removed from most default aliases.
`AllScope` was left for a few frequently used aliases where the lookup was faster.

### Don't wrap `stderr` as `ErrorRecord` [#5190](https://github.com/PowerShell/PowerShell/issues/5190)

Keep stderr as string and don't wrap as an `ErrorRecord`.
This means that errors from running `pwsh.exe` within PowerShell Core are now just `stderr` strings even though they visually look like ErrorRecords
This change my require you to change how your tests handle native commmand errors.

### `-Verbose` and `-Debug` no longer overrides `$ErrorActionPreference` [#5113](https://github.com/PowerShell/PowerShell/issues/5113)

Previously, if `-Verbose` or `-Debug` were specified, it overrode the behavior of `$ErrorActionPreference`.
With this change, `-Verbose` and `-Debug` no longer affect the behavior of `$ErrorActionPreference`.


## Cmdlet changes

### Invoke-RestMethod doesn't return useful info when no data is returned. [#5320](https://github.com/PowerShell/PowerShell/issues/5320)
When an API returns just `null`, Invoke-RestMethod was serializing this as the string `"null"` instead of `$null`.  This change fixes the logic in `Invoke-RestMethod` to properly serialize a valid single value JSON `null` literal as `$null`.

### Remove DCOM support from `*-Computer` cmdlets (replacement for 5152) [#5277](https://github.com/PowerShell/PowerShell/issues/5277)

DCOM is not supported in CoreFX, so the `*-Computer` cmdlets removed the `-ComputerName` parameter that supported DCOM-based remoting.

### Remove `-ComputerName` from `*-Service` cmdlets [#5090](https://github.com/PowerShell/PowerShell/issues/5094)

In order to encourage the consistent use of PSRP, the `-ComputerName` parameter was removed from `*-Service` cmdlets.

### Fix `Get-Item -LiteralPath a*b` if `a*b` doesn't actually exist to return error [#5197](https://github.com/PowerShell/PowerShell/issues/5197)

Previously, `-LiteralPath` given a wildcard will treat it the same as `-Path` and if the wildcard found no files, it would silently exit.
Correct behavior should be that `-LiteralPath` is literal so if the file doesn't exist, it should error.
Change is to treat wildcards used with `-Literal` as literal.

### `Import-Csv` should apply `PSTypeNames` upon import when type information is present in the CSV [#5134](https://github.com/PowerShell/PowerShell/issues/5134)

Previously, objects exported using `Export-CSV` with TypeInformation imported with `ConvertFrom-Csv` were not retaining the type information.
This change adds the type information to PSTypeNames member if available in the csv file.


### `-NoTypeInformation` should be default on `Export-Csv` [#5131](https://github.com/PowerShell/PowerShell/issues/5131)

This change was made to address customer feedback on the default behavior of `Export-CSV` to include type information.

Previously, the cmdlet would output a comment as the first line containing the type name of the object.  
The change is to suppress this by default as it's not understood by most tools.  
Use `-IncludeTypeInformation` to retain the previous behavior.


### Web Cmdlets should warn when `-Credential` is sent over unencrypted connections [#5112](https://github.com/PowerShell/PowerShell/issues/5112)

When using HTTP, content including passwords are sent as clear-text.
This change is to not allow this by default and return an error if credentials are being passsed in an insecure manner.
Users can bypass this by using the `-AllowUnencryptedAuthethentication` switch.


## API changes

### Remove `AddTypeCommandBase` class [#5407](https://github.com/PowerShell/PowerShell/issues/5407)
The `AddTypeCommandBase` class was removed from `Add-Type` to improve performance.
TODO: Note sure what else, to add here.


### Unify cmdlets with parameter `-Encoding` to be of type `System.Text.Encoding` [#5080](https://github.com/PowerShell/PowerShell/issues/5080)

The `-Encoding` value `Byte` has been removed from the filesystem provider cmdlets.
A new parameter `-AsByteStream` is now used to specify that a byte stream is required as input or that output will be a stream of bytes.


### Add better error message for empty and null -UFormat arg [#5055](https://github.com/PowerShell/PowerShell/issues/5055)
Previously, when passing an empty format string to `-UFormat`, a somewhat unhelpful error message would appear.  A more descriptive error has been added.


### Clean up console code [#4995](https://github.com/PowerShell/PowerShell/issues/4995)
The following features were removed as they are not supported in PowerShell Core, and there are no plans to add support as they exist for legacy reasons for Windows PowerShell: psconsolefile switch and code, importsystemmodulues switch and code, and font changing code.


### removed RunspaceConfiguration support [#4942](https://github.com/PowerShell/PowerShell/issues/4942)
Previously, when creating a PowerShell runspace programmatically using the api you could use the legacy RunspaceConfiguration or the newer InitialSessionState.
This change removed support for RunspaceConfiguration and only supports InitialSessionState.


### CommandInvocationIntrinsics.InvokeScript bind arguments to $input instead of $args [#4923](https://github.com/PowerShell/PowerShell/issues/4923)
An incorrect position of a parameter resulted in the args passed as input instead of as args.


### remove unsupported -showwindow switch from get-help [#4903](https://github.com/PowerShell/PowerShell/issues/4903)
`-showwindow` relies on WPF, which is not supported on CoreCLR.


### allow * to be used in registry path for remove-item [#4866](https://github.com/PowerShell/PowerShell/issues/4866)
Using `-literalpath` with an asterisk and the filesystem provider will now return an error rather than returning quietly.

TODO: Comibine with 5197?


### fix set-service failing test [#4802](https://github.com/PowerShell/PowerShell/issues/4802)
`New-Service` should throw on System `-StartupType`
TODO : Looks like this a change in test cases, not sure what the documentation process should be

### Enhance the -split operator with negative maximum token counts to split from the end [#4765](https://github.com/PowerShell/PowerShell/issues/4765)
After the enhancement, only a `<Max-substrings>` value of `0` will be accepted as the explicit signal that all tokens should be returned, however many are found in the input string(s).
Negative `<Max-substrings>` values will work analogously to the already supported positive values, except that: they return the specified number of strings from the end of the string(s) and all individually extracted tokens are returned in input order, and whatever unsplit part remains, if any, is returned as the first token.


### Rename $IsOSX to $IsMacOS [#4700](https://github.com/PowerShell/PowerShell/issues/4700)
The naming in PowerShell should be consistent with our naming andd conform to Apple's use of macOS instead of OSX, however, for readability and consistently we are staying with Pascal casing.


### Prepare for BOM-less UTF-8 default character encoding with respect to $OutputEncoding and console code page  [#4681](https://github.com/PowerShell/PowerShell/issues/4681)
Change `$OutputEncoding` to be utf8 without BOM rather than ASCII as BOM-less UTF-8 character encoding is the default for PowerShell Core.


### Make error message consistent when invalid script is passed to -File, better error when passed ambiguous arg [#4573](https://github.com/PowerShell/PowerShell/issues/4573)
Change the exit codes of pwsh.exe to align with Unix conventions


### Removal of `LocalAccount` and cmdlets from  `Diagnostics` modules. [#4302](https://github.com/PowerShell/PowerShell/issues/4302) [#4303](https://github.com/PowerShell/PowerShell/issues/4303)
Due to unsupported APIs, the `LocalAccounts` module and the `Counter` cmdlets in the `Diagnostics` module were removed until a better solution is found. 


### Remove UTC and SQM telemetry code [#4190](https://github.com/PowerShell/PowerShell/issues/4190)
TODO: Should this still be in the doc if the change is not visible to the user and is about telemetry collection?


### Executing powershell script with bool parameter doesnt work [#4036](https://github.com/PowerShell/PowerShell/issues/4036)
Previously, using powershell.exe (now pwsh.exe) to execute a PowerShell script using `-File` provided no way to pass $true/$false as parameter values.  Support for $true/$false as parsed values to parameters was added. Switch values are also supported as currently documented syntax doesn't work.


### Remove ClrVersion property from $PSVersionTable [#4027](https://github.com/PowerShell/PowerShell/issues/4027)
The ClrVersion property of `$PSVersionTable` is not useful with CoreCLR,  end users should not be using that value to determine compatibility.


### Change positional parameter for powershell.exe from -Command to -File [#4019](https://github.com/PowerShell/PowerShell/issues/4019)
Enable shebang use of PowerShell on non-Windows platforms. This means on Unix based systems,
you can make a script executable which would invoke PowerShell automatically rather than explicitly invoking pwsh.
This also means that you can now do things like `powershell foo.ps1` or `powershell fooScript` without specifying `-File`.
However, this change now requires that you explicitly specify `-c` or `-Command` when trying to do things like `powershell.exe Get-Command`.

### Implement Unicode escape parsing [#3958](https://github.com/PowerShell/PowerShell/issues/3958)
TODO


### Change New-ModuleManifest encoding to UTF8NoBOM on non-Windows platforms [#3940](https://github.com/PowerShell/PowerShell/issues/3940)
Previously, `New-ModuleManifest` creates psd1 manifests in UTF-16 with BOM, creating a problem for Linux tools.  This breaking change changes the encoding of `New-ModuleManifest` to be UTF (no BOM) in non-Windows platforms.


### Prevent Get-ChildItem from recursing into symlinks (#1875). [#3780](https://github.com/PowerShell/PowerShell/issues/3780)
This change brings `Get-ChildItem` more in line with the Unix `ls -r` and the Windows `dir /s` native commands.  Like the mentioned commands, the cmdlet will display simbolic links to directories found during recursion, but will not recurse into them.


### Fix `Get-Content -Delimiter` to not include the delimiter in the returned lines [#3706](https://github.com/PowerShell/PowerShell/issues/3706)
Previously, the output while using `Get-Content -Delimiter` was inconsistent and inconvient as it required further processing of the data to remove the delimiter.
This change removes the delimiter in returned lines.


### Implement Format-Hex in C# [#3320](https://github.com/PowerShell/PowerShell/issues/3320)
 The `-raw` parameter is now No-op. Going forward all of the output will be displayed with a true representation of numbers that includes all of the bytes for its type (what the Raw parameter was formally doing).


### PowerShell as a default shell doesn't work with script command [#3319](https://github.com/PowerShell/PowerShell/issues/3319)
On Unix, it is a convention for shells to accept `-i` for an interactive shell and many tools expect this behavior (`script` for example, and when setting powershell as the default shell) and calls the shell with the `-i` switch. This change is breaking in that -i previously could be used as short hand to match `-inputformat` which now will need to be `-in`.


### Completions for environment variables differ between platforms [#3227](https://github.com/PowerShell/PowerShell/issues/3227)
The casing of `$PSModulePath` has been corrected to be consistent with Windows PowerShell.
TODO: not sure if this actually a breaking change.  If the change is to make something consistent with Windows, there would be no difference between 6.0 and 5.1


### Typo fix in Get-ComputerInfo property name [#3167](https://github.com/PowerShell/PowerShell/issues/3167)
`BiosSerialNumber` was misspelled as "BiosSeralNumber".


### Add Get-StringHash and Get-FileHash cmdlets [#3024](https://github.com/PowerShell/PowerShell/issues/3024)
TODO: Labeled with breaking change so the difference between windows and core can be described


### Add validation on get-* cmdlets where passing $null returns all objects instead of error [#2672](https://github.com/PowerShell/PowerShell/issues/2672)
Passing `$null` to any of the following will now throw an error: `Get-Credential -UserName`, `Get-Event -SourceIdentifier`, `Get-EventSubscriber -SourceIdentifier`, `Get-Help -Name`, `Get-PSBreakpoint -Script`, `Get-PSProvider -PSProvider`, `Get-PSSessionConfiguration -Name`, `Get-PSSnapin -Name`, `Get-Runspace -Name`, `Get-RunspaceDebug -RunspaceName`, `Get-Service -Name`, `Get-TraceSource -Name`, `Get-Variable -Name`, `Get-WmiObject -Class`, and `Get-WmiObject -Property`.


### Add support W3C Extended Log File Format in Import-Csv [#2482](https://github.com/PowerShell/PowerShell/issues/2482)
Previously, the `Iport-Csv` cmdlet cannot be used to directly import the log files in W3C extended log format and additional action would be required.
With this change, W3C extended log format is supported.


### Parameter binding problem with ValueFromRemainingArguments in PS functions [#2035](https://github.com/PowerShell/PowerShell/issues/2035)
TODO


### BuildVersion should be removed from $PSVersionTable [#1415](https://github.com/PowerShell/PowerShell/issues/1415)
Remove the `BuildVersion` property from `$PSVersionTable`. This property was strongly tied to the Windows build version. Instead, we recommend that you use `GitCommitId` to retrieve the exact build version of PowerShell Core.
