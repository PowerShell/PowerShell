# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "ConvertFrom-SddlString Tests" -Tags "CI", "RequireAdminOnWindows" {

    BeforeAll {
        if (-not $IsWindows) { return }
        $sddl = (Get-Item -Path WSMan:\localhost\Service\RootSDDL).Value
        $testCases = @(
            @{ Type = "_UNSPECIFIED_" }
            @{ Type = "FileSystemRights" }
            @{ Type = "RegistryRights" }
            @{ Type = "ActiveDirectoryRights" }
            @{ Type = "MutexRights" }
            @{ Type = "SemaphoreRights" }
            @{ Type = "EventWaitHandleRights" }
        )
        $expectedProperties = @('Owner', 'Group', 'DiscretionaryAcl', 'SystemAcl', 'RawDescriptor')
    }

    It "Validate ConvertFrom-SddlString with type <Type>" -Skip:(!$IsWindows) -TestCases $testCases {
        param($Type)

        $arguments = @{ Sddl = $sddl; }
        if ($Type -ne "_UNSPECIFIED_") {
            $arguments.Add("Type", $Type)
        }

        $result = ConvertFrom-SddlString @arguments
        foreach ($property in $expectedProperties)
        {
            $result.$property | Should -Not -Be $null
        }
    }

    It "Validate that ConvertFrom-SddlString with type <Type> via ValueFromPipeline" -Skip:(!$IsWindows) -TestCases $testCases {
        param($Type)

        $arguments = @{ }
        if ($Type -ne "_UNSPECIFIED_") {
            $arguments.Add("Type", $Type)
        }

        $result = $sddl | ConvertFrom-SddlString @arguments
        foreach ($property in $expectedProperties)
        {
            $result.$property | Should -Not -Be $null
        }
    }
}
