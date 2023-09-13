# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

New-Module PesterInterop {
    $script:isPester4 = (Get-Module Pester).Version -lt '5.0'

    if ($script:isPester4) {
        function BeforeDiscovery {
            param (
                [ScriptBlock]$ScriptBlock
            )

            . $ScriptBlock
        }
    }
}

Describe 'Comment Based Help' {
    BeforeAll {
        $ProgressPreference = 'SilentlyContinue'

        function Get-HelpField {
            <#
            .SYNOPSIS
                A utility function to get a the content of a specific field from help.
            #>
            [CmdletBinding(DefaultParameterSetName = 'ByCommandName')]
            param (
                [Parameter(Mandatory, Position = 1, ParameterSetName = 'ByCommandName')]
                [string]
                $CommandName,

                [Parameter(Mandatory, ParameterSetName = 'FromHelpInfo')]
                [PSTypeName('HelpInfo')]
                $HelpInfo,

                [Parameter(Mandatory)]
                [string]
                $Name
            )

            if ($CommandName) {
                $HelpInfo = Get-Help $CommandName -ErrorAction Ignore
            }

            $output = switch ($Name) {
                'DESCRIPTION'           { $HelpInfo.Description.Text }
                'NOTES'                 { $HelpInfo.alertSet.alert.Text }
                'LINKS'                 { $HelpInfo.relatedLinks.navigationLink }
                'LINKS,URI'             { $HelpInfo.relatedLinks.navigationLink.uri }
                'LINKS,TEXT'            { $HelpInfo.relatedLinks.navigationLink.linkText }
                'EXAMPLE'               { $HelpInfo.examples.example }
                'EXAMPLE,CODE'          { $HelpInfo.examples.example.code }
                'EXAMPLE,STRING'        {
                    foreach ($example in $HelpInfo.examples.example) {
                        $example | Out-String -Width 250
                    }
                }
                'INPUTS'                { $HelpInfo.inputTypes.inputType.type.name }
                'OUTPUTS'               { $HelpInfo.returnValues.returnValue.type.name }
                'PARAMETER'             { $HelpInfo.parameters.parameter }
                'PARAMETER,DESCRIPTION' { $HelpInfo.parameters.parameter.description.Text }
                'SYNTAX,ITEM'           { $HelpInfo.syntax.syntaxItem }
                'SYNTAX,PARAMETER'      { $HelpInfo.syntax.syntaxItem.parameter }
                'SYNTAX,STRING'         { $HelpInfo.syntax | Out-String -Width 250 }
                default                 { $HelpInfo.$Name }
            }

            foreach ($value in $output) {
                if ($value -is [string]) {
                    $value.Trim()
                } else {
                    $value
                }
            }
        }
    }

    Context 'Content discovery' {
        Context 'Function help' {
            Context 'Before function' {
                It 'Should find help content immediately above a function' {
                    . {
                        <#
                        .SYNOPSIS
                            An example function.
                        #>
                        function ExampleBeforeFunction { }
                    }

                    Get-HelpField 'ExampleBeforeFunction' -Name 'SYNOPSIS' | Should -Be 'An example function.'
                }

                It 'Should find help content separated by one line from a function' {
                    . {
                        <#
                        .SYNOPSIS
                            An example function with one empty line after help.
                        #>

                        function ExampleBeforeFunction { }
                    }

                    Get-HelpField 'ExampleBeforeFunction' -Name 'SYNOPSIS' | Should -Be 'An example function with one empty line after help.'
                }

                It 'Should not find help content separated by two or more lines from a function' {
                    . {
                        <#
                        .SYNOPSIS
                            An example function.
                        #>


                        function ExampleBeforeFunction { }
                    }

                    Get-HelpField 'ExampleBeforeFunction' -Name 'SYNOPSIS' | Should -Be 'ExampleBeforeFunction'
                }
            }

            Context 'Inside function' {
                It 'Should find help content at the start of the function' {
                    . {
                        function ExampleInsideFunction {
                            <#
                            .SYNOPSIS
                                An example function.
                            #>
                        }
                    }

                    Get-HelpField 'ExampleInsideFunction' -Name 'SYNOPSIS' | Should -Be 'An example function.'
                }

                It 'Should find help content anywhere before param' {
                    . {
                        function ExampleInsideFunction {


                            <#
                            .SYNOPSIS
                                An example function.
                            #>


                            param ( )
                        }
                    }

                    Get-HelpField 'ExampleInsideFunction' -Name 'SYNOPSIS' | Should -Be 'An example function.'
                }

                It 'Should find help content at the end of the function' {
                    . {
                        function ExampleInsideFunction {
                            param ( )
                            Write-Host 'Hello world'
                            <#
                            .SYNOPSIS
                                An example function.
                            #>
                        }
                    }

                    Get-HelpField 'ExampleInsideFunction' -Name 'SYNOPSIS' | Should -Be 'An example function.'
                }

                It 'Should not find help content between statements in the function' {
                    . {
                        function ExampleInsideFunction {
                            param ( )
                            <#
                            .SYNOPSIS
                                An example function.
                            #>
                            Write-Host 'Hello world'
                        }
                    }

                    Get-HelpField 'ExampleInsideFunction' -Name 'SYNOPSIS' | Should -Be 'ExampleInsideFunction'
                }
            }
        }

        Context 'Script help' {
            BeforeAll {
                $script = Join-Path -Path $TestDrive -ChildPath 'script.ps1'
            }

            It 'Should find help content at the start of a script' {
                Set-Content -Path $script -Value @'
<#
.SYNOPSIS
Script help.
#>

Write-Host 'Hello world'
'@

                Get-HelpField $script -Name 'SYNOPSIS' | Should -Be 'Script help.'
            }

            It 'Should find help content at the end of a script' {
                Set-Content -Path $script -Value @'
Write-Host 'Hello world'

<#
.SYNOPSIS
Script help.
#>
'@

                Get-HelpField $script -Name 'SYNOPSIS' | Should -Be 'Script help.'
            }

            It 'Should find help content anywhere before param' {
                Set-Content -Path $script -Value @'



                <#
.SYNOPSIS
Script help.
#>


param ( )
'@


                Get-HelpField $script -Name 'SYNOPSIS' | Should -Be 'Script help.'
            }

            It 'Should find help after a #Requires statement in a script' {
                Set-Content -Path $script -Value @'
#Requires -PSEdition Core

<#
.SYNOPSIS
Script help.
#>

Write-Host 'Hello world'
'@

                Get-HelpField $script -Name 'SYNOPSIS' | Should -Be 'Script help.'
            }

            It 'Should find help content after other comments at the start of a script' {
                Set-Content -Path $script -Value @'
# Arbitrary leading comments

<#
.SYNOPSIS
Script help.
#>

Write-Host 'Hello world'
'@

                Get-HelpField $script -Name 'SYNOPSIS' | Should -Be 'Script help.'
            }

            It 'Should find help content for a script which includes a function' {
                Set-Content -Path $script -Value @'
<#
.SYNOPSIS
Script help.
#>

<#
.SYNOPSIS
Function help.
#>
function NestedFunction { }
'@

                Get-HelpField $script -Name 'SYNOPSIS' | Should -Be 'Script help.'
            }

            It 'Should not find help content between statements in a script' {
                Set-Content -Path $script -Value @'
Write-Host 'Start'

<#
.SYNOPSIS
Script help.
#>

Write-Host 'End'
'@

                Get-Help $script | ForEach-Object Trim | Should -Be 'script.ps1'
            }

            It 'Should not find help content if there is no line break between #Requires and help content' {
                Set-Content -Path $script -Value @'
#Requires -PSEdition Core
<#
.SYNOPSIS
Script help.
#>

Write-Host 'Hello world'
'@

                Get-Help $script | ForEach-Object Trim | Should -Be 'script.ps1'
            }

            It 'Should not find help content after a using statement in a script' {
                Set-Content -Path $script -Value @'
using namespace System.Management.Automation

<#
.SYNOPSIS
Script help.
#>

Write-Host 'Hello world'
'@

                Get-Help $script | ForEach-Object Trim | Should -Be 'script.ps1'
            }

            It 'Should not consider function help within a sript as script help' {
                Set-Content -Path $script -Value @'
<#
.SYNOPSIS
Function help.
#>
function NestedFunction { }
'@

                Get-Help $script | ForEach-Object Trim | Should -Be 'script.ps1'
            }
        }
    }

    Context '-? parameter' {
        It 'Can get help using the "-?" parameter' {
            function ExampleFunction {
                <#
                .SYNOPSIS
                    An example function.
                #>
            }

            $helpInfo = ExampleFunction -?

            $helpInfo | Should -Not -BeNullOrEmpty
            Get-HelpField -HelpInfo $helpInfo -Name 'SYNOPSIS' | Should -Be 'An example function.'
        }
    }

    Context 'Comment style' {
        BeforeDiscovery {
            $testCases = @(
                @{ Name = 'SYNOPSIS';      Expected = 'An example function\.' }
                @{ Name = 'DESCRIPTIOn';   Expected = 'A description(\r?\n){2}    with indented text and a blank line\.' }
                @{ Name = 'NOTES';         Expected = 'This is an example function\.' }
                @{ Name = 'LINKS,URI';     Expected = 'https://blogs.msdn.com/powershell' }
                @{ Name = 'LINKS,TEXT';    Expected = 'Other commands\.' }
                @{ Name = 'EXAMPLE,CODE';  Expected = 'ExampleFunction Arguments' }
                @{ Name = 'INPUTS';        Expected = 'Accepts anything you like\.' }
                @{ Name = 'OUTPUTS';       Expected = 'Returns anything you like\.' }
                @{ Name = 'COMPONENT';     Expected = 'ComponentName' }
                @{ Name = 'ROLE';          Expected = 'RoleName' }
                @{ Name = 'FUNCTIONALITY'; Expected = 'FunctionalityName' }
            )
        }

        Context 'Block comment' {
            BeforeAll {
                function ExampleFunction {
                    <#
                    .SYNOPSIS
                        An example function.
                    .DESCRIPTION
                        A description

                            with indented text and a blank line.
                    .NOTES
                        This is an example function.
                    .LINK
                        https://blogs.msdn.com/powershell
                    .LINK
                        Other commands.
                    .EXAMPLE
                        ExampleFunction Arguments
                    .INPUTS
                        Accepts anything you like.
                    .OUTPUTS
                        Returns anything you like.
                    .COMPONENT
                        ComponentName
                    .ROLE
                        RoleName
                    .FUNCTIONALITY
                        FunctionalityName
                    #>
                }

                $helpInfo = $helpErrors = $null
                $helpInfo = Get-Help ExampleFunction -ErrorAction SilentlyContinue -ErrorVariable helpErrors
            }

            It 'Should successfully get help for ExampleFunction' {
                $helpInfo | Should -Not -BeNullOrEmpty
                $helpInfo.Name | Should -Be 'ExampleFunction'
                $helpErrors | Should -BeNullOrEmpty
            }

            It 'Should get the value of <Name> from the help block' -TestCases $testCases {
                param ($Name, $Expected)

                Get-HelpField -HelpInfo $helpInfo -Name $Name | Where-Object { $_ } | Should -Match $Expected
            }
        }

        Context 'Line comment' {
            BeforeAll {
                function ExampleFunction {
                    # .SYNOPSIS
                    #     An example function.
                    # .DESCRIPTION
                    #     A description
                    #
                    #         with indented text and a blank line.
                    # .NOTES
                    #     This is an example function.
                    # .LINK
                    #     https://blogs.msdn.com/powershell
                    # .LINK
                    #     Other commands.
                    # .EXAMPLE
                    #     ExampleFunction Arguments
                    # .INPUTS
                    #     Accepts anything you like.
                    # .OUTPUTS
                    #     Returns anything you like.
                    # .COMPONENT
                    #     ComponentName
                    # .ROLE
                    #     RoleName
                    # .FUNCTIONALITY
                    #     FunctionalityName
                }

                $helpInfo = $helpErrors = $null
                $helpInfo = Get-Help ExampleFunction -ErrorAction SilentlyContinue -ErrorVariable helpErrors
            }

            It 'Should successfully get help for ExampleFunction' {
                $helpInfo | Should -Not -BeNullOrEmpty
                $helpInfo.Name | Should -Be 'ExampleFunction'
                $helpErrors | Should -BeNullOrEmpty
            }

            It 'Should get the value of <Name> from the help block' -TestCases $testCases {
                param ($Name, $Expected)

                Get-HelpField -HelpInfo $helpInfo -Name $Name | Where-Object { $_ } | Should -Match $Expected
            }
        }

        Context 'Partial content' {
            It 'Allows explicitly empty values' {
                function ExampleFunction {
                    <#
                    .SYNOPSIS
                        An example function.
                    .DESCRIPTION

                    .NOTES
                        This function has an empty description.
                    #>
                }

                $helpInfo = Get-Help ExampleFunction

                Get-HelpField -HelpInfo $helpInfo -Name 'SYNOPSIS' | Should -Be 'An example function.'
                Get-HelpField -HelpInfo $helpInfo -Name 'DESCRIPTION' | Should -BeNullOrEmpty
                Get-HelpField -HelpInfo $helpInfo -Name 'NOTES' | Should -Be 'This function has an empty description.'
            }
        }
    }

    Context 'Parameters' {
        It 'Should support parameter help in the CBH block' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                    .PARAMETER Name
                        A parameter.
                #>
                param (
                    $Name
                )
            }

            Get-HelpField 'ExampleFunction' -Name 'PARAMETER,DESCRIPTION' | Should -Be 'A parameter.'
        }

        It 'Should support parameter help within the param block as a block comment' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    <#
                        A parameter.
                    #>
                    $Name
                )
            }

            Get-HelpField -CommandName 'ExampleFunction' -Name 'PARAMETER,DESCRIPTION' | Should -Be 'A parameter.'
        }

        It 'Should support parameter help within the param block as a line comment' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # A parameter.
                    $Name
                )
            }

            Get-HelpField 'ExampleFunction' -Name 'PARAMETER,DESCRIPTION' | Should -Be 'A parameter.'
        }

        It 'Should only consider the most proximate comment in the param block' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    <#
                        Unrelated comment.
                    #>

                    <#
                        A parameter.
                    #>
                    $Name
                )
            }

            Get-HelpField 'ExampleFunction' -Name 'PARAMETER,DESCRIPTION' | Should -Be 'A parameter.'
        }

        It 'Should set <Name> to the default value of <Expected> when no attributes are defined' -TestCases @(
            @{ Name = 'required';      Expected = 'false' }
            @{ Name = 'globbing';      Expected = 'false' }
            @{ Name = 'pipelineInput'; Expected = 'false' }
            @{ Name = 'position';      Expected = '1' }
            @{ Name = 'defaultValue';  Expected = '' }
        ) {
            param ($Name, $Expected)

            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    $Name
                )
            }

            $helpInfo = Get-Help ExampleFunction
            $helpInfo | Should -Not -BeNullOrEmpty

            $parameterHelp = Get-HelpField -HelpInfo $helpInfo -Name 'PARAMETER'
            $parameterHelp | Should -Not -BeNullOrEmpty

            $parameterHelp.$Name | Should -Be $Expected
        }

        It 'Should set type based on the supplied parameter type' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    [string]
                    $Name
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.type.name | Should -Be 'string'
        }

        It 'Should set required based on the Parameter attribute' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    [Parameter(Mandatory)]
                    $Name
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.required | Should -Be 'true'
        }

        It 'Should set position based on the Parameter attribute' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    [Parameter(Position = 42)]
                    $Name
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            <#
                Looks like a bug.

                HelpSystem adds one to the declared value.

                If you declare position as [int]::MaxValue it loops around and defines it as -2147483648.

                Probably harmless considering usage, but still a bug.

                $parameterHelp.position | Should -Be 42
            #>
            Set-ItResult -Inconclusive -Because 'HelpSystem is automatically adding 1 to the position value.'
        }

        It 'Should set pipelineInput to "ByValue" based on the Parameter attribute' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    [Parameter(ValueFromPipeline)]
                    $Name
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.pipelineInput | Should -Be 'true (ByValue)'
        }

        It 'Should set pipelineInput to "ByPropertyName" based on the Parameter attribute' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    [Parameter(ValueFromPipelineByPropertyName)]
                    $Name
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.pipelineInput | Should -Be 'true (ByPropertyName)'
        }

        It 'Should set pipelineInput to "ByValue, ByPropertyName" based on the Parameter attribute' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    [Parameter(ValueFromPipeline, ValueFromPipelineByPropertyName)]
                    $Name
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.pipelineInput | Should -Be 'true (ByValue, ByPropertyName)'
        }

        It 'Should set globbing based on the SupportsWildcards attribute' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    [SupportsWildcards()]
                    $Name
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.globbing | Should -Be 'true'
        }

        It 'Should set defaultValue based on an assigned value' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    $Name = 'A default value.'
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.defaultValue | Should -Be 'A default value.'
        }

        It 'Should set defaultValue based on a PSDefaultValue attribute' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    [PSDefaultValue(Value = 'A default value.')]
                    $Name
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.defaultValue | Should -Be 'A default value.'
        }

        It 'Should set defaultValue to an a variable name if the default is a variable' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    $Name = $value
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.defaultValue | Should -Be '$value'
        }

        It 'Should set defaultValue to an expression if the default is an expression' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                param (
                    # Parameter help.
                    $Name = (Get-Process -ID $PID)
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.defaultValue | Should -Be '(Get-Process -ID $PID)'
        }

        It 'Should not include the language-defined parameter <_>' -TestCases @(
            [System.Management.Automation.Internal.CommonParameters].GetProperties().Name
            [System.Management.Automation.Internal.ShouldProcessParameters].GetProperties().Name
            [System.Management.Automation.Internal.TransactionParameters].GetProperties().Name
            [System.Management.Automation.PagingParameters].GetProperties().Name
        ) {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                [CmdletBinding(SupportsShouldProcess, SupportsTransactions, SupportsPaging)]
                param (
                    # Parameter help.
                    $Name
                )
            }

            $parameterHelp = Get-HelpField 'ExampleFunction' -Name 'PARAMETER'
            $parameterHelp.name | Should -Not -Contain $Name
        }
    }

    Context 'Syntax' {
        It 'Should not include the language-defined parameter <_>' -TestCases @(
            [System.Management.Automation.Internal.CommonParameters].GetProperties().Name
            [System.Management.Automation.Internal.ShouldProcessParameters].GetProperties().Name
            [System.Management.Automation.Internal.TransactionParameters].GetProperties().Name
            [System.Management.Automation.PagingParameters].GetProperties().Name
        ) {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                [CmdletBinding(SupportsShouldProcess, SupportsTransactions, SupportsPaging)]
                param (
                    # Parameter help.
                    $Name
                )
            }

            $syntax = Get-HelpField 'ExampleFunction' -Name 'SYNTAX,ITEM'
            $syntax.parameter.name | Should -Not -Contain $Name
        }

        It 'Should represent mandatory optional parameters' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                [CmdletBinding()]
                param (
                    [string]
                    $Name
                )
            }


        }

        It 'Should represent mandatory parameters' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                [CmdletBinding(PositionalBinding = $false)]
                param (
                    [Parameter(Mandatory)]
                    $Name
                )
            }

            $helpInfo = Get-Help 'ExampleFunction'

            $parameter = Get-HelpField -HelpInfo $helpInfo -Name 'SYNTAX,PARAMETER'
            $parameter.required | Should -Be 'true'
            $parameter.parameterValue.required | Should -Be 'true'

            Get-HelpField -HelpInfo $helpInfo -Name 'SYNTAX,STRING' | Should -Match '(?m)^ExampleFunction -Name <Object>'
        }

        It 'Should represent positional parameters' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                [CmdletBinding()]
                param (
                    [Parameter(Position = 42)]
                    $Name
                )
            }

            $helpInfo = Get-Help 'ExampleFunction'

            <#
                See "Should set position based on the Parameter attribute"

                $parameter = Get-HelpField -HelpInfo $helpInfo -Name 'SYNTAX,PARAMETER'
                $parameter.position | Should -Be 42
            #>

            Get-HelpField -HelpInfo $helpInfo -Name 'SYNTAX,STRING' | Should -Match '(?m)^ExampleFunction \[\[-Name\] <Object>\]'
        }

        It 'Should represent mandatory positional parameters' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                [CmdletBinding()]
                param (
                    [Parameter(Mandatory, Position = 42)]
                    $Name
                )
            }

            $helpInfo = Get-Help 'ExampleFunction'

            <#
                See "Should set position based on the Parameter attribute"

                $parameter = Get-HelpField -HelpInfo $helpInfo -Name 'SYNTAX,PARAMETER'
                $parameter.position | Should -Be 42
            #>

            Get-HelpField -HelpInfo $helpInfo -Name 'SYNTAX,STRING' | Should -Match '(?m)^ExampleFunction \[-Name\] <Object>'
        }

        It 'Should represent enums parameters' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                [CmdletBinding()]
                param (
                    [Parameter()]
                    [DayOfWeek]
                    $Name
                )
            }

            Get-HelpField 'ExampleFunction' -Name 'SYNTAX,STRING' | Should -Match '(?m)^ExampleFunction \[\[-Name\] {Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday}]'
        }

        It 'Should represent Switch parameters' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
                [CmdletBinding()]
                param (
                    [Parameter()]
                    [Switch]
                    $Name
                )
            }

            Get-HelpField 'ExampleFunction' -Name 'SYNTAX,STRING' | Should -Match '(?m)^ExampleFunction \[-Name\]'
        }

        Context 'Parameter sets' {
            It 'Should create a syntaxItem for each parameter set' {
                function ExampleFunction {
                    <#
                        .SYNOPSIS
                            An example function.
                    #>
                    [CmdletBinding(DefaultParameterSetName = 'FirstParameterSet')]
                    param (
                        # Parameter help.
                        [Parameter(ParameterSetName = 'FirstParameterSet')]
                        $First,

                        # Parameter help.
                        [Parameter(ParameterSetName = 'SecondParameterSet')]
                        $Second
                    )
                }

                Get-HelpField 'ExampleFunction' -Name 'SYNTAX,ITEM' | Should -HaveCount 2
            }

            It 'Should correctly the syntax string for the parameter set' {
                function ExampleFunction {
                    <#
                        .SYNOPSIS
                            An example function.
                    #>
                    [CmdletBinding(DefaultParameterSetName = 'FirstParameterSet')]
                    param (
                        # Parameter help.
                        [Parameter(Mandatory, ParameterSetName = 'FirstParameterSet')]
                        $First,

                        # Parameter help.
                        [Parameter(Mandatory, ParameterSetName = 'SecondParameterSet')]
                        $Second
                    )
                }

                $syntax = Get-HelpField 'ExampleFunction' -Name 'SYNTAX,STRING'

                $syntax | Should -Match '(?m)^ExampleFunction -First <Object> \[<CommonParameters>\]'
                $syntax | Should -Match '(?m)^ExampleFunction -Second <Object> \[<CommonParameters>\]'
            }

            It 'Should should create a parameter syntax entry for each parameter set a parameter belongs to' {
                function ExampleFunction {
                    <#
                        .SYNOPSIS
                            An example function.
                    #>
                    [CmdletBinding(DefaultParameterSetName = 'FirstParameterSet')]
                    param (
                        # Parameter help.
                        [Parameter(ParameterSetName = 'FirstParameterSet')]
                        [Parameter(ParameterSetName = 'SecondParameterSet')]
                        $Name
                    )
                }

                $syntax = Get-HelpField 'ExampleFunction' -Name 'SYNTAX,ITEM'

                $syntax.parameter | Should -HaveCount 2
                $syntax.parameter.name | Sort-Object -Unique | Should -Be 'Name'
            }

            It 'Should create a parameter syntax entry for each named parameter set and the default parameter set' {
                function ExampleFunction {
                    <#
                        .SYNOPSIS
                            An example function.
                    #>
                    [CmdletBinding(DefaultParameterSetName = 'DefaultSet')]
                    param (
                        # Parameter help.
                        [Parameter()]
                        $Name,

                        # Parameter help.
                        [Parameter(ParameterSetName = 'FirstParameterSet')]
                        $First,

                        # Parameter help.
                        [Parameter(ParameterSetName = 'SecondParameterSet')]
                        $Second
                    )
                }

                $syntax = Get-HelpField 'ExampleFunction' -Name 'SYNTAX,ITEM'

                $syntax | Should -HaveCount 3
                # Name * 3 + First * 1 + Second * 1
                $syntax.parameter | Should -HaveCount 5
                $syntax.parameter.name | Sort-Object -Unique | Should -Be 'First', 'Name', 'Second'
            }
        }
    }

    Context 'Examples' {
        It 'Should have empty examples if none are present' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                #>
            }

            Get-HelpField 'ExampleFunction' -Name 'EXAMPLE' | Should -BeNullOrEmpty
        }

        It 'Should find a single example' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                    .EXAMPLE
                        ExampleFunction

                        Comment
                #>
            }

            Get-HelpField 'ExampleFunction' -Name 'EXAMPLE' | Should -HaveCount 1
        }

        It 'Should prefix the example in the command with "PS > "' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                    .EXAMPLE
                        ExampleFunction

                        Comment
                #>
            }

            Get-HelpField 'ExampleFunction' -Name 'EXAMPLE,STRING' | Should -Match '(?m)^PS\s>\s'
        }

        It 'Should not prefix an example which already includes a prompt' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                    .EXAMPLE
                        PROMPT> ExampleFunction

                        Comment
                #>
            }

            Get-HelpField 'ExampleFunction' -Name 'EXAMPLE,STRING' | Should -Match '(?m)^PROMPT>ExampleFunction'
        }

        It 'Should allow multiple examples' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                    .EXAMPLE
                        ExampleFunction

                        Comment1
                    .EXAMPLE
                        ExampleFunction

                        Comment2
                #>
            }

            $helpInfo = Get-Help 'ExampleFunction'

            $examples = Get-HelpField -HelpInfo $helpInfo -Name 'EXAMPLE'
            $examples | Should -HaveCount 2

            $exampleStrings = Get-HelpField -HelpInfo $helpInfo -Name 'EXAMPLE,STRING'

            $examples[0].code | Should -Be 'ExampleFunction'
            $examples[0].remarks.Text | Should -Contain 'Comment1'
            $exampleStrings[0] | Should -Match '(?sm)^PS\s>\sExampleFunction.+Comment1'

            $examples[1].code | Should -Be 'ExampleFunction'
            $examples[1].remarks.Text | Should -Contain 'Comment2'
            $exampleStrings[1] | Should -Match '(?sm)PS\s>\sExampleFunction.+Comment2'
        }

        It 'Should support multi-line code examples' {
            function ExampleFunction {
                <#
                    .SYNOPSIS
                        An example function.
                    .EXAMPLE
                        $var = ExampleFunction
                        $var | Select-Object *

                        Comment
                #>
            }

            Get-HelpField 'ExampleFunction' -Name 'EXAMPLE,CODE' | Should -Match '\$var = ExampleFunction\r?\n\$var | Select-Object \*'
        }
    }

    Context 'Fowarded' {

    }

    Context 'External' {

    }

    Context 'Filtering' {
        BeforeDiscovery {
            $testCases = @(
                @{ Name = 'Component';     Value = 'ComponentName' }
                @{ Name = 'Role';          Value = 'RoleName' }
                @{ Name = 'Functionality'; Value= 'FunctionalityName' }
            )
        }

        BeforeAll {
            function ExampleFunction {
                <#
                .SYNOPSIS
                    An example function.
                .COMPONENT
                    ComponentName
                .ROLE
                    RoleName
                .FUNCTIONALITY
                    FunctionalityName
                #>
            }
        }

        It 'Should find content when filtering by <Name>' -TestCases $testCases {
            param ( $Name, $Value )

            $params = @{
                $Name       = $Value
                Name        = 'ExampleFunction'
                ErrorAction = 'Stop'
            }
            { Get-Help @params } | Should -Not -Throw
        }

        It 'Should throw if a help is not found when filtering by <Name>' -TestCases $testCases {
            param ( $Name )

            $params = @{
                $Name       = 'DoesNotExist'
                Name        = 'ExampleFunction'
                ErrorAction = 'Stop'
            }
            { Get-Help @params } | Should -Throw -ErrorId 'HelpNotFound,Microsoft.PowerShell.Commands.GetHelpCommand'
        }
    }
}




    # Context 'get-help helpFunc5' {

    #     function helpFunc5
    #     {
    #         # .EXTERNALHELP scriptHelp.Tests.xml
    #     }
    #     $x = Get-Help helpFunc5
    #     It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
    #     It '$x.Synopsis' { $x.Synopsis | Should -BeExactly "A useless function, really." }
    # }

    # Context 'get-help helpFunc6 script help xml' {
    #     function helpFunc6
    #     {
    #         # .EXTERNALHELP scriptHelp1.xml
    #     }
    #     if ($PSUICulture -ieq "en-us")
    #     {
    #         $x = Get-Help helpFunc6
    #         It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
    #         It '$x.Synopsis' { $x.Synopsis | Should -BeExactly "Useless.  Really, trust me on this one." }
    #     }
    # }

    # Context 'get-help helpFunc6 script help xml' {
    #     function helpFunc6
    #     {
    #         # .EXTERNALHELP newbase/scriptHelp1.xml
    #     }
    #     if ($PSUICulture -ieq "en-us")
    #     {
    #         $x = Get-Help helpFunc6
    #         It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
    #         It '$x.Synopsis' { $x.Synopsis | Should -BeExactly "Useless in newbase.  Really, trust me on this one." }
    #     }
    # }

    # Context 'get-help helpFunc7' {

    #     function helpFunc7
    #     {
    #         # .FORWARDHELPTARGETNAME Get-Help
    #         # .FORWARDHELPCATEGORY Cmdlet
    #     }
    #     $x = Get-Help helpFunc7
    #     It '$x.Name' { $x.Name | Should -BeExactly 'Get-Help' }
    #     It '$x.Category' { $x.Category | Should -BeExactly 'Cmdlet' }

    #     # Make sure help is a function, or the test would fail
    #     if ($null -ne (Get-Command -type Function help))
    #     {
    #         if ((Get-Content function:help) -Match "FORWARDHELP")
    #         {
    #             $x = Get-Help help
    #             It '$x.Name' { $x.Name | Should -BeExactly 'Get-Help' }
    #             It '$x.Category' { $x.Category | Should -BeExactly 'Cmdlet' }
    #         }
    #     }
    # }

    # Context 'get-help helpFunc8' {

    #     function func8
    #     {
    #         # .SYNOPSIS
    #         #    Help on helpFunc8, not func8
    #         function helpFunc8
    #         {
    #         }
    #         Get-Help helpFunc8
    #     }

    #     $x = Get-Help func8
    #     It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }

    #     $x = func8
    #     It '$x.Synopsis' { $x.Synopsis | Should -BeExactly 'Help on helpFunc8, not func8' }
    # }

    # Context 'get-help helpFunc9' {
    #     function helpFunc9
    #     {
    #         # .SYNOPSIS
    #         #    Help on helpFunc9, not func9
    #         param($x)

    #         function func9
    #         {
    #         }
    #         Get-Help func9
    #     }
    #     $x = Get-Help helpFunc9
    #     It 'help is on the outer functon' { $x.Synopsis | Should -BeExactly 'Help on helpFunc9, not func9' }
    #     $x = helpFunc9
    #     It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
    # }
