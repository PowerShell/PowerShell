# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using namespace System.Management.Automation.Language

Describe "StaticParameterBinder tests" -Tags "CI" {
    BeforeAll {
        $testCases = @(
            @{
                Source = 'Get-Alias abc'
                Description = 'string constant value'
                BoundParametersCount = 1
                ExceptionCount = 0
                ValidateScript = {
                    param ($result)
                    $result.BoundParameters.Name.ConstantValue | Should -Be 'abc'
                }
            },
            @{
                Source = 'Get-Alias {abc}'
                Description = 'script block value'
                BoundParametersCount = 1
                ExceptionCount = 0
                ValidateScript = {
                    param ($result)
                    $result.BoundParameters.Name.Value | Should -BeOfType ([ScriptBlockExpressionAst].FullName)
                    $result.BoundParameters.Name.Value.Extent.Text | Should -Be '{abc}'
                }
            },
            @{
                Source = 'Get-Alias -Path abc'
                Description = 'parameter -path not found'
                BoundParametersCount = 0
                ExceptionCount = 1
                ValidateScript = {
                    param ($result)
                    $result.BindingExceptions.Path.CommandElement.Extent.Text | Should -Be '-Path'
                    $result.BindingExceptions.Path.BindingException.ErrorId | Should -Be 'NamedParameterNotFound'
                }
            },
            @{
                Source = 'Get-Alias -Path -Name:abc'
                Description = 'parameter -name found while -path not found'
                BoundParametersCount = 1
                ExceptionCount = 1
                ValidateScript = {
                    param ($result)
                    $result.BoundParameters.Name.ConstantValue | Should -Be 'abc'
                    $result.BindingExceptions.Path.CommandElement.Extent.Text | Should -Be '-Path'
                    $result.BindingExceptions.Path.BindingException.ErrorId | Should -Be 'NamedParameterNotFound'
                }
            },
            @{
                Source = 'Get-Alias -Name -abc'
                Description = 'parameter -abc should be used as value'
                BoundParametersCount = 1
                ExceptionCount = 0
                ValidateScript = {
                    param ($result)
                    $result.BoundParameters.Name.Value | Should -BeOfType ([CommandParameterAst].FullName)
                    $result.BoundParameters.Name.Value.Extent.Text | Should -Be '-abc'
                }
            },
            @{
                Source = 'Get-Alias aa bb'
                Description = 'unbound positional parameter bb'
                BoundParametersCount = 1
                ExceptionCount = 1
                ValidateScript = {
                    param ($result)
                    $result.BoundParameters.Name.ConstantValue | Should -Be 'aa'
                    $result.BindingExceptions.bb.BindingException.ErrorId | Should -Be 'PositionalParameterNotFound'
                }
            },
            @{
                Source = 'Get-Alias aa,bb,cc'
                Description = 'array argument'
                BoundParametersCount = 1
                ExceptionCount = 0
                ValidateScript = {
                    param ($result)
                    $result.BoundParameters.Name.Value | Should -BeOfType ([ArrayLiteralAst].FullName)
                    $result.BoundParameters.Name.Value.Extent.Text | Should -Be 'aa,bb,cc'
                }
            },
            @{
                Source = 'Get-ChildItem -Name abc -rec'
                Description = 'switch params and positional param'
                BoundParametersCount = 3
                ExceptionCount = 0
                ValidateScript = {
                    param ($result)
                    $result.BoundParameters.Name.ConstantValue | Should -BeTrue
                    $result.BoundParameters.Recurse.ConstantValue | Should -BeTrue
                    $result.BoundParameters.Path.ConstantValue | Should -Be 'abc'
                }
            },
            @{
                Source = 'Get-ChildItem -Name -f'
                Description = 'switch parameter -name found while ambiguous parameter -f'
                BoundParametersCount = 1
                ExceptionCount = 1
                ValidateScript = {
                    param ($result)
                    $result.BoundParameters.Name.ConstantValue | Should -BeTrue
                    $result.BindingExceptions.f.CommandElement.Extent.Text | Should -Be '-f'
                    $result.BindingExceptions.f.BindingException.ErrorId | Should -Be 'AmbiguousParameter'
                }
            },
            @{
                Source = 'Get-ChildItem -Path -f'
                Description = 'non-switch parameter -path followed by ambiguous parameter -f'
                BoundParametersCount = 0
                ExceptionCount = 1
                ValidateScript = {
                    param ($result)
                    $result.BindingExceptions.f.CommandElement.Extent.Text | Should -Be '-f'
                    $result.BindingExceptions.f.BindingException.ErrorId | Should -Be 'AmbiguousParameter'
                }
            }
        )
    }

    It "<Description>: '<Source>'" -TestCases $testCases {
        param ($Source, $BoundParametersCount, $ExceptionCount, $ValidateScript)

        $ast = [Parser]::ParseInput($Source, [ref]$null, [ref]$null)
        $cmdAst = $ast.Find({$args[0] -is [CommandAst]}, $false)
        $result = [StaticParameterBinder]::BindCommand($cmdAst)

        $result.BoundParameters.Count | Should -Be $BoundParametersCount
        $result.BindingExceptions.Count | Should -Be $ExceptionCount
        . $ValidateScript $result
    }
}
