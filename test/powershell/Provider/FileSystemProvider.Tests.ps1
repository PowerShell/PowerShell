Describe "SplitPath Tests (Windows Only)" -tags "CI" {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( $IsWindows ) {
            
            $multilevelPath = 'C:\Temp\folder1'
            $singlelevelPath = 'C:\Temp'
            $multilevelUNC = '\\server1\share1\folder'
            $UNCRoot = '\\server1\share1'

            $splitPathParentCases = @(
                @{TestName='Multilevel'; Path = $multiLevelPath; Result = 'C:\temp'}
                @{TestName='Single Level'; Path = $singlelevelPath; Result = 'C:\'}
                @{TestName='Multilevel UNC'; Path = $multilevelUNC; Result = '\\server1\share1'}
                @{TestName='UNC Root'; Path = $UNCRoot; Result = '\\server1'}
            )

            $splitPathChildCases = @(
                @{TestName='Multilevel'; Path = $multiLevelPath; Result = 'folder1'}
                @{TestName='Single Level'; Path = $singlelevelPath; Result = 'temp'}
                @{TestName='Multilevel UNC'; Path = $multilevelUNC; Result = 'folder'}
                @{TestName='UNC Root'; Path = $UNCRoot; Result = 'share1'}
            )

        }
        else{
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }
    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }
    
    It 'Splits Path on a <TestName> Path and gets the parent' -TestCases $splitPathParentCases -test {
        param($TestName,$Path,$Result)
            Split-Path -Path $Path -Parent | Should be $Result
    }

    It 'Splits Path on a <TestName> Path and gets the child' -TestCases $splitPathChildCases -test {
        param($TestName,$Path,$Result)
            Split-Path -Path $Path -Leaf | Should be $Result
    }
    
    It 'Does not split a drive leter'{
        Split-Path -Path 'C:\' | Should be ''
    }
   
} 