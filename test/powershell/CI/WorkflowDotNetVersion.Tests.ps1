# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "GitHub Actions Workflow .NET Version Configuration" -Tags CI {
    BeforeAll {
        $repoRoot = (Resolve-Path "$PSScriptRoot/../../..").Path
        $globalJsonPath = Join-Path $repoRoot "global.json"
        
        # Parse global.json to get expected SDK version
        $globalJson = Get-Content $globalJsonPath -Raw | ConvertFrom-Json
        $expectedSdkVersion = $globalJson.sdk.version
        
        # Find all workflow YAML files
        $workflowFiles = Get-ChildItem -Path "$repoRoot/.github/workflows" -Filter "*.yml" -File
        $workflowFiles += Get-ChildItem -Path "$repoRoot/.github/workflows" -Filter "*.yaml" -File -ErrorAction SilentlyContinue
        
        # Find all composite action YAML files
        $actionFiles = Get-ChildItem -Path "$repoRoot/.github/actions" -Recurse -Filter "action.yml" -File
        $actionFiles += Get-ChildItem -Path "$repoRoot/.github/actions" -Recurse -Filter "action.yaml" -File -ErrorAction SilentlyContinue
        
        $script:allYamlFiles = @($workflowFiles) + @($actionFiles)
        
        # Prepare test cases for files that use setup-dotnet
        $script:setupDotnetTestCases = @()
        foreach ($file in $script:allYamlFiles) {
            $content = Get-Content $file.FullName -Raw
            if ($content -match 'actions/setup-dotnet@') {
                $script:setupDotnetTestCases += @{
                    FileName = $file.Name
                    FilePath = $file.FullName
                    Content = $content
                }
            }
        }
    }

    Context "global.json validation" {
        It "global.json file should exist at repository root" {
            $globalJsonPath | Should -Exist
        }

        It "global.json should contain a valid SDK version" {
            $expectedSdkVersion | Should -Not -BeNullOrEmpty
            $expectedSdkVersion | Should -Match '^\d+\.\d+\.\d+'
        }
    }

    Context "Workflow files .NET setup validation" {
        It "Should find workflow and action files to test" {
            $script:allYamlFiles.Count | Should -BeGreaterThan 0
        }

        It "Should find files using setup-dotnet action" {
            $script:setupDotnetTestCases.Count | Should -BeGreaterThan 0
        }

        It "All files using setup-dotnet should have global-json-file parameter" {
            $missingFiles = @()
            foreach ($testCase in $script:setupDotnetTestCases) {
                # Split content into lines and check for global-json-file parameter
                $lines = $testCase.Content -split "`r?`n"
                $hasValidParameter = $false
                
                foreach ($line in $lines) {
                    # Check if line contains global-json-file and is not commented
                    if ($line -match '^\s*global-json-file:\s*\.\\?/?global\.json\s*$' -and
                        $line -notmatch '^\s*#') {
                        $hasValidParameter = $true
                        break
                    }
                }
                
                if (-not $hasValidParameter) {
                    $missingFiles += $testCase.FileName
                }
            }
            
            if ($missingFiles.Count -gt 0) {
                throw "The following files use setup-dotnet but are missing global-json-file parameter: $($missingFiles -join ', ')"
            }
            
            # If we get here, all files passed
            $script:setupDotnetTestCases.Count | Should -BeGreaterThan 0
        }

        It "All files using setup-dotnet should reference correct global.json path" {
            $incorrectFiles = @()
            foreach ($testCase in $script:setupDotnetTestCases) {
                if ($testCase.Content -match 'global-json-file:\s*(\.\\?/?global\.json)') {
                    $path = $Matches[1]
                    $normalizedPath = $path -replace '\\', '/'
                    if ($normalizedPath -notmatch '^\./?global\.json$') {
                        $incorrectFiles += "$($testCase.FileName) (path: $path)"
                    }
                } else {
                    $incorrectFiles += "$($testCase.FileName) (missing global-json-file parameter)"
                }
            }
            
            if ($incorrectFiles.Count -gt 0) {
                throw "The following files have incorrect global.json paths: $($incorrectFiles -join ', ')"
            }
            
            # If we get here, all files passed
            $script:setupDotnetTestCases.Count | Should -BeGreaterThan 0
        }
    }

    Context "Composite Actions .NET setup validation" {
        BeforeAll {
            # Get all composite action files that are used for agent/workflow creation
            $ciActionPath = Join-Path $repoRoot ".github/actions/build/ci/action.yml"
            $nixTestActionPath = Join-Path $repoRoot ".github/actions/test/nix/action.yml"
            $windowsTestActionPath = Join-Path $repoRoot ".github/actions/test/windows/action.yml"
            
            $criticalActions = @(
                $ciActionPath,
                $nixTestActionPath,
                $windowsTestActionPath
            )
        }

        It "Critical build/test actions should exist" {
            foreach ($actionPath in $criticalActions) {
                $actionPath | Should -Exist
            }
        }

        foreach ($actionPath in $criticalActions) {
            It "Action '$($actionPath | Split-Path -Leaf)' should configure .NET SDK from global.json" {
                if (Test-Path $actionPath) {
                    $content = Get-Content $actionPath -Raw
                    $content | Should -Match 'actions/setup-dotnet@'
                    $content | Should -Match 'global-json-file:\s*\.\\?/?global\.json'
                }
            }
        }
    }
}
