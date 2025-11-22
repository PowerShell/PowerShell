# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Comment-based help NOTES section formatting' -Tags "CI", "Feature" {
    BeforeAll {
        # Use an existing cmdlet with NOTES section for testing
        $helpWithNotes = Get-Help Remove-PSSession -Full
    }

    It 'NOTES section should have only one blank line after header (not two)' {
        # Get the full help output as a string
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

        # The second line after NOTES header should have content (not another blank line)
        # This was the bug: there were TWO blank lines instead of one
        $lines[$notesIndex + 2] | Should -Match '\S' -Because "Content should start on the second line after NOTES header (bug was extra blank line)"
    }

    It 'NOTES content should have 4-space indentation, not 8 (double indentation bug)' {
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

        # Find first non-blank content line in NOTES section
        $contentLine = $null
        for ($i = $notesIndex + 1; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '\S') {
                $contentLine = $lines[$i]
                break
            }
        }

        $contentLine | Should -Not -BeNullOrEmpty

        # Count leading spaces - should be 4, not 8
        if ($contentLine -match '^(\s+)') {
            $indentation = $matches[1].Length
            $indentation | Should -Be 4 -Because "NOTES content should be indented 4 spaces, not 8 (bug was nested leftIndent causing 4+4=8)"
        } else {
            throw "Expected NOTES content to be indented, but found no leading whitespace"
        }
    }
}
