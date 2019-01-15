# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Verify Markdown Links" {
    BeforeAll {
        # Cleanup jobs for reliability
        Get-Job | stop-job
        get-job | remove-job
    }

    AfterAll {
        # Cleanup jobs to leave the process the same
        Write-Verbose -verbose "cleaning jobs ..."
        Get-Job | stop-job
        get-job | remove-job
    }

    $groups = Get-ChildItem -Path "$PSScriptRoot\..\..\..\*.md" -Recurse | Group-Object -Property directory

    $jobs = @{}
    # start all link verification in parallel
    Write-Verbose -verbose "starting jobs for performance ..."
    Foreach($group in $groups)
    {
        $job = start-job {
            param([object] $group)
            foreach($file in $group.Group)
            {
                $results = markdown-link-check $file 2>&1
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
            Context "Verify links in $file" {
                $failures = $result -like '*[✖]*' | ForEach-Object { $_.Substring(4) }
                $passes = $result -like '*[✓]*' | ForEach-Object {
                    @{url=$_.Substring(4)}
                }
                $trueFailures = @()
                $verifyFailures = @()
                foreach ($failure in $failures) {
                    if($failure -like 'https://www.amazon.com*')
                    {
                        # In testing amazon links often failed when they are valid
                        # Verify manually
                        $verifyFailures += @{url = $failure}
                    }
                    else
                    {
                        $trueFailures += @{url = $failure}
                    }
                }

                # must have some code in the test for it to pass
                function noop {
                }

                if($passes)
                {
                    it "<url> should work" -TestCases $passes {
                        noop
                    }
                }

                if($trueFailures)
                {
                    it "<url> should work" -TestCases $trueFailures  {
                        throw "Url unreachable"
                    }
                }

                if($verifyFailures)
                {
                    it "<url> should work" -TestCases $verifyFailures -Pending  {
                    }
                }
            }
        }
    }
}
