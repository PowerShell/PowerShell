# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Assembly::LoadWithPartialName Validation Test" -Tags "CI" {

    $defaultErrorId = 'FileLoadException'
    $testcases = @(
        # verify winforms is blocked
        # winforms assembly is supported for .Net Core 3.0, if a new assembly needs to be blocked
        # enable this test and add to list below
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

    # This test is currently being skipped because System.Windows.Forms is part of .NET Core 3.0 so it gets
    # load and thus no exception is thrown failing this test.

    It "Assembly::LoadWithPartialName should fail to load blacklisted assembly: <Name>" -Pending -TestCases $testcases {
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
