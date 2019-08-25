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
            $SecurityDescriptorSddl = 'D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;SU)'
            $WrongSecurityDescriptorSddl = 'D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BB)(A;;CCLCSWLOCRRC;;;SU)'
            $TestServiceName1 = 'TestServiceName1'
            $svccmd = Get-Command $svcbinaryname
            $svccmd | Should -Not -BeNullOrEmpty
            $BinaryPathName = $svccmd.Path
            $ServiceSetting = @{
                Name = $TestServiceName1
                BinaryPathName = $BinaryPathName
                SecurityDescriptorSddl  =$SecurityDescriptorSddl
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

        it 'Throws exception if wrong SecurityDescriptor is provided' {
            $ServiceSetting.SecurityDescriptorSddl = $WrongSecurityDescriptorSddl
            {New-Service @ServiceSetting -ErrorAction Stop} | Should -Not -Throw -ErrorId 'System.ArgumentException,Microsoft.PowerShell.Commands.NewServiceCommand'
        }

        it 'Throws exception if wrong StartType is provided' {
            $ServiceSetting.StartType = 'bar'
            $ExpectedErrorID = 'CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.NewServiceCommand'
            $ExpectedMessage = "Cannot bind parameter 'StartupType'. Cannot convert value "bar" to type `"Microsoft.PowerShell.Commands.ServiceStartupType`". Error: `"Unable to match the identifier name bar to a valid enumerator name. Specify one of the following enumerator names and try again:
            Automatic, Manual, Disabled, AutomaticDelayedStart, InvalidValue`""
            {New-Service @ServiceSetting -ErrorAction Stop} | Should -Not -Throw -ErrorId $ExpectedErrorID -ExpectedMessage $ExpectedMessage
        }
    }
}
