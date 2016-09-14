Describe "Validate basic PSDrive Cmdlets" -Tags "CI" {
    BeforeAll {
        #just use same location as TestDrive for simplicity
        $psDriveRoot = "TestDrive:"
        $psDriveName = "PsTestDriveName"
    }

    BeforeEach {
        New-PSDrive -Name $psDriveName -PSProvider FileSystem -Root $psDriveRoot > $null
    }

    AfterEach {
        Remove-PSDrive -Name $psDriveName -ErrorAction SilentlyContinue
    }

    It "Create a new PSDrive" {
        $newDrive = New-PSDrive -Name "NewDifferentPSDrive" -PSProvider FileSystem -Root $psDriveRoot
        try {
            $newDrive.Name | Should Be "NewDifferentPSDrive"
            $newDrive.Root | Should Be (Convert-Path $psDriveRoot)
        }
        finally {
            Remove-PSDrive -Name "NewDifferentPSDrive" -ErrorAction SilentlyContinue
        }
    }

    It "Read data from a PSDrive" {
        $driveProp = Get-ItemProperty ${psDriveName}:
        $driveProp.PSDrive.Name | Should Be $psDriveName
    }

    It "Remove the PSDrive" {
        $existsBefore = Test-Path "${psDriveName}:\"
        Remove-PSDrive -Name ${psDriveName} -ea SilentlyContinue
        $existsAfter = Test-Path "${psDriveName}:\"
        $existsBefore | Should Be $true
        $existsAfter | Should Be $false
    }

    It "Verify 'Used' and 'Free' script properties" {
        $drive = Get-PSDrive -Name $psDriveName
        $drive.Used -gt 0 | Should Be $true
        $drive.Free -gt 0 | Should Be $true
    }
}
