# escape=`
FROM brycem/win10build
LABEL Readme.md="https://github.com/PowerShell/PowerShell/blob/master/docker/README.md",`
      Description="This Dockerfile will install and build the latest nighly PS build."

ARG FORK=PowerShell
ARG BRANCH=master
ARG PULLREQ=na

# Prepare shell environment to Log-to > C:\Dockerfile.log
SHELL ["C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe","-command","$ErrorActionPreference = 'Stop';","$ConfirmPreference = 'None';",`
    "$VerbosePreference = 'Continue';","Start-Transcript -path C:\\Dockerfile.log -append -IncludeInvocationHeader;","$PSVersionTable|Write-Output;",`
    "$WinVer = $(Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion');",`
    "$OsVer = $('OperatingSystem:  '+ $WinVer.ProductName +' '+$WinVer.EditionId+' '+$WinVer.InstallationType);",`
    "$BldVer = $('FullBuildString:  '+ $WinVer.BuildLabEx); Write-Output -InputObject $OsVer,$BldVer; "]
WORKDIR /

# Build & Test PowerShell
RUN $vcBld = ${env:ProgramFiles(x86)}+'\Microsoft Visual C++ Build Tools\vcbuildtools.bat';`
    test-path $vcBld; [String]$cmdline = ([char]0x0022+$vcBld+[char]0x0022+' x64&set');`
    cmd /c $cmdLine | Foreach-Object {$cmdVar,$cmdVal=$_.split('=');Set-Item -Path env:$cmdVar -Value $cmdVal};`
    dir env:|out-host;`
    If (test-path c:\PowerShell) {Remove-Item c:\PowerShell -Recurse -Force -ea Ignore}`
    $gitArgs=@('clone','-v','--recursive',('https://github.com/'+$env:FORK+'/PowerShell.git'),'-b',$env:BRANCH);`
    $proc = Start-Process git.exe -ArgumentList $gitArgs -PassThru -Wait -NoNewWindow;`
    If (($proc.ExitCode -eq 0) -and (Test-Path 'C:\PowerShell\Build.psm1')) {`
        Write-Host 'PowerShell repo successfully cloned to c:\PowerShell.'`
    } else {Echo $gitArgs; dir c:\PowerShell\;Throw ('git.exe returned '+$proc.ExitCode)}`
    Set-Location C:\PowerShell;`
    If ($Env:PULLREQ -ne 'na') {`
        $gitArgs=@('fetch','-v','origin',('+refs/pull/'+$env:PULLREQ+'/merge:'));`
        $proc = Start-Process git.exe -ArgumentList $gitArgs -PassThru -Wait -NoNewWindow;`
        If ($proc.ExitCode -eq 0) {`
            Write-Host ('PowerShell repo successfully fetched PR:'+$env:PULLREQ)`
        } else {Echo $gitArgs; Throw ('git.exe returned '+$proc.ExitCode)}`
        $gitArgs=@('checkout','-vf','FETCH_HEAD')`
    }`
    $Global:IsWindows=$true;`
    git submodule update --init;`
    Import-Module .\build.psm1;Start-PSBootstrap;`
    Start-PSBuild -Clean -CrossGen -Publish -PSModuleRestore -Configuration 'Release' -Runtime 'win10-x64';`
    $testResultsNonAdminFile = 'C:\TestsResultsNonAdmin.xml';`
    $testResultsAdminFile = 'C:\TestsResultsAdmin.xml';`
    $ExcludeTag = @('Slow', 'Feature', 'Scenario');`
    Write-Host -Foreground Green 'Running "CI" CoreCLR tests..';`
    $env:CoreOutput = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -Configuration 'Release'));`
    Start-PSPester -bindir $env:CoreOutput -outputFile $testResultsNonAdminFile -Unelevate -Tag @() `
        -ExcludeTag ($ExcludeTag + @('RequireAdminOnWindows'));` 
    Start-PSPester -bindir $env:CoreOutput -outputFile $testResultsAdminFile `
        -Tag @('RequireAdminOnWindows') -ExcludeTag $ExcludeTag;`
    Test-PSPesterResults -TestResultsFile $testResultsNonAdminFile,$testResultsAdminFile;`
    Start-PSBuild -FullCLR -Configuration 'Release' -PSModuleRestore;`
    $env:FullOutput = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -FullCLR));`
    Write-Host -Foreground Green 'Run FullCLR tests';`
    $testResultsFileFullCLR = 'C:\TestsResults.FullCLR.xml';`
    Start-PSPester -FullCLR -bindir $env:FullOutput -outputFile $testResultsFileFullCLR `
        -Tag $null -path 'test/fullCLR';`
    Test-PSPesterResults -TestResultsFile $testResultsFileFullCLR;`
    Set-BuildVariable -Name TestPassed -Value True;`
    $zipFile = Start-PSPackage -Type zip; $msiFile = Start-PSPackage -Type msi;`
    $MsiArgs = @(('/i '+$msiFile),'/log c:\PowerShell-win10-x64.msi.log','/qn','/norestart');`
    $proc = Start-Process -FilePath msiexec.exe -ArgumentList $MsiArgs -wait -PassThru;`
    If ($proc.ExitCode -eq 0) {`
        $psexe = Get-Item -Path $Env:ProgramFiles\PowerShell\*\powershell.exe;`
        Write-Host ('Successfully installed to '+$psexe);`
        New-Item -Type SymbolicLink -Path $Env:ProgramFiles\PowerShell\ -Name Core `
            -Value $psexe.DirectoryName | Out-Null;`
        $PSVersionTable | Export-Clixml -Path .\OsPsVer.clix;`
        [System.Environment]::SetEnvironmentVariable('PsCoreExe',($psexe.FullName),'Machine')`
    } else {`
        Write-Output -InputObject $(get-content -Path C:\PowerShell-win10-x64.msi.log -ea Ignore);`
        Throw ('Installation failed! PowerShell-win10-x64.msi returned '+$proc.ExitCode)`
    }

# Verify New Powershell.exe
SHELL ["C:\\Program Files\\PowerShell\\Core\\powershell.exe","-command","$ErrorActionPreference='Stop';"]
RUN $PSVersionTable|Out-Host; $OsPsVer = Import-Clixml -Path .\OsPsVer.clix;`
    If ($PSVersionTable.PSEdition -ne 'Core') {Throw ('['+$PSVersionTable.PSEdition+'] is not [Core]!')}`
    If ($PSVersionTable.PSVersion -eq $OsPsVer.PSVersion) {Throw ('['+$PSVersionTable.PSVersion+'] is unchanged...')}`
    Remove-Item C:\Dockerfile.log -Force; Remove-Item C:\PowerShell\OsPsVer.clix -Force;`
    Write-Host ('Hello World!  PowerShell Core '+$PSVersionTable.PSVersion+' has been built and installed.')

ENTRYPOINT ["C:\\Program Files\\PowerShell\\Core\\powershell.exe","-noexit","-command","$ErrorActionPreference='Stop';"]