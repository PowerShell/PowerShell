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
            $zoo | Should -BeOfType 'System.Management.Automation.VerbInfo'
            $zoo.Verb | Should -BeExactly 'Get'
        }

        It "works on simple function" {
            $hash = @{ Name = "Hello"; Blah = "World" }
            SimpleTest @hash | Should -BeExactly 'Key: Hello; Path: ; Args: -Blah: World'

            $hash = @{ Name = "Hello"; Path = "World" }
            SimpleTest @hash | Should -BeExactly 'Key: Hello; Path: World; Args: '

            $hash = @{ Name = "Hello" }
            SimpleTest @hash -Path "Yeah" | Should -BeExactly 'Key: Hello; Path: Yeah; Args: '

            $hash = @{ Key = "Hello" }
            SimpleTest @hash | Should -BeExactly 'Key: Hello; Path: ; Args: '
        }

        It "works on ScriptBlock.GetPowerShell" {
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            $ps = { param($hash) Get-Verb @hash; Get-Variable zoo }.GetPowerShell($hash)

            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType 'System.Management.Automation.VerbInfo'
                $result[0].Verb | Should -BeExactly 'Get'
            } finally {
                $ps.Dispose()
            }
        }
    }

    Context "Explicitly specified named parameter supersedes the same one in Hashtable splatting" {

        It "works with the same parameter name" {
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            Get-Verb @hash -Verb "Send" > $null
            $zoo | Should -BeOfType "System.Management.Automation.VerbInfo"
            $zoo.Verb | Should -BeExactly "Send"

            $ps = { param($hash) Get-Verb @hash -Verb "Send"; Get-Variable zoo }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType 'System.Management.Automation.VerbInfo'
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            $hash = @{ Name = "Hello"; Path = "World" }
            SimpleTest @hash -Path "Yeah" | Should -BeExactly 'Key: Hello; Path: Yeah; Args: '

            $hash = @{ Name = "Hello"; Blah = "World" }
            SimpleTest @hash -Name "Yeah" | Should -BeExactly 'Key: Yeah; Path: ; Args: -Blah: World'
        }

        It "works with the same alias name" {
            $hash = @{ Verb = "Get"; ov = "zoo" }
            Get-Verb @hash -Verb "Send" -ov "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType "System.Management.Automation.VerbInfo"
            $bar.Verb | Should -BeExactly "Send"

            $ps = { param($hash) Get-Verb @hash -Verb "Send" -ov "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType 'System.Management.Automation.VerbInfo'
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            $hash = @{ key = "Hello"; Path = "World" }
            SimpleTest @hash -Key "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with parameter name and alias name mixed" {
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            Get-Verb @hash -Verb "Send" -ov "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType "System.Management.Automation.VerbInfo"
            $bar.Verb | Should -BeExactly "Send"

            $ps = { param($hash) Get-Verb @hash -Verb "Send" -ov "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType 'System.Management.Automation.VerbInfo'
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            $hash = @{ Name = "Hello"; Path = "World" }
            SimpleTest @hash -Key "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with unambiguous prefix and parameter name mixed - prefix explicitly specified" {
            $hash = @{ Verb = "Get"; OutVariable = "zoo" }
            Get-Verb @hash -Verb "Send" -outv "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType "System.Management.Automation.VerbInfo"
            $bar.Verb | Should -BeExactly "Send"

            $ps = { param($hash) Get-Verb @hash -Verb "Send" -outv "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType 'System.Management.Automation.VerbInfo'
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            $hash = @{ Name = "Hello"; Path = "World" }
            SimpleTest @hash -n "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with unambiguous prefix and parameter name mixed - prefix in splatting hashtable" {
            $hash = @{ Verb = "Get"; outv = "zoo" }
            Get-Verb @hash -Verb "Send" -OutVariable "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType "System.Management.Automation.VerbInfo"
            $bar.Verb | Should -BeExactly "Send"

            $ps = { param($hash) Get-Verb @hash -Verb "Send" -OutVariable "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType 'System.Management.Automation.VerbInfo'
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            $hash = @{ n = "Hello"; Path = "World" }
            SimpleTest @hash -Name "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with unambiguous prefix and alias name mixed - prefix explicitly specified" {
            $hash = @{ Verb = "Get"; ov = "zoo" }
            Get-Verb @hash -Verb "Send" -outv "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType "System.Management.Automation.VerbInfo"
            $bar.Verb | Should -BeExactly "Send"

            $ps = { param($hash) Get-Verb @hash -Verb "Send" -outv "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType 'System.Management.Automation.VerbInfo'
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            $hash = @{ key = "Hello"; Path = "World" }
            SimpleTest @hash -n "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }

        It "works with unambiguous prefix and alias name mixed - prefix in splatting hashtable" {
            $hash = @{ Verb = "Get"; outv = "zoo" }
            Get-Verb @hash -Verb "Send" -ov "bar" > $null
            $zoo | Should -BeNullOrEmpty
            $bar | Should -BeOfType "System.Management.Automation.VerbInfo"
            $bar.Verb | Should -BeExactly "Send"

            $ps = { param($hash) Get-Verb @hash -Verb "Send" -ov "bar"; Get-Variable bar }.GetPowerShell($hash)
            try {
                $result = $ps.Invoke()
                $result[0] | Should -BeOfType 'System.Management.Automation.VerbInfo'
                $result[0].Verb | Should -BeExactly 'Send'
            } finally {
                $ps.Dispose()
            }

            $hash = @{ n = "Hello"; Path = "World" }
            SimpleTest @hash -key "Yeah" | Should -BeExactly 'Key: Yeah; Path: World; Args: '
        }
    }
}
