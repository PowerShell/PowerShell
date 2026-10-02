# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
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
                    if ( $expected.CompletionText -Match "System.Management.Automation.PerformanceData|System.Management.Automation.Security" ) { $skip = $true }
                    It ($expected.CompletionText) -Skip:$skip {
                        $expected.Found | Should -BeTrue
                    }
                }

                foreach ($notExpected in $test.NotExpectedResults)
                {
                    It "Not expected: $notExpected" {
                        foreach ($result in $results.CompletionMatches)
                        {
                            $result.CompletionText | Should -Not -Be $notExpected
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


class NumberCompleter : IArgumentCompleter
{

    [int] $From
    [int] $To
    [int] $Step

    NumberCompleter([int] $from, [int] $to, [int] $step)
    {
        if ($from -gt $to) {
            throw [ArgumentOutOfRangeException]::new("from")
        }
        $this.From = $from
        $this.To = $to
        $this.Step = if($step -lt 1) { 1 } else { $step }
    }

    [IEnumerable[CompletionResult]] CompleteArgument(
        [string] $CommandName,
        [string] $parameterName,
        [string] $wordToComplete,
        [CommandAst] $commandAst,
        [IDictionary] $fakeBoundParameters)
    {
        $resultList = [List[CompletionResult]]::new()
        $local:to = $this.To
        for ($i = $this.From; $i -le $to; $i += $this.Step) {
            if ($i.ToString().StartsWith($wordToComplete, [System.StringComparison]::Ordinal)) {
                $num = $i.ToString()
                $resultList.Add([CompletionResult]::new($num, $num, "ParameterValue", $num))
            }
        }

        return $resultList
    }
}

class NumberCompletionAttribute : ArgumentCompleterAttribute, IArgumentCompleterFactory
{
    [int] $From
    [int] $To
    [int] $Step

    NumberCompletionAttribute([int] $from, [int] $to)
    {
        $this.From = $from
        $this.To = $to
        $this.Step = 1
    }

    [IArgumentCompleter] Create() { return [NumberCompleter]::new($this.From, $this.To, $this.Step) }
}

function FactoryCompletionAdd {
    param(
        [NumberCompletion(0, 50, Step = 5)]
        [int] $Number
    )
}

Describe "Factory based extensible completion" -Tags "CI" {
    @{
        ExpectedResults = @(
            @{CompletionText = "5"; ResultType = "ParameterValue" }
            @{CompletionText = "50"; ResultType = "ParameterValue" }
        )
        TestInput       = 'FactoryCompletionAdd -Number 5'
    } | Get-CompletionTestCaseData | Test-Completions
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

Describe "Test completion of parameters for native commands" -Tags "CI" {
    Register-ArgumentCompleter -Native -CommandName foo -ScriptBlock {
        Param($wordToComplete)

        @("-dir", "-verbose", "-help", "-version") |
        Where-Object {
            $_ -Match "$wordToComplete*"
        } |
        ForEach-Object {
            [CompletionResult]::new($_, $_, [CompletionResultType]::ParameterName, $_)
        }
    }

    @{
        ExpectedResults = @(
            @{CompletionText = "-version"; ResultType = "ParameterName"}
            @{CompletionText = "-verbose"; ResultType = "ParameterName"}
            @{CompletionText = "-dir"; ResultType = "ParameterName"}
            @{CompletionText = "-help"; ResultType = "ParameterName"}
        )
        TestInput = 'foo -'
    } | Get-CompletionTestCaseData | Test-Completions

    @{
        ExpectedResults = @(
            @{CompletionText = "-version"; ResultType = "ParameterName"}
            @{CompletionText = "-verbose"; ResultType = "ParameterName"}
        )
        TestInput = 'foo -v'
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

Describe "ArgumentCompletionsAttribute tests" -Tags "CI" {

    BeforeAll {
        function TestArgumentCompletionsAttribute
        {
            param(
                [ArgumentCompletions("value1", "value2", "value3")]
                $Alpha,
                $Beta
            )
        }

        function TestArgumentCompletionsAttribute1
        {
            param(
                [ArgumentCompletionsAttribute("value1", "value2", "value3")]
                $Alpha,
                $Beta
            )
        }

        $cmdletSrc=@'
        using System;
        using System.Management.Automation;
        using System.Collections.Generic;

        namespace Test.A {

            [Cmdlet(VerbsCommon.Get, "ArgumentCompletions")]
            public class TestArgumentCompletionsAttributeCommand : PSCmdlet
            {
                [Parameter]
                [ArgumentCompletions("value1", "value2", "value3")]
                public string Param1;

                protected override void EndProcessing()
                {
                    WriteObject(Param1);
                }
            }

            [Cmdlet(VerbsCommon.Get, "ArgumentCompletions1")]
            public class TestArgumentCompletionsAttributeCommand1 : PSCmdlet
            {
                [Parameter]
                [ArgumentCompletionsAttribute("value1", "value2", "value3")]
                public string Param1;

                protected override void EndProcessing()
                {
                    WriteObject(Param1);
                }
            }
        }
'@
        $cls = Add-Type -TypeDefinition $cmdletSrc -PassThru | Select-Object -First 1
        $testModule = Import-Module $cls.Assembly -PassThru

        $testCasesScript = @(
            @{ attributeName = "ArgumentCompletions"         ; cmdletName = "TestArgumentCompletionsAttribute"  },
            @{ attributeName = "ArgumentCompletionsAttribute"; cmdletName = "TestArgumentCompletionsAttribute1" }
        )

        $testCasesCSharp = @(
            @{ attributeName = "ArgumentCompletions"         ; cmdletName = "Get-ArgumentCompletions"  },
            @{ attributeName = "ArgumentCompletionsAttribute"; cmdletName = "Get-ArgumentCompletions1" }
        )
    }

    AfterAll {
        Remove-Module -ModuleInfo $testModule
    }

    It "<attributeName> works in script" -TestCases $testCasesScript {
        param($attributeName, $cmdletName)

        $line = "$cmdletName -Alpha val"
        $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
        $res.CompletionMatches.Count | Should -Be 3
        $res.CompletionMatches.CompletionText -join " " | Should -BeExactly "value1 value2 value3"
        { TestArgumentCompletionsAttribute -Alpha unExpectedValue } | Should -Not -Throw
    }

    It "<attributeName> works in C#" -TestCases $testCasesCSharp {
        param($attributeName, $cmdletName)

        $line = "$cmdletName -Param1 val"
        $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
        $res.CompletionMatches.Count | Should -Be 3
        $res.CompletionMatches.CompletionText -join " " | Should -BeExactly "value1 value2 value3"
        { TestArgumentCompletionsAttribute -Param1 unExpectedValue } | Should -Not -Throw
    }
}


Describe "Get-ArgumentCompleter cmdlet" -Tags "CI" {
    BeforeAll {
        # Register test completers
        Register-ArgumentCompleter -CommandName TestGetCmd -ParameterName TestParam -ScriptBlock { "test1" }
        Register-ArgumentCompleter -ParameterName GlobalTestParam -ScriptBlock { "global" }
        Register-ArgumentCompleter -Native -CommandName testnative -ScriptBlock { "native1" }
    }

    AfterAll {
        # Clean up
        Unregister-ArgumentCompleter -CommandName TestGetCmd -ParameterName TestParam
        Unregister-ArgumentCompleter -ParameterName GlobalTestParam
        Unregister-ArgumentCompleter -Native -CommandName testnative
    }

    It "Returns PowerShell completers by default" {
        $results = Get-ArgumentCompleter
        $results | Should -Not -BeNullOrEmpty
        $results | ForEach-Object { $_.Type | Should -Be 'PowerShell' }
    }

    It "Returns native completers with -Native switch" {
        $results = Get-ArgumentCompleter -Native
        $results | Should -Not -BeNullOrEmpty
        $results | ForEach-Object { $_.Type | Should -BeIn @('Native', 'NativeFallback') }
    }

    It "Filters by CommandName" {
        $results = Get-ArgumentCompleter -CommandName TestGetCmd
        $results | Should -Not -BeNullOrEmpty
        $results.CommandName | Should -Contain 'TestGetCmd'
    }

    It "Filters by ParameterName" {
        $results = Get-ArgumentCompleter -ParameterName TestParam
        $results | Should -Not -BeNullOrEmpty
        $results.ParameterName | Should -Contain 'TestParam'
    }

    It "Supports wildcards in CommandName" {
        $results = Get-ArgumentCompleter -CommandName "TestGet*"
        $results | Should -Not -BeNullOrEmpty
        $results.CommandName | Should -Contain 'TestGetCmd'
    }

    It "Supports wildcards in ParameterName" {
        $results = Get-ArgumentCompleter -ParameterName "*TestParam"
        $results | Should -Not -BeNullOrEmpty
    }

    It "Returns ArgumentCompleterInfo objects with correct properties" {
        $results = Get-ArgumentCompleter -CommandName TestGetCmd
        $result = $results | Where-Object { $_.CommandName -eq 'TestGetCmd' }
        $result | Should -Not -BeNullOrEmpty
        $result.CommandName | Should -Be 'TestGetCmd'
        $result.ParameterName | Should -Be 'TestParam'
        $result.ScriptBlock | Should -Not -BeNullOrEmpty
        $result.Type | Should -Be 'PowerShell'
    }

    It "Returns native completer with correct Type" {
        $results = Get-ArgumentCompleter -Native -CommandName testnative
        $result = $results | Where-Object { $_.CommandName -eq 'testnative' }
        $result | Should -Not -BeNullOrEmpty
        $result.Type | Should -Be 'Native'
        $result.ParameterName | Should -BeNullOrEmpty
    }
}

Describe "Unregister-ArgumentCompleter cmdlet" -Tags "CI" {
    It "Removes PowerShell completer by command and parameter" {
        Register-ArgumentCompleter -CommandName UnregisterTest -ParameterName TestParam -ScriptBlock { "test" }
        $before = Get-ArgumentCompleter -CommandName UnregisterTest
        $before | Should -Not -BeNullOrEmpty

        Unregister-ArgumentCompleter -CommandName UnregisterTest -ParameterName TestParam

        $after = Get-ArgumentCompleter -CommandName UnregisterTest -ParameterName TestParam
        $after | Should -BeNullOrEmpty
    }

    It "Removes global parameter completer" {
        Register-ArgumentCompleter -ParameterName UnregisterGlobalParam -ScriptBlock { "global" }
        $before = Get-ArgumentCompleter -ParameterName UnregisterGlobalParam
        $before | Should -Not -BeNullOrEmpty

        Unregister-ArgumentCompleter -ParameterName UnregisterGlobalParam

        $after = Get-ArgumentCompleter -ParameterName UnregisterGlobalParam
        $after | Should -BeNullOrEmpty
    }

    It "Removes native command completer" {
        Register-ArgumentCompleter -Native -CommandName unregisternative -ScriptBlock { "native" }
        $before = Get-ArgumentCompleter -Native -CommandName unregisternative
        $before | Should -Not -BeNullOrEmpty

        Unregister-ArgumentCompleter -Native -CommandName unregisternative

        $after = Get-ArgumentCompleter -Native -CommandName unregisternative
        $after | Should -BeNullOrEmpty
    }

    It "Removes native fallback completer" {
        Register-ArgumentCompleter -NativeFallback -ScriptBlock { "fallback" }
        $before = Get-ArgumentCompleter -Native | Where-Object { $_.Type -eq 'NativeFallback' }
        $before | Should -Not -BeNullOrEmpty

        Unregister-ArgumentCompleter -NativeFallback

        $after = Get-ArgumentCompleter -Native | Where-Object { $_.Type -eq 'NativeFallback' }
        $after | Should -BeNullOrEmpty
    }

    It "Does not error when removing non-existent completer" {
        { Unregister-ArgumentCompleter -CommandName NonExistent -ParameterName NonExistent } | Should -Not -Throw
    }
}
