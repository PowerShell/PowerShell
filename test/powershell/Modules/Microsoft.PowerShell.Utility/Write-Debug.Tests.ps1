# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Write-Debug tests" -Tags "CI" {
    It "Should not have added line breaks" {
        $text = "0123456789"
        while ($text.Length -lt [Console]::WindowWidth) {
            $text += $text
        }
        $origDebugPref = $DebugPreference
        $DebugPreference = "Continue"
        try {
            $out = Write-Debug $text 5>&1
            $out | Should -BeExactly $text
        }
        finally {
            $DebugPreference = $origDebugPref
        }
    }
}
