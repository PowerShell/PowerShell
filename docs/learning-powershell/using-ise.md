Using PowerShell Integrated Scripting Environment (ISE)
====
The PowerShell ISE only works on Windows. If you are not using Windows, please see [Using Visual Studio Code](./using-vscode.md).
Please note the ISE is not supported if you are using PowerShell installed from the [PowerShell package][get-powershell] or directly [built][build-powershell] from GitHub.

Editing with ISE
---
-	Launch PowerShell ISE
  *	 Press Windows Key -> type "PowerShell ISE", click on PowerShell ISE to launch the ISE
-	Create a new PowerShell Script
  *	Click on **File->New**
  *	Add a few lines of PowerShell code in your newly created file. In this case, we will use the following script snippet.

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
  * **Note**: You can find more examples [here](http://examples.oreilly.com/9780596528492/).

-	To save the script file
  *	Click on **File->Save As**. Then type "helloworld.ps1"
-	To close the helloworld.ps1 file
  *	**File->Close**
-	To reopen the helloworld.ps1 file
  *	**File->Open**, then choose helloworld.ps1
- For more details, go to [How to Write and Run Scripts in the Windows PowerShell ISE](https://msdn.microsoft.com/en-us/powershell/scripting/core-powershell/ise/how-to-write-and-run-scripts-in-the-windows-powershell-ise).


Debugging with ISE
----

To execute the entire script file, you can press **F5**. To execute several lines of your scripts, simply select them and press **F8**. If you would like to stop the execution on a particular line in order to examine some variables to check if the program runs as expected. In that case, you may follow the steps below. Let's take the helloworld.ps1 as an example and assume line 17 is the place where you want to stop.

-	Set a break point: Move mouse over on the line 17, and press **F9**. You will see the line 17 is highlighted, which means a breakpoint is set.
-	Press **F5** to run the script
-	Enter 80 (or any number in Fahrenheit) from the command line prompt
-	Notice that the ISE output pane becomes “[DBG]: PS C:\Test>>” prompt. This means the program is in the debugging mode. It stops at Line 17:

```PowerShell
PS C:\test> C:\test\helloword.ps1
Input a temperature in Fahrenheit: 80
Hit Line breakpoint on 'C:\test\helloword.ps1:17'
''[DBG]: PS C:\test>>'

```

- From the output pane, you can type $celsius and $fahrenheit to examine the values of these variables to see if they are correct.

```PowerShell
[DBG]: PS C:\Test>> $celsius
26.6666666666667

[DBG]: PS C:\Tset>> $fahrenheit
80
```
- Press **F5** to let the program continue running

```PowerShell
[DBG]: PS C:\test>>
27 Celsius

PS C:\test>
```
See [How to Debug in ISE][debug] for more information.

[debug]:https://msdn.microsoft.com/en-us/powershell/scripting/core-powershell/ise/how-to-debug-scripts-in-windows-powershell-ise#bkmk_2
[get-powershell]:../../README.md#get-powershell
[build-powershell]:../../README.md#building-the-repository
