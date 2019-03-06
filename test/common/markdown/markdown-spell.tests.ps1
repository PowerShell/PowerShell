# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Verify Markdown Spelling"
{
    BeforeAll
    {
        # Try to run `mdspell`, if it doesn't work, install it:
        if( !(Get-Command -Name 'mdspell' -ErrorAction SilentlyContinue) )
        {
            Write-Verbose "installing markdown-spelling tool please wait ...!" -Verbose
            start-nativeExecution
            {
                sudo npm install -g markdown-spellcheck@0.11.0
            }
        }

        # Cleanup jobs for reliability
        get-job | remove-job -force
    }

    AfterAll
    {
        # Cleanup jobs to leave the process the same
        get-job | remove-job -force
    }

    $groups = Get-ChildItem -Path "$PSScriptRoot\..\..\..\*.md" -Recurse | Where-Object {$_.DirectoryName -notlike '*node_modules*'} | Group-Object -Property directory

    $jobs = @{}
    # Start all spell checking in parallel to save time:
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
            Context "Verify spellling in $file"
            {
                $failures = $result -like '*spelling errors found*'
                $passes = $result -like '*free of spelling errors*'

                # must have some code in the test for it to pass
                function noop {}

                if($passes)
                {
                    it "<mdfile> should have no spelling issues" -TestCases $passes
                    {
                        noop
                    }
                }
                else
                {
                    it "<mdfile> should have no spelling issues"
                    {
                        param($mdfile)
                        Write-Verbose "File failing is $mdfile" -Verbose
                        throw "You have a spelling error! Did you recently modify any markdown files?"
                    }
                }
            }
        }
    }
}
