$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. "$here/Test-Mocks.ps1"

Describe "Select-Object" {
    BeforeEach {
        $dirObject  = GetFileMock
        $TestLength = 3
    }

    It "Handle piped input without error" {
        { $dirObject | Select-Object } | Should Not Throw
    } 

    It "Should treat input as a single object with the inputObject parameter" {
        $result   = $(Select-Object -inputObject $dirObject -last $TestLength).Length
        $expected = $dirObject.Length

    $result | Should Be $expected
    }

    It "Should be able to use the alias" {
        { $dirObject | select } | Should Not Throw
    }

    It "Should have same result when using alias" {
        $result   = $dirObject | select  
        $expected = $dirObject | Select-Object

    $result | Should Be $expected
    }

    It "Should return correct object with First parameter" {
        $result = $dirObject | Select-Object -First $TestLength

        $result.Length | Should Be $TestLength

        for ($i=0; $i -lt $TestLength; $i++)
        {
            $result[$i].Name | Should Be $dirObject[$i].Name
        }
    }

    It "Should return correct object with Last parameter" {
        $result = $dirObject | Select-Object -Last $TestLength

        $result.Length | Should Be $TestLength

        for ($i=0; $i -lt $TestLength; $i++)
        {
            $result[$i].Name | Should Be $dirObject[$dirObject.Length - $TestLength + $i].Name
        }
    }

    It "Should work correctly with Unique parameter" {
        $result   = ("a","b","c","a","a","a" | Select-Object -Unique).Length 
        $expected = 3

        $result | Should Be $expected
    }

    It "Should return correct object with Skip parameter" {
        $result = $dirObject | Select-Object -Skip $TestLength

        $result.Length       | Should Be ($dirObject.Length - $TestLength)

        for ($i=0; $i -lt $TestLength; $i++)
        {
            $result[$i].Name | Should Be $dirObject[$TestLength + $i].Name
        }
    }

    It "Should return an object with selected columns" {
        $result = $dirObject | Select-Object -Property Name, Size

        $result.Length  | Should Be $dirObject.Length
        $result[0].Name | Should Be $dirObject[0].Name
        $result[0].Size | Should Be $dirObject[0].Size
        $result[0].Mode | Should BeNullOrEmpty
    }

    It "Should send output to pipe properly" {
        {$dirObject | Select-Object -Unique | pipelineConsume} | Should Not Throw
    } 

    It "Should select array indices with Index parameter" {
        $firstIndex  = 2
        $secondIndex = 4
        $result      = $dirObject | Select-Object -Index $firstIndex, $secondIndex

        $result[0].Name | Should Be $dirObject[$firstIndex].Name
        $result[1].Name | Should Be $dirObject[$secondIndex].Name
    }

    # Note that these two tests will modify original values of $dirObject

    It "Should not wait when used without -Wait option" {
        $orig1  = $dirObject[0].Size
        $orig2  = $dirObject[$TestLength].Size
        $result = $dirObject | addOneToSizeProperty | Select-Object -First $TestLength

        $result[0].Size              | Should Be ($orig1 + 1)
        $dirObject[0].Size           | Should Be ($orig1 + 1)
        $dirObject[$TestLength].Size | Should Be $orig2
    }

    It "Should wait when used with -Wait option" {
        $orig1  = $dirObject[0].Size
        $orig2  = $dirObject[$TestLength].Size
        $result = $dirObject | addOneToSizeProperty | Select-Object -First $TestLength -Wait

        $result[0].Size              | Should Be ($orig1 + 1)
        $dirObject[0].Size           | Should Be ($orig1 + 1)
        $dirObject[$TestLength].Size | Should Be ($orig2 + 1)
    }
}


