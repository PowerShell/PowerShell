# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Comment-based help NOTES section formatting' -Tags "CI", "Feature" {
    It 'NOTES section should have single blank line after header and 4-space indentation' {
        $helpText = Get-Help Remove-PSSession -Full | Out-String
        $lines = $helpText -split "`r?`n"

        # Find NOTES section
        $notesIndex = -1
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '^NOTES\s*$') {
                $notesIndex = $i
                break
            }
        }

        $notesIndex | Should -BeGreaterThan -1 -Because "NOTES section should exist"

        # The line after NOTES header should be blank
        $lines[$notesIndex + 1] | Should -Match '^\s*$' -Because "There should be a blank line after NOTES header"

        # The second line after NOTES header should have content with 4-space indentation
        $contentLine = $lines[$notesIndex + 2]
        $contentLine | Should -Match '^[ ]{4}\S' -Because "Content should have exactly 4-space indentation"
    }
}
