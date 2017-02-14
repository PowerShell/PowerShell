Import-Module $PSScriptRoot\..\..\Common\Test.Helpers.psm1

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
                get-childitem nosuchfile.nosuchextension -ea stop -ev err
            }
            catch {}

            It '$err.Count' { $err.Count | Should Be 1 }
            It '$err[0] should not be $null' { $err[0] | Should Not Be $null }
            It '$err[0].GetType().Name' { $err[0] | Should BeOfType "System.Management.Automation.ActionPreferenceStopException" }
            It '$err[0].ErrorRecord' { $err[0].ErrorRecord | Should not BeNullOrEmpty }
            It '$err[0].ErrorRecord.Exception.GetType().Name' { $err[0].ErrorRecord.Exception | Should BeOfType "System.Management.Automation.ItemNotFoundException" }
        }

        It 'ActionPreference Ignore Works' {
            $errorCount = $error.Count
            Get-Process -Name asdfasdfsadfsadf -ErrorAction Ignore

            $error.Count | Should Be $errorCount
        }

        It 'action preference of Ignore cannot be set as a preference variable' {
            try {
                { $GLOBAL:errorActionPreference = "Ignore" } | Should Not Throw
                $exc = { Get-Process -Name asdfasdfasdf } | ShouldBeErrorId "System.NotSupportedException,Microsoft.PowerShell.Commands.GetProcessCommand"
                $exc.exception.Message | Should Match "Ignore .* ActionPreference"
            }
            finally {
                $GLOBAL:errorActionPreference = $orgin
            }
        }

        It 'action preference of Suspend cannot be set as a preference variable' {
            try {
                    $exc = {
                        $GLOBAL:errorActionPreference = "Suspend"
                    } | ShouldBeErrorId "RuntimeException"
                    $exc.exception.Message | Should Match "Suspend .* ActionPreference"
            }
            finally {
                $GLOBAL:errorActionPreference = $orgin
            }
        }

        It 'enum disambiguation works' {
            $errorCount = $error.Count
            Get-Process -Name asdfasdfsadfsadf -ErrorAction Ig

            $error.Count | Should Be $errorCount
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

            { MyHelperFunction -ErrorAction Suspend } | ShouldBeErrorId "ParameterBindingFailed,MyHelperFunction"
        }

        It 'ErrorAction = Suspend does not work on cmdlets' {
            { Get-Process -ErrorAction Suspend } | ShouldBeErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.GetProcessCommand"
        }

        It 'WarningAction = Suspend does not work' {
            { Get-Process -WarningAction Suspend } | ShouldBeErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.GetProcessCommand"
        }

        #issue 2076
        It 'ErrorAction and WarningAction are the only action preferences do not support suspend' -Pending{
            $params = [System.Management.Automation.Internal.CommonParameters].GetProperties().Name | Select-String Action

            $suspendErrors = $null
            $num=0

            $params | % {
                        $input=@{'InputObject' = 'Test';$_='Suspend'}

                        try {
                            Write-Output @input
                            } catch {
                                $_.FullyQualifiedErrorId | Should Be "ParameterBindingFailed,Microsoft.PowerShell.Commands.WriteOutputCommand"
                                $num++
                            }
                    }
            $num | Should Be 2
        }
}
