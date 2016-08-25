PowerShell Remoting Over SSH
============================

Overview
=====
PowerShell remoting normally uses WinRM for connection negotiation and data transport.  SSH was chosen for this remoting implementation since it is now available for both Linux and Windows platforms and allows true multiplatform PowerShell remoting.  However, WinRM also provides a robust hosting model for PowerShell remote sessions which this implementation does not yet do.  And this means that PowerShell remote endpoint configuration and JEA (Just Enough Administration) is not yet supported in this implementation.

PowerShell SSH remoting lets you do basic PowerShell session remoting between Windows and Linux machines.  This is done by creating a PowerShell hosting process on the target machine as an SSH subsystem.  Eventually this will be changed to a more general hosting model similar to how WinRM works in order to support endpoint configuration and JEA.

The New-PSSession, Enter-PSSession and Invoke-Command cmdlets now have a new parameter set to facilitate this new remoting connection
```powershell
[-HostName <string>]  [-UserName <string>]  [-KeyPath <string>]
```
This new parameter set will likely change but for now allows you to create SSH PSSessions that you can interact with from the command line or invoke commands and scripts on.
You specify the target machine with the HostName parameter and provide the user name with UserName.  When running the cmdlets interactively at the PowerShell command line you will be prompted for a password.  But you also have the option to use SSH key authentication and provide a private key file path with the KeyPath parameter.  Note that PSCredential is not yet supported.

General setup information
==========================
SSH is required to be installed on all machines.  You should install both client (ssh.exe) and server (sshd.exe) so that you can experiment with remoting to and from the machines.
For Windows you will need to install Win32 Open SSH from GitHub.  For Linux you will need to install SSH (including server) appropriate to your platform.  You will also need a recent PowerShell build or package from GitHub having the SSH remoting feature.  SSH Subsystems is used to establish a PowerShell process on the remote machine and the SSH server will need to be configured for that.  In addition you will need to enable password authentication and optionally key based authentication.

Setup on Windows Machine
=========================
1.  Install the latest [PowerShell for Windows] build from GitHub
    - You can tell if it has the SSH remoting support by looking at the parameter sets for New-PSSession
    ```powershell
    Get-Command New-PSSession -syntax
    New-PSSession [-Name <string[]>] [-HostName <string>] [-UserName <string>] [-KeyPath <string>] [<CommonParameters>]
    ```
1.  Install the latest [Win32 Open SSH] build from GitHub using the [installation] instructions
1.  Edit the sshd_config file at the location where you installed Win32 Open SSH
    - Make sure password authentication is enabled
      + PasswordAuthentication yes
    - Add a PowerShell subsystem entry
      + Subsystem	powershell `PowerShell_Install_Path`\powershell.exe -sshs -NoLogo -NoProfile
    - Optionally enable key authentication
      + RSAAuthentication yes
      + PubkeyAuthentication yes
1.  Restart the sshd service
    - Restart-Service sshd
1. Add the path where OpenSSH is installed to your Path Env Variable
    - This should be along the lines of C:\ProgramFiles\OpenSSH\
    - This allows for the ssh.exe to be found and resolves the issue you may see as reported in [#2003] with Invoke-Command New-PSSession & Enter-PSSession 


[PowerShell for Windows]: https://github.com/PowerShell/PowerShell/blob/master/docs/installation/windows.md#msi
[Win32 Open SSH]: https://github.com/PowerShell/Win32-OpenSSH
[installation]: https://github.com/PowerShell/Win32-OpenSSH/wiki/Install-Win32-OpenSSH
[#2003]: https://github.com/PowerShell/PowerShell/issues/2003
Setup on Linux (Ubuntu 14.04) Machine:
======================================
1.  Install the latest [PowerShell for Linux] build from GitHub
    - You can tell if it has the SSH remoting support by looking at the parameter sets for New-PSSession
    ```powershell
    Get-Command New-PSSession -syntax
    New-PSSession [-Name <string[]>] [-HostName <string>] [-UserName <string>] [-KeyPath <string>] [<CommonParameters>]
    ```
1.  Install [Ubuntu SSH] as needed
    - sudo apt install openssh-client
    - sudo apt install openssh-server
1.  Edit the sshd_config file at location /etc/ssh
    - Make sure password authentication is enabled
      + PasswordAuthentication yes
    - Add a PowerShell subsystem entry
      + Subsystem powershell powershell -sshs -NoLogo -NoProfile
    - Optionally enable key authentication
      + RSAAuthentication yes
      + PubkeyAuthentication yes
1.  Restart the sshd service
    - sudo service ssh restart

[PowerShell for Linux]: https://github.com/PowerShell/PowerShell/blob/master/docs/installation/linux.md#ubuntu-1404
[Ubuntu SSH]: https://help.ubuntu.com/lts/serverguide/openssh-server.html


PowerShell Remoting Example:
=====================================
The easiest way to test remoting is to just try it on a single machine.  Here I will create a remote session back to the same machine on a Linux box.  Notice that I am using PowerShell cmdlets from a command prompt so we see prompts from SSH asking to verify the host computer as well as password prompts.  You can do the same thing on a Windows machine to ensure remoting is working there and then remote between machines by simply changing the host name.

```powershell
#
# Linux to Linux
#
PS /home/TestUser> $session = New-PSSession -HostName UbuntuVM1 -UserName TestUser
The authenticity of host 'UbuntuVM1 (9.129.17.107)' cannot be established.
ECDSA key fingerprint is SHA256:2kCbnhT2dUE6WCGgVJ8Hyfu1z2wE4lifaJXLO7QJy0Y.
Are you sure you want to continue connecting (yes/no)?
TestUser@UbuntuVM1s password:

PS /home/TestUser> $session

 Id Name            ComputerName    ComputerType    State         ConfigurationName     Availability
 -- ----            ------------    ------------    -----         -----------------     ------------
  1 SSH1            UbuntuVM1       RemoteMachine   Opened        DefaultShell             Available

PS /home/TestUser> Enter-PSSession $session

[UbuntuVM1]: PS /home/TestUser> uname -a
Linux TestUser-UbuntuVM1 4.2.0-42-generic 49~14.04.1-Ubuntu SMP Wed Jun 29 20:22:11 UTC 2016 x86_64 x86_64 x86_64 GNU/Linux

[UbuntuVM1]: PS /home/TestUser> Exit-PSSession

PS /home/TestUser> Invoke-Command $session -ScriptBlock { Get-Process powershell }

Handles  NPM(K)    PM(K)      WS(K)     CPU(s)     Id  SI ProcessName                    PSComputerName
-------  ------    -----      -----     ------     --  -- -----------                    --------------
      0       0        0         19       3.23  10635 635 powershell                     UbuntuVM1
      0       0        0         21       4.92  11033 017 powershell                     UbuntuVM1
      0       0        0         20       3.07  11076 076 powershell                     UbuntuVM1


#
# Linux to Windows
#
PS /home/TestUser> Enter-PSSession -HostName WinVM1 -UserName PTestName
PTestName@WinVM1s password:

[WinVM1]: PS C:\Users\PTestName\Documents> cmd /c ver

Microsoft Windows [Version 10.0.10586]

[WinVM1]: PS C:\Users\PTestName\Documents> 
```


Known Issues:
------------
1.  You can currently establish a connection either interactively with user name and password or via key authentication.  PSCredential is not yet supported.
1.  Remote debugging from Linux to Windows does not work.  However, remote debugging from Windows to Linux does work.
1.  Fan out to multiple machines not yet supported.
1.  sudo command does not work in remote session to Linux machine.

