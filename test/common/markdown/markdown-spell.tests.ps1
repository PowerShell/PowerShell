# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Verify Markdown Spelling" {
    BeforeAll {
        if(!(Get-Command -Name 'mdspell' -ErrorAction SilentlyContinue))
        {
            Write-Host "installing markdown-spelling tool please wait ...!" -Verbose
            start-nativeExecution {
                sudo npm install -g markdown-spellcheck@0.11.0
            }
        }

        if(!(Get-Module -Name 'ThreadJob' -ListAvailable -ErrorAction SilentlyContinue))
        {
            Install-Module -Name ThreadJob -Scope CurrentUser
        }

        # Cleanup jobs for reliability
        get-job | remove-job -force
    }

    AfterAll {
        # Cleanup jobs to leave the process the same
        get-job | remove-job -force
    }

    $groups = Get-ChildItem -Path "$PSScriptRoot\..\..\..\*.md" -Recurse | Where-Object {$_.DirectoryName -notlike '*node_modules*'} | Group-Object -Property directory

    $jobs = @{}
    # start all link verification in parallel
    Foreach($group in $groups)
    {
        $job = Start-ThreadJob {
            param([object] $group)
            foreach($file in $group.Group)
            {
                $results = mdspell $file 2>&1 --ignore-numbers --ignore-acronyms --report --en-us;
                Write-Output ([PSCustomObject]@{
                    file = $file
                    results = $results
                })
            }
        } -ArgumentList @($group)
        $jobs.add($group.name,$job)
    }

    # Get the results and verify
    foreach($key in $jobs.keys)
    {
        $job = $jobs.$key
        $results = Receive-Job -Job $job -Wait
        Remove-job -job $Job
        foreach($jobResult in $results)
        {
            $file = $jobResult.file
            $result = $jobResult.results
            Context "Verify spellling in $file" {
                $failures = $result -like '*spelling errors found in*'
                $passes = $result -like '*are free of spelling errors*'
                $trueFailures = @()
                $verifyFailures = @()

                # must have some code in the test for it to pass
                function noop {}

                if($passes)
                {
                    it "<url> should work" -TestCases $passes {
                        noop
                    }
                }

                if($trueFailures)
                {
                    it "<url> should work" -TestCases $trueFailures  {
                        param($url)
                        throw "You have a spelling error! Did you recently modify any markdown files?"
                    }
                }

                if($verifyFailures)
                {
                    it "<url> should work" -TestCases $verifyFailures -Pending {}
                }
            }
        }
    }
}
