Describe "Write-Output" {
    $testString = $testString
    Context "Input Tests" {
        It "Should allow piped input" {
            { $testString | Write-Output } | Should Not Throw
        }

        It "Should write output to the output stream when using piped input" {
            $testString | Write-Output | Should Be $testString
        }

        It "Should use inputobject switch" {
            { Write-Output -InputObject $testString } | Should Not Throw
        }

        It "Should write output to the output stream when using inputobject switch" {
             Write-Output -InputObject $testString | Should Be $testString
        }

        It "Should be able to write to a variable" {
            Write-Output -InputObject $testString -OutVariable var
            $var | Should Be $testString
        }
    }

    Context "Pipeline Command Tests" {
        It "Should send object to the next command in the pipeline" {
            Write-Output -InputObject (1+1) | Should Be 2
        }

        It "Should have the same result between inputobject switch and piped input" {
            Write-Output -InputObject (1+1) | Should Be 2

            1+1 | Write-Output | Should Be 2
        }
    }

    Context "Alias Tests" {
        It "Should have the same result between the echo alias and the cmdlet" {
            $alias  = echo -InputObject $testString
            $cmdlet = Write-Output -InputObject $testString

            $alias | Should Be $cmdlet
        }

        It "Should have the same result between the write alias and the cmdlet" {
            $alias  = write -InputObject $testString
            $cmdlet = Write-Output -InputObject $testString

            $alias | Should Be $cmdlet
        }
    }

    Context "Enumerate Objects" {
        $enumerationObject = @(1,2,3)
        It "Should see individual objects when not using the NoEnumerate switch" {
            $singleCollection = $(Write-Output $enumerationObject| Measure-Object).Count

            $singleCollection | Should Be $enumerationObject.length
        }

        It "Should be able to treat a collection as a single object using the NoEnumerate switch" {
            $singleCollection = $(Write-Output $enumerationObject -NoEnumerate | Measure-Object).Count

            $singleCollection | Should Be 1
        }
    }
}
