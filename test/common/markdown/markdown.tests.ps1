$moduleRootFilePath = Split-Path -Path $PSScriptRoot -Parent

# Identify the repository root path of the resource module
$repoRootPath = (Resolve-Path -LiteralPath (Join-path $moduleRootFilePath "../..")).ProviderPath
Write-Verbose -message "RepoRoot: $repoRootPath" -Verbose
Write-Verbose -message "PSScriptRoot: $PSScriptRoot" -Verbose
$repoRootPathFound = $false

Describe 'Common Tests - Validate Markdown Files' -Tag 'CI' {
    if (Get-Command -Name 'npm' -ErrorAction SilentlyContinue)
    {
        Write-Warning -Message "NPM is checking Gulp is installed. This may take a few moments."

        $null = Start-Process `
            -FilePath "npm" `
            -ArgumentList @('install','--silent') `
            -Wait `
            -WorkingDirectory $PSScriptRoot `
            -PassThru `
            -NoNewWindow
        $null = Start-Process `
            -FilePath "npm" `
            -ArgumentList @('install','-g','gulp','--silent') `
            -Wait `
            -WorkingDirectory $PSScriptRoot `
            -PassThru `
            -NoNewWindow

        It "Should not have errors in any markdown files" {

            $mdErrors = 0
            Push-Location -Path $PSScriptRoot
            try
            {
                $docsToTest = @(
                    './*.md'
                    './docs/installation/*.md'
                )
                $filter = ($docsToTest -join ',')
                Write-Verbose "Filter: $filter" -Verbose
                &"gulp" test-mdsyntax --silent `
                    --rootpath $repoRootPath `
                    --filter $filter

                Start-Sleep -Seconds 3
            }
            catch [System.Exception]
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

            Write-Verbose "$mdIssuesPath should exist" -Verbose
            $mdIssuesPath | should exist

            Get-Content -Path $mdIssuesPath | ForEach-Object -Process {
                if ([string]::IsNullOrEmpty($_) -eq $false -and $_ -ne '--EMPTY--')
                {
                    Write-Warning -Message $_
                    $mdErrors ++
                }
            }

            Remove-Item -Path $mdIssuesPath -Force -ErrorAction SilentlyContinue

            if($mdErrors -gt 0)
            {
                Write-Warning 'See https://github.com/DavidAnson/markdownlint/blob/master/doc/Rules.md for an explination of the error codes.'
            }

            $mdErrors | Should Be 0
        }

        # We're using this tool to delete the node_modules folder because it gets too long
        # for PowerShell to remove.
        $null = Start-Process `
            -FilePath "npm" `
            -ArgumentList @('install','rimraf','-g','--silent') `
            -Wait `
            -WorkingDirectory $PSScriptRoot `
            -PassThru `
            -NoNewWindow
        $null = Start-Process `
            -FilePath "rimraf" `
            -ArgumentList @(Join-Path -Path $PSScriptRoot -ChildPath 'node_modules') `
            -Wait `
            -WorkingDirectory $PSScriptRoot `
            -PassThru `
            -NoNewWindow
    }
    else
    {
        Write-Warning -Message ("Unable to run gulp to test markdown files. Please " + `
                                "be sure that you have installed nodejs and npm in order " + `
                                "to have this text execute.")
    }
}
