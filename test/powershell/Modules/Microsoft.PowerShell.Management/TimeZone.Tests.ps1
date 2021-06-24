# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
    --------------------------------------
    Script Notes: opportunities for improving this test script
    --------------------------------------
    Localization
        Many of the tests below looking-up timezones by Name do not support localization.
<<<<<<< HEAD
        That is, the current tests use us english versions of StandardName and DaylightName for tests.

        ref: https://msdn.microsoft.com/library/windows/desktop/ms725481.aspx
=======
        That is, the current tests use us english versions of StandardName and DaylighName for tests.
        
        ref: https://msdn.microsoft.com/en-us/library/windows/desktop/ms725481.aspx
>>>>>>> origin/source-depot
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

<<<<<<< HEAD
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
=======
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
>>>>>>> origin/source-depot

    ## The local time zone could be set to UTC or GMT*. In this case, the .NET API returns the region ID
    ## and not 'UTC'. To avoid a string matching error, we compare the BaseUtcOffset instead.
    It "Call with ListAvailable switch returns a list containing TimeZoneInfo.Local" {
<<<<<<< HEAD
        $observedIdList = Get-TimeZone -ListAvailable | Select-Object -ExpandProperty BaseUtcOffset
        $oneExpectedOffset = ([System.TimeZoneInfo]::Local).BaseUtcOffset
        $oneExpectedOffset | Should -BeIn $observedIdList
    }
=======
        $result = Remotely { 
            $observedIdList = Get-TimeZone -ListAvailable | select -ExpandProperty Id
            $oneExpectedId = ([System.TimeZoneInfo]::Local).Id
            $observedIdList  -contains $oneExpectedId
        }
        $result | Should Be $true
    } 
>>>>>>> origin/source-depot

    ## The local time zone could be set to UTC or GMT*. In this case, the .NET API returns the region ID
    ## and not UTC. To avoid a string matching error, we compare the BaseUtcOffset instead.
    It "Call with ListAvailable switch returns a list containing one returned by Get-TimeZone" {
<<<<<<< HEAD
        $observedIdList = Get-TimeZone -ListAvailable | Select-Object -ExpandProperty BaseUtcOffset
        $oneExpectedOffset = (Get-TimeZone).BaseUtcOffset
        $oneExpectedOffset | Should -BeIn $observedIdList
=======
        $result = Remotely { 
            $observedIdList = Get-TimeZone -ListAvailable | select -ExpandProperty Id
            $oneExpectedId = (Get-TimeZone).Id
            $observedIdList -contains $oneExpectedId
        }
        $result | Should Be $true
>>>>>>> origin/source-depot
    }
    
    It "Call Get-TimeZone using ID param and single item" {
<<<<<<< HEAD
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
    $defaultParamValues = $PSdefaultParameterValues.Clone()

    # Set-TimeZone fails due to missing ApiSet dependency on Windows Server 2012 R2.
    $osInfo = [System.Environment]::OSVersion.Version
    $isSrv2k12R2 = $osInfo.Major -eq 6 -and $osInfo.Minor -eq 3

    $PSDefaultParameterValues["it:skip"] = !$IsWindows -or $isSrv2k12R2

    Describe "Set-Timezone test case: call by single Id" -Tags @('CI', 'RequireAdminOnWindows') {
        BeforeAll {
            if ($IsWindows) {
                $originalTimeZoneId = (Get-TimeZone).Id
            }
        }
        AfterAll {
            if ($IsWindows) {
                Set-TimeZone -Id $originalTimeZoneId
=======
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
>>>>>>> origin/source-depot
            }
            Set-TimeZone -Id $testTimezone.Id
            $observed = Get-TimeZone
            $testTimezone.Id -eq $observed.Id
        }
<<<<<<< HEAD

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
            if ($IsWindows)
            {
                $originalTimeZoneId = (Get-TimeZone).Id
            }
        }
        AfterAll {
            if ($IsWindows) {
                Set-TimeZone -Id $originalTimeZoneId
=======
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
>>>>>>> origin/source-depot
            }
                
            Set-TimeZone -Name $testTimezone.StandardName
            $observed = Get-TimeZone
            $testTimezone.StandardName -eq $observed.StandardName
        }
<<<<<<< HEAD

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
=======
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
>>>>>>> origin/source-depot
