# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

class CompletionResult
{
    [string]$CompletionText
    [string]$ListItemText
    [System.Management.Automation.CompletionResultType]$ResultType
    [string]$ToolTip
    [bool]$Found

    [bool] Equals($Other)
    {
        if ($Other -isnot [CompletionResult] -and
            $Other -isnot [System.Management.Automation.CompletionResult])
        {
            return $false
        }

        # Comparison is intentionally fuzzy - CompletionText and ResultType must be specified
        # but the other properties don't need to match if they aren't specified

        if ($this.CompletionText -cne $Other.CompletionText -or
            $this.ResultType -ne $Other.ResultType)
        {
            return $false
        }

        if ($this.ListItemText -cne $Other.ListItemText -and
            ![string]::IsNullOrEmpty($this.ListItemText) -and ![string]::IsNullOrEmpty($Other.ListItemText))
        {
            return $false
        }

        if ($this.ToolTip -cne $Other.ToolTip -and
            ![string]::IsNullOrEmpty($this.ToolTip) -and ![string]::IsNullOrEmpty($Other.ToolTip))
        {
            return $false
        }

        return $true
    }
}

class CompletionTestCase
{
    [string]$Description
    [CompletionResult[]]$ExpectedResults
    [string[]]$NotExpectedResults
    [hashtable]$TestInput
}

function Get-Completions
{
    [CmdletBinding()]
    param([string]$InputScript, [int]$CursorColumn, $Options = $null)

    if (!$PSBoundParameters.ContainsKey('CursorColumn'))
    {
        $CursorColumn = $InputScript.IndexOf('<#CURSOR#>')
        if ($CursorColumn -lt 0)
        {
            $CursorColumn = $InputScript.Length
        }
        else
        {
            $InputScript = $InputScript -replace '<#CURSOR#>',''
        }
    }

    $results = [System.Management.Automation.CommandCompletion]::CompleteInput(
        <#inputScript#>  $InputScript,
        <#cursorColumn#> $CursorColumn,
        <#options#>      $Options)

    return $results
}

function Get-CompletionTestCaseData
{
    param(
        [Parameter(ValueFromPipeline)]
        [hashtable[]]$Data)

    process
    {
        Write-Output ([CompletionTestCase[]]$Data)
    }
}

function Test-Completions
{
    param(
        [Parameter(ValueFromPipeline)]
        [CompletionTestCase[]]$TestCases,
        [string]
        $Description)

    process
    {
        foreach ($test in $TestCases)
        {
            Describe $test.Description -Tags "CI" {
                $hash = $Test.TestInput
                $results = Get-Completions @hash

                foreach ($expected in $test.ExpectedResults)
                {
                    foreach ($result in $results.CompletionMatches)
                    {

                        if ($expected.Equals($result))
                        {
                            It "Checking for duplicates of: $($expected.CompletionText)" {
                                # We should only find 1 of each expected result
                                $expected.Found | Should -BeFalse
                            }
                            $expected.Found = $true
                        }
                    }
                }

                foreach ($expected in $test.ExpectedResults)
                {
                    It "Checking for presence of expected result: $($expected.CompletionText)" {
                        $expected.Found | Should -BeTrue
                    }
                }

                foreach ($notExpected in $test.NotExpectedResults)
                {
                    foreach ($result in $results.CompletionMatches)
                    {
                        It "Checking for results that should not be found: $notExpected" {
                            $result.CompletionText | Should -Not -Be $notExpected
                        }
                    }
                }

            }
        }
    }
}
