Describe "Compare-Object" {
    # First ensure the environment is set up
    if (Test-Path "/tmp")
    {
        $testDirectory = "/tmp/testDirectory"
        $nl = "`n"
        $slash = "/"

    }
    else
    {
        $testDirectory = "C:\Users\v-zafolw\testdirectory"
        $nl = "`r`n"
        $slash = "\\"
    }

    $dir = $testDirectory
    New-Item $testDirectory -ItemType directory -Force

    $content1 = "line 1" + $nl + "line 2"
    $content2 = "line 1" + $nl + "line 2.1"
    $content3 = "line 1" + $nl + "line 2" + $nl + "line 3"
    $content4 = "line 1" + $nl + "line 2.1" + $nl + "Line 3"

    $file1 = $testDirectory + $slash + "test1.txt"
    $file2 = $testDirectory + $slash + "test2.txt"
    $file3 = $testDirectory + $slash + "test3.txt"
    $file4 = $testDirectory + $slash + "test4.txt"

    New-Item $file1 -ItemType file -Value $content1 -Force
    New-Item $file2 -ItemType file -Value $content2 -Force
    New-Item $file3 -ItemType file -Value $content3 -Force
    New-Item $file4 -ItemType file -Value $content4 -Force

    Test-Path $testDirectory | Should Be $true

    It "Should be able to compare the same object using the referenceObject and differenceObject switches" {
       { Compare-Object -ReferenceObject $(Get-Content $file1) -DifferenceObject $(Get-Content $file2) } | Should Not Throw
    }

    It "Should not throw when referenceobject switch is not used" {
        { Compare-Object $(Get-Content $file1) -DifferenceObject $(Get-Content $file2) } | Should Not Throw
    }

    It "Should not throw when differenceobject switch is not used" {
        { Compare-Object -ReferenceObject $(Get-Content $file1) $(Get-Content $file2) } | Should Not Throw
    }

    It "Should be able to execute compare object using the compare alias" {
        { compare -ReferenceObject $(Get-Content $file1) -DifferenceObject $(Get-Content $file2) } | Should Not Throw
    }

    It "Should produce the same output when the compare alias is used" {
        $alias    = compare -ReferenceObject $(Get-Content $file1) -DifferenceObject $(Get-Content $file2)
        $fullname = Compare-Object -ReferenceObject $(Get-Content $file1) -DifferenceObject $(Get-Content $file2)

        $alias[0].InputObject   | Should Be $fullname[0].InputObject
        $alias[0].SideIndicator | Should Be $fullname[0].SideIndicator
        $alias[1].InputObject   | Should Be $fullname[1].InputObject
        $alias[1].SideIndicator | Should Be $fullname[1].SideIndicator

        $alias.Length | Should Be 2 # There should be no other elements to test

    }

    It "Should be able to execute compare object using the diff alias" {
        $alias    = diff -ReferenceObject $(Get-Content $file1) -DifferenceObject $(Get-Content $file2)
        $fullname = Compare-Object -ReferenceObject $(Get-Content $file1) -DifferenceObject $(Get-Content $file2)

        $alias[0].InputObject   | Should Be $fullname[0].InputObject
        $alias[0].SideIndicator | Should Be $fullname[0].SideIndicator
        $alias[1].InputObject   | Should Be $fullname[1].InputObject
        $alias[1].SideIndicator | Should Be $fullname[1].SideIndicator

        $alias.Length | Should Be 2 # There should be no other elements to test
    }

    It "Should indicate data that exists only in the reference dataset" {
        $actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) 

        $actualOutput[1].SideIndicator | Should Be "<="
    }

    It "Should indicate data that exists only in the difference dataset" {
        $actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) 

        $actualOutput[1].SideIndicator | Should Be "<="
    }

    It "Should indicate data that exists in both datasets when the includeEqual switch is used" {
        $actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -IncludeEqual

        $actualOutput.Length           | Should Be 4
        $actualOutput[0].SideIndicator | Should Be "=="
        $actualOutput[1].SideIndicator | Should Be "=="
    }

    It "Should be able to use the casesensitive switch" {
        { Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -CaseSensitive } | Should Not Throw
    }

    It "Should correctly indicate that different cases are different when the casesensitive switch is used" {
        $caOutput  = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -CaseSensitive
        $ncaOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) 

        $caOutput.Length | Should Be 4

        $ncaOutput[1].SideIndicator | Should Not Be $caOutput[1].SideIndicator
        $ncaOutput[2].SideIndicator | Should Not Be $caOutput[2].SideIndicator
        $ncaOutput[3].SideIndicator | Should Not Be $caOutput[3].SideIndicator

    }

    It "Should throw when reference set is null" {
        { Compare-Object -ReferenceObject $anonexistentvariable -DifferenceObject $(Get-Content $file4) } | Should Throw
    }

    It "Should throw when difference set is null" {
        { Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $anonexistentvariable } | Should Throw
    }

    It "Should give a 0 array when using excludedifferent switch without also using the includeequal switch" {
        $actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -ExcludeDifferent

        $actualOutput.Length | Should Be 0
    }

    It "Should only display equal lines when excludeDifferent switch is used alongside the includeequal switch" {
        $actualOutput = Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -IncludeEqual -ExcludeDifferent

        $actualOutput.Length | Should Be 2
    }

    It "Should be able to pass objects to pipeline using the passthru switch" {
        { Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -Passthru | Format-Wide } | Should Not Throw
    }

    It "Should be able to specify the property of two objects to compare" {
        $actualOutput = Compare-Object -ReferenceObject $file3 -DifferenceObject $testDirectory -Property Length

        $actualOutput[0].Length | Should BeGreaterThan 0
        $actualOutput[1].Length | Should BeGreaterThan 0
        $actualOutput[0].Length | Should Not Be $actualOutput[1].Length
    }

    It "Should be able to specify the syncwindow without error" {
        { Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -syncWindow 5 } | Should Not Throw
        { Compare-Object -ReferenceObject $(Get-Content $file3) -DifferenceObject $(Get-Content $file4) -syncWindow 8 } | Should Not Throw
    }

    It "Should have the expected output when changing the syncwindow" {
        $var1 = 1..15
        $var2 = 15..1

        $actualOutput = Compare-Object -ReferenceObject $var1 -DifferenceObject $var2 -syncWindow 6

        $actualOutput[0].InputObject | Should Be 15
        $actualOutput[1].InputObject | Should Be 1
        $actualOutput[2].InputObject | Should Be 1
        $actualOutput[3].InputObject | Should Be 15

        $actualOutput[0].SideIndicator | Should be "=>"
        $actualOutput[1].SideIndicator | Should be "<="
        $actualOutput[2].SideIndicator | Should be "=>"
        $actualOutput[3].SideIndicator | Should be "<="
    }

# Clean up after yourself
    Remove-Item $testDirectory -Recurse -Force
    Test-Path $testDirectory | Should Be $false
}
