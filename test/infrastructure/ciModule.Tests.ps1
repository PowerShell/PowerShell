# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# NOTE: This test file tests the Test-MergeConflictMarker function which detects Git merge conflict markers.
# IMPORTANT: Do NOT use here-strings or literal conflict markers (e.g., "<<<<<<<", "=======", ">>>>>>>")
# in this file, as they will trigger conflict marker detection in CI pipelines.
# Instead, use string multiplication (e.g., '<' * 7) to dynamically generate these markers at runtime.

Describe "Test-MergeConflictMarker" {
    BeforeAll {
        # Import the module
        Import-Module "$PSScriptRoot/../../tools/ci.psm1" -Force

        # Create a temporary test workspace
        $script:testWorkspace = Join-Path $TestDrive "workspace"
        New-Item -ItemType Directory -Path $script:testWorkspace -Force | Out-Null

        # Create temporary output files
        $script:testOutputPath = Join-Path $TestDrive "outputs.txt"
        $script:testSummaryPath = Join-Path $TestDrive "summary.md"
    }

    AfterEach {
        # Clean up test files after each test
        if (Test-Path $script:testWorkspace) {
            Get-ChildItem $script:testWorkspace -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
        }
        Remove-Item $script:testOutputPath -Force -ErrorAction SilentlyContinue
        Remove-Item $script:testSummaryPath -Force -ErrorAction SilentlyContinue
    }

    Context "When no files are provided" {
        It "Should handle empty file array" {
            # The function parameter has Mandatory validation which rejects empty arrays by design
            # This test verifies that behavior
            $emptyArray = @()
            { Test-MergeConflictMarker -File $emptyArray -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath } | Should -Throw -ExpectedMessage "*empty array*"
        }
    }

    Context "When files have no conflicts" {
        It "Should pass for clean files" {
            $testFile = Join-Path $script:testWorkspace "clean.txt"
            "This is a clean file" | Out-File $testFile -Encoding utf8

            Test-MergeConflictMarker -File @("clean.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath

            $outputs = Get-Content $script:testOutputPath
            $outputs | Should -Contain "files-checked=1"
            $outputs | Should -Contain "conflicts-found=0"

            $summary = Get-Content $script:testSummaryPath -Raw
            $summary | Should -Match "No Conflicts Found"
        }
    }

    Context "When files have conflict markers" {
        It "Should detect <<<<<<< marker" {
            $testFile = Join-Path $script:testWorkspace "conflict1.txt"
            "Some content`n" + ('<' * 7) + " HEAD`nConflicting content" | Out-File $testFile -Encoding utf8

            { Test-MergeConflictMarker -File @("conflict1.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath } | Should -Throw

            $outputs = Get-Content $script:testOutputPath
            $outputs | Should -Contain "files-checked=1"
            $outputs | Should -Contain "conflicts-found=1"
        }

        It "Should detect ======= marker" {
            $testFile = Join-Path $script:testWorkspace "conflict2.txt"
            "Some content`n" + ('=' * 7) + "`nMore content" | Out-File $testFile -Encoding utf8

            { Test-MergeConflictMarker -File @("conflict2.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath } | Should -Throw
        }

        It "Should detect >>>>>>> marker" {
            $testFile = Join-Path $script:testWorkspace "conflict3.txt"
            "Some content`n" + ('>' * 7) + " branch-name`nMore content" | Out-File $testFile -Encoding utf8

            { Test-MergeConflictMarker -File @("conflict3.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath } | Should -Throw
        }

        It "Should detect multiple markers in one file" {
            $testFile = Join-Path $script:testWorkspace "conflict4.txt"
            $content = "Some content`n" + ('<' * 7) + " HEAD`nContent A`n" + ('=' * 7) + "`nContent B`n" + ('>' * 7) + " branch`nMore content"
            $content | Out-File $testFile -Encoding utf8

            { Test-MergeConflictMarker -File @("conflict4.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath } | Should -Throw

            $summary = Get-Content $script:testSummaryPath -Raw
            $summary | Should -Match "Conflicts Detected"
            $summary | Should -Match "conflict4.txt"
        }

        It "Should detect conflicts in multiple files" {
            $testFile1 = Join-Path $script:testWorkspace "conflict5.txt"
            ('<' * 7) + " HEAD" | Out-File $testFile1 -Encoding utf8

            $testFile2 = Join-Path $script:testWorkspace "conflict6.txt"
            ('=' * 7) | Out-File $testFile2 -Encoding utf8

            { Test-MergeConflictMarker -File @("conflict5.txt", "conflict6.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath } | Should -Throw

            $outputs = Get-Content $script:testOutputPath
            $outputs | Should -Contain "files-checked=2"
            $outputs | Should -Contain "conflicts-found=2"
        }
    }

    Context "When markers are not at line start" {
        It "Should not detect markers in middle of line" {
            $testFile = Join-Path $script:testWorkspace "notconflict.txt"
            "This line has <<<<<<< in the middle" | Out-File $testFile -Encoding utf8

            Test-MergeConflictMarker -File @("notconflict.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath

            $outputs = Get-Content $script:testOutputPath
            $outputs | Should -Contain "conflicts-found=0"
        }

        It "Should not detect markers with wrong number of characters" {
            $testFile = Join-Path $script:testWorkspace "wrongcount.txt"
            ('<' * 6) + " Only 6`n" + ('<' * 8) + " 8 characters" | Out-File $testFile -Encoding utf8

            Test-MergeConflictMarker -File @("wrongcount.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath

            $outputs = Get-Content $script:testOutputPath
            $outputs | Should -Contain "conflicts-found=0"
        }
    }

    Context "When handling special file scenarios" {
        It "Should skip non-existent files" {
            Test-MergeConflictMarker -File @("nonexistent.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath

            $outputs = Get-Content $script:testOutputPath
            $outputs | Should -Contain "files-checked=0"
        }

        It "Should handle absolute paths" {
            $testFile = Join-Path $script:testWorkspace "absolute.txt"
            "Clean content" | Out-File $testFile -Encoding utf8

            Test-MergeConflictMarker -File @($testFile) -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath

            $outputs = Get-Content $script:testOutputPath
            $outputs | Should -Contain "conflicts-found=0"
        }

        It "Should handle mixed relative and absolute paths" {
            $testFile1 = Join-Path $script:testWorkspace "relative.txt"
            "Clean" | Out-File $testFile1 -Encoding utf8

            $testFile2 = Join-Path $script:testWorkspace "absolute.txt"
            "Clean" | Out-File $testFile2 -Encoding utf8

            Test-MergeConflictMarker -File @("relative.txt", $testFile2) -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath

            $outputs = Get-Content $script:testOutputPath
            $outputs | Should -Contain "files-checked=2"
            $outputs | Should -Contain "conflicts-found=0"
        }
    }

    Context "When summary and output generation" {
        It "Should generate proper GitHub Actions outputs format" {
            $testFile = Join-Path $script:testWorkspace "test.txt"
            "Clean file" | Out-File $testFile -Encoding utf8

            Test-MergeConflictMarker -File @("test.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath

            $outputs = Get-Content $script:testOutputPath
            $outputs | Where-Object {$_ -match "^files-checked=\d+$"} | Should -Not -BeNullOrEmpty
            $outputs | Where-Object {$_ -match "^conflicts-found=\d+$"} | Should -Not -BeNullOrEmpty
        }

        It "Should generate markdown summary with conflict details" {
            $testFile = Join-Path $script:testWorkspace "marked.txt"
            $content = "Line 1`n" + ('<' * 7) + " HEAD`nLine 3`n" + ('=' * 7) + "`nLine 5"
            $content | Out-File $testFile -Encoding utf8

            { Test-MergeConflictMarker -File @("marked.txt") -WorkspacePath $script:testWorkspace -OutputPath $script:testOutputPath -SummaryPath $script:testSummaryPath } | Should -Throw

            $summary = Get-Content $script:testSummaryPath -Raw
            $summary | Should -Match "# Merge Conflict Marker Check Results"
            $summary | Should -Match "marked.txt"
            $summary | Should -Match "\| Line \| Marker \|"
        }
    }
}

Describe "Install-CIPester" {
    BeforeAll {
        # Import the module
        Import-Module "$PSScriptRoot/../../tools/ci.psm1" -Force
    }

    Context "When checking function exists" {
        It "Should export Install-CIPester function" {
            $function = Get-Command Install-CIPester -ErrorAction SilentlyContinue
            $function | Should -Not -BeNullOrEmpty
            $function.ModuleName | Should -Be 'ci'
        }

        It "Should have expected parameters" {
            $function = Get-Command Install-CIPester
            $function.Parameters.Keys | Should -Contain 'MinimumVersion'
            $function.Parameters.Keys | Should -Contain 'MaximumVersion'
            $function.Parameters.Keys | Should -Contain 'Force'
        }

        It "Should accept version parameters" {
            $function = Get-Command Install-CIPester
            $function.Parameters['MinimumVersion'].ParameterType.Name | Should -Be 'String'
            $function.Parameters['MaximumVersion'].ParameterType.Name | Should -Be 'String'
            $function.Parameters['Force'].ParameterType.Name | Should -Be 'SwitchParameter'
        }
    }

    Context "When validating real execution" {
        # These tests only run in CI where we can safely install/test Pester

        It "Should successfully run without errors when Pester exists" {
            if (!$env:CI) {
                Set-ItResult -Skipped -Because "Test requires CI environment to safely install Pester"
            }

            { Install-CIPester -ErrorAction Stop } | Should -Not -Throw
        }

        It "Should accept custom version parameters" {
            if (!$env:CI) {
                Set-ItResult -Skipped -Because "Test requires CI environment to safely install Pester"
            }

            { Install-CIPester -MinimumVersion '4.0.0' -MaximumVersion '5.99.99' -ErrorAction Stop } | Should -Not -Throw
        }
    }
}

