# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

BeforeDiscovery {

    $testCases = @(
        @{
            remoteInfo = 'upstream	git@ssh.dev.azure.com:v3/azDoOrg/PowerShellCore/PowerShell (fetch)'
            upstreamRemote = 'upstream'
            org = 'azDoOrg'
            project = 'PowerShellCore'
            UpstreamHost = 'git@ssh.dev.azure.com'
            RemoteType = 'AzureRepo'
        }
        @{
            remoteInfo = 'upstream	https://azDoOrg.visualstudio.com/PowerShell/_git/PowerShell (fetch)'
            upstreamRemote = 'upstream'
            org = 'azDoOrg'
            project = 'PowerShell'
            UpstreamHost = 'azDoOrg.visualstudio.com'
            RemoteType = 'AzureRepo'
        }
        @{
            remoteInfo = 'upstream	https://github.com/PowerShell/PowerShell.git (fetch)'
            upstreamRemote = 'upstream'
            org = 'PowerShell'
            project = 'github.com'
            UpstreamHost = 'github.com'
            RemoteType = 'GitHub'
        }
        @{
            remoteInfo = 'github	https://github.com/PowerShell/PowerShell.git (fetch)'
            upstreamRemote = 'github'
            org = 'PowerShell'
            project = 'github.com'
            UpstreamHost = 'github.com'
            RemoteType = 'GitHub'
        }
        @{
            remoteInfo = 'asonetuhasoeu	git@github.com:PowerShell/PowerShell.git (fetch)'
            upstreamRemote = 'asonetuhasoeu'
            org = 'PowerShell'
            project = 'github.com'
            UpstreamHost = 'github.com'
            RemoteType = 'GitHub'
        }

    )
}
Describe "Get-UpstreamInfo" {
    BeforeAll {
        Import-Module $PSScriptRoot/../../tools/releaseTools.psm1 -force -Verbose
    }
    It "parses remote Info correctly: <RemoteInfo>" -TestCases $testCases -Test {
        param(
            [string]
            $RemoteInfo,
            [string]
            $UpstreamRemote,
            [string]
            $org,
            [string]
            $Project,
            [string]
            $UpstreamHost,
            [string]
            $remoteType
        )

        $upstreamInfo = Get-UpstreamInfo -Upstream $RemoteInfo -UpstreamRemote $UpstreamRemote
        $upstreamInfo | Should -Not -BeNullOrEmpty
        $upstreamInfo.org | Should -Be $org
        $upstreamInfo.project | Should -Be $Project
        $upstreamInfo.repo | Should -Be 'PowerShell'
        $upstreamInfo.host | Should -Be $UpstreamHost
        $upstreamInfo.remoteType | Should -Be $remoteType
    }
}
