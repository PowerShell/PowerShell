# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "ShouldProcess respects VerbosePreference" -Tags "CI","Feature" {
    
    BeforeAll {
        function Test-ShouldProcess {
            [CmdletBinding(SupportsShouldProcess)]
            param($Target = "TestTarget", $Action = "TestAction")
            if ($PSCmdlet.ShouldProcess($Target, $Action)) { "OK" }
        }
    }

    Context "VerbosePreference variable" {
        It "Emits verbose output when VerbosePreference is Continue" {
            $VerbosePreference = 'Continue'
            $output = Test-ShouldProcess 4>&1
            $VerbosePreference = 'SilentlyContinue'
            
            $output | Where-Object { $_ -eq "OK" } | Should -Not -BeNullOrEmpty
            $verboseOutput = $output | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }
            $verboseOutput | Should -Not -BeNullOrEmpty
            $verboseOutput.Message | Should -Match "TestAction.*TestTarget"
        }

        It "Does not emit verbose output when VerbosePreference is SilentlyContinue" {
            $VerbosePreference = 'SilentlyContinue'
            $output = Test-ShouldProcess 4>&1
            
            $output | Where-Object { $_ -eq "OK" } | Should -Not -BeNullOrEmpty
            $verboseOutput = $output | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }
            $verboseOutput | Should -BeNullOrEmpty
        }
    }

    Context "Verbose parameter" {
        It "Emits verbose output when -Verbose is specified" {
            $output = Test-ShouldProcess -Verbose 4>&1
            
            $output | Where-Object { $_ -eq "OK" } | Should -Not -BeNullOrEmpty
            $verboseOutput = $output | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }
            $verboseOutput | Should -Not -BeNullOrEmpty
            $verboseOutput.Message | Should -Match "TestAction.*TestTarget"
        }
    }
}
