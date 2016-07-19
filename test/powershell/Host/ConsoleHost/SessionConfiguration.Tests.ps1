##
##  Copyright (c) Microsoft Corporation, 2015
##
##  ConsoleHost Session Configuration tests
##

$powershellexe = (get-process -id $PID).mainmodule.filename

$path = $PSScriptRoot
if ($path -eq $null) { $path = Split-Path $MyInvocation.InvocationName }
if ($path -eq $null) { $path = $pwd }

$regHKLMKey = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\PowerShell\ConsoleSessionConfiguration"
$regHKCUKey = "HKCU:\SOFTWARE\Policies\Microsoft\Windows\PowerShell\ConsoleSessionConfiguration"
$regEnable = "EnableConsoleSessionConfiguration"
$regName = "ConsoleSessionConfigurationName"
$regValue = "microsoft.powershell"

function ClearConsoleSessionConfigurationGroupPolicy
{
    if ( $IsCore ) { return }
    Remove-Item -Path $regHKLMKey -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $regHKCUKey -Force -ErrorAction SilentlyContinue
}
function SetConsoleSessionConfigurationGroupPolicy
{
    if ( $IsCore ) { return }
    if (! (Test-Path $regHKCUKey)) { New-Item -Path $regHKCUKey -Force }
    # Enable Group Policy console session configuration
    Set-ItemProperty -Path $regHKCUKey -Name $regEnable -Value 1
    # Set Group Policy console session configuration name
    Set-ItemProperty -Path $regHKCUKey -Name $regName -Value $regValue
}
# Ensure that there is no console session configuration group policy settings
ClearConsoleSessionConfigurationGroupPolicy

Describe "Confirms that default powershell.exe runs in non-configured sessions" -Tags 'CI' {
    # Run powershell command in non-configured session
    $results = & $powershellexe -command 'Write-Output $PSSenderInfo'

    It "Verifies that command does not run in configured loop-back session" {
        $results | Should Be $null
    }
}

Describe "Tests powershell.exe session configuration switch parameter" -Tags 'CI' {
    # Command will be run in loop-back remote session
    It "Verifies powershell configured session command results contain expected PSSenderInfo RunAsUser" -skip:($IsCore) {
        # Run powershell command in configuration session
        $results = & $powershellexe -config microsoft.powershell -command 'Write-Output $PSSenderInfo'
        $results | Should Not Be $null
        ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
    }
}

Describe "Tests powershell.exe session configuration with -File switch" -Tags 'CI' {

    BeforeAll {
        $filepath = setup -f PSConfig102.ps1 -value 'Write-Output $PSSenderInfo' -pass
    }

    # Script file will be run in loop-back remote session
    It "Verifies powershell configured session script file results contain expected PSSenderInfo RunAsUser" -skip:($IsCore) {
    # Run powershell script file in configuration session
    $results = & $powershellexe -config microsoft.powershell -file $filePath
        $results | Should Not Be $null
        ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
    }
}


Describe "Tests powershell.exe session configuration from HKLM Group Policy setting" -Tags 'CI' {

    BeforeAll {
        SetConsoleSessionConfigurationGroupPolicy
    }
    AfterAll {
        ClearConsoleSessionConfigurationGroupPolicy
    }
    It "Verifies powershell configured session expected PSSenderInfo result contains RunAsUser" -skip:($IsCore) {
        # Run powershell.exe command
        $results = & $powershellexe -command 'Write-Output $PSSenderInfo'
        # Command should be run in powershell configured endpoint (microsoft.powershell)
        $results | Should Not Be $null
        ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
    }
}


Describe "Tests powershell.exe session configuration from HKCU Group Policy setting" -Tags 'CI' {

    BeforeAll {
        SetConsoleSessionConfigurationGroupPolicy

    }
    AfterAll {
        ClearConsoleSessionConfigurationGroupPolicy
    }
    It "Verifies powershell configured session expected PSSenderInfo result contains RunAsUser" -skip:($IsCore) {

        # Run powershell.exe command
        $results = & $powershellexe -command 'Write-Output $PSSenderInfo'

        # Command should be run in powershell configured endpoint (microsoft.powershell)
        $results | Should Not Be $null
        ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
    }
}


Describe "Tests powershell.exe session configuration with restricted custom endpoint" -Tags 'CI' {

    BeforeAll {
        $filePath = "${TestDrive}/Test201A.pssc"
        $configName = "Test201AConfig"
    }
    AfterAll {
        if ($configuration -ne $null) { $configuration | Unregister-PSSessionConfiguration -Force -ErrorAction SilentlyContinue }
    }
    It "Verifies the custom configuration was created successfully" -skip:($IsCore) {
        New-PSSessionConfigurationFile -Path $filePath -SessionType RestrictedRemoteServer `
            -VisibleCmdlets 'Get-Command','Get-Runspace','Get-Module','Write-Output' `
            -ModulesToImport PSScheduledJob
        $configuration = Register-PSSessionConfiguration -Name $configName -Path $filePath -Force -ErrorAction SilentlyContinue
        $configuration | Should Not Be $null
    }

        # Verify running in configured restricted session
    It "Verifies that the Get-ChildItem command cannot run" -skip:($IsCore) {
        $results = & $powershellexe -config $configName -command 'Get-ChildItem' 2>&1
        $results | Should Not Be $null
        ($results | ? { $_ -match "CommandNotFoundException" }) | Should Not Be $null
    }

    It "Verifies results returned from powershell running in custom test configuration" -skip:($IsCore) {
        # Get Get-Runspace command
        $results = & $powershellexe -config $configName -command 'Get-Command Get-Runspace'
        $results | Should Not Be $null
        $results | ? { $_ -match "Get-Runspace" } | Should Not Be $null
    }

    It "Verifies results returned contain the expected PSScheduledJob module" -skip:($IsCore) {
        # Get module list
        $results = & $powershellexe -config $configName -command 'Get-Module'
        $results | Should Not Be $null
        $results | ? { $_ -match "PSScheduledJob" } | Should Not Be $null
    }
}


Describe "Tests powershell.exe session configuration with encoded command" -Tags 'CI' {

    BeforeAll {
        $command = 'Write-Output $PSSenderInfo'
        $eCommand = [System.Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($command))
    }

    # Command will be run in loop-back remote session
    It "Verifies powershell configured session encoded command results contain expected PSSenderInfo RunAsUser" -skip:($IsCore) {
        # Run powershell command in configured session (microsoft.powershell)
        $results = & $powershellexe -config microsoft.powershell -EncodedCommand $eCommand
        $results | Should Not Be $null
        ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
    }
}

Describe "Verify powershell.exe banner copyright notice" -Tags 'CI' {
    It "Verify copyright year is current year" {

        $stdoutFile = "TestDrive:\stdout.txt"
        $bogusFile = $(New-Guid).Guid + ".ps1"
        # get the copyright banner to show without interactive shell by using -file
        # we expect an error from -file so ignore it
        & $powershellexe -file $bogusFile 1> $stdoutFile 2> $null
        $stdout = get-content $stdoutFile
        $foundCopyright = $false
        $foundYear = $false
        foreach ($line in $stdout) {
            if ($line.contains("Microsoft")) { # company name wouldn't be localized
                $foundCopyright = $true
                foreach ($word in $line.split(" ")) {
                    [int]$year = 0
                    try {
                        $year = [int]::Parse($word)
                    } catch [FormatException] {
                        # can be ignored for words
                    }
                    if ( $year -gt 0) { # find the year which is only number
                        $foundYear = $true
                        $year | Should Be (Get-Date).Year
                    }
                }
            } 
        }
        $foundCopyright | Should Be $true
        $foundYear | Should Be $true
    }
}
