. ./Test-Mocks.ps1

Describe "Select-Object" {
    BeforeEach {
        $dirObject=GetFileMock
        $TestLength = 3
    }

    It "Handle piped input without error" {
        { $dirObject | Test-Path } | Should Not Throw
    } 

    It "-inputObject parameter should treats object as a single object" {
        $(Select-Object -inputObject $dirObject -last $TestLength).Length | Should Be $dirObject.Length
    }

    It "Should be able to use the alias" {
        { $dirObject | select } | Should Not Throw
        { $dirObject | select } | Should Not BeNullOrEmpty
    }

    It "-First parameter should return correct object" {
        $result = $dirObject | Select-Object -First $TestLength
        $result.Length | Should Be $TestLength
        for ($i=0; $i -lt $TestLength; $i++)
        {
            $result[$i].Name | Should Be $dirObject[$i].Name
        }
    }

    It "-Last parameter should return correct object" {
        $result = $dirObject | Select-Object -Last $TestLength
        $result.Length | Should Be $TestLength
        for ($i=0; $i -lt $TestLength; $i++)
        {
            $result[$i].Name | Should Be $dirObject[$dirObject.Length - $TestLength + $i].Name
        }
    }

    It "-Unique parameter should work correctly" {
        ("a","b","c","a","a","a" | Select-Object -Unique).Length | Should Be 3
    }

    It "-Skip parameter should return correct object" {
        $result = $dirObject | Select-Object -Skip $TestLength
        $result.Length | Should Be ($dirObject.Length - $TestLength)
        for ($i=0; $i -lt $TestLength; $i++)
        {
            $result[$i].Name | Should Be $dirObject[$TestLength + $i].Name
        }
    }

    It "-Property parameter should return an object with selected columns" {
        $result = $dirObject | Select-Object -Property Name, Size
        $result.Length | Should Be $dirObject.Length
        $result[0].Name | Should Be $dirObject[0].Name
        $result[0].Size | Should Be $dirObject[0].Size
        $result[0].Mode | Should BeNullOrEmpty
    }

    It "Select-Object should send output to pipe properly" {
        {$dirObject | Select-Object -Unique | pipelineConsume} | Should Not Throw
    } 

    It "-Index parameter should select array indices" {
        $firstIndex = 2
        $secondIndex = 4
        $result = $dirObject | Select-Object -Index $firstIndex, $secondIndex
        $result[0].Name | Should Be $dirObject[$firstIndex].Name
        $result[1].Name | Should Be $dirObject[$secondIndex].Name
    }

    # Note that these two tests will modify original values of $dirObject

    It "-First option does not wait when used without -Wait option" {
        $orig1 = $dirObject[0].Size
        $orig2 = $dirObject[$TestLength].Size
        $result = $dirObject | addOne | Select-Object -First $TestLength
        $result[0].Size | Should Be ($orig1 + 1)
        $dirObject[0].Size | Should Be ($orig1 + 1)
        $dirObject[$TestLength].Size | Should Be $orig2
    }

    It "-First option does wait when used with -Wait option" {
        $orig1 = $dirObject[0].Size
        $orig2 = $dirObject[$TestLength].Size
        $result = $dirObject | addOne | Select-Object -First $TestLength -Wait
        $result[0].Size | Should Be ($orig1 + 1)
        $dirObject[0].Size | Should Be ($orig1 + 1)
        $dirObject[$TestLength].Size | Should Be ($orig2 + 1)
    }
}


