# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Attribute tests" -Tags "CI" {
    BeforeEach {
        Remove-Item $testdrive/test.ps1 -Force -ErrorAction SilentlyContinue
    }

    It "<attribute> attribute returns argument error if empty" -TestCases @(
        @{Attribute="HelpMessage"},
        @{Attribute="HelpMessageBaseName"},
        @{Attribute="HelpMessageResourceId"}
    ) {
        param($attribute)

        $script = @"
[CmdletBinding()]
Param (
[Parameter($attribute="")]
[String]`$Parameter1
)
Write-Output "Hello"
"@
        New-Item -Path $testdrive/test.ps1 -Value $script -ItemType File
        { & $testdrive/test.ps1 } | Should -Throw -ErrorId "Argument"
    }
}
