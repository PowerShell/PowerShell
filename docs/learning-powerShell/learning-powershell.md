Learning PowerShell
====

Whether you're new to programming or an experienced developer, we'll help you get started with PowerShell.
In this document we'll cover the following: installing PowerShell, samples walkthrough, PowerShell editor, debugger, testing tools and a map book for experienced bash users to get started with PowerShell faster. The exercises in this document are intended to give you a solid foundation in how to use PowerShell and get PowerShell to do work for you. You won't be a PowerShell guru at the end of reading this material but you will be well on your way with the right set of knowledge to start using PowerShell. If you have 30 minutes now, let’s try it.


Installing PowerShell
----

First you need to setup your computer working environment if you have not done so. Choose the platform below and follow the instructions. At the end of this exercise, you should be able to launch the PowerShell session.


- [PowerShell on Linux][powershell-on-linux]
- [PowerShell on OS X][powershell-on-os-x]
- PowerShell on Windows

  For this tutorial, you do not need to install PowerShell if you are running on Windows. You can launch PowerShell command console by pressing Windows Key and typing PowerShell, and clicking on 'Windows PowerShell'.

[powershell-on-linux]: https://github.com/PowerShell/PowerShell/blob/master/docs/building/linux.md
[powershell-on-os-x]: https://github.com/PowerShell/PowerShell/blob/master/docs/building/osx.md

TODO: Raghu for setup-dev-environment.md


Getting Started with PowerShell
----
PowerShell command has a Verb-None structure with a set of parameters. It's easy to learn and use PowerShell. For example, "Get-Process" will display all the running processes on your system. Let's walk through with a few examples by clicking on the [PowerShell Beginner's Guide](powershell-beginners-guide.md).

Now you have learned the basics of PowerShell. Please continue reading: Editor, Debugger, and Testing Tool if you want to do some development work in PowerShell.

PowerShell Editor
----

In this section, you will create a PowerShell script using PowerShell editor. You can certainly use your favorite editor to write scripts. As an example, we use Visual Studio Code (VS Code) which works for Windows, Linux, or OS X. Click on the following link to start create your first PowerShell script, let's say helloworld.ps1.

- [Using Visual Studio Code (VS Code)][use-vscode-editor]

On Windows, you can also use [PowerShell Integrated Scripting Environment (ISE)][use-ise-editor] to edit PowerShell scripts.

[use-vscode-editor]:./using-vscode.md#editing-with-vs-code
[use-ise-editor]:./using-ise.md#editing-with-ise

PowerShell Debugger
----

Assuming you have written a PowerShell script which may contains a software bug, you would like to fix the issue via debugging. As an example, we use VS Code. Click on the link below to start debugging:

- [Using Visual Studio Code (VS Code)][use-vscode-debugger]
- [PowerShell Command-line Debugging][cli-debugging]

On Windows, you can also use  [ISE][use-ise-debugger] to debug PowerShell scripts.

[use-vscode-debugger]:./using-vscode.md#debugging-with-vs-code
[use-ise-debugger]:./using-ise.md#debugging-with-ise
[cli-debugging]:./debugging-from-commandline.md


PowerShell Testing
----

We recommend using Pester testing tool which is initiated by the PowerShell Community for writing test cases. To use the tool please read [ Pester Guides](https://github.com/pester/Pester) and [Writing Pester Tests Guidelines](https://github.com/PowerShell/PowerShell/blob/master/docs/testing-guidelines/WritingPesterTests.md).


Map Book for Experienced Bash users
----

TODO: Don & JP to fill in

| Bash           | PowerShell    | Description     |
|:---------------|:--------------|:----------------|
| ls             |ls             |List files and folders   
| cd             |cd             |Change directory    
| mkdir          |mkdir          |Create a new folder
| Clear, Ctrl+L, Reset | cls | Clear screen
|                |               |                 |   
|                |               |                 ||


More Reading
----
- Microsoft Virtual Academy: [GetStarted with PowerShell][getstarted-with-powershell]
- [Windows PowerShell in Action][in-action] by Bruce Payette
- [Why Learn PowerShell][why-learn-powershell] by Script Guy
- [Introduction to PowerShell][powershell-intro] from Pluralsight
- PowerShell Web Docs: [Basic cookbooks][basic-cookbooks]
- [PowerShell eBooks][ebooks-from-powershell.com] from PowerShell.com
- [PowerShell Training and Tutorials][lynda-training] from Lynda.com
- [Learn PowerShell][channel9-learn-powershell] from channel9
- [Learn PowerShell Video Library][powershell.com-learn-powershell] from PowerShell.com
- [PowerShell Quick Reference][quick-reference] by PowerShellMagazine.com
- [PowerShell 5 How-To Videos][script-guy-how-to] by Script Guy


[getstarted-with-powershell]: https://channel9.msdn.com/Series/GetStartedPowerShell3
[in-action]: https://www.amazon.com/Windows-PowerShell-Action-Second-Payette/dp/1935182137
[why-learn-powershell]: https://blogs.technet.microsoft.com/heyscriptingguy/2014/10/18/weekend-scripter-why-learn-powershell/
[powershell-intro]: https://www.pluralsight.com/courses/powershell-intro
[basic-cookbooks]: https://msdn.microsoft.com/en-us/powershell/scripting/getting-started/basic-cookbooks
[ebooks-from-powershell.com]: http://powershell.com/cs/blogs/ebookv2/default.aspx
[lynda-training]: https://www.lynda.com/PowerShell-training-tutorials/5779-0.html
[channel9-learn-powershell]: https://channel9.msdn.com/Search?term=powershell#ch9Search
[powershell.com-learn-powershell]: http://powershell.com/cs/media/14/default.aspx
[quick-reference]: http://www.powershellmagazine.com/2014/04/24/windows-powershell-4-0-and-other-quick-reference-guides/
[script-guy-how-to]:https://blogs.technet.microsoft.com/tommypatterson/2015/09/04/ed-wilsons-powershell5-videos-now-on-channel9-2/
