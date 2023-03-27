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
        Register-PackageSource -Name $localSourceName -provider NuGet -Location $localSourceLocation -Force -Trusted

        # register the gallery location
        $galleryLocation = "https://www.powershellgallery.com/api/v2"
        $gallerySourceName = [Guid]::newGuid().ToString("n")
        Register-PackageSource -Name $gallerySourceName -Location $galleryLocation -ProviderName 'PowerShellGet' -Trusted -ErrorAction SilentlyContinue

        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
        }

    AfterAll {
        $ProgressPreference = $SavedProgressPreference
        Unregister-PackageSource -Source $localSourceName -ErrorAction Ignore
        Unregister-PackageSource -Name $gallerySourceName -ErrorAction Ignore
        Uninstall-Module NanoServerPackage -ErrorAction Ignore -WarningAction SilentlyContinue
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
        Set-ItResult -Skipped -Because "local test package provider not installable"
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

    It "Find-package"  {
        $f = Find-Package -ProviderName NuGet -Name $packageName -Source $localSourceName
        $f.Name | Should -Contain "$packageName"
	}

    It "Install-package"  {
        $i = Install-Package -ProviderName NuGet -Name $packageName -Force -Source $localSourceName -Scope CurrentUser
        $i.Name | Should -Contain "$packageName"
	}

    It "Get-package"  {
        $g = Get-Package -ProviderName NuGet -Name $packageName
        $g.Name | Should -Contain "$packageName"
	}

    It "save-package"  {
        $s = Save-Package -ProviderName NuGet -Name $packageName -Path $TestDrive -Force -Source $localSourceName
        $s.Name | Should -Contain "$packageName"
	}

    It "uninstall-package"  {
        $u = Uninstall-Package -ProviderName NuGet -Name $packageName
        $u.Name | Should -Contain "$packageName"
	}
}
