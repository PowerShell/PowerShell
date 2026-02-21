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
            $originalVerbosePreference = $VerbosePreference
            try {
                $VerbosePreference = 'Continue'
                $output = Test-ShouldProcess 4>&1

                $output | Where-Object { $_ -eq "OK" } | Should -Not -BeNullOrEmpty
                $verboseOutput = $output | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }
                $verboseOutput | Should -Not -BeNullOrEmpty
                $verboseOutput.Message | Should -Match "TestAction.*TestTarget"
            }
            finally {
                $VerbosePreference = $originalVerbosePreference
            }
        }

        It "Does not emit verbose output when VerbosePreference is SilentlyContinue" {
            $originalVerbosePreference = $VerbosePreference
            try {
                $VerbosePreference = 'SilentlyContinue'
                $output = Test-ShouldProcess 4>&1

                $output | Where-Object { $_ -eq "OK" } | Should -Not -BeNullOrEmpty
                $verboseOutput = $output | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }
                $verboseOutput | Should -BeNullOrEmpty
            }
            finally {
                $VerbosePreference = $originalVerbosePreference
            }
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

        It "Does not emit verbose output when -Verbose:`$false is specified even if VerbosePreference is Continue" {
            $originalVerbosePreference = $VerbosePreference
            try {
                $VerbosePreference = 'Continue'
                $output = Test-ShouldProcess -Verbose:$false 4>&1

                $output | Where-Object { $_ -eq "OK" } | Should -Not -BeNullOrEmpty
                $verboseOutput = $output | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }
                $verboseOutput | Should -BeNullOrEmpty
            }
            finally {
                $VerbosePreference = $originalVerbosePreference
            }
        }
    }
}
