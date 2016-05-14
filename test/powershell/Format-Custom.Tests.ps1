Describe "Format-Custom" {

    Context "Check Format-Custom aliases" {

        It "Should have the same output between the alias and the unaliased function" {
            $nonaliased = Get-FormatData | Format-Custom
            $aliased    = Get-FormatData | fc
            $($nonaliased | Out-String).CompareTo($($aliased | Out-String)) | Should Be 0
        }
    }

    Context "Check specific flags on Format-Custom" {

        It "Should be able to specify the depth in output" {
            $getprocesspester =  Get-FormatData | Format-Custom -depth 1
            ($getprocesspester).Count                   | Should BeGreaterThan 0
        }

        It "Should be able to use the Property flag to select properties" {
            $CommandName = Get-Command | Format-Custom -Property "Name"
            $CommandName               | Should Not Match "Source"
        }

    }
}


Describe "Format-Custom DRT basic functionality" -Tags DRT{
    It "Format-Custom with subobject should work" -Pending:($env:TRAVIS_OS_NAME -eq "osx") {
        $expectResult1 = "this is the name"
        $expectResult2 = "this is the name of the sub object"
        $testObject = @{}
        $testObject.name = $expectResult1
        $testObject.subObjectValue = @{}
        $testObject.subObjectValue.name = $expectResult2
        $testObject.subObjectValue.array = (0..63)
        $testObject.subObjectValue.stringarray = @("one","two")
        $result = $testObject | Format-Custom | Out-String
        $result | Should Match $expectResult1
        $result | Should Match $expectResult2
        $result | Should Match "one"
        $result | Should Match "two"
    }
}
