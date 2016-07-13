$removeAliasList = @{ Alias = "ac" }, 
    @{ Alias = "compare" },
    @{ Alias = "cpp" },
    @{ Alias = "diff" },
    @{ Alias = "sleep" },
    @{ Alias = "sort" },
    @{ Alias = "start" },
    @{ Alias = "cat" },
    @{ Alias = "cp" },
    @{ Alias = "ls" },
    @{ Alias = "man" },
    @{ Alias = "mount" },
    @{ Alias = "mv" },
    @{ Alias = "ps" },
    @{ Alias = "rm" },
    @{ Alias = "rmdir"} 

$keepAliasList = @{ Alias = "cd"; Definition = "Set-Location"},
    @{ Alias = "dir";   Definition = "Get-ChildItem"},
    @{ Alias = "echo";  Definition = "Write-output"},
    @{ Alias = "fc";    Definition = "format-custom"},
    @{ Alias = "kill";  Definition = "stop-process"},
    @{ Alias = "clear"; Definition = "clear-host"}

Describe "Windows aliases do not conflict with Linux commands" {

    It "Should not have alias '<Alias>' on Linux" -Skip:$IsWindows -testcases $removeAliasList {
        param ( $alias )
        get-alias $alias -ea silentlycontinue | should benullorempty
    }

    It "Should have alias '<alias>' defined as <definition>" -testcases $keepAliasList {
        param ( $alias, $definition )
        (Get-Alias $alias).Definition | Should Be $definition
    }

    It "Should have more as a function" {
        Test-Path Function:more | Should Be $true
    }
}
