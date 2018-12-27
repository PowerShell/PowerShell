# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Assembly::LoadWithPartialName Validation Test" -Tags "CI" {

    $defaultErrorId = 'FileLoadException'
    $testcases = @(
        # verify winforms is blocked
        @{
            Name = 'system.windows.forms'
            ErrorId = $defaultErrorId
        }
        # Verify alternative casing is blocked
        @{
            Name = 'System.Windows.Forms'
            ErrorId = $defaultErrorId
        }
    )

    # All existing cases should fail on all platforms either because it doesn't exist or
    # because the assembly is blacklisted
    It "Assembly::LoadWithPartialName should fail to load blacklisted assembly: <Name>" -TestCases $testcases {
        param(
            [Parameter(Mandatory)]
            [string]
            $Name,
            [Parameter(Mandatory)]
            [string]
            $ErrorId
        )

        {[System.Reflection.Assembly]::LoadWithPartialName($Name)} | Should -Throw -ErrorId $ErrorId
    }
}
