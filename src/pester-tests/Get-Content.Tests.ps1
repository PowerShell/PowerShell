Describe "Get-Content" {
    $testString = "This is a test content for a file"
    $nl         = "`n"

    $firstline  = "Here's a first line "
    $secondline = " here's a second line"
    $thirdline  = "more text"
    $fourthline = "just to make sure"
    $fifthline  = "there's plenty to work with"

    $testString2 = $firstline + $nl + $secondline + $nl + $thirdline + $nl + $fourthline + $nl + $fifthline

    $testPath   = "/tmp/testfile1"
    $testPath2  = "/tmp/testfile2"

    BeforeEach {
        New-Item -Path $testPath -Force -Value $testString
        New-Item -Path $testPath2 -Force -Value $testString2
    }

    It "Should throw an error on a directory  " {
        # also tests that -erroraction SilentlyContinue will work.
        Get-Content . -ErrorAction SilentlyContinue | Should Throw
    }

    It "Should return an Object when listing only a single line" {
        (Get-Content -Path $testPath).GetType().BaseType.Name | Should Be "Object"
    }

    It "Should deliver an array object when listing a file with multiple lines" {
        (Get-Content -Path $testPath2).GetType().BaseType.Name | Should Be "Array"
    }

    It "Should return the correct information from a file" {
        (Get-Content -Path $testPath) | Should Be $testString
    }

    It "Should be able to call using the cat alias" {
        { cat -Path $testPath } | Should Not Throw
    }

    It "Should be able to call using the gc alias" {
        { gc -Path $testPath } | Should Not Throw
    }

    It "Should be able to call using the type alias" {
        { type -Path $testPath } | Should Not Throw
    }

    It "Should return the same values for aliases" {
        $getContentAlias = Get-Content -Path $testPath
        $gcAlias         = gc -Path $testPath
        $catAlias        = cat -Path $testPath
        $typeAlias       = type -Path $testPath

        $getContentAlias | Should Be $gcAlias
        $getContentAlias | Should Be $catAlias
        $getContentAlias | Should Be $typeAlias
    }

    It "Should be able to return a specific line from a file" {
        (Get-Content -Path $testPath2)[1] | Should be $secondline
    }

    It "Should be able to specify the number of lines to get the content of using the TotalCount switch" {
        $returnArray = (cat -Path $testPath2 -TotalCount 2)

        $returnArray[0] | Should Be $firstline
        $returnArray[1] | Should Be $secondline
    }

    It "Should be able to specify the number of lines to get the content of using the Head switch" {
        $returnArray = (cat -Path $testPath2 -Head 2)

        $returnArray[0] | Should Be $firstline
        $returnArray[1] | Should Be $secondline
    }

    It "Should be able to specify the number of lines to get the content of using the First switch" {
        $returnArray = (cat -Path $testPath2 -First 2)

        $returnArray[0] | Should Be $firstline
        $returnArray[1] | Should Be $secondline
    }

    It "Should return the last line of a file using the Tail switch" {
        Get-Content -Path $testPath -Tail 1 | Should Be $testString
    }


    It "Should return the last lines of a file using the Last alias" {
        Get-Content -Path $testPath2 -Last 1 | Should Be $fifthline
    }

    It "Should be able to get content within a different drive" {
        pushd env:
        $expectedoutput = [Environment]::GetEnvironmentVariable("PATH");

        { Get-Content PATH } | Should Not Throw
        Get-Content PATH     | Should Be $expectedoutput

        popd
    }
}
