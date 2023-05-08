# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'ForEach-Object -Parallel Basic Tests' -Tags 'CI' {

    BeforeAll {
        $sb = { "Hello!" }
        $psosb = [psobject]{ "Hello!" }
    }

    It "Verifies dollar underbar variable" {

        $expected = 1..10
        $result = $expected | ForEach-Object -Parallel { $_ }
        $result.Count | Should -BeExactly $expected.Count
        $result | Should -Contain 1
        $result | Should -Contain 10
    }

    It 'Verifies using variables' {

        $var = "Hello"
        $varArray = "Hello","There"
        $result = 1..1 | ForEach-Object -Parallel { $using:var; $using:varArray[1] }
        $result.Count | Should -BeExactly 2
        $result[0] | Should -BeExactly $var
        $result[1] | Should -BeExactly $varArray[1]
    }

    It 'Verifies in scope using variables in nested calls' {

        $Test = "Test1"
        $results = 1..2 | ForEach-Object -Parallel {
            $using:Test
            $Test = "Test2"
            1..2 | ForEach-Object -Parallel {
                $using:Test
                $Test = "Test3"
                1..2 | ForEach-Object -Parallel {
                    $using:Test
                }
            }
        }
        $results.Count | Should -BeExactly 14
        $groups = $results | Group-Object -AsHashTable
        $groups['Test1'].Count | Should -BeExactly 2
        $groups['Test2'].Count | Should -BeExactly 4
        $groups['Test3'].Count | Should -BeExactly 8
    }

    It 'Verifies in scope using variables with different names in nested calls' {
        $Test1 = "TestA"
        $results = 1..2 | ForEach-Object -parallel {
            $using:Test1
            $Test2 = "TestB"
            1..2 | ForEach-Object -parallel {
                $using:Test2
            }
        }
        $results.Count | Should -BeExactly 6
        $groups = $results | Group-Object -AsHashTable
        $groups['TestA'].Count | Should -BeExactly 2
        $groups['TestB'].Count | Should -BeExactly 4
    }

    It 'Verifies using variable in nested scriptblock' {

        $test = 'testC'
        $results = 1..2 | ForEach-Object -parallel {
            & { $using:test }
        }
        $results.Count | Should -BeExactly 2
        $groups = $results | Group-Object -AsHashTable
        $groups['TestC'].Count | Should -BeExactly 2
    }

    It 'Verifies expected error for out of scope using variable in nested calls' {

        $Test = "TestZ"
        1..1 | ForEach-Object -Parallel {
            $using:Test
            # Variable '$Test' is not defined in this scope.
            1..1 | ForEach-Object -Parallel {
                $using:Test
            }
        } -ErrorVariable usingErrors 2>$null

        $usingErrors[0].FullyQualifiedErrorId | Should -BeExactly 'UsingVariableIsUndefined,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It 'Verifies using variables with passed-in script block object' {

        $var = "Hello"
        $varArray = "Hello","There"
        $sBlock = [scriptblock]::Create('$using:var; $using:varArray[1]')
        $result = 1..1 | ForEach-Object -Parallel $sBlock
        $result[0] | Should -BeExactly $var
        $result[1] | Should -BeExactly $varArray[1]
    }

    It 'Verifies using variables with passed-in {} script block object' {

        $var = "Hello"
        $varArray = "Hello","There"
        $sBlock = { $using:var; $using:varArray[1] }
        $result = 1..1 | ForEach-Object -Parallel $sBlock
        $result[0] | Should -BeExactly $var
        $result[1] | Should -BeExactly $varArray[1]
    }

    It 'Verifies in scope using same-name variables in nested calls for passed-in script block objects' {

        $Test1 = "Test1"
        $sBlock = [scriptblock]::Create(@'
            $using:Test1
            $Test2 = "Test2"
            $sBlock2 = [scriptblock]::Create('$using:Test2')
            1..2 | ForEach-Object -Parallel $sBlock2
'@)
        $results = 1..2 | ForEach-Object -Parallel $sBlock
        $results.Count | Should -BeExactly 6
        $groups = $results | Group-Object -AsHashTable
        $groups['Test1'].Count | Should -BeExactly 2
        $groups['Test2'].Count | Should -BeExactly 4
    }

    It 'Verifies in scope using same-name variables in nested calls for passed-in {} script block objects' {

        $Test1 = "Test1"
        $sBlock = {
            $using:Test1
            $Test2 = "Test2"
            $sBlock2 = [scriptblock]::Create('$using:Test2')
            1..2 | ForEach-Object -Parallel $sBlock2
        }
        $results = 1..2 | ForEach-Object -Parallel $sBlock
        $results.Count | Should -BeExactly 6
        $groups = $results | Group-Object -AsHashTable
        $groups['Test1'].Count | Should -BeExactly 2
        $groups['Test2'].Count | Should -BeExactly 4
    }

    It 'Verifies in scope using same-name variables in nested calls for mixed script block objects' {

        $Test1 = "Test1"
        $sBlock = [scriptblock]::Create(@'
            $using:Test1
            $Test2 = "Test2"
            1..2 | ForEach-Object -Parallel { $using:Test2 }
'@)
        $results = 1..2 | ForEach-Object -Parallel $sBlock
        $results.Count | Should -BeExactly 6
        $groups = $results | Group-Object -AsHashTable
        $groups['Test1'].Count | Should -BeExactly 2
        $groups['Test2'].Count | Should -BeExactly 4
    }

    It 'Verifies in scope using same-name variables in nested calls for mixed script block {} objects' {

        $Test1 = "Test1"
        $sBlock = {
            $using:Test1
            $Test2 = "Test2"
            1..2 | ForEach-Object -Parallel { $using:Test2 }
        }
        $results = 1..2 | ForEach-Object -Parallel $sBlock
        $results.Count | Should -BeExactly 6
        $groups = $results | Group-Object -AsHashTable
        $groups['Test1'].Count | Should -BeExactly 2
        $groups['Test2'].Count | Should -BeExactly 4
    }

    It 'Verifies in scope using different-name variables in nested calls for passed-in script block objects' {

        $Test1 = "Test1"
        $sBlock = [scriptblock]::Create(@'
            $using:Test1
            $Test2 = "Test2"
            $sBlock2 = [scriptblock]::Create('$using:Test2')
            1..2 | ForEach-Object -Parallel $sBlock2
'@)
        $results = 1..2 | ForEach-Object -Parallel $sBlock
        $results.Count | Should -BeExactly 6
        $groups = $results | Group-Object -AsHashTable
        $groups['Test1'].Count | Should -BeExactly 2
        $groups['Test2'].Count | Should -BeExactly 4
    }

    It 'Verifies in scope using different-name variables in nested calls for passed-in script {} block objects' {

        $Test1 = "Test1"
        $sBlock = {
            $using:Test1
            $Test2 = "Test2"
            $sBlock2 = [scriptblock]::Create('$using:Test2')
            1..2 | ForEach-Object -Parallel $sBlock2
        }
        $results = 1..2 | ForEach-Object -Parallel $sBlock
        $results.Count | Should -BeExactly 6
        $groups = $results | Group-Object -AsHashTable
        $groups['Test1'].Count | Should -BeExactly 2
        $groups['Test2'].Count | Should -BeExactly 4
    }

    It 'Verifies in scope using different-name variables in nested calls for mixed script block objects' {

        $Test1 = "Test1"
        $sBlock = [scriptblock]::Create(@'
            $using:Test1
            $Test2 = "Test2"
            1..2 | ForEach-Object -Parallel { $using:Test2 }
'@)
        $results = 1..2 | ForEach-Object -Parallel $sBlock
        $results.Count | Should -BeExactly 6
        $groups = $results | Group-Object -AsHashTable
        $groups['Test1'].Count | Should -BeExactly 2
        $groups['Test2'].Count | Should -BeExactly 4
    }

    It 'Verifies in scope using different-name variables in nested calls for mixed script block {} objects' {

        $Test1 = "Test1"
        $sBlock = {
            $using:Test1
            $Test2 = "Test2"
            1..2 | ForEach-Object -Parallel { $using:Test2 }
        }
        $results = 1..2 | ForEach-Object -Parallel $sBlock
        $results.Count | Should -BeExactly 6
        $groups = $results | Group-Object -AsHashTable
        $groups['Test1'].Count | Should -BeExactly 2
        $groups['Test2'].Count | Should -BeExactly 4
    }

    It 'Verifies expected error for out of scope using variable in nested calls for passed-in script block objects' {

        $Test = "TestZ"
        $sBlock = [scriptblock]::Create(@'
            $using:Test
            $sBlock2 = [scriptblock]::Create('$using:Test')
            1..1 | ForEach-Object -Parallel $sBlock2
'@)
        1..1 | ForEach-Object -Parallel $SBlock -ErrorVariable usingErrors 2>$null
        $usingErrors[0].FullyQualifiedErrorId | Should -BeExactly 'UsingVariableIsUndefined,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It 'Verifies expected error for out of scope using variable in nested calls for passed-in {} script block objects' {

        $Test = "TestZ"
        $sBlock = [scriptblock]::Create(@'
            $using:Test
            $sBlock2 = { $using:Test }
            1..1 | ForEach-Object -Parallel $sBlock2
'@)
        1..1 | ForEach-Object -Parallel $SBlock -ErrorVariable usingErrors 2>$null
        $usingErrors[0].FullyQualifiedErrorId | Should -BeExactly 'UsingVariableIsUndefined,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It 'Verifies expected error for out of scope using variable in nested calls for mixed script block objects' {

        $Test = "TestZ"
        $sBlock = [scriptblock]::Create(@'
            $using:Test
            1..1 | ForEach-Object -Parallel { $using:Test }
'@)
        1..1 | ForEach-Object -Parallel $SBlock -ErrorVariable usingErrors 2>$null
        $usingErrors[0].FullyQualifiedErrorId | Should -BeExactly 'UsingVariableIsUndefined,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It 'Verifies terminating error streaming' {

        $result = 1..1 | ForEach-Object -Parallel { throw 'Terminating Error!'; "Hello" } 2>&1
        $result.Count | Should -BeExactly 1
        $result.ToString() | Should -BeExactly 'Terminating Error!'
        $result.FullyQualifiedErrorId | Should -BeExactly 'PSTaskException'
    }

    It 'Verifies terminating error in multiple iterations' {

        $results = 1..2 | ForEach-Object -Parallel {
            if ($_ -eq 1) {
                throw 'Terminating Error!'
                "Hello!"
            }
            else {
                "Goodbye!"
            }
        } 2>&1

        $resultStrings = $results | ForEach-Object { $_.ToString() }
        $resultStrings | Should -Not -Contain "Hello!"
        $resultStrings | Should -Contain "Goodbye!"
        $resultStrings | Should -Contain "Terminating Error!"
    }

    It 'Verifies non-terminating error streaming' {

        $actualError = 1..1 | ForEach-Object -Parallel { Write-Error "Error!" } 2>&1
        $actualError.ToString() | Should -BeExactly 'Error!'
        $actualError.FullyQualifiedErrorId | Should -BeExactly 'Microsoft.PowerShell.Commands.WriteErrorException'
    }

    It 'Verifies warning data streaming' {

        $actualWarning = 1..1 | ForEach-Object -Parallel { Write-Warning "Warning!" } 3>&1
        $actualWarning.Message | Should -BeExactly 'Warning!'
    }

    It 'Verifies verbose data streaming' {

        $actualVerbose = 1..1 | ForEach-Object -Parallel { Write-Verbose "Verbose!" -Verbose } -Verbose 4>&1
        $actualVerbose.Message | Should -BeExactly 'Verbose!'
    }

    It 'Verifies debug data streaming' {

        $actualDebug = 1..1 | ForEach-Object -Parallel { Write-Debug "Debug!" -Debug } -Debug 5>&1
        $actualDebug.Message | Should -BeExactly 'Debug!'
    }

    It 'Verifies progress data streaming' {

        $ps = [Powershell]::Create([System.Management.Automation.RunspaceMode]::NewRunspace)

        $ps.Commands.AddScript("`$ProgressPreference='Continue'; 1..2 | ForEach-Object -Parallel { Write-Progress -Activity Progress -Status Running }") | Out-Null
        $ps.Invoke()
        $pr = $ps.Streams.Progress.ReadAll()
        $pr.Count | Should -Be 2
        $pr[0].Activity | Should -Be Progress
        $pr[0].StatusDescription | Should -Be Running
        $ps.Dispose()
    }

    It 'Verifies information data streaming' {

        $actualInformation = 1..1 | ForEach-Object -Parallel { Write-Information "Information!" } 6>&1
        $actualInformation.MessageData | Should -BeExactly 'Information!'
    }

    It 'Verifies error for using script block variable' {

        { 1..1 | ForEach-Object -Parallel { $using:sb } } | Should -Throw -ErrorId 'ParallelUsingVariableCannotBeScriptBlock,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It 'Verifies error for using script block variable in PSObject' {

        { 1..1 | ForEach-Object -Parallel { $using:psosb } } | Should -Throw -ErrorId 'ParallelUsingVariableCannotBeScriptBlock,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It 'Verifies error for script block piped variable' {

        $actualError = $sb | ForEach-Object -Parallel { "Hello" } 2>&1
        $actualError.FullyQualifiedErrorId | Should -BeExactly 'ParallelPipedInputObjectCannotBeScriptBlock,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It 'Verifies that parallel script blocks run in FullLanguage mode by default' {

        $results = 1..1 | ForEach-Object -Parallel { $ExecutionContext.SessionState.LanguageMode }
        $results | Should -BeExactly 'FullLanguage'
    }

    It 'Verifies that the current working directory is preserved' {
        $parallelScriptLocation = 1..1 | ForEach-Object -Parallel { $PWD }
        $parallelScriptLocation.Path | Should -BeExactly $PWD.Path
    }

    It 'Verifies that the current working directory can have wildcards in its name' {
        $oldLocation = Get-Location

        $wildcardName = New-Item -Path 'TestDrive:\' -Name '[' -ItemType Directory
        Set-Location -LiteralPath $wildcardName.FullName
        try
        {
            { 1..1 | ForEach-Object -Parallel { $PWD } } | Should -Not -Throw

            $wildcardPathResult = 1..1 | ForEach-Object -Parallel { $PWD }
            $wildcardPathResult.Path | Should -BeExactly $PWD.Path
        }
        finally
        {
            Set-Location -Path $oldLocation
            if ($drive -is [System.IO.DirectoryInfo]) {
                $drive | Remove-Item -Force
            }
        }
    }

    It 'Verifies no terminating error if current working drive is not found' {
        $oldLocation = Get-Location
        try
        {
            New-PSDrive -Name ZZ -PSProvider FileSystem -Root $TestDrive
            Set-Location -Path 'ZZ:'
            { 1..1 | ForEach-Object -Parallel { $_ } } | Should -Not -Throw
        }
        finally
        {
            Set-Location -Path $oldLocation
        }
    }
}

Describe 'ForEach-Object -Parallel common parameters' -Tags 'CI' {

    BeforeAll {

        # Test cases
        $TestCasesNotSupportedCommonParameters = @(
            @{
                testName    = 'Verifies that ErrorAction common parameter is not supported'
                scriptBlock = { 1..1 | ForEach-Object -Parallel { "Hello" } -ErrorAction Stop }
            },
            @{
                testName    = 'Verifies that WarningAction common parameter is not supported'
                scriptBlock = { 1..1 | ForEach-Object -Parallel { "Hello" } -WarningAction SilentlyContinue }
            },
            @{
                testName    = 'Verifies that InformationAction common parameter is not supported'
                scriptBlock = { 1..1 | ForEach-Object -Parallel { "Hello" } -InformationAction SilentlyContinue }
            },
            @{
                testName    = 'Verifies that PipelineVariable common parameter is not supported'
                scriptBlock = { 1..1 | ForEach-Object -Parallel { "Hello" } -PipelineVariable pipeVar }
            }
        )

        $TestCasesForSupportedCommonParameters = @(
            @{
                testName       = 'Verifies ErrorVariable common parameter'
                scriptBlock    = { 1..1 | ForEach-Object -Parallel { Write-Error "Error:$_" } -ErrorVariable global:actualVariable }
                expectedResult = 'Error:1'
            },
            @{
                testName       = 'Verifies WarningVarible common parameter'
                scriptBlock    = { 1..1 | ForEach-Object -Parallel { Write-Warning "Warning:$_" } -WarningVariable global:actualVariable }
                expectedResult = 'Warning:1'
            },
            @{
                testName       = 'Verifies InformationVariable common parameter'
                scriptBlock    = { 1..1 | ForEach-Object -Parallel { Write-Information "Information:$_" } -InformationVariable global:actualVariable }
                expectedResult = 'Information:1'
            },
            @{
                testName       = 'Verifies OutVariable common parameter'
                scriptBlock    = { 1..1 | ForEach-Object -Parallel { Write-Output "Output:$_" } -OutVariable global:actualVariable }
                expectedResult = 'Output:1'
            }
        )
    }

    BeforeEach {
        $global:actualVariable = $null
    }

    AfterAll {
        $global:actualVariable = $null
    }

    It "<testName>" -TestCases $TestCasesNotSupportedCommonParameters {

        param ($scriptBlock)

        { & $scriptBlock } | Should -Throw -ErrorId 'ParallelCommonParametersNotSupported,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It "<testName>" -TestCases $TestCasesForSupportedCommonParameters {

        param ($scriptBlock, $expectedResult)

        & $scriptBlock *>$null
        $global:actualVariable[0].ToString() | Should -BeExactly $expectedResult
    }
}

Describe 'ForEach-Object -Parallel -AsJob Basic Tests' -Tags 'CI' {

    It 'Verifies TimeoutSeconds parameter is excluded from AsJob' {

        { 1..1 | ForEach-Object -AsJob -Parallel { "Hello" } -TimeoutSeconds 60 } | Should -Throw -ErrorId 'ParallelCannotUseTimeoutWithJob,Microsoft.PowerShell.Commands.ForEachObjectCommand'
    }

    It 'Verifies ForEach-Object -Parallel jobs appear in job repository' {

        $job = 1..1 | ForEach-Object -AsJob -Parallel { "Hello" }
        Get-Job | Should -Contain $job
        $job | Wait-Job | Remove-Job
    }

    It 'Verifies dollar underbar variable' {

        $expected = 1..10
        $job = $expected | ForEach-Object -AsJob -Parallel { $_ }
        $result = $job | Wait-Job | Receive-Job
        $job | Remove-Job
        $result.Count | Should -BeExactly $expected.Count
        $result | Should -Contain 1
        $result | Should -Contain 10
    }

    It 'Verifies using variables' {

        $Var1 = "Hello"
        $Var2 = "Goodbye"
        $Var3 = 105
        $Var4 = "One","Two","Three"
        $job = 1..1 | ForEach-Object -AsJob -Parallel {
            Write-Output $using:Var1
            Write-Output $using:Var2
            Write-Output $using:Var3
            Write-Output @(,$using:Var4)
            Write-Output $using:Var4[1]
        }
        $results = $job | Wait-Job | Receive-Job
        $job | Remove-Job

        $results[0] | Should -BeExactly $Var1
        $results[1] | Should -BeExactly $Var2
        $results[2] | Should -BeExactly $Var3
        $results[3] | Should -BeExactly $Var4
        $results[4] | Should -BeExactly $Var4[1]
    }

    It 'Verifies terminating error in single iteration' {

        $job = 1..1 | ForEach-Object -AsJob -Parallel { throw "Terminating Error!"; "Hello" }
        $results = $job | Wait-Job | Receive-Job 2>$null
        $results.Count | Should -BeExactly 0
        $job.State | Should -BeExactly 'Failed'
        $job.ChildJobs[0].JobStateInfo.State | Should -BeExactly 'Failed'
        $job.ChildJobs[0].JobStateInfo.Reason.Message | Should -BeExactly 'Terminating Error!'
        $job | Remove-Job
    }

    It 'Verifies terminating error in double iteration' {

        $job = 1..2 | ForEach-Object -AsJob -Parallel {
            if ($_ -eq 1) {
                throw "Terminating Error!"
                "Goodbye!"
            }
            else {
                "Hello!"
            }
        }
        $results = $job | Wait-Job | Receive-Job 2>$null
        $results | Should -Contain 'Hello!'
        $results | Should -Not -Contain 'Goodbye!'
        $job.JobStateInfo.State | Should -BeExactly 'Failed'
        $job.ChildJobs[0].JobStateInfo.State | Should -BeExactly 'Failed'
        $job.ChildJobs[0].JobStateInfo.Reason.Message | Should -BeExactly 'Terminating Error!'
        $job.ChildJobs[1].JobStateInfo.State | Should -BeExactly 'Completed'
        $job | Remove-Job
    }

    It 'Verifies non-terminating error' {

        $job = 1..1 | ForEach-Object -AsJob -Parallel { Write-Error "Error:$_" }
        $results = $job | Wait-Job | Receive-Job 2>&1
        $job | Remove-Job
        $results.ToString() | Should -BeExactly "Error:1"
    }

    It 'Verifies warning data' {

        $job = 1..1 | ForEach-Object -AsJob -Parallel { Write-Warning "Warning:$_" }
        $results = $job | Wait-Job | Receive-Job 3>&1
        $job | Remove-Job
        $results.Message | Should -BeExactly "Warning:1"
    }

    It 'Verifies verbose data' {

        $job = 1..1 | ForEach-Object -AsJob -Parallel { Write-Verbose "Verbose:$_" -Verbose }
        $results = $job | Wait-Job | Receive-Job -Verbose 4>&1
        $job | Remove-Job
        $results.Message | Should -BeExactly "Verbose:1"
    }

    It 'Verifies debug data' {

        $job = 1..1 | ForEach-Object -AsJob -Parallel { Write-Debug "Debug:$_" -Debug }
        $results = $job | Wait-Job | Receive-Job -Debug 5>&1
        $job | Remove-Job
        $results.Message | Should -BeExactly "Debug:1"
    }

    It 'Verifies information data' {

        $job = 1..1 | ForEach-Object -AsJob -Parallel { Write-Information "Information:$_" }
        $results = $job | Wait-Job | Receive-Job 6>&1
        $job | Remove-Job
        $results.MessageData | Should -BeExactly "Information:1"
    }

    It 'Verifies job Command property' {

        $job = 1..1 | ForEach-Object -AsJob -Parallel {"Hello"}
        $job.Command | Should -BeExactly '"Hello"'
        $job.ChildJobs[0].Command | Should -BeExactly '"Hello"'
        $job | Wait-Job | Remove-Job
    }

    It 'Verifies that the current working directory is preserved' {
        $job = 1..1 | ForEach-Object -AsJob -Parallel { $PWD }
        $parallelScriptLocation = $job | Wait-Job | Receive-Job
        $job | Remove-Job
        $parallelScriptLocation.Path | Should -BeExactly $PWD.Path
    }
}

Describe 'ForEach-Object -Parallel runspace pool tests' -Tags 'CI' {

    It "Verifies job allocated runspace count is limited to pool size" {

        $job = 1..4 | ForEach-Object -Parallel { Start-Sleep 1 } -AsJob -ThrottleLimit 2 | Wait-Job
        $job.AllocatedRunspaceCount | Should -BeExactly 2
        $job | Remove-Job
    }

    It "Verifies job with -UseNewRunspace switch allocates one runspace per iteration" {

        $job = 1..10 | ForEach-Object -Parallel { $_ } -AsJob -ThrottleLimit 2 -UseNewRunspace | Wait-Job
        $job.AllocatedRunspaceCount | Should -BeExactly 10
        $job | Remove-Job
    }
}

Describe 'ForEach-Object -Parallel Functional Tests' -Tags 'Feature' {

    It 'Verifies job queuing and throttle limit' {

        # Run four job tasks, two in parallel at a time.
        $job = 1..4 | ForEach-Object -Parallel { Start-Sleep 60 } -AsJob -ThrottleLimit 2

        # Wait for child job 2 to begin running for up to ten seconds
        if ( !(Wait-UntilTrue -TimeoutInMilliseconds 10000 -IntervalInMilliseconds 250 `
               { $job.ChildJobs[1].JobStateInfo.State -eq 'Running' }))
        {
            throw "ForEach-Object -Parallel child job 2 did not start"
        }

        # Two job tasks should be running and two waiting to run
        $job.ChildJobs[0].JobStateInfo.State | Should -BeExactly 'Running'
        $job.ChildJobs[1].JobStateInfo.State | Should -BeExactly 'Running'
        $job.ChildJobs[2].JobStateInfo.State | Should -BeExactly 'NotStarted'
        $job.ChildJobs[3].JobStateInfo.State | Should -BeExactly 'NotStarted'

        $job | Remove-Job -Force
    }

    It 'Verifies jobs work with Receive-Job -AutoRemove parameter' {

        $job = 1..4 | ForEach-Object -AsJob -Parallel { "Hello:$_" }
        $null = $job | Receive-Job -Wait -AutoRemoveJob
        Get-Job | Should -Not -Contain $job
    }

    It 'Verifies parallel task queuing' {

        $results = 10..1 | ForEach-Object -Parallel { Start-Sleep 1; $_ } -ThrottleLimit 5
        $results[0] | Should -BeGreaterThan 5
        $results[1] | Should -BeGreaterThan 5
        $results[2] | Should -BeGreaterThan 5
        $results[3] | Should -BeGreaterThan 5
        $results[4] | Should -BeGreaterThan 5
    }

    It 'Verifies timeout and throttle parameters' {

        # With ThrottleLimit set to 1, the two 60 second long script blocks will run sequentially,
        # until the timeout in 5 seconds.
        $results = 1..2 | ForEach-Object -Parallel { "Output $_"; Start-Sleep -Seconds 60 } -TimeoutSeconds 5 -ThrottleLimit 1 2>&1
        $results.Count | Should -BeExactly 2
        $results[0] | Should -BeExactly 'Output 1'
        $results[1].FullyQualifiedErrorId | Should -BeExactly 'PSTaskException'
        $results[1].Exception | Should -BeOfType System.Management.Automation.PipelineStoppedException
    }
}
