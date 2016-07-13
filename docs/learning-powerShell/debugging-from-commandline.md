Debugging in PowerShell Command-line
=====

As we know we can debug PowerShell code via GUI tools like [VS Code](./using-vscode.md#debugging-with-vs-code) or [ISE](./using-ise.md#debugging-with-ise). In addition we can directly perform debugging within the PowerShell command-line session by using the PowerShell debugger cmdlets. This document demonstrates how to use the cmdlets for the PowerShell command-line debugging. We will cover two topics: set a debug breakpoint on a line of code and on a variable.

Let's use the following code snippet as our sample script.

```PowerShell
# Convert Fahrenheit to Celsius
function ConvertFahrenheitToCelsius([double] $fahrenheit)
{
$celsius = $fahrenheit - 32
$celsius = $celsius / 1.8
$celsius
}

$fahrenheit = Read-Host 'Input a temperature in Fahrenheit'
$result =[int](ConvertFahrenheitToCelsius($fahrenheit))
Write-Host "$result Celsius"

```


 **1. Set a Breakpoint on a Line**

- Open a [PowerShell editor](learning-powershell.md#powershell-editor)
- Save the above code snippet to a file, let's say "test.ps1"
- Clear existing breakpoints if any

```PowerShell
 Get-PSBreakpoint | Remove-PSBreakpoint
 ```
- Use **Set-PSBreakpoint** cmdlet to set a debug breakpoint, say set it to line 5

```PowerShell
PS C:\Test>Set-PSBreakpoint -Line 5 -Script .\test.ps1

ID Script             Line       Command          Variable          Action
-- ------             ----       -------          --------          ------
 5 test.ps1              5
```
- Run the script, test.ps1. As we have set a breakpoint, it is expected the program will break into the debugger at the line 5.

```PowerShell

PS C:\Test> .\test.ps1
Input a temperature in Fahrenheit: 80
Hit Line breakpoint on 'C:\Test\test.ps1:5'

At C:\Test\test.ps1:5 char:1
+ $celsius = $celsius / 1.8
+ ~~~~~~~~~~~~~~~~~~~~~~~~~
[DBG]: PS C:\Test>>
```

- The PowerShell prompt has been changed to **[DBG]: PS C:\Test>>** as you may noticed. This means
 we have entered into the debug mode. To watch the variables like $celsius, simply type $celsius as below.
- To exit from the debugging, type **"q"**
- To get help for the debugging commands, simple type **"?"**

```PowerShell
[DBG]: PS C:\Test>> $celsius
48
[DBG]: PS C:\Test>> $fahrenheit
80
[DBG]: PS PS C:\Test>> ?

 s, stepInto         Single step (step into functions, scripts, etc.)
 v, stepOver         Step to next statement (step over functions, scripts, etc.)
 o, stepOut          Step out of the current function, script, etc.

 c, continue         Continue operation
 q, quit             Stop operation and exit the debugger
 d, detach           Continue operation and detach the debugger.

 k, Get-PSCallStack  Display call stack

 l, list             List source code for the current script.
                     Use "list" to start from the current line, "list <m>"
                     to start from line <m>, and "list <m> <n>" to list <n>
                     lines starting from line <m>

 <enter>             Repeat last command if it was stepInto, stepOver or list

 ?, h                displays this help message.


For instructions about how to customize your debugger prompt, type "help about_prompt".

[DBG]: PS C:\Test>> s
At C:\Test\test.ps1:6 char:1
+ $celsius
+ ~~~~~~~~
[DBG]: PS C:\Test>> $celsius
26.6666666666667
[DBG]: PS C:\Test>> $fahrenheit
80

[DBG]: PS C:\Test>> q
PS C:\Test>

```


**2. Set a Breakpoint on a Variable**
- Clear existing breakpoints if any

```PowerShell
 PS C:\Test>Get-PSBreakpoint | Remove-PSBreakpoint
 ```
- Use **Set-PSBreakpoint** cmdlet to set a debug breakpoint, say set it to line 5

```PowerShell

 PS C:\Test>Set-PSBreakpoint -Variable "celsius" -Mode write -Script .\test.ps1

```

- Run the script, test.ps1.

  Once hit the debug breakpoint, we can type **l** to list the source code that debugger is currently executing. As we can see line 3 has an asterisk at the front, meaning that's the line the program is currently executing and broke into the debugger as illustrated below.
- Type **q** to exit from the debugging mode

```PowerShell

PS C:\Test> .\test.ps1
Input a temperature in Fahrenheit: 80
Hit Variable breakpoint on 'C:\Test\test.ps1:$celsius' (Write access)

At C:\Test\test.ps1:3 char:1
+ $celsius = $fahrenheit - 32
+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~
[DBG]: PS C:\Test>> l


    1:  function ConvertFahrenheitToCelsius([double] $fahrenheit)
    2:  {
    3:* $celsius = $fahrenheit - 32
    4:  $celsius = $celsius / 1.8
    5:  $celsius
    6:  }
    7:
    8:  $fahrenheit = Read-Host 'Input a temperature in Fahrenheit'
    9:  $result =[int](ConvertFahrenheitToCelsius($fahrenheit))
   10:  Write-Host "$result Celsius"


[DBG]: PS C:\Test>> $celsius
48
[DBG]: PS C:\Test>> v
At C:\Test\test.ps1:4 char:1
+ $celsius = $celsius / 1.8
+ ~~~~~~~~~~~~~~~~~~~~~~~~~
[DBG]: PS C:\Test>> v
Hit Variable breakpoint on 'C:\Test\test.ps1:$celsius' (Write access)

At C:\Test\test.ps1:4 char:1
+ $celsius = $celsius / 1.8
+ ~~~~~~~~~~~~~~~~~~~~~~~~~
[DBG]: PS C:\Test>> $celsius
26.6666666666667
[DBG]: PS C:\Test>> q
PS C:\Test>

```

Now you know the basics of the PowerShell debugging from PowerShell command-line. For further learning read the following articles.


More Reading
=====
- [about_Debuggers](https://technet.microsoft.com/en-us/library/hh847790.aspx)
- [PowerShell Debugging](https://blogs.technet.microsoft.com/heyscriptingguy/tag/debugging/)
