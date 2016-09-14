Describe "Validate Registry Provider" -Tags "CI" {
    BeforeAll {
        $registryBase = "HKLM:\software\Microsoft\PowerShell\3\"
        $testKey = "TestKeyThatWillNotConflict"
        $testSubKey = "SubKey"
        $restoreLocation = Get-Location

        #skip all tests on non-windows platform
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ($IsWindows -eq $false) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }

    AfterAll {
        #restore the previous environment
        Set-Location -Path $restoreLocation
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    Context "Validate basic registry provider Cmdlets" {
        BeforeEach {
            Set-Location $registryBase
            New-Item -Path $testKey > $null
            Set-Location $testKey
            New-Item -Path $testSubKey > $null
        }

        AfterEach {
            Set-Location $registryBase
            Remove-Item -Path $testKey -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Verify New-Item" {
            $newKey = New-Item -Path "NewItemTest"
            $testKeyExists = Test-Path "NewItemTest"
            $testKeyExists | Should Be $true
            Split-Path $newKey.Name -Leaf | Should Be "NewItemTest"
        }

        It "Verify Copy-Item" {
            $copyKey = Copy-Item -Path $testSubKey -Destination "CopiedKey" -PassThru
            $copyExists = Test-Path "CopiedKey"
            $copyExists | Should Be $true
            Split-Path $copyKey.Name -Leaf | Should Be "CopiedKey"
        }

        It "Verify Move-Item" {
            $movedKey = Move-Item -Path $testSubKey -Destination "MovedKey" -PassThru
            $moveExists = Test-Path "MovedKey"
            $moveExists | Should Be $true
            Split-Path $movedKey.Name -Leaf | Should Be "MovedKey"
        }

        It "Verify Rename-Item" {
            $existBefore = Test-Path $testSubKey
            $renamedKey = Rename-Item -path $testSubKey -NewName "RenamedKey" -PassThru
            $existAfter = Test-Path $testSubKey
            $renameExist = Test-Path "RenamedKey"
            $existBefore | Should Be $true
            $existAfter | Should Be $false
            $renameExist | Should Be $true
            Split-Path $renamedKey.Name -Leaf | Should Be "RenamedKey"
        }
    }

    Context "Valdiate basic registry property Cmdlets" {
        BeforeAll {
            $testPropertyName = "TestEntry"
            $testPropertyValue = 1
        }

        BeforeEach {
            Set-Location $registryBase
            New-Item -Path $testKey > $null
            New-ItemProperty -Path $testKey -Name $testPropertyName -Value $testPropertyValue -PropertyType DWORD > $null
        }

        AfterEach {
            Set-Location $registryBase
            Remove-Item -Path $testKey -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Verify New-ItemProperty" {
            New-ItemProperty -Path $testKey -Name "NewTestEntry" -Value 99 -PropertyType DWORD > $null
            $property = Get-ItemProperty -Path $testKey -Name "NewTestEntry"
            $property.NewTestEntry | Should Be 99
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
            $testSubKey = Join-Path -Path $testKey -ChildPath "SubKey"
            New-Item -Path $testSubKey > $null
            Copy-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testSubKey
            $property = Get-ItemProperty -Path $testSubKey -Name $testPropertyName
            $property."$testPropertyName" | Should Be $testPropertyValue
        }

        It "Verify Move-ItemProperty" {
            $testSubKey = Join-Path -Path $testKey -ChildPath "SubKey"
            New-Item -Path $testSubKey > $null
            Move-ItemProperty -Path $testKey -Name $testPropertyName -Destination $testSubKey
            $movedProperty = Get-ItemProperty -Path $testSubKey -Name $testPropertyName
            $movedproperty."$testPropertyName" | Should Be $testPropertyValue
        }

        It "Verify Rename-ItemProperty" {
            Rename-ItemProperty -Path $testKey -Name $testPropertyName -NewName "RenamedProperty"
            $property =  Get-ItemProperty -Path $testKey -Name "RenamedProperty"
            $property.RenamedProperty | Should Be $testPropertyValue
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

        It "Verify Remove-ItemProperty Whatif" {
            Remove-ItemProperty -Path $testKey -Name $testPropertyName -WhatIf -ErrorVariable $errorUsingWhatif
            $property = Get-ItemProperty -Path $testKey -name $testPropertyName 
            $property."$testPropertyName" | should be $testPropertyValue
            $errorObj | Should be $null
        }
    }
}
