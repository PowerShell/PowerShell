# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# DEMONSTRATION TEST FOR GITHUB ACTIONS ANNOTATIONS FEATURE
# This test intentionally fails to demonstrate the GitHub Actions annotation feature.
# When this test runs in CI and fails, it will generate clickable file annotations
# with direct links to this file and line number.
#
# REMOVE THIS FILE after verifying the feature works in production CI runs.

Describe "GitHub Actions Annotations Demo" -Tag 'CI' {
    Context "Demonstration of annotation feature" {
        It "Should fail to demonstrate GitHub Actions annotations - REMOVE THIS TEST" {
            # This test intentionally fails to show the annotation feature
            # When it fails, Show-PSPesterError will generate:
            # ::error file=test/powershell/Modules/DemoGitHubAnnotations.Tests.ps1,line=<N>,title=...
            # 
            # The annotation will appear as a clickable link in:
            # - The GitHub Actions "Annotations" section
            # - The PR "Files changed" tab (if in a PR)
            #
            # After confirming annotations appear correctly, DELETE THIS FILE.
            
            $expected = "Pass"
            $actual = "Fail"
            
            $actual | Should -Be $expected -Because "This test demonstrates GitHub Actions annotations for test failures. DELETE THIS FILE after verifying the feature works."
        }
    }
}
