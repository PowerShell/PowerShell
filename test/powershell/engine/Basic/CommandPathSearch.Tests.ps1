return
Describe "Command Path Search" -Tags "DRT" {

    BeforeAll {
        $null = mkdir TestDrive:\bin
        $null = mkdir TestDrive:\bin\d1
        $null = mkdir TestDrive:\bin\d2

        $testDriveRoot = (Get-PSDrive TestDrive).Root
        
        "@echo d1-t2-cmd" | Out-File -Encoding Ascii TestDrive:bin\d1\t2.cmd
        "@echo d2-t2-cmd" | Out-File -Encoding Ascii TestDrive:bin\d2\t2.cmd
        "@echo t1-cmd" | Out-File -Encoding Ascii TestDrive:bin\d1\t1.cmd
        "@echo t1-bat" | Out-File -Encoding Ascii TestDrive:bin\d2\t1.bat
        "@echo d1-t3-oops" | Out-File -Encoding Ascii TestDrive:bin\d1\t3.oops.cmd
        "@echo d2-t3" | Out-File -Encoding Ascii TestDrive:bin\d2\t3.cmd
        "@echo d2-t4-cmd" | Out-File -Encoding Ascii TestDrive:bin\d2\t4.cmd
        "'d2-t4-ps1'" | Out-File -Encoding Ascii TestDrive:bin\d2\t4.ps1

        $origPATHEXT = $env:PATHEXT
        $origPATH = $env:PATH

        $env:PATH = "${testDriveRoot}\bin\d1;${testDriveRoot}\bin\d2"
        $env:PATHEXT = ".BAT;.CMD"
    }

    AfterAll {
        $env:PATH = $origPATH
        $env:PATHEXT = $origPATHEXT
    }

    BeforeEach {
        pushd TestDrive:\
    }

    AfterEach {
        popd
    }

    It "relative path works correctly" {
        .\bin\d1\t2 | Should Be 'd1-t2-cmd'
        .\bin\d1\t2.cmd | Should Be 'd1-t2-cmd'
        .\bin\d2\t2 | Should Be 'd2-t2-cmd'
        .\bin\d2\t2.cmd | Should Be 'd2-t2-cmd'

        cd bin

        .\d1\t2 | Should Be 'd1-t2-cmd'
        .\d1\t2.cmd | Should Be 'd1-t2-cmd'
        .\d2\t2 | Should Be 'd2-t2-cmd'
        .\d2\t2.cmd | Should Be 'd2-t2-cmd'
    }

    It "ps1 wins over PATHEXT" {
        t4 | Should Be "d2-t4-ps1"
    }

    It "PATH searched in correct order" {
        t2 | Should Be "d1-t2-cmd"
        t2.cmd | Should Be "d1-t2-cmd"
    }

    It "PATH wins over PATHEXT" {
        t1 | Should Be 't1-cmd'
    }

    It "ext wins over path if specified" {
        t1.cmd | Should Be 't1-cmd'
        t1.bat | Should Be 't1-bat'
    }

    It "Check full filename" {
        t3 | Should Be 'd2-t3'
        t3.cmd | Should Be 'd2-t3'

        t3.oops | Should Be 'd1-t3-oops'
        t3.oops.cmd | Should Be 'd1-t3-oops'
    }

    It "Extra whitespace around command name" {
        & "t3 " | Should Be 'd2-t3'
        & "t3.cmd " | Should Be 'd2-t3'
        & ".\bin\d2\t3 " | Should Be 'd2-t3'
        & ".\bin\d2\t3.cmd " | Should Be 'd2-t3'

        # Leading space only worked with a drive qualified name
        # but trailing space w/ no leading space opens the document
        & " ${testDriveRoot}\bin\d2\t3" | Should Be 'd2-t3'
        & " ${testDriveRoot}\bin\d2\t3 " | Should Be 'd2-t3'
        & " ${testDriveRoot}\bin\d2\t3.cmd" | Should Be 'd2-t3'
        & " ${testDriveRoot}\bin\d2\t3.cmd " | Should Be 'd2-t3'
    }
}

