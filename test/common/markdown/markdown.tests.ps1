# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
$moduleRootFilePath = Split-Path -Path $PSScriptRoot -Parent

# Identify the repository root path of the resource module
$repoRootPath = (Resolve-Path -LiteralPath (Join-path $moduleRootFilePath "../..")).ProviderPath
$repoRootPathFound = $false

Describe 'Common Tests - Validate Markdown Files' -Tag 'CI' {
    BeforeAll {
        # Skip if not windows, We don't need these tests to run on linux (the tests run fine in travis-ci)
        $skip = !$IsWindows
        if ( !$skip )
        {
            $NpmInstalled = "not installed"
            if (Get-Command -Name 'npm' -ErrorAction SilentlyContinue)
            {
                $NpmInstalled = "Installed"
                Write-Verbose -Message "NPM is checking Gulp is installed. This may take a few moments." -Verbose
                Start-Process `
                    -FilePath "npm" `
                    -ArgumentList @('install','--silent') `
                    -Wait `
                    -WorkingDirectory $PSScriptRoot `
                    -NoNewWindow
                Start-Process `
                    -FilePath "npm" `
                    -ArgumentList @('install','-g','gulp','--silent') `
                    -Wait `
                    -WorkingDirectory $PSScriptRoot `
                    -NoNewWindow
            }
            elseif( -not $env:AppVeyor)
            {
                <#
                    On Windows, but not an AppVeyor and pre-requisites are missing
                    For now we will skip, and write a warning.  Work to resolve this is tracked in:
                    https://github.com/PowerShell/PowerShell/issues/3429
                #>
                Write-Warning "Node and npm are required to run this test"
                $skip = $true
            }
        }
    }

    AfterAll {
        if ( !$skip )
        {
            <#
                NPM install all the tools needed to run this test in the test folder.
                We will now clean these up.
                We're using this tool to delete the node_modules folder because it gets too long
                for PowerShell to remove.
            #>
            Start-Process `
                -FilePath "npm" `
                -ArgumentList @('install','rimraf','-g','--silent') `
                -Wait `
                -WorkingDirectory $PSScriptRoot `
                -NoNewWindow
            Start-Process `
                -FilePath "rimraf" `
                -ArgumentList @(Join-Path -Path $PSScriptRoot -ChildPath 'node_modules') `
                -Wait `
                -WorkingDirectory $PSScriptRoot `
                -NoNewWindow
        }
    }

    It "Should not have errors in any markdown files" -Skip:$skip {
        $NpmInstalled | should BeExactly "Installed"
        $mdErrors = 0
        Push-Location -Path $PSScriptRoot
        try
        {
            $docsToTest = @(
                './.github/CONTRIBUTING.md'
                './*.md'
                './demos/python/*.md'
                './docker/*.md'
                './docs/*.md'
                './docs/building/*.md'
                './docs/cmdlet-example/*.md'
                './docs/git/submodules.md'
                './docs/installation/*.md'
                './docs/maintainers/*.md'
                './docs/testing-guidelines/testing-guidelines.md'
                './test/powershell/README.md'
                './tools/*.md'
            )
            $filter = ($docsToTest -join ',')
            &"gulp" test-mdsyntax --silent `
                --rootpath $repoRootPath `
                --filter $filter

        }
        catch
        {
            Write-Warning -Message ("Unable to run gulp to test markdown files. Please " + `
                                    "be sure that you have installed nodejs and have " + `
                                    "run 'npm install -g gulp' in order to have this " + `
                                    "text execute.")
        }
        finally
        {
            Pop-Location
        }

        $LASTEXITCODE | Should beexactly 0

        $mdIssuesPath = Join-Path -Path $PSScriptRoot -ChildPath "markdownissues.txt"

        $mdIssuesPath | should exist

        [string] $markdownErrors = Get-Content -Path $mdIssuesPath
        Remove-Item -Path $mdIssuesPath -Force -ErrorAction SilentlyContinue

        if ($markdownErrors -ne "--EMPTY--")
        {
            $markdownErrors += ' (See https://github.com/DavidAnson/markdownlint/blob/master/doc/Rules.md for an explanation of the error codes)'
        }

        $markdownErrors | Should BeExactly "--EMPTY--"
    }
}
