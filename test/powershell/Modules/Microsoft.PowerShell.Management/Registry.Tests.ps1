try {
    #skip all tests on non-windows platform
    $defaultParamValues = $PSdefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:skip"] = !$IsWindows

Describe "Basic Registry Provider Tests" -Tags @("CI", "RequireAdminOnWindows") {
    BeforeAll {
        if ($IsWindows) {
            $restoreLocation = Get-Location
            $registryBase = "HKLM:\software\Microsoft\PowerShell\3\"
            $parentKey = "TestKeyThatWillNotConflict"
            $testKey = "TestKey"
            $testKey2 = "TestKey2"
            $testPropertyName = "TestEntry"
            $testPropertyValue = 1
        }
    }

    AfterAll {
        if ($IsWindows) {
            #restore the previous environment
            Set-Location -Path $restoreLocation
        }
    }

    BeforeEach {
        if ($IsWindows) {
            #create a parent key that can be easily removed and will not conflict
            Set-Location $registryBase
            New-Item -Path $parentKey > $null
            #create the test keys/test properties for the tests to manipulate
            Set-Location $parentKey
            New-Item -Path $testKey > $null
            New-Item -Path $testKey2 > $null
            New-ItemProperty -Path $testKey -Name $testPropertyName -Value $testPropertyValue > $null
        }
    }

    AfterEach {
        if ($IsWindows) {
            Set-Location $registryBase
            Remove-Item -Path $parentKey -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context "Validate basic registry provider Cmdlets" {
        It "Verify New-Item" {
            $newKey = New-Item -Path "NewItemTest"
            Test-Path "NewItemTest" | Should Be $true
            Split-Path $newKey.Name -Leaf | Should Be "NewItemTest"
        }

        It "Verify Copy-Item" {
            $copyKey = Copy-Item -Path $testKey -Destination "CopiedKey" -PassThru
            Test-Path "CopiedKey" | Should Be $true
            Split-Path $copyKey.Name -Leaf | Should Be "CopiedKey"
        }

        It "Verify Move-Item" {
            $movedKey = Move-Item -Path $testKey -Destination "MovedKey" -PassThru
            Test-Path "MovedKey" | Should Be $true
            Split-Path $movedKey.Name -Leaf | Should Be "MovedKey"
        }

        It "Verify Rename-Item" {
            $existBefore = Test-Path $testKey
            $renamedKey = Rename-Item -path $testKey -NewName "RenamedKey" -PassThru
            $existAfter = Test-Path $testKey
            $existBefore | Should Be $true
            $existAfter | Should Be $false
            Test-Path "RenamedKey" | Should Be $true
            Split-Path $renamedKey.Name -Leaf | Should Be "RenamedKey"
        }
    }

    Context "Valdiate basic registry property Cmdlets" {
        It "Verify New-ItemProperty" {
            New-ItemProperty -Path $testKey -Name "NewTestEntry" -Value 99 > $null
            $property = Get-ItemProperty -Path $testKey -Name "NewTestEntry"
            $property.NewTestEntry | Should Be 99
            $property.PSChildName | Should Be $testKey
        }

        It "Verify Set-ItemProperty" {
            Set-ItemProperty -Path $testKey -Name $testPropertyName -Value 2
            $property = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $property."$testPropertyName" | Should Be 2
        }

        It "Verify Get-ItemPropertyValue" {
            $propertyValue = Get-ItemPropertyValue -Path $testKey -Name $testPropertyName
            $propertyValue | Should Be $testPropertyValue
        }

        It "Verify Copy-ItemProperty" {
            Copy-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testKey2
            $property1 = Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2 = Get-ItemProperty -Path $testKey2 -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2."$testPropertyName" | Should Be $property1."$testPropertyName"
            $property1.PSChildName | Should Be $testKey
            $property2.PSChildName | Should Be $testKey2
        }

        It "Verify Move-ItemProperty" {
            Move-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testKey2
            $property1 = Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2 = Get-ItemProperty -Path $testKey2 -Name $testPropertyName -ErrorAction SilentlyContinue
            $property1 | Should BeNullOrEmpty
            $property2."$testPropertyName" | Should Be $testPropertyValue
            $property2.PSChildName | Should Be $testKey2
        }

        It "Verify Rename-ItemProperty" {
            Rename-ItemProperty -Path $testKey -Name $testPropertyName -NewName "RenamedProperty"
            $property1 = Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2 = Get-ItemProperty -Path $testKey -Name "RenamedProperty" -ErrorAction SilentlyContinue
            $property1 | Should BeNullOrEmpty
            $property2.RenamedProperty | Should Be $testPropertyValue
            $property2.PSChildName | Should Be $testKey
        }

        It "Verify Clear-ItemProperty" {
            Clear-ItemProperty -Path $testKey -Name $testPropertyName
            $property = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $property."$testPropertyName" | Should Be 0
        }

        It "Verify Remove-ItemProperty" {
            Remove-ItemProperty -Path $testKey -Name $testPropertyName
            $properties = @(Get-ItemProperty -Path $testKey)
            $properties.Count | Should Be 0
        }
    }
}

Describe "Extended Registry Provider Tests" -Tags @("Feature", "RequireAdminOnWindows") {
    BeforeAll {
        if ($IsWindows) {
            $restoreLocation = Get-Location
            $registryBase = "HKLM:\software\Microsoft\PowerShell\3\"
            $parentKey = "TestKeyThatWillNotConflict"
            $testKey = "TestKey"
            $testKey2 = "TestKey2"
            $testPropertyName = "TestEntry"
            $testPropertyValue = 1
        }
    }

    AfterAll {
        if ($IsWindows) {
            #restore the previous environment
            Set-Location -Path $restoreLocation
        }
    }

    BeforeEach {
        if ($IsWindows) {
            #create a parent key that can be easily removed and will not conflict
            Set-Location $registryBase
            New-Item -Path $parentKey > $null
            #create the test keys/test properties for the tests to manipulate
            Set-Location $parentKey
            New-Item -Path $testKey > $null
            New-Item -Path $testKey2 > $null
            New-ItemProperty -Path $testKey -Name $testPropertyName -Value $testPropertyValue > $null
            New-ItemProperty -Path $testKey2 -Name $testPropertyName -Value $testPropertyValue > $null
        }
    }

    AfterEach {
        if ($IsWindows) {
            Set-Location $registryBase
            Remove-Item -Path $parentKey -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context "Valdiate New-ItemProperty Parameters" {
        BeforeEach {
            #remove the current properties so that we can remake them with different parameters
            Remove-ItemProperty -Path $testKey -Name $testPropertyName -Force -ErrorAction SilentlyContinue
            Remove-ItemProperty -Path $testKey2 -Name $testPropertyName -Force -ErrorAction SilentlyContinue
        }

        It "Verify Filter" {
            try {
                $result = New-ItemProperty -Path ".\*" -Filter "Test*" -Name $testPropertyName -Value $testPropertyValue -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "NotSupported,Microsoft.PowerShell.Commands.NewItemPropertyCommand" }
        }

        It "Verify Include" {
            $result = New-ItemProperty -Path ".\*" -Include "*2" -Name $testPropertyName -Value $testPropertyValue
            $result."$testPropertyName" | Should Be $testPropertyValue
            $result.PSChildName | Should Be $testKey2
        }

        It "Verify Exclude" {
            $result = New-ItemProperty -Path ".\*" -Exclude "*2" -Name $testPropertyName -Value $testPropertyValue
            $result."$testPropertyName" | Should Be $testPropertyValue
            $result.PSChildName | Should Be $testKey
        }

        It "Verify Confirm can be bypassed" {
            $result = New-ItemProperty -Path $testKey -Name $testPropertyName -Value $testPropertyValue -force -Confirm:$false
            $result."$testPropertyName" | Should Be $testPropertyValue
            $result.PSChildName | Should Be $testKey
        }

        It "Verify WhatIf" {
            $result = New-ItemProperty -Path $testKey -Name $testPropertyName -Value $testPropertyValue -whatif
            $result | Should BeNullOrEmpty
        }
    }

    Context "Valdiate Get-ItemProperty Parameters" {
        It "Verify Name" {
            $result = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $result."$testPropertyName" | Should Be $testPropertyValue
            $result.PSChildName | Should Be $testKey
        }

        It "Verify Path but no Name" {
            $result = Get-ItemProperty -Path $testKey
            $result."$testPropertyName" | Should Be $testPropertyValue
            $result.PSChildName | Should Be $testKey
        }

        It "Verify Filter" {
            try {
                $result = Get-ItemProperty -Path ".\*" -Filter "*Test*" -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "NotSupported,Microsoft.PowerShell.Commands.GetItemPropertyCommand" }
        }

        It "Verify Include" {
            $result = Get-ItemProperty -Path ".\*" -Include "*2"
            $result."$testPropertyName" | Should Be $testPropertyValue
            $result.PSChildName | Should Be $testKey2
        }

        It "Verify Exclude" {
            $result = Get-ItemProperty -Path ".\*" -Exclude "*2"
            $result."$testPropertyName" | Should Be $testPropertyValue
            $result.PSChildName | Should Be $testKey
        }
    }

    Context "Valdiate Get-ItemPropertyValue Parameters" {
        It "Verify Name" {
            $result = Get-ItemPropertyValue -Path $testKey -Name $testPropertyName
            $result | Should Be $testPropertyValue
        }
    }

    Context "Valdiate Set-ItemPropertyValue Parameters" {
        BeforeAll {
            $newPropertyValue = 2
        }

        It "Verify Name" {
            Set-ItemProperty -Path $testKey -Name $testPropertyName -Value $newPropertyValue
            $result = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $result."$testPropertyName" | Should Be $newPropertyValue
        }

        It "Verify PassThru" {
            $result = Set-ItemProperty -Path $testKey -Name $testPropertyName -Value $newPropertyValue -PassThru
            $result."$testPropertyName" | Should Be $newPropertyValue
        }

        It "Verify Piped Default Parameter" {
            $prop = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $prop | Set-ItemProperty -Name $testPropertyName -Value $newPropertyValue
            $result = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $result."$testPropertyName" | Should Be $newPropertyValue
        }

        It "Verify WhatIf" {
            $result = Set-ItemProperty -Path $testKey -Name $testPropertyName -Value $newPropertyValue -PassThru -WhatIf
            $result = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $result."$testPropertyName" | Should Be $testPropertyValue
        }

        It "Verify Confirm can be bypassed" {
            $result = Set-ItemProperty -Path $testKey -Name $testPropertyName -Value $newPropertyValue -PassThru -Confirm:$false
            $result = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $result."$testPropertyName" | Should Be $newPropertyValue
        }
    }

    Context "Valdiate Copy-ItemProperty Parameters" {
        BeforeEach {
            #remove the current property so that we have a place to copy to
            Remove-ItemProperty -Path $testKey2 -Name $testPropertyName -Force -ErrorAction SilentlyContinue
        }

        It "Verify PassThru" {
            #passthru returns the property on testKey not testKey2
            $property1 = Copy-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testKey2 -PassThru
            $property2 = Get-ItemProperty -Path $testKey2 -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2."$testPropertyName" | Should Be $property1."$testPropertyName"
            $property1.PSChildName | Should Be $testKey
            $property2.PSChildName | Should Be $testKey2
        }

        It "Verify Confirm can be bypassed" {
            Copy-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testKey2 -Confirm:$false
            $property1 = Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2 = Get-ItemProperty -Path $testKey2 -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2."$testPropertyName" | Should Be $property1."$testPropertyName"
            $property1.PSChildName | Should Be $testKey
            $property2.PSChildName | Should Be $testKey2
        }

        It "Verify WhatIf" {
            Copy-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testKey2 -WhatIf
            try {
                Get-ItemProperty -Path $testKey2 -Name $testPropertyName -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "System.Management.Automation.PSArgumentException,Microsoft.PowerShell.Commands.GetItemPropertyCommand" }
        }
    }

    Context "Valdiate Move-ItemProperty Parameters" {
        BeforeEach {
            #remove the current property so that we have a place to move to
            Remove-ItemProperty -Path $testKey2 -Name $testPropertyName -Force -ErrorAction SilentlyContinue
        }

        It "Verify PassThru" {
            $property2 = Move-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testKey2 -PassThru
            $property1 = Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction SilentlyContinue
            $property1 | Should BeNullOrEmpty
            $property2."$testPropertyName" | Should Be $testPropertyValue
            $property2.PSChildName | Should Be $testKey2
        }

        It "Verify Confirm can be bypassed" {
            Move-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testKey2 -Confirm:$false
            $property1 = Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2 = Get-ItemProperty -Path $testKey2 -Name $testPropertyName -ErrorAction SilentlyContinue
            $property1 | Should BeNullOrEmpty
            $property2."$testPropertyName" | Should Be $testPropertyValue
            $property2.PSChildName | Should Be $testKey2
        }

        It "Verify WhatIf" {
            Move-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testKey2 -WhatIf
            $property1 = Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2 = Get-ItemProperty -Path $testKey2 -Name $testPropertyName -ErrorAction SilentlyContinue
            $property1."$testPropertyName" | Should Be $testPropertyValue
            $property1.PSChildName | Should Be $testKey
            $property2 | Should BeNullOrEmpty
        }
    }

    Context "Valdiate Rename-ItemProperty Parameters" {
        BeforeAll {
            $newPropertyName = "NewEntry"
        }

        It "Verify Confirm can be bypassed" {
            Rename-ItemProperty -Path $testKey -Name $testPropertyName -NewName $newPropertyName -Confirm:$false
            $property1 = Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2 = Get-ItemProperty -Path $testKey -Name $newPropertyName -ErrorAction SilentlyContinue
            $property1 | Should BeNullOrEmpty
            $property2."$newPropertyName" | Should Be $testPropertyValue
        }

        It "Verify WhatIf" {
            Rename-ItemProperty -Path $testKey -Name $testPropertyName -NewName $newPropertyName -WhatIf
            $property1 = Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction SilentlyContinue
            $property2 = Get-ItemProperty -Path $testKey -Name $newPropertyName -ErrorAction SilentlyContinue
            $property1."$testPropertyName" | Should Be $testPropertyValue
            $property2 | Should BeNullOrEmpty
        }
    }

    Context "Valdiate Clear-ItemProperty Parameters" {
        It "Verify Confirm can be bypassed" {
            Clear-ItemProperty -Path $testKey -Name $testPropertyName -Confirm:$false
            $result = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $result."$testPropertyName" | Should Be 0
        }

        It "Verify WhatIf" {
            Clear-ItemProperty -Path $testKey -Name $testPropertyName -WhatIf
            $result = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $result."$testPropertyName" | Should Be $testPropertyValue
        }
    }

    Context "Valdiate Remove-ItemProperty Parameters" {
        It "Verify Confirm can be bypassed" {
            Remove-ItemProperty -Path $testKey -Name $testPropertyName -Confirm:$false
            try {
                Get-ItemProperty -Path $testKey -Name $testPropertyName -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "System.Management.Automation.PSArgumentException,Microsoft.PowerShell.Commands.GetItemPropertyCommand" }
        }

        It "Verify WhatIf" {
            Remove-ItemProperty -Path $testKey -Name $testPropertyName -WhatIf
            $result = Get-ItemProperty -Path $testKey -Name $testPropertyName
            $result."$testPropertyName" | Should Be $testPropertyValue
        }
    }
}

} finally {
    $global:PSdefaultParameterValues = $defaultParamValues
}
