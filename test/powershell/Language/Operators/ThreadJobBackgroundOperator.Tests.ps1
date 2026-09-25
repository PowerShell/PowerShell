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

    Context "Runtime ThreadJob Tests" {
        It "Creates a background job with &! operator" {
            $job = Write-Output "Hello from ThreadJob" &!
            try {
                $job | Should -Not -BeNullOrEmpty
                $job | Should -BeOfType [System.Management.Automation.Job]

                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Receives output from ThreadJob background job" {
            $job = Write-Output "Test Output" &!
            try {
                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }

                $result = $completedJob | Receive-Job
                $result | Should -Be "Test Output"
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Runs simple expression as ThreadJob" {
            $job = 1 + 1 &!
            try {
                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }

                $result = $completedJob | Receive-Job
                $result | Should -Be 2
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Validates ThreadJob is created when Start-ThreadJob is available" -Skip:(-not $threadJobAvailable) {
            $job = Write-Output "ThreadJob Test" &!
            try {
                $job | Should -Not -BeNullOrEmpty
                $job.PSJobTypeName | Should -Be 'ThreadJob'

                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Falls back to regular job when Start-ThreadJob is unavailable" -Skip:$threadJobAvailable {
            # This test runs only when ThreadJob is not available
            $job = Write-Output "Fallback Test" &!
            try {
                $job | Should -Not -BeNullOrEmpty
                $job | Should -BeOfType [System.Management.Automation.Job]
                # Should not be a ThreadJob
                $job.PSJobTypeName | Should -Not -Be 'ThreadJob'

                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It 'Captures variables automatically without explicit $using:' {
            $testVar = "CapturedValue"
            $job = Write-Output $testVar &!
            try {
                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }

                $result = $completedJob | Receive-Job
                $result | Should -Be "CapturedValue"
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It 'Captures variables with explicit $using: in scriptblock' {
            $testVar = "CapturedValueWithUsing"
            $job = { $using:testVar } &!
            try {
                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }

                $result = $completedJob | Receive-Job
                $result | Should -Be "CapturedValueWithUsing"
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Runs pipeline as ThreadJob" {
            $job = 1,2,3 | ForEach-Object { $_ * 2 } &!
            try {
                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }

                $result = $completedJob | Receive-Job
                $result | Should -Be @(2, 4, 6)
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Works with variable assignment" {
            $job = 1 + 2 &!
            try {
                $job | Should -Not -BeNullOrEmpty

                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }

                $result = $completedJob | Receive-Job
                $result | Should -Be 3
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Can be combined with && operator" {
            $job = testexe -returncode 0 && Write-Output "success" &!
            try {
                $job | Should -Not -BeNullOrEmpty
                $job | Should -BeOfType [System.Management.Automation.Job]

                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }

                $result = $completedJob | Receive-Job
                $result | Should -Contain "0"
                $result | Should -Contain "success"
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Works with command execution" {
            $job = Get-Process -Id $PID | Select-Object -ExpandProperty Name &!
            try {
                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }

                $result = $completedJob | Receive-Job
                $result | Should -Not -BeNullOrEmpty
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Handles errors in ThreadJob" {
            $job = { throw "Test Error" } &!
            try {
                $completedJob = $job | Wait-Job -Timeout 30
                if (-not $completedJob) {
                    throw "Job did not complete within the allotted timeout (30 seconds)."
                }

                $completedJob.State | Should -Be 'Failed'
            }
            finally {
                if ($null -ne $job) {
                    $job | Remove-Job -Force -ErrorAction Ignore
                }
            }
        }

        It "Creates multiple ThreadJobs" {
            $job1 = Write-Output "Job1" &!
            $job2 = Write-Output "Job2" &!
            $job3 = Write-Output "Job3" &!
            try {
                $completedJobs = $job1, $job2, $job3 | Wait-Job -Timeout 30
                if ($completedJobs.Count -ne 3) {
                    throw "Not all jobs completed within the allotted timeout (30 seconds)."
                }

                $results = $completedJobs | Receive-Job
                $results | Should -Contain "Job1"
                $results | Should -Contain "Job2"
                $results | Should -Contain "Job3"
            }
            finally {
                $job1, $job2, $job3 | Where-Object { $null -ne $_ } | Remove-Job -Force -ErrorAction Ignore
            }
        }
    }

    Context "Syntax Validation Tests" {
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
