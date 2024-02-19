# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Dynamic parameters" -Tags "CI" {
    context "Dynamic Parameter Basics" {
        BeforeAll {
            function Test-DynamicParameterBasics {
                [CmdletBinding()]
                param()

                dynamicParam {
                    $dynamicParams = [Management.Automation.RuntimeDefinedParameterDictionary]::new()
                    $dynamicParams.Add(
                        "Foo", [Management.Automation.RuntimeDefinedParameter]::new(
                            "Foo",
                            [int],
                            @(
                                $parameter = [Parameter]::new()
                                $parameter.Mandatory = $true
                                $parameter.Position = 0
                                $parameter
                            )
                        )
                    )
                    $dynamicParams.Add(
                        "Bar", [Management.Automation.RuntimeDefinedParameter]::new(
                            "Bar",
                            [string],
                            @(
                                $parameter = [Parameter]::new()
                                $parameter.Position = 1
                                $parameter
                            )
                        )
                    )
                    $dynamicParams
                }

                process {
                    [PSCustomObject]([Ordered]@{} + $PSBoundParameters)
                }
            }
        }
        it "Will bind to dynamic parameters" {
            $output = Test-DynamicParameterBasics -Foo 1 -bar 2
            $output.Foo | Should -be 1
            $output.bar | Should -be 2
        }

        it "Will allow dynamic parameters to be accessed from .Parameters" {
            (Get-Command Test-DynamicParameterBasics).Parameters["Foo"].Attributes |
                Where-Object { $_.Mandatory -is [bool] } |
                Select-Object -ExpandProperty Mandatory |
                Should -Be $true
            (Get-Command Test-DynamicParameterBasics).Parameters["Bar"] | Should -not -be $null
        }
    }

    context "Dynamic Parameters Using InvocationName" {
        BeforeAll {
            function Test-DynamicParameterWithSmartAlias {
                [CmdletBinding()]
                param()

                dynamicParam {
                    if ($MyInvocation.InvocationName -and
                        ($MyInvocation.InvocationName -ne $MyInvocation.MyCommand.Name)) {
                        $dynamicParams = [Management.Automation.RuntimeDefinedParameterDictionary]::new()
                        $dynamicParams.Add(
                        "$($MyInvocation.InvocationName)", [Management.Automation.RuntimeDefinedParameter]::new(
                            "$($MyInvocation.InvocationName)",
                            [switch],
                            @(
                                [Parameter]::new()
                            )
                        )
                        )
                        $dynamicParams
                    }

                }

                process {
                    [Ordered]@{} + $PSBoundParameters
                }
            }

            Set-Alias AliasingWithDynamicParameters Test-DynamicParameterWithSmartAlias
            Set-Alias ItIsPossibleToHaveMultipleAliasesToTheSameCommand Test-DynamicParameterWithSmartAlias
            Set-Alias YouCanUseCommandNamesToInfluenceDynamicParameters Test-DynamicParameterWithSmartAlias
        }

        it "Can have different parameters depending on what it is called" {
            AliasingWithDynamicParameters -AliasingWithDynamicParameters
            ItIsPossibleToHaveMultipleAliasesToTheSameCommand -ItIsPossibleToHaveMultipleAliasesToTheSameCommand
            YouCanUseCommandNamesToInfluenceDynamicParameters -YouCanUseCommandNamesToInfluenceDynamicParameters
        }
    }

    context "Emitting Dynamic Parameters" {
        BeforeAll {

            function Test-DynamicEmit {
                [CmdletBinding()]
                param($path)

                dynamicparam {
                    # $Input will contain the command elements of the current invocation.
                    # $_ will be the current command.

                    # Simply emit a few dynamic parameters.
                    # All dynamic emitted dynamic parameters will be joined into a dictionary.
                    [Management.Automation.RuntimeDefinedParameter]::new(
                        "Foo",
                        [string],
                        @([Parameter]::new())
                    )

                    [Management.Automation.RuntimeDefinedParameter]::new(
                        "Bar",
                        [string],
                        @([Parameter]::new())
                    )

                    [Management.Automation.RuntimeDefinedParameter]::new(
                        "Baz",
                        [string],
                        @([Parameter]::new())
                    )
                }

                process {
                    @($PSBoundParameters.Values)
                }
            }

            function Test-DynamicConditionalEmit {
                [CmdletBinding()]
                param($path)

                dynamicparam {
                    # $Input will contain the command elements of the current invocation.
                    # $_ will be the current command.
                    $pathToBe = @($input)[1]

                    # This is a simple and easily testable example, and not anywhere near complete parsing
                    # (this check will only work if $Path is assigned positionally)
                    if ($pathToBe -is [Management.Automation.Language.StringConstantExpressionAst]) {
                        $pathToBe = $pathToBe.Value
                    }

                    # If the path is not explicitly "badPath"
                    if ($pathToBe -ne "BadPath") {
                        # create a dynamic parameter
                        [Management.Automation.RuntimeDefinedParameter]::new(
                            "foo",
                            [int],
                            @([Parameter]::new())
                        )
                    }
                }

                process {
                    @($PSBoundParameters.Values)
                }
            }


        }

        it "Can emit dynamic parameters, rather than returning a RuntimeParameterDictionary" {
            Test-DynamicEmit -Foo 1 -Bar 2 -Baz 3 | Should -Be @(1,2,3)
        }

        it "Can conditionally emit a parameter" {
            Test-DynamicConditionalEmit -Foo 1 | Should -Be @(1)
        }

        it "Can conditionally _not_ emit a parameter" {
            { Test-DynamicConditionalEmit badpath -Foo 1 -ErrorAction stop } | Should -Throw
        }
    }
}
