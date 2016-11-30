Describe "Get-Verb" -Tags "CI" {

    It "Should get a list of Verbs" {
        Get-Verb | Should not be $null
    }

    It "Should get a specific verb" {
        (Get-Verb -Verb Add | Measure-Object).Count | Should be 1
        (Get-Verb -Verb Add -Group Common | Measure-Object).Count | Should be 1
    }

    It "Should get a specific group" {
        Get-Verb -Group Common | Should not be $null
    }

    It "Should not return duplicate Verbs with Verb paramater" {
        $dups = Get-Verb -Verb Add,ad*,a*
        $unique = $dups | 
            Select-Object -Property * -Unique
        $dups.Count | Should be $unique.Count 
    }

    It "Should not return duplicate Verbs with Group paramater" {
        $dupVerbs = Get-Verb -Group Data,Data
        $uniqueVerbs = $dupVerbs | 
            Select-Object -Property * -Unique
        $dupVerbs.Count | Should be $uniqueVerbs.Count 
    }

    It "Should filter using the Verb parameter" {
        Get-Verb -Verb fakeVerbNeverExists | Should BeNullOrEmpty
    }

    It "Should not accept Groups that are not in the validate set" {
        try{
            Get-Verb -Group FakeGroupNeverExists -ErrorAction Stop
        }
        Catch{
            $PSItem.FullyQualifiedErrorId | Should be 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetVerbCommand' 
        }
    }
}
