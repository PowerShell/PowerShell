# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Write-Verbose" -Tags "CI" {
    It "Should be able to call cmdlet without error" {
	{ Write-Verbose -Message "test" -ErrorAction SilentlyContinue } | Should -Not -Throw
    }

    It "Should not display verbose output by default" {
	$VerbosePreference | Should -Be SilentlyContinue

	Write-Verbose -Message "test" | Should -BeNullOrEmpty
    }

    It "Should be able to set verbose output to display by changing the `$VerbosePreference automatic variable" {
	$VerbosePreference = "Continue"

	Write-Verbose -Message "test" 4>&1 | Should -Not -BeNullOrEmpty

	$VerbosePreference = "SilentlyContinue"
    }

    It "Should be able to set verbose output to display by using the verbose switch" {
	Write-Verbose -Message "test" -Verbose 4>&1 | Should -BeExactly "test"
    }

    It "Should be able to set verbose switch using a colon and boolean" {
	{ Write-Verbose -Message "test" -Verbose:$false } | Should -Not -Throw

	$(Write-Verbose -Message "test" -Verbose:$true) 4>&1 | Should -BeExactly "test"
    }

    It "Should not have added line breaks" {
        $text = "0123456789"
        try {
            while ($text.Length -lt [Console]::WindowWidth) {
                $text += $text
            }
        } catch {
            # Ignore errors if the console doesn't support WindowWidth
        }
        $origVerbosePref = $VerbosePreference
        $VerbosePreference = "continue"
        try {
            $out = Write-Verbose $text 4>&1
            $out | Should -BeExactly $text
        }
        finally {
            $VerbosePreference = $origVerbosePref
        }
    }
}
