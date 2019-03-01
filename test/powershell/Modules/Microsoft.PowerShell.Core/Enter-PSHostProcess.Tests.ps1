# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

function Get-PipePath {
    param (
        $PipeName
    )
    if ($IsWindows) {
        return "\\.\pipe\$PipeName"
    }
    "$([System.IO.Path]::GetTempPath())CoreFxPipe_$PipeName"
}

function Start-PSProcess {
    param (
        [switch] $WindowsPowerShell,
        [array] $ArgumentList
    )

    $pwsh = if ($WindowsPowerShell) { "powershell" } else { "pwsh" }

    $params = @{
        FilePath = $pwsh
        PassThru = $true
        # RedirectStandardOutput = "TestDrive:\$([System.IO.Path]::GetRandomFileName())"
        # RedirectStandardError = "TestDrive:\$([System.IO.Path]::GetRandomFileName())"
        # RedirectStandardInput = New-TemporaryFile
    }

    if ($ArgumentList) {
        $params.ArgumentList = $ArgumentList
    }

    Start-Process @params
}

Describe "Enter-PSHostProcess tests" -Tag Feature {
    Context "By Process Id" {

        It "Can enter, exit, and re-enter another PSHost" {
            # $pwsh = Start-PSProcess

            try {
                # Wait-UntilTrue { (Get-PSHostProcessInfo -Id $pwsh.Id) -ne $null }

@'
Enter-PSHostProcess -Id {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $pid | pwsh -c - | Should -Be $pid

@'
Enter-PSHostProcess -Id {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $pid | pwsh -c - | Should -Be $pid

            } finally {
                $pwsh | Stop-Process -Force -ErrorAction SilentlyContinue
            }
        }

        It "Can enter and exit another Windows PowerShell PSHost" -Skip:(!$IsWindows) {
            $powershell = Start-PSProcess -WindowsPowerShell

            try {
                Wait-UntilTrue { (Get-PSHostProcessInfo -Id $powershell.Id) -ne $null }

@'
Enter-PSHostProcess -Id {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $powershell.Id | pwsh -c - | Should -Be $powershell.Id

            } finally {
                $powershell | Stop-Process -Force -ErrorAction SilentlyContinue
            }
        }

        It "Can enter using NamedPipeConnectionInfo" {
            $pwsh = Start-PSProcess

            try {
                Wait-UntilTrue { (Get-PSHostProcessInfo -Id $pwsh.Id) -ne $null }

                $npInfo = [System.Management.Automation.Runspaces.NamedPipeConnectionInfo]::new($pwsh.Id)
                $rs = [runspacefactory]::CreateRunspace($npInfo)
                $rs.Open()
                $ps = [powershell]::Create()
                $ps.Runspace = $rs
                $ps.AddScript('$pid').Invoke() | Should -Be $pwsh.Id
            } finally {
                $rs.Dispose()
                $pwsh | Stop-Process -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context "By CustomPipeName" {

        It "Can enter, exit, and re-enter using CustomPipeName" {
            $pipeName = [System.IO.Path]::GetRandomFileName()
            $pipePath = Get-PipePath -PipeName $pipeName
            $pwsh = Start-PSProcess -ArgumentList @("-CustomPipeName",$pipeName)

            try {
                Wait-UntilTrue { Test-Path $pipePath }

@'
Enter-PSHostProcess -CustomPipeName {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $pipeName | pwsh -c - | Should -Be $pwsh.Id

@'
Enter-PSHostProcess -CustomPipeName {0} -ErrorAction Stop
$pid
Exit-PSHostProcess
'@ -f $pipeName | pwsh -c - | Should -Be $pwsh.Id

            } finally {
                $pwsh | Stop-Process -Force -ErrorAction SilentlyContinue
            }
        }

        It "Should throw if CustomPipeName does not exist" {
            { Enter-PSHostProcess -CustomPipeName badpipename } | Should -Throw -ExpectedMessage "No named pipe was found with CustomPipeName: badpipename."
        }

        It "Should throw if CustomPipeName is too long on Linux or macOS" -Skip:($IsWindows) {
            $longPipeName = "DoggoipsumwaggywagssmolborkingdoggowithalongsnootforpatsdoingmeafrightenporgoYapperporgolongwatershoobcloudsbigolpupperlengthboy"

            "`$pid" | pwsh -CustomPipeName $longPipeName -c -
            # 64 is the ExitCode for BadCommandLineParameter
            $LASTEXITCODE | Should -Be 64
        }
    }
}
