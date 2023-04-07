# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
    --------------------------------------
    Script Notes: opportunities for improving this test script
    --------------------------------------
    Localization
        Many of the tests below looking-up timezones by Name do not support localization.
        That is, the current tests use us english versions of StandardName and DaylightName for tests.

        ref: https://msdn.microsoft.com/library/windows/desktop/ms725481.aspx
           [snippet] Both StandardName and DaylightName are localized according to the current user default UI language.
#>

function Assert-ListsSame
{
    param([object[]] $expected, [object[]] $observed )
    $compResult = Compare-Object $observed $expected | Select-Object -ExpandProperty InputObject
    if ($compResult)
    {
        $observedList = ([string]::Join("|",$observed))
        $expectedList = ([string]::Join("|",$expected))
        $observedList | Should -Be $expectedList
    }
}

Describe "Get-Timezone test cases" -Tags "CI" {

    BeforeAll {
        $TimeZonesAvailable = [System.TimeZoneInfo]::GetSystemTimeZones()

        $defaultParamValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues["it:skip"] = ($TimeZonesAvailable.Count -eq 0)
    }

    AfterAll {
        $global:PSDefaultParameterValues = $defaultParamValues
    }

    It "Call without ListAvailable switch returns current TimeZoneInfo" {
        $observed = (Get-TimeZone).Id
        $expected = ([System.TimeZoneInfo]::Local).Id
        $observed | Should -Be $expected
    }

    It "Call without ListAvailable switch returns an object of type TimeZoneInfo" {
        $result = Get-TimeZone
        $result | Should -BeOfType TimeZoneInfo
    }

    It "Call WITH ListAvailable switch returns ArrayList of TimeZoneInfo objects where the list is greater than 0 item" {
        $list = Get-TimeZone -ListAvailable
        $list.Count | Should -BeGreaterThan 0

        ,$list | Should -BeOfType Object[]
        $list[0] | Should -BeOfType TimeZoneInfo
    }

    ## The local time zone could be set to UTC or GMT*. In this case, the .NET API returns the region ID
    ## and not 'UTC'. To avoid a string matching error, we compare the BaseUtcOffset instead.
    It "Call with ListAvailable switch returns a list containing TimeZoneInfo.Local" {
        $observedIdList = Get-TimeZone -ListAvailable | Select-Object -ExpandProperty BaseUtcOffset
        $oneExpectedOffset = ([System.TimeZoneInfo]::Local).BaseUtcOffset
        $oneExpectedOffset | Should -BeIn $observedIdList
    }

    ## The local time zone could be set to UTC or GMT*. In this case, the .NET API returns the region ID
    ## and not UTC. To avoid a string matching error, we compare the BaseUtcOffset instead.
    It "Call with ListAvailable switch returns a list containing one returned by Get-TimeZone" {
        $observedIdList = Get-TimeZone -ListAvailable | Select-Object -ExpandProperty BaseUtcOffset
        $oneExpectedOffset = (Get-TimeZone).BaseUtcOffset
        $oneExpectedOffset | Should -BeIn $observedIdList
    }

    It "Call Get-TimeZone using ID param and single item" {
        $selectedTZ = $TimeZonesAvailable[0]
        (Get-TimeZone -Id $selectedTZ.Id).Id | Should -Be $selectedTZ.Id
    }

    It "Call Get-TimeZone using ID param and multiple items" {
        $selectedTZ = $TimeZonesAvailable | Select-Object -First 3 -ExpandProperty Id
        $result = (Get-TimeZone -Id $selectedTZ).Id
        Assert-ListsSame $result $selectedTZ
    }

    It "Call Get-TimeZone using ID param and multiple items, where first and third are invalid ids - expect error" {
        $selectedTZ = $TimeZonesAvailable[0].Id
        $null = Get-TimeZone -Id @("Cape Verde Standard",$selectedTZ,"Azores Standard") `
                             -ErrorVariable errVar -ErrorAction SilentlyContinue
        $errVar.Count | Should -Be 2
        $errVar[0].FullyQualifiedErrorID | Should -Be "TimeZoneNotFound,Microsoft.PowerShell.Commands.GetTimeZoneCommand"
    }

    It "Call Get-TimeZone using ID param and multiple items, one is wild card but error action ignore works as expected" {
        $selectedTZ = $TimeZonesAvailable | Select-Object -First 3 -ExpandProperty Id
        $inputArray = $selectedTZ + "*"
        $result = Get-TimeZone -Id $inputArray -ErrorAction SilentlyContinue | ForEach-Object Id
        Assert-ListsSame $selectedTZ $result
    }

    It "Call Get-TimeZone using Name param and singe item" {
        $timezoneList = Get-TimeZone -ListAvailable
        $timezoneName = $timezoneList[0].StandardName
        $observed = Get-TimeZone -Name $timezoneName
        $observed.StandardName | Should -Be $timezoneName
    }

    It "Call Get-TimeZone using Name param with wild card" {
        $result = (Get-TimeZone -Name "Pacific*").Id
        $expectedIdList = ($TimeZonesAvailable | Where-Object { $_.StandardName -match "^Pacific" }).Id
        Assert-ListsSame $expectedIdList $result
    }

    It "Call Get-TimeZone Name parameter from pipeline by value " {
        $result = ("Pacific*" | Get-TimeZone).Id
        $expectedIdList = ($TimeZonesAvailable | Where-Object { $_.StandardName -match "^Pacific" }).Id
        Assert-ListsSame $expectedIdList $result
    }

    It "Call Get-TimeZone Id parameter from pipeline by ByPropertyName" {
        $timezoneList = Get-TimeZone -ListAvailable
        $timezone = $timezoneList[0]
        $observed = $timezone | Get-TimeZone
        $observed.StandardName | Should -Be $timezone.StandardName
    }
}

try {
    Describe "Set-Timezone test case: call by single Id" -Tags @('CI', 'RequireAdminOnWindows') {
        BeforeAll {
            if (Test-IsWinServer2012R2) {
                $defaultParamValues = $global:PSdefaultParameterValues.Clone()

                # Set-TimeZone fails due to missing ApiSet dependency on Windows Server 2012 R2.
                $global:PSDefaultParameterValues["it:skip"] = $true
            }
            elseif ($IsWindows) {
                $originalTimeZoneId = (Get-TimeZone).Id
            }
        }
        AfterAll {
            if (Test-IsWinServer2012R2) {
                $global:PSDefaultParameterValues = $defaultParamValues
            }
            if ($IsWindows) {
                Set-TimeZone -Id $originalTimeZoneId
            }
        }

        It "Call Set-TimeZone by Id" {
            $origTimeZoneID = (Get-TimeZone).Id
            $timezoneList = Get-TimeZone -ListAvailable
            $testTimezone = $null
            foreach ($timezone in $timezoneList) {
                if ($timezone.Id -ne $origTimeZoneID) {
                    $testTimezone = $timezone
                    break
                }
            }
            Set-TimeZone -Id $testTimezone.Id
            $observed = Get-TimeZone
            $testTimezone.Id | Should -Be $observed.Id
        }
    }

    Describe "Set-Timezone test cases" -Tags @('Feature', 'RequireAdminOnWindows') {
        BeforeAll {
            if (Test-IsWinServer2012R2) {
                $defaultParamValues = $global:PSdefaultParameterValues.Clone()

                # Set-TimeZone fails due to missing ApiSet dependency on Windows Server 2012 R2.
                $global:PSDefaultParameterValues["it:skip"] = $true
            }
            elseif ($IsWindows) {
                $originalTimeZoneId = (Get-TimeZone).Id
            }
        }
        AfterAll {
            if (Test-IsWinServer2012R2) {
                $global:PSDefaultParameterValues = $defaultParamValues
            }
            if ($IsWindows) {
                Set-TimeZone -Id $originalTimeZoneId
            }
        }

        It "Call Set-TimeZone with invalid Id" {
            { Set-TimeZone -Id "zzInvalidID" } | Should -Throw -ErrorId "TimeZoneNotFound,Microsoft.PowerShell.Commands.SetTimeZoneCommand"
        }

        It "Call Set-TimeZone by Name" {
            $origTimeZoneName = (Get-TimeZone).StandardName
            $timezoneList = Get-TimeZone -ListAvailable
            $testTimezone = $null
            foreach ($timezone in $timezoneList) {
                if ($timezone.StandardName -ne $origTimeZoneName) {
                    $testTimezone = $timezone
                    break
                }
            }
            Set-TimeZone -Name $testTimezone.StandardName
            $observed = Get-TimeZone
            $testTimezone.StandardName | Should -Be $observed.StandardName
        }

        It "Call Set-TimeZone with invalid Name" {
            { Set-TimeZone -Name "zzINVALID_Name" } | Should -Throw -ErrorId "TimeZoneNotFound,Microsoft.PowerShell.Commands.SetTimeZoneCommand"
        }

        It "Call Set-TimeZone from pipeline input object of type TimeZoneInfo" {
            $origTimeZoneID = (Get-TimeZone).Id
            $timezoneList = Get-TimeZone -ListAvailable
            $testTimezone = $null
            foreach ($timezone in $timezoneList) {
                if ($timezone.Id -ne $origTimeZoneID) {
                    $testTimezone = $timezone
                    break
                }
            }

            $testTimezone | Set-TimeZone
            $observed = Get-TimeZone
            $observed.ID | Should -Be $testTimezone.Id
        }

        It "Call Set-TimeZone from pipeline input object of type TimeZoneInfo, verify supports whatif" {
            $origTimeZoneID = (Get-TimeZone).Id
            $timezoneList = Get-TimeZone -ListAvailable
            $testTimezone = $null
            foreach ($timezone in $timezoneList) {
                if ($timezone.Id -ne $origTimeZoneID) {
                    $testTimezone = $timezone
                    break
                }
            }

            Set-TimeZone -Id $testTimezone.Id -WhatIf > $null
            $observed = Get-TimeZone
            $observed.Id | Should -Be $origTimeZoneID
        }
    }
}
finally {
    $global:PSDefaultParameterValues = $defaultParamValues
}
