# Get-PSReadlineKeyHandler

## SYNOPSIS
Gets the key bindings for the PSReadline module.

## DESCRIPTION
Gets the key bindings for the PSReadline module.

If neither -Bound nor -Unbound is specified, returns all bound keys and unbound functions.

If -Bound is specified and -Unbound is not specified, only bound keys are returned.

If -Unound is specified and -Bound is not specified, only unbound keys are returned.

If both -Bound and -Unound are specified, returns all bound keys and unbound functions.

## PARAMETERS

### Bound [switch] = True

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Include functions that are bound.


### Unbound [switch] = True

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Include functions that are unbound.



## INPUTS
### None
You cannot pipe objects to Get-PSReadlineKeyHandler

## OUTPUTS
### Microsoft.PowerShell.KeyHandler

Returns one entry for each key binding (or chord) for bound functions and/or one entry for each unbound function



## RELATED LINKS

[about_PSReadline]()

# Get-PSReadlineOption

## SYNOPSIS
Returns the values for the options that can be configured.

## DESCRIPTION
Get-PSReadlineOption returns the current state of the settings that can be configured by Set-PSReadlineOption.

The object returned can be used to change PSReadline options.
This provides a slightly simpler way of setting syntax coloring options for multiple kinds of tokens.

## PARAMETERS


## INPUTS
### None
You cannot pipe objects to Get-PSReadlineOption

## OUTPUTS
### 





## RELATED LINKS

[about_PSReadline]()

# Set-PSReadlineKeyHandler

## SYNOPSIS
Binds or rebinds keys to user defined or PSReadline provided key handlers.

## DESCRIPTION
This cmdlet is used to customize what happens when a particular key or sequence of keys is pressed while PSReadline is reading input.

With user defined key bindings, you can do nearly anything that is possible from a PowerShell script.
Typically you might just edit the command line in some novel way, but because the handlers are just PowerShell scripts, you can do interesting things like change directories, launch programs, etc.

## PARAMETERS

### Chord [String[]]

```powershell
[Parameter(
  Mandatory = $true,
  Position = 0)]
```

The key or sequence of keys to be bound to a Function or ScriptBlock.
A single binding is specified with a single string.
If the binding is a sequence of keys, the keys are separated with a comma, e.g. "Ctrl+X,Ctrl+X".
Note that this parameter accepts multiple strings.
Each string is a separate binding, not a sequence of keys for a single binding.


### ScriptBlock [ScriptBlock]

```powershell
[Parameter(
  Mandatory = $true,
  Position = 1,
  ParameterSetName = 'Set 1')]
```

The ScriptBlock is called when the Chord is entered.
The ScriptBlock is passed one or sometimes two arguments.
The first argument is the key pressed (a ConsoleKeyInfo.)  The second argument could be any object depending on the context.


### BriefDescription [String]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

A brief description of the key binding.
Used in the output of cmdlet Get-PSReadlineKeyHandler.


### Description [String]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

A more verbose description of the key binding.
Used in the output of the cmdlet Get-PSReadlineKeyHandler.


### Function [String]

```powershell
[Parameter(
  Mandatory = $true,
  Position = 1,
  ParameterSetName = 'Set 2')]
```

The name of an existing key handler provided by PSReadline.
This parameter allows one to rebind existing key bindings or to bind a handler provided by PSReadline that is currently unbound.

Using the ScriptBlock parameter, one can achieve equivalent functionality by calling the method directly from the ScriptBlock.
This parameter is preferred though - it makes it easier to determine which functions are bound and unbound.



## INPUTS
### None
You cannot pipe objects to Set-PSReadlineKeyHandler

## OUTPUTS
### 




## EXAMPLES
### --------------  Example 1  --------------

```powershell
PS C:\> Set-PSReadlineKeyHandler -Key UpArrow -Function HistorySearchBackward
```
This command binds the up arrow key to the function HistorySearchBackward which will use the currently entered command line as the beginning of the search string when searching through history.
### --------------  Example 2  --------------

```powershell
PS C:\> Set-PSReadlineKeyHandler -Chord Shift+Ctrl+B -ScriptBlock {
    [PSConsoleUtilities.PSConsoleReadLine]::RevertLine()
    [PSConsoleUtilities.PSConsoleReadLine]::Insert('build')
>>>     [PSConsoleUtilities.PSConsoleReadLine]::AcceptLine()
}
```
This example binds the key Ctrl+Shift+B to a script block that clears the line, inserts build, then accepts the line.
This example shows how a single key can be used to execute a command.

## RELATED LINKS

[about_PSReadline]()

# Set-PSReadlineOption

## SYNOPSIS
Customizes the behavior of command line editing in PSReadline.

## DESCRIPTION
The Set-PSReadlineOption cmdlet is used to customize the behavior of the PSReadline module when editing the command line.

## PARAMETERS

### EditMode [EditMode] = Windows

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the command line editing mode.
This will reset any key bindings set by Set-PSReadlineKeyHandler.

Valid values are:

-- Windows: Key bindings emulate PowerShell/cmd with some bindings emulating Visual Studio.

-- Emacs: Key bindings emulate Bash or Emacs.


### ContinuationPrompt [String] = >>> 

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the string displayed at the beginning of the second and subsequent lines when multi-line input is being entered.
Defaults to '\>\>\> '.
The empty string is valid.


### ContinuationPromptForegroundColor [ConsoleColor]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the foreground color of the continuation prompt.


### ContinuationPromptBackgroundColor [ConsoleColor]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the background color of the continuation prompt.


### EmphasisForegroundColor [ConsoleColor] = Cyan

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the foreground color used for emphasis, e.g.
to highlight search text.


### EmphasisBackgroundColor [ConsoleColor]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the background color used for emphasis, e.g.
to highlight search text.


### ErrorForegroundColor [ConsoleColor] = Red

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the foreground color used for errors.


### ErrorBackgroundColor [ConsoleColor]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the background color used for errors.


### HistoryNoDuplicates [switch]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies that duplicate commands should not be added to PSReadline history.


### AddToHistoryHandler [Func[String, Boolean]]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies a ScriptBlock that can be used to control which commands get added to PSReadline history.


### ValidationHandler [Func[String, Object]]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies a ScriptBlock that is called from ValidateAndAcceptLine.
If a non-null object is returned or an exception is thrown, validation fails and the error is reported.
If the object returned/thrown has a Message property, it's value is used in the error message, and if there is an Offset property, the cursor is moved to that offset after reporting the error.
If there is no Message property, the ToString method is called to report the error.


### HistorySearchCursorMovesToEnd [switch]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```




### MaximumHistoryCount [Int32] = 1024

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the maximum number of commands to save in PSReadline history.
Note that PSReadline history is separate from PowerShell history.


### MaximumKillRingCount [Int32] = 10

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the maximum number of items stored in the kill ring.


### ResetTokenColors [switch]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Restore the token colors to the default settings.


### ShowToolTips [switch]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

When displaying possible completions, show tooltips in the list of completions.


### ExtraPromptLineCount [Int32] = 0

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Use this option if your prompt spans more than one line and you want the extra lines to appear when PSReadline displays the prompt after showing some output, e.g.
when showing a list of completions.


### DingTone [Int32] = 1221

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

When BellStyle is set to Audible, specifies the tone of the beep.


### DingDuration [Int32] = 50ms

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

When BellStyle is set to Audible, specifies the duration of the beep.


### BellStyle [BellStyle] = Audible

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies how PSReadLine should respond to various error and ambiguous conditions.

Valid values are:

-- Audible: a short beep

-- Visible: a brief flash is performed

-- None: no feedback


### CompletionQueryItems [Int32] = 100

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the maximum number of completion items that will be shown without prompting.
If the number of items to show is greater than this value, PSReadline will prompt y/n before displaying the completion items.


### WordDelimiters [string] = ;:,.[]{}()/\|^&*-=+

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the characters that delimit words for functions like ForwardWord or KillWord.


### HistorySearchCaseSensitive [switch]

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the searching history is case sensitive in functions like ReverseSearchHistory or HistorySearchBackward.


### HistorySaveStyle [HistorySaveStyle] = SaveIncrementally

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies how PSReadLine should save history.

Valid values are:

-- SaveIncrementally: save history after each command is executed - and share across multiple instances of PowerShell

-- SaveAtExit: append history file when PowerShell exits

-- SaveNothing: don't use a history file


### HistorySavePath [String] = ~\AppData\Roaming\PSReadline\$($host.Name)_history.txt

```powershell
[Parameter(ParameterSetName = 'Set 1')]
```

Specifies the path to the history file.


### TokenKind [TokenClassification]

```powershell
[Parameter(
  Mandatory = $true,
  Position = 0,
  ParameterSetName = 'Set 2')]
```

Specifies the kind of token when setting token coloring options with the -ForegroundColor and -BackgroundColor parameters.


### ForegroundColor [ConsoleColor]

```powershell
[Parameter(
  Position = 1,
  ParameterSetName = 'Set 2')]
```

Specifies the foreground color for the token kind specified by the parameter -TokenKind.


### BackgroundColor [ConsoleColor]

```powershell
[Parameter(
  Position = 2,
  ParameterSetName = 'Set 2')]
```

Specifies the background color for the token kind specified by the parameter -TokenKind.



## INPUTS
### None
You cannot pipe objects to Set-PSReadlineOption


## OUTPUTS
### None
This cmdlet does not generate any output.




## RELATED LINKS

[about_PSReadline]()


