# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "New-Service cmdlet tests" -Tags "CI" {
    # Service cmdlet is currently working on windows only
    # So skip the tests on non-Windows
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( -not $IsWindows ) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }
    # Restore the defaults
    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    $testCases =
    @{ data = $null          ; binaryPathName = 'TestDrive:\test.ext' },
    @{ data = [String]::Empty; binaryPathName = 'TestDrive:\test.ext' }

    Context 'Check null or empty value to the -Name parameter' {
        It 'Should throw if <value> is not passed to -Name parameter' -TestCases $testCases {
            param($data, $binaryPathName)
            { $null = New-Service -Name $data -BinaryPathName $binaryPathName -ErrorAction Stop } |
            Should -Throw -ErrorId 'ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.NewServiceCommand'
        }
    }

    $testCases =
    @{ data = 'TestService' ; binaryPathName = [string]::Empty },
    @{ data = 'TestService' ; binaryPathName = $null }

    Context 'Check null or empty value to the -BinaryPathName parameter' {
        It 'Should throw if <value> is not passed to -BinaryPathName parameter' -TestCases $testCases {
            param($data, $binaryPathName)
            { $null = New-Service -Name $data -BinaryPathName $binaryPathName -ErrorAction Stop } |
            Should -Throw -ErrorId 'ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.NewServiceCommand'
        }
    }

    Context 'Creates New service with given properties using New-Service' {
        BeforeAll {
            $TestServiceName1 = 'TestServiceName1'
            $svccmd = Get-Command $svcbinaryname
            $svccmd | Should -Not -BeNullOrEmpty
            $BinaryPathName = $svccmd.Path
            $ServiceSetting = @{
                Name = $TestServiceName1
                BinaryPathName = $BinaryPathName
                StartupType = 'Manual'
                Desciption  = 'Test Service'
            }
        }

        It 'Creates New service with given properties' {
            $Service = {New-Service @ServiceSetting -ErrorAction Stop} | Should -Not -Throw -Passthru
            $Service | Should -Not -BeNullOrEmpty
            $Service.Name | Should -Be $BinaryPathName
            $Service.BinaryPathName | Should -Be $TestServiceName1
            $Service.StartupType | Should -Be $StartupType
            $Service.Desciption | Should -Be $Desciption
        }

        it 'Throws exception if wrong StartType is provided' {
            $ServiceSetting.StartType = 'bar'
            $ExpectedErrorID = 'CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.NewServiceCommand'
            {New-Service @ServiceSetting -ErrorAction Stop} | Should -Not -Throw -ErrorId $ExpectedErrorID
        }
    }
}
