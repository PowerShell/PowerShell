Describe 'Online help tests for PowerShell Core Cmdlets' -Tags "CI" {

    # The csv files (V2Cmdlets.csv and V3Cmdlets.csv) contain a list of cmdlets and expected HelpURIs.
    # The HelpURI is part of the cmdlet metadata, and when the user runs 'get-help <cmdletName> -online'
    # the browser navigates to the address in the HelpURI. However, if a help file is present, the HelpURI
    # on the file take precedence over the one in the cmdlet metadata. Therefore, the help content
    # in the box needs to be deleted before running the tests, because otherwise, the HelpURI
    # (when calling get-help -online) might not matched the one in the csv file.

    # Currently update-help does not work on Linux (see  https://github.com/PowerShell/PowerShell/issues/951),
    # so if the help content is deleted, we do not have a way to install it back, and any subsequent
    # 'get-help' tests which depend on the help content will fail. Until this update-help issue is fixed,
    # we need to skip these tests on Linux and OSX.

    $skipTest = ([System.Management.Automation.Platform]::IsLinux -or [System.Management.Automation.Platform]::IsOSX)

    BeforeAll {

        # There is a bug in Linux in which a 'Boolean value defined inside Describe block cannot be evaluated properly 
        # in if statement inside BeforeAll block on Linux' Becuase of this, I need to define the bool value again inside the block.
        # See https://github.com/PowerShell/PowerShell/issues/2445
        #
        $skipTest = ([System.Management.Automation.Platform]::IsLinux -or [System.Management.Automation.Platform]::IsOSX)

        if (-not $skipTest)
        {
            # Enable the test hook
            [system.management.automation.internal.internaltesthooks]::SetTestHook('BypassOnlineHelpRetrieval', $true)

            # Remove the help content
            Write-Verbose "Deleting help content for get-help -online tests" -Verbose
            $path = Join-Path $PSHOME (Get-UICulture).Name
            Get-ChildItem "$path\*dll-help.xml" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
        }
    }

    AfterAll {
        if (-not $skipTest)
        {
            # Disable the test hook
            [system.management.automation.internal.internaltesthooks]::SetTestHook('BypassOnlineHelpRetrieval', $false)
        }
    }

    foreach ($filePath in @("$PSScriptRoot\testdata\HelpURI\V2Cmdlets.csv", "$PSScriptRoot\testdata\HelpURI\V3Cmdlets.csv"))
    {
        $cmdletList = Import-Csv $filePath -ea Stop

        foreach ($cmdlet in $cmdletList)
        {
            # If the cmdlet is not preset in CoreCLR, skip it.
            if (Get-Command $cmdlet.TopicTitle -ea SilentlyContinue)
            {
                # TopicTitle - is the cmdlet name in the csv file
                # HelpURI - is the expected help URI in the csv file

                It "Validate 'get-help $($cmdlet.TopicTitle) -Online'" -skip:$skipTest {
                    $actualURI = Get-Help $cmdlet.TopicTitle -Online
                    $actualURI = $actualURI.Replace("Help URI: ","")
                    $actualURI | Should Be $cmdlet.HelpURI
                }
            }
        }
    }
}

Describe 'Get-Help -Online opens the default web browser and navigates to the cmdlet help content' -Tags "Feature" {

    $skipTest = [System.Management.Automation.Platform]::IsIoT -or
                [System.Management.Automation.Platform]::IsNanoServer

    It "Get-Help get-process -online" -skip:$skipTest {

        { Get-Help get-process -online } | Should Not Throw
    }
}

Describe 'Get-Help -Online is not supported on Nano Server and IoT' -Tags "CI" {

    $skipTest = -not ([System.Management.Automation.Platform]::IsIoT -or [System.Management.Automation.Platform]::IsNanoServer)

    It "Get-help -online <cmdletName> throws InvalidOperation." -skip:$skipTest {

        try
        {
            Get-Help Get-Help -Online
            throw "Execution should not have succeeded"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "InvalidOperation,Microsoft.PowerShell.Commands.GetHelpCommand"
        }
    }
}
