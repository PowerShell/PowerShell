# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Tests for (error, warning, etc) action preference" -Tags "CI" {
        BeforeAll {
            $orgin = $GLOBAL:errorActionPreference
        }

        AfterAll {
            if ($GLOBAL:errorActionPreference -ne $orgin)
            {
                $GLOBAL:errorActionPreference = $orgin
            }
        }
        Context 'Setting ErrorActionPreference to stop prevents user from getting the error exception' {
            $err = $null
            try
            {
                get-childitem nosuchfile.nosuchextension -ErrorAction stop -ErrorVariable err
            }
            catch {}

            It '$err.Count' { $err.Count | Should -Be 1 }
            It '$err[0] should not be $null' { $err[0] | Should -Not -BeNullOrEmpty }
            It '$err[0].GetType().Name' { $err[0] | Should -BeOfType "System.Management.Automation.ActionPreferenceStopException" }
            It '$err[0].ErrorRecord' { $err[0].ErrorRecord | Should -Not -BeNullOrEmpty }
            It '$err[0].ErrorRecord.Exception.GetType().Name' { $err[0].ErrorRecord.Exception | Should -BeOfType "System.Management.Automation.ItemNotFoundException" }
        }

        It 'ActionPreference Ignore Works' {
            $errorCount = $error.Count
            Get-Process -Name asdfasdfsadfsadf -ErrorAction Ignore

            $error.Count | Should -BeExactly $errorCount
        }

        It 'action preference of Ignore cannot be set as a preference variable' {
            $e = {
                $GLOBAL:errorActionPreference = "Ignore"
                Get-Process -Name asdfasdfasdf
            } | Should -Throw -ErrorId 'System.NotSupportedException,Microsoft.PowerShell.Commands.GetProcessCommand' -PassThru
            $e.CategoryInfo.Reason | Should -BeExactly 'NotSupportedException'

            $GLOBAL:errorActionPreference = $orgin
        }

        It 'action preference of Suspend cannot be set as a preference variable' {
            $e = {
                $GLOBAL:errorActionPreference = "Suspend"
                Get-Process -Name asdfasdfasdf
            } | Should -Throw -ErrorId 'RuntimeException' -PassThru
            $e.CategoryInfo.Reason | Should -BeExactly 'ArgumentTransformationMetadataException'

            $GLOBAL:errorActionPreference = $orgin
        }

        It 'enum disambiguation works' {
            $errorCount = $error.Count
            Get-Process -Name asdfasdfsadfsadf -ErrorAction Ig

            $error.Count | Should -BeExactly $errorCount
        }

        It 'ErrorAction = Suspend works on Workflow' -Skip:$IsCoreCLR {
           . .\TestsOnWinFullOnly.ps1
            Run-TestOnWinFull "ActionPreference:ErrorAction=SuspendOnWorkflow"
        }

        It 'ErrorAction = Suspend does not work on functions' {
            function MyHelperFunction {
                [CmdletBinding()]
                param()
                "Hello"
            }

            { MyHelperFunction -ErrorAction Suspend } | Should -Throw -ErrorId "ParameterBindingFailed,MyHelperFunction"
        }

        It 'ErrorAction = Suspend does not work on cmdlets' {
            { Get-Process -ErrorAction Suspend } | Should -Throw -ErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.GetProcessCommand"
        }

        It 'WarningAction = Suspend does not work' {
            { Get-Process -WarningAction Suspend } | Should -Throw -ErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.GetProcessCommand"
        }

        #issue 2076
        It 'ErrorAction and WarningAction are the only action preferences do not support suspend' -Pending{
            $params = [System.Management.Automation.Internal.CommonParameters].GetProperties().Name | Select-String Action

            $suspendErrors = $null
            $num=0

            $params | ForEach-Object {
                        $input=@{'InputObject' = 'Test';$_='Suspend'}
                        { Write-Output @input } | Should -Throw -ErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.WriteOutputCommand"
                    }
        }

        It '<switch> does not take precedence over $ErrorActionPreference' -TestCases @(
            @{switch="Verbose"},
            @{switch="Debug"}
        ) {
            param($switch)
            $ErrorActionPreference = "SilentlyContinue"
            $params = @{
                ItemType = "File";
                Path = "$testdrive\test.txt";
                Confirm = $false
            }
            New-Item @params > $null
            $params += @{$switch=$true}
            { New-Item @params } | Should -Not -Throw
            $ErrorActionPreference = "Stop"
            { New-Item @params } | Should -Throw -ErrorId "NewItemIOError,Microsoft.PowerShell.Commands.NewItemCommand"
            Remove-Item "$testdrive\test.txt" -Force
        }
}
