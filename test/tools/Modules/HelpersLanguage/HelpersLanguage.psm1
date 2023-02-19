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
        # for test coverage purpose, tests validate columnNumber or offset
        [switch]$CheckColumnNumber
    )

    Context "Parse error expected: <<$src>>" {
        # Test case error if this fails
        $expectedErrors.Count | Should -Be $expectedOffsets.Count

        if ($SkipAndCheckRuntimeError)
        {
            It "error should happen at parse time, not at runtime" -Skip {}
            $errors = Get-RuntimeError -Src $src
            # for runtime errors we will only get the first one
            $expectedErrors = ,$expectedErrors[0]
            $expectedOffsets = ,$expectedOffsets[0]
        }
        else
        {
            $errors = Get-ParseResults -Src $src
        }

        It "Error count" { $errors.Count | Should -Be $expectedErrors.Count }
        for ($i = 0; $i -lt $errors.Count; ++$i)
        {
            $err = $errors[$i]

            if ($SkipAndCheckRuntimeError)
            {
                $errorId = $err.FullyQualifiedErrorId
            }
            else
            {
                $errorId = $err.ErrorId
            }
            It "Error Id (iteration:$i)" { $errorId | Should -Be $expectedErrors[$i] }
            $actualPosition = $err.Extent.StartScriptPosition.Offset
            if ( $CheckColumnNumber ) { $actualPosition = $err.Extent.StartScriptPosition.ColumnNumber }
            It "Error position (iteration:$i)" -Pending:$SkipAndCheckRuntimeError { $actualPosition | Should -Be $expectedOffsets[$i] }
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
    Context "Error Statement expected: <<$src>>" {
        $ast = Get-ParseResults $src -Ast
        $asts = @(Flatten-Ast $ast.EndBlock.Statements[0])

        It 'Type is ErrorStatementAst' { $asts[0] | Should BeOfType System.Management.Automation.Language.ErrorStatementAst }
        It "`$asts.count" { $asts.Count | Should -Be ($a.Count + 1) }
        It "`$asts[0].Extent.Text" { $asts[0].Extent.Text | Should -Be $errorStmtExtent }
        for ($i = 0; $i -lt $a.Count; ++$i)
        {
            It "`$asts[$($i + 1)].Extent.Text" {  $asts[$i + 1].Extent.Text | Should -Be $a[$i] }
        }
    }
}

function Test-Ast
{
    param([string]$src)
    $a = $args
    $ast = Get-ParseResults $src -Ast
    $asts = @(Flatten-Ast $ast)
    Context "Ast Validation: <<$src>>" {
        It "`$asts.count" { $asts.Count | Should -Be $a.Count }
        for ($i = 0; $i -lt $a.Count; ++$i)
        {
            It "`$asts[$i].Extent.Text" { $asts[$i].Extent.Text | Should -Be $a[$i] }
        }
    }
}

## ErrorStatement is special for SwitchStatement
    function Test-ErrorStmtForSwitchFlag
    {
        param([string]$src, [string]$flagName)
        $a = $args
        $ast = Get-ParseResults $src -Ast
        $ast = $ast.EndBlock.Statements[0]
        Context "Ast Validation: <<$src>>" {
            $ast | Should BeOfType System.Management.Automation.Language.ErrorStatementAst
            $ast.Flags.ContainsKey($flagName) | Should -BeTrue

            $asts = @(Flatten-Ast $ast.Flags[$flagName].Item2)

            $asts.Count | Should -Be $a.Count
            for ($i = 0; $i -lt $a.Count; ++$i)
            {
                $asts[$i].Extent.Text | Should -Be $a[$i]
            }
        }
    }

Export-ModuleMember -Function Test-ErrorStmt, Test-Ast, ShouldBeParseError, Get-ParseResults, Get-RuntimeError, Test-ErrorStmtForSwitchFlag
