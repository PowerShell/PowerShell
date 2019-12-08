# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-FormatData" -Tags "CI" {

    Context "Check return type of Get-FormatData" {
        It "Should return an object[] as the return type" {
            $result = Get-FormatData
            , $result | Should -BeOfType "System.Object[]"
        }
    }

    # Note: Format data for [System.IO.FileInfo] (among others) is not to be
    #       returned to remoting v5.0- remoting clients.

    Context "Local use: Can get format data requiring v5.1+ by default" {
        BeforeAll {
            $cmds = @(
                @{ cmd = { Get-FormatData System.IO.FileInfo } }
                @{ cmd = { (Get-FormatData System.IO.FileInfo &) | Receive-Job -Wait -AutoRemoveJob } }
            )
        }
        It "Can get format data requiring v5.1+ with <cmd>" -TestCases $cmds {
            param([scriptblock] $cmd)
            (& $cmd).TypeNames | Should -Contain 'System.IO.FileInfo'
        }
    }

    Context "Can override client version with -PowerShellVersion" {
        BeforeAll {
            $cmds = @(
                @{ shouldBeNull = $true; cmd = { Get-FormatData System.IO.FileInfo -PowerShellVersion 5.0 } }
                @{ shouldBeNull = $false; cmd = { Get-FormatData System.IO.FileInfo -PowerShellVersion 5.1 } }
                @{ shouldBeNull = $false; cmd = { $PSSenderInfo = [System.Management.Automation.Internal.InternalTestHooks]::GetCustomPSSenderInfo('foo', [version] '5.0'); Get-FormatData System.IO.FileInfo -PowerShellVersion 5.1 } }
            )
        }
        It "<cmd> should return <shouldBeNull> for a null-output test" -TestCases $cmds {
            param([scriptblock] $cmd, [bool] $shouldBeNull)
            $null -eq $(& $cmd) | Should -Be $shouldBeNull
        }
    }

    Context "Remote use: Don't get format data requiring v5.1 by default" {
        BeforeAll {
            # Simulated PSSenderInfo instances for various PowerShell versions.
            $pssiV50 = [System.Management.Automation.Internal.InternalTestHooks]::GetCustomPSSenderInfo('foo', [version] '5.0')
            $pssiV51 = [System.Management.Automation.Internal.InternalTestHooks]::GetCustomPSSenderInfo('foo', [version] '5.1')
            $pssiV70 = [System.Management.Automation.Internal.InternalTestHooks]::GetCustomPSSenderInfo('foo', [version] '7.0')
            $cmds = @(
                @{ shouldBeNull = $true; cmd = { $PSSenderInfo = $pssiV50; Get-FormatData System.IO.FileInfo } }
                @{ shouldBeNull = $false; cmd = { $PSSenderInfo = $pssiV51; Get-FormatData System.IO.FileInfo } }
                @{ shouldBeNull = $false; cmd = { $PSSenderInfo = $pssiV70; Get-FormatData System.IO.FileInfo } }
            )
        }
        It "When remoting, <cmd> should return <shouldBeNull> for a null-output test" -TestCases $cmds {
            param([scriptblock] $cmd, [bool] $shouldBeNull)
            $null -eq $(& $cmd) | Should -Be $shouldBeNull
        }
    }

}
