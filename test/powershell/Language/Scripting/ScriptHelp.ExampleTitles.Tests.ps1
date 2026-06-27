# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
$ProgressPreference = "SilentlyContinue"

Describe 'get-help comment-based help example titles' -Tags "CI" {

    Context 'get-help .EXAMPLE with title on the same line' {
        BeforeAll {
            function helpFuncWithExampleTitles {
                <#
                  .SYNOPSIS
                  A function with titled examples.

                  .EXAMPLE Retrieving an item from a directory
                  Get-Item -Path C:\Temp

                  Retrieves the item at C:\Temp

                  .EXAMPLE Listing files in a folder
                  Get-ChildItem -Path C:\Temp

                  Lists all files and folders in C:\Temp
                #>
                param()
            }
            $script:x = Get-Help helpFuncWithExampleTitles
        }

        It 'should have 2 examples' { $script:x.examples.example.Count | Should -Be 2 }
        It 'example 1 title should contain custom title' { $script:x.examples.example[0].title | Should -BeLike '*EXAMPLE 1: Retrieving an item from a directory*' }
        It 'example 2 title should contain custom title' { $script:x.examples.example[1].title | Should -BeLike '*EXAMPLE 2: Listing files in a folder*' }
        It 'example 1 code' { $script:x.examples.example[0].code | Should -BeExactly 'Get-Item -Path C:\Temp' }
        It 'example 1 remarks' { $script:x.examples.example[0].remarks[0].text | Should -BeExactly 'Retrieves the item at C:\Temp' }
        It 'example 2 code' { $script:x.examples.example[1].code | Should -BeExactly 'Get-ChildItem -Path C:\Temp' }
        It 'example 2 remarks' { $script:x.examples.example[1].remarks[0].text | Should -BeExactly 'Lists all files and folders in C:\Temp' }
    }

    Context 'get-help .EXAMPLE mixed titled and untitled examples' {
        BeforeAll {
            function helpFuncMixedExamples {
                <#
                  .SYNOPSIS
                  A function with mixed titled and untitled examples.

                  .EXAMPLE
                  Get-Item -Path C:\Temp

                  Retrieves the item at C:\Temp

                  .EXAMPLE Custom title for second example
                  Get-ChildItem -Path C:\Temp

                  Lists all files and folders in C:\Temp

                  .EXAMPLE
                  Remove-Item -Path C:\Temp\test.txt

                  Removes a file
                #>
                param()
            }
            $script:x = Get-Help helpFuncMixedExamples
        }

        It 'should have 3 examples' { $script:x.examples.example.Count | Should -Be 3 }
        It 'example 1 title should be untitled' { $script:x.examples.example[0].title | Should -BeLike '*EXAMPLE 1 -*' }
        It 'example 1 title should not contain colon' { $script:x.examples.example[0].title | Should -Not -BeLike '*EXAMPLE 1:*' }
        It 'example 2 title should contain custom title' { $script:x.examples.example[1].title | Should -BeLike '*EXAMPLE 2: Custom title for second example*' }
        It 'example 3 title should be untitled' { $script:x.examples.example[2].title | Should -BeLike '*EXAMPLE 3 -*' }
        It 'example 3 title should not contain colon' { $script:x.examples.example[2].title | Should -Not -BeLike '*EXAMPLE 3:*' }
        It 'example 1 code' { $script:x.examples.example[0].code | Should -BeExactly 'Get-Item -Path C:\Temp' }
        It 'example 2 code' { $script:x.examples.example[1].code | Should -BeExactly 'Get-ChildItem -Path C:\Temp' }
        It 'example 3 code' { $script:x.examples.example[2].code | Should -BeExactly 'Remove-Item -Path C:\Temp\test.txt' }
    }

    Context 'CommentHelpInfo.GetCommentBlock round-trips example titles' {
        It 'GetCommentBlock should include example title' {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(@'
function TestFunc {
<#
    .EXAMPLE Example Title
    Get-Process

    Gets all processes
#>
    param()
}
'@, [ref]$null, [ref]$null)

            $helpInfo = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $helpInfo.ExampleTitles[0] | Should -BeExactly 'Example Title'
            $helpInfo.Examples[0] | Should -BeLike '*Get-Process*'
            $commentBlock = $helpInfo.GetCommentBlock()
            $commentBlock | Should -BeLike '*.EXAMPLE Example Title*'
        }

        It 'untitled example should have empty title' {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(@'
function TestFunc {
<#
    .EXAMPLE
    Get-Process

    Gets all processes
#>
    param()
}
'@, [ref]$null, [ref]$null)

            $helpInfo = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $helpInfo.Examples.Count | Should -Be 1
            $helpInfo.ExampleTitles[0] | Should -BeExactly ''
            $helpInfo.Examples[0] | Should -BeLike '*Get-Process*'
        }

        It 'GetCommentBlock for untitled example should emit .EXAMPLE without title' {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(@'
function TestFunc {
<#
    .EXAMPLE
    Get-Process

    Gets all processes
#>
    param()
}
'@, [ref]$null, [ref]$null)

            $helpInfo = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $commentBlock = $helpInfo.GetCommentBlock()
            # Should have ".EXAMPLE" on its own line, not ".EXAMPLE " with trailing text
            $commentBlock | Should -Match '\.EXAMPLE\r?\n'
            $commentBlock | Should -Not -Match '\.EXAMPLE [^\r\n]'
        }

        It 'mixed titled and untitled examples preserve titles in order' {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(@'
function TestFunc {
<#
    .EXAMPLE
    Get-Process

    Gets all processes

    .EXAMPLE Second Example Title
    Get-Service

    Gets services

    .EXAMPLE
    Get-Item -Path C:\

    Gets an item
#>
    param()
}
'@, [ref]$null, [ref]$null)

            $helpInfo = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $helpInfo.Examples.Count | Should -Be 3
            $helpInfo.ExampleTitles[0] | Should -BeExactly ''
            $helpInfo.Examples[0] | Should -BeLike '*Get-Process*'
            $helpInfo.ExampleTitles[1] | Should -BeExactly 'Second Example Title'
            $helpInfo.Examples[1] | Should -BeLike '*Get-Service*'
            $helpInfo.ExampleTitles[2] | Should -BeExactly ''
            $helpInfo.Examples[2] | Should -BeLike '*Get-Item*'
        }

        It 'GetCommentBlock round-trips mixed titled and untitled examples' {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(@'
function TestFunc {
<#
    .EXAMPLE
    Get-Process

    Gets all processes

    .EXAMPLE My Custom Title
    Get-Service

    Gets services
#>
    param()
}
'@, [ref]$null, [ref]$null)

            $helpInfo = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $commentBlock = $helpInfo.GetCommentBlock()
            # The titled example should appear with its title
            $commentBlock | Should -BeLike '*.EXAMPLE My Custom Title*'
            # Re-parse and verify the data survives the round-trip
            $ast2 = [System.Management.Automation.Language.Parser]::ParseInput(
                "function TestFunc2 {`n$commentBlock`nparam()`n}", [ref]$null, [ref]$null)
            $helpInfo2 = $ast2.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $helpInfo2.Examples.Count | Should -Be 2
            $helpInfo2.ExampleTitles[0] | Should -BeExactly ''
            $helpInfo2.ExampleTitles[1] | Should -BeExactly 'My Custom Title'
        }

        It 'multiple titled examples preserve each title' {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(@'
function TestFunc {
<#
    .EXAMPLE First Title
    Get-Process

    Gets all processes

    .EXAMPLE Second Title
    Get-Service

    Gets services

    .EXAMPLE Third Title
    Get-Item

    Gets an item
#>
    param()
}
'@, [ref]$null, [ref]$null)

            $helpInfo = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $helpInfo.Examples.Count | Should -Be 3
            $helpInfo.ExampleTitles[0] | Should -BeExactly 'First Title'
            $helpInfo.ExampleTitles[1] | Should -BeExactly 'Second Title'
            $helpInfo.ExampleTitles[2] | Should -BeExactly 'Third Title'
            $helpInfo.Examples[0] | Should -BeLike '*Get-Process*'
            $helpInfo.Examples[1] | Should -BeLike '*Get-Service*'
            $helpInfo.Examples[2] | Should -BeLike '*Get-Item*'
        }

        It 'ExampleTitles count matches Examples count for all-untitled help' {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(@'
function TestFunc {
<#
    .EXAMPLE
    Get-Process

    .EXAMPLE
    Get-Service
#>
    param()
}
'@, [ref]$null, [ref]$null)

            $helpInfo = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $helpInfo.Examples.Count | Should -Be 2
            $helpInfo.ExampleTitles.Count | Should -Be $helpInfo.Examples.Count
            $helpInfo.ExampleTitles[0] | Should -BeExactly ''
            $helpInfo.ExampleTitles[1] | Should -BeExactly ''
        }

        It 'line-comment style supports titled .EXAMPLE' {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(@'
function TestFunc {
    # .SYNOPSIS
    #   Line-comment titled example.
    #
    # .EXAMPLE Line comment title
    #   Get-Process
    #
    #   Gets all processes
    param()
}
'@, [ref]$null, [ref]$null)

            $helpInfo = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $helpInfo.Examples.Count | Should -Be 1
            $helpInfo.ExampleTitles[0] | Should -BeExactly 'Line comment title'
            $helpInfo.Examples[0] | Should -BeLike '*Get-Process*'
        }

        It 'preserves a title that ends with a dash character' {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput(@'
function TestFunc {
<#
    .EXAMPLE Step 1 -
    Get-Process

    Gets processes
#>
    param()
}
'@, [ref]$null, [ref]$null)

            $helpInfo = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)[0].GetHelpContent()
            $helpInfo.ExampleTitles[0] | Should -BeExactly 'Step 1 -'
            $helpInfo.GetCommentBlock() | Should -BeLike '*.EXAMPLE Step 1 -*'
        }
    }

    Context 'backward-compatibility for untitled .EXAMPLE help' {
        BeforeAll {
            function helpFuncUntitledRegression {
                <#
                  .SYNOPSIS
                  Regression check for untitled examples.

                  .EXAMPLE
                  Get-Item -Path C:\Temp

                  Retrieves the item at C:\Temp
                #>
                param()
            }
            $script:x = Get-Help helpFuncUntitledRegression
        }

        It 'untitled example title contains no colon (unchanged from before)' {
            $script:x.examples.example[0].title | Should -Not -BeLike '*:*'
        }
        It 'untitled example title still contains EXAMPLE 1' {
            $script:x.examples.example[0].title | Should -BeLike '*EXAMPLE 1*'
        }
        It 'untitled example code is unchanged' {
            $script:x.examples.example[0].code | Should -BeExactly 'Get-Item -Path C:\Temp'
        }
    }
}
