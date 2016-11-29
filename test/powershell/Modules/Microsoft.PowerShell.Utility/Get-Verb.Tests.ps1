Describe "Get-Verb" -Tags "CI" {

    It "Should get a list of Verbs" -test {
        Get-Verb | Should not be $null
    }

    It "Should get a specific verb" -test {
        (Get-Verb -Verb Add | Measure-Object).Count | Should be 1
        (Get-Verb -Verb Add -Group Common | Measure-Object).Count | Should be 1
    }

    It "Should get a specific group" -test {
        Get-Verb -Group Common | Should not be $null
    }

    It "Should not return duplicate Verbs with Verb paramater" -test {
        $dups = Get-Verb -Verb Add,ad*,a*
        $unique = $dups | 
            Select-Object -Unique
        $dups.Count | Should be $unique.Count 
    }

    It "Should not return duplicate Verbs with Group paramater" -test {
        $dupVerbs = Get-Verb -Group other,other
        $uniqueVerbs = $dupVerbs | 
            Select-Object -Unique
        $dupVerbs.Count | Should be $uniqueVerbs.Count 
    }

    It "Should filter using the verb parameter" -test {
        Get-Verb -Verb fakeVerbNeverExists | Should be ''
    }

    It "Should not accept Groups that are not in the validate set" -test {
        try{
            Get-Verb -Group FakeGroupNeverExists -ErrorAction Stop
        }
        Catch{
            if($PSItem.FullyQualifiedErrorId -eq 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetVerbCommand'){
                $result = $true
            }
            else{
                $result = $false
            }
            $result | should be $true
        }
    }
}
