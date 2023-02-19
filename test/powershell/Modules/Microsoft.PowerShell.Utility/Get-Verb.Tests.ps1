# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Verb" -Tags "CI" {

    It "Should get a list of Verbs" {
        Get-Verb | Should -Not -BeNullOrEmpty
    }

    It "Should get a specific verb" {
        @(Get-Verb -verb Add).Count | Should -Be 1
        @(Get-Verb -verb Add -Group Common).Count | Should -Be 1
    }

    It "Should get a specific group" {
        (Get-Verb -Group Common).Group | Sort-Object -Unique | Should -Be Common
    }

    It "Should not return duplicate Verbs with Verb parameter" {
        $dups = Get-Verb -verb Add,ad*,a*
        $unique = $dups |
            Select-Object -Property * -Unique
        $dups.Count | Should -Be $unique.Count
    }

    It "Should not return duplicate Verbs with Group parameter" {
        $dupVerbs = Get-Verb -Group Data,Data
        $uniqueVerbs = $dupVerbs |
            Select-Object -Property * -Unique
        $dupVerbs.Count | Should -Be $uniqueVerbs.Count
    }

    It "Should filter using the Verb parameter" {
        Get-Verb -verb fakeVerbNeverExists | Should -BeNullOrEmpty
    }

    It "Should not accept Groups that are not in the validate set" {
        { Get-Verb -Group FakeGroupNeverExists -ErrorAction Stop } |
            Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.PowerShell.Commands.GetVerbCommand'
    }

    It "Accept all valid verb groups" {
        $groups = ([System.Reflection.IntrospectionExtensions]::GetTypeInfo([PSObject]).Assembly.ExportedTypes |
            Where-Object {$_.Name -match '^Verbs.'} |
            Select-Object -Property @{Name='VerbGroup';Expression={$_.Name.Substring(5)}}).VerbGroup
        ForEach($group in $groups)
        {
            {Get-Verb -Group $group} | Should -Not -Throw
        }
    }

    It "Should not have verbs without descriptions" {
        $noDescVerbs = (Get-Verb | Where-Object { [string]::IsNullOrEmpty($_.Description) }).Verb -join ", "
        $noDescVerbs | Should -BeNullOrEmpty
    }

    It "Should not have verbs without alias prefixes" {
        $noPrefixVerbs = (Get-Verb | Where-Object { [string]::IsNullOrEmpty($_.AliasPrefix) }).Verb -join ", "
        $noPrefixVerbs | Should -BeNullOrEmpty
    }

    It "Should not have duplicate alias prefixes" {
        $dupPrefixVerbs = ((Get-Verb | Group-Object -Property AliasPrefix | Where-Object Count -GT 1).Group).Verb -join ", "
        $dupPrefixVerbs | Should -BeNullOrEmpty
    }
}
