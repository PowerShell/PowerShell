<#
    Much of this script belongs in a module, but we don't support importing classes yet.
#>
using namespace System.Management.Automation
using namespace System.Management.Automation.Language
using namespace System.Collections
using namespace System.Collections.Generic

#region Testcase infrastructure

class CompletionTestResult
{
    [string]$CompletionText
    [string]$ListItemText
    [CompletionResultType]$ResultType
    [string]$ToolTip
    [bool]$Found

    [bool] Equals($Other)
    {
        if ($Other -isnot [CompletionTestResult] -and
            $Other -isnot [CompletionResult])
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
    [CompletionTestResult[]]$ExpectedResults
    [string[]]$NotExpectedResults
    [string]$TestInput
}

function Get-Completions
{
    param([string]$inputScript, [int]$cursorColumn = $inputScript.Length)

    $results = [System.Management.Automation.CommandCompletion]::CompleteInput(
        <#inputScript#>  $inputScript,
        <#cursorColumn#> $cursorColumn,
        <#options#>      $null)

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
        [CompletionTestCase[]]$TestCases)
    
    process
    {
        foreach ($test in $TestCases)
        {
            Context ("Command line: <" + $test.TestInput + ">") {
                $results = Get-Completions $test.TestInput
                foreach ($result in $results.CompletionMatches)
                {
                    foreach ($expected in $test.ExpectedResults)
                    {
                        if ($expected.Equals($result))
                        {
                            $expected.Found = $true
                        }
                    }
                }
                foreach ($expected in $test.ExpectedResults)
                {
                    $skip = $false
                    if ( $expected.CompletionText -match "System.Management.Automation.PerformanceData|System.Management.Automation.Security" ) { $skip = $true }
                    It ($expected.CompletionText) -skip:$skip {
                        $expected.Found | Should Be $true
                    }
                }

                foreach ($notExpected in $test.NotExpectedResults)
                {
                    It "Not expected: $notExpected" {
                        foreach ($result in $results.CompletionMatches)
                        {
                            ($result.CompletionText -ceq $notExpected) | Should Be $False
                        }
                    }
                }
            }
        }
    }
}

#endregion Testcase infrastructure

function AlphaArgumentCompleter
{
    param(
        [string] $CommandName,
        [string] $parameterName,
        [string] $wordToComplete,
        [CommandAst] $commandAst,
        [IDictionary] $fakeBoundParameters)

    $beta = $fakeBoundParameters['beta']
    $gamma = $fakeBoundParameters['Gamma']
    $result = "beta: $beta  gamma: $gamma  command: $commandName  parameterName: $parameterName  wordToComplete: $wordToComplete"
    [CompletionResult]::new($result, $result, "ParameterValue", $result)
}

class BetaArgumentCompleter : IArgumentCompleter
{
    [IEnumerable[CompletionResult]] CompleteArgument(
        [string] $CommandName,
        [string] $parameterName,
        [string] $wordToComplete,
        [CommandAst] $commandAst,
        [IDictionary] $fakeBoundParameters)
    {
        $resultList = [List[CompletionResult]]::new()

        $alpha = $fakeBoundParameters['Alpha']
        $gamma = $fakeBoundParameters['Gamma']
        $result = "alpha: $alpha  gamma: $gamma  command: $commandName  parameterName: $parameterName  wordToComplete: $wordToComplete"
        $resultList.Add([CompletionResult]::new($result, $result, "ParameterValue", $result))

        return $resultList
    }
}

function TestFunction
{
    param(
        [ArgumentCompleter({ AlphaArgumentCompleter @args })]
        $Alpha,
        [ArgumentCompleter([BetaArgumentCompleter])]
        $Beta,
        $Gamma
    )
}

Describe "Script block based extensible completion" -Tags "CI" {
    @{
        ExpectedResults = @(
            @{CompletionText = "beta: 11  gamma: 22  command: TestFunction  parameterName: Alpha  wordToComplete: aa"
              ResultType = "ParameterValue"})
        TestInput = 'TestFunction -Beta 11 -Gamma 22 -Alpha aa'
    } | Get-CompletionTestCaseData | Test-Completions
}

Describe "Test class based extensible completion" -Tags "CI" {
    @{
        ExpectedResults = @(
            @{CompletionText = "alpha: 42  gamma: 44  command: TestFunction  parameterName: Beta  wordToComplete: zz"
              ResultType = "ParameterValue"})
        TestInput = 'TestFunction -Alpha 42 -Gamma 44 -Beta zz'
    } | Get-CompletionTestCaseData | Test-Completions
}

Describe "Test registration based extensible completion" -Tags "CI" {
    Register-ArgumentCompleter -Command TestFunction -Parameter Gamma -ScriptBlock {
        param(
            [string] $CommandName,
            [string] $parameterName,
            [string] $wordToComplete,
            [CommandAst] $commandAst,
            [IDictionary] $fakeBoundParameters)

        $beta = $fakeBoundParameters['beta']
        $alpha = $fakeBoundParameters['alpha']
        $result = "beta: $beta  alpha: $alpha  command: $commandName  parameterName: $parameterName  wordToComplete: $wordToComplete"
        [CompletionResult]::new($result, $result, "ParameterValue", $result)
    }

    @{
        ExpectedResults = @(
            @{CompletionText = "beta: bb  alpha: aa  command: TestFunction  parameterName: Gamma  wordToComplete: 42"
              ResultType = "ParameterValue"})
        TestInput = 'TestFunction -Alpha aa -Beta bb -Gamma 42'
    } | Get-CompletionTestCaseData | Test-Completions
}

Describe "Test extensible completion of native commands" -Tags "CI" {
    Register-ArgumentCompleter -Command netsh -Native -ScriptBlock {
        [CompletionResult]::new('advfirewall', 'advfirewall', "ParameterValue", 'advfirewall')
        [CompletionResult]::new('bridge', 'bridge', "ParameterValue", 'bridge')
    }

    @{
        ExpectedResults = @(
            @{CompletionText = "advfirewall"; ResultType = "ParameterValue"}
            @{CompletionText = "bridge"; ResultType = "ParameterValue"}
            )
        TestInput = 'netsh '
    } | Get-CompletionTestCaseData | Test-Completions
}

Describe "Test extensible completion of using namespace" -Tags "CI" {
    @{
        ExpectedResults = @(
            @{CompletionText = "System"; ResultType = "Namespace"}
            )
        TestInput = 'Using namespace sys'
    },
    @{
        ExpectedResults = @(
            @{CompletionText = "System.Xml"; ResultType = "Namespace"}
            @{CompletionText = "System.Data"; ResultType = "Namespace"}
            @{CompletionText = "System.Collections"; ResultType = "Namespace"}
            @{CompletionText = "System.IO"; ResultType = "Namespace"}
            )
        TestInput = 'Using namespace system.'
    },
    @{
        ExpectedResults = @(
            @{CompletionText = "System.Management.Automation"; ResultType = "Namespace"}
            )
        TestInput = 'Using namespace System.Management.Automati'
    },
    @{
        ExpectedResults = @(
            @{CompletionText = "System.Management.Automation.Host"; ResultType = "Namespace"}
            @{CompletionText = "System.Management.Automation.Internal"; ResultType = "Namespace"}
            @{CompletionText = "System.Management.Automation.Language"; ResultType = "Namespace"}
            @{CompletionText = "System.Management.Automation.PerformanceData"; ResultType = "Namespace"}
            @{CompletionText = "System.Management.Automation.Provider"; ResultType = "Namespace"}
            @{CompletionText = "System.Management.Automation.Remoting"; ResultType = "Namespace"}
            @{CompletionText = "System.Management.Automation.Runspaces"; ResultType = "Namespace"}
            @{CompletionText = "System.Management.Automation.Security"; ResultType = "Namespace"}
            )
        TestInput = 'using namespace System.Management.Automation.'
    } | Get-CompletionTestCaseData | Test-Completions
}

Describe "Type extensible completion of type after using namespace" -Tags "CI" {
    @{
        ExpectedResults = @(
            @{CompletionText = "IO.TextReader"; ResultType = "Type"}
            )
        TestInput = 'using namespace System; [TextR'
    },
    @{
        ExpectedResults = @(
            @{CompletionText = "TextReader"; ResultType = "Type"}
            )
        TestInput = 'using namespace System.IO; [TextR'
    },
    @{
        ExpectedResults = @(
            @{CompletionText = "Alias"; ResultType = "Type"}
            )
        TestInput = '[aliasatt'
    },
    @{
        ExpectedResults = @(
            @{CompletionText = "string"; ResultType = "Type"}
            )
        TestInput = 'using namespace System; [strin'
    } | Get-CompletionTestCaseData | Test-Completions
}

Describe "Additional type name completion tests" -Tags "CI" {
    @{
        ExpectedResults = @(
            @{CompletionText = "System"; ResultType = "Namespace"}
            @{CompletionText = "System.Security.AccessControl.SystemAcl"; ResultType = "Type"}
            )
        TestInput = 'Get-Command -ParameterType System'
    },
    @{
        ExpectedResults = @(
            @{CompletionText = "System.Action"; ResultType = "Type"}
            @{CompletionText = "System.Activator"; ResultType = "Type"}
            )
        TestInput = 'Get-Command -ParameterType System.'
    },
    @{
        ExpectedResults = @(
            @{CompletionText = "System.Collections.Generic.LinkedList"; ResultType = "Type"; ListItemText = "LinkedList<>"; ToolTip = "System.Collections.Generic.LinkedList[T]"}
            @{CompletionText = "System.Collections.Generic.LinkedListNode"; ResultType = "Type"; ListItemText = "LinkedListNode<>"; ToolTip = "System.Collections.Generic.LinkedListNode[T]"}
            @{CompletionText = "System.Collections.Generic.List"; ResultType = "Type"; ListItemText = "List<>"; ToolTip = "System.Collections.Generic.List[T]"}
            )
        TestInput = 'Get-Command -ParameterType System.Collections.Generic.Li'
    },
    @{
        ExpectedResults = @(
            @{CompletionText = "System.Collections.Generic.Dictionary"; ResultType = "Type"; ListItemText = "Dictionary<>"; ToolTip = "System.Collections.Generic.Dictionary[T1, T2]"}
            )
        TestInput = 'Get-Command -ParameterType System.Collections.Generic.Dic'
    } | Get-CompletionTestCaseData | Test-Completions
}
