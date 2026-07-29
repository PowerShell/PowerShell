# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'setReleaseTag.ps1' {
    BeforeAll {
        $script:setReleaseTagPath = Join-Path $PSScriptRoot '../../tools/releaseBuild/setReleaseTag.ps1'
    }

    It 'rejects malformed rebuild branch <Branch>' -TestCases @(
        @{ Branch = 'refs/heads/rebuild/v7.0.1' }
        @{ Branch = 'refs/heads/rebuild/v7.0.1-rebuild' }
        @{ Branch = 'refs/heads/rebuild/v7.0.1-rebuild.one' }
        @{ Branch = 'refs/heads/rebuild/v7.0.1-rebuild.01' }
        @{ Branch = 'refs/heads/rebuild/not-a-version-rebuild.1' }
    ) {
        param($Branch)

        $expectedMessage = "Malformed rebuild branch '$Branch'. Expected branch name format: 'rebuild/v<major>.<minor>.<patch>-rebuild.<number>'."
        { & $script:setReleaseTagPath -ReleaseTag fromBranch -Branch $Branch } | Should -Throw -ExpectedMessage $expectedMessage
    }

    It 'rejects a malformed rebuild branch when ReleaseTag is omitted' {
        $branch = 'refs/heads/rebuild/v7.0.1'
        $expectedMessage = "Malformed rebuild branch '$branch'. Expected branch name format: 'rebuild/v<major>.<minor>.<patch>-rebuild.<number>'."

        { & $script:setReleaseTagPath -Branch $branch } | Should -Throw -ExpectedMessage $expectedMessage
    }

    It 'derives the release tag from valid rebuild branch <Branch>' -TestCases @(
        @{ Branch = 'refs/heads/rebuild/v7.0.1-rebuild.1'; ExpectedTag = 'v7.0.1-rebuild.1' }
        @{ Branch = 'refs/heads/rebuild/v7.0.1-rebuild.12'; ExpectedTag = 'v7.0.1-rebuild.12' }
    ) {
        param($Branch, $ExpectedTag)

        & $script:setReleaseTagPath -ReleaseTag fromBranch -Branch $Branch |
            Should -BeExactly $ExpectedTag
    }

    It 'uses an explicit release tag without validating the branch' {
        & $script:setReleaseTagPath -ReleaseTag 'v7.0.1-rebuild.1' -Branch 'refs/heads/rebuild/v7.0.1' |
            Should -BeExactly 'v7.0.1-rebuild.1'
    }
}
