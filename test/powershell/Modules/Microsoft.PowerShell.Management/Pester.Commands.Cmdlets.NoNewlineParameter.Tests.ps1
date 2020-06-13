# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Tests related to TFS item 1370133 [PSUpgrade] Need -NoNewline parameter on Out-File, Add-Content and Set-Content
# Connect request https://connect.microsoft.com/PowerShell/feedback/details/524739/need-nonewline-parameter-on-out-file-add-content-and-set-content

Describe "Tests for -NoNewline parameter of Out-File, Add-Content and Set-Content" -tags "Feature" {

    It "NoNewline parameter works on Out-File" {
         $temp = "${TESTDRIVE}/test1.txt"
         1..5 | Out-File $temp -Encoding 'ASCII' -NoNewline
         (Get-Content $temp -AsByteStream).Count | Should -Be 5
    }

    It "NoNewline parameter works on Set-Content" {
         $temp = "${TESTDRIVE}/test2.txt"
         Set-Content -Path $temp -Value 'a','b','c' -Encoding 'ASCII' -NoNewline
         (Get-Content $temp -AsByteStream).Count | Should -Be 3
    }

    It "NoNewline parameter works on Add-Content" {
         $temp = "${TESTDRIVE}/test3.txt"
         $temp = New-TemporaryFile
         1..9 | ForEach-Object {Add-Content -Path $temp -Value $_ -Encoding 'ASCII' -NoNewline}
         (Get-Content $temp -AsByteStream).Count | Should -Be 9
    }
}
