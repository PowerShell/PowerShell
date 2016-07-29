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

Describe "Get-Timezone test case no switches" -tags 'BVT' {
    It "Call without ListAvailable switch returns current TimeZoneInfo" {
        $result = Remotely { 
            $observed = (Get-TimeZone).Id 
            $expected = ([System.TimeZoneInfo]::Local).Id
            return $observed -eq $expected
        }
        $result | Should Be $true
    } 
}

Describe "Get-Timezone test cases" -tags 'Innerloop','RI' {

    It "Call without ListAvailable switch returns an object of type TimeZoneInfo" {
        $result = Remotely { (Get-TimeZone).GetType().Name }
        $result | Should Be "TimeZoneInfo"
    } 

    It "Call WITH ListAvailable switch returns ArrayList of TimeZoneInfo objects where the list is greater than 0 item" {
        $result = Remotely { 
            $list = Get-TimeZone -ListAvailable
            $observedListType = $list.GetType().Name 
            $observedItemType = $list[0].GetType().Name 
            return @($observedListType,$observedItemType,$list.Count > 0)
        }
        $result | Should Be @("Object[]","TimeZoneInfo",$true)
    } 

    It "Call with ListAvailable switch returns a list containing TimeZoneInfo.Local" {
        $result = Remotely { 
            $observedIdList = Get-TimeZone -ListAvailable | select -ExpandProperty Id
            $oneExpectedId = ([System.TimeZoneInfo]::Local).Id
            $observedIdList  -contains $oneExpectedId
        }
        $result | Should Be $true
    } 

    It "Call with ListAvailable switch returns a list containing one returned by Get-TimeZone" {
        $result = Remotely { 
            $observedIdList = Get-TimeZone -ListAvailable | select -ExpandProperty Id
            $oneExpectedId = (Get-TimeZone).Id
            $observedIdList -contains $oneExpectedId
        }
        $result | Should Be $true
    }
    
    It "Call Get-TimeZone using ID param and single item" {
        $result = Remotely { (Get-TimeZone -Id "Cape Verde Standard Time").Id -eq "Cape Verde Standard Time" }
        $result | Should Be $true
    }  
    
    It "Call Get-TimeZone using ID param and multiple items" {
        $idList = @("Cape Verde Standard Time","Morocco Standard Time","Azores Standard Time")
        $result = Remotely { 
            param([string[]] $idList)
            (Get-TimeZone -Id $idList).Id
        } -ArgumentList (,$idList)
        Assert-ListsSame $result $idList 
    } 
    
    It "Call Get-TimeZone using ID param and multiple items, where first and third are invalid ids - expect error" {
        $result = Remotely { Get-TimeZone -Id @("Cape Verde Standard","Morocco Standard Time","Azores Standard") }
        $result.GetError().FullyQualifiedErrorID | Should Be "TimeZoneNotFound,Microsoft.PowerShell.Commands.GetTimeZoneCommand"
    }  
    
    It "Call Get-TimeZone using ID param and multiple items, one is wild card but error action ignore works as expected" {
        $result = Remotely { (Get-TimeZone -Id @("Cape Verde Standard Time","Morocco Standard Time","*","Azores Standard Time") -ErrorAction SilentlyContinue).Id }
        $expectedIdList = @("Cape Verde Standard Time","Morocco Standard Time","Azores Standard Time")
        Assert-ListsSame $expectedIdList $result
    } 
    
    It "Call Get-TimeZone using Name param and singe item" {
        $result = Remotely { 
            $timezoneList = Get-TimeZone -ListAvailable
            $timezoneName = $timezoneList[0].StandardName
            $observed = Get-TimeZone -Name $timezoneName
            $observed.StandardName -eq $timezoneName
        }
        $result | Should Be $true
    } 

    It "Call Get-TimeZone using Name param with wild card" {
        $result = Remotely { (Get-TimeZone -Name "Pacific*").Id }
        $expectedIdList = @("Pacific Standard Time (Mexico)","Pacific Standard Time","Pacific SA Standard Time")
        Assert-ListsSame $expectedIdList $result 
    }  
    
    It "Verify that alias 'gtz' exists" {
        $result = Remotely { (Get-Alias -Name "gtz").Name }
        $result | Should Be "gtz"
    }       

    It "Call Get-TimeZone Name parameter from pipeline by value " {
        $result = Remotely { ("Pacific*" | Get-TimeZone).Id }
        $expectedIdList = @("Pacific Standard Time (Mexico)","Pacific Standard Time","Pacific SA Standard Time")
        Assert-ListsSame $expectedIdList $result 
    }                     

    It "Call Get-TimeZone Id parameter from pipeline by ByPropertyName" {
        $result = Remotely { 
            $timezoneList = Get-TimeZone -ListAvailable
            $timezone = $timezoneList[0]
            $observed = $timezone | Get-TimeZone
            $observed.StandardName -eq $timezone.StandardName
        }
        $result | Should Be $true
    }
}

Describe "Set-Timezone test case: call by single Id" -tags 'BVT' {
    $originalTimeZoneId
    BeforeAll {
        $originalTimeZoneId = Remotely { (Get-TimeZone).Id }
    }
    AfterAll {
        Remotely { param($tz) Set-TimeZone -ID $tz } -ArgumentList $originalTimeZoneId
    }

    It "Call Set-TimeZone by Id" {
        $result = Remotely { 
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
            $testTimezone.Id -eq $observed.Id
        }
        $result| Should Be $true
    }
}

Describe "Set-Timezone test cases" -tags 'Innerloop','RI' {
    $originalTimeZoneId
    BeforeAll {
        $originalTimeZoneId = Remotely { (Get-TimeZone).Id }
    }
    AfterAll {
        Remotely { param($tz) Set-TimeZone -ID $tz } -ArgumentList $originalTimeZoneId
    }



    It "Call Set-TimeZone with invalid Id" {
        $result = Remotely {  Set-TimeZone -Id "zzInvalidID" }
        $result.GetError().FullyQualifiedErrorID | Should Be "TimeZoneNotFound,Microsoft.PowerShell.Commands.SetTimeZoneCommand"
    }

    It "Call Set-TimeZone by Name" {
        $result = Remotely { 
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
            $testTimezone.StandardName -eq $observed.StandardName
        }
        $result | Should Be $true
    }

        It "Call Set-TimeZone with invalid Name" {
        $result = Remotely { Set-TimeZone -Name "zzINVALID_Name" }
        $result.GetError().FullyQualifiedErrorID | Should Be "TimeZoneNotFound,Microsoft.PowerShell.Commands.SetTimeZoneCommand"
    }

    It "Verify that alias 'stz' exists" {
        $result = Remotely { (Get-Alias -Name "stz").Name }
        $result | Should Be "stz"
    }

    It "Call Set-TimeZone from pipeline input object of type TimeZoneInfo" {
        $result = Remotely { 
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
            $observed.ID -eq $testTimezone.Id 
        }
        $result | Should Be $true
    }

    It "Call Set-TimeZone from pipeline input object of type TimeZoneInfo, verify supports whatif" {
        $result = Remotely { 
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
            $observed.Id -eq $origTimeZoneID 
        }
        $result.GetError() | Should BeNullOrEmpty
        $result | Should Be $true
    }
}
