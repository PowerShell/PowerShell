# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
#
# Run the new parser, return either errors or the ast
#

function Get-ParseResults
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true,Mandatory=$true)]
        [string]$src,
        [switch]$Ast
    )

    $errors = $null
    $result = [System.Management.Automation.Language.Parser]::ParseInput($src, [ref]$null, [ref]$errors)
    if ($Ast) { $result } else { ,$errors }
}

#
# Run script and return errors
#
function Get-RuntimeError
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true,Mandatory=$true)][string]$src
    )

    $errors = $null
    try
    {
        [scriptblock]::Create($src).Invoke() > $null
    }
    catch
    {
        return $_.Exception.InnerException.ErrorRecord
    }
}

function position_message
{
    param($position)

    if ($position.Line.Length -lt $position.ColumnNumber)
    {
        $position.Line + " <<<<"
    }
    else
    {
        $position.Line.Insert($position.ColumnNumber, " <<<<")
    }
}

#
# Pester friendly version of Test-Error
#
function ShouldBeParseError
{
    [CmdletBinding()]
    param(
        [string]$src,
        [string[]]$expectedErrors,
        [int[]]$expectedOffsets,
        # This is a temporary solution after moving type creation from parse time to runtime
        [switch]$SkipAndCheckRuntimeError,
        # for test coverarage purpose, tests validate columnNumber or offset
        [switch]$CheckColumnNumber
    )

    $safeSrc = $src -replace '<', '&lt;' -replace '>', '&gt;'
    Context "Parse error expected: '$safeSrc'" {
        if ($SkipAndCheckRuntimeError)
        {
            It "error should happen at parse time, not at runtime" -Skip {}
            $errors = Get-RuntimeError -Src $src
            $expectedErrors = ,@($expectedErrors)[0]
            $expectedOffsets = ,@($expectedOffsets)[0]
        }
        else
        {
            $errors = Get-ParseResults -Src $src
        }

        # Pre-compute all values during discovery and store only simple data
        $iterCases = @()
        for ($i = 0; $i -lt $expectedErrors.Count; $i++) {
            $err = $errors[$i]
            if ($SkipAndCheckRuntimeError) {
                $eid = $err.FullyQualifiedErrorId
            } else {
                $eid = $err.ErrorId
            }
            $pos = $err.Extent.StartScriptPosition.Offset
            if ($CheckColumnNumber) { $pos = $err.Extent.StartScriptPosition.ColumnNumber }

            $iterCases += @{
                i = $i
                expectedErrorId = $expectedErrors[$i]
                expectedOff = $expectedOffsets[$i]
                actualErrorId = $eid
                actualPos = $pos
            }
        }

        It "Error count" -TestCases @(@{
            actualCount = $errors.Count
            expectedCount = $expectedErrors.Count
        }) {
            param($actualCount, $expectedCount)
            $actualCount | Should -Be $expectedCount
        }

        It "Error Id (iteration:<i>)" -TestCases $iterCases {
            param($i, $expectedErrorId, $actualErrorId)
            $actualErrorId | Should -Be $expectedErrorId
        }

        It "Error position (iteration:<i>)" -Pending:([bool]$SkipAndCheckRuntimeError) -TestCases $iterCases {
            param($i, $expectedOff, $actualPos)
            $actualPos | Should -Be $expectedOff
        }
    }
}

function Flatten-Ast
{
    [CmdletBinding()]
    param([System.Management.Automation.Language.Ast] $ast)

    $ast
    $ast | gm -type property | Where-Object { ($prop = $_.Name) -ne 'Parent' } | ForEach-Object {
        $ast.$prop | Where-Object { $_ -is [System.Management.Automation.Language.Ast] } | ForEach-Object { Flatten-Ast $_ }
    }
}

function Test-ErrorStmt
{
    param([string]$src, [string]$errorStmtExtent)
    $a = $args
    $safeSrc = $src -replace '<', '&lt;' -replace '>', '&gt;'
    Context "Error Statement expected: '$safeSrc'" {
        $ast = Get-ParseResults $src -Ast
        $asts = @(Flatten-Ast $ast.EndBlock.Statements[0])

        $astsTypeName = $asts[0].GetType().FullName
        $astsCount = $asts.Count
        $astsFirstText = $asts[0].Extent.Text
        $expCount = $a.Count + 1

        $argCases = @()
        for ($i = 0; $i -lt $a.Count; $i++) {
            $argCases += @{
                Index = $i + 1
                ExpectedText = $a[$i]
                ActualText = $asts[$i + 1].Extent.Text
            }
        }

        It 'Type is ErrorStatementAst' -TestCases @(@{ tn = $astsTypeName }) {
            param($tn)
            $tn | Should -Be 'System.Management.Automation.Language.ErrorStatementAst'
        }
        It "`$asts.count" -TestCases @(@{ ac = $astsCount; ec = $expCount }) {
            param($ac, $ec)
            $ac | Should -Be $ec
        }
        It "`$asts[0].Extent.Text" -TestCases @(@{ at = $astsFirstText; et = $errorStmtExtent }) {
            param($at, $et)
            $at | Should -Be $et
        }
        if ($argCases.Count -gt 0) {
            It "`$asts[<Index>].Extent.Text" -TestCases $argCases {
                param($Index, $ExpectedText, $ActualText)
                $ActualText | Should -Be $ExpectedText
            }
        }
    }
}

function Test-Ast
{
    param([string]$src)
    $a = $args
    $ast = Get-ParseResults $src -Ast
    $asts = @(Flatten-Ast $ast)

    $astsCount = $asts.Count
    $argCases = @()
    for ($i = 0; $i -lt $a.Count; $i++) {
        $argCases += @{
            Index = $i
            ExpectedText = $a[$i]
            ActualText = $asts[$i].Extent.Text
        }
    }

    $safeSrc = $src -replace '<', '&lt;' -replace '>', '&gt;'
    Context "Ast Validation: '$safeSrc'" {
        It "`$asts.count" -TestCases @(@{ ac = $astsCount; ec = $a.Count }) {
            param($ac, $ec)
            $ac | Should -Be $ec
        }
        if ($argCases.Count -gt 0) {
            It "`$asts[<Index>].Extent.Text" -TestCases $argCases {
                param($Index, $ExpectedText, $ActualText)
                $ActualText | Should -Be $ExpectedText
            }
        }
    }
}

## ErrorStatement is special for SwitchStatement
    function Test-ErrorStmtForSwitchFlag
    {
        param([string]$src, [string]$flagName)
        $a = $args
        $ast = Get-ParseResults $src -Ast
        $switchAst = $ast.EndBlock.Statements[0]
        $asts = @(Flatten-Ast $switchAst.Flags[$flagName].Item2)

        $switchTypeName = $switchAst.GetType().FullName
        $hasKey = $switchAst.Flags.ContainsKey($flagName)
        $astsCount = $asts.Count
        $argCases = @()
        for ($i = 0; $i -lt $a.Count; $i++) {
            $argCases += @{
                Index = $i
                ExpectedText = $a[$i]
                ActualText = $asts[$i].Extent.Text
            }
        }

        $safeSrc = $src -replace '<', '&lt;' -replace '>', '&gt;'
        Context "Ast Validation: '$safeSrc'" {
            It 'Type is ErrorStatementAst' -TestCases @(@{ tn = $switchTypeName }) {
                param($tn)
                $tn | Should -Be 'System.Management.Automation.Language.ErrorStatementAst'
            }
            It "Has flag key '$flagName'" -TestCases @(@{ hk = $hasKey }) {
                param($hk)
                $hk | Should -BeTrue
            }
            It "`$asts.count" -TestCases @(@{ ac = $astsCount; ec = $a.Count }) {
                param($ac, $ec)
                $ac | Should -Be $ec
            }
            if ($argCases.Count -gt 0) {
                It "`$asts[<Index>].Extent.Text" -TestCases $argCases {
                    param($Index, $ExpectedText, $ActualText)
                    $ActualText | Should -Be $ExpectedText
                }
            }
        }
    }

Export-ModuleMember -Function Test-ErrorStmt, Test-Ast, ShouldBeParseError, Get-ParseResults, Get-RuntimeError, Test-ErrorStmtForSwitchFlag
