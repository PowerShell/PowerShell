# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

function Test-Elevated {
    [CmdletBinding()]
    [OutputType([bool])]
    Param()

    # if the current Powershell session was called with administrator privileges,
    # the Administrator Group's well-known SID will show up in the Groups for the current identity.
    # Note that the SID won't show up unless the process is elevated.
    return (([Security.Principal.WindowsIdentity]::GetCurrent()).Groups -contains "S-1-5-32-544")
}

function Invoke-Msiexec {
    param(
        [Parameter(ParameterSetName = 'Install', Mandatory)]
        [Switch]$Install,

        [Parameter(ParameterSetName = 'Uninstall', Mandatory)]
        [Switch]$Uninstall,

        [Parameter(Mandatory)]
        [ValidateScript({Test-Path -Path $_})]
        [String]$MsiPath,

        [Parameter(ParameterSetName = 'Install')]
        [HashTable] $Properties

    )
    $action = "$($PsCmdlet.ParameterSetName)ing"
    if ($Install.IsPresent) {
        $switch = '/I'
    } else {
        $switch = '/x'
    }

    $additionalOptions = @()
    if ($Properties) {
        foreach ($key in $Properties.Keys) {
            $additionalOptions += "$key=$($Properties.$key)"
        }
    }

    $argumentList = "$switch $MsiPath /quiet /l*vx $msiLog $additionalOptions"
    $msiExecProcess = Start-Process msiexec.exe -Wait -ArgumentList $argumentList -NoNewWindow -PassThru
    if ($msiExecProcess.ExitCode -ne 0) {
        $exitCode = $msiExecProcess.ExitCode
        throw "$action MSI failed and returned error code $exitCode."
    }
}

Describe -Name "Windows MSI" -Fixture {
    BeforeAll {
        $msiX64Path = $env:PsMsiX64Path

        # Get any existing powershell core in the path
        $beforePath = @(([System.Environment]::GetEnvironmentVariable('PATH', 'MACHINE')) -split ';' |
                Where-Object {$_ -like '*files\powershell*'})

        $msiLog = Join-Path -Path $TestDrive -ChildPath 'msilog.txt'

        foreach ($pathPart in $beforePath) {
            Write-Warning "Found existing PowerShell path: $pathPart"
        }

        if (!(Test-Elevated)) {
            Write-Warning "Tests must be elevated"
        }
        $uploadedLog = $false
    }
    BeforeEach {
        $Error.Clear()
    }
    AfterEach {
        if ($Error.Count -ne 0 -and !$uploadedLog) {
            Copy-Item -Path $msiLog -Destination $env:temp -Force
            Write-Verbose "MSI log is at $env:temp\msilog.txt" -Verbose
            $uploadedLog = $true 
        }
    }

    Context "Add Path disabled" {
        It "MSI should install without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Install -MsiPath $msiX64Path -Properties @{ADD_PATH = 0}
            } | Should -Not -Throw
        }

        It "MSI should have not be updated path" -Skip:(!(Test-Elevated)) {
            $psPath = ([System.Environment]::GetEnvironmentVariable('PATH', 'MACHINE')) -split ';' |
                Where-Object {$_ -like '*files\powershell*' -and $_ -notin $beforePath}

            $psPath | Should -BeNullOrEmpty
        }

        It "MSI should uninstall without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Uninstall -MsiPath $msiX64Path
            } | Should -Not -Throw
        }
    }

    Context "Add Path enabled" {
        It "MSI should install without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Install -MsiPath $msiX64Path -Properties @{ADD_PATH = 1}
            } | Should -Not -Throw
        }

        It "MSI should have updated path" -Skip:(!(Test-Elevated)) {
            $psPath = ([System.Environment]::GetEnvironmentVariable('PATH', 'MACHINE')) -split ';' |
                Where-Object {$_ -like '*files\powershell*' -and $_ -notin $beforePath}

            $psPath | Should -Not -BeNullOrEmpty
        }

        It "MSI should uninstall without error" -Skip:(!(Test-Elevated)) {
            {
                Invoke-MsiExec -Uninstall -MsiPath $msiX64Path
            } | Should -Not -Throw
        }
    }
}
