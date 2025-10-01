# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "DSC PowerShell Profile Resource Tests" -Tag "CI" {
    BeforeAll {
        # Ensure DSC v3 is available
        if (-not (Get-Command -name dsc -CommandType Application -ErrorAction SilentlyContinue)) {
            throw "DSC v3 is not installed"
        }

        $dscExe = Get-Command -name dsc -CommandType Application | Select-Object -First 1

        $testProfileContent = "# Test profile content currentuser currenthost"
        $testProfilePathCurrentUserCurrentHost = $PROFILE.CurrentUserCurrentHost
        Copy-Item -Path $testProfilePathCurrentUserCurrentHost -Destination "$TestDrive/currentuser-currenthost-profile.bak" -Force -ErrorAction SilentlyContinue
        Set-Content -Path $testProfilePathCurrentUserCurrentHost -Value $testProfileContent -Force

        $testProfileContent = "# Test profile content currentuser allhosts"
        $testProfilePathCurrentUserAllHosts = $PROFILE.CurrentUserAllHosts
        Copy-Item -Path $testProfilePathCurrentUserAllHosts -Destination "$TestDrive/currentuser-allhosts-profile.bak" -Force -ErrorAction SilentlyContinue
        Set-Content -Path $testProfilePathCurrentUserAllHosts -Value $testProfileContent -Force

        $testProfileContent = "# Test profile content allusers currenthost"
        $testProfilePathAllUsersCurrentHost = $PROFILE.AllUsersCurrentHost
        Copy-Item -Path $testProfilePathAllUsersCurrentHost -Destination "$TestDrive/allusers-currenthost-profile.bak" -Force -ErrorAction SilentlyContinue
        Set-Content -Path $testProfilePathAllUsersCurrentHost -Value $testProfileContent -Force

        $testProfileContent = "# Test profile content allusers allhosts"
        $testProfilePathAllUsersAllHosts = $PROFILE.AllUsersAllHosts
        Copy-Item -Path $testProfilePathAllUsersAllHosts -Destination "$TestDrive/allusers-allhosts-profile.bak" -Force -ErrorAction SilentlyContinue
        Set-Content -Path $testProfilePathAllUsersAllHosts -Value $testProfileContent -Force

        $originalPath = $env:PATH
        $env:PATH += ";$PSHome"
    }
    AfterAll {
        # Restore original profile
        $testProfilePathCurrentUserCurrentHost = (Get-Item $PROFILE.CurrentUserCurrentHost).FullName
        Copy-Item -Path "$TestDrive/currentuser-currenthost-profile.bak" -Destination $testProfilePathCurrentUserCurrentHost -Force

        $testProfilePathCurrentUserAllHosts = (Get-Item $PROFILE.CurrentUserAllHosts).FullName
        Copy-Item -Path "$TestDrive/currentuser-allhosts-profile.bak" -Destination $testProfilePathCurrentUserAllHosts -Force

        $testProfilePathAllUsersCurrentHost = (Get-Item $PROFILE.AllUsersCurrentHost).FullName
        Copy-Item -Path "$TestDrive/allusers-currenthost-profile.bak" -Destination $testProfilePathAllUsersCurrentHost -Force

        $testProfilePathAllUsersAllHosts = (Get-Item $PROFILE.AllUsersAllHosts).FullName
        Copy-Item -Path "$TestDrive/allusers-allhosts-profile.bak" -Destination $testProfilePathAllUsersAllHosts -Force

        $env:PATH = $originalPath
        Remove-Item -Path "$TestDrive/currentuser-currenthost-profile.bak" -Force
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
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - CurrentUserCurrentHost!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can set current user current host profile' {
        $setOutput = (& $dscExe config get --file .\psprofile_set.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - CurrentUserAllHosts!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can set all users current host profile' {
        $setOutput = (& $dscExe config set --file .\psprofile_alluserscurrenthost.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - AllUsersCurrentHost!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }
}

Describe "DSC PowerShell Profile resource elevated tests" -Tag "CI", 'RequireAdminOnWindows', 'RequireSudoOnUnix' {
    BeforeAll {
        # Ensure DSC v3 is available
        if (-not (Get-Command -name dsc -CommandType Application -ErrorAction SilentlyContinue)) {
            throw "DSC v3 is not installed"
        }

        $dscExe = Get-Command -name dsc -CommandType Application | Select-Object -First 1

        $originalPath = $env:PATH
        $env:PATH += ";$PSHome"
    }
    AfterAll {
        $env:PATH = $originalPath
    }

    It 'DSC resource can set all users all hosts profile' {
        $setOutput = (& $dscExe config set --file .\psprofile_alluser_allhost.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - AllUsersAllHosts!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can set all users current hosts profile' {
        $setOutput = (& $dscExe config set --file .\psprofile_alluserscurrenthost.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - AllUsersCurrentHost!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }
}
