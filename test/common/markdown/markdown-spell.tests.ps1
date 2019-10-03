# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Verify Markdown Spelling" {
    BeforeAll {
        if (!(Get-Command -Name 'mdspell' -ErrorAction SilentlyContinue)) {
            Start-NativeExecution {
                sudo yarn global add markdown-spellcheck@0.11.0
            }
        }

        Get-Job | Remove-Job -Force
    }

    AfterAll {
        Get-Job | Remove-Job -Force
    }

    $groups = Get-ChildItem -Path "$PSScriptRoot\..\..\..\*.md" -Recurse | 
        Where-Object DirectoryName -notlike '*node_modules*' | 
        Group-Object -Property directory

    $jobs = @{}
    # Start all spelling verification in parallel
    Foreach($group in $groups)
    {
        $job = Start-ThreadJob {
            param([object[]] $group)
            foreach($file in $group)
            {
                $results = mdspell --en-us --ignore-numbers --ignore-acronyms --report $file 2>&1
                Write-Output ([PSCustomObject]@{
                    file = $file
                    results = $results
                })
            }
        } -ArgumentList @($group.Group)
        $jobs.add($group.name,$job)
    }

    # Get the results and verify
    foreach ($key in $jobs.GetEnumerator().Name)
    {
        $job = $jobs.$key
        $results = Receive-Job -Job $job -Wait
        Remove-job -Job $Job
        foreach ($jobResult in $results)
        {
            $file = $jobResult.file
            $result = $jobResult.results
            Context "Verify spelling in $file" {
                $failures = @($result) -like '*spelling errors found in*'
                $passes = @($result) -like '*free of spelling*'
 
                $trueFailures = foreach ($Failure in $Failures) {
                    @{ Spell = $Failure }
                }

                if ($trueFailures) {
                    It "<spell>!" -TestCases $trueFailures {
                        param($spell)
                        throw "Tool reported spelling as wrong.`n$spell"
                    }
                }
            }
        }
    }
}
