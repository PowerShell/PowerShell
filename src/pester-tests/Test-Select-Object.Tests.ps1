. ../../../src/pester-tests/Test-Mocks.ps1

Describe "Select-Object" {
    BeforeEach {
        $o=GetFileMock
        $TestLength = 3
    }

    It "Handle piped input without error" {
        { $o | Test-Path } | Should Not Throw
    } 

    It "-inputObject parameter should treats object as a single object" {
        $(Select-Object -inputObject $o -last $TestLength).Length | Should Be $o.Length
    }

    It "Should be able to use the alias" {
        { $o | select } | Should Not Throw
        { $o | select } | Should Not BeNullOrEmpty
    }

    It "-First parameter should return correct object" {
        $r = $o | Select-Object -First $TestLength
        $r.Length | Should Be $TestLength
        for ($i=0; $i -lt $TestLength; $i++)
        {
            $r[$i].Name | Should Be $o[$i].Name
        }
    }

    It "-Last parameter should return correct object" {
        $r = $o | Select-Object -Last $TestLength
        $r.Length | Should Be $TestLength
        for ($i=0; $i -lt $TestLength; $i++)
        {
            $r[$i].Name | Should Be $o[$o.Length - $TestLength + $i].Name
        }
    }

    It "-Unique parameter should work correctly" {
        ("a","b","c","a","a","a" | Select-Object -Unique).Length | Should Be 3
    }

    It "-Skip parameter should return correct object" {
        $r = $o | Select-Object -Skip $TestLength
        $r.Length | Should Be ($o.Length - $TestLength)
        for ($i=0; $i -lt $TestLength; $i++)
        {
            $r[$i].Name | Should Be $o[$TestLength + $i].Name
        }
    }

    It "-Property parameter should return an object with selected columns" {
        $r = $o | Select-Object -Property Name, Size
        $r.Length | Should Be $o.Length
        $r[0].Name | Should Be $o[0].Name
        $r[0].Size | Should Be $o[0].Size
        $r[0].Mode | Should BeNullOrEmpty
    }

    It "Select-Object should send output to pipe properly" {
        {$o | Select-Object -Unique | pipelineConsume} | Should Not Throw
    } 

    It "-Index parameter should select array indices" {
        $firstIndex = 2
        $secondIndex = 4
        $r = $o | Select-Object -Index $firstIndex, $secondIndex
        $r[0].Name | Should Be $o[$firstIndex].Name
        $r[1].Name | Should Be $o[$secondIndex].Name
    }

    # Note that these two tests will modify original values of $o

    It "-First option does not wait when used without -Wait option" {
        $orig1 = $o[0].Size
        $orig2 = $o[$TestLength].Size
        $r = $o | addOne | Select-Object -First $TestLength
        $r[0].Size | Should Be ($orig1 + 1)
        $o[0].Size | Should Be ($orig1 + 1)
        $o[$TestLength].Size | Should Be $orig2
    }

    It "-First option does wait when used with -Wait option" {
        $orig1 = $o[0].Size
        $orig2 = $o[$TestLength].Size
        $r = $o | addOne | Select-Object -First $TestLength -Wait
        $r[0].Size | Should Be ($orig1 + 1)
        $o[0].Size | Should Be ($orig1 + 1)
        $o[$TestLength].Size | Should Be ($orig2 + 1)
    }
}


