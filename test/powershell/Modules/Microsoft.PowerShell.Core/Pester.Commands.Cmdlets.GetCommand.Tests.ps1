##
## Copyright (c) Microsoft Corporation, 2015
##

Describe "Tests Get-Command with relative paths and wildcards" -Tag "CI" {

    BeforeAll {
        # Create temporary EXE command files
        $file1 = Setup -f WildCardCommandA.exe -pass
        $file2 = Setup -f WildCardCommand[B].exe -pass
        #$null = New-Item -ItemType File -Path (Join-Path $TestDrive WildCardCommandA.exe) -ErrorAction Ignore
        #$null = New-Item -ItemType File -Path (Join-Path $TestDRive WildCardCommand[B].exe) -ErrorAction Ignore
        if ( $IsLinux -or $IsOSX ) {
            /bin/chmod 777 "$file1"
            /bin/chmod 777 "$file2"
        }
    }

    It "Test wildcard with drive relative directory path" {
        $pathName = Join-Path $TestDrive "WildCardCommandA*"
        $driveOffset = $pathName.IndexOf(":")
        $pathName = $pathName.Substring($driveOffset + 1)
        $result = Get-Command -Name $pathName
        $result | Should Not Be $null
        $result.Name | Should Be WildCardCommandA.exe
    }

    It "Test wildcard with relative directory path" {
        push-location $TestDrive
        $result = Get-Command -Name .\WildCardCommandA*
        pop-location
        $result | Should Not Be $null
        $result | Should Be WildCardCommandA.exe
    }
    
    It "Test with PowerShell wildcard and relative path" {
        push-location $TestDrive

        # This should use the wildcard to find WildCardCommandA.exe
        $result = Get-Command -Name .\WildCardCommand[A].exe
        $result | Should Not Be $null
        $result | Should Be WildCardCommandA.exe

        # This should find the file WildCardCommand[B].exe
        $result = Get-Command -Name .\WildCardCommand[B].exe
        $result | Should Not Be $null
        $result | Should Be WildCardCommand[B].exe

        Pop-Location
    }

}
