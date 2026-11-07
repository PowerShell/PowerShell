# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Module analysis cache" -Tags 'CI' {
    BeforeAll {
        Import-Module HelpersCommon
    }

    It "Saves the analysis of an auto-loaded module when a short-lived process exits" {
        $cachePath = Join-Path $TestDrive 'ModuleAnalysisCache'
        $oldCachePath = $env:PSModuleAnalysisCachePath
        try {
            $env:PSModuleAnalysisCachePath = $cachePath

            # Get-ChildItem is not loaded in a new session, so it is found through module auto-loading.
            Start-NativeExecution { & "$PSHOME/pwsh" -NoProfile -NonInteractive -Command 'Get-ChildItem -Path $PSHOME | Out-Null' }
        }
        finally {
            $env:PSModuleAnalysisCachePath = $oldCachePath
        }

        $cachePath | Should -Exist
        (Get-Item $cachePath).Length | Should -BeGreaterThan 0
    }
}
