Describe "SplitPath Tests (Windows Only)" -tags "CI" {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( ! $IsWindows ) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }
    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }
    
    It 'Splits a multilevel path' {
        Split-Path -Path 'C:\Temp\folder1' | Should be 'C:\temp'
    }

    It 'Returns a multilevel path child' {
        Split-Path -Path 'C:\Temp\folder1' -Leaf | Should be 'folder1'
    }
   
    It 'Splits a single level path' {
        Split-Path -Path 'C:\Temp' | Should be 'C:\'
    }

    It 'Returns a single level child' {
        Split-Path -Path 'C:\Temp' -Leaf | Should be 'temp'
    }

    It 'Splits a multilevel unc path' {
        Split-Path -Path '\\server1\share1\folder' | Should be '\\server1\share1'
    }

    It 'Returns a multilevel unc path child' {
        Split-Path -Path '\\server1\share1\folder' -Leaf | Should be 'folder'
    }

    It 'Splits a unc path' {
        Split-Path -Path '\\server1\share1' | Should be '\\server1'
    }

    It 'Returns a unc path child' {
        Split-Path -Path '\\server1\share1' -Leaf | Should be 'share1'
    }

    It 'Does not split a drive leter'{
        Split-Path -Path 'C:\' | Should be ''
    }
   
} 
