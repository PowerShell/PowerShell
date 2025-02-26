# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Tab completion bug fix" -Tags "CI" {

    It "Issue#682 - '[system.manage<tab>' should work" {
        $result = TabExpansion2 -inputScript "[system.manage" -cursorColumn "[system.manage".Length
        $result.CompletionMatches | Should -HaveCount 1
        $result.CompletionMatches[0].CompletionText | Should -BeExactly "System.Management"
    }

    It "Issue#1350 - '1 -sp<tab>' should work" {
        $result = TabExpansion2 -inputScript "1 -sp" -cursorColumn "1 -sp".Length
        $result.CompletionMatches | Should -HaveCount 1
        $result.CompletionMatches[0].CompletionText | Should -BeExactly "-split"
    }

    It "Issue#1350 - '1 -a<tab>' should work" {
        $result = TabExpansion2 -inputScript "1 -a" -cursorColumn "1 -a".Length
        $result.CompletionMatches | Should -HaveCount 2
        $result.CompletionMatches[0].CompletionText | Should -BeExactly "-and"
        $result.CompletionMatches[1].CompletionText | Should -BeExactly "-as"
    }
    It "Issue#2295 - '[pscu<tab>' should expand to [pscustomobject]" {
        $result = TabExpansion2 -inputScript "[pscu" -cursorColumn "[pscu".Length
        $result.CompletionMatches | Should -HaveCount 1
        $result.CompletionMatches[0].CompletionText | Should -BeExactly "pscustomobject"
    }
    It "Issue#1345 - 'Import-Module -n<tab>' should work" {
        $cmd = "Import-Module -n"
        $result = TabExpansion2 -inputScript $cmd -cursorColumn $cmd.Length
        $result.CompletionMatches | Should -HaveCount 2
        $result.CompletionMatches[0].CompletionText | Should -BeExactly "-Name"
        $result.CompletionMatches[1].CompletionText | Should -BeExactly "-NoClobber"
    }

    It "Issue#11227 - [CompletionCompleters]::CompleteVariable and [CompletionCompleters]::CompleteType should work" {
        $result = [System.Management.Automation.CompletionCompleters]::CompleteType("CompletionComple")
        $result.Count | Should -BeExactly 1
        $result[0].CompletionText | Should -BeExactly 'System.Management.Automation.CompletionCompleters'

        $result = [System.Management.Automation.CompletionCompleters]::CompleteVariable("errorAction")
        $result.Count | Should -BeExactly 1
        $result[0].CompletionText | Should -BeExactly '$ErrorActionPreference'
    }

    It "Issue#24756 - Wildcard completions should not return early due to missing results in one container" -Skip:(!$IsWindows) {
        try
        {
            $keys = New-Item -Path @(
                'HKCU:\AB1'
                'HKCU:\AB2'
                'HKCU:\AB2\Test'
            )

            $res = TabExpansion2 -inputScript 'Get-ChildItem -Path HKCU:\AB?\'
            $res.CompletionMatches.Count | Should -Be 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "HKCU:\AB2\Test"
        }
        finally
        {
            if ($keys)
            {
                Remove-Item -Path HKCU:\AB? -Recurse -ErrorAction SilentlyContinue
            }
        }
    }

    Context "Issue#3416 - 'Select-Object'" {
        BeforeAll {
            $DatetimeProperties = @((Get-Date).psobject.baseobject.psobject.properties) | Sort-Object -Property Name
        }
        It "Issue#3416 - 'Select-Object -ExcludeProperty <tab>' should work" {
            $cmd = "Get-Date | Select-Object -ExcludeProperty "
            $result = TabExpansion2 -inputScript $cmd -cursorColumn $cmd.Length
            $result.CompletionMatches | Should -HaveCount $DatetimeProperties.Count
            $result.CompletionMatches[0].CompletionText | Should -BeExactly $DatetimeProperties[0].Name # Date
            $result.CompletionMatches[1].CompletionText | Should -BeExactly $DatetimeProperties[1].Name # DateTime
       }
       It "Issue#3416 - 'Select-Object -ExpandProperty <tab>' should work" {
           $cmd = "Get-Date | Select-Object -ExpandProperty "
           $result = TabExpansion2 -inputScript $cmd -cursorColumn $cmd.Length
           $result.CompletionMatches | Should -HaveCount $DatetimeProperties.Count
           $result.CompletionMatches[0].CompletionText | Should -BeExactly $DatetimeProperties[0].Name # Date
           $result.CompletionMatches[1].CompletionText | Should -BeExactly $DatetimeProperties[1].Name # DateTime
       }
    }

    It "Issue#3628 - 'Sort-Object @{<tab>' should work" {
        $cmd = "Get-Date | Sort-Object @{"
        $result = TabExpansion2 -inputScript $cmd -cursorColumn $cmd.Length
        $result.CompletionMatches | Should -HaveCount 3
        $result.CompletionMatches[0].CompletionText | Should -BeExactly 'Expression'
        $result.CompletionMatches[1].CompletionText | Should -BeExactly 'Ascending'
        $result.CompletionMatches[2].CompletionText | Should -BeExactly 'Descending'
    }

    It "'Get-Date | Sort-Object @{Expression=<tab>' should work without completion" {
        $cmd = "Get-Date | Sort-Object @{Expression="
        $result = TabExpansion2 -inputScript $cmd -cursorColumn $cmd.Length
        $result.CompletionMatches | Should -HaveCount 0
    }

    It "Issue#5322 - 'Get-Date | Sort-Object @{Expression=...;' should work" {
        $cmd = "Get-Date | Sort-Object @{Expression=...;"
        $result = TabExpansion2 -inputScript $cmd -cursorColumn $cmd.Length
        $result.CurrentMatchIndex | Should -Be -1
        $result.ReplacementIndex | Should -Be 40
        $result.ReplacementLength | Should -Be 0
        $result.CompletionMatches[0].CompletionText | Should -BeExactly 'Ascending'
        $result.CompletionMatches[1].CompletionText | Should -BeExactly 'Descending'
    }

    It "Issue#19912 - Tab completion should not crash" {
        $ISS = [initialsessionstate]::CreateDefault()
        $Runspace = [runspacefactory]::CreateRunspace($ISS)
        $Runspace.Open()
        $OldRunspace = [runspace]::DefaultRunspace
        try
        {
            [runspace]::DefaultRunspace = $Runspace
            {[System.Management.Automation.CommandCompletion]::CompleteInput('Get-', 3, $null)} | Should -Not -Throw
        }
        finally
        {
            [runspace]::DefaultRunspace = $OldRunspace
            $Runspace.Dispose()
        }
    }
}
