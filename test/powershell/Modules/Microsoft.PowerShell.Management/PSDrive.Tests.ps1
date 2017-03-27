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
                $newDrive.Name | Should Be "NewDifferentPSDrive"
                $newDrive.Root | Should Be (Convert-Path $psDriveRoot)
            }
            finally {
                Remove-PSDrive -Name "NewDifferentPSDrive" -Force -ErrorAction SilentlyContinue
            }
        }

        It "Read data from a PSDrive" {
            $driveProp = Get-ItemProperty ${psDriveName}:
            $driveProp.PSDrive.Name | Should Be $psDriveName
        }

        It "Remove the PSDrive" {
            $existsBefore = Test-Path "${psDriveName}:\"
            Remove-PSDrive -Name ${psDriveName} -ErrorAction SilentlyContinue
            $existsAfter = Test-Path "${psDriveName}:\"
            $existsBefore | Should Be $true
            $existsAfter | Should Be $false
        }

        It "Verify 'Used' and 'Free' script properties" {
            $drive = Get-PSDrive -Name $psDriveName
            $drive.Used -eq $null | Should Be $false
            $drive.Free -eq $null | Should Be $false
        }
    }
}

Describe "Extended Alias Provider Tests" -Tags "Feature" {
    BeforeAll {
        #just use same location as TestDrive for simplicity
        $psDriveRoot = "TestDrive:"
        $psDriveName = "PsTestDriveName"
    }

    Context "Valdiate New-PSDrive Cmdlet Parameters" {
        AfterEach {
            Remove-PSDrive -Name $psDriveName -Force -ErrorAction SilentlyContinue
        }

        It "Verify Description" {
            $result = New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot -Description "Test PSDrive to remove"
            $result.Description | Should Be "Test PSDrive to remove"
        }

        It "Verify Confirm can be bypassed" {
            $result = New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot -Confirm:$false
            $result.Name | Should Be $psDriveName
        }

        It "Verify WhatIf" {
            New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot -WhatIf > $null
            try {
                Get-PSDrive -Name $psDriveName -ErrorAction Stop
                throw "Expected exception not thrown"
            }
            catch { $_.FullyQualifiedErrorId | Should Be "GetLocationNoMatchingDrive,Microsoft.PowerShell.Commands.GetPSDriveCommand" }
        }

        It "Verify Scope" {
            New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot -Description "Test PSDrive to remove" -Scope Local > $null
            $foundGlobal = $true
            try {
               $globalDrive = Get-PSDrive -Name $psDriveName -Scope Global -ErrorAction Stop
            }
            catch { $foundGlobal = $false }
            $localDrive = Get-PSDrive -Name $psDriveName -Scope Local
            $foundGlobal | Should Be $false
            $localDrive.Name | Should Be $psDriveName
        }
    }

    Context "Valdiate Get-PSDrive Cmdlet Parameters" {
        BeforeEach {
            New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot > $null
        }

        AfterEach {
            Remove-PSDrive -Name $psDriveName -Force -ErrorAction SilentlyContinue
        }

        It "Verify Name" {
            $result = Get-PSDrive -Name $psDriveName
            $result.Name | Should Be $psDriveName
        }

        It "Verify PSProvider" {
            $result = Get-PSDrive -PSProvider "Alias"
            $result.Name | Should Be "Alias"
        }

        It "Verify Scope" {
            $result = Get-PSDrive -Scope 1 #scope 1 because drive was created in BeforeAll
            $result.Name -contains $psDriveName | Should Be $true
        }
    }

    Context "Valdiate Remove-PSDrive Cmdlet Parameters" {
        BeforeEach {
            New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot > $null
        }

        AfterEach {
            Remove-PSDrive -Name $psDriveName -Force -ErrorAction SilentlyContinue
        }

        It "Verify Confirm can be bypassed" {
            Remove-PSDrive $psDriveName -Confirm:$false
            $exists = Test-Path -Path $psDriveName
            $exists | Should Be $false
        }

        It "Verify WhatIf" {
            Remove-PSDrive $psDriveName -WhatIf
            $exists = Test-Path -Path "${psDriveName}:"
            $exists | Should Be $true
        }
    }
}
