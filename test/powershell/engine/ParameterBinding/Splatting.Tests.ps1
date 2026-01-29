# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Splatting Parameter Binding Tests" -Tags "CI" {

    BeforeAll {
        function SimpleTest {
            param(
                [Alias('Key')]
                $Name,
                $Path
            )

            "Key: $Name; Path: $Path; Args: $args"
        }

        function ArgCollector {
            , $args
        }

        function FunctionWithSwitch {
            param(
                [switch]$Switch,
                $Other = 'default'
            )

            "Switch: $($Switch.IsPresent), Other: $Other"
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

        It "splats switch value set to true" {
            $splat = @{ Switch = $true }
            $result = FunctionWithSwitch @splat
            $result | Should -Be "Switch: True, Other: default"
        }

        It "splats switch value set to false" {
            $splat = @{ Switch = $false }
            $result = FunctionWithSwitch @splat
            $result | Should -Be "Switch: False, Other: default"
        }

        It "splats switch value with extra parameter" {
            $splat = @{ Switch = $false; Other = 'foo' }
            $result = FunctionWithSwitch @splat
            $result | Should -Be "Switch: False, Other: foo"
        }
    }

    Context "Array Splatting" {
        It "splats values positionally" {
            $splat = 'name', 'path'
            $result = SimpleTest @splat
            $result | Should -Be "Key: name; Path: path; Args: "
        }

        It "splats parameter name and value" {
            $splat = ArgCollector -Key name -Path path
            $result = SimpleTest @splat
            $result | Should -Be "Key: name; Path: path; Args: "
        }

        It "splats parameter switch with no extra arguments" {
            $splat = ArgCollector -Switch
            $result = FunctionWithSwitch @splat
            $result | Should -Be "Switch: $true, Other: default"
        }

        It "splats parameter switch with no value" {
            $splat = ArgCollector -Switch -Other foo
            $result = FunctionWithSwitch @splat
            $result | Should -Be "Switch: $true, Other: foo"
        }

        It "splats parameter switch with <Switch>" -TestCases @(
            @{ Switch = $true }
            @{ Switch = $false }
        ) {
            param ($Switch)

            $splat = ArgCollector -Switch:$Switch -Other foo
            $result = FunctionWithSwitch @splat
            $result | Should -Be "Switch: $Switch, Other: foo"
        }

        It "splats hashtable into `$args with Switch <Switch> to then splat onto another function" -TestCases @(
            @{ Switch = $true }
            @{ Switch = $false }
        ) {
            param ($Switch)

            $splat = @{ Switch = $Switch; Other = 'foo' }
            $argSplat = ArgCollector @splat
            $result = FunctionWithSwitch @argSplat
            $result | Should -Be "Switch: $Switch, Other: foo"
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
}
