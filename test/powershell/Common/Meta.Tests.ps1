using namespace System.Text.RegularExpressions

# We run the Meta tests only on AppVeyor CI and locally
if ($env:TRAVIS) {
    $skipTests = $true
}

# Get a root repository folder
try {
    if ($env:APPVEYOR) {
        $rootPath = $env:APPVEYOR_BUILD_FOLDER
    } else {
        # Local test
        $rootPath = Join-Path $PSScriptRoot $(git rev-parse --show-cdup)
    }
} catch {
    # Skip tests if no GIT
    $skipTests = $true
}

if (!$skipTests) {

    # We check only files with following extensions
    $extensions = "*.ps1", "*.psm1", "*.cs", ".resx"

    # Output warnings with failed files
    $IsWarning = $true

    $AllFiles = $false

    if ($AllFiles) {
        # We check all files only on demand
        $checkPaths = Get-ChildItem -Path $rootPath -Include $extensions -Recurse | Select-Object -ExpandProperty FullName
    } else {
        # On CI we check only files changed in PR
        $checkPaths = @(git diff --name-only origin/master..) -match $extensionRegex | ForEach-Object { Resolve-Path (Join-Path $rootPath $_) }
    }

    $WrongEncodedFiles = $TabsInFiles = $EmptyFiles = $NoNewlineFiles = $NoHttpsFiles = @()
    $tabsRegEx  = [Regex]::new('(?m)^\s*[\t]+\s*\w*|[ \t]+\r?$', [RegexOptions]::Multiline+[RegexOptions]::Compiled)

    # The regex catch itself so mask it
    $strHttp = '(?m)=(.|\n)*"http'
    $strHttp += '://.*?"'
    $HttpRegEx  = [Regex]::new($strHttp, [RegexOptions]::Multiline+[RegexOptions]::Compiled)

    foreach ($file in $checkPaths) {

        Write-Host "Check file: $file"
        $text = [System.IO.File]::ReadAllText($file)
        if ($text -eq "") {
            $EmptyFiles += ,$file
            continue
        }

        if ($tabsRegEx.Match($text).Success) {
            $TabsInFiles += ,$file
        }

        if ($HttpRegEx.Match($text).Success) {
            $NoHttpsFiles += ,$file
        }

        if ($text[-1] -ne "`n") {
            $NoNewlineFiles += ,$file
        }

    }
}

Describe 'Common Tests - File Formatting' -Tags "CI" {
    BeforeAll {
        $defaultParamValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues["It:Skip"] = $skipTests
    }

    AfterAll {
        $global:PSDefaultParameterValues = $defaultParamValues

        if ($IsWarning) {
            if ($WrongEncodedFiles.Count -gt 0) {
                Write-Warning "Wrong Encoded Files: $($WrongEncodedFiles -join [Environment]::Newline)"
            }
            if ($TabsInFiles.Count -gt 0) {
                Write-Warning "Files with leading tabs and trailing spaces and tabs: $($TabsInFiles -join [Environment]::Newline)"
            }
            if ($EmptyFiles.Count -gt 0) {
                Write-Warning "Empty Files: $($EmptyFiles -join [Environment]::Newline)"
            }
            if ($NoNewlineFiles.Count -gt 0) {
                Write-Warning "Files without Newline in the End: $($NoNewlineFiles -join [Environment]::Newline)"
            }
            if ($NoHttpsFiles.Count -gt 0) {
                Write-Warning "Files with HTTP links: $($NoHttpsFiles -join [Environment]::Newline)"
            }
        }
    }

    It "Should not contain any files with non-Unicode file encoding" -Pending:$true <#-Skip:$skipTests#> {
        $WrongEncodedFiles.Count | Should Be 0
    }

    It 'Should not contain any files with leading tab characters and trailing spaces and tab characters' {
        $TabsInFiles.Count | Should Be 0
    }

    It 'Should not contain empty files' {
        $EmptyFiles.Count | Should Be 0
    }

    It 'Should not contain files without a newline at the file end' {
        $NoNewlineFiles.Count | Should Be 0
    }
    It 'Should not contain files with HTTP links' {
        $NoHttpsFiles.Count | Should Be 0
    }
}
