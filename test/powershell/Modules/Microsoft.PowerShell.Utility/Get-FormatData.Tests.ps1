# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-FormatData" -Tags "CI" {

    Context "Check return type of Get-FormatData" {
        It "Should return an object[] as the return type" {
            $result = Get-FormatData
            , $result | Should -BeOfType "System.Object[]"
        }
    }

    Context "Local use: Can get format data requiring v5.1 by default" {
        $cmds =
            @{ cmd = { Get-FormatData System.IO.FileInfo } },
            @{ cmd = { (Get-FormatData System.IO.FileInfo &) | Receive-Job -Wait -AutoRemoveJob } }
        It "Can get format data requiring v5.1 with <cmd>" -TestCases $cmds {
            param([scriptblock] $cmd)
            $format = & $cmd
            $format.TypeNames | Should -HaveCount 2
            $format.TypeNames[0] | Should -BeExactly "System.IO.DirectoryInfo"
            $format.TypeNames[1] | Should -BeExactly "System.IO.FileInfo"
            $format.FormatViewDefinition | Should -HaveCount ($IsWindows ? 4 : 5)
        }
    }

    Context "Remote use: Don't get format data requiring v5.1 by default" {
        # Simulate a remoting session.
        $PSSenderInfo = [pscustomobject] @{ PSShowComputerName = $true }
        $cmds =
            @{ cmd = { Get-FormatData System.IO.FileInfo }; shouldBeNull = $true },
            @{ cmd = { Get-FormatData System.IO.FileInfo -PowerShellVersion 5.1 }; shouldBeNull = $false }
        It "When remoting, <cmd> should return <shouldBeNull> for a null-output test" -TestCases $cmds {
            param([scriptblock] $cmd, [bool] $shouldBeNull)
            $null -eq $(& $cmd) | Should -Be $shouldBeNull
        }
    }

}
