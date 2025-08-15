# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Hashtable Splatting Parameter Binding Tests" -Tags "CI" {

    BeforeAll {
        function SimpleTest {
            param(
                [Alias('Key')]
                $Name,
                $Path
            )

            "Key: $Name; Path: $Path; Args: $args"
        }
    }

    Context "Basic Hashtable Splatting" {

        It "works on cmdlet" {
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            Get-Verb @hash > $null
            $zoo | Should -BeOfType System.Management.Automation.VerbInfo
            $zoo.Verb | Should -BeExactly 'Get'
        }

        It "works on simple function" {
            $hash = @{ Name = "Hello"; Blah = "World" }
            SimpleTest @hash | Should -BeExactly 'Key: Hello; Path: ; Args: -Blah: World'

            $hash = @{ Name = "Hello"; Path = "World" }
            SimpleTest @hash | Should -BeExactly 'Key: Hello; Path: World; Args: '

            $hash = @{ Name = "Hello" }
            SimpleTest @hash -Path "Yeah" | Should -BeExactly 'Key: Hello; Path: Yeah; Args: '
            SimpleTest -Path "Yeah" @hash | Should -BeExactly 'Key: Hello; Path: Yeah; Args: '

            $hash = @{ Key = "Hello" }
            SimpleTest @hash | Should -BeExactly 'Key: Hello; Path: ; Args: '
        }

        It "works on ScriptBlock.GetPowerShell" {
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            $ps = { param($hash) Get-Verb @hash; Get-Variable zoo }.GetPowerShell($hash)

            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType System.Management.Automation.VerbInfo
                $result[0].Verb | Should -BeExactly 'Get'
            } finally {
                $ps.Dispose()
            }
        }

        It "works on steppable pipeline" {
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            $sp = { Get-Verb @hash }.GetSteppablePipeline()
            try {
                $sp.Begin($false)

                $result = $sp.Process()
                $result | Should -BeOfType System.Management.Automation.VerbInfo
                $result.Verb | Should -BeExactly 'Get'
                $zoo | Should -BeOfType System.Management.Automation.VerbInfo
                $zoo.Verb | Should -BeExactly 'Get'

                $sp.End()
            } finally {
                $sp.Dispose()
            }
        }
    }

    Context "Explicitly specified named parameter supersedes the same one in Hashtable splatting" {

        It "works with the same parameter name" {
            ## Regular use with cmdlet
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            Get-Verb @hash -Verb "Send" > $null
            $zoo | Should -BeOfType System.Management.Automation.VerbInfo
            $zoo.Verb | Should -BeExactly "Send"

            $zoo = $null
            Get-Verb -Verb "Send" @hash > $null
            $zoo | Should -BeOfType System.Management.Automation.VerbInfo
            $zoo.Verb | Should -BeExactly "Send"

            ## GetPowerShell
            $ps = { param($hash) Get-Verb @hash -Verb "Send"; Get-Variable zoo }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType System.Management.Automation.VerbInfo
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            ## Steppable pipeline
            $zoo = $null
            $sp = { Get-Verb @hash -Verb "Send" }.GetSteppablePipeline()
            try {
                $sp.Begin($false)

                $result = $sp.Process()
                $result | Should -BeOfType System.Management.Automation.VerbInfo
                $result.Verb | Should -BeExactly 'Send'
                $zoo | Should -BeOfType System.Management.Automation.VerbInfo
                $zoo.Verb | Should -BeExactly 'Send'

                $sp.End()
            } finally {
                $sp.Dispose()
            }

            ## Regular use with simple function
            $hash = @{ Name = "Hello"; Path = "World" }
            SimpleTest @hash -Path "Yeah" | Should -BeExactly 'Key: Hello; Path: Yeah; Args: '
            SimpleTest -Path "Yeah" @hash | Should -BeExactly 'Key: Hello; Path: Yeah; Args: '

            $hash = @{ Name = "Hello"; Blah = "World" }
            SimpleTest @hash -Name "Yeah" | Should -BeExactly 'Key: Yeah; Path: ; Args: -Blah: World'
        }

        It "works with the same alias name" {
            ## Regular use with cmdlet
            $hash = @{ Verb = "Get"; ov = "zoo" }
            Get-Verb @hash -Verb "Send" -ov "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType System.Management.Automation.VerbInfo
            $bar.Verb | Should -BeExactly "Send"

            ## GetPowerShell
            $ps = { param($hash) Get-Verb @hash -Verb "Send" -ov "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType System.Management.Automation.VerbInfo
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            ## Steppable pipeline
            $bar = $null
            $sp = { Get-Verb @hash -Verb "Send" -ov "bar" }.GetSteppablePipeline()
            try {
                $sp.Begin($false)

                $result = $sp.Process()
                $result | Should -BeOfType System.Management.Automation.VerbInfo
                $result.Verb | Should -BeExactly 'Send'
                $zoo | Should -BeNullOrEmpty
                $bar | Should -BeOfType System.Management.Automation.VerbInfo
                $bar.Verb | Should -BeExactly 'Send'

                $sp.End()
            } finally {
                $sp.Dispose()
            }

            ## Regular use with simple function
            $hash = @{ key = "Hello"; Path = "World" }
            SimpleTest @hash -Key "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with parameter name and alias name mixed" {
            ## Regular use with cmdlet
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            Get-Verb @hash -Verb "Send" -ov "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType System.Management.Automation.VerbInfo
            $bar.Verb | Should -BeExactly "Send"

            ## GetPowerShell
            $ps = { param($hash) Get-Verb @hash -Verb "Send" -ov "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType System.Management.Automation.VerbInfo
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            ## Steppable pipeline
            $bar = $null
            $sp = { Get-Verb @hash -Verb "Send" -ov "bar" }.GetSteppablePipeline()
            try {
                $sp.Begin($false)

                $result = $sp.Process()
                $result | Should -BeOfType System.Management.Automation.VerbInfo
                $result.Verb | Should -BeExactly 'Send'
                $zoo | Should -BeNullOrEmpty
                $bar | Should -BeOfType System.Management.Automation.VerbInfo
                $bar.Verb | Should -BeExactly 'Send'

                $sp.End()
            } finally {
                $sp.Dispose()
            }

            ## Regular use with simple function
            $hash = @{ Name = "Hello"; Path = "World" }
            SimpleTest @hash -Key "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with unambiguous prefix and parameter name mixed - prefix explicitly specified" {
            ## Regular use with cmdlet
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            Get-Verb @hash -Verb "Send" -outv "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType System.Management.Automation.VerbInfo
            $bar.Verb | Should -BeExactly "Send"

            ## GetPowerShell
            $ps = { param($hash) Get-Verb @hash -Verb "Send" -outv "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType System.Management.Automation.VerbInfo
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            ## Steppable pipeline
            $bar = $null
            $sp = { Get-Verb @hash -Verb "Send" -outv "bar" }.GetSteppablePipeline()
            try {
                $sp.Begin($false)

                $result = $sp.Process()
                $result | Should -BeOfType System.Management.Automation.VerbInfo
                $result.Verb | Should -BeExactly 'Send'
                $zoo | Should -BeNullOrEmpty
                $bar | Should -BeOfType System.Management.Automation.VerbInfo
                $bar.Verb | Should -BeExactly 'Send'

                $sp.End()
            } finally {
                $sp.Dispose()
            }

            ## Regular use with simple function
            $hash = @{ Name = "Hello"; Path = "World" }
            SimpleTest @hash -n "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with unambiguous prefix and parameter name mixed - prefix in splatting hashtable" {
            ## Regular use with cmdlet
            $hash = @{ Verb = "Get"; outv = "zoo" }
            Get-Verb @hash -Verb "Send" -OutVariable "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType System.Management.Automation.VerbInfo
            $bar.Verb | Should -BeExactly "Send"

            ## GetPowerShell
            $ps = { param($hash) Get-Verb @hash -Verb "Send" -OutVariable "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType System.Management.Automation.VerbInfo
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            ## Steppable pipeline
            $bar = $null
            $sp = { Get-Verb @hash -Verb "Send" -OutVariable "bar" }.GetSteppablePipeline()
            try {
                $sp.Begin($false)

                $result = $sp.Process()
                $result | Should -BeOfType System.Management.Automation.VerbInfo
                $result.Verb | Should -BeExactly 'Send'
                $zoo | Should -BeNullOrEmpty
                $bar | Should -BeOfType System.Management.Automation.VerbInfo
                $bar.Verb | Should -BeExactly 'Send'

                $sp.End()
            } finally {
                $sp.Dispose()
            }

            ## Regular use with simple function
            $hash = @{ n = "Hello"; Path = "World" }
            SimpleTest @hash -Name "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with unambiguous prefix and alias name mixed - prefix explicitly specified" {
            ## Regular use with cmdlet
            $hash = @{ Verb = "Get"; ov = "zoo" }
            Get-Verb @hash -Verb "Send" -outv "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType System.Management.Automation.VerbInfo
            $bar.Verb | Should -BeExactly "Send"

            ## GetPowerShell
            $ps = { param($hash) Get-Verb @hash -Verb "Send" -outv "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType System.Management.Automation.VerbInfo
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            ## Steppable pipeline
            $bar = $null
            $sp = { Get-Verb @hash -Verb "Send" -outv "bar" }.GetSteppablePipeline()
            try {
                $sp.Begin($false)

                $result = $sp.Process()
                $result | Should -BeOfType System.Management.Automation.VerbInfo
                $result.Verb | Should -BeExactly 'Send'
                $zoo | Should -BeNullOrEmpty
                $bar | Should -BeOfType System.Management.Automation.VerbInfo
                $bar.Verb | Should -BeExactly 'Send'

                $sp.End()
            } finally {
                $sp.Dispose()
            }

            ## Regular use with simple function
            $hash = @{ key = "Hello"; Path = "World" }
            SimpleTest @hash -n "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with unambiguous prefix and alias name mixed - prefix in splatting hashtable" {
            ## Regular use with cmdlet
            $hash = @{ Verb = "Get"; outv = "zoo" }
            Get-Verb @hash -Verb "Send" -ov "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType System.Management.Automation.VerbInfo
            $bar.Verb | Should -BeExactly "Send"

            ## GetPowerShell
            $ps = { param($hash) Get-Verb @hash -Verb "Send" -ov "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType System.Management.Automation.VerbInfo
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            ## Steppable pipeline
            $bar = $null
            $sp = { Get-Verb @hash -Verb "Send" -ov "bar" }.GetSteppablePipeline()
            try {
                $sp.Begin($false)

                $result = $sp.Process()
                $result | Should -BeOfType System.Management.Automation.VerbInfo
                $result.Verb | Should -BeExactly 'Send'
                $zoo | Should -BeNullOrEmpty
                $bar | Should -BeOfType System.Management.Automation.VerbInfo
                $bar.Verb | Should -BeExactly 'Send'

                $sp.End()
            } finally {
                $sp.Dispose()
            }

            ## Regular use with simple function
            $hash = @{ n = "Hello"; Path = "World" }
            SimpleTest @hash -key "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }
    }

    Context "-@ Parameter splatting" {
        BeforeAll {
            $nativeCmdArgs = @(
                '-CommandWithArgs'
                # First 3 are pwsh, '-CommandWithArgs', and this command
                '[Environment]::GetCommandLineArgs() | Select-Object -Skip 3 | ConvertTo-Json -AsArray'
            )

            Function Test-SimpleFunction {
                $args
            }

            Function Test-AdvancedFunction {
                [CmdletBinding()]
                param (
                    [Parameter()]
                    [string]
                    $Param1,

                    [Parameter()]
                    [string]
                    $Param2
                )

                @{
                    Param1 = $Param1
                    Param2 = $Param2
                }
            }
        }
        It "Splats a hashtable" {
            $ht = [Ordered]@{ Name = "Test"; Path = "C:\Temp" }
            $actual = Test-SimpleFunction -@ $ht

            $actual.Count | Should -Be 4
            $actual[0] | Should -BeExactly '-Name:'
            $actual[0].'<CommandParameterName>' | Should -BeExactly 'Name'
            $actual[1] | Should -BeExactly 'Test'
            $actual[2] | Should -BeExactly '-Path:'
            $actual[2].'<CommandParameterName>' | Should -BeExactly 'Path'
            $actual[3] | Should -BeExactly 'C:\Temp'
        }

        It "Splats an array" {
            $ht = @('Positional', '-LooksLikeSwitch', '-LooksLikeParameter:Value')
            $actual = Test-SimpleFunction -@ $ht

            $actual.Count | Should -Be 3
            $actual[0] | Should -BeExactly 'Positional'
            $actual[0].PSObject.Properties.Match('<CommandParameterName>') | Should -BeNullOrEmpty
            $actual[1] | Should -BeExactly '-LooksLikeSwitch'
            $actual[1].PSObject.Properties.Match('<CommandParameterName>') | Should -BeNullOrEmpty
            $actual[2] | Should -BeExactly '-LooksLikeParameter:Value'
            $actual[2].PSObject.Properties.Match('<CommandParameterName>') | Should -BeNullOrEmpty
        }

        It "Splats multiple hashtables" {
            $ht1 = [Ordered]@{ Name = "Test"; Path = "C:\Temp" }
            $ht2 = @{ Other = $true }
            $actual = Test-SimpleFunction -@ $ht1 -@ $ht2

            $actual.Count | Should -Be 6
            $actual[0] | Should -BeExactly '-Name:'
            $actual[0].'<CommandParameterName>' | Should -BeExactly 'Name'
            $actual[1] | Should -BeExactly 'Test'
            $actual[2] | Should -BeExactly '-Path:'
            $actual[2].'<CommandParameterName>' | Should -BeExactly 'Path'
            $actual[3] | Should -BeExactly 'C:\Temp'
            $actual[4] | Should -BeExactly '-Other:'
            $actual[4].'<CommandParameterName>' | Should -BeExactly 'Other'
            $actual[5] | Should -BeTrue
        }

        It "Splats an expression value" {
            Function Get-Splat {
                [Ordered]@{ Name = "Test"; Path = "C:\Temp" }
            }

            $actual = Test-SimpleFunction -@ (Get-Splat)

            $actual.Count | Should -Be 4
            $actual[0] | Should -BeExactly '-Name:'
            $actual[0].'<CommandParameterName>' | Should -BeExactly 'Name'
            $actual[1] | Should -BeExactly 'Test'
            $actual[2] | Should -BeExactly '-Path:'
            $actual[2].'<CommandParameterName>' | Should -BeExactly 'Path'
            $actual[3] | Should -BeExactly 'C:\Temp'
        }

        It "Mixes parameter splat with splatted variable" {
            $ht1 = [Ordered]@{ Name = "Test"; Path = "C:\Temp" }
            $ht2 = @{ Other = $true }
            $actual = Test-SimpleFunction -@ $ht1 @ht2

            $actual.Count | Should -Be 6
            $actual[0] | Should -BeExactly '-Name:'
            $actual[0].'<CommandParameterName>' | Should -BeExactly 'Name'
            $actual[1] | Should -BeExactly 'Test'
            $actual[2] | Should -BeExactly '-Path:'
            $actual[2].'<CommandParameterName>' | Should -BeExactly 'Path'
            $actual[3] | Should -BeExactly 'C:\Temp'
            $actual[4] | Should -BeExactly '-Other:'
            $actual[4].'<CommandParameterName>' | Should -BeExactly 'Other'
            $actual[5] | Should -BeTrue
        }

        It "Splats native command" {
            # Splatting an array will splat the next level of arrays so
            # pos 3 and 4 should be separate args while 5 and 6 become a single
            # arg. This is the same as 'pwsh @nativeCmdArgs @array'.
            $array = @('pos 1', 'pos 2', @('pos 3', 'pos 4', @('pos 5', 'pos 6')))
            $actual = pwsh @nativeCmdArgs -@ $array | ConvertFrom-Json

            $actual.Count | Should -Be 5
            $actual[0] | Should -BeExactly 'pos 1'
            $actual[1] | Should -BeExactly 'pos 2'
            $actual[2] | Should -BeExactly 'pos 3'
            $actual[3] | Should -BeExactly 'pos 4'
            $actual[4] | Should -BeExactly 'pos 5 pos 6'
        }

        It "Passes -@ literal to native command without splatting" {
            $actual = pwsh @nativeCmdArgs '-@' value | ConvertFrom-Json

            $actual.Count | Should -Be 2
            $actual[0] | Should -BeExactly '-@'
            $actual[1] | Should -BeExactly 'value'
        }

        It "Passes -@ literal to simple function" {
            $actual = Test-SimpleFunction '-@' value

            $actual.Count | Should -Be 2
            $actual[0] | Should -BeExactly '-@'
            $actual[0].PSObject.Properties.Match('<CommandParameterName>') | Should -BeNullOrEmpty
            $actual[1] | Should -BeExactly 'value'
        }

        It "Binds to -@ parameter with splat on simple function" {
            Function Test-SimpleAtParam {
                param(${@})

                ${@}
            }

            $actual = Test-SimpleAtParam -@ @{ '-@' = 'value' }
            $actual | Should -BeExactly value
        }

        It "Binds to -@ parameter with splat on advanced function" {
            Function Test-AdvAtParam {
                param([Parameter()]${@})

                ${@}
            }

            $actual = Test-AdvAtParam -@ @{ '-@' = 'value' }
            $actual | Should -BeExactly value
        }

        It "PSDefaultParameterValues will not attempt to splat -@ value" {
            Function Test-DefaultParamValuesWithSplatParam {
                param([Parameter()]${@}, $Param)

                @{
                    '@' = ${@}
                    Param = $Param
                }
            }

            # This is not expected to splat the value but rather bind
            # to the '-@' parameter if present.
            $PSDefaultParameterValues['Test-DefaultParamValuesWithSplatParam:@'] = @{ Param = 'Value' }
            try {
                $actual = Test-DefaultParamValuesWithSplatParam
            }
            finally {
                $PSDefaultParameterValues.Remove('Test-DefaultParamValuesWithSplatParam:@')
            }

            $actual.'@' | Should -BeOfType ([Hashtable])
            $actual.'@'.Count | Should -Be 1
            $actual.'@'.Param | Should -BeExactly 'Value'
            $actual.Param | Should -BeNullOrEmpty
        }

        # Same behaviour as Test-AdvancedFunction @ht1 -Param1 arg
        It "Explicit parameter overrides splatted value" {
            $ht = @{ Param1 = 'splat' }
            $actual = Test-AdvancedFunction -@ $ht -Param1 arg

            $actual.Param1 | Should -BeExactly 'arg'
            $actual.Param2 | Should -BeNullOrEmpty
        }

        It "Fails when used without a value" {
            {
                Get-Item -@
            } | Should -Throw "Missing value after the splat parameter -@."
        }

        It "Fails when used with a splatted value" {
            {
                Get-Item -@ @value
            } | Should -Throw "Cannot use a @splat expression '@value' after the splat parameter '-@'. Remove the splat parameter or change the splat to a variable instead."
        }

        It "Fails when used with a -Command:Parameter expression" {
            {
                Get-Item -@ -Parameter:Value
            } | Should -Throw "Cannot use a -Parameter:Value expression '-Parameter:Value' after the splat parameter '-@'. Remove the splat parameter or specify the parameter value in a hashtable instead."
        }

        # Same behaviour as Test-AdvancedFunction @ht1 @ht2
        It "Fails when defining the same parameter multiple times" {
            $ht1 = @{Param1 = 'first'}
            $ht2 = @{Param1 = 'second'}
            {
                Test-AdvancedFunction -@ $ht1 -@ $ht2
            } | Should -Throw "Cannot bind parameter because parameter 'Param1' is specified more than once"
        }
    }
}
