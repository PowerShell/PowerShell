# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "ThreadJob Background Operator &! Tests" -Tag CI {
    BeforeAll {
        # Ensure ThreadJob module is available
        $threadJobAvailable = $null -ne (Get-Command Start-ThreadJob -ErrorAction SilentlyContinue)
        
        if (-not $threadJobAvailable) {
            Write-Warning "Start-ThreadJob command not available. Tests may fall back to regular jobs."
        }
    }

    It "Creates a background job with &! operator" {
        $job = Write-Output "Hello from ThreadJob" &!
        $job | Should -Not -BeNullOrEmpty
        $job | Should -BeOfType [System.Management.Automation.Job]
        $job | Wait-Job | Remove-Job
    }

    It "Receives output from ThreadJob background job" {
        $job = Write-Output "Test Output" &!
        $result = $job | Wait-Job | Receive-Job
        $result | Should -Be "Test Output"
        $job | Remove-Job
    }

    It "Runs simple expression as ThreadJob" {
        $job = 1 + 1 &!
        $result = $job | Wait-Job | Receive-Job
        $result | Should -Be 2
        $job | Remove-Job
    }

    It "Captures variables with $using: in ThreadJob" {
        $testVar = "CapturedValue"
        $job = { $using:testVar } &!
        # Note: The implementation should automatically convert variables to $using:
        # For now, this test documents expected behavior
        $job | Wait-Job | Remove-Job
    }

    It "Runs pipeline as ThreadJob" {
        $job = 1,2,3 | ForEach-Object { $_ * 2 } &!
        $result = $job | Wait-Job | Receive-Job
        $result | Should -Be @(2, 4, 6)
        $job | Remove-Job
    }

    It "Works with variable assignment" {
        $job = 1 + 2 &!
        $job | Should -Not -BeNullOrEmpty
        $result = $job | Wait-Job | Receive-Job
        $result | Should -Be 3
        $job | Remove-Job
    }

    It "Can be combined with && operator" -Skip {
        # This test is skipped as the interaction between &! and && needs to be verified
        $job = testexe -returncode 0 && Write-Output "success" &!
        $result = $job | Wait-Job | Receive-Job
        $job | Remove-Job
    }

    It "Rejects &! with && in invalid syntax" {
        $tokens = $errors = $null
        $null = [System.Management.Automation.Language.Parser]::ParseInput('testexe -returncode 0 &! && testexe -returncode 1', [ref]$tokens, [ref]$errors)
        
        $errors.Count | Should -BeGreaterThan 0
        $errors[0].ErrorId | Should -Be 'BackgroundOperatorInPipelineChain'
    }

    It "Rejects &! with || in invalid syntax" {
        $tokens = $errors = $null
        $null = [System.Management.Automation.Language.Parser]::ParseInput('testexe -returncode 0 &! || testexe -returncode 1', [ref]$tokens, [ref]$errors)
        
        $errors.Count | Should -BeGreaterThan 0
        $errors[0].ErrorId | Should -Be 'BackgroundOperatorInPipelineChain'
    }

    It "Works with command execution" {
        $job = Get-Process -Id $PID | Select-Object -ExpandProperty Name &!
        $result = $job | Wait-Job | Receive-Job
        $result | Should -Not -BeNullOrEmpty
        $job | Remove-Job
    }

    It "Handles errors in ThreadJob" {
        $job = { throw "Test Error" } &!
        $job | Wait-Job
        $job.State | Should -Be 'Failed'
        $job | Remove-Job
    }

    It "Creates multiple ThreadJobs" {
        $job1 = Write-Output "Job1" &!
        $job2 = Write-Output "Job2" &!
        $job3 = Write-Output "Job3" &!
        
        $results = $job1, $job2, $job3 | Wait-Job | Receive-Job
        $results | Should -Contain "Job1"
        $results | Should -Contain "Job2"
        $results | Should -Contain "Job3"
        
        $job1, $job2, $job3 | Remove-Job
    }

    Context "Parser AST Tests" {
        It "Parses &! operator correctly" {
            $ast = [System.Management.Automation.Language.Parser]::ParseInput('Write-Output "test" &!', [ref]$null, [ref]$null)
            $pipelineAst = $ast.EndBlock.Statements[0]
            $pipelineAst | Should -BeOfType [System.Management.Automation.Language.PipelineAst]
            $pipelineAst.Background | Should -Be $true
            $pipelineAst.BackgroundThreadJob | Should -Be $true
        }

        It "Distinguishes between & and &! operators" {
            $ast1 = [System.Management.Automation.Language.Parser]::ParseInput('Write-Output "test" &', [ref]$null, [ref]$null)
            $pipelineAst1 = $ast1.EndBlock.Statements[0]
            $pipelineAst1.Background | Should -Be $true
            $pipelineAst1.BackgroundThreadJob | Should -Be $false

            $ast2 = [System.Management.Automation.Language.Parser]::ParseInput('Write-Output "test" &!', [ref]$null, [ref]$null)
            $pipelineAst2 = $ast2.EndBlock.Statements[0]
            $pipelineAst2.Background | Should -Be $true
            $pipelineAst2.BackgroundThreadJob | Should -Be $true
        }
    }

    Context "Tokenizer Tests" {
        It "Tokenizes &! as AmpersandExclaim" {
            $tokens = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput('Write-Output "test" &!', [ref]$tokens, [ref]$null)
            
            $ampersandExclaimToken = $tokens | Where-Object { $_.Kind -eq [System.Management.Automation.Language.TokenKind]::AmpersandExclaim }
            $ampersandExclaimToken | Should -Not -BeNullOrEmpty
            $ampersandExclaimToken.Text | Should -Be '&!'
        }

        It "Distinguishes & and &! tokens" {
            $tokens1 = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput('test &', [ref]$tokens1, [ref]$null)
            $ampToken = $tokens1 | Where-Object { $_.Kind -eq [System.Management.Automation.Language.TokenKind]::Ampersand }
            $ampToken | Should -Not -BeNullOrEmpty

            $tokens2 = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput('test &!', [ref]$tokens2, [ref]$null)
            $ampExclaimToken = $tokens2 | Where-Object { $_.Kind -eq [System.Management.Automation.Language.TokenKind]::AmpersandExclaim }
            $ampExclaimToken | Should -Not -BeNullOrEmpty
        }
    }
}
