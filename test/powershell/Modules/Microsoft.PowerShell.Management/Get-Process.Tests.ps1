# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-Process for admin" -Tags @('CI', 'RequireAdminOnWindows') {
    It "Should support -IncludeUserName" {
        (Get-Process -Id $pid -IncludeUserName).UserName | Should -Match $env:USERNAME
    }

    It "Should support -Module" -Pending:$IsMacOS {
        $modules = Get-Process -Id $pid -Module
        $modules.GetType() | Should -BeExactly "System.Object[]"
        foreach ($module in $modules) {
            $module.GetType() | Should -BeExactly "System.Diagnostics.ProcessModule"
        }
    }

    It "Should support -FileVersionInfo" {
        $pwshVersion = Get-Process -Id $pid -FileVersionInfo
        if ($IsWindows) {
            $PSVersionTable.PSVersion | Should -MatchExactly $pwshVersion.FileVersion
            $gitCommitId = $PSVersionTable.GitCommitId
            if ($gitCommitId.StartsWith("v")) { $gitCommitId = $gitCommitId.Substring(1) }
            $pwshVersion.ProductVersion.Replace("-dirty","") | Should -BeExactly $gitCommitId
        } else {
            $pwshVersion.FileVersion | Should -BeNullOrEmpty
        }
    }

    It "Run with parameter -FileVersionInfo should not stop responding on non Windows platform also when process' main module is null." -Skip:$IsWindows {
        # Main module for idle process can be null on non-Windows platforms
        { $pwshVersion = Get-Process -Id 0 -FileVersionInfo -ErrorAction Stop } | Should -Not -Throw
    }

    It "Run with parameter -FileVersionInfo for idle process should throw on Windows." -Skip:(!$IsWindows) {
        { $pwshVersion = Get-Process -Id 0 -FileVersionInfo -ErrorAction Stop } | Should -Throw -ErrorId "CouldNotEnumerateFileVer,Microsoft.PowerShell.Commands.GetProcessCommand"
    }
}

Describe "Get-Process" -Tags "CI" {
    # These tests are no good, please replace!
    BeforeAll {
        $ps = Get-Process
        $idleProcessPid = 0
    }
    It "Should return a type of Object[] for Get-Process cmdlet" -Pending:$IsMacOS {
        ,$ps | Should -BeOfType "System.Object[]"
    }

    It "Should have not empty Name flags set for Get-Process object" -Pending:$IsMacOS {
        $ps | foreach-object { $_.Name | Should -Not -BeNullOrEmpty }
    }

    It "Should throw an error for non existing process id." {
        $randomId = 123456789
        { Get-Process -Id $randomId -ErrorAction Stop } | Should -Throw -ErrorId "NoProcessFoundForGivenId,Microsoft.PowerShell.Commands.GetProcessCommand"
    }

    It "Should throw an exception when process id is null." {
        { Get-Process -id $null } | Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.GetProcessCommand"
    }

    It "Should throw an exception when -InputObject parameter is null." {
        { Get-Process -InputObject $null } | Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.GetProcessCommand"
    }

    It "Should not fail to get process name even if it is unavailable." {
        { (Get-Process -Id $idleProcessPid).Name } | Should -Not -Throw
    }

    It "Test for process property = Name" {
        (Get-Process -Id $pid).Name | Should -BeExactly "pwsh"
    }

    It "Test for process property = Id" {
        (Get-Process -Id $pid).Id | Should -BeExactly $pid
    }

    It "Should fail to run Get-Process with -IncludeUserName without admin" -Skip:(!$IsWindows)  {
        { Get-Process -IncludeUserName } | Should -Throw -ErrorId "IncludeUserNameRequiresElevation,Microsoft.PowerShell.Commands.GetProcessCommand"
    }

    It "Should fail to run Get-Process with -Module without admin" -Skip:(!$IsWindows) {
        { Get-Process -Module -ErrorAction Stop } | Should -Throw -ErrorId "CouldNotEnumerateModules,Microsoft.PowerShell.Commands.GetProcessCommand"
    }

    It "Should fail to run Get-Process with -FileVersionInfo without admin" -Skip:(!$IsWindows) {
        { Get-Process -FileVersionInfo -ErrorAction Stop } | Should -Throw -ErrorId "CouldNotEnumerateFileVer,Microsoft.PowerShell.Commands.GetProcessCommand"
    }
}

Describe "Get-Process Formatting" -Tags "Feature" {
    It "Should not have Handle in table format header" {
        $types = "System.Diagnostics.Process","System.Diagnostics.Process#IncludeUserName"

        foreach ($type in $types) {
            $formatData = Get-FormatData -TypeName $type -PowerShellVersion $PSVersionTable.PSVersion
            $tableControls = $formatData.FormatViewDefinition | Where-Object {$_.Control -is "System.Management.Automation.TableControl"}
            foreach ($tableControl in $tableControls) {
                $tableControl.Control.Headers.Label -match "Handle*" | Should -BeNullOrEmpty
                # verify that rows without headers isn't the handlecount (as PowerShell will create a header that matches the property name)
                $tableControl.Control.Rows.Columns.DisplayEntry.Value -eq "HandleCount" | Should -BeNullOrEmpty
            }
        }
    }
}

Describe "Process Parent property" -Tags "CI" {
    It "Has Parent process property" {
        $powershellexe = (get-process -id $PID).mainmodule.filename
        & $powershellexe -noprofile -command '(Get-Process -Id $pid).Parent' | Should -Not -BeNullOrEmpty
    }

    It "Has valid parent process ID property" {
        $powershellexe = (get-process -id $PID).mainmodule.filename
        & $powershellexe -noprofile -command '(Get-Process -Id $pid).Parent.Id' | Should -Be $pid
    }
}

