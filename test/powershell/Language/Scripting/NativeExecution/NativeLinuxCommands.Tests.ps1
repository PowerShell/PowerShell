if ( $IsWindows ) {
    $PesterSkipOrPending = @{ Skip = $true }
}
elseif ( $IsOSX ) {
    $PesterSkipOrPending = @{ Pending = $true }
}
else {
    $PesterSkipOrPending = @{}
}
Describe "NativeLinuxCommands" -tags "CI" {
    It "Should return a type of 'string' for hostname cmdlet" {
        $result = hostname
        $result | Should Not BeNullOrEmpty
        $result | Should BeOfType string
    }

    It "Should find Application grep" @PesterSkipOrPending {
        (get-command grep).CommandType | Should Be Application
    }

    It "Should pipe to grep and get result" @PesterSkipOrPending {
        "hello world" | grep hello | Should Be "hello world"
    }

    It "Should find Application touch" @PesterSkipOrPending {
        (get-command touch).CommandType | Should Be Application
    }

    It "Should not redirect standard input if native command is the first command in pipeline (1)" @PesterSkipOrPending {
        stty | ForEach-Object -Begin { $out = @() } -Process { $out += $_ }
        $out.Length -gt 0 | Should Be $true
        $out[0] -like "speed * baud; line =*" | Should Be $true
    }

    It "Should not redirect standard input if native command is the first command in pipeline (2)" @PesterSkipOrPending {
        $out = stty
        $out.Length -gt 0 | Should Be $true
        $out[0] -like "speed * baud; line =*" | Should Be $true
    }
}

Describe "Scripts with extensions" -tags "CI" {
    BeforeAll {
        $data = "Hello World"
        Setup -File testScript.ps1 -Content "'$data'"
        $originalPath = $env:PATH
        $env:PATH += [IO.Path]::PathSeparator + $TestDrive
    }

    AfterAll {
        $env:PATH = $originalPath
    }

    It "Should run a script with its full name" {
        testScript.ps1 | Should Be $data
    }

    It "Should run a script with its short name" {
        testScript | Should Be $data
    }
}
