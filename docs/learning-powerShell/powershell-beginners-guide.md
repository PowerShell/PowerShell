PowerShell Beginners Guide
====

If you are new to PowerShell, this document will walk you through a few examples to give you some basic ideas of PowerShell. We recommend that you open a PowerShell console/session and type along with the instructions in this document to get most out of this exercise.


Launch PowerShell Console/Session
---
Follow the [Installing PowerShell](./learning-powershell.md "Installing PowerShell") instruction you can install the PowerShell and launch the PowerShell session.


Getting Familiar with PowerShell Commands
---

As mentioned above PowerShell commands is designed to have Verb-Noun structure, for instance Get-Process, Set-Location, Clear-Host, etc. Let’s exercise some of the basic PowerShell commands also known as **cmdlets**.

**1. Get-Process**: displays the processes running on your system.

By default, you will get data back similar to the following:
``` PowerShell
PS C:\>Get-Process

Handles   NPM(K)    PM(K)     WS(K)    VM(M)    CPU(s)  Id   ProcessName
-------  ------     -----     -----    -----    ------  --  -----------
    147      11     2108       9316    ...82     0.02   12    notepad
    174      12     4276       1460    ...53     3.56   309   cmd
    121       9     1580       3096    ...85     3.94   8620  svchost
   2168     374   448940     321504      820   867.39   1209  iexplore

…
```
Only interested in the instance of notepad that are running on your computer? Try this:
```PowerShell
PS C:\> Get-Process -Name notepad

Handles   NPM(K)    PM(K)     WS(K)   VM(M)    CPU(s)   Id   ProcessName
-------  ------     -----     -----   -----    ------   --  -----------
    147      11     2108       9316    ...82     0.02   12    notepad

```
Want to get back more than one process? Then just specify process names, separating with commas. For example,
```PowerShell
PS C:\> Get-Process -Name notepad, iexplore
Handles  NPM(K)    PM(K)      WS(K) VM(M)   CPU(s)     Id  ProcessName
-------  ------    -----      ----- -----   ------     --  -----------
   1753     247   418220     194960   991   385.97   1748   iexplore
    144      11     2152       9320 ...82     0.08  30004   notepad


```

**2. Clear-Host**: Clears the display in the command window
```PowerShell
PS C:\> Get-Process
PS C:\> Clear-Host
```
Type too much just for clearing the screen? Here is how the alias can help.

**3. Get-Alias**:  Improves the user experience by using the Cmdlet aliases

To find the available aliases, you can type below cmdlet:
```PowerShell
PS C:\> Get-Alias

CommandType     Name
-----------     ----
…
Alias           cat -> Get-Content
Alias           cd -> Set-Location
Alias           cls -> Clear-Host
Alias           cp -> Copy-Item
Alias           clv -> Clear-Variable
Alias           gmo -> Get-Module
Alias           man -> help
Alias           rm -> Remove-Item
Alias           ls -> Get-ChildItem
Alias           type -> Get-Content
…

As you can see "cls" is an alias of Clear-Host. Now try it:

PS C:\> Get-Process
PS C:\> cls
```
**4. cd - Set-Location**: change your current working directory
```PowerShell
PS C:\> Set-Location C:\test
PS C:\test>
```
**5. ls or dir - Get-ChildItem**: list all items in the specified location

```PowerShell
Get all files under the current directory:

PS C:\test> Get-ChildItem

Get all files under the current directory as well as its subdirectories:
PS C:\test> dir -Recurse

List all files with file extension "txt".

PS C:\test> ls –Path *.txt -Recurse -Force
```

**6. New-Item**: Create a file

```PowerShell
An empty file is created if you type the following:
PS C:\test> New-Item -Path c:\test\test.txt


    Directory: C:\test


Mode                LastWriteTime         Length Name
----                -------------         ------ ----
-a----         7/7/2016   7:17 PM              0 test.txt
```
You can use the **-value** parameter to add some data to your file. For example, the following command adds the phrase "Hello World" as a file content to the test.txt. Because the test.txt file exists already, we use **-force** parameter to replace the existing content.

```PowerShell
PS C:\test> New-Item -Path c:\test\test.txt -Value "Hello World!" -force

    Directory: C:\test


Mode                LastWriteTime         Length Name
----                -------------         ------ ----
-a----         7/7/2016   7:19 PM             12 test.txt

```
There are other ways to add some data to a file, for example, you can use Set-Content to set the file contents:

```PowerShell
PS C:\test>Set-Content -Path c:\test\test.txt -Value "Hello World too!"
```
Or simply use ">>" as below:
```
# create an empty file
"" > empty.txt  

# set "hello world!!!" as content of text.txt file
"hello world!!!" > test.txt

```
The pound sign (#) above is used for comments in PowerShell.

**7. type, cat - Get-Content**: get the content of an item

```PowerShell
PS C:\>Get-Content -Path "C:\Test\test.txt"
PS C:\>type -Path "C:\Test\test.txt"

Hello World!
```
**8. rm, del - Remove-Item**: delete a file or folder

This cmdlet will delete the file c:\test\test.txt:
```PowerShell
PS C:\test> Remove-Item c:\test\test.txt
```
**9. Exit**: - to exit the PowerShell session, type "exit"
```PowerShell
PS C:\test> exit
```

Need Help?
----
The most important command in PowerShell is possibly the Get-Help, which allows you to quickly learn PowerShell without having to surfing around the Internet. The Get-Help cmdlet also shows you how PowerShell commands work with examples.


PS C:\>**Get-Help**

You can use this cmdlet to get help with any PowerShell commands.

PS C:\>**Get-Help -Name Get-Process**

It shows the syntax and other technical information of the Get-Process cmdlet.


PS C:\>**Get-Help -Name Get-Process -Examples**

It displays the examples how to use the Get-Process cmdlet.
If you use **-full** parameter, i.e., "Get-Help -Name Get-Process -Full", it will display more technical information.



Discover All Commands Available on Your System
----

You want to discover what PowerShell cmdlets available on your system. Simple, just run "Get-Command" as below.

PS C:\> **Get-Command**

If you want to know whether a particular cmdlet exists on your system, you can do something like below:

PS C:\> **Get-Command Get-Process**

If you want to know the syntax of Get-Process cmdlet, type

PS C:\> **Get-Command Get-Process -Syntax**

If you want to know how to sue the get-process, type

PS C:\> **Get-Help Get-Process -example**


PowerShell Pipeline '|'
----
Sometimes when you run Get-ChildItem or "dir", you want to get a list of files in a descending order. To archive that, type:
```PowerShell
PS C:\> dir | sort -Descending
```
Say you want to get the largest file in a directory
```PowerShell
PS C:\> dir | sort -Property length -Descending | Select-Object -First 1


    Directory: C:\


Mode                LastWriteTime       Length  Name
----                -------------       ------  ----
-a----        5/16/2016   1:15 PM        32972  test.log

```
How to Create and Run PowerShell scripts
----
- You can use ISE, VS Code, or any favorite editor to create a PowerShell script and save the script with a .ps1 file extension (helloworld.ps1 in the example)
- To run the script, cd to your current folder and type .\helloworld.ps1

See [Running PowerShell Scripts Is as Easy as 1-2-3] [run-ps] for more details.

[run-ps]:http://windowsitpro.com/powershell/running-powershell-scripts-easy-1-2-3

More Reading
----
Books & eBooks & Blogs & Tutorials
- [Windows PowerShell in Action][in-action] by Bruce Payette
- [Windows PowerShell Cookbook][cookbook] by Lee Holmes
- [eBooks from PowerShell.org](https://powershell.org/ebooks/)
- [eBooks List][ebook-list] by Martin Schvartzman
-	[eBooks from PowerShell.com][ebooks-powershell.com]
- [Tutorial from MVP][tutorial]
-	Script Guy blog: [The best way to Learn PowerShell][to-learn]
-	[Understanding PowerShell Module][ps-module]
-	[How and When to Create PowerShell Module][create-ps-module] by Adam Bertram
-	Video: [Get Started with PowerShell Remoting][remoting] from Channel9
- Video: [PowerShell Remoting in Depth][in-depth] from Channel9
-	[PowerShell Basics: Remote Management][remote-mgmt] from ITPro
- [Running Remote Commands][remote-commands] from PowerShell Web Docs
- [Samples for PowerShell Scripts][examples]
- [Samples for Writing a PowerShell Script Module][examples-ps-module]
- [Writing a PowerShell module in C#][writing-ps-module]
- [Examples of Cmdlets Code][sample-code]


[in-action]: https://www.amazon.com/Windows-PowerShell-Action-Second-Payette/dp/1935182137
[cookbook]: http://shop.oreilly.com/product/9780596801519.do
[ebook-list]: https://blogs.technet.microsoft.com/pstips/2014/05/26/free-powershell-ebooks/
[ebooks-powershell.com]: http://powershell.com/cs/blogs/ebookv2/default.aspx
[tutorial]: http://www.computerperformance.co.uk/powershell/index.htm
[to-learn]:https://blogs.technet.microsoft.com/heyscriptingguy/2015/01/04/weekend-scripter-the-best-ways-to-learn-powershell/
[ps-module]:https://msdn.microsoft.com/en-us/library/dd878324%28v=vs.85%29.aspx
[create-ps-module]:http://www.tomsitpro.com/articles/powershell-modules,2-846.html
[remoting]:https://channel9.msdn.com/Series/GetStartedPowerShell3/06
[in-depth]: https://channel9.msdn.com/events/MMS/2012/SV-B406
[remote-mgmt]:http://windowsitpro.com/powershell/powershell-basics-remote-management
[remote-commands]:https://msdn.microsoft.com/en-us/powershell/scripting/core-powershell/running-remote-commands
[examples]:http://examples.oreilly.com/9780596528492/
[examples-ps-module]:https://msdn.microsoft.com/en-us/library/dd878340%28v=vs.85%29.aspx
[writing-ps-module]:http://www.powershellmagazine.com/2014/03/18/writing-a-powershell-module-in-c-part-1-the-basics/
[sample-code]:https://msdn.microsoft.com/en-us/library/ff602031%28v=vs.85%29.aspx
