Describe "Get-Verb" -Tags "CI" {

    It "Should get a list of Verbs" {
        Get-Verb | Should not be $null
    }

    It "Should get a specific verb" {
        @(Get-Verb -Verb Add).Count | Should be 1
        @(Get-Verb -Verb Add -Group Common).Count | Should be 1
    }

    It "Should get a specific group" {
        (Get-Verb -Group Common).Group | Sort-Object -Unique | Should be Common
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
            throw "Expected error did not occur"
        }
        Catch{
            $PSItem.FullyQualifiedErrorId | Should be 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetVerbCommand' 
        }
    }

    It "Accept all valid verb groups" {
        $groups = ([System.Reflection.IntrospectionExtensions]::GetTypeInfo([PSObject]).Assembly.ExportedTypes | 
            Where-Object {$_.Name -match '^Verbs.'} |
            Select-Object -Property @{Name='VerbGroup';Expression={$_.Name.Substring(5)}}).VerbGroup
        ForEach($group in $groups)
        {
            {Get-Verb -Group $group} | Should Not Throw
        }
    }
}
