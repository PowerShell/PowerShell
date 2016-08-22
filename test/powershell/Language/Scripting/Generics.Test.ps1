Describe "Generics support" -Tags "CI" {
    #
    # 3 types are tested, list, stack, and dictionary.
    #
    # list and stack are in different assemblies, and dictionary
    # takes more than one type parameter.
    #
    $listType = "system.collections.generic.list"
    $stackType = "system.collections.generic.stack"
    $dictionaryType = "system.collections.generic.dictionary"

    It 'list[Int]' {
        $x = new-object "$listType[int]"
        $x.Add(42)
        $x.count | Should Be 1
    }

    It 'stack[Int]' {
        $x = new-object "$stackType[int]"
        $x.Push(42)
        $x.count | Should Be 1
    }

    It 'dictionary[string, Int]' {
        $x = new-object "$dictionaryType[string, int]"
        $x.foo = 42
        $x.foo | Should Be 42
    }

    It 'list[[Int]]' {
        $x = new-object "$listType[[int]]"
        $x.Add(42)
        $x.count | Should Be 1
    }

    It 'stack[[Int]]' {
        $x = new-object "$stackType[[int]]"
        $x.Push(42)
        $x.count | Should Be 1
    }

    It 'dictionary[[string], [Int]]' {
        $x = new-object "$dictionaryType[[string] , [int]]"
        $x.foo = 42
        $x.foo | Should Be 42
    }

    It 'dictionaryType[dictionary[list[int],string], stack[double]]' {
        $x = new-object "$dictionaryType[$dictionaryType[$listType[int],string], $stackType[double]]"
        $x.gettype().fullname -like '*double*' | Should Be $true
    }

    It 'non-generic EventHandler' {
        # EventHandler has a generic and a non-generic.  This code caused an exception trying to
        # use the non-generic.
        $x = [System.EventHandler[System.Management.Automation.PSInvocationStateChangedEventArgs]]

        # The error message for a generic that doesn't meet the constraints should mention which
        # argument failed.
        $ex = $null
        try {
            [nullable[object]]
        } catch {
            $_.FullyQualifiedErrorId | Should be 'TypeNotFoundWithMessage'
            $_ -like "*`[T`]*" | Should Be $true
        }
    }

    It 'Array' {
        $x = [system.array]::ConvertAll.OverloadDefinitions
        $x | Should match "static\s+TOutput\[\]\s+ConvertAll\[TInput,\s+TOutput\]\("
   }
}