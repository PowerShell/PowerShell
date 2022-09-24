# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'WinEnvironmentVariable cmdlet tests' -Tag CI {

    Context "EnvironmentVariable" {
        BeforeAll {
            $processguid = New-Guid
            $userguid = New-Guid
            Set-WinEnvironmentVariable -Value $processguid -Name foo -Target Process
            Set-WinEnvironmentVariable -Value $userguid -Name foo -Target User -Force
        }

        AfterAll {
            Set-WinEnvironmentVariable -Value "" -Name foo -Target Process
            Set-WinEnvironmentVariable -Value "" -Name foo -Target User
        }
        It 'Get-WinEnvironmentVariable returns different environment variables are referenced for each Target.' {

            $processfoo = Get-WinEnvironmentVariable -Name foo -Target Process
            $userfoo = Get-WinEnvironmentVariable -Name foo -Target User
            $processfoo | Should -Not -Be $userfoo
        }

        It '-Target Machine is Settable, but Require Administrator authentication' {
            $machineguid = New-Guid
            { $machineguid | Set-WinEnvironmentVariable -Name foo -Target Machine -Force} |
                Should -Throw -ErrorId "PermissionDenied,Microsoft.PowerShell.Commands.SetWinEnvironmentVariable"
        }
        Context "Target Process" {

            AfterEach {
                Set-WinEnvironmentVariable -Value "" -Name bar -Target Process
            }

            It 'Get-WinEnvironmentVariable returns [EnvironmentVariable]::GetEnvironmentVariables in same Target' {
                $envs = Get-WinEnvironmentVariable -Target Process | Select-Object Name, Value | Sort-Object Name

                $envsName = $envs | Select-Object -ExpandProperty Name
                $envsValue = $envs | Select-Object -ExpandProperty Value

                $check = [Environment]::GetEnvironmentVariables("Process").GetEnumerator() | Select-Object Name, Value |Sort-Object Name
                $check | Select-Object -ExpandProperty Name| Should -Be $envsName
                $check | Select-Object -ExpandProperty Value| Should -Be $envsValue
            }
            It 'Get-WinEnvironmentVariable -Name returns what is in Set-WinEnvironmentVariable in same Target' {
                $guid = New-Guid | Select-Object -ExpandProperty Guid
                Set-WinEnvironmentVariable -Value $guid -Name bar -Target Process
                Get-WinEnvironmentVariable -Name bar -Target Process | Select-Object -ExpandProperty Value | Should -Be $guid
            }

            It 'Get-WinEnvironmentVariable -Name returns an array if EnvironmentVariable Path, PATHEXT or PSMOdulePath' {
                $DetectedDelimiterEnvrionmentVariable = @("Path", "PATHEXT", "PSModulePath")
                foreach($envname in $DetectedDelimiterEnvrionmentVariable){
                    @(1, 2) | Set-WinEnvironmentVariable -Name $envname -Target Process
                    Get-WinEnvironmentVariable -Name $envname -Target Process
                    $out = Get-WinEnvironmentVariable -Name $envname -Target Process
                    $out.Value.Count | Should -Be 2
                    $out.Value[0] | Should -Be 1
                    $out.Value[1] | Should -Be 2
                }
            }

            It 'Get-WinEnvironmentVariable -Delimiter returns an array if EnvironmentVariable has specify Symbol' {
                "1,2,3" | Set-WinEnvironmentVariable -Name bar -Delimiter "," -Target Process
                $out = Get-WinEnvironmentVariable -Name bar -Delimiter "," -Target Process
                $out.Value.Count | Should -Be 3
                $out.Value[0] | Should -Be 1
                $out.Value[1] | Should -Be 2
                $out.Value[2] | Should -Be 3
            }
            It 'Get-WinEnvironmentVariable -Raw returns one item if EnvironmentVariable Path or PATHTXT' {
                1,2 | Set-WinEnvironmentVariable -Name Path -Target Process
                (Get-WinEnvironmentVariable -Name Path -Target Process -Raw).Count | Should -Be 1
                Get-WinEnvironmentVariable -Name Path -Target Process -Raw | Should -BeExactly "1$([System.IO.Path]::PathSeparator)2"
            }
            It 'Get-WinEnvironmentVariable -Append will add Path, PATHTXT txt' {
                'hello' | Set-WinEnvironmentVariable -Name Path -Target Process
                'world' | Set-WinEnvironmentVariable -Name Path -Target Process -Append
                Get-WinEnvironmentVariable -Name Path -Target Process -Raw | Should -BeExactly "hello$([System.IO.Path]::PathSeparator)world"
            }

            It 'Get-WinEnvironmentVariable  if EnvironmentVariable is not exist, thorw error' {
                { Get-WinEnvironmentVariable -Name NotExists -Target Process } |
                    Should -Throw -ErrorId "EnvironmentVariableNotFound,Microsoft.PowerShell.Commands.GetWinEnvironmentVariable"
            }
            It 'Set-WinEnvironmentVariable -Append will add txt on ther EnvironmentVariable' {
                'hello' | Set-WinEnvironmentVariable -Name bar -Target Process
                'world' | Set-WinEnvironmentVariable -Name bar -Target Process -Append -Delimiter ","
                Get-WinEnvironmentVariable -Name bar -Target Process -Raw | Should -BeExactly "hello,world"
            }
            It 'Set-WinEnvironmentVariable removes all leading and trailing occurrences of a set of blank character' {
                '   hello' | Set-WinEnvironmentVariable -Name bar -Target Process
                '   world!   ' | Set-WinEnvironmentVariable -Name bar -Target Process -Append -Delimiter ";"
                @(' What your name   ', ' sir? ') | Set-WinEnvironmentVariable -Name bar -Target Process -Append -Delimiter ";"
                Get-WinEnvironmentVariable -Name bar -Target Process -Raw | Should -BeExactly "hello$([System.IO.Path]::PathSeparator)world!$([System.IO.Path]::PathSeparator)What your name$([System.IO.Path]::PathSeparator)sir?"
            }

            It 'Set-WinEnvironmentVariable -Append needs to separator' {
                'hello' | Set-WinEnvironmentVariable -Name bar -Target Process
                { 'world' | Set-WinEnvironmentVariable -Name bar -Target Process -Append } |
                    Should -Throw -ErrorId "DelimiterNotDetected,Microsoft.PowerShell.Commands.SetWinEnvironmentVariable"
            }
            It 'Set-WinEnvironemtVariable, if -Value is List, needs to Delimiter' {
                { @('hello', 'World') | Set-WinEnvironmentVariable -Name bar -Target Process } |
                    Should -Throw -ErrorId "DelimiterNotDetected,Microsoft.PowerShell.Commands.SetWinEnvironmentVariable"
            }

            It 'Set-WinEnvironmentVariable should not return object' {
                $result = 'hello' | Set-WinEnvironmentVariable -Name bar -Target Process
                $result | Should -BeNullOrEmpty
            }
            It 'Set-WinEnvironmentVariable, if -Value is "", remove EnvironmentVariable' {
                'hello' | Set-WinEnvironmentVariable -Name bar -Target Process
                Set-WinEnvironmentVariable -Value "" -Name bar -Target Process

                { Get-WinEnvironmentVariable -Name bar -Target Process } |
                    Should -Throw -ErrorId "EnvironmentVariableNotFound,Microsoft.PowerShell.Commands.GetWinEnvironmentVariable"
            }
        }

    }

}
