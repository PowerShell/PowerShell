#
#  Copyright (c) Microsoft Corporation.
#  Licensed under the Apache License, Version 2.0 (the "License");
#  you may not use this file except in compliance with the License.
#  You may obtain a copy of the License at
#  https://www.apache.org/licenses/LICENSE-2.0
#
#  Unless required by applicable law or agreed to in writing, software
#  distributed under the License is distributed on an "AS IS" BASIS,
#  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
#  See the License for the specific language governing permissions and
#  limitations under the License.
#
# ------------------ PackageManagement Test  -----------------------------------

Describe "PackageManagement Acceptance Test" -Tags "Feature" {

    BeforeAll {
        # the package name for testing
        $packageName = "PowerShell.TestPackage"

        # register the asset directory
        $localSourceName = [Guid]::NewGuid().ToString("n")
        $localSourceLocation = Join-Path $PSScriptRoot assets
        $newSourceResult = Register-PackageSource -Name $localSourceName -provider NuGet -Location $localSourceLocation -Force -Trusted -Verbose
        Write-Verbose -Verbose -Message "Register-PackageSource -Name $localSourceName -provider NuGet -Location $localSourceLocation -Force -Trusted"
        $newSourceResult | Out-String -Stream | Write-Verbose -Verbose
        $localSourceCheck = Get-PackageSource -Name $localSourceName -ErrorAction Ignore

        $skipPackageTests = $false
        # It is possible that Register-PackageSource appears to succeed but the source is not actually registered
        # collect as much information as possible to help diagnose the problem
        if (($newSourceResult.Location -ne $localSourceLocation) -or ($localSourceCheck.Location -ne $localSourceLocation)) {
            Write-Verbose -Verbose "Skipping tests because local source could not be correctly created"
            if ( $newSourceResult ) {
                $newSourceResult | out-string -str | Write-Verbose -Verbose
            }
            else {
                Write-Verbose "newSourceResult is null"
            }

            if ( $localSourceCheck ) {
                $localSourceCheck | out-string -str | Write-Verbose -Verbose
            }
            else {
                Write-Verbose "LocalSourceCheck is null"
            }

            if (-not $IsWindows) {
                Get-ChildItem "$HOME/.config" -Recurse -File | out-string -str | Write-Verbose -Verbose
            }

            $skipPackageTests = $true
        }

        # register the gallery location
        $galleryLocation = "https://www.powershellgallery.com/api/v2"
        $gallerySourceName = [Guid]::newGuid().ToString("n")
        # this is expected to fail as there should already be a source pointing to the gallery url
        Register-PackageSource -Name $gallerySourceName -Location $galleryLocation -ProviderName 'PowerShellGet' -Trusted -ErrorAction SilentlyContinue

        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"

        Get-PackageSource | Out-String -Stream | Write-Verbose -Verbose
        }

    AfterAll {
        $ProgressPreference = $SavedProgressPreference
        try {
            # non-fatal errors
            Unregister-PackageSource -Source $localSourceName -ErrorAction Ignore
            Unregister-PackageSource -Name $gallerySourceName -ErrorAction Ignore
            Uninstall-Module NanoServerPackage -ErrorAction Ignore -WarningAction SilentlyContinue
        }
        catch {
            Write-Warning "Failure in AfterAll: $_"
        }
    }

    It "get-packageprovider" {
        $gpp = Get-PackageProvider
        $gpp.Name | Should -Contain 'NuGet'
        $gpp.Name | Should -Contain 'PowerShellGet'
    }

    It "find-packageprovider PowerShellGet" {
        $fpp = (Find-PackageProvider -Name "PowerShellGet" -Force).name
        $fpp | Should -Contain "PowerShellGet"
    }

    It "install-packageprovider, Expect succeed" {
        Set-ItResult -Pending -Because "local test package provider not installable"
		$ippArgs = @{
			Name = "NanoServerPackage"
			Force = $true
			Source = $galleryLocation
			Scope  = "CurrentUser"
			WarningAction = "SilentlyContinue"
		}
        $ipp = (Install-PackageProvider @ippArgs).name
        $ipp | Should -Contain "NanoServerPackage"
    }

    It "Find-package" -skip:$skipPackageTests {
        $f = Find-Package -ProviderName NuGet -Name $packageName -Source $localSourceName
        $f.Name | Should -Contain "$packageName" -Because "PackageSource $localSourceLocation not created"
	}

    It "Install-package" -skip:$skipPackageTests {
        $i = Install-Package -ProviderName NuGet -Name $packageName -Force -Source $localSourceName -Scope CurrentUser
        $i.Name | Should -Contain "$packageName" -Because "PackageSource $localSourceLocation not created"
	}

    It "Get-package" -skip:$skipPackageTests {
        $g = Get-Package -ProviderName NuGet -Name $packageName
        $g.Name | Should -Contain "$packageName"
	}

    It "save-package" -skip:$skipPackageTests {
        $s = Save-Package -ProviderName NuGet -Name $packageName -Path $TestDrive -Force -Source $localSourceName
        $s.Name | Should -Contain "$packageName" -Because (Get-ChildItem $TestDrive)
	}

    It "uninstall-package" -skip:$skipPackageTests {
        $u = Uninstall-Package -ProviderName NuGet -Name $packageName
        $u.Name | Should -Contain "$packageName"
	}
}
