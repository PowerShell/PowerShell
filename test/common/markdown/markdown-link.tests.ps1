# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Verify Markdown Links" {
    BeforeAll {
        # WARNING: Keep markdown-link-check pinned at 3.7.2 OR ELSE...
        if(!(Get-Command -Name 'markdown-link-check' -ErrorAction SilentlyContinue))
        {
            Write-Verbose "installing markdown-link-check ..." -Verbose
            start-nativeExecution {
                    sudo npm install -g markdown-link-check@3.7.2
                    # Sometimes this folder is left behind with root permissions and is needed by later NPM installs which don't need sudo
                    sudo rm -rf ~/.npm/_cacache
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
        Write-Verbose -verbose "starting jobs for $($group.Name) ..."
        $job = Start-ThreadJob {
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

    Write-Verbose -verbose "Getting and printing results ..."
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
                # failures look like `[✖] https://someurl` (perhaps without the https://)
                # passes look like `[✓] https://someurl` (perhaps without the https://)
                $failures = $result -like '*[✖]*' | ForEach-Object { $_.Substring(4).Trim() }
                $passes = $result -like '*[✓]*' | ForEach-Object {
                    @{url=$_.Substring(4).Trim() }
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
                        param($url)

                        $prefix = $url.Substring(0,7)

                        # Logging for diagnosability.  Azure DevOps sometimes redacts the full url.
                        Write-Verbose "prefix: '$prefix'" -Verbose
                        if($url -match '^http(s)?:')
                        {
                            # If invoke-WebRequest can handle the URL, re-verify, with 5 retries
                            try{
                                $null = Invoke-WebRequest -uri $url -RetryIntervalSec 3 -MaximumRetryCount 6
                            }
                            catch
                            {
                                throw "retry of URL failed with error: $($_.Message)"
                            }
                        }
                        else {
                            throw "Tool reported URL as unreachable."
                        }
                    }
                }

                if($verifyFailures)
                {
                    it "<url> should work" -TestCases $verifyFailures -Pending  {
                    }
                }

                if(!$passes -and !$trueFailures -and !$verifyFailures)
                {
                    It "has no links" {
                        noop
                    }
                }
            }
        }
    }
}
