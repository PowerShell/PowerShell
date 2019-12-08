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
        BeforeAll {
            $cmds = @(
                @{ cmd = { Get-FormatData System.IO.FileInfo } }
                @{ cmd = { (Get-FormatData System.IO.FileInfo &) | Receive-Job -Wait -AutoRemoveJob } }
            )
        }
        It "Can get format data requiring v5.1 with <cmd>" -TestCases $cmds {
            param([scriptblock] $cmd)
            (& $cmd).TypeNames | Should -Contain 'System.IO.FileInfo'
        }
    }

    Context "Can override client version with -PowerShellVersion" {
        BeforeAll {
            $cmds = @(
                @{ cmd = { Get-FormatData System.IO.FileInfo -PowerShellVersion 5.0 }; shouldBeNull = $true },
                @{ cmd = { Get-FormatData System.IO.FileInfo -PowerShellVersion 5.1 }; shouldBeNull = $false }
            )
        }
        It "<cmd> should return <shouldBeNull> for a null-output test" -TestCases $cmds {
            param([scriptblock] $cmd, [bool] $shouldBeNull)
            $null -eq $(& $cmd) | Should -Be $shouldBeNull
        }
    }

    Context "Remote use: Don't get format data requiring v5.1 by default" {
        BeforeAll {
            # Simulate a remoting session from a v5.0- client.
            $PSSenderInfo = [System.Management.Automation.Remoting.PSSenderInfo]::new(
                [System.Management.Automation.Remoting.PSPrincipal]::new(
                    [System.Management.Automation.Remoting.PSIdentity]::new('none', $true, 'foo', $null),
                    $null
                ),
                'http://example.org:5985/wsman?PSVersion=3.0'
            )
            $PSSenderInfo.ApplicationArguments = [psobject] [PSPrimitiveDictionary]::new(@{
                'PSVersiontable' = [psobject] [PSPrimitiveDictionary]::new(@{
                    'PSVersion' = [Version]::new(3, 0)
                })
            })
            $cmds = @(
                @{ cmd = { Get-FormatData System.IO.FileInfo }; shouldBeNull = $true },
                @{ cmd = { Get-FormatData System.IO.FileInfo -PowerShellVersion 5.1 }; shouldBeNull = $false }
            )
        }
        It "When remoting, <cmd> should return <shouldBeNull> for a null-output test" -TestCases $cmds {
            param([scriptblock] $cmd, [bool] $shouldBeNull)
            $null -eq $(& $cmd) | Should -Be $shouldBeNull
        }
    }

}
