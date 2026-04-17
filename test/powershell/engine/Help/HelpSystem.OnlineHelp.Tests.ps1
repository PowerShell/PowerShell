# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe 'Online help tests for PowerShell Cmdlets' -Tags "Feature" {

    # The csv files (V2Cmdlets.csv and V3Cmdlets.csv) contain a list of cmdlets and expected HelpURIs.
    # The HelpURI is part of the cmdlet metadata, and when the user runs 'get-help <cmdletName> -online'
    # the browser navigates to the address in the HelpURI. However, if a help file is present, the HelpURI
    # on the file take precedence over the one in the cmdlet metadata.

    BeforeDiscovery {
        # Only load CSV data at discovery time. Do NOT call Get-Command here
        # because it triggers module auto-loading for ~340 cmdlets and causes
        # discovery to exceed the 30-second timeout.
        $testCases = @()
        foreach ($filePath in @("$PSScriptRoot\assets\HelpURI\V2Cmdlets.csv", "$PSScriptRoot\assets\HelpURI\V3Cmdlets.csv"))
        {
            $cmdletList = Import-Csv $filePath -ErrorAction Stop
            foreach ($cmdlet in $cmdletList)
            {
                $testCases += @{
                    TopicTitle = $cmdlet.TopicTitle
                    HelpURI    = $cmdlet.HelpURI
                }
            }
        }
    }

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

    # TopicTitle - is the cmdlet name in the csv file
    # HelpURI - is the expected help URI in the csv file
    # If this is correct, then launching the cmdlet should open the expected help URI.
    It "Validate 'get-help <TopicTitle> -Online'" -ForEach $testCases {
        $command = Get-Command $TopicTitle -ErrorAction SilentlyContinue
        if ($null -eq $command) {
            Set-ItResult -Skipped -Because "Command '$TopicTitle' not found"
            return
        }
        if ($command.Module.PrivateData.ImplicitRemoting) {
            Remove-Module $command.Module
            Set-ItResult -Skipped -Because "Command '$TopicTitle' uses implicit remoting"
            return
        }
        $actualURI = Get-Help $TopicTitle -Online
        $actualURI = $actualURI.Replace("Help URI: ","")
        $actualURI | Should -Be $HelpURI
    }
}

Describe 'Get-Help -Online is not supported on Nano Server and IoT' -Tags "CI" {

    BeforeAll {
        $skipTest = -not ([System.Management.Automation.Platform]::IsIoT -or [System.Management.Automation.Platform]::IsNanoServer)
    }

    It "Get-help -online <cmdletName> throws InvalidOperation." -Skip:$skipTest {
        { Get-Help Get-Help -Online } | Should -Throw -ErrorId "InvalidOperation,Microsoft.PowerShell.Commands.GetHelpCommand"
    }
}

Describe 'Get-Help should throw on network paths' -Tags "CI" {
    BeforeAll {
        $script:skipTest = -not $IsWindows
    }

    It "Get-Help should throw not on <command>" -Skip:$skipTest -TestCases (Get-HelpNetworkTestCases -PositiveCases) {
        param(
            $Command,
            $ExpectedError
        )

        { Get-Help -Name $Command  } | Should -Not -Throw
    }
}
