# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'ProxyCommand Tests' -Tag 'CI' {
    BeforeAll {
        $testCases = @(
            @{ Name = 'ValidateLengthAttribute';                         ParamBlock = '[ValidateLength(1, 10)][int]${Parameter}' }
            @{ Name = 'ValidateRangeAttribute with Minimum and Maximum'; ParamBlock = '[ValidateRange(1, 10)][int]${Parameter}' }
            @{ Name = 'ValidateRangeAttribute with RangeKind';           ParamBlock = '[ValidateRange([System.Management.Automation.ValidateRangeKind]::Positive)][int]${Parameter}' }
            @{ Name = 'AllowNullAttribute';                              ParamBlock = '[AllowNull()][int]${Parameter}' }
            @{ Name = 'AllowEmptyStringAttribute';                       ParamBlock = '[AllowEmptyString()][int]${Parameter}' }
            @{ Name = 'AllowEmptyCollectionAttribute';                   ParamBlock = '[AllowEmptyCollection()][int]${Parameter}' }
            @{ Name = 'ValidatePatternAttribute';                        ParamBlock = '[ValidatePattern(''.*'')][int]${Parameter}' }
            @{ Name = 'ValidateCountAttribute';                          ParamBlock = '[ValidateCount(1, 10)][int]${Parameter}' }
            @{ Name = 'ValidateNotNullAttribute';                        ParamBlock = '[ValidateNotNull()][int]${Parameter}' }
            @{ Name = 'ValidateNotNullOrEmptyAttribute';                 ParamBlock = '[ValidateNotNullOrEmpty()][int]${Parameter}' }
            @{ Name = 'ValidateSetAttribute with explicit set';          ParamBlock = '[ValidateSet(''1'',''10'')][int]${Parameter}' }
            @{ Name = 'PSTypeNameAttribute';                             ParamBlock = '[PSTypeName(''TypeName'')][int]${Parameter}' }
        )

        $validateRangeEnumTestCases = @(
            @{
                Name       = 'Enum min and Enum max';
                ParamBlock = '[ValidateRange([Microsoft.PowerShell.ExecutionPolicy]::Unrestricted, [Microsoft.PowerShell.ExecutionPolicy]::Undefined)][Microsoft.PowerShell.ExecutionPolicy]${Parameter}'
                Expected   = '[ValidateRange([Microsoft.PowerShell.ExecutionPolicy]::Unrestricted, [Microsoft.PowerShell.ExecutionPolicy]::Undefined)][Microsoft.PowerShell.ExecutionPolicy]${Parameter}'
            },
            @{
                Name       = 'Enum min and int max';
                ParamBlock = '[ValidateRange([Microsoft.PowerShell.ExecutionPolicy]::Unrestricted, 5)][Microsoft.PowerShell.ExecutionPolicy]${Parameter}'
                Expected   = '[ValidateRange(0, 5)][Microsoft.PowerShell.ExecutionPolicy]${Parameter}'
            }
            @{
                Name       = 'int min and Enum max';
                ParamBlock = '[ValidateRange(0, [Microsoft.PowerShell.ExecutionPolicy]::Undefined)][Microsoft.PowerShell.ExecutionPolicy]${Parameter}'
                Expected   = '[ValidateRange(0, 5)][Microsoft.PowerShell.ExecutionPolicy]${Parameter}'
            }
        )
    }

    Context 'GetParamBlock method' {
        AfterAll {
            Remove-Item function:testProxyCommandFunction -ErrorAction SilentlyContinue
        }

        It 'Generates a param block when <Name> is used' -TestCases $testCases {
            param (
                $Name,
                $ParamBlock
            )

            $functionDefinition = 'param ( {0} )' -f $ParamBlock
            Set-Item -Path function:testProxyCommandFunction -Value $functionDefinition

            $generatedParamBlock = [System.Management.Automation.ProxyCommand]::GetParamBlock(
                (Get-Command testProxyCommandFunction)
            )
            $generatedParamBlock = $generatedParamBlock -split '\r?\n' -replace '^ *' -join ''

            $generatedParamBlock | Should -Be $ParamBlock
        }

        It 'Generates a param block when ValidateRangeAttribute is used with <Name>' -TestCases $validateRangeEnumTestCases {
            param (
                $Name,
                $ParamBlock,
                $Expected
            )

            $functionDefinition = 'param ( {0} )' -f $ParamBlock
            Set-Item -Path function:testProxyCommandFunction -Value $functionDefinition

            $generatedParamBlock = [System.Management.Automation.ProxyCommand]::GetParamBlock(
                (Get-Command testProxyCommandFunction)
            )
            $generatedParamBlock = $generatedParamBlock -split '\r?\n' -replace '^ *' -join ''

            $generatedParamBlock | Should -Be $Expected
        }

        It 'Generates a param block when ValidateScriptAttribute is used' {
            param (
                $Name,
                $ParamBlock
            )

            $functionDefinition = 'param ( [ValidateScript({ $true })][int]${Parameter} )'
            Set-Item -Path function:testProxyCommandFunction -Value $functionDefinition
            $generatedParamBlock = [System.Management.Automation.ProxyCommand]::GetParamBlock(
                (Get-Command testProxyCommandFunction)
            )
            $generatedParamBlock = $generatedParamBlock -split '\r?\n' -replace '^ *' -join ''

            $generatedParamBlock | Should -Be '[ValidateScript({  $true  })][int]${Parameter}'
        }
    }
}
