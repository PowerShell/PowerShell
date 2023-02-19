# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Basic Alias Provider Tests" -Tags "CI" {
    Context "Validate basic PSDrive Cmdlets" {
        BeforeAll {
            #just use same location as TestDrive for simplicity
            $psDriveRoot = "TestDrive:"
            $psDriveName = "PsTestDriveName"
        }

        BeforeEach {
            New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot > $null
        }

        AfterEach {
            Remove-PSDrive -Name $psDriveName -Force -ErrorAction SilentlyContinue
        }

        It "Create a new PSDrive" {
            try {
                $newDrive = New-PSDrive -Name "NewDifferentPSDrive" -PSProvider FileSystem -Root $psDriveRoot
                $newDrive.Name | Should -BeExactly "NewDifferentPSDrive"
                $newDrive.Root | Should -BeExactly (Convert-Path $psDriveRoot)
            }
            finally {
                Remove-PSDrive -Name "NewDifferentPSDrive" -Force -ErrorAction SilentlyContinue
            }
        }

        It "Read data from a PSDrive" {
            $driveProp = Get-ItemProperty ${psDriveName}:
            $driveProp.PSDrive.Name | Should -BeExactly $psDriveName
        }

        It "Remove the PSDrive" {
            $existsBefore = Test-Path "${psDriveName}:\"
            Remove-PSDrive -Name ${psDriveName} -ErrorAction SilentlyContinue
            $existsAfter = Test-Path "${psDriveName}:\"
            $existsBefore | Should -BeTrue
            $existsAfter | Should -BeFalse
        }

        It "Verify 'Used' and 'Free' script properties" {
            $drive = Get-PSDrive -Name $psDriveName
            $drive.Used | Should -Not -BeNullOrEmpty
            $drive.Free | Should -Not -BeNullOrEmpty
        }
    }
}

Describe "Extended Alias Provider Tests" -Tags "Feature" {
    BeforeAll {
        #just use same location as TestDrive for simplicity
        $psDriveRoot = "TestDrive:"
        $psDriveName = "PsTestDriveName"
    }

    Context "Validate New-PSDrive Cmdlet Parameters" {
        AfterEach {
            Remove-PSDrive -Name $psDriveName -Force -ErrorAction SilentlyContinue
        }

        It "Verify Description" {
            $result = New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot -Description "Test PSDrive to remove"
            $result.Description | Should -BeExactly "Test PSDrive to remove"
        }

        It "Verify Confirm can be bypassed" {
            $result = New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot -Confirm:$false
            $result.Name | Should -BeExactly $psDriveName
        }

        It "Verify WhatIf" {
            New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot -WhatIf > $null
            { Get-PSDrive -Name $psDriveName -ErrorAction Stop } | Should -Throw -ErrorId "GetLocationNoMatchingDrive,Microsoft.PowerShell.Commands.GetPSDriveCommand"
        }

        It "Verify Scope" {
            New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot -Description "Test PSDrive to remove" -Scope Local > $null
            $foundGlobal = $true
            { $globalDrive = Get-PSDrive -Name $psDriveName -Scope Global -ErrorAction Stop } | Should -Throw -ErrorId "GetDriveNoMatchingDrive,Microsoft.PowerShell.Commands.GetPSDriveCommand"
            $localDrive = Get-PSDrive -Name $psDriveName -Scope Local
            $localDrive.Name | Should -BeExactly $psDriveName
        }

        It "Verify '-Persist' parameter is not available on UNIX" -Skip:($IsWindows) {
                { New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot -Persist -Description "Test PSDrive to remove" } | Should -Throw -ErrorId "NamedParameterNotFound,Microsoft.PowerShell.Commands.NewPSDriveCommand"
        }
    }

    Context "Validate Get-PSDrive Cmdlet Parameters" {
        BeforeEach {
            New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot > $null
        }

        AfterEach {
            Remove-PSDrive -Name $psDriveName -Force -ErrorAction SilentlyContinue
        }

        It "Verify Name" {
            $result = Get-PSDrive -Name $psDriveName
            $result.Name | Should -BeExactly $psDriveName
        }

        It "Verify PSProvider" {
            $result = Get-PSDrive -PSProvider "Alias"
            $result.Name | Should -BeExactly "Alias"
        }

        It "Verify Scope" {
            $result = Get-PSDrive -Scope 1 #scope 1 because drive was created in BeforeAll
            $result.Name -contains $psDriveName | Should -BeTrue
        }
    }

    Context "Validate Remove-PSDrive Cmdlet Parameters" {
        BeforeEach {
            New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot > $null
        }

        AfterEach {
            Remove-PSDrive -Name $psDriveName -Force -ErrorAction SilentlyContinue
        }

        It "Verify Confirm can be bypassed" {
            Remove-PSDrive $psDriveName -Confirm:$false
            $exists = Test-Path -Path $psDriveName
            $exists | Should -BeFalse
        }

        It "Verify WhatIf" {
            Remove-PSDrive $psDriveName -WhatIf
            $exists = Test-Path -Path "${psDriveName}:"
            $exists | Should -BeTrue
        }
    }
}
