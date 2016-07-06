# This is a Pester test suite to validate the cmdlets in the TimeZone module
#
# Copyright (c) Microsoft Corporation, 2016

<#
    --------------------------------------
    Script Notes: opportunities for improving this test script
    --------------------------------------
    Localization
        Many of the tests below looking-up timezones by Name do not support localization.
        That is, the current tests use us english versions of StandardName and DaylighName for tests.
        
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

Describe "Set-Timezone test case: call by single Id" -tags 'BVT' {
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

Describe "Set-Timezone test cases" -tags 'Innerloop','RI' {
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
