# Learning PowerShell

Whether you're a Developer, a DevOps or an IT Professional, this doc will help you getting started with PowerShell.
In this document we'll cover the following:
installing PowerShell, samples walkthrough, PowerShell editor, debugger, testing tools and a map book for experienced bash users to get started with PowerShell faster.

The exercises in this document are intended to give you a solid foundation in how to use PowerShell.
You won't be a PowerShell guru at the end of reading this material but you will be well on your way with the right set of knowledge to start using PowerShell.

If you have 30 minutes now, let’s try it.

## Installing PowerShell

First you need to set up your computer working environment if you have not done so.
Choose the platform below and follow the instructions.
At the end of this exercise, you should be able to launch the PowerShell session.

- Get PowerShell by installing package
    * [PowerShell on Linux][inst-linux]
    * [PowerShell on macOS][inst-macos]
    * [PowerShell on Windows][inst-win]

  For this tutorial, you do not need to install PowerShell if you are running on Windows.
  You can launch PowerShell console by pressing Windows key, typing PowerShell, and clicking on Windows PowerShell.
  However if you want to try out the latest PowerShell, follow the [PowerShell on Windows][inst-win].

- Alternatively you can get the PowerShell by [building it][build-powershell]

[build-powershell]:../../README.md#building-the-repository
[inst-linux]: https://docs.microsoft.com/powershell/scripting/install/installing-powershell-core-on-linux
[inst-win]: https://docs.microsoft.com/powershell/scripting/install/installing-powershell-core-on-windows
[inst-macos]: https://docs.microsoft.com/powershell/scripting/install/installing-powershell-core-on-macos

## Getting Started with PowerShell

PowerShell commands follow a Verb-Noun semantic with a set of parameters.
It's easy to learn and use PowerShell.
For example, `Get-Process` will display all the running processes on your system.
Let's walk through with a few examples from the [PowerShell Beginner's Guide](powershell-beginners-guide.md).

Now you have learned the basics of PowerShell.
Please continue reading if you want to do some development work in PowerShell.

### PowerShell Editor

In this section, you will create a PowerShell script using a text editor.
You can use your favorite editor to write scripts.
We use Visual Studio Code (VS Code) which works on Windows, Linux, and macOS.
Click on the following link to create your first PowerShell script.

- [Using Visual Studio Code (VS Code)](https://docs.microsoft.com/powershell/scripting/dev-cross-plat/vscode/using-vscode)

### PowerShell Debugger

Debugging can help you find bugs and fix problems in your PowerShell scripts.
Click on the link below to learn more about debugging:

- [Using Visual Studio Code (VS Code)](https://docs.microsoft.com/powershell/scripting/dev-cross-plat/vscode/using-vscode#debugging-with-visual-studio-code)
- [PowerShell Command-line Debugging][cli-debugging]

[cli-debugging]:./debugging-from-commandline.md

### PowerShell Testing

We recommend using Pester testing tool which is initiated by the PowerShell Community for writing test cases.
To use the tool please read [Pester Guides](https://github.com/pester/Pester) and [Writing Pester Tests Guidelines](https://github.com/PowerShell/PowerShell/blob/master/docs/testing-guidelines/WritingPesterTests.md).

### Map Book for Experienced Bash users

The table below lists the usage of some basic commands to help you get started on PowerShell faster.
Note that all bash commands should continue working on PowerShell session.

| Bash                            | PowerShell                              | Description
|:--------------------------------|:----------------------------------------|:---------------------
| ls                              | dir, Get-ChildItem                      | List files and folders
| tree                            | dir -Recurse, Get-ChildItem -Recurse    | List all files and folders
| cd                              | cd, Set-Location                        | Change directory
| pwd                             | pwd, $pwd, Get-Location                 | Show working directory
| clear, Ctrl+L, reset            | cls, clear                              | Clear screen
| mkdir                           | New-Item -ItemType Directory            | Create a new folder
| touch test.txt                  | New-Item -Path test.txt                 | Create a new empty file
| cat test1.txt test2.txt         | Get-Content test1.txt, test2.txt        | Display files contents
| cp ./source.txt ./dest/dest.txt | Copy-Item source.txt dest/dest.txt      | Copy a file
| cp -r ./source ./dest           | Copy-Item ./source ./dest -Recurse      | Recursively copy from one folder to another
| mv ./source.txt ./dest/dest.txt | Move-Item ./source.txt ./dest/dest.txt  | Move a file to other folder
| rm test.txt                     | Remove-Item test.txt                    | Delete a file
| rm -r &lt;folderName>           | Remove-Item &lt;folderName> -Recurse    | Delete a folder
| find -name build*               | Get-ChildItem build* -Recurse           | Find a file or folder starting with 'build'
| grep -Rin "sometext" --include="*.cs" |Get-ChildItem -Recurse -Filter *.cs <br> \| Select-String -Pattern "sometext" | Recursively case-insensitive search for text in files
| curl https://github.com         | Invoke-RestMethod https://github.com    | Transfer data to or from the web

### Recommended Training and Reading

- Microsoft Virtual Academy: [Getting Started with PowerShell][getstarted-with-powershell]
- [Why Learn PowerShell][why-learn-powershell] by Ed Wilson
- PowerShell Web Docs: [Basic cookbooks][basic-cookbooks]
- [The Guide to Learning PowerShell][ebook-from-Idera] by Tobias Weltner
- [PowerShell-related Videos][channel9-learn-powershell] on Channel 9
- [PowerShell Quick Reference Guides][quick-reference] by PowerShellMagazine.com
- [PowerShell Tips][idera-powershell-tips] from Idera
- [PowerShell 5 How-To Videos][script-guy-how-to] by Ed Wilson
- [PowerShell Documentation](https://docs.microsoft.com/powershell)
- [Interactive learning with PSKoans](https://aka.ms/pskoans)

### Commercial Resources

- [Windows PowerShell in Action][in-action] by [Bruce Payette](https://github.com/brucepay)
- [Introduction to PowerShell][powershell-intro] from Pluralsight
- [PowerShell Training and Tutorials][lynda-training] from Lynda.com
- [Learn Windows PowerShell in a Month of Lunches][learn-win-powershell] by Don Jones and Jeffrey Hicks
- [Learn PowerShell in a Month of Lunches][learn-powershell] by Travis Plunk (@TravisEz13),
  Tyler Leonhardt (@tylerleonhardt), Don Jones, and Jeffery Hicks

[in-action]: https://www.amazon.com/Windows-PowerShell-Action-Second-Payette/dp/1935182137
[powershell-intro]: https://www.pluralsight.com/courses/powershell-intro
[lynda-training]: https://www.lynda.com/PowerShell-training-tutorials/5779-0.html
[learn-win-powershell]: https://www.amazon.com/Learn-Windows-PowerShell-Month-Lunches/dp/1617294160
[learn-powershell]: https://www.manning.com/books/learn-powershell-in-a-month-of-lunches

[getstarted-with-powershell]: https://channel9.msdn.com/Series/GetStartedPowerShell3
[why-learn-powershell]: https://blogs.technet.microsoft.com/heyscriptingguy/2014/10/18/weekend-scripter-why-learn-powershell/
[ebook-from-Idera]:https://www.idera.com/resourcecentral/whitepapers/powershell-ebook
[channel9-learn-powershell]: https://channel9.msdn.com/Search?term=powershell#ch9Search
[idera-powershell-tips]: https://blog.idera.com/database-tools/powershell/powertips/
[quick-reference]: https://www.powershellmagazine.com/2014/04/24/windows-powershell-4-0-and-other-quick-reference-guides/
[script-guy-how-to]:https://blogs.technet.microsoft.com/tommypatterson/2015/09/04/ed-wilsons-powershell5-videos-now-on-channel9-2/
[basic-cookbooks]:https://docs.microsoft.com/powershell/scripting/samples/sample-scripts-for-administration
