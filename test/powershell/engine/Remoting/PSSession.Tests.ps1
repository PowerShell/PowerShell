# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

#
# PSSession tests for non-Windows platforms
#

Describe "New-PSSessionOption parameters for non-Windows platforms" -Tag "CI" {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        if ($IsWindows)  {
            $PSDefaultParameterValues['it:skip'] = $true
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

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
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        if ($IsWindows)  {
            $PSDefaultParameterValues['it:skip'] = $true
        }
        else {
            $userName = "User_$(Get-Random -Maximum 99999)"
            $userPassword = "Password_$(Get-Random -Maximum 99999)"
            $cred = [pscredential]::new($userName, (ConvertTo-SecureString -String $userPassword -AsPlainText -Force))
            $soSkipCA = New-PSSessionOption -SkipCACheck
            $soSkipCN = New-PSSessionOption -SkipCNCheck
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
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

        $er = { & $scriptBlock } | Should -Throw -ErrorId 'System.Management.Automation.Remoting.PSRemotingDataStructureException,Microsoft.PowerShell.Commands.NewPSSessionCommand' -PassThru
        $er.Exception.ErrorCode | Should -Be $expectedErrorCode
    }
}
