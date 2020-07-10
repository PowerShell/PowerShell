# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Using of ternary operator" -Tags CI {
    BeforeAll {
        $testCases = @(
            ## Condition: variable and constant expressions
            @{ Script = { $true ? 1 : 2 };  ExpectedValue = 1 }
            @{ Script = { $true? ?1 :2 };   ExpectedValue = 2 }
            @{ Script = { ${true}?1:2 };    ExpectedValue = 1 }
            @{ Script = { 1 ? 1kb : 0xf };  ExpectedValue = 1kb }
            @{ Script = { 0 ?1kb:0xf };     ExpectedValue = 15 }
            @{ Script = { 's' ?1kb:0xf };   ExpectedValue = 1kb }
            @{ Script = { $null ?1kb:0xf }; ExpectedValue = 15 }
            @{ Script = { '' ?1kb:0xf };    ExpectedValue = 15 }

            ## Condition: other primary expressions
            @{ Script = { 1,2,3,4 ? 'Core' : 'Desktop' };          ExpectedValue = 'Core' }
            @{ Script = { @(1,2,3,4) ? 'Core' : 'Desktop' };       ExpectedValue = 'Core' }
            @{ Script = { @{name = 'name'} ? 'Core' : 'Desktop' };  ExpectedValue = 'Core' }
            @{ Script = { @{name = 'name'}.name ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }
            @{ Script = { @{name = 'name'}.Contains('name') ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }
            @{ Script = { (Test-Path Env:\NonExist) ? 'true' : 'false' };     ExpectedValue = 'false' }
            @{ Script = { (Test-Path Env:\PSModulePath) ? 'true' : 'false' }; ExpectedValue = 'true' }
            @{ Script = { $($p = Get-Process -Id $PID; $p.Id -eq $PID) ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }
            @{ Script = { ($a = 1) ? 2 : 3 };  ExpectedValue = 2 }
            @{ Script = { $($a = 1) ? 2 : 3 }; ExpectedValue = 3 }
            @{ Script = { (Write-Warning -Message warning -WarningAction SilentlyContinue) ? 1 : 2 }; ExpectedValue = 2 }
            @{ Script = { (Write-Error -Message error -ErrorAction SilentlyContinue) ? 1 : 2 };     ExpectedValue = 2 }

            ## Condition: unary and binary expression expressions
            @{ Script = { -not $IsCoreCLR ? 'Desktop' : 'Core' };             ExpectedValue = 'Core' }
            @{ Script = { $PSEdition -eq 'Core' ? 'Core' : 'Desktop' };       ExpectedValue = 'Core' }
            @{ Script = { $IsCoreCLR -and (Get-Process -Id $PID).Id -eq $PID ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }
            @{ Script = { $IsCoreCLR -and 'pwsh' -match 'p.*h' ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }
            @{ Script = { 1,2,3 -contains 2 ? 'Core' : 'Desktop' }; ExpectedValue = 'Core' }

            ## Nested ternary expressions
            @{ Script = { $IsCoreCLR ? $false ? 'nested-if-true' : 'nested-if-false' : 'if-false' }; ExpectedValue = 'nested-if-false' }
            @{ Script = { $IsCoreCLR ? $false ? 'nested-if-true' : $true ? 'nested-nested-if-true' : 'nested-nested-if-false' : 'if-false' }; ExpectedValue = 'nested-nested-if-true' }

            ## Binary operator has higher precedence order than ternary
            @{ Script = { !$IsCoreCLR ? 'Core' : 'Desktop' -EQ 'Core' };  ExpectedValue = !$IsCoreCLR ? 'Core' : ('Desktop' -eq 'Core') }
            @{ Script = { ($IsCoreCLR ? 'Core' : 'Desktop') -eq 'Core' }; ExpectedValue = $true }
        )
    }

    AfterAll {
        if ($skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        }
    }

    It "Ternary expression '<Script>' should generate correct results" -TestCases $testCases {
        param($Script, $ExpectedValue)
        & $Script | Should -BeExactly $ExpectedValue
    }

    It "Ternary expression which generates a terminating error should halt appropriately" {
        { (Write-Error -Message error -ErrorAction Stop) ? 1 : 2 } | Should -Throw -ErrorId Microsoft.PowerShell.Commands.WriteErrorException
    }

    It "Use ternary operator in parameter default values" {
        function testFunc {
            param($psExec = $IsCoreCLR ? 'pwsh' : 'powershell.exe')
            $psExec
        }
        testFunc | Should -BeExactly 'pwsh'
    }

    It "Use ternary operator with assignments" {
        $IsCoreCLR ? ([string]$var = 'string') : 'blah' > $null
        $var = [System.IO.FileInfo]::new('abc')
        $var | Should -BeOfType string
        $var | Should -BeExactly 'abc'
    }

    It "Use ternary operator in pipeline" {
        $result = $IsCoreCLR ? 'Core' : 'Desktop' | ForEach-Object { $_ + '-Pipe' }
        $result | Should -BeExactly 'Core-Pipe'
    }

    It "Return script block from ternary expression" {
        $result = ${IsCoreCLR}?{'Core'}:{'Desktop'}
        $result | Should -BeOfType scriptblock
        & $result | Should -BeExactly 'Core'
    }

    It "Tab completion for variables assigned with ternary expression should work as expected" {
        ## Type inference for the ternary expression should aggregate the inferred values from both branches
        $text1 = '$var1 = $IsCoreCLR ? (Get-Item $PSHOME) : (Get-Process -Id $PID); $var1.Full'
        $result = TabExpansion2 -inputScript $text1 -cursorColumn $text1.Length
        $result.CompletionMatches[0].CompletionText | Should -BeExactly FullName

        $text2 = '$var1 = $IsCoreCLR ? (Get-Item $PSHOME) : (Get-Process -Id $PID); $var1.Proce'
        $result = TabExpansion2 -inputScript $text2 -cursorColumn $text2.Length
        $result.CompletionMatches[0].CompletionText | Should -BeExactly ProcessName

        $text3 = '$IsCoreCLR ? ($var2 = Get-Item $PSHOME) : "blah"; $var2.Full'
        $result = TabExpansion2 -inputScript $text3 -cursorColumn $text3.Length
        $result.CompletionMatches[0].CompletionText | Should -BeExactly FullName
    }
}
