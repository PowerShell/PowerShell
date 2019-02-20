# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Import-Module HelpersCommon
$moduleRootFilePath = Split-Path -Path $PSScriptRoot -Parent

# Identify the repository root path of the resource module
$repoRootPath = (Resolve-Path -LiteralPath (Join-path $moduleRootFilePath "../..")).ProviderPath
$repoRootPathFound = $false

Describe 'Common Tests - Validate Markdown Files' -Tag 'CI' {
    BeforeAll {
        Push-Location $psscriptroot
        $skip = $false
        $NpmInstalled = "not installed"
        if (Get-Command -Name 'npm' -ErrorAction SilentlyContinue)
        {
            $NpmInstalled = "Installed"
            Write-Verbose -Message "NPM is checking Gulp is installed. This may take a few moments." -Verbose
            start-nativeExecution { npm install --silent }
            start-nativeExecution { npm install 'gulp@4.0.0' --silent }
            if(!(Get-Command -Name 'gulp' -ErrorAction SilentlyContinue))
            {
                start-nativeExecution {
                    sudo npm install -g 'gulp@4.0.0' --silent
                    # Sometimes this folder is left behind with root permissions and is needed by later NPM installs which don't need sudo
                    sudo rm -rf ~/.npm/_cacache
                }
            }
            if(!(Get-Command -Name 'node' -ErrorAction SilentlyContinue))
            {
                throw "node not found"
            }
        }
        if(!(Get-Command -Name 'node' -ErrorAction SilentlyContinue))
        {
            <#
                On Windows, pre-requisites are missing
                For now we will skip, and write a warning.  Work to resolve this is tracked in:
                https://github.com/PowerShell/PowerShell/issues/3429
            #>
            Write-Warning "Node and npm are required to run this test"
            $skip = $true
        }

        $mdIssuesPath = Join-Path -Path $PSScriptRoot -ChildPath "markdownissues.txt"
        Remove-Item -Path $mdIssuesPath -Force -ErrorAction SilentlyContinue
    }

    AfterAll {
        Pop-Location
    }

    It "Should not have errors in any markdown files" -skip:$skip {
        $NpmInstalled | should BeExactly "Installed"
        $mdErrors = 0
        Push-Location -Path $PSScriptRoot
        try
        {
            $docsToTest = @(
                './.github/*.md'
                './README.md'
                './demos/python/*.md'
                './docker/*.md'
                './docs/building/*.md'
                './docs/community/*.md'
                './docs/host-powershell/*.md'
                './docs/cmdlet-example/*.md'
                './docs/maintainers/*.md'
                './test/powershell/README.md'
                './tools/*.md'
                './.github/ISSUE_TEMPLATE/*.md'
            )
            $filter = ($docsToTest -join ',')

            # Gulp 4 beta is returning non-zero exit code even when there is not an error
            Start-NativeExecution {
                    &"gulp" test-mdsyntax --silent `
                        --rootpath $repoRootPath `
                        --filter $filter
                } -VerboseOutputOnError -IgnoreExitcode

        }
        finally
        {
            Pop-Location
        }

        $mdIssuesPath | Should -Exist

        [string[]] $markdownErrors = Get-Content -Path $mdIssuesPath
        Remove-Item -Path $mdIssuesPath -Force -ErrorAction SilentlyContinue

        if ($markdownErrors -ne "--EMPTY--")
        {
            $markdownErrors += ' (See https://github.com/DavidAnson/markdownlint/blob/master/doc/Rules.md for an explanation of the error codes)'
        }

        $markdownErrors -join "`n" | Should -BeExactly "--EMPTY--"
    }
}
