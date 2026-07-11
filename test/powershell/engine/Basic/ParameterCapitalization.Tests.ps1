# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Guardrail for parameter casing in PowerShell tests (see #26614).
# Parameters are case-insensitive at runtime, but source style prefers PascalCase
# (e.g. -Force, -ErrorAction) for readability and consistency with docs/examples.

Describe "Parameter capitalization in PowerShell test scripts" -Tags @("CI") {
    BeforeAll {
        $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path
        $scanRoot = Join-Path $repoRoot "test/powershell"

        # High-frequency switches that should appear PascalCase in source.
        # Intentional lowercase uses (binding case-insensitivity tests, stream fixtures)
        # are allowlisted by relative path below.
        $parameterPattern = '(?<=[\s|])-(force|erroraction|warningaction|verbose|debug|whatif|confirm)(?=[\s:\|]|$)'

        $allowlistRelativePaths = @(
            "test/powershell/Language/Scripting/CommonParameters.Tests.ps1"
            "test/powershell/Language/Parser/BNotOperator.Tests.ps1"
            "test/powershell/Language/Parser/RedirectionOperator.Tests.ps1"
            "test/powershell/engine/Basic/ParameterCapitalization.Tests.ps1"
        )
    }

    It "Uses PascalCase for common parameters (no bare lowercase -force / -erroraction / etc.)" {
        $files = Get-ChildItem -Path $scanRoot -Recurse -Include *.ps1, *.psm1 -File -ErrorAction Stop

        $violations = [System.Collections.Generic.List[string]]::new()
        foreach ($file in $files) {
            $rel = $file.FullName.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
            if ($allowlistRelativePaths -contains $rel) {
                continue
            }

            $matches = Select-String -Path $file.FullName -Pattern $parameterPattern -CaseSensitive -AllMatches
            foreach ($m in $matches) {
                $trim = $m.Line.TrimStart()
                # Comments that only discuss the switch are fine.
                if ($trim.StartsWith('#')) {
                    continue
                }
                $violations.Add(("{0}:{1}: {2}" -f $rel, $m.LineNumber, $m.Line.Trim()))
            }
        }

        if ($violations.Count -gt 0) {
            $header = "Found $($violations.Count) lowercase common-parameter(s). Prefer PascalCase (e.g. -Force, -ErrorAction). See PowerShell/PowerShell#26614."
            throw ($header + [Environment]::NewLine + ($violations -join [Environment]::NewLine))
        }

        $violations.Count | Should -Be 0
    }
}