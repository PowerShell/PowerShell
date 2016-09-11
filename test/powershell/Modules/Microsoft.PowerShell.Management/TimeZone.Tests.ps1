# This is a Pester test suite to validate the cmdlets in the TimeZone module
#
# Copyright (c) Microsoft Corporation, 2016

<#
    --------------------------------------
    Script Notes: opportunities for improving this test script
    --------------------------------------
    Localization
        Many of the tests below looking-up timezones by Name do not support localization.
        That is, the current tests use us english versions of StandardName and DaylightName for tests.

        ref: https://msdn.microsoft.com/en-us/library/windows/desktop/ms725481.aspx
           [snippet] Both StandardName and DaylightName are localized according to the current user default UI language.
#>

if ($IsWindows) {

function Assert-ListsSame
{
    param([object[]] $expected, [object[]] $observed )
    $compResult = Compare-Object $observed $expected | Select-Object -ExpandProperty InputObject
    if ($compResult)
    {
        $observedList = ([string]::Join("|",$observed))
        $expectedList = ([string]::Join("|",$expected))
        $observedList | Should Be $expectedList
    }
}

Describe "Get-Timezone test case no switches" -Tags "CI" {
    It "Call without ListAvailable switch returns current TimeZoneInfo" {
        $observed = (Get-TimeZone).Id
        $expected = ([System.TimeZoneInfo]::Local).Id
        $observed -eq $expected | Should Be $true
    }
}

Describe "Get-Timezone test cases" -Tags "CI" {

    It "Call without ListAvailable switch returns an object of type TimeZoneInfo" {
        $result = (Get-TimeZone).GetType().Name
        $result | Should Be "TimeZoneInfo"
    }

    It "Call WITH ListAvailable switch returns ArrayList of TimeZoneInfo objects where the list is greater than 0 item" {
        $list = Get-TimeZone -ListAvailable
        $list.Count -gt 0 | Should Be $true

        $list.GetType().Name | Should Be "Object[]"
        $list[0].GetType().Name | Should Be "TimeZoneInfo"
    }

    It "Call with ListAvailable switch returns a list containing TimeZoneInfo.Local" {
        $observedIdList = Get-TimeZone -ListAvailable | select -ExpandProperty Id
        $oneExpectedId = ([System.TimeZoneInfo]::Local).Id
        $observedIdList -contains $oneExpectedId | Should Be $true
    }

    It "Call with ListAvailable switch returns a list containing one returned by Get-TimeZone" {
        $observedIdList = Get-TimeZone -ListAvailable | select -ExpandProperty Id
        $oneExpectedId = (Get-TimeZone).Id
        $observedIdList -contains $oneExpectedId | Should Be $true
    }

    It "Call Get-TimeZone using ID param and single item" {
        (Get-TimeZone -Id "Cape Verde Standard Time").Id -eq "Cape Verde Standard Time" | Should Be $true
    }

    It "Call Get-TimeZone using ID param and multiple items" {
        $idList = @("Cape Verde Standard Time","Morocco Standard Time","Azores Standard Time")
        $result = (Get-TimeZone -Id $idList).Id
        Assert-ListsSame $result $idList
    }

    It "Call Get-TimeZone using ID param and multiple items, where first and third are invalid ids - expect error" {
        $null = Get-TimeZone -Id @("Cape Verde Standard","Morocco Standard Time","Azores Standard") `
                             -ErrorVariable errVar -ErrorAction SilentlyContinue
        $errVar.Count -eq 2 | Should Be $true
        $errVar[0].FullyQualifiedErrorID | Should Be "TimeZoneNotFound,Microsoft.PowerShell.Commands.GetTimeZoneCommand"
    }

    It "Call Get-TimeZone using ID param and multiple items, one is wild card but error action ignore works as expected" {
        $result = Get-TimeZone -Id @("Cape Verde Standard Time","Morocco Standard Time","*","Azores Standard Time") `
                               -ErrorAction SilentlyContinue | % Id
        $expectedIdList = @("Cape Verde Standard Time","Morocco Standard Time","Azores Standard Time")
        Assert-ListsSame $expectedIdList $result
    }

    It "Call Get-TimeZone using Name param and singe item" {
        $timezoneList = Get-TimeZone -ListAvailable
        $timezoneName = $timezoneList[0].StandardName
        $observed = Get-TimeZone -Name $timezoneName
        $observed.StandardName -eq $timezoneName | Should Be $true
    }

    It "Call Get-TimeZone using Name param with wild card" {
        $result = (Get-TimeZone -Name "Pacific*").Id
        $expectedIdList = @("Pacific Standard Time (Mexico)","Pacific Standard Time","Pacific SA Standard Time")
        Assert-ListsSame $expectedIdList $result
    }

    It "Verify that alias 'gtz' exists" {
        (Get-Alias -Name "gtz").Name | Should Be "gtz"
    }

    It "Call Get-TimeZone Name parameter from pipeline by value " {
        $result = ("Pacific*" | Get-TimeZone).Id
        $expectedIdList = @("Pacific Standard Time (Mexico)","Pacific Standard Time","Pacific SA Standard Time")
        Assert-ListsSame $expectedIdList $result
    }

    It "Call Get-TimeZone Id parameter from pipeline by ByPropertyName" {
        $timezoneList = Get-TimeZone -ListAvailable
        $timezone = $timezoneList[0]
        $observed = $timezone | Get-TimeZone
        $observed.StandardName -eq $timezone.StandardName | Should Be $true
    }
}

Describe "Set-Timezone test case: call by single Id" -Tags "CI" {
    $originalTimeZoneId
    BeforeAll {
        $originalTimeZoneId = (Get-TimeZone).Id
    }
    AfterAll {
        Set-TimeZone -ID $originalTimeZoneId
    }

    It "Call Set-TimeZone by Id" {
        $origTimeZoneID = (Get-TimeZone).Id
        $timezoneList = Get-TimeZone -ListAvailable
        $testTimezone = $null
        foreach($timezone in $timezoneList)
        {
            if ($timezone.Id -ne $origTimeZoneID)
            {
               $testTimezone = $timezone
               break
            }
        }
        Set-TimeZone -Id $testTimezone.Id
        $observed = Get-TimeZone
        $testTimezone.Id -eq $observed.Id | Should Be $true
    }
}

Describe "Set-Timezone test cases" -Tags "Feature" {
    $originalTimeZoneId
    BeforeAll {
        $originalTimeZoneId = (Get-TimeZone).Id
    }
    AfterAll {
        Set-TimeZone -ID $originalTimeZoneId
    }

    It "Call Set-TimeZone with invalid Id" {
        $exception = $null
        try { Set-TimeZone -Id "zzInvalidID" } catch { $exception = $_ }
        $exception.FullyQualifiedErrorID | Should Be "TimeZoneNotFound,Microsoft.PowerShell.Commands.SetTimeZoneCommand"
    }

    It "Call Set-TimeZone by Name" {
        $origTimeZoneName = (Get-TimeZone).StandardName
        $timezoneList = Get-TimeZone -ListAvailable
        $testTimezone = $null
        foreach($timezone in $timezoneList)
        {
            if ($timezone.StandardName -ne $origTimeZoneName)
            {
                $testTimezone = $timezone
                break
            }
        }
        Set-TimeZone -Name $testTimezone.StandardName
        $observed = Get-TimeZone
        $testTimezone.StandardName -eq $observed.StandardName | Should Be $true
    }

    It "Call Set-TimeZone with invalid Name" {
        $exception = $null
        try { Set-TimeZone -Name "zzINVALID_Name" } catch { $exception = $_ }
        $exception.FullyQualifiedErrorID | Should Be "TimeZoneNotFound,Microsoft.PowerShell.Commands.SetTimeZoneCommand"
    }

    It "Verify that alias 'stz' exists" {
        (Get-Alias -Name "stz").Name | Should Be "stz"
    }

    It "Call Set-TimeZone from pipeline input object of type TimeZoneInfo" {
        $origTimeZoneID = (Get-TimeZone).Id
        $timezoneList = Get-TimeZone -ListAvailable
        $testTimezone = $null
        foreach($timezone in $timezoneList)
        {
            if ($timezone.Id -ne $origTimeZoneID)
            {
                $testTimezone = $timezone
                break
            }
        }

        $testTimezone | Set-TimeZone
        $observed = Get-TimeZone
        $observed.ID -eq $testTimezone.Id | Should Be $true
    }

    It "Call Set-TimeZone from pipeline input object of type TimeZoneInfo, verify supports whatif" {
        $origTimeZoneID = (Get-TimeZone).Id
        $timezoneList = Get-TimeZone -ListAvailable
        $testTimezone = $null
        foreach($timezone in $timezoneList)
        {
            if ($timezone.Id -ne $origTimeZoneID)
            {
                $testTimezone = $timezone
                break
            }
        }

        Set-TimeZone -Id $testTimezone.Id -WhatIf > $null
        $observed = Get-TimeZone
        $observed.Id -eq $origTimeZoneID | Should Be $true
    }
}

}
