# escape=`
FROM microsoft/nanoserver
LABEL Readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md",`
      Description="This Dockerfile will install the latest milestone release of PS."

ARG POWERSHELL_ZIP=https://github.com/PowerShell/PowerShell/releases/download/v6.0.0-alpha.14/powershell-6.0.0-alpha.14-win10-x64.zip
ARG POWERSHELL_SHA256=3F5FD873B6E3062D9741B019BC645E6F20999BE66B2FDAA4374495FEBEDD0E03

# Prepare shell environment to Log-to > C:\Dockerfile.log
SHELL ["C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe","-command","$ErrorActionPreference = 'Stop';","$ConfirmPreference = 'None';",`
    "$VerbosePreference = 'Continue';","Start-Transcript -path C:\\Dockerfile.log -append -IncludeInvocationHeader;","$PSVersionTable|Write-Output;",`
    "$WinVer = $(Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion');",`
    "$OsVer = $('OperatingSystem:  '+ $WinVer.ProductName +' '+$WinVer.EditionId+' '+$WinVer.InstallationType);",`
    "$BldVer = $('FullBuildString:  '+ $WinVer.BuildLabEx); Write-Output -InputObject $OsVer,$BldVer; "]
WORKDIR /

# Install PowerShell package and clean up
ADD $POWERSHELL_ZIP /PowerShell-win10-x64.zip
RUN [System.IO.FileInfo]$ZipFile = Get-Item -Path ./powershell-win10-x64.zip;`
    [String]$ZipHash = (Get-FileHash  -Path $ZipFile -Algorithm SHA256 | select -ExpandProperty Hash);`
    If ($ZipHash -notmatch $Env:POWERSHELL_SHA256){`
        Throw [String]$('['+$ZipHash+'] does not match ['+$Env:POWERSHELL_SHA256+']!')`
    }`
    [System.IO.DirectoryInfo]$PsFolder=New-Item -Path $Env:ProgramFiles\PowerShell -ItemType Directory -Force;`
    Add-Type -AssemblyName System.IO.Compression.ZipFile;`
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipFile,$PsFolder);`
    If (Get-ChildItem -Path $PsFolder/powershell.exe){`
        Write-Host 'Powershell installation completed successfully.'; Remove-Item -Path $ZipFile;`
        New-Item -Type SymbolicLink -Path $PsFolder\ -Name Core -Value $PsFolder | Out-Null;`
        $PSVersionTable | Export-Clixml -Path .\OsPsVer.clix;`
    } else {Throw 'Installation failed!  See c:\Dockerfile.log'}

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