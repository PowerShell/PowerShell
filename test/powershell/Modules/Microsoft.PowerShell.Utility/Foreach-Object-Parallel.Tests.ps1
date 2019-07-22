# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'ForEach-Object -Parallel Basic Tests' -Tags 'CI' {

    BeforeAll {

        $sb = { "Hello!" }
    }

    It "Verifies dollar underbar variable" {

        $expected = 1..10
        $result = $expected | ForEach-Object -Parallel -ScriptBlock { $_ }
        $result.Count | Should -BeExactly $expected.Count
        $result | Should -Contain 1
        $result | Should -Contain 10
    }

    It 'Verifies using variables' {

        $var = "Hello"
        $varArray = "Hello","There"
        $result = 1..1 | ForEach-Object -Parallel -ScriptBlock { $using:var; $using:varArray[1] }
        $result.Count | Should -BeExactly 2
        $result[0] | Should -BeExactly $var
        $result[1] | Should -BeExactly $varArray[1]
    }

    It 'Verifies non-terminating error streaming' {

        $expectedError = 1..1 | ForEach-Object -Parallel -ScriptBlock { Write-Error "Error!" } 2>&1
        $expectedError.ToString() | Should -BeExactly 'Error!'
        $expectedError.FullyQualifiedErrorId | Should -BeExactly 'Microsoft.PowerShell.Commands.WriteErrorException'
    }

    It 'Verifies terminating error streaming' {

        $result = 1..1 | ForEach-Object -Parallel -ScriptBlock { throw 'Terminating Error!'; "Hello" } 2>&1
        $result.Count | Should -BeExactly 1
        $result.ToString() | Should -BeExactly 'Terminating Error!'
        $result.FullyQualifiedErrorId | Should -BeExactly 'PSTaskException'
    }

    It 'Verifies warning data streaming' {

        $expectedWarning = 1..1 | ForEach-Object -Parallel -ScriptBlock { Write-Warning "Warning!" } 3>&1
        $expectedWarning.Message | Should -BeExactly 'Warning!'
    }

    It 'Verifies verbose data streaming' {

        $expectedVerbose = 1..1 | ForEach-Object -Parallel -ScriptBlock { Write-Verbose "Verbose!" -Verbose } -Verbose 4>&1
        $expectedVerbose.Message | Should -BeExactly 'Verbose!'
    }

    It 'Verifies debug data streaming' {
    
        $expectedDebug = 1..1 | ForEach-Object -Parallel -ScriptBlock { Write-Debug "Debug!" -Debug } -Debug 5>&1
        $expectedDebug.Message | Should -BeExactly 'Debug!'
    }

    It 'Verifies information data streaming' {

        $expectedInformation = 1..1 | ForEach-Object -Parallel -ScriptBlock { Write-Information "Information!" } 6>&1
        $expectedInformation.MessageData | Should -BeExactly 'Information!'
    }

    It 'Verifies error for using script block variable' {

        { 1..1 | ForEach-Object -Parallel -ScriptBlock { $using:sb } } | Should -Throw -ErrorId 'ParallelUsingVariableCannotBeScriptBlock,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It 'Verifies error for script block piped variable' {
    
        { $sb | ForEach-Object -Parallel -ScriptBlock { "Hello" } -ErrorAction Stop } | Should -Throw -ErrorId 'ParallelPipedInputObjectCannotBeScriptBlock,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }
}

Describe 'ForEach-Object -Parallel Functional Tests' -Tags 'Feature' {

    It 'Verifies timeout and throttle parameters' {

        # With ThrottleLimit set to 1, the two 60 second long script blocks will run sequentially, 
        # until the timeout in 5 seconds.
        $results = 1..2 | ForEach-Object -Parallel { "Output $_"; Start-Sleep -Seconds 60 } -TimeoutSeconds 5 -ThrottleLimit 1 2>&1
        $results.Count | Should -BeExactly 2
        $results[0] | Should -BeExactly 'Output 1'
        $results[1].FullyQualifiedErrorId | Should -BeExactly 'PSTaskException'
        $results[1].Exception | Should -BeOfType [System.Management.Automation.PipelineStoppedException]
    }
}
