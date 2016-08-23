Describe "Parallel foreach syntax" -Tags "CI" {

   Context 'Should be able to retrieve AST of parallel foreach, error in regular case' {
        $errors = @()
        $ast = [System.Management.Automation.Language.Parser]::ParseInput(
            'foreach -parallel ($foo in $bar) {}', [ref] $null, [ref] $errors)
        It '$errors.Count' { $errors.Count | Should Be 1 }
        It '$ast.EndBlock.Statements[0].Flags' { $ast.EndBlock.Statements[0].Flags | Should Be 'Parallel' }        
    }

    It 'Should be able to retrieve AST of parallel foreach, works in JobDefinition case' -Skip:$IsCoreCLR {
        . .\TestsOnWinFullOnly.ps1
        Run-TestOnWinFull "ForeachParallel:ASTOfParallelForeachOnWorkflow"
    }

    Context 'Supports newlines before and after' {
        $errors = @()
        $ast = [System.Management.Automation.Language.Parser]::ParseInput(
            "foreach `n-parallel `n(`$foo in `$bar) {}", [ref] $null, [ref] $null)
        It '$errors.Count' { $errors.Count | Should Be 0 }
        It '$ast.EndBlock.Statements[0].Flags' { $ast.EndBlock.Statements[0].Flags | Should Be 'Parallel' }        
    }

    Context 'Generates an error on invalid parameter' {
        $errors = @()
        $ast = [System.Management.Automation.Language.Parser]::ParseInput(
            'foreach -bogus ($input in $bar) { }', [ref]$null, [ref]$errors)
        It '$errors.Count' { $errors.Count | Should Be 1 }        
        It '$errors[0].ErrorId' { $errors[0].ErrorId | Should Be InvalidForeachFlag }
    }

    Context 'Generate an error on -parallel that is not a workflow' {
        $errors = @()
        $ast = [System.Management.Automation.Language.Parser]::ParseInput(
            'foreach -parallel ($input in $bar) { }', [ref]$null, [ref]$errors)
        It '$errors.Count' { $errors.Count | Should Be 1 }        
        It '$errors[0].ErrorId' { $errors[0].ErrorId | Should Be ParallelNotSupported }
    }
}