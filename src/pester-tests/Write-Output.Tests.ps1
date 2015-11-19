Describe "Write-Output" {
    It "Should allow piped input" {
        { "hi" | Write-Output } | Should Not Throw
    }

    It "Should use inputobject switch" {
        { Write-Output -InputObject "hi" } | Should Not Throw
    }

    It "Should be able to write to a variable" {
        Write-Output -InputObject "hi" -OutVariable var
        $var | Should Be "hi"
    }

    It "Should send object to the next command in the pipeline" {
        Write-Output -InputObject (1+1) | Should Be 2
    }
    
    It "Should have the same result between inputobject switch and piped input" {
        Write-Output -InputObject (1+1) | Should Be 2

        1+1 | Write-Output | Should Be 2
    }

    It "Should have the same result between the echo alias and the cmdlet" {
        $alias  = echo -InputObject "hi"
        $cmdlet = Write-Output -InputObject "hi"

        $alias | Should Be $cmdlet
    }

    It "Should have the same result between the write alias and the cmdlet" {
        $alias  = write -InputObject "hi"
        $cmdlet = Write-Output -InputObject "hi"

        $alias | Should Be $cmdlet
    }
    
    It "Should see individual objects when not using the NoEnumerate switch" {
        $singleCollection = $(Write-Output @(1,2,3)| Measure-Object).Count

        $singleCollection | Should Be 3
    }

    It "Should be able to treat a collection as a single object using the NoEnumerate switch" {
        $singleCollection = $(Write-Output @(1,2,3) -NoEnumerate | Measure-Object).Count

        $singleCollection | Should Be 1
    }
}