# escape=`
FROM microsoft/windowsservercore
LABEL Readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md",`
      Description="This Dockerfile will install the latest release of PS."

ARG POWERSHELL_MSI=https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.15/PowerShell_6.0.0-alpha.15-win10-win2k16-x64.msi
ARG POWERSHELL_SHA256=CC52D21F3287E412B9C3B73C98BB5B06F8056D49D63201072216DF92B7F2E59B

# Prepare shell environment to Log-to > C:\Dockerfile.log
SHELL ["C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe","-command","$ErrorActionPreference = 'Stop';","$ConfirmPreference = 'None';",`
    "$VerbosePreference = 'Continue';","Start-Transcript -path C:\\Dockerfile.log -append -IncludeInvocationHeader;","$PSVersionTable|Write-Output;",`
    "$WinVer = $(Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion');",`
    "$OsVer = $('OperatingSystem:  '+ $WinVer.ProductName +' '+$WinVer.EditionId+' '+$WinVer.InstallationType);",`
    "$BldVer = $('FullBuildString:  '+ $WinVer.BuildLabEx); Write-Output -InputObject $OsVer,$BldVer; "]

# Install PowerShell package and clean up
ADD $POWERSHELL_MSI /PowerShell-win10-win2k16-x64.msi
RUN [System.IO.FileInfo]$MsiFile = Get-Item -Path ./PowerShell-win10-win2k16-x64.msi;`
    [String]$MsiHash=(Get-FileHash -Path $MSiFile -Algorithm SHA256 | select -ExpandProperty Hash);`
    If ($MsiHash -notmatch $Env:POWERSHELL_SHA256) {`
        Throw [String]$('['+$MsiHash+'] does not match ['+$Env:POWERSHELL_SHA256+']!') `
    }`
    $proc=Start-Process -FilePath msiexec.exe -ArgumentList '-qn','-i c:\PowerShell-win10-win2k16-x64.msi',`
        '-log c:\PowerShell-win10-win2k16-x64.msi.log','-norestart' -wait -PassThru;`
    If ($proc.ExitCode -eq 0) {`
        Remove-Item -Path $MsiFile;`
        $psexe=Get-Item -Path $Env:ProgramFiles\PowerShell\*\powershell.exe;`
        Write-Host ('Successfully installed to '+$psexe);`
        New-Item -Type SymbolicLink -Path $Env:ProgramFiles\PowerShell\ -Name Core `
            -Value $psexe.DirectoryName | Out-Null;`
        $PSVersionTable | Export-Clixml -Path .\OsPsVer.clix;`
    } else { throw 'Installation failed!  See c:\PowerShell-win10-win2k16-x64.msi.log'}

# Verify New Powershell.exe runs
SHELL ["C:\\Program Files\\PowerShell\\Core\\powershell.exe","-command","$ErrorActionPreference='Stop';"]
RUN $PSVersionTable|Out-Host; $OsPsVer = Import-Clixml -Path .\OsPsVer.clix; Remove-Item -Path .\OsPsVer.clix;`
    If ($PSVersionTable.PSEdition -ne 'Core') {Throw [String]$('['+$PSVersionTable.PSEdition+'] is not [Core]!')}`
    If ($PSVersionTable.PSVersion -eq $OsPsVer.PSVersion) {Throw [String]$('['+$PSVersionTable.PSVersion+'] is unchanged...')}`
    Write-Host "Hello World!  PowerShell Core installation has been verfied."

SHELL ["CMD.EXE","/C"]
RUN del /q C:\Dockerfile.log &>c:\entrypoint.bat echo @echo off &>>c:\entrypoint.bat`
        echo "C:\Program Files\PowerShell\Core\powershell.exe" -command $ErrorActionPreference='Stop';%* 

ENTRYPOINT ["C:\\Program Files\\PowerShell\\Core\\powershell.exe","-noexit","-command","$ErrorActionPreference='Stop';"]