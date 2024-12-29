# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Select-String" -Tags "CI" {
    BeforeAll {
        $nl = [Environment]::NewLine
        $currentDirectory = $PWD.Path
        $originalRendering = $PSStyle.OutputRendering
        $PSStyle.OutputRendering = 'Ansi'
    }

    AfterAll {
        $PSStyle.OutputRendering = $originalRendering
        Push-Location $currentDirectory
    }

    Context "String actions" {
        BeforeAll {
            $testinputone = "hello","Hello","goodbye"
            $testinputtwo = "hello","Hello"
        }

        It "Should be called without errors" {
            { $testinputone | Select-String -Pattern "hello" } | Should -Not -Throw
        }

        It "Should return an array data type when multiple matches are found" {
            $result = $testinputtwo | Select-String -Pattern "hello"
            ,$result | Should -BeOfType System.Array
        }

        It "Should return an object type when one match is found" {
            $result = $testinputtwo | Select-String -Pattern "hello" -CaseSensitive
            ,$result | Should -BeOfType System.Object
        }

        It "Should return matchinfo type" {
            $result = $testinputtwo | Select-String -Pattern "hello" -CaseSensitive
            ,$result | Should -BeOfType Microsoft.PowerShell.Commands.MatchInfo
        }

        It "Should be called without an error using ca for casesensitive " {
            {$testinputone | Select-String -Pattern "hello" -ca } | Should -Not -Throw
        }

        It "Should use the ca alias for casesensitive" {
            $firstMatch = $testinputtwo  | Select-String -Pattern "hello" -CaseSensitive
            $secondMatch = $testinputtwo | Select-String -Pattern "hello" -ca

            $equal = @(Compare-Object $firstMatch $secondMatch).Length -eq 0
            $equal | Should -BeTrue
        }

        It "Should only return the case sensitive match when the casesensitive switch is used" {
            $testinputtwo | Select-String -Pattern "hello" -CaseSensitive | Should -Be "hello"
        }

        It "Should accept a collection of strings from the input object" {
            { Select-String -InputObject "some stuff", "other stuff" -Pattern "other" } | Should -Not -Throw
        }

        It "Should return system.object when the input object switch is used on a collection" {
            $result = Select-String -InputObject "some stuff", "other stuff" -Pattern "other"
            ,$result | Should -BeOfType System.Object
        }

        It "Should return null or empty when the input object switch is used on a collection and the pattern does not exist" {
            Select-String -InputObject "some stuff", "other stuff" -Pattern "neither" | Should -BeNullOrEmpty
        }

        It "Should return a bool type when the quiet switch is used" {
            ,($testinputtwo | Select-String -Quiet "hello" -CaseSensitive) | Should -BeOfType System.Boolean
        }

        It "Should be true when select string returns a positive result when the quiet switch is used" {
            ($testinputtwo | Select-String -Quiet "hello" -CaseSensitive) | Should -BeTrue
        }

        It "Should be empty when select string does not return a result when the quiet switch is used" {
            $testinputtwo | Select-String -Quiet "goodbye"  | Should -BeNullOrEmpty
        }

        It "Should return an array of non matching strings when the switch of NotMatch is used and the string do not match" {
            $testinputone | Select-String -Pattern "goodbye" -NotMatch | Should -BeExactly "hello", "Hello"
        }

        It "Should output a string with the first match highlighted" {
            if ($Host.UI.SupportsVirtualTerminal -and !(Test-Path env:__SuppressAnsiEscapeSequences))
            {
                $result = $testinputone | Select-String -Pattern "l" | Out-String
                $result | Should -Be "${nl}he`e[7ml`e[0mlo${nl}He`e[7ml`e[0mlo${nl}${nl}"
            }
            else
            {
                $result = $testinputone | Select-String -Pattern "l" | Out-String
                $result | Should -Be "${nl}hello${nl}Hello${nl}${nl}"
            }
        }

        It "Should output a string with all matches highlighted when AllMatch is used" {
            if ($Host.UI.SupportsVirtualTerminal -and !(Test-Path env:__SuppressAnsiEscapeSequences))
            {
                $result = $testinputone | Select-String -Pattern "l" -AllMatch | Out-String
                $result | Should -Be "${nl}he`e[7ml`e[0m`e[7ml`e[0mo${nl}He`e[7ml`e[0m`e[7ml`e[0mo${nl}${nl}"
            }
            else
            {
                $result = $testinputone | Select-String -Pattern "l" -AllMatch | Out-String
                $result | Should -Be "${nl}hello${nl}Hello${nl}${nl}"
            }
        }

        It "Should output a string with the first match highlighted when SimpleMatch is used" {
            if ($Host.UI.SupportsVirtualTerminal -and !(Test-Path env:__SuppressAnsiEscapeSequences))
            {
                $result = $testinputone | Select-String -Pattern "l" -SimpleMatch | Out-String
                $result | Should -Be "${nl}he`e[7ml`e[0mlo${nl}He`e[7ml`e[0mlo${nl}${nl}"
            }
            else
            {
                $result = $testinputone | Select-String -Pattern "l" -SimpleMatch | Out-String
                $result | Should -Be "${nl}hello${nl}Hello${nl}${nl}"
            }
        }

        It "Should output a string without highlighting when NoEmphasis is used" {
            $result = $testinputone | Select-String -Pattern "l" -NoEmphasis | Out-String
            $result | Should -Be "${nl}hello${nl}Hello${nl}${nl}"
        }

        It "Should return an array of matching strings without virtual terminal sequences" {
            $testinputone | Select-String -Pattern "l" | Should -Be "hello", "hello"
        }

        It "Should return a string type when -Raw is used" {
            $result = $testinputtwo | Select-String -Pattern "hello" -CaseSensitive -Raw
            $result | Should -BeOfType System.String
        }

        It "Should return ParameterBindingException when -Raw and -Quiet are used together" {
            { $testinputone | Select-String -Pattern "hello" -Raw -Quiet -ErrorAction Stop } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
    }

    Context "Filesystem actions" {
        $testDirectory = $TestDrive
        $testInputFile = Join-Path -Path $testDirectory -ChildPath testfile1.txt

        BeforeEach {
            New-Item $testInputFile -ItemType "file" -Force -Value "This is a text string, and another string${nl}This is the second line${nl}This is the third line${nl}This is the fourth line${nl}No matches"
        }

        AfterEach {
            Remove-Item $testInputFile -Force
        }

        It "Should return an object when a match is found is the file on only one line" {
            $result = Select-String $testInputFile -Pattern "string"
            ,$result | Should -BeOfType System.Object
        }

        It "Should return an array when a match is found is the file on several lines" {
            $result = Select-String $testInputFile -Pattern "in"
            ,$result | Should -BeOfType System.Array
            $result[0] | Should -BeOfType Microsoft.PowerShell.Commands.MatchInfo
        }

        It "Should return the name of the file and the string that 'string' is found if there is only one lines that has a match" {
            $expected = $testInputFile + ":1:This is a text string, and another string"

            Select-String $testInputFile -Pattern "string" | Should -BeExactly $expected
        }

        It "Should return all strings where 'second' is found in testfile1 if there is only one lines that has a match" {
            $expected = $testInputFile + ":2:This is the second line"

            Select-String $testInputFile  -Pattern "second" | Should -BeExactly $expected
        }

        It "Should return all strings where 'in' is found in testfile1 pattern switch is not required" {
            $expected1 = "This is a text string, and another string"
            $expected2 = "This is the second line"
            $expected3 = "This is the third line"
            $expected4 = "This is the fourth line"

            (Select-String in $testInputFile)[0].Line | Should -BeExactly $expected1
            (Select-String in $testInputFile)[1].Line | Should -BeExactly $expected2
            (Select-String in $testInputFile)[2].Line | Should -BeExactly $expected3
            (Select-String in $testInputFile)[3].Line | Should -BeExactly $expected4
            (Select-String in $testInputFile)[4].Line | Should -BeNullOrEmpty
        }

        It "Should return empty because 'for' is not found in testfile1 " {
            Select-String for $testInputFile | Should -BeNullOrEmpty
        }

        It "Should return the third line in testfile1 and the lines above and below it " {
            $expectedLine       = "testfile1.txt:2:This is the second line"
            $expectedLineBefore = "testfile1.txt:3:This is the third line"
            $expectedLineAfter  = "testfile1.txt:4:This is the fourth line"

            Select-String third $testInputFile -Context 1 | Should -Match $expectedLine
            Select-String third $testInputFile -Context 1 | Should -Match $expectedLineBefore
            Select-String third $testInputFile -Context 1 | Should -Match $expectedLineAfter
        }

        It "Should return the number of matches for 'is' in textfile1 " {
            (Select-String is $testInputFile -CaseSensitive).count | Should -Be 4
        }

        It "Should return the third line in testfile1 when a relative path is used" {
            $expected  = "testfile1.txt:3:This is the third line"

            $relativePath = Join-Path -Path $testDirectory -ChildPath ".."
            $relativePath = Join-Path -Path $relativePath -ChildPath $TestDirectory.Name
            $relativePath = Join-Path -Path $relativePath -ChildPath testfile1.txt
            Select-String third $relativePath  | Should -Match $expected
        }

        It "Should return the fourth line in testfile1 when a relative path is used" {
            $expected = "testfile1.txt:5:No matches"

            Push-Location $testDirectory

            Select-String matches (Join-Path -Path $testDirectory -ChildPath testfile1.txt)  | Should -Match $expected
            Pop-Location
        }

        It "Should return the fourth line in testfile1 when a regular expression is used" {
            $expected  = "testfile1.txt:5:No matches"

            Select-String 'matc*' $testInputFile -CaseSensitive | Should -Match $expected
        }

        It "Should return the fourth line in testfile1 when a regular expression is used, using the alias for casesensitive" {
            $expected  = "testfile1.txt:5:No matches"

            Select-String 'matc*' $testInputFile -ca | Should -Match $expected
        }

        It "Should return all strings where 'in' is found in testfile1, when -Raw is used." {
            $expected1 = "This is a text string, and another string"
            $expected2 = "This is the second line"
            $expected3 = "This is the third line"
            $expected4 = "This is the fourth line"

            (Select-String in $testInputFile -Raw)[0] | Should -BeExactly $expected1
            (Select-String in $testInputFile -Raw)[1] | Should -BeExactly $expected2
            (Select-String in $testInputFile -Raw)[2] | Should -BeExactly $expected3
            (Select-String in $testInputFile -Raw)[3] | Should -BeExactly $expected4
            (Select-String in $testInputFile -Raw)[4] | Should -BeNullOrEmpty
        }

        It "Should ignore -Context parameter when -Raw is used." {
            $expected = "This is the second line"
            Select-String second $testInputFile -Raw -Context 2,2 | Should -BeExactly $expected
        }
    }

    Context "Culture parameter" {
        It "Should throw if -Culture parameter is used without -SimpleMatch parameter" {
            { "1" | Select-String -Pattern "hello" -Culture "ru-RU" } | Should -Throw -ErrorId "CannotSpecifyCultureWithoutSimpleMatch,Microsoft.PowerShell.Commands.SelectStringCommand"
        }

        It "Should accept a culture: '<culture>'" -TestCases: @(
            @{ culture = "Ordinal"},
            @{ culture = "Invariant"},
            @{ culture = "Current"},
            @{ culture = "ru-RU"}
        ) {
            param ($culture)
            { "1" | Select-String -Pattern "hello" -Culture $culture -SimpleMatch } | Should -Not -Throw
        }

        It "Should works if -Culture parameter is a culture name: '<culture>'-'<pattern>'-'CaseSensitive:<casesensitive>'" -TestCases: @(
            @{pattern = 'file'; culture = 'tr-TR';       expected = 'file'; casesensitive = $false }
            @{pattern = 'fIle'; culture = 'tr-TR';       expected = $null;  casesensitive = $false }
            @{pattern = 'fIle'; culture = 'tr-TR';       expected = $null;  casesensitive = $true }
            @{pattern = "f`u{0130}le"; culture = 'tr-TR';expected = 'file'; casesensitive = $false }
            @{pattern = 'file'; culture = 'en-US';       expected = 'file'; casesensitive = $false }
            @{pattern = 'fIle'; culture = 'en-US';       expected = 'file'; casesensitive = $false }
            @{pattern = 'fIle'; culture = 'en-US';       expected = $null;  casesensitive = $true }
            @{pattern = 'file'; culture = 'Ordinal';     expected = 'file'; casesensitive = $false }
            @{pattern = 'fIle'; culture = 'Ordinal';     expected = 'file'; casesensitive = $false }
            @{pattern = 'fIle'; culture = 'Ordinal';     expected = $null;  casesensitive = $true }
            @{pattern = 'file'; culture = 'Invariant';   expected = 'file'; casesensitive = $false }
            @{pattern = 'fIle'; culture = 'Invariant';   expected = 'file'; casesensitive = $false }
            @{pattern = 'fIle'; culture = 'Invariant';   expected = $null;  casesensitive = $true }
            @{pattern = 'file'; culture = 'Current';     expected = 'file'; casesensitive = $false }
            @{pattern = 'fIle'; culture = 'Current';     expected = 'file'; casesensitive = $false }
            @{pattern = 'fIle'; culture = 'Current';     expected = $null;  casesensitive = $true }
        ) {
            param ($pattern, $culture, $expected, $casesensitive)

            if ($culture -ne 'Current' -or [CultureInfo]::CurrentCulture.Name -ne "tr-TR") {
                'file' | Select-String -Pattern $pattern -Culture $culture -SimpleMatch -CaseSensitive:$casesensitive | Should -BeExactly $expected
            }
       }
    }
}
