# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Tests for (error, warning, etc) action preference" -Tags "CI" {
    $commonActionPreferenceParameterTestCases = foreach ($commonParameterName in [System.Management.Automation.Cmdlet]::CommonParameters | Select-String Action) {
        @{
            ActionPreferenceParameterName = $commonParameterName
        }
    }

    $actionPreferenceVariableTestCases = foreach ($variable in Get-Variable -Name *Preference -Scope Global | Where-Object Value -Is [System.Management.Automation.ActionPreference]) {
        @{
            ActionPreferenceVariableName = $variable.Name
            StreamName = $variable.Name -replace '(Action)?Preference$'
        }
    }

    $actionPreferenceVariableValueTestCases = @(
        @{
            Value = [System.Management.Automation.ActionPreference]::Suspend
            DisplayValue = '[System.Management.Automation.ActionPreference]::Suspend'
        }
        @{
            Value        = 'Suspend'
            DisplayValue = '''Suspend'''
        }
    )

    BeforeAll {
        $orgin = $GLOBAL:errorActionPreference

        function Join-TestCase {
            [OutputType([Hashtable[]])]
            [CmdletBinding()]
            param(
                [Hashtable[]]$Set1,
                [Hashtable[]]$Set2
            )
            foreach ($ht1 in $Set1) {
                foreach ($ht2 in $Set2) {
                    $ht1 + $ht2
                }
            }
        }

        function Test-ActionPreferenceVariableSuspendValue {
            [CmdletBinding()]
            param(
                $Value
            )
            if ($DebugPreference -eq $Value) {
                Write-Debug -Message 'A debug message'
            } elseif ($ErrorActionPreference -eq $Value) {
                Write-Error -Message 'An error message'
            } elseif ($InformationPreference -eq $Value) {
                Write-Information -MessageData 'Some information'
            } elseif ($ProgressPreference -eq $Value) {
                Write-Progress -Activity 'Some progress'
            } elseif ($VerbosePreference -eq $Value) {
                Write-Verbose -Message 'A verbose message'
            } elseif ($WarningPreference -eq $Value) {
                Write-Warning -Message 'A warning message'
            }
        }
    }

    AfterAll {
        if ($GLOBAL:errorActionPreference -ne $orgin) {
            $GLOBAL:errorActionPreference = $orgin
        }
    }

    Context 'Setting ErrorActionPreference to stop prevents user from getting the error exception' {
        $err = $null
        try {
            Get-ChildItem nosuchfile.nosuchextension -ErrorAction stop -ErrorVariable err
        } catch { }

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

    It 'The $global:<ActionPreferenceVariableName> variable does not support Suspend' -TestCases $actionPreferenceVariableTestCases {
        param($ActionPreferenceVariableName)

        $e = {
            Set-Variable -Name $ActionPreferenceVariableName -Scope Global -Value ([System.Management.Automation.ActionPreference]::Suspend)
        } | Should -Throw -ErrorId RuntimeException -PassThru

        $e.CategoryInfo.Reason | Should -BeExactly 'ArgumentTransformationMetadataException'
    }

    It 'A local $<ActionPreferenceVariableName> variable does not support <DisplayValue>' -TestCases (Join-TestCase -Set1 $actionPreferenceVariableTestCases -Set2 $actionPreferenceVariableValueTestCases) {
        param(
            $ActionPreferenceVariableName,
            $StreamName,
            $Value,
            $DisplayValue
        )

        $global:e = {
            Set-Variable -Name $ActionPreferenceVariableName -Value $Value
            Test-ActionPreferenceVariableSuspendValue -Value $Value
        } | Should -Throw -ErrorId "System.NotSupportedException,Microsoft.PowerShell.Commands.Write${StreamName}Command" -PassThru

        $e.CategoryInfo.Reason | Should -BeExactly 'NotSupportedException'
    }

    It 'enum disambiguation works' {
        $errorCount = $error.Count
        Get-Process -Name asdfasdfsadfsadf -ErrorAction Ig

        $error.Count | Should -BeExactly $errorCount
    }

    #issue 2076
    It 'The -<ActionPreferenceParameterName> common parameter does not support Suspend on cmdlets' -TestCases $commonActionPreferenceParameterTestCases {
        param($ActionPreferenceParameterName)

        $commonParameters = @{
            "${ActionPreferenceParameterName}" = [System.Management.Automation.ActionPreference]::Suspend
        }

        { Write-Output -InputObject Test @commonParameters } | Should -Throw -ErrorId "ParameterBindingFailed,Microsoft.PowerShell.Commands.WriteOutputCommand"
    }

    It 'The -<ActionPreferenceParameterName> common parameter does not support Suspend on functions' -TestCases $commonActionPreferenceParameterTestCases {
        param($ActionPreferenceParameterName)

        function MyHelperFunction {
            [CmdletBinding()]
            param()
            "Hello"
        }

        $commonParameters = @{
            "${ActionPreferenceParameterName}" = [System.Management.Automation.ActionPreference]::Suspend
        }

        { MyHelperFunction -ErrorAction Suspend } | Should -Throw -ErrorId "ParameterBindingFailed,MyHelperFunction"
    }

    It '<switch> does not take precedence over $ErrorActionPreference' -TestCases @(
        @{switch = "Verbose" },
        @{switch = "Debug" }
    ) {
        param($switch)
        $ErrorActionPreference = "SilentlyContinue"
        $params = @{
            ItemType = "File";
            Path     = "$testdrive\test.txt";
            Confirm  = $false
        }
        New-Item @params > $null
        $params += @{$switch = $true }
        { New-Item @params } | Should -Not -Throw
        $ErrorActionPreference = "Stop"
        { New-Item @params } | Should -Throw -ErrorId "NewItemIOError,Microsoft.PowerShell.Commands.NewItemCommand"
        Remove-Item "$testdrive\test.txt" -Force
    }
}
