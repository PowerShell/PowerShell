$removeAliasList = @("ac","compare","cpp","diff","sleep","sort","start","cat","cp","ls","man","mount","mv","ps","rm","rmdir")
$keepAliasList = @{cd="Set-Location"},@{dir="Get-ChildItem"},@{echo="Write-output"},@{fc="format-custom"},@{kill="stop-process"},@{clear="clear-host"}
Describe "Windows aliases do not conflict with Linux commands" -Tags "CI" {
    foreach ($alias in $removeAliasList) {
        It "Should not have certain aliases on Linux" -Skip:$IsWindows {
            Test-Path Alias:$alias | Should Be $false
        }
    }

    foreach ($alias in $keepAliasList) {
        It "Should have aliases that are Bash built-ins on Linux" {
            (Get-Alias $alias.Keys).Definition | Should Be $alias.Values
        }
    }

    It "Should have more as a function" {
        Test-Path Function:more | Should Be $true
    }
}
