# Breaking Changes for PowerShell 6.0

## Explicit Removal of Support

### Workflow

### Custom Snap-Ins

### WMI v1 cmdlets
(Get-WmiObject, invoke-WmiMethod, etc.)

### RunspaceConfiguration

## Engine/language changes

### Rename `powershell.exe` to `pwsh.exe` [#5101](https://github.com/PowerShell/PowerShell/issues/5101)

In order to give users a deterministic way to call PowerShell Core on Windows (as opposed to Windows PowerShell),
the PowerShell Core binary was changed to `pwsh.exe` on Windows and `pwsh` on non-Windows platforms.

The shortened name is also consistent with naming of shells on non-Windows platforms.

### Don't insert line breaks to output (except for tables) [#5193](https://github.com/PowerShell/PowerShell/issues/5193)
Previously, output was aligned to the width of the console and line breaks were added at the end width of the console,
meaning the output didn't get reformatted as expected if the terminal was resized.
This change was not applied to tables, as the line breaks are necessary to keep the columns aligned.

### Skip null-element check for collections with a value-type element type [#5432](https://github.com/PowerShell/PowerShell/issues/5432)
For the `Mandatory` parameter and `ValidateNotNull` and `ValidateNotNullOrEmpty` attributes, skip the null-element check if the collection's element type is value type.  

### Change `$OutputEncoding` to use `UTF-8 NoBOM` encoding rather than ASCII [#5369](https://github.com/PowerShell/PowerShell/issues/5369)
The previous encoding, ASCII (7-bit), would result in incorrect alteration of the output in some cases.  This change is to make `UTF-8 NoBOM` default, which preserves unicode output with an encoding supported by most tools and operating systems.

### Remove `AllScope` from most default aliases [#5268](https://github.com/PowerShell/PowerShell/issues/5268)
To speed up scope creation, `AllScope` was removed from most default aliases.
`AllScope` was left for a few frequently used aliases where the lookup was faster.

### `-Verbose` and `-Debug` no longer overrides `$ErrorActionPreference` [#5113](https://github.com/PowerShell/PowerShell/issues/5113)

Previously, if `-Verbose` or `-Debug` were specified, it overrode the behavior of `$ErrorActionPreference`.
With this change, `-Verbose` and `-Debug` no longer affect the behavior of `$ErrorActionPreference`.


## Cmdlet changes

### Invoke-RestMethod doesn't return useful info when no data is returned. [#5320](https://github.com/PowerShell/PowerShell/issues/5320)
When an API returns just `null`, Invoke-RestMethod was serializing this as the string `"null"` instead of `$null`.  This change fixes the logic in `Invoke-RestMethod` to properly serialize a valid single value JSON `null` literal as `$null`.

### Remove `-ComputerName` from `\*-Computer` cmdlets [#5277](https://github.com/PowerShell/PowerShell/issues/5277)

Due to issues with RPC remoting in CoreFx (particularly on non-Windows platforms) and ensuring a consistent remoting experience in PowerShell,
the `-ComputerName` parameter was removed from the `\*-Computer` cmdlets. 
Use `Invoke-Command` instead as the way to execute cmdlets remotely.

### Remove `-ComputerName` from `*-Service` cmdlets [#5090](https://github.com/PowerShell/PowerShell/issues/5094)

In order to encourage the consistent use of PSRP, the `-ComputerName` parameter was removed from `*-Service` cmdlets.

### Fix `Get-Item -LiteralPath a*b` if `a*b` doesn't actually exist to return error [#5197](https://github.com/PowerShell/PowerShell/issues/5197)

Previously, `-LiteralPath` given a wildcard would treat it the same as `-Path` and if the wildcard found no files, it would silently exit.
Correct behavior should be that `-LiteralPath` is literal so if the file doesn't exist, it should error.
Change is to treat wildcards used with `-Literal` as literal.

### `Import-Csv` should apply `PSTypeNames` upon import when type information is present in the CSV [#5134](https://github.com/PowerShell/PowerShell/issues/5134)

Previously, objects exported using `Export-CSV` with TypeInformation imported with `ConvertFrom-Csv` were not retaining the type information.
This change adds the type information to PSTypeNames member if available from the csv file.


### `-NoTypeInformation` should be default on `Export-Csv` [#5131](https://github.com/PowerShell/PowerShell/issues/5131)

This change was made to address customer feedback on the default behavior of `Export-CSV` to include type information.

Previously, the cmdlet would output a comment as the first line containing the type name of the object.  
The change is to suppress this by default as it's not understood by most tools.  
Use `-IncludeTypeInformation` to retain the previous behavior.


### Web Cmdlets should warn when `-Credential` is sent over unencrypted connections [#5112](https://github.com/PowerShell/PowerShell/issues/5112)

When using HTTP, content including passwords are sent as clear-text.
This change is to not allow this by default and return an error if credentials are being passed in an insecure manner.
Users can bypass this by using the `-AllowUnencryptedAuthethentication` switch.


## API changes

### Remove `AddTypeCommandBase` class [#5407](https://github.com/PowerShell/PowerShell/issues/5407)
The `AddTypeCommandBase` class was removed from `Add-Type` to improve performance.
This class is only used by the Add-Type cmdlet and should not impact users.


### Unify cmdlets with parameter `-Encoding` to be of type `System.Text.Encoding` [#5080](https://github.com/PowerShell/PowerShell/issues/5080)

The `-Encoding` value `Byte` has been removed from the filesystem provider cmdlets.
A new parameter, `-AsByteStream`, is now used to specify that a byte stream is required as input or that the output is a stream of bytes.


### Add better error message for empty and null -UFormat arg [#5055](https://github.com/PowerShell/PowerShell/issues/5055)
Previously, when passing an empty format string to `-UFormat`, an unhelpful error message would appear.  A more descriptive error has been added.


### Clean up console code [#4995](https://github.com/PowerShell/PowerShell/issues/4995)
The following features were removed as they are not supported in PowerShell Core,
and there are no plans to add support as they exist for legacy reasons for Windows PowerShell:
psconsolefile switch and code, importsystemmodulues switch and code, and font changing code.


### Removed RunspaceConfiguration support [#4942](https://github.com/PowerShell/PowerShell/issues/4942)
Previously, when creating a PowerShell runspace programmatically using the api you could use the legacy RunspaceConfiguration or the newer InitialSessionState.
This change removed support for RunspaceConfiguration and only supports InitialSessionState.


### CommandInvocationIntrinsics.InvokeScript bind arguments to $input instead of $args [#4923](https://github.com/PowerShell/PowerShell/issues/4923)
An incorrect position of a parameter resulted in the args passed as input instead of as args.


### Remove unsupported `-showwindow` switch from `Get-Help` [#4903](https://github.com/PowerShell/PowerShell/issues/4903)
`-showwindow` relies on WPF, which is not supported on CoreCLR.


### Allow * to be used in registry path for `Remove-Item` [#4866](https://github.com/PowerShell/PowerShell/issues/4866)

Previously, `-LiteralPath` given a wildcard would treat it the same as `-Path` and if the wildcard found no files, it would silently exit.
Correct behavior should be that `-LiteralPath` is literal so if the file doesn't exist, it should error.
Change is to treat wildcards used with `-Literal` as literal.


### Fix `Set-Service` failing test [#4802](https://github.com/PowerShell/PowerShell/issues/4802)
Previously, if `New-Service -StartupType foo` was used, `foo` was ignored and the service was created with some default startup type.
This change is to explicitly throw an error for an invalid startup type.

### Rename $IsOSX to $IsMacOS [#4700](https://github.com/PowerShell/PowerShell/issues/4700)
The naming in PowerShell should be consistent with our naming and conform to Apple's use of macOS instead of OSX.
However, for readability and consistently we are staying with Pascal casing.

### Make error message consistent when invalid script is passed to -File, better error when passed ambiguous arg [#4573](https://github.com/PowerShell/PowerShell/issues/4573)
Change the exit codes of pwsh.exe to align with Unix conventions


### Removal of `LocalAccount` and cmdlets from  `Diagnostics` modules. [#4302](https://github.com/PowerShell/PowerShell/issues/4302) [#4303](https://github.com/PowerShell/PowerShell/issues/4303)
Due to unsupported APIs, the `LocalAccounts` module and the `Counter` cmdlets in the `Diagnostics` module were removed until a better solution is found. 

### Executing powershell script with bool parameter does not work [#4036](https://github.com/PowerShell/PowerShell/issues/4036)
Previously, using powershell.exe (now pwsh.exe) to execute a PowerShell script using `-File` provided no way to pass $true/$false as parameter values.
Support for $true/$false as parsed values to parameters was added. 
Switch values are also supported as currently documented syntax doesn't work.


### Remove ClrVersion property from $PSVersionTable [#4027](https://github.com/PowerShell/PowerShell/issues/4027)
The ClrVersion property of `$PSVersionTable` is not useful with CoreCLR, end users should not be using that value to determine compatibility.


### Change positional parameter for powershell.exe from -Command to -File [#4019](https://github.com/PowerShell/PowerShell/issues/4019)
Enable shebang use of PowerShell on non-Windows platforms. This means on Unix based systems,
you can make a script executable that would invoke PowerShell automatically rather than explicitly invoking pwsh.
This also means that you can now do things like `powershell foo.ps1` or `powershell fooScript` without specifying `-File`.
However, this change now requires that you explicitly specify `-c` or `-Command` when trying to do things like `powershell.exe Get-Command`.

### Implement Unicode escape parsing [#3958](https://github.com/PowerShell/PowerShell/issues/3958)
\`u#### or \`u{####} is converted to the corresponding Unicode character.
To output a literal `u, escape the backtick: ``u.


### Change New-ModuleManifest encoding to UTF8NoBOM on non-Windows platforms [#3940](https://github.com/PowerShell/PowerShell/issues/3940)
Previously, `New-ModuleManifest` creates psd1 manifests in UTF-16 with BOM, creating a problem for Linux tools.  This breaking change changes the encoding of `New-ModuleManifest` to be UTF (no BOM) in non-Windows platforms.


### Prevent `Get-ChildItem` from recursing into symlinks (#1875). [#3780](https://github.com/PowerShell/PowerShell/issues/3780)
This change brings `Get-ChildItem` more in line with the Unix `ls -r` and the Windows `dir /s` native commands.
Like the mentioned commands, the cmdlet displays symbolic links to directories found during recursion, but does not recurse into them.


### Fix `Get-Content -Delimiter` to not include the delimiter in the returned lines [#3706](https://github.com/PowerShell/PowerShell/issues/3706)
Previously, the output while using `Get-Content -Delimiter` was inconsistent and inconvenient as it required further processing of the data to remove the delimiter.
This change removes the delimiter in returned lines.


### Implement Format-Hex in C# [#3320](https://github.com/PowerShell/PowerShell/issues/3320)
 The `-raw` parameter is now No-op. Going forward all of the output will be displayed with a true representation of numbers that includes all of the bytes for its type (what the Raw parameter was formally doing).


### PowerShell as a default shell doesn't work with script command [#3319](https://github.com/PowerShell/PowerShell/issues/3319)
On Unix, it is a convention for shells to accept `-i` for an interactive shell and many tools expect this behavior (`script` for example, and when setting powershell as the default shell) and calls the shell with the `-i` switch. This change is breaking in that `-i` previously could be used as short hand to match `-inputformat`, which now needs to be `-in`.


### Typo fix in Get-ComputerInfo property name [#3167](https://github.com/PowerShell/PowerShell/issues/3167)
`BiosSerialNumber` was misspelled as `BiosSeralNumber`.


### Add Get-StringHash and Get-FileHash cmdlets [#3024](https://github.com/PowerShell/PowerShell/issues/3024)
This change is that some hash algorithms are not supported by corefx, therefore they are no longer available:
MACTripleDES, RIPEMD160


### Add validation on get-* cmdlets where passing $null returns all objects instead of error [#2672](https://github.com/PowerShell/PowerShell/issues/2672)
Passing `$null` to any of the following now throws an error: `Get-Credential -UserName`, `Get-Event -SourceIdentifier`, `Get-EventSubscriber -SourceIdentifier`, `Get-Help -Name`, `Get-PSBreakpoint -Script`, `Get-PSProvider -PSProvider`, `Get-PSSessionConfiguration -Name`, `Get-PSSnapin -Name`, `Get-Runspace -Name`, `Get-RunspaceDebug -RunspaceName`, `Get-Service -Name`, `Get-TraceSource -Name`, `Get-Variable -Name`, `Get-WmiObject -Class`, and `Get-WmiObject -Property`.


### Add support W3C Extended Log File Format in `Import-Csv` [#2482](https://github.com/PowerShell/PowerShell/issues/2482)
Previously, the `Import-Csv` cmdlet cannot be used to directly import the log files in W3C extended log format and additional action would be required.
With this change, W3C extended log format is supported.


### Parameter binding problem with ValueFromRemainingArguments in PS functions [#2035](https://github.com/PowerShell/PowerShell/issues/2035)
ValueFromRemainingArguments now returns the values as an array instead of a single value which itself is an array.


### BuildVersion should be removed from $PSVersionTable [#1415](https://github.com/PowerShell/PowerShell/issues/1415)
Remove the `BuildVersion` property from `$PSVersionTable`. This property was tied to the Windows build version. Instead, we recommend that you use `GitCommitId` to retrieve the exact build version of PowerShell Core.
