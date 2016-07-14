Describe "Command Path Search" -Tags "CI" {

    BeforeAll {
        setup -d bin
        setup -d bin/d1
        setup -d bid/d2
        
        setup -f bin/d1/t2.cmd -content "@echo d1-t2-cmd"
        setup -f bin/d2/t2.cmd -content "@echo d2-t2-cmd"

        setup -f bin/d1/t2.cmd -content "@echo d1-t2-cmd"
        setup -f bin/d1/t2.cmd -content "@echo d1-t2-cmd"

        
        setup -f bin/d2/t2.cmd -content "@echo d2-t2-cmd" 
        setup -f bin/d1/t1.cmd -content "@echo t1-cmd" 
        setup -f bin/d2/t1.bat -content "@echo t1-bat" 
        setup -f bin/d1/t3.oops.cmd -content "@echo d1-t3-oops" 
        setup -f bin/d2/t3.cmd -content "@echo d2-t3" 
        setup -f bin/d2/t4.cmd -content "@echo d2-t4-cmd" 

        setup -f bin/d2/t4.ps1 -content "'d2-t4-ps1'" 

        $origPATHEXT = $env:PATHEXT
        $origPATH = $env:PATH

        if ( $IsWindows ) {
            $env:PATH = "${testDriveRoot}\bin\d1;${testDriveRoot}\bin\d2"
        }
        else {
            $env:PATH = "${testdrive}/bin/d1:${testdrive}/bin/d2"
        }
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

    It "relative path works correctly" -skip:(!$IsWindows) {
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

    It "ps1 wins over PATHEXT" -skip:(!$IsWindows) {
        t4 | Should Be "d2-t4-ps1"
    }

    It "PATH searched in correct order" -skip:(!$IsWindows) {
        t2 | Should Be "d1-t2-cmd"
        t2.cmd | Should Be "d1-t2-cmd"
    }

    It "PATH wins over PATHEXT" -skip:(!$IsWindows) {
        t1 | Should Be 't1-cmd'
    }

    It "ext wins over path if specified" -skip:(!$IsWindows) {
        t1.cmd | Should Be 't1-cmd'
        t1.bat | Should Be 't1-bat'
    }

    It "Check full filename" -skip:(!$IsWindows) {
        t3 | Should Be 'd2-t3'
        t3.cmd | Should Be 'd2-t3'

        t3.oops | Should Be 'd1-t3-oops'
        t3.oops.cmd | Should Be 'd1-t3-oops'
    }

    It "Extra whitespace around command name" -skip:(!$IsWindows) {
        & "t3 " | Should Be 'd2-t3'
        & "t3.cmd " | Should Be 'd2-t3'
        & ".\bin\d2\t3 " | Should Be 'd2-t3'
        & ".\bin\d2\t3.cmd " | Should Be 'd2-t3'

        # Leading space only worked with a drive qualified name
        # but trailing space w/ no leading space opens the document
        & " ${testdrive}\bin\d2\t3" | Should Be 'd2-t3'
        & " ${testdrive}\bin\d2\t3 " | Should Be 'd2-t3'
        & " ${testdrive}\bin\d2\t3.cmd" | Should Be 'd2-t3'
        & " ${testdrive}\bin\d2\t3.cmd " | Should Be 'd2-t3'
    }
}

