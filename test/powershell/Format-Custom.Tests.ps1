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
