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
    @{ data = $null         ; binaryPathName = 'TestDrive:\test.ext' },
    @{ data = 'TestService' ; binaryPathName = $null }

    Context 'Check null value to the parameters' {
        It 'Should throw if <value> is not passed to parameters' -TestCases $testCases {
            param($data, $binaryPathName)
            { $null = New-Service -Name $data -BinaryPathName $binaryPathName -ErrorAction Stop } |
            Should -Throw -ErrorId 'ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.NewServiceCommand'
        }
    }

    $testCases =
    @{ data = [string]::Empty ; binaryPathName = 'TestDrive:\test.ext' },
    @{ data = 'TestService'   ; binaryPathName = [string]::Empty }

    Context 'Check empty value to the parameters' {
        It 'Should throw if <value> is not passed to parameters' -TestCases $testCases {
            param($data, $binaryPathName)
            { $null = New-Service -Name $data -BinaryPathName $binaryPathName -ErrorAction Stop } |
            Should -Throw -ErrorId 'ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.NewServiceCommand'
        }
    }

    Context 'Creates New service with given properties using New-Service' {
        BeforeEach {
            $TestServiceName1 = 'TestService'
            $svccmd = Get-Command $TestServiceName1
            $svccmd | Should -Not -BeNullOrEmpty
            $BinaryPathName = $svccmd.Path
            $SecurityDescriptorSddl = 'D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;SU)'
            $WrongSecurityDescriptorSddl = 'D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BB)(A;;CCLCSWLOCRRC;;;SU)'
            $ServiceSetting = @{
                Name = $TestServiceName1
                BinaryPathName = $BinaryPathName
                SecurityDescriptorSddl  =$SecurityDescriptorSddl
                StartupType = 'Manual'
                Description  = 'Test Service'
            }
        }

        AfterEach {
            if(Get-Service -Name $TestServiceName1 -ErrorAction SilentlyContinue){
                Remove-Service -Name $TestServiceName1
            }
        }

        It 'Creates New service with given properties' {
            {$Script:Service = New-Service @ServiceSetting -ErrorAction Stop} | Should -Not -Throw
            $Script:Service | Should -Not -BeNullOrEmpty
            $Script:Service.ServiceName | Should -Be $TestServiceName1
            $Script:Service.StartupType | Should -Be $StartupType
            $Script:Service.Desciption | Should -Be $Desciption

            $Counter = 0
            $ExpectedSDDL = ConvertFrom-SddlString -Sddl $SecurityDescriptorSddl

            # Selecting the first item in the output array as below command gives plain text output from the native sc.exe.
            $ServiceSDDL = ConvertFrom-SddlString -Sddl (sc sdshow $TestServiceName1)[1]

            $ServiceSDDL.Owner | Should -Be $ExpectedSDDL.Owner
            $ServiceSDDL.Group | Should -Be $ExpectedSDDL.Group
            $ServiceSDDL.DiscretionaryAcl.Count | Should -Be $ExpectedSDDL.DiscretionaryAcl.Count
            $ServiceSDDL.DiscretionaryAcl | ForEach-Object -Process {
                $_ | Should -Be $ExpectedSDDL.DiscretionaryAcl[$Counter]
                $Counter++
            }
        }

        it 'Throws exception if wrong StartType is provided' {
            $ServiceSetting.StartupType = 'bar'
            $ExpectedErrorID = 'CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.NewServiceCommand'
            {New-Service @ServiceSetting -ErrorAction Stop} | Should -Throw -ErrorId $ExpectedErrorID
        }

        it 'Throws exception if wrong SecurityDescriptor is provided' {
            $ServiceSetting.SecurityDescriptorSddl = $WrongSecurityDescriptorSddl
            {New-Service @ServiceSetting -ErrorAction Stop} | Should -Throw -ErrorId 'System.ArgumentException,Microsoft.PowerShell.Commands.NewServiceCommand'
        }
    }
}
