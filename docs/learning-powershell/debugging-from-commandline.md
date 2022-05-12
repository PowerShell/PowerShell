# Debugging in PowerShell Command-line

As we know, we can debug PowerShell code via GUI tools like [Visual Studio Code](https://docs.microsoft.com/powershell/scripting/dev-cross-plat/vscode/using-vscode#debugging-with-visual-studio-code). In addition, we can
directly perform debugging within the PowerShell command-line session by using the PowerShell debugger cmdlets. This document demonstrates how to use the cmdlets for the PowerShell command-line debugging. We will cover the following topics:
setting a debug breakpoint on a line of code and on a variable.

Let's use the following code snippet as our sample script.

```powershell
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

## Setting a Breakpoint on a Line

- Open a [PowerShell editor](README.md#powershell-editor)
- Save the above code snippet to a file. For example, "test.ps1"
- Go to your command-line PowerShell
- Clear existing breakpoints if any

```powershell
 PS /home/jen/debug>Get-PSBreakpoint | Remove-PSBreakpoint
```

- Use **Set-PSBreakpoint** cmdlet to set a debug breakpoint. In this case, we will set it to line 5

```powershell
PS /home/jen/debug>Set-PSBreakpoint -Line 5 -Script ./test.ps1

ID Script             Line       Command          Variable          Action
-- ------             ----       -------          --------          ------
 0 test.ps1              5
```

- Run the script "test.ps1". As we have set a breakpoint, it is expected the program will break into the debugger at the line 5.

```powershell

PS /home/jen/debug> ./test.ps1
Input a temperature in Fahrenheit: 80
Hit Line breakpoint on '/home/jen/debug/test.ps1:5'

At /home/jen/debug/test.ps1:5 char:1
+ $celsius = $celsius / 1.8
+ ~~~~~~~~~~~~~~~~~~~~~~~~~
[DBG]: PS /home/jen/debug>>
```

- The PowerShell prompt now has the prefix **[DBG]:** as you may have noticed. This means
 we have entered into the debug mode. To watch the variables like $celsius, simply type **$celsius** as below.
- To exit from the debugging, type **q**
- To get help for the debugging commands, simply type **?**. The following is an example of debugging output.

```PowerShell
[DBG]: PS /home/jen/debug>> $celsius
48
[DBG]: PS /home/jen/debug>> $fahrenheit
80
[DBG]: PS /home/jen/debug>> ?

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

[DBG]: PS /home/jen/debug>> s
At PS /home/jen/debug/test.ps1:6 char:1
+ $celsius
+ ~~~~~~~~
[DBG]: PS /home/jen/debug>> $celsius
26.6666666666667
[DBG]: PS /home/jen/debug>> $fahrenheit
80

[DBG]: PS /home/jen/debug>> q
PS /home/jen/debug>

```

## Setting a Breakpoint on a Variable
- Clear existing breakpoints if there are any

```powershell
 PS /home/jen/debug>Get-PSBreakpoint | Remove-PSBreakpoint
 ```

- Use **Set-PSBreakpoint** cmdlet to set a debug breakpoint. In this case, we set it to line 5

```powershell

 PS /home/jen/debug>Set-PSBreakpoint -Variable "celsius" -Mode write -Script ./test.ps1

```

- Run the script "test.ps1"

  Once hit the debug breakpoint, we can type **l** to list the source code that debugger is currently executing. As we can see line 3 has an asterisk at the front, meaning that's the line the program is currently executing and broke into the debugger as illustrated below.
- Type **q** to exit from the debugging mode. The following is an example of debugging output.

```powershell
./test.ps1
Input a temperature in Fahrenheit: 80
Hit Variable breakpoint on '/home/jen/debug/test.ps1:$celsius' (Write access)

At /home/jen/debug/test.ps1:3 char:1
+ $celsius = $fahrenheit - 32
+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~
[DBG]: PS /home/jen/debug>> l


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


[DBG]: PS /home/jen/debug>> $celsius
48
[DBG]: PS /home/jen/debug>> v
At /home/jen/debug/test.ps1:4 char:1
+ $celsius = $celsius / 1.8
+ ~~~~~~~~~~~~~~~~~~~~~~~~~
[DBG]: PS /home/jen/debug>> v
Hit Variable breakpoint on '/home/jen/debug/test.ps1:$celsius' (Write access)

At /home/jen/debug/test.ps1:4 char:1
+ $celsius = $celsius / 1.8
+ ~~~~~~~~~~~~~~~~~~~~~~~~~
[DBG]: PS /home/jen/debug>> $celsius
26.6666666666667
[DBG]: PS /home/jen/debug>> q
PS /home/jen/debug>

```

Now you know the basics of the PowerShell debugging from PowerShell command-line. For further learning, read the following articles.

## More Reading

- [about_Debuggers](https://docs.microsoft.com/powershell/module/microsoft.powershell.core/about/about_debuggers)
- [PowerShell Debugging](https://blogs.technet.microsoft.com/heyscriptingguy/tag/debugging/)
