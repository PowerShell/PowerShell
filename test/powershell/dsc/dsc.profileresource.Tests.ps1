# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "DSC PowerShell Profile Resource Tests" -Tag "CI" {
    BeforeAll {
        $DSC_ROOT = $env:DSC_ROOT

        if (-not (Test-Path -Path $DSC_ROOT)) {
            throw "DSC_ROOT environment variable is not set or path does not exist."
        }

        Write-Verbose "DSC_ROOT is set to $DSC_ROOT" -Verbose

        $originalPath = $env:PATH

        $pathSeparator = [System.IO.Path]::PathSeparator
        $env:PATH += "$pathSeparator$DSC_ROOT"
        $env:PATH += "$pathSeparator$PSHome"

        Write-Verbose "Updated PATH to include DSC_ROOT: $env:PATH" -Verbose

        # Ensure DSC v3 is available
        if (-not (Get-Command -name dsc -CommandType Application -ErrorAction SilentlyContinue)) {
            Get-ChildItem $DSC_ROOT -Recurse 'dsc' | ForEach-Object {
                Write-Verbose "Found DSC executable at $($_.FullName)" -Verbose
            }
            throw "DSC v3 is not installed"
        }

        $dscExe = Get-Command -name dsc -CommandType Application | Select-Object -First 1

        $testProfileContent = "# Test profile content currentuser currenthost"
        $testProfilePathCurrentUserCurrentHost = $PROFILE.CurrentUserCurrentHost
        Copy-Item -Path $testProfilePathCurrentUserCurrentHost -Destination "$TestDrive/currentuser-currenthost-profile.bak" -Force -ErrorAction SilentlyContinue
        New-Item -Path $testProfilePathCurrentUserCurrentHost -Value $testProfileContent -Force -ItemType File

        $testProfileContent = "# Test profile content currentuser allhosts"
        $testProfilePathCurrentUserAllHosts = $PROFILE.CurrentUserAllHosts
        Copy-Item -Path $testProfilePathCurrentUserAllHosts -Destination "$TestDrive/currentuser-allhosts-profile.bak" -Force -ErrorAction SilentlyContinue
        New-Item -Path $testProfilePathCurrentUserAllHosts -Value $testProfileContent -Force -ItemType File
    }
    AfterAll {
        # Restore original profile
        $testProfilePathCurrentUserCurrentHost = $PROFILE.CurrentUserCurrentHost
        Copy-Item -Path "$TestDrive/currentuser-currenthost-profile.bak" -Destination $testProfilePathCurrentUserCurrentHost -Force -ErrorAction SilentlyContinue

        $testProfilePathCurrentUserAllHosts = $PROFILE.CurrentUserAllHosts
        Copy-Item -Path "$TestDrive/currentuser-allhosts-profile.bak" -Destination $testProfilePathCurrentUserAllHosts -Force -ErrorAction SilentlyContinue

        $env:PATH = $originalPath
        Remove-Item -Path "$TestDrive/currentuser-currenthost-profile.bak" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "$TestDrive/currentuser-allhosts-profile.bak" -Force -ErrorAction SilentlyContinue
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
        $setOutput = (& $dscExe config set --file $PSScriptRoot/psprofile_currentuser_currenthost.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - CurrentUserCurrentHost!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can get current user current host profile' {
        $getOutput = (& $dscExe config get --file $PSScriptRoot/psprofile_currentuser_currenthost.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - CurrentUserCurrentHost!'"
        $getOutput.results.result.actualState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can set current user all hosts profile' {
        $setOutput = (& $dscExe config set --file $PSScriptRoot/psprofile_currentuser_allhosts.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - CurrentUserAllHosts!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can get current user all hosts profile' {
        $getOutput = (& $dscExe config get --file $PSScriptRoot/psprofile_currentuser_allhosts.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - CurrentUserAllHosts!'"
        $getOutput.results.result.actualState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can export all profiles' {
        $exportOutput = (& $dscExe config export --file $PSScriptRoot/psprofile_export.dsc.yaml -o json) | ConvertFrom-Json

        $exportOutput.resources | Should -HaveCount 4

        $exportOutput.resources | ForEach-Object {
            $_.type | Should -Be 'Microsoft.PowerShell/Profile'
            $_.name | Should -BeIn @('AllUsersCurrentHost', 'AllUsersAllHosts', 'CurrentUserCurrentHost', 'CurrentUserAllHosts')
        }
    }
}

Describe "DSC PowerShell Profile resource elevated tests" -Tag "CI", 'RequireAdminOnWindows', 'RequireSudoOnUnix' {
    BeforeAll {
        $DSC_ROOT = $env:DSC_ROOT

        if (-not (Test-Path -Path $DSC_ROOT)) {
            throw "DSC_ROOT environment variable is not set or path does not exist."
        }

        Write-Verbose "DSC_ROOT is set to $DSC_ROOT" -Verbose
        $pathSeparator = [System.IO.Path]::PathSeparator

        $env:PATH += "$pathSeparator$DSC_ROOT"

        $env:PATH += "$pathSeparator$PSHome"

        Write-Verbose "Updated PATH to include DSC_ROOT: $env:PATH" -Verbose

        # Ensure DSC v3 is available
        if (-not (Get-Command -name dsc -CommandType Application -ErrorAction SilentlyContinue)) {
            Get-ChildItem $DSC_ROOT -Recurse 'dsc' | ForEach-Object {
                Write-Verbose "Found DSC executable at $($_.FullName)" -Verbose
            }
            throw "DSC v3 is not installed"
        }

        $dscExe = Get-Command -name dsc -CommandType Application | Select-Object -First 1

        $testProfileContent = "# Test profile content allusers currenthost"
        $testProfilePathAllUsersCurrentHost = $PROFILE.AllUsersCurrentHost
        Copy-Item -Path $testProfilePathAllUsersCurrentHost -Destination "$TestDrive/allusers-currenthost-profile.bak" -Force -ErrorAction SilentlyContinue
        New-Item -Path $testProfilePathAllUsersCurrentHost -Value $testProfileContent -Force -ItemType File

        $testProfileContent = "# Test profile content allusers allhosts"
        $testProfilePathAllUsersAllHosts = $PROFILE.AllUsersAllHosts
        Copy-Item -Path $testProfilePathAllUsersAllHosts -Destination "$TestDrive/allusers-allhosts-profile.bak" -Force -ErrorAction SilentlyContinue
        New-Item -Path $testProfilePathAllUsersAllHosts -Value $testProfileContent -Force -ItemType File

        $originalPath = $env:PATH
        $env:PATH += ";$PSHome"
    }
    AfterAll {
        $env:PATH = $originalPath

        $testProfilePathAllUsersCurrentHost = $PROFILE.AllUsersCurrentHost
        Copy-Item -Path "$TestDrive/allusers-currenthost-profile.bak" -Destination $testProfilePathAllUsersCurrentHost -Force -ErrorAction SilentlyContinue

        $testProfilePathAllUsersAllHosts = $PROFILE.AllUsersAllHosts
        Copy-Item -Path "$TestDrive/allusers-allhosts-profile.bak" -Destination $testProfilePathAllUsersAllHosts -Force -ErrorAction SilentlyContinue

        Remove-Item -Path "$TestDrive/currentuser-allhosts-profile.bak" -Force -ErrorAction SilentlyContinue
        Remove-Item -Path "$TestDrive/allusers-allhosts-profile.bak" -Force -ErrorAction SilentlyContinue
    }

    It 'DSC resource can set all users all hosts profile' {
        $setOutput = (& $dscExe config set --file $PSScriptRoot/psprofile_alluser_allhost.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - AllUsersAllHosts!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can get all users all hosts profile' {
        $getOutput = (& $dscExe config get --file $PSScriptRoot/psprofile_alluser_allhost.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - AllUsersAllHosts!'"
        $getOutput.results.result.actualState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can set all users current hosts profile' {
        $setOutput = (& $dscExe config set --file $PSScriptRoot/psprofile_allusers_currenthost.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - AllUsersCurrentHost!'"
        $setOutput.results.result.afterState.content | Should -BeExactly $expectedContent
    }

    It 'DSC resource can get all users current hosts profile' {
        $getOutput = (& $dscExe config get --file $PSScriptRoot/psprofile_allusers_currenthost.dsc.yaml -o json) | ConvertFrom-Json
        $expectedContent = "Write-Host 'Welcome to your PowerShell profile - AllUsersCurrentHost!'"
        $getOutput.results.result.actualState.content | Should -BeExactly $expectedContent
    }
}
