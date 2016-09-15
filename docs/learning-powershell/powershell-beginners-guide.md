PowerShell Beginner’s Guide
====

If you are new to PowerShell, this document will walk you through a few examples to give you some basic ideas of PowerShell.
We recommend that you open a PowerShell console/session and type along with the instructions in this document to get most out of this exercise.


Launch PowerShell Console/Session
---
First you need to launch a PowerShell session by following the [Installing PowerShell Guide](./README.md#installing-powershell).


Getting Familiar with PowerShell Commands
---
In this section, you will learn how to
- create a file, delete a file and change file directory
- discover what version of PowerShell you are currently using
- exit a PowerShell session
- get help if you needed
- find syntax of PowerShell cmdlets
- and more

As mentioned above, PowerShell commands are designed to have Verb-Noun structure, for instance Get-Process, Set-Location, Clear-Host, etc.
Let’s exercise some of the basic PowerShell commands, also known as **cmdlets**.

Please note that we will use the PowerShell prompt sign **PS />** as it appears on Linux in the following examples.
It is shown as **PS C:\\>** on  Windows.

**1. Get-Process**: Gets the processes that are running on the local computer or a remote computer.

By default, you will get data back similar to the following:
``` PowerShell
PS /> Get-Process

Handles   NPM(K)    PM(K)     WS(K)     CPU(s)     Id    ProcessName
-------  ------     -----     -----     ------     --    -----------
    -      -          -           1      0.012     12    bash
    -      -          -          21     20.220    449    powershell
    -      -          -          11     61.630   8620    code
    -      -          -          74    403.150   1209    firefox

…
```
Only interested in the instance of firefox process that are running on your computer?
Try this:
```PowerShell
PS /> Get-Process -Name firefox

Handles   NPM(K)    PM(K)     WS(K)    CPU(s)     Id   ProcessName
-------  ------     -----     -----    ------     --   -----------
    -      -          -          74   403.150   1209   firefox

```
Want to get back more than one process?
Then just specify process names and separate them with commas.
```PowerShell
PS /> Get-Process -Name firefox, powershell
Handles   NPM(K)    PM(K)     WS(K)    CPU(s)     Id   ProcessName
-------  ------     -----     -----    ------     --   -----------
    -      -          -          74   403.150   1209   firefox
    -      -          -          21    20.220    449   powershell

```

**2. Clear-Host**: Clears the display in the host program.
```PowerShell
PS /> Get-Process
PS /> Clear-Host
```
Type too much just for clearing the screen?
Here is how the alias can help.

**3. Get-Alias**: Gets the aliases for the current session.
```PowerShell
PS /> Get-Alias

CommandType     Name
-----------     ----
…

Alias           cd -> Set-Location
Alias           cls -> Clear-Host
Alias           copy -> Copy-Item
Alias           dir -> Get-ChildItem
Alias           gc -> Get-Content
Alias           gmo -> Get-Module
Alias           ri -> Remove-Item
Alias           type -> Get-Content
…

As you can see "cls" is an alias of Clear-Host.
Now try it:

PS /> Get-Process
PS /> cls
```
**4. cd - Set-Location**: Sets the current working location to a specified location.
```PowerShell
PS /> Set-Location /home
PS /home>
```
**5. dir - Get-ChildItem**: Gets the items and child items in one or more specified locations.

```PowerShell
Get all files under the current directory:

PS /> Get-ChildItem

Get all files under the current directory as well as its subdirectories:
PS /> cd $home
PS /home/jen> dir -Recurse

List all files with "txt" file extension.

PS /> cd $home
PS /home/jen> dir –Path *.txt -Recurse
```

**6. New-Item**: Creates a new item.

```PowerShell
An empty file is created if you type the following:
PS /home/jen> New-Item -Path ./test.txt


    Directory: /home/jen


Mode                LastWriteTime         Length  Name
----                -------------         ------  ----
-a----         7/7/2016   7:17 PM              0  test.txt
```
You can use the **-Value** parameter to add some data to your file.
For example, the following command adds the phrase "Hello world!" as a file content to the test.txt.
Because the test.txt file exists already, we use **-Force** parameter to replace the existing content.

```PowerShell
PS /home/jen> New-Item -Path ./test.txt -Value "Hello world!" -Force

    Directory: /home/jen


Mode                LastWriteTime         Length  Name
----                -------------         ------  ----
-a----         7/7/2016   7:19 PM             24  test.txt

```
There are other ways to add some data to a file.
For example, you can use Set-Content to set the file contents:

```PowerShell
PS /home/jen>Set-Content -Path ./test.txt -Value "Hello world again!"
```
Or simply use ">" as below:
```
# create an empty file
"" > test.txt  

# set "Hello world!" as content of test.txt file
"Hello world!!!" > test.txt

```
The pound sign (#) above is used for comments in PowerShell.

**7. type - Get-Content**: Gets the content of the item at the specified location.

```PowerShell
PS /home/jen> Get-Content -Path ./test.txt
PS /home/jen> type -Path ./test.txt

Hello world again!
```
**8. del - Remove-Item**: Deletes the specified items.

This cmdlet will delete the file /home/jen/test.txt:
```PowerShell
PS /home/jen> Remove-Item ./test.txt
```

**9. $PSVersionTable**: Displays the version of PowerShell you are currently using.

Type **$PSVersionTable** in your PowerShell session, you will see something like below.
"PSVersion" indicates the PowerShell version that you are using.

```PowerShell
Name                           Value
----                           -----
PSVersion                      6.0.0-alpha
PSEdition                      Core
PSCompatibleVersions           {1.0, 2.0, 3.0, 4.0...}
BuildVersion                   3.0.0.0
GitCommitId                    v6.0.0-alpha.10
CLRVersion                     
WSManStackVersion              3.0
PSRemotingProtocolVersion      2.3
SerializationVersion           1.1.0.1

```

**10. Exit**: To exit the PowerShell session, type "exit".
```PowerShell
PS /home/jen> exit
```

Need Help?
----
The most important command in PowerShell is possibly the Get-Help, which allows you to quickly learn PowerShell without having to search around the internet.
The Get-Help cmdlet also shows you how PowerShell commands work with examples.

It shows the syntax and other technical information of the Get-Process cmdlet.
```PowerShell
PS /> Get-Help -Name Get-Process
```

It displays the examples how to use the Get-Process cmdlet.
```PowerShell
PS />Get-Help -Name Get-Process -Examples
```

If you use **-Full** parameter, for example, `Get-Help -Name Get-Process -Full`, it will display more technical information.


Discover Commands Available on Your System
----

You want to discover what PowerShell cmdlets available on your system? Just run "Get-Command" as below:
```PowerShell
PS /> Get-Command
```
If you want to know whether a particular cmdlet exists on your system, you can do something like below:
```PowerShell
PS /> Get-Command Get-Process
```
If you want to know the syntax of Get-Process cmdlet, type:
```PowerShell
PS /> Get-Command Get-Process -Syntax
```
If you want to know how to use the Get-Process, type:
```PowerShell
PS /> Get-Help Get-Process -Example
```

PowerShell Pipeline '|'
----
Sometimes when you run Get-ChildItem or "dir", you want to get a list of files and folders in a descending order.
To achieve that, type:
```PowerShell
PS /home/jen> dir | Sort-Object -Descending
```
Say you want to get the largest file in a directory
```PowerShell
PS /home/jen> dir | Sort-Object -Property Length -Descending | Select-Object -First 1


    Directory: /home/jen


Mode                LastWriteTime       Length  Name
----                -------------       ------  ----
-a----        5/16/2016   1:15 PM        32972  test.log

```
How to Create and Run PowerShell scripts
----
You can use Visual Studio Code or your favorite editor to create a PowerShell script and save it with a `.ps1` file extension.
For more details, see [Create and Run PowerShell Script Guide][create-run-script]


Recommended Training and Reading
----
-	Video: [Get Started with PowerShell][remoting] from Channel9
- [eBooks from PowerShell.org](https://powershell.org/ebooks/)
-	[eBooks from PowerShell.com][ebooks-powershell.com]
- [eBooks List][ebook-list] by Martin Schvartzman
- [Tutorial from MVP][tutorial]
-	Script Guy blog: [The best way to Learn PowerShell][to-learn]
-	[Understanding PowerShell Module][ps-module]
-	[How and When to Create PowerShell Module][create-ps-module] by Adam Bertram
- Video: [PowerShell Remoting in Depth][in-depth] from Channel9
-	[PowerShell Basics: Remote Management][remote-mgmt] from ITPro
- [Running Remote Commands][remote-commands] from PowerShell Web Docs
- [Samples for PowerShell Scripts][examples]
- [Samples for Writing a PowerShell Script Module][examples-ps-module]
- [Writing a PowerShell module in C#][writing-ps-module]
- [Examples of Cmdlets Code][sample-code]


Commercial Resources
----
- [Windows PowerShell in Action][in-action] by Bruce Payette
- [Windows PowerShell Cookbook][cookbook] by Lee Holmes

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
[create-run-script]:./create-powershell-scripts.md
