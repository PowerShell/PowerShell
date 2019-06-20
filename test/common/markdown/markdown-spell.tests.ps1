# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Verify Markdown Spelling" {
    BeforeAll {
        if(!(Get-Command -Name 'mdspell' -ErrorAction SilentlyContinue)) {
            Start-NativeExecution {
                sudo yarn global add markdown-spellcheck@0.11.0
            }
        }

        # Cleanup jobs for reliability
        get-job | remove-job -force
    }

    AfterAll {
        # Cleanup jobs to leave the process the same
        get-job | remove-job -force
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
    foreach($key in $jobs.GetEnumerator().Name)
    {
        $job = $jobs.$key
        $results = Receive-Job -Job $job -Wait
        Remove-job -job $Job
        foreach($jobResult in $results)
        {
            $file = $jobResult.file
            $result = $jobResult.results
            Context "Verify spelling in $file" {
                $failures = @($result) -like '*spelling errors found in*'
                $passes = @($result) -like '*free of spelling*'
 
                $trueFailures = foreach ($Failure in $Failures) {
                    @{ Spell = $Failure }
                }

                if($trueFailures) {
                    it "<spell>!" -TestCases $trueFailures {
                        param($spell)
                        Write-Warning $spell
                        throw "Tool reported spelling as wrong."
                    }
                }
            }
        }
    }
}
