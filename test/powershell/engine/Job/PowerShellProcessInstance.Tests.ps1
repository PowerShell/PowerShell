# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PowerShellProcessInstance path resolution' -Tags 'Feature' {
    Context 'Start-Job functionality' {
        It 'Can start and complete a simple job' {
            $job = Start-Job -ScriptBlock { 'test output' }
            $result = $job | Wait-Job | Receive-Job
            $result | Should -BeExactly 'test output'
            Remove-Job $job -Force
        }

        It 'Can start nested jobs' {
            $job = Start-Job -ScriptBlock { 
                Start-Job -ScriptBlock { 'nested test' } | Wait-Job | Receive-Job 
            }
            $result = $job | Wait-Job | Receive-Job
            $result | Should -BeExactly 'nested test'
            Remove-Job $job -Force
        }

        It 'Can retrieve environment information from job' {
            $job = Start-Job -ScriptBlock { 
                [System.Environment]::ProcessPath
            }
            $result = $job | Wait-Job | Receive-Job
            $result | Should -Not -BeNullOrEmpty
            Remove-Job $job -Force
        }
    }
}
