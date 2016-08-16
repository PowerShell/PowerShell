PowerShell Remoting Over SSH
============================

About
=====
PowerShell remoting has traditionally been implemented through WinRM for Windows only.  But for Open PowerShell we want to support multiplatform remoting for both Windows and Linux.  There is some work being done to support WinRM on Linux via OMI.  But this implemenation completely bypasses WinRM and uses SSH for the remoting transport.
The benefit to this approach is that SSH is a known and popular secure transport/shell and with the recent Win32 Open SSH effort it is now available on Windows as well as Linux platforms.   
The downside is that some Windows specific remoting functions don't work yet.  For example PowerShell remoting endpoint configuration is not yet supported and since JEA (Just Enough Administration)
is dependent on this, it is also not yet supported.  See the "Issues" section at the end of this document.

Open PowerShell SSH remoting lets you do basic remoting between machines by starting a PowerShell process on the target machine that hosts the remote session.  
Eventually this will be changed to a more general hosting model similar to how WinRM works to support endpoint configuration and JEA.

The New-PSSession, Enter-PSSession, Invoke-Command cmdlets now have a new parameter set to facilitate this new remoting connection
```powershell
[-HostName <string>]  [-UserName <string>]  [-KeyPath <string>]
```
This new parameter set will likely change but for now allows you to create SSH PSSessions where you can interact with it from the comand line or invoke commands on the session.  
You specify the target machine with HostName parameter and provide the user name with UserName.  When running the cmdlets interactively at the PowerShell command line you will be prompted for a password.  But you also have the option to use SSH key authentication and provide a private key file path with the KeyPath parameter.  PSCredential is not yet supported.

General set up information
==========================
SSH is of course required to be installed on the machines.  You should install both client (ssh.exe) and server (sshd.exe) so you can experiment with remoting to and from the machines.
For the Windows machine you will need to install Win32 Open SSH from github.  For the Linux machine the ssh.exe client is automatically installed with Ubuntu but you will need to install an sshd.exe package in order to remote into the machine.  You will also need a recent Open PowerShell build or package having the SSH remoting feature.  
Open PowerShell remoting uses the SSH subsystem to establish a PowerShell process on the remote target machine.  So you will need to edit the sshd_config file to add a new PowerShell subsytem before a target machine can accept a remote connection. In addition you will need to enable password authentication and optionally key based authentication.

Set up on Windows Machine
=========================
1.  Install the latest Open PowerShell for Windows build from github [open-powershell]
    - You can tell if it has the SSH remoting support by looking at the parameter sets for New-PSSession
    ```powershell
    Get-Command New-PSSession -syntax
    New-PSSession [-Name <string[]>] [-HostName <string>] [-UserName <string>] [-KeyPath <string>] [<CommonParameters>]
    ```
1.  Install the latest Win32 Open SSH from github [open-ssh] using the installation instructions [open-ssh-install]
1.  Edit the sshd_config file at the location where you installed Win32 Open SSH.
    - Make sure password authentication is enabled
      + PasswordAuthentication yes
    - Add a PowerShell subsystem entry so that a PowerShell host session can be created
      + Subsystem	powershell `PowerShell_Install_Path`\powershell.exe -sshs -NoLogo -NoProfile
    - Optionally enable key authentication
      + RSAAuthentication yes
      + PubkeyAuthentication yes
1.  Restart the sshd service
    - Restart-Service sshd

[open-powershell]: https://github.com/PowerShell/PowerShell
[open-ssh]: https://github.com/PowerShell/Win32-OpenSSH
[open-ssh-install]: https://github.com/PowerShell/Win32-OpenSSH/wiki/Install-Win32-OpenSSH

Set up on Linux (Ubuntu 14.04) Machine:
======================================
1.  Install the latest Open PowerShell for Linux build from github [open-powershell]
    - You can tell if it has the SSH remoting support by looking at the parameter sets for New-PSSession
    ```powershell
    Get-Command New-PSSession -syntax
    New-PSSession [-Name <string[]>] [-HostName <string>] [-UserName <string>] [-KeyPath <string>] [<CommonParameters>]
    ```
1.  Install SSHD as needed
    - sudo dpkg -i powershell_0.6.0-392-ga5b5dc5-1_amd64.deb
1.  Edit the sshd_config file at the location where you installed Win32 Open SSH.
    - Make sure password authentication is enabled
      + PasswordAuthentication yes
    - Add a PowerShell subsystem entry so that a PowerShell host session can be created 
      + Subsystem powershell `PowerShell_Install_Path`/powershell -sshs -NoLogo -NoProfile
    - Optionally enable key authentication
      + RSAAuthentication yes
      + PubkeyAuthentication yes
1.  Restart the sshd service
    - sudo service ssh restart

[open-powershell]: https://github.com/PowerShell/PowerShell


Using PowerShell Remoting Example:
=================================
The easiest way to test remoting is to just try it on a single machine.  Here I will create a remote session back to the same machine on a Linux box.  Notice that I am using PowerShell cmdlets from a command prompt so we see prompts from ssh asking to verify the host computer as well as password prompts.  You can do the same thing on a Windows machine to ensure remoting is working there and then remote between machines by simply specifying the host name.

```powershell
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

PS /home/TestUser> Invoke-Command $session -Script { Get-Process powershell }

Handles  NPM(K)    PM(K)      WS(K)     CPU(s)     Id  SI ProcessName                    PSComputerName
-------  ------    -----      -----     ------     --  -- -----------                    --------------
      0       0        0         19       3.23  10635 635 powershell                     UbuntuVM1
      0       0        0         21       4.92  11033 017 powershell                     UbuntuVM1
      0       0        0         20       3.07  11076 076 powershell                     UbuntuVM1


  
PS /home/TestUser> Enter-PSSession -HostName WinVM1 -UserName PTestName
PTestName@WinVM1s password:

[WinVM1]: PS C:\Users\PTestName\Documents> cmd /c ver

Microsoft Windows [Version 10.0.10586]

[WinVM1]: PS C:\Users\PTestName\Documents> 
```


Known Issues:
------------
1.  You can only establish a connection with UserName/Password (interactive) or key authentication.  PSCredential is not yet supported.
1.  Endpoint configuration and JEA is not yet supported.
1.  Remote debugging from Linux to Windows does not work.  However, remote debugging from Windows to Linux does work.
1.  SSH connection attempt errors are not currently surfaced making connection errors appear to hang PowerShell (although a transport timeout does eventually occur).  To avoid connection errors make sure HostName and UserName names are correct and that the sshd service is running on the target machine. 
1.  Fan out to multiple machines not yet supported.
1.  sudo command does not work in remote session to Linux machine.

