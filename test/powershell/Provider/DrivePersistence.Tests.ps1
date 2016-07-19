Describe "Tests to check persistence of providers drives" -Tags "Feature" {
    
    BeforeAll {
        # test for bug 2784052
        $originalDriveCount = (Get-PSDrive).Count
        if ( $IsWindows ) {
            $count = 7
            $newDrives = Get-ChildItem function:[e-z]: -Name | ?{ !(Test-Path $_) } | select-object -First $count
            $newDriveLetters = $newDrives -replace ".$"
            $null = new-psdrive $newDriveLetters[0] -psp FileSystem -root 'c:\'
            $null = new-psdrive $newDriveLetters[1] -psp Registry -root 'hkcu:\'
            $null = new-psdrive $newDriveLetters[2] -psp Environment -root ''
            $null = new-psdrive $newDriveLetters[3] -psp Function -root ''
            $null = new-psdrive $newDriveLetters[4] -psp Certificate -root '\'
            $null = new-psdrive $newDriveLetters[5] -psp Variable -root ''
            $null = new-psdrive $newDriveLetters[6] -psp Alias -root ''
        }
        else {
            $count = 5
            $newDriveLetters = [int][char]"a"..[int][char]"z"|select-object -first $count | %{ [char]$_ }
            $null = new-psdrive $newDriveLetters[0] -psp FileSystem -root '/' -scope global
            $null = new-psdrive $newDriveLetters[1] -psp Environment -root '' -scope global
            $null = new-psdrive $newDriveLetters[2] -psp Function -root '' -scope global
            $null = new-psdrive $newDriveLetters[3] -psp Variable -root '' -scope global
            $null = new-psdrive $newDriveLetters[4] -psp Alias -root '' -scope global
        }
        $driveCollection = $newDriveLetters | %{ @{ Drive = "${_}:" } }

        # run Get-PSDrive several times to check for bug 2784052 (Fixing regression in CL#498641)
        # bug in if condition in get-psdrive resulted in custom non-filesystem provider drives being removed when get-psdrive was called.
        1..3 | % {Get-PSDrive} | Out-Null
    }
    AfterAll {
        Remove-PSDrive $newDriveLetters
    }

    It "Get-PSDrive should not remove custom drives" {
        $newDriveCount = (Get-PSDrive).Count
        $newDriveCount | Should Be ($originalDriveCount + $count)
    }
    It "drive <drive> should exist" -testcases $driveCollection {
        param ( $drive )
        "$drive" | should exist
    }
}
