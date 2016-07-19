
#
# Run the new parser, return either errors or the ast
#
function Get-ParseResults
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$True,Mandatory=$True)]
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
        [Parameter(ValueFromPipeline=$True,Mandatory=$True)]
        [string]$src
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
# Run the new parser, check the errors against the expected errors
#
function Test-Error
{
    [CmdletBinding()]
    param([string]$src, [string[]]$expectedErrors, [int[]]$expectedOffsets)

    Assert ($expectedErrors.Count -eq $expectedOffsets.Count) "Test case error"

    $errors = Get-ParseResults -Src $src


    if ($null -ne $errors)
    {
        Assert ($errors.Count -eq $expectedErrors.Count) "Expected $($expectedErrors.Count) errors, got $($errors.Count)"
        for ($i = 0; $i -lt $errors.Count; ++$i)
        {
            $err = $errors[$i]
            Assert ($expectedErrors[$i] -eq $err.ErrorId) ("Unexpected error: {0,-30}{1}" -f ("$($err.ErrorId):",
                      (position_message $err.Extent.StartScriptPosition)))
            Assert ($expectedOffsets[$i] -eq $err.Extent.StartScriptPosition.Offset) `
                "Expected position: $($expectedOffsets[$i]), got $($err.Extent.StartScriptPosition.Offset)"
       }
    }
    else
    {
        Assert $false "Expected errors but didn't receive any."
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
        [switch]$SkipAndCheckRuntimeError
    )

    Context "Parse error expected: <<$src>>" {
        # Test case error if this fails
        $expectedErrors.Count | Should Be $expectedOffsets.Count
        
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

        It "Error count" { $errors.Count | Should Be $expectedErrors.Count }
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
            It "Error Id" { $errorId | Should Be $expectedErrors[$i] }
            It "Error position" -Pending:$SkipAndCheckRuntimeError { $err.Extent.StartScriptPosition.Offset | Should Be $expectedOffsets[$i] }
       }
    }
}


function Flatten-Ast
{
    [CmdletBinding()]
    param([System.Management.Automation.Language.Ast] $ast)

    $ast
    $ast | gm -type property | ? { ($prop = $_.Name) -ne 'Parent' } | % { 
        $ast.$prop | ? { $_ -is [System.Management.Automation.Language.Ast] } | % { Flatten-Ast $_ }
    }
}

function Test-ErrorStmt
{
    param([string]$src, [string]$errorStmtExtent)

    $ast = Get-ParseResults $src -Ast
    $asts = @(Flatten-Ast $ast.EndBlock.Statements[0])

    Assert ($asts[0] -is [System.Management.Automation.Language.ErrorStatementAst]) "Expected error statement"
    Assert ($asts.Count -eq $args.Count + 1) "Incorrect number of nested asts"
    Assert ($asts[0].Extent.Text -eq $errorStmtExtent) "Error statement expected <$errorStmtExtent>, got <$($asts[0].Extent.Text)>"
    for ($i = 0; $i -lt $args.Count; ++$i)
    {
        Assert ($asts[$i + 1].Extent.Text -eq $args[$i]) "Nested ast incorrect: <$($asts[$i+1].Extent.Text)>, expected <$($args[$i])>"
    }
}

function Test-Ast
{
    param([string]$src)

    $ast = Get-ParseResults $src -Ast
    $asts = @(Flatten-Ast $ast)
    Assert ($asts.Count -eq $args.Count) "Incorrect number of nested asts, got $($asts.Count), expected $($args.Count)"
    for ($i = 0; $i -lt $args.Count; ++$i)
    {
        Assert ($asts[$i].Extent.Text -eq $args[$i]) "Nested ast incorrect: <$($asts[$i].Extent.Text)>, expected <$($args[$i])>"
    }
}

Export-ModuleMember -Function Test-Error, Test-ErrorStmt, Test-Ast, ShouldBeParseError, Get-ParseResults, Get-RuntimeError
