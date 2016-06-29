##
##  Copyright (c) Microsoft Corporation, 2015
##
##  ConsoleHost Session Configuration tests
##

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
    Remove-Item -Path $regHKLMKey -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $regHKCUKey -Force -ErrorAction SilentlyContinue
}

# Ensure that there is no console session configuration group policy settings
ClearConsoleSessionConfigurationGroupPolicy


Describe "Confirms that default powershell.exe runs in non-configured sessions" -Tags 'InnerLoop','DRT' {

    # Run powershell command in non-configured session
    $results = powershell.exe -command 'Write-Output $PSenderInfo'

    It "Verifies that command does not run in configured loop-back session" {
        $results | Should Be $null
    }
}


Describe "Tests powershell.exe session configuration switch parameter" -Tags 'InnerLoop','P1' {

    # Run powershell command in configuration session
    $results = powershell.exe -config microsoft.powershell -command 'Write-Output $PSSenderInfo'

    # Command will be run in loop-back remote session
    It "Verifies powershell configured session command results contain expected PSSenderInfo RunAsUser" {
        $results | Should Not Be $null
        ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
    }
}


Describe "Tests powershell.exe session configuration with -File switch" -Tags 'InnerLoop','P1' {

    if (($path -eq $null) -or ! (Test-Path $path))
    {
        "Skipping test because cannot access path"
        return
    }

    try
    {
        $fileName = "PSConfig102.ps1"
        $filePath = Join-Path $path $fileName
        'Write-Output $PSSenderInfo' > $filePath

        # Run powershell script file in configuration session
        $results = powershell.exe -config microsoft.powershell -file $filePath

        # Script file will be run in loop-back remote session
        It "Verifies powershell configured session script file results contain expected PSSenderInfo RunAsUser" {
            $results | Should Not Be $null
            ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
        }
    }
    finally
    {
        if ($filePath -ne $null) { Remove-Item $filePath -Force -ErrorAction SilentlyContinue }
    }
}


Describe "Tests powershell.exe session configuration from HKLM Group Policy setting" -Tags 'InnerLoop','P1' {

    try
    {
        if (! (Test-Path $regHKLMKey)) { New-Item -Path $regHKLMKey -Force }

        # Enable Group Policy console session configuration
        Set-ItemProperty -Path $regHKLMKey -Name $regEnable -Value 1

        # Set Group Policy console session configuration name
        Set-ItemProperty -Path $regHKLMKey -Name $regName -Value $regValue

        # Run powershell.exe command
        $results = powershell.exe -command 'Write-Output $PSSenderInfo'

        # Command should be run in powershell configured endpoint (microsoft.powershell)
        It "Verifies powershell configured session expected PSSenderInfo result contains RunAsUser" {
            $results | Should Not Be $null
            ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
        }
    }
    finally
    {
        ClearConsoleSessionConfigurationGroupPolicy
    }
}


Describe "Tests powershell.exe session configuration from HKCU Group Policy setting" -Tags 'InnerLoop','P1' {

    try
    {
        if (! (Test-Path $regHKCUKey)) { New-Item -Path $regHKCUKey -Force }

        # Enable Group Policy console session configuration
        Set-ItemProperty -Path $regHKCUKey -Name $regEnable -Value 1

        # Set Group Policy console session configuration name
        Set-ItemProperty -Path $regHKCUKey -Name $regName -Value $regValue

        # Run powershell.exe command
        $results = powershell.exe -command 'Write-Output $PSSenderInfo'

        # Command should be run in powershell configured endpoint (microsoft.powershell)
        It "Verifies powershell configured session expected PSSenderInfo result contains RunAsUser" {
            $results | Should Not Be $null
            ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
        }
    }
    finally
    {
        ClearConsoleSessionConfigurationGroupPolicy
    }
}


Describe "Tests powershell.exe session configuration with restricted custom endpoint" -Tags 'InnerLoop','P1' {

    try
    {
        if (($path -eq $null) -or ! (Test-Path $path))
        {
            "Skipping test because cannot access path"
            return
        }

        $filePath = Join-Path $path "Test201A.pssc"
        $configName = "Test201AConfig"

        New-PSSessionConfigurationFile -Path $filePath -SessionType RestrictedRemoteServer `
            -VisibleCmdlets 'Get-Command','Get-Runspace','Get-Module','Write-Output' `
            -ModulesToImport PSScheduledJob
        $configuration = Register-PSSessionConfiguration -Name $configName -Path $filePath -Force -ErrorAction SilentlyContinue
        It "Verifies the custom configuration was created successfully" {
            $configuration | Should Not Be $null
        }

        # Verify running in configured restricted session
        $results = powershell.exe -config $configName -command 'Get-ChildItem' 2>&1
        It "Verifies that the Get-ChildItem command cannot run" {
            $results | Should Not Be $null
            ($results | ? { $_ -match "CommandNotFoundException" }) | Should Not Be $null
        }

        # Get Get-Runspace command
        $results = powershell.exe -config $configName -command 'Get-Command Get-Runspace'
        It "Verifies results returned from powershell running in custom test configuration" {
            $results | Should Not Be $null
            $results | ? { $_ -match "Get-Runspace" } | Should Not Be $null
        }

        # Get module list
        $results = powershell.exe -config $configName -command 'Get-Module'
        It "Verifies results returned contain the expected PSScheduledJob module" {
            $results | Should Not Be $null
            $results | ? { $_ -match "PSScheduledJob" } | Should Not Be $null
        }
    }
    finally
    {
        if ($configuration -ne $null) { $configuration | Unregister-PSSessionConfiguration -Force -ErrorAction SilentlyContinue }
        if ($filePath -ne $null) { Remove-Item $filePath -Force -ErrorAction SilentlyContinue }
    }
}


Describe "Tests powershell.exe session configuration with encoded command" -Tags 'InnerLoop','P1' {

    $command = 'Write-Output $PSSenderInfo'
    $eCommand = [System.Convert]::ToBase64String(
        [System.Text.Encoding]::Unicode.GetBytes($command)
    )

    # Run powershell command in configured session (microsoft.powershell)
    $results = powershell.exe -config microsoft.powershell -EncodedCommand $eCommand

    # Command will be run in loop-back remote session
    It "Verifies powershell configured session encoded command results contain expected PSSenderInfo RunAsUser" {
        $results | Should Not Be $null
        ($results | ? { $_ -match "RunAsUser" }) | Should Not Be $null
    }
}

Describe "Verify powershell.exe banner copyright notice" -Tags 'InnerLoop','P1' {
    
    It "Verify copyright year is current year" {

        $stdoutFile = "TestDrive:\stdout.txt"
        $bogusFile = $(New-Guid).Guid + ".ps1"
        # get the copyright banner to show without interactive shell by using -file
        # we expect an error from -file so ignore it
        powershell -file $bogusFile 1> $stdoutFile 2> $null
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