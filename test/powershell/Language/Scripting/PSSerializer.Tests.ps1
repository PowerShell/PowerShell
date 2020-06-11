# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for lossless rehydration of serialized types.' -Tags 'CI' {
    BeforeAll {
        $cmdBp = Set-PSBreakpoint -Command Get-Process
        $varBp = Set-PSBreakpoint -Variable ?
        $lineBp = Set-PSBreakpoint -Script $PSScriptRoot/PSSerializer.Tests.ps1 -Line 1

        function ShouldRehydrateLosslessly {
            [CmdletBinding()]
            param(
                [Parameter(Mandatory, ValueFromPipeline)]
                [ValidateNotNull()]
                [System.Management.Automation.Breakpoint]
                $Breakpoint
            )
            $dehydratedBp = [System.Management.Automation.PSSerializer]::Serialize($Breakpoint)
            $rehydratedBp = [System.Management.Automation.PSSerializer]::Deserialize($dehydratedBp)
            foreach ($property in $Breakpoint.PSObject.Properties) {
                $bpValue = $Breakpoint.$($property.Name)
                $rehydratedBpValue = $rehydratedBp.$($property.Name)
                $propertyType = $property.TypeNameOfValue -as [System.Type]
                if ($null -eq $bpValue) {
                    $rehydratedBpValue | Should -Be $null
                } elseif ($propertyType.IsValueType) {
                    $bpValue | Should -Be $rehydratedBpValue
                } elseif ($propertyType -eq [string]) {
                    $bpValue | Should -BeExactly $rehydratedBpValue
                } else {
                    $bpValue.ToString() | Should -BeExactly $rehydratedBpValue.ToString()
                }
            }
        }
    }

    AfterAll {
        Remove-PSBreakpoint -Breakpoint $cmdBp,$varBp,$lineBp
    }

    It 'Losslessly rehydrates command breakpoints' {
        $cmdBp | ShouldRehydrateLosslessly
    }

    It 'Losslessly rehydrates variable breakpoints' {
        $varBp | ShouldRehydrateLosslessly
    }

    It 'Losslessly rehydrates line breakpoints' {
        $lineBp | ShouldRehydrateLosslessly
    }
}
