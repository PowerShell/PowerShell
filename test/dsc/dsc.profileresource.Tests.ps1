# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "DSC PowerShell Profile Resource Tests" -Tag "CI" {
    BeforeAll {
        # Ensure DSC v3 is available
        if (-not (Get-Command -name dsc -CommandType Application -ErrorAction SilentlyContinue)) {
            throw "DSC v3 is not installed"
        }

        $dscExe = Get-Command -name dsc -CommandType Application

        $testProfileContent = "# Test profile content"
        $testProfilePath = Get-Item $PROFILE
        Copy-Item -Path $testProfilePath -Destination "$TestDrive/profile.bak" -Force

        Set-Content -Path $testProfilePath -Value $testProfileContent -Force

        $originalPath = $env:PATH
        $env:PATH += ";$PSHome"
    }
    AfterAll {
        # Restore original profile
        $testProfilePath = Get-Item $PROFILE
        Copy-Item -Path "$TestDrive/profile.bak" -Destination $testProfilePath -Force

        $env:PATH = $originalPath
        Remove-Item -Path "$TestDrive/profile.bak" -Force
    }

    It 'DSC resource is located at $PSHome' {
        $resourceFile = Join-Path -Path $PSHome -ChildPath 'pwsh.profile.resource.ps1'
        $resourceFile | Should -Exist

        $resourceManifest = Join-Path -Path $PSHome -ChildPath 'pwsh.profile.dsc.resource.json'
        $resourceManifest | Should -Exist
    }

    It 'DSC resource can be found' {
        (& $dscExe resource list -o json | ConvertFrom-Json  | Select-Object -Property type).type | Should -Contain 'Microsoft.PowerShell/Profile'
    }

    It 'DSC resource can set current user current host profile' {
        $setOutput = (& $dscExe config set --file .\psprofile_set.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }
}
