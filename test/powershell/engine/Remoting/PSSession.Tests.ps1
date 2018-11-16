# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

#
# PSSession tests for non-Windows platforms
#
try 
{
    if ( $IsWindows ) {
        $PSDefaultParameterValues['it:skip'] = $true
    }

    Describe "New-PSSessionOption parameters for non-Windows platforms" -Tag "CI" {

        It "Verifies New-PSSessionOption parameters" {

            $cmdInfo = Get-Command New-PSSessionOption

            $commonParameterCount = [System.Management.Automation.Internal.CommonParameters].GetProperties().Length
            $cmdInfo.Parameters.Count | Should -Be ($commonParameterCount + 2) -Because "Only -SkipCACheck and -SkipCNCheck switch parameters are available"

            { $null = $cmdInfo.ResolveParameter("SkipCACheck") } | Should -Not -Throw -Because "SkipCACheck parameter should be available"
            { $null = $cmdInfo.ResolveParameter("SkipCNCheck") } | Should -Not -Throw -Because "SkipCNCheck parameter should be available"
        }
    }

    Describe "SkipCACheck and SkipCNCheck PSSession options are required for New-PSSession on non-Windows platforms" -Tag "CI" {

        BeforeAll {
            $cred = [pscredential]::new("BogusUser", (ConvertTo-SecureString -String "BogusPassword" -AsPlainText -Force))
            $soSkipCA = New-PSSessionOption -SkipCACheck
            $soSkipCN = New-PSSessionOption -SkipCNCheck
        }

        $testCases = @(
            @{
                Name = 'Verifies expected error when session options is missing'
                ScriptBlock = { New-PSSession -cn localhost -Credential $cred -Authentication Basic -UseSSL }
                ExpectedErrorCode = 825
            },
            @{
                Name = 'Verifies expected error when SkipCACheck option is missing'
                ScriptBlock = { New-PSSession -cn localhost -Credential $cred -Authentication Basic -UseSSl -SessionOption $soSkipCN }
                ExpectedErrorCode = 825
            },
            @{
                Name = 'Verifies expected error when SkipCNCheck option is missing'
                ScriptBlock = { New-PSSession -cn localhost -Credential $cred -Authentication Basic -UseSSl -SessionOption $soSkipCA }
                ExpectedErrorCode = 825
            }
        )

        It "<Name>" -TestCases $testCases {
            param ($scriptBlock, $expectedErrorCode)

            try {
                & $scriptBlock
                throw "No Exception!"
            }
            catch {
                $_.Exception.ErrorCode | Should Be $expectedErrorCode
            }
        }
    }
}
finally
{
    $PSDefaultParameterValues.remove("it:skip")
}
