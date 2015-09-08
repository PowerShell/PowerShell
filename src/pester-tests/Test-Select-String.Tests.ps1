Describe ".\Test-Select-String" {
    Context "String actions" {
    $testInputOne = "Hello","HELLO", "Goodbye"    
    $testInputTwo = "Hello","HELLO"

        It "Should be called with out error" {
            { $testInputOne | Select-String -Pattern "HELLO" } | Should Not Throw
        }

        It "Should be called without error using the sls alias" {
            { $testInputOne | sls -Pattern "HELLO" } | Should Not Throw
        }

        It "Should return an array data type when multiple matches are found" {
            ( $testInputTwo | Select-String -Pattern "HELLO").GetType().BaseType | Should Be Array
        }
    
        It "Should return an array of matches when multiple matches are found" {
              $testInputOne | Select-String -Pattern "HELLO" | Should Be "HELLO", "Hello"
        }

        It "Should return an object type when one match is found" {
        # look into the aliases for the switches.  ca for case-sensitive, n for notmatch, etc
            ( $testInputTwo | Select-String -Pattern "HELLO" -CaseSensitive).GetType().BaseType | Should Be System.Object
        }

        It "Should only return the case sensitive match when the casesensitive switch is used" {
             $testInputTwo | Select-String -Pattern "HELLO" -CaseSensitive | Should Be "HELLO"
        }

        It "Should accept a collection of strings from the input object"{
            { Select-String -InputObject "Some stuff", "Other stuff" -Pattern "Other" } | Should Not Throw
        }

        It "Should return System.Object when the input object switch is used on a collection"{
            ( Select-String -InputObject "Some stuff", "Other stuff" -Pattern "Other" ).GetType().BaseType | Should Be System.Object
        }

        It "Should return null or empty when the input object switch is used on a collection and the pattern does not exist"{
            Select-String -InputObject "Some stuff", "Other stuff" -Pattern "Neither" | Should BeNullOrEmpty 
        }

        It "Should return a bool type when the quiet switch is used"{
            ($testInputTwo | Select-String -Quiet "HELLO" -CaseSensitive).GetType() | Should Be bool
        }

        It "Should be true when select string returns a positive result when the quiet switch is used"{
            ($testInputTwo | Select-String -Quiet "HELLO" -CaseSensitive) | Should Be TRUE
        }

        It "Should be empty when select string does not return a result when the quiet switch is used"{
            $testInputTwo | Select-String -Quiet "Goodbye"  | Should BeNullOrEmpty 
        }

        It "Should return an array of non matching strings when the switch of NotMatch is used and the string do not match"{
            $testInputOne | Select-String -Pattern "Goodbye" -NotMatch | Should Be "HELLO", "Hello"
        }
    }

    Context "Filesytem actions" {
        $testInputFile =  "/tmp/testfile1.txt"
        BeforeEach {
            New-Item $testInputFile -Itemtype "file" -Force -Value "This is a text string, and another string`nThis is the second line`nThis is the third line`nThis is the fourth line`nNo matches"
        }

        It "Should return an object when a match is found is the file on only one line"{
            (Select-String $testInputFile -Pattern "string").GetType().BaseType | Should be System.Object
        }

        It "Should return the name of the file and the string that 'string' is found if there is only one lines that has a match" {
            $expected = $testInputFile + ":1:This is a text string, and another string"

            Select-String $testInputFile -Pattern "string" | Should Be $expected 
        }

        It "Should return all strings where 'second' is found in testfile1 if there is only one lines that has a match" {
            $expected = $testInputFile + ":2:This is the second line"

            Select-String $testInputFile  -Pattern "second"| Should Be $expected
        }

        #this should probably go up near the one that returns 'object' when only a single match is found
        It "Should return an array when a match is found is the file on several lines"{
            (Select-String $testInputFile -Pattern "in").GetType().BaseType | Should be array
        }

        It "Should return all strings where 'in' is found in testfile1 pattern switch is not required" {
            $expected1 = $testInputFile + ":1:This is a text string, and another string"
            $expected2 = $testInputFile + ":2:This is the second line"
            $expected3 = $testInputFile + ":3:This is the third line"
            $expected4 = $testInputFile + ":4:This is the fourth line"

            (Select-String in $testInputFile)[0] | Should Be $expected1
            (Select-String in $testInputFile)[1] | Should Be $expected2
            (Select-String in $testInputFile)[2] | Should Be $expected3
            (Select-String in $testInputFile)[3] | Should Be $expected4
            (Select-String in $testInputFile)[4] | Should BeNullOrEmpty
        }

        It "Should return empty because 'for' is not  found in testfile1 " {
            Select-String for $testInputFile | Should BeNullOrEmpty
        }

        It "Should return the third line in testfile1 and the lines above and below it " {
            $expectedLine       = "testfile1.txt:2:This is the second line"
            $expectedLineBefore = "testfile1.txt:3:This is the third line"
            $expectedLineAfter  = "/tmp/testfile1.txt:4:This is the fourth line"

            Select-String third $testInputFile -Context 1 | Should Match $expectedLine
            Select-String third $testInputFile -Context 1 | Should Match $expectedLineBefore  
            Select-String third $testInputFile -Context 1 | Should Match $expectedLineAfter         
        }

        It "Should return the number of matches for 'is' in textfile1 " {
            (Select-String is $testInputFile -CaseSensitive).count| Should Be 4
        }

        It "Should return the third line in testfile1 when a relative path is used"{
            $expected  = "/tmp/testfile1.txt:3:This is the third line"

            Select-String third /tmp/../tmp/testfile1.txt  | Should Match $expected
        }

        It "Should return the fourth line in testfile1 when a relative path is used"{
            $testDirectory = "/tmp/"
            $expected      = "/tmp/testfile1.txt:5:No matches"

            pushd $testDirectory

            Select-String matches $testDirectory/testfile1.txt  | Should Match $expected
        }

        It "Should return the fourth line in testfile1 when a regular expression is used"{
            $expected  = "/tmp/testfile1.txt:5:No matches"

            Select-String 'matc*' $testInputFile -CaseSensitive | Should Match $expected
        }

        It "Should return the fourth line in testfile1 when a regular expression is used, using the alias for casesensitive"{
            $expected  = "/tmp/testfile1.txt:5:No matches"

            Select-String 'matc*' $testInputFile -ca | Should Match $expected
        }
    }
}