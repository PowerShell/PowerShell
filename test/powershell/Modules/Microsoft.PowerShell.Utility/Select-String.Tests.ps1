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

    Context "Many files" {
        BeforeAll {
            $manyDirectory = Join-Path $TestDrive 'many'
            $null = New-Item -ItemType Directory -Path $manyDirectory
            $expectedMatches = [System.Collections.Generic.List[string]]::new()
            for ($i = 0; $i -lt 300; $i++) {
                $name = 'file{0:D3}.txt' -f $i
                if ($i % 7 -eq 0) {
                    $lines = 'nothing here', 'or here'
                }
                else {
                    $lines = 'first line', "match $i", 'other', "match again $i"
                    $expectedMatches.Add("${name}:2:match $i")
                    $expectedMatches.Add("${name}:4:match again $i")
                }

                Set-Content -Path (Join-Path $manyDirectory $name) -Value $lines
            }

            $manyFiles = Get-ChildItem -Path $manyDirectory -File | Sort-Object -Property Name
        }

        It "Should take -Context lines from results as changed downstream" {
            $empty = Join-Path $TestDrive 'context-empty.txt'
            $data = Join-Path $TestDrive 'context-data.txt'
            Set-Content -LiteralPath $empty -Value @() -NoNewline
            Set-Content -LiteralPath $data -Value 'pad', 'hit1', 'hit2'
            $actual = Select-String -LiteralPath $empty, $data -Pattern '^hit' -Context 1, 0 | ForEach-Object {
                if ($_.Line -eq 'hit1') { $_.Line = 'edited' }
                else { $_.Context.PreContext[0] }
            }

            $actual | Should -BeExactly 'edited'
        }

        It "Should return the matches of many files in input order: <name>" -TestCases @(
            @{ name = 'piped one at a time' }
            @{ name = 'named together in -LiteralPath' }
        ) {
            param ($name)

            $actual = if ($name -eq 'piped one at a time') {
                $manyFiles | Select-String -Pattern 'match'
            }
            else {
                Select-String -LiteralPath $manyFiles.FullName -Pattern 'match'
            }

            $actual = $actual | ForEach-Object { '{0}:{1}:{2}' -f $_.Filename, $_.LineNumber, $_.Line }
            $actual -join "`n" | Should -BeExactly ($expectedMatches -join "`n")
        }

        It "Should leave out files by -Include in input order when named together" {
            $actual = Select-String -LiteralPath $manyFiles.FullName -Pattern 'match' -Include '*file00*' | ForEach-Object { $_.Filename } | Select-Object -Unique
            $actual | Should -Be @('file001.txt', 'file002.txt', 'file003.txt', 'file004.txt', 'file005.txt', 'file006.txt', 'file008.txt', 'file009.txt')
        }

        It "Should not raise an -Include error for a file after the -Quiet answer" {
            $bin = Join-Path $TestDrive 'later.bin'
            Set-Content -Path $bin -Value 'match'
            Select-String -LiteralPath $manyFiles[1].FullName, $bin -Pattern 'match' -Include '*.txt', '[' -Quiet | Should -BeExactly $true
        }

        It "Should skip a folder before applying -Include: <name>" -TestCases @(
            @{ name = 'the folder alone' }
            @{ name = 'with a file named together' }
        ) {
            param ($name)

            if ($name -eq 'the folder alone') {
                Select-String -Path $manyDirectory -Pattern 'match' -Include '[' -Quiet | Should -BeExactly $false
            }
            else {
                Select-String -Path $manyDirectory, $manyFiles[1].FullName -Pattern 'match' -Include '*.txt', '[' -Quiet | Should -BeExactly $true
            }
        }

        It "Should close each file before writing its -List result" {
            $copy = Join-Path $TestDrive 'listcopy'
            Copy-Item -Path $manyDirectory -Destination $copy -Recurse
            $paths = (Get-ChildItem $copy -File | Sort-Object Name | Select-Object -First 20).FullName
            { Select-String -LiteralPath $paths -Pattern 'match' -List | Remove-Item -ErrorAction Stop } | Should -Not -Throw
            (Get-ChildItem $copy -File).Count | Should -Be ((Get-ChildItem $manyDirectory -File).Count - 17)
        }

        It "Should search an empty file among files named together" {
            $empty = Join-Path $TestDrive 'empty.txt'
            Set-Content -Path $empty -Value $null -NoNewline
            $actual = Select-String -LiteralPath $empty, $manyFiles[1].FullName -Pattern 'match' | ForEach-Object { $_.Filename }
            $actual | Should -Be @('file001.txt', 'file001.txt')
        }

        It "Should return one -Quiet result per file, in input order" {
            $actual = $manyFiles[0..3] | Select-String -Pattern 'match' -Quiet
            $actual | Should -Be @($false, $true, $true, $true)
        }

        It "Should return the first match of each file with -List: <name>" -TestCases @(
            @{ name = 'piped one at a time' }
            @{ name = 'named together in -LiteralPath' }
        ) {
            param ($name)

            $actual = if ($name -eq 'piped one at a time') {
                $manyFiles | Select-String -Pattern 'match' -List
            }
            else {
                Select-String -LiteralPath $manyFiles.FullName -Pattern 'match' -List
            }

            $actual = $actual | ForEach-Object { '{0}:{1}' -f $_.Filename, $_.LineNumber }
            $actual.Count | Should -Be ($expectedMatches.Count / 2)
            $actual[0] | Should -BeExactly 'file001.txt:2'
        }

        It "Should write every match found before the command feeding it fails" {
            $written = [System.Collections.Generic.List[object]]::new()
            {
                & { $manyFiles[1..5]; throw 'upstream stopped' } | Select-String -Pattern 'match' | ForEach-Object { $written.Add($_) }
            } | Should -Throw 'upstream stopped'
            $written.Count | Should -Be 10
        }

        It "Should give the same results for files named together in -Path when they hold too many matches" {
            $paths = $manyFiles.FullName
            $normal = Select-String -Path $paths -Pattern 'match' | ForEach-Object { '{0}:{1}' -f $_.Filename, $_.LineNumber }
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringMaxHeldMatchesPerRecord', 7)
            try {
                $limited = Select-String -Path $paths -Pattern 'match' | ForEach-Object { '{0}:{1}' -f $_.Filename, $_.LineNumber }
            }
            finally {
                [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringMaxHeldMatchesPerRecord', 0)
            }

            $normal.Count | Should -Be $expectedMatches.Count
            ($limited -join "`n") | Should -BeExactly ($normal -join "`n")
        }

        It "Should return one -Quiet result for files named together in -Path" {
            Select-String -Path $manyFiles.FullName -Pattern 'match' -Quiet | Should -BeExactly $true
            Select-String -Path $manyFiles.FullName -Pattern 'not present anywhere' -Quiet | Should -BeExactly $false
        }

        It "Should stop when a later command needs no more input: <name>" -TestCases @(
            @{ name = 'piped one at a time' }
            @{ name = 'named together in -LiteralPath' }
        ) {
            param ($name)

            $actual = if ($name -eq 'piped one at a time') {
                $manyFiles | Select-String -Pattern 'match' | Select-Object -First 1
            }
            else {
                Select-String -LiteralPath $manyFiles.FullName -Pattern 'match' | Select-Object -First 1
            }

            '{0}:{1}' -f $actual.Filename, $actual.LineNumber | Should -BeExactly 'file001.txt:2'

            # Every search has finished and closed its file: the files can be deleted at once.
            $copy = Join-Path $TestDrive 'stopcopy'
            Copy-Item -Path $manyDirectory -Destination $copy -Recurse
            Select-String -LiteralPath (Get-ChildItem $copy -File).FullName -Pattern 'match' | Select-Object -First 1 | Out-Null
            { Remove-Item -Path $copy -Recurse -ErrorAction Stop } | Should -Not -Throw
        }
    }

    Context "Line ends and long lines" {
        BeforeAll {
            $lineEndsFile = Join-Path $TestDrive 'lineends.txt'
            [System.IO.File]::WriteAllText($lineEndsFile, "a needle`r`nb`rneedle c`nneedle`r`r`nlast needle")
            $longLineFile = Join-Path $TestDrive 'longline.txt'
            [System.IO.File]::WriteAllText($longLineFile, "short`n" + ('x' * 100000) + "needle`nend")
        }

        It "Should count lines ended by CR, LF and CRLF as ReadLine does" {
            $actual = Select-String -Path $lineEndsFile -Pattern 'needle'
            $actual.LineNumber | Should -Be @(1, 3, 4, 6)
            $actual.Line | Should -Be @('a needle', 'needle c', 'needle', 'last needle')
        }

        It "Should return the non-matching lines, including an empty one, with -NotMatch" {
            $actual = Select-String -Path $lineEndsFile -Pattern 'needle' -NotMatch
            $actual.LineNumber | Should -Be @(2, 5)
            $actual.Line | Should -Be @('b', '')
        }

        It "Should give the same results when a file has too many matches to hold: <name>" -TestCases @(
            @{ name = 'matches';       arguments = @{ Pattern = '\d' } }
            @{ name = 'context lines'; arguments = @{ Pattern = '\d'; Context = 1, 2 } }
            @{ name = 'not match';     arguments = @{ Pattern = '7'; NotMatch = $true } }
        ) {
            param ($arguments)

            # Two files in one -LiteralPath, so they are searched on worker threads and their matches held.
            $manyMatchesFile = Join-Path $TestDrive 'manymatches.txt'
            Set-Content -Path $manyMatchesFile -Value (1..500 | ForEach-Object { "line $_" })
            $otherFile = Join-Path $TestDrive 'manymatches2.txt'
            Set-Content -Path $otherFile -Value (1..300 | ForEach-Object { "other $_" })
            $paths = $manyMatchesFile, $otherFile
            $describe = { $input | ForEach-Object { '{0}|{1}|{2}|{3}' -f $_.Filename, $_.LineNumber, $_.Line, ($_.Context.PreContext -join ',') } }

            $normal = Select-String -LiteralPath $paths @arguments | & $describe
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringMaxHeldMatches', 5)
            try {
                $searchedAgain = Select-String -LiteralPath $paths @arguments | & $describe
            }
            finally {
                [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringMaxHeldMatches', 0)
            }

            $normal.Count | Should -BeGreaterThan 5
            ($searchedAgain -join "`n") | Should -BeExactly ($normal -join "`n")
        }

        It "Should write the matches before an invalid byte with a strict encoding" {
            $strictFile = Join-Path $TestDrive 'strict.txt'
            $bytes = [System.Collections.Generic.List[byte]]::new()
            $bytes.AddRange([System.Text.Encoding]::ASCII.GetBytes("hit`n" + ('a' * 4091)))
            $bytes.Add(0xFF)
            [System.IO.File]::WriteAllBytes($strictFile, $bytes.ToArray())
            $strict = [System.Text.UTF8Encoding]::new($false, $true)
            Select-String -LiteralPath $strictFile -Pattern 'hit' -Quiet -Encoding $strict | Should -BeExactly $true
        }

        It "Should return a line longer than the read buffer whole" {
            $actual = Select-String -Path $longLineFile -Pattern 'needle'
            $actual.LineNumber | Should -Be 2
            $actual.Line.Length | Should -Be 100006
        }
    }

    Context "Large files searched in chunks" {
        BeforeAll {
            # Lines ended by LF, CRLF, CR and CR CR LF, empty lines, invalid UTF-8, characters of 2 to 4 bytes,
            # a line longer than a chunk, a UTF-8 byte-order mark and no final line end.
            $bytes = [System.Collections.Generic.List[byte]]::new()
            $bytes.AddRange([byte[]](0xEF, 0xBB, 0xBF))
            $words = 'alpha', 'needle', 'Needle', "caf`u{00E9}", "`u{65E5}`u{672C}", "`u{1F600}", 'beta'
            $ends = "`n", "`r`n", "`r", "`r`r`n", "`n`n"
            for ($i = 0; $i -lt 2000; $i++) {
                $line = '{0} {1} {2}' -f $i, $words[$i % $words.Count], $words[($i * 3) % $words.Count]
                $bytes.AddRange([System.Text.Encoding]::UTF8.GetBytes($line))
                if ($i % 97 -eq 0) {
                    $bytes.AddRange([byte[]](0xFF, 0x80, 0x20, 0x6E, 0x65, 0x65, 0x64, 0x6C, 0x65, 0xE2, 0x82))
                }

                if ($i -eq 1000) {
                    $bytes.AddRange([System.Text.Encoding]::UTF8.GetBytes((' long' * 400) + ' needle'))
                }

                $bytes.AddRange([System.Text.Encoding]::UTF8.GetBytes($ends[$i % $ends.Count]))
            }

            $bytes.AddRange([System.Text.Encoding]::UTF8.GetBytes('last needle without a line end'))
            $chunkedFile = Join-Path $TestDrive 'chunked.txt'
            [System.IO.File]::WriteAllBytes($chunkedFile, $bytes.ToArray())

            function Get-Description($result) {
                $result | ForEach-Object {
                    if ($_ -is [Microsoft.PowerShell.Commands.MatchInfo]) {
                        '{0}|{1}|{2}|{3}' -f $_.Filename, $_.LineNumber, $_.Line, (($_.Matches | ForEach-Object { "$($_.Index),$($_.Length)" }) -join ';')
                    }
                    else {
                        "$_"
                    }
                }
            }

            function Get-ChunksStarted {
                $chunkedType = [Microsoft.PowerShell.Commands.MatchInfo].Assembly.GetType('Microsoft.PowerShell.Commands.ChunkedFile')
                $chunkedType.GetProperty('ChunksStarted', [System.Reflection.BindingFlags]'Static, NonPublic').GetValue($null)
            }

            function Set-SplitHooks([long] $minLength, [int] $chunkLength, [int] $maxHeld) {
                [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringSplitMinLength', $minLength)
                [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringChunkLength', $chunkLength)
                [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringMaxHeldMatches', $maxHeld)
            }
        }

        AfterAll {
            Set-SplitHooks 0 0 0
        }

        It "Should search a large file in chunks only while the test hooks are set" {
            $before = Get-ChunksStarted
            $null = Select-String -Path $chunkedFile -Pattern 'needle'
            Get-ChunksStarted | Should -Be $before

            Set-SplitHooks 1024 256 0
            try {
                $null = Select-String -Path $chunkedFile -Pattern 'needle'
            }
            finally {
                Set-SplitHooks 0 0 0
            }

            (Get-ChunksStarted) - $before | Should -BeGreaterThan 10
        }

        It "Should not split a large file that starts with a UTF-16 byte-order mark" {
            $utf16File = Join-Path $TestDrive 'utf16.txt'
            [System.IO.File]::WriteAllText($utf16File, ((1..200 | ForEach-Object { "line $_ needle" }) -join "`n"), [System.Text.UnicodeEncoding]::new($false, $true))
            $onePass = Get-Description (Select-String -Path $utf16File -Pattern 'needle')
            $before = Get-ChunksStarted
            Set-SplitHooks 1024 256 0
            try {
                $split = Get-Description (Select-String -Path $utf16File -Pattern 'needle')
            }
            finally {
                Set-SplitHooks 0 0 0
            }

            Get-ChunksStarted | Should -Be $before
            ($split -join "`n") | Should -BeExactly ($onePass -join "`n")
        }

        It "Should give the same results in chunks as in one pass: <name>" -TestCases @(
            @{ name = 'any case';         arguments = @{ Pattern = 'needle' } }
            @{ name = 'case-sensitive';   arguments = @{ Pattern = 'Needle'; CaseSensitive = $true } }
            @{ name = 'simple match';     arguments = @{ Pattern = "caf`u{00E9}"; SimpleMatch = $true } }
            @{ name = 'regex';            arguments = @{ Pattern = '^\d+ \S+ beta$' } }
            @{ name = 'all matches';      arguments = @{ Pattern = 'needle'; AllMatches = $true } }
            @{ name = 'not match';        arguments = @{ Pattern = 'a'; NotMatch = $true } }
            @{ name = 'list';             arguments = @{ Pattern = 'beta'; List = $true } }
            @{ name = 'quiet';            arguments = @{ Pattern = "`u{1F600}"; Quiet = $true } }
            @{ name = 'raw';              arguments = @{ Pattern = 'needle'; Raw = $true } }
            @{ name = 'last line';        arguments = @{ Pattern = 'without a line end' } }
        ) {
            param ($arguments)

            Set-SplitHooks 0 0 0
            $onePass = Get-Description (Select-String -Path $chunkedFile @arguments)
            $before = Get-ChunksStarted
            Set-SplitHooks 1024 256 0
            try {
                $chunked = Get-Description (Select-String -Path $chunkedFile @arguments)
            }
            finally {
                Set-SplitHooks 0 0 0
            }

            # -List and -Quiet stop at the first match and read in one pass; everything else is split.
            $expectSplit = -not ($arguments.List -or $arguments.Quiet)
            ((Get-ChunksStarted) - $before -gt 0) | Should -Be $expectSplit
            $onePass | Should -Not -BeNullOrEmpty
            ($chunked -join "`n") | Should -BeExactly ($onePass -join "`n")
        }

        It "Should read a large file in one pass with -List and -Quiet: <name>" -TestCases @(
            @{ name = 'list';  arguments = @{ Pattern = '\d'; List = $true } }
            @{ name = 'quiet'; arguments = @{ Pattern = '\d'; Quiet = $true } }
        ) {
            param ($arguments)

            $onePass = Get-Description (Select-String -Path $chunkedFile @arguments)
            $before = Get-ChunksStarted
            Set-SplitHooks 1024 256 3
            try {
                $split = Get-Description (Select-String -Path $chunkedFile @arguments)
            }
            finally {
                Set-SplitHooks 0 0 0
            }

            Get-ChunksStarted | Should -Be $before
            @($split).Count | Should -Be 1
            ($split -join "`n") | Should -BeExactly ($onePass -join "`n")
        }

        It "Should close a large file before writing its -List result" {
            $copy = Join-Path $TestDrive 'chunked-delete.txt'
            Copy-Item -Path $chunkedFile -Destination $copy
            Set-SplitHooks 1024 256 0
            try {
                { Select-String -LiteralPath $copy -Pattern 'needle' -List | Remove-Item -ErrorAction Stop } | Should -Not -Throw
            }
            finally {
                Set-SplitHooks 0 0 0
            }

            Test-Path $copy | Should -BeFalse
        }

        It "Should give the same results when large files hold too many matches together" {
            $second = Join-Path $TestDrive 'chunked3.txt'
            Copy-Item -Path $chunkedFile -Destination $second
            $onePass = Get-Description (Select-String -LiteralPath $chunkedFile, $second -Pattern '\d')
            $before = Get-ChunksStarted
            Set-SplitHooks 1024 256 0
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringMaxHeldMatchesPerRecord', 50)
            try {
                $split = Get-Description (Select-String -LiteralPath $chunkedFile, $second -Pattern '\d')
            }
            finally {
                Set-SplitHooks 0 0 0
                [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringMaxHeldMatchesPerRecord', 0)
            }

            (Get-ChunksStarted) - $before | Should -BeGreaterThan 10
            ($split -join "`n") | Should -BeExactly ($onePass -join "`n")
        }

        It "Should give the same results when a large file and small files share the match limit" {
            $paths = [System.Collections.Generic.List[string]]::new()
            foreach ($i in 1..3) {
                $small = Join-Path $TestDrive "small$i.txt"
                Set-Content -LiteralPath $small -Value (1..300 | ForEach-Object { "small $i line $_" })
                $paths.Add($small)
            }
            $paths.Insert(1, $chunkedFile)
            $onePass = Get-Description (Select-String -LiteralPath $paths -Pattern '\d')
            $before = Get-ChunksStarted
            Set-SplitHooks 1024 256 0
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringMaxHeldMatchesPerRecord', 50)
            try {
                $split = Get-Description (Select-String -LiteralPath $paths -Pattern '\d')
            }
            finally {
                Set-SplitHooks 0 0 0
                [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('SelectStringMaxHeldMatchesPerRecord', 0)
            }

            (Get-ChunksStarted) - $before | Should -BeGreaterThan 10
            ($split -join "`n") | Should -BeExactly ($onePass -join "`n")
        }

        It "Should give the same results for several large files named together" {
            $second = Join-Path $TestDrive 'chunked2.txt'
            Copy-Item -Path $chunkedFile -Destination $second
            $onePass = Get-Description (Select-String -LiteralPath $chunkedFile, $second -Pattern 'needle')
            Set-SplitHooks 1024 256 5
            try {
                $chunked = Get-Description (Select-String -LiteralPath $chunkedFile, $second -Pattern 'needle')
            }
            finally {
                Set-SplitHooks 0 0 0
            }

            ($chunked -join "`n") | Should -BeExactly ($onePass -join "`n")
        }

        It "Should give the same results when chunks have too many matches to hold" {
            Set-SplitHooks 0 0 0
            $onePass = Get-Description (Select-String -Path $chunkedFile -Pattern '\d')
            Set-SplitHooks 1024 4096 3
            try {
                $chunked = Get-Description (Select-String -Path $chunkedFile -Pattern '\d')
            }
            finally {
                Set-SplitHooks 0 0 0
            }

            $onePass.Count | Should -Be 2000
            ($chunked -join "`n") | Should -BeExactly ($onePass -join "`n")
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
