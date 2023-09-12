# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
$ProgressPreference = "SilentlyContinue"

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

Describe 'Get-Help Function' -Tags 'Feature' {
    BeforeDiscovery {
        $testCases = @(
            @{ Name = 'Synopsis';                                Expected = 'A relatively useless function.' }
            @{ Name = 'Description';                             Expected = "A description`n`n    with indented text and a blank line." }
            @{ Name = 'alertSet.alert';                          Expected = 'This function is mostly harmless.';        Getter = { $content.alertSet.alert } }
            @{ Name = 'relatedLinks.navigationLink[0].uri';      Expected = 'https://blogs.msdn.com/powershell';        Getter = { $content.relatedLinks.navigationLink[0].uri } }
            @{ Name = 'relatedLinks.navigationLink[1].linkText'; Expected = 'other commands';                           Getter = { $content.relatedLinks.navigationLink[1].linkText } }
            @{ Name = 'examples.example.code';                   Expected = "If you need an example, you're hopeless."; Getter = { $content.examples.example.code } }
            @{ Name = 'inputTypes.inputType.type.name';          Expected = 'Anything you like.';                       Getter = { $content.inputTypes.inputType.type.name } }
            @{ Name = 'returnValues.returnValue.type.name';      Expected = 'Nothing.';                                 Getter = { $content.returnValues.returnValue.type.name } }
            @{ Name = 'Component';                               Expected = 'Something' }
            @{ Name = 'Role';                                    Expected = 'CrazyUser' }
            @{ Name = 'Functionality';                           Expected = 'Useless' }
        )
    }

    BeforeAll {
        function TestHelpError {
            [CmdletBinding()]
            param(
            $x,
            [System.Management.Automation.ErrorRecord[]]$e,
            [string] $expectedError
            )
            It 'Help result should be $null' { $x | Should -BeNullOrEmpty }
            It '$e.Count' { $e.Count | Should -BeGreaterThan 0 }
            It 'FullyQualifiedErrorId' { $e[0].FullyQualifiedErrorId | Should -BeExactly $expectedError }
        }

        # .SYNOPSIS
        #
        #    A relatively useless function.
        #
        # .DESCRIPTION
        #
        #    A description
        #
        #        with indented text and a blank line.
        #
        # .NOTES
        #
        #    This function is mostly harmless.
        #
        # .LINK
        #
        #    https://blogs.msdn.com/powershell
        #
        # .LINK
        #
        #    other commands
        #
        # .EXAMPLE
        #
        #    If you need an example, you're hopeless.
        #
        # .INPUTS
        #
        #    Anything you like.
        #
        # .OUTPUTS
        #
        #    Nothing.
        #
        # .COMPONENT
        #
        #    Something
        #
        # .ROLE
        #
        #    CrazyUser
        #
        # .FUNCTIONALITY
        #
        #    Useless
        #
        function helpFunc1 {}

        Set-Item function:dynamicHelpFunc1 -Value {
            # .SYNOPSIS
            #
            #    A relatively useless function.
            #
            # .DESCRIPTION
            #
            #    A description
            #
            #        with indented text and a blank line.
            #
            # .NOTES
            #
            #    This function is mostly harmless.
            #
            # .LINK
            #
            #    https://blogs.msdn.com/powershell
            #
            # .LINK
            #
            #    other commands
            #
            # .EXAMPLE
            #
            #    If you need an example, you're hopeless.
            #
            # .INPUTS
            #
            #    Anything you like.
            #
            # .OUTPUTS
            #
            #    Nothing.
            #
            # .COMPONENT
            #
            #    Something
            #
            # .ROLE
            #
            #    CrazyUser
            #
            # .FUNCTIONALITY
            #
            #    Useless
            #

            process { }
        }
    }

    Context 'Get-Help helpFunc1' {
        BeforeAll {
            $content = Get-Help helpFunc1 -ErrorAction SilentlyContinue -ErrorVariable helpError
        }

        It 'Should not be null' {
            Get-Help helpFunc1 | Should -Not -BeNullOrEmpty
        }

        It 'Should not raise errors getting help' {
            $helpError | Should -BeNullOrEmpty
        }

        It 'Should get <Name>' -TestCases $properties {
            param ($Name, $Expected, $Getter)

            if ($Getter) {
                $value = & $Getter
            } else {
                $value = $content.$Name
            }

            $value | Should -BeExactly $Expected
        }
    }

    Context 'Get-Help dynamicHelpFunc1' {
        BeforeAll {
            $content = Get-Help dynamicHelpFunc1 -ErrorAction SilentlyContinue -ErrorVariable helpError
        }

        It 'Should not be null' {
            Get-Help dynamicHelpFunc1 | Should -Not -BeNullOrEmpty
        }

        It 'Should not raise errors getting help' {
            $helpError | Should -BeNullOrEmpty
        }

        It 'Should get <Name>' -TestCases $properties {
            param ($Name, $Expected, $Getter)

            if ($Getter) {
                $value = & $Getter
            } else {
                $value = $content.$Name
            }

            $value | Should -BeExactly $Expected
        }
    }

    Context 'Filtering' {
        It 'Can get role specific help' {
            {
                Get-Help helpFunc1 -Role CrazyUser -ErrorAction Stop | Should -Not -BeNullOrEmpty
            } | Should -Not -Throw
        }

        It 'Can filter help based on Functionality' {
            {
                Get-Help helpFunc1 -Functionality Useless -ErrorAction Stop | Should -Not -BeNullOrEmpty
            } | Should -Not -Throw
        }

        It 'Should throw if a help is not found when filtering by <Name>' -TestCases @(
            @{ Name = 'Component' }
            @{ Name = 'Role' }
            @{ Name = 'Functionality' }
        ) {
            param ( $Name )

            $params = @{
                $Name       = 'blah'
                Name        = 'helpFunc1'
                ErrorAction = 'Stop'
            }
            { Get-Help @params } | Should -Throw -ErrorId 'HelpNotFound,Microsoft.PowerShell.Commands.GetHelpCommand'
        }
    }
}

Describe 'get-help file' -Tags "CI" {
    BeforeAll {
        try {
            if ($IsWindows) {
                $tmpfile = [IO.Path]::ChangeExtension([IO.Path]::GetTempFileName(), 'ps1')
            }
            else {
                $tmpfile = Join-Path $env:HOME $([IO.Path]::ChangeExtension([IO.Path]::GetRandomFileName(), 'ps1'))
            }
        } catch {
            return
        }
    }

    AfterAll {
        Remove-Item $tmpfile -Force -ErrorAction silentlycontinue
    }

    Context 'get-help file1' {

        @'
    # .SYNOPSIS
    #    Function help, not script help
    function foo
    {
    }

    get-help foo
'@ > $tmpfile

        $x = Get-Help $tmpfile
        It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
        $x = & $tmpfile
        It '$x.Synopsis' { $x.Synopsis | Should -BeExactly 'Function help, not script help' }
    }

    Context 'get-help file2' {

	    # Note - 2 blank lines below are intentional, do not delete
        @'
        # .SYNOPSIS
        #    Script help, not function help


        function foo
        {
        }

        get-help foo
'@ > $tmpfile

        $x = Get-Help $tmpfile
        It '$x.Synopsis' { $x.Synopsis | Should -BeExactly 'Script help, not function help' }
        $x = & $tmpfile
        It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
    }
}

Describe 'get-help other tests' -Tags "CI" {
    BeforeAll {
        try {
            if ($IsWindows) {
                $tempFile = [IO.Path]::ChangeExtension([IO.Path]::GetTempFileName(), "ps1")
            }
            else {
                $tempFile = Join-Path $env:HOME $([IO.Path]::ChangeExtension([IO.Path]::GetRandomFileName(), "ps1"))
            }
        } catch {
            return
        }
    }

    AfterAll {
        Remove-Item $tempFile -Force -ErrorAction silentlycontinue
    }

    Context 'get-help missingHelp' {
        # Blank lines here are important, do not adjust the formatting, remove blank lines, etc.

        <#
        .SYNOPSIS
        This help block doesn't belong to any function because it is more than 1 line away from the function.
        #>


        function missingHelp { param($abc) }
            $x = Get-Help missingHelp
            It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
            It '$x.Synopsis' { $x.Synopsis.Trim() | Should -BeExactly 'missingHelp [[-abc] <Object>]' }
        }

    Context 'get-help helpFunc2' {

    <#
    .SYNOPSIS
        This help block goes on helpFunc2
    #>

    function helpFunc2 { param($abc) }

        $x = Get-Help helpFunc2
        It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
        It '$x.Synopsis' { $x.Synopsis.Trim() | Should -BeExactly 'This help block goes on helpFunc2' }
    }

    Context 'get-help file and get-help helpFunc2' {
        $script = @"
            <#
            .SYNOPSIS
                This is script help
            #>

            <#
            .SYNOPSIS
                This is function help for helpFunc2
            #>
            function helpFunc2 {}

            get-help helpFunc2
"@

        Set-Content $tempFile $script
        $x = Get-Help $tempFile
        It '$x.Synopsis' { $x.Synopsis | Should -BeExactly "This is script help" }

        $x = & $tempFile
        It '$x.Synopsis' { $x.Synopsis | Should -BeExactly "This is function help for helpFunc2" }
    }

    Context 'get-help file and get-help helpFunc2' {
    $script = @"
        #
        # .SYNOPSIS
        #    This is script help
        #

        #
        # .SYNOPSIS
        #    This is function help for helpFunc2
        #
        function helpFunc2 {}

        get-help helpFunc2
"@

        Set-Content $tempFile $script
        $x = Get-Help $tempFile
        It $x.Synopsis { $x.Synopsis | Should -BeExactly "This is script help" }

        $x = & $tempFile
        It $x.Synopsis { $x.Synopsis | Should -BeExactly "This is function help for helpFunc2" }
    }

    Context 'get-help psuedo file' {

    $script = @'
        ###########################################
        #
        # Psuedo-Copyright header comment
        #
        ###########################################

        #requires -version 2.0

        #
        # .Synopsis
        #	Changes Admin passwords across all KDE servers.
        #
        [CmdletBinding(DefaultParameterSetName="Live")]
        param(
        [Parameter(
	        ParameterSetName="Live",
	        Mandatory=$true)]
        $live,
        [Parameter(
	        ParameterSetName="Test",
	        Mandatory=$true)]
        $test)
'@

        Set-Content $tempFile $script
        $x = Get-Help $tempFile

        It '$x.Synopsis' { $x.Synopsis | Should -BeExactly "Changes Admin passwords across all KDE servers." }
        It '$x.parameters.parameter[0].required' { $x.parameters.parameter[0].required | Should -BeTrue}
        It '$x.syntax.syntaxItem[0].parameter.required' { $x.syntax.syntaxItem[0].parameter.required | Should -BeTrue}
        It '$x.syntax.syntaxItem[0].parameter.parameterValue.required' { $x.syntax.syntaxItem[0].parameter.parameterValue.required | Should -BeTrue}
        It 'Common parameters should not be appear in the syntax' { $x.Syntax -like "*verbose*" | Should -BeFalse }
        It 'Common parameters should not be in syntax maml' {@($x.syntax.syntaxItem[0].parameter).Count | Should -Be 1}
        It 'Common parameters should also not appear in parameters maml' { $x.parameters.parameter.Count | Should -Be 2}
    }

    It 'helpFunc3 -?' {

        ##############################################

        function helpFunc3()
        {
        #
        #
        #.Synopsis
        #
        #   A synopsis of helpFunc3.
        #
        #
        }

        $x = helpFunc3 -?
        $x.Synopsis | Should -BeExactly "A synopsis of helpFunc3."
    }

    It 'get-help helpFunc4' {

        <#
        .Description
          description

        .Synopsis

        .Component
          component
        #>
        function helpFunc4()
        {
        }

        $x = Get-Help helpFunc4
        $x.Synopsis | Should -BeExactly ""
    }

    Context 'get-help helpFunc5' {

        function helpFunc5
        {
            # .EXTERNALHELP scriptHelp.Tests.xml
        }
        $x = Get-Help helpFunc5
        It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
        It '$x.Synopsis' { $x.Synopsis | Should -BeExactly "A useless function, really." }
    }

    Context 'get-help helpFunc6 script help xml' {
        function helpFunc6
        {
            # .EXTERNALHELP scriptHelp1.xml
        }
        if ($PSUICulture -ieq "en-us")
        {
            $x = Get-Help helpFunc6
            It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
            It '$x.Synopsis' { $x.Synopsis | Should -BeExactly "Useless.  Really, trust me on this one." }
        }
    }

    Context 'get-help helpFunc6 script help xml' {
        function helpFunc6
        {
            # .EXTERNALHELP newbase/scriptHelp1.xml
        }
        if ($PSUICulture -ieq "en-us")
        {
            $x = Get-Help helpFunc6
            It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
            It '$x.Synopsis' { $x.Synopsis | Should -BeExactly "Useless in newbase.  Really, trust me on this one." }
        }
    }

    Context 'get-help helpFunc7' {

        function helpFunc7
        {
            # .FORWARDHELPTARGETNAME Get-Help
            # .FORWARDHELPCATEGORY Cmdlet
        }
        $x = Get-Help helpFunc7
        It '$x.Name' { $x.Name | Should -BeExactly 'Get-Help' }
        It '$x.Category' { $x.Category | Should -BeExactly 'Cmdlet' }

        # Make sure help is a function, or the test would fail
        if ($null -ne (Get-Command -type Function help))
        {
            if ((Get-Content function:help) -Match "FORWARDHELP")
            {
                $x = Get-Help help
                It '$x.Name' { $x.Name | Should -BeExactly 'Get-Help' }
                It '$x.Category' { $x.Category | Should -BeExactly 'Cmdlet' }
            }
        }
    }

    Context 'get-help helpFunc8' {

        function func8
        {
            # .SYNOPSIS
            #    Help on helpFunc8, not func8
            function helpFunc8
            {
            }
            Get-Help helpFunc8
        }

        $x = Get-Help func8
        It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }

        $x = func8
        It '$x.Synopsis' { $x.Synopsis | Should -BeExactly 'Help on helpFunc8, not func8' }
    }

    Context 'get-help helpFunc9' {
        function helpFunc9
        {
            # .SYNOPSIS
            #    Help on helpFunc9, not func9
            param($x)

            function func9
            {
            }
            Get-Help func9
        }
        $x = Get-Help helpFunc9
        It 'help is on the outer functon' { $x.Synopsis | Should -BeExactly 'Help on helpFunc9, not func9' }
        $x = helpFunc9
        It '$x should not be $null' { $x | Should -Not -BeNullOrEmpty }
    }

    It 'get-help helpFunc10' {
        #######################################################
        # .SYNOPSIS
        #    Help on helpFunc10
        #######################################################
        function helpFunc10
        {
        }

        $x = Get-Help helpFunc10
        $x.Synopsis | Should -BeExactly 'Help on helpFunc10'
    }

    Context 'get-help helpFunc11' {
        function helpFunc11
        {
        # .Synopsis
        #
        #   useless, sorry

            param(

                # not in help

                <# abc #><# help #> [parameter()]    [string]
                $abc,

                [string]
                # def help
                $def,

                [parameter()]
                # ghi help
                $ghi,

                # jkl help

                $jkl,

                <#mno help#>$mno,
                <#pqr help#>[int]$pqr

                )
        }

        $x = Get-Help helpFunc11 -det
        $x.Parameters.parameter | ForEach-Object {
            $dText = $_.description[0].text
            It ('$_.description ({0})' -f $title) { $dText | Should -Match "^$($_.Name)\s+help" }
        }
    }

    Context 'get-help helpFunc12' {
        # Test case w/ cmdlet binding, but no parameter sets.
        function helpFunc12 {
            Param ([Parameter(Mandatory=$true)][String]$Name,
                   [string]$Extension = "txt",
                   $NoType,
                   [switch]$ASwitch,
                   [System.Management.Automation.CommandTypes]$AnEnum)
            $name = $name + "." + $extension
            $name

        <#
        .SYNOPSIS
        Adds a file name extension to a supplied name.

        .EXAMPLE

            PS>helpFunc12 -Name foo

            Adds .txt to foo

        .EXAMPLE

            C:\PS>helpFunc12 bar txt

            Adds .txt to bar
         #>
        }
        $x = Get-Help helpFunc12
        It '$x.syntax' { ($x.syntax | Out-String -Width 250) | Should -Match "helpFunc12 \[-Name] <String> \[\[-Extension] <String>] \[\[-NoType] <Object>] \[-ASwitch] \[\[-AnEnum] \{Alias.*All}] \[<CommonParameters>]" }
        It '$x.syntax.syntaxItem.parameter[3].position' { $x.syntax.syntaxItem.parameter[3].position | Should -BeExactly 'named' }
        It '$x.syntax.syntaxItem.parameter[3].parameterValue' { $x.syntax.syntaxItem.parameter[3].parameterValue | Should -BeNullOrEmpty }
        It '$x.parameters.parameter[3].parameterValue' { $x.parameters.parameter[3].parameterValue | Should -Not -BeNullOrEmpty }
        It '$x.syntax.syntaxItem.parameter[4].parameterValueGroup' { $x.syntax.syntaxItem.parameter[4].parameterValueGroup | Should -Not -BeNullOrEmpty }
        It '$x.parameters.parameter[4].parameterValueGroup' { $x.parameters.parameter[4].parameterValueGroup | Should -Not -BeNullOrEmpty }
        It '$x.examples.example[0].introduction[0].text' { $x.examples.example[0].introduction[0].text | Should -BeExactly 'PS>' }
        It '$x.examples.example[0].code' { $x.examples.example[0].code | Should -BeExactly 'helpFunc12 -Name foo' }
        It '$x.examples.example[0].remarks[0].text' { $x.examples.example[0].remarks[0].text | Should -BeExactly 'Adds .txt to foo' }
        It '$x.examples.example[0].remarks.length' { $x.examples.example[0].remarks.length | Should -Be 5 }
        It '$x.examples.example[1].introduction[0].text' { $x.examples.example[1].introduction[0].text | Should -BeExactly 'C:\PS>' }
        It '$x.examples.example[1].code' { $x.examples.example[1].code | Should -BeExactly 'helpFunc12 bar txt' }
        It '$x.examples.example[1].remarks[0].text' { $x.examples.example[1].remarks[0].text | Should -BeExactly 'Adds .txt to bar' }
        It '$x.examples.example[1].remarks.length' { $x.examples.example[1].remarks.length | Should -Be 5 }
    }

    Context 'get-help helpFunc12' {
        function helpFunc13
        {
        <#
            .Synopsis

            empty synopsis
        #>
            param(
                [SupportsWildcards()]
                $p1,
                $p2 = 42,
                [PSDefaultValue(Help="parameter is mandatory")]
                $p3 = $(throw "parameter p3 is not specified")
            )
        }

        $x = Get-Help helpFunc13

        It '$x.Parameters.parameter[0].globbing' { $x.Parameters.parameter[0].globbing | Should -BeExactly 'true' }
        It '$x.Parameters.parameter[1].defaultValue' { $x.Parameters.parameter[1].defaultValue | Should -BeExactly '42' }
        It '$x.Parameters.parameter[2].defaultValue' { $x.Parameters.parameter[2].defaultValue | Should -BeExactly 'parameter is mandatory' }
    }

    Context 'get-help helpFunc14' {
        function helpFunc14
        {
            param(
                [SupportsWildcards()]
                $p1
            )
        }

        $x = Get-Help helpFunc14

        It '$x.Parameters.parameter[0].globbing' { $x.Parameters.parameter[0].globbing | Should -BeExactly 'true' }
    }

    Context 'get-help helpFunc15' {
        function helpFunc15
        {
            param(
                $p1
            )
        }

        $x = Get-Help helpFunc15

        It '$x.Parameters.parameter[0].globbing' { $x.Parameters.parameter[0].globbing | Should -BeExactly 'false' }
    }

    Context 'get-help -Examples prompt string should have trailing space' {
        function foo {
            <#
              .EXAMPLE
              foo bar
            #>
              param()
        }

        It 'prompt should be exactly "PS > " with trailing space' {
            (Get-Help foo -Examples).examples.example.introduction.Text | Should -BeExactly "PS > "
        }
    }

    Context 'get-help -Examples multi-line code block should be handled' {
        function foo {
            <#
              .EXAMPLE
              $a = Get-Service
              $a | group Status

              .EXAMPLE
              PS> $a = Get-Service
              PS> $a | group Status

              Explanation.

              .EXAMPLE

              PS> Get-Service
              Output

              .EXAMPLE
              PS> Get-Service
              Output

              Explanation.
              Second line.

              .EXAMPLE
              PS> Get-Service
              Output

              Explanation.

              Next section.
            #>
              param()
        }

        $x = Get-Help foo
        It '$x.examples.example[0].introduction[0].text' { $x.examples.example[0].introduction[0].text | Should -BeExactly "PS > " }
        It '$x.examples.example[0].code' { $x.examples.example[0].code | Should -BeExactly "`$a = Get-Service`n`$a | group Status" }
        It '$x.examples.example[0].remarks[0].text' { $x.examples.example[0].remarks[0].text | Should -BeNullOrEmpty }
        It '$x.examples.example[0].remarks.length' { $x.examples.example[0].remarks.length | Should -Be 5 }
        It '$x.examples.example[1].introduction[0].text' { $x.examples.example[1].introduction[0].text | Should -BeExactly "PS>" }
        It '$x.examples.example[1].code' { $x.examples.example[1].code | Should -BeExactly "`$a = Get-Service`nPS> `$a | group Status" }
        It '$x.examples.example[1].remarks[0].text' { $x.examples.example[1].remarks[0].text | Should -BeExactly "Explanation." }
        It '$x.examples.example[1].remarks.length' { $x.examples.example[1].remarks.length | Should -Be 5 }
        It '$x.examples.example[2].introduction[0].text' { $x.examples.example[2].introduction[0].text | Should -BeExactly "PS>" }
        It '$x.examples.example[2].code' { $x.examples.example[2].code | Should -BeExactly "Get-Service`nOutput" }
        It '$x.examples.example[2].remarks[0].text' { $x.examples.example[2].remarks[0].text | Should -BeNullOrEmpty }
        It '$x.examples.example[2].remarks.length' { $x.examples.example[2].remarks.length | Should -Be 5 }
        It '$x.examples.example[3].introduction[0].text' { $x.examples.example[3].introduction[0].text | Should -BeExactly "PS>" }
        It '$x.examples.example[3].code' { $x.examples.example[3].code | Should -BeExactly "Get-Service`nOutput" }
        It '$x.examples.example[3].remarks[0].text' { $x.examples.example[3].remarks[0].text | Should -BeExactly "Explanation.`nSecond line." }
        It '$x.examples.example[3].remarks.length' { $x.examples.example[3].remarks.length | Should -Be 5 }
        It '$x.examples.example[4].introduction[0].text' { $x.examples.example[4].introduction[0].text | Should -BeExactly "PS>" }
        It '$x.examples.example[4].code' { $x.examples.example[4].code | Should -BeExactly "Get-Service`nOutput" }
        It '$x.examples.example[4].remarks[0].text' { $x.examples.example[4].remarks[0].text | Should -BeExactly "Explanation.`n`nNext section." }
        It '$x.examples.example[4].remarks.length' { $x.examples.example[4].remarks.length | Should -Be 5 }
    }
}
