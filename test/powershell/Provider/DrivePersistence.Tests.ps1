Describe "Tests to check persistence of providers drives" -Tags "P1", "RI" {
    
    # test for bug 2784052
    It "Get-PSDrive should not remove custom drives" {
         $newDrives = Get-ChildItem function:[e-z]: -Name | ?{ !(Test-Path $_) } | select -First 7
         $newDriveLetters = $newDrives -replace ".$"
         $originalDriveCount = (Get-PSDrive).Count
         new-psdrive $newDriveLetters[0] -psp FileSystem -root 'c:\'
         new-psdrive $newDriveLetters[1] -psp Registry -root 'hkcu:\'
         new-psdrive $newDriveLetters[2] -psp Environment -root ''
         new-psdrive $newDriveLetters[3] -psp Function -root ''
         new-psdrive $newDriveLetters[4] -psp Certificate -root '\'
         new-psdrive $newDriveLetters[5] -psp Variable -root ''
         new-psdrive $newDriveLetters[6] -psp Alias -root ''

         # run Get-PSDrive several times to check for bug 2784052 (Fixing regression in CL#498641)
         # bug in 'if' condition in get-psdrive resulted in custom non-filesystem provider drives being removed when get-psdrive was called.
         1..3 | % {Get-PSDrive} | Out-Null
         $newDriveCount = (Get-PSDrive).Count
         $newDriveCount | Should Be ($originalDriveCount + 7)
         $newDrives | %{ Test-Path $_ | Should Be $true }
         Remove-PSDrive $newDriveLetters
    }
}