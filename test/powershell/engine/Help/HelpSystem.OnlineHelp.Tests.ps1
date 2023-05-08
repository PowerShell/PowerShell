# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Online help tests for PowerShell Cmdlets' -Tags "Feature" {

    # The csv files (V2Cmdlets.csv and V3Cmdlets.csv) contain a list of cmdlets and expected HelpURIs.
    # The HelpURI is part of the cmdlet metadata, and when the user runs 'get-help <cmdletName> -online'
    # the browser navigates to the address in the HelpURI. However, if a help file is present, the HelpURI
    # on the file take precedence over the one in the cmdlet metadata.

    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"

        # Enable the test hook. This does the following:
        # 1) get-help will not find a help file; instead, it will generate a metadata driven object.
        # 2) get-help -online <cmdletName>  will return the helpuri instead of opening the default web browser.
        [system.management.automation.internal.internaltesthooks]::SetTestHook('BypassOnlineHelpRetrieval', $true)
    }

    AfterAll {

        # Disable the test hook
        [system.management.automation.internal.internaltesthooks]::SetTestHook('BypassOnlineHelpRetrieval', $false)
        $ProgressPreference = $SavedProgressPreference
    }

    foreach ($filePath in @("$PSScriptRoot\assets\HelpURI\V2Cmdlets.csv", "$PSScriptRoot\assets\HelpURI\V3Cmdlets.csv"))
    {
        $cmdletList = Import-Csv $filePath -ErrorAction Stop

        foreach ($cmdlet in $cmdletList)
        {
            # If the cmdlet is not preset in CoreCLR, skip it.
            $command = Get-Command $cmdlet.TopicTitle -ErrorAction SilentlyContinue
            $skipTest = $null -eq ($command)
            if ((-not $skipTest) -and $command.Module.PrivateData.ImplicitRemoting)
            {
                $skipTest = $true
                Remove-Module $command.Module
            }

            # TopicTitle - is the cmdlet name in the csv file
            # HelpURI - is the expected help URI in the csv file
            # If this is correct, then launching the cmdlet should open the expected help URI.

            It "Validate 'get-help $($cmdlet.TopicTitle) -Online'" -Skip:$skipTest {
                $actualURI = Get-Help $cmdlet.TopicTitle -Online
                $actualURI = $actualURI.Replace("Help URI: ","")
                $actualURI | Should -Be $cmdlet.HelpURI
            }
        }
    }
}

Describe 'Get-Help -Online is not supported on Nano Server and IoT' -Tags "CI" {

    $skipTest = -not ([System.Management.Automation.Platform]::IsIoT -or [System.Management.Automation.Platform]::IsNanoServer)

    It "Get-help -online <cmdletName> throws InvalidOperation." -Skip:$skipTest {
        { Get-Help Get-Help -Online } | Should -Throw -ErrorId "InvalidOperation,Microsoft.PowerShell.Commands.GetHelpCommand"
    }
}
