#
#  Copyright (c) Microsoft Corporation. All rights reserved.
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

$gallery = "https://www.powershellgallery.com/api/v2"
$source = 'OneGetTestSource'

Describe "PackageManagement Acceptance Test" -Tags "Feature" {

 BeforeAll{
    Register-PackageSource -Name Nugettest -provider NuGet -Location https://www.nuget.org/api/v2 -Force

    $packageSource = Get-PackageSource -Location $gallery -ErrorAction SilentlyContinue
    if ($packageSource) {
        $source = $packageSource.Name
        Set-PackageSource -Name $source -Trusted
    } else {
        Register-PackageSource -Name $source -Location $gallery -ProviderName 'PowerShellGet' -Trusted -ErrorAction SilentlyContinue
    }

    $SavedProgressPreference = $ProgressPreference
    $ProgressPreference = "SilentlyContinue"
 }
 AfterAll {
     $ProgressPreference = $SavedProgressPreference
 }
    It "get-packageprovider" {

        $gpp = Get-PackageProvider

        $gpp.Name | Should -Contain 'NuGet'

        $gpp.Name | Should -Contain 'PowerShellGet'
    }

    It "find-packageprovider PowerShellGet" {
        $fpp = (Find-PackageProvider -Name "PowerShellGet" -force).name
        $fpp | Should -Contain "PowerShellGet"
    }

    It "install-packageprovider, Expect succeed" {
        $ipp = (Install-PackageProvider -Name NanoServerPackage -Force -Source $source -Scope CurrentUser).name
        $ipp | Should -Contain "NanoServerPackage"
    }

    It "Find-package"  {
        $f = Find-Package -ProviderName NuGet -Name jquery -source Nugettest
        $f.Name | Should -Contain "jquery"
	}

    it "Install-package"  {
        $i = install-Package -ProviderName NuGet -Name jquery -force -source Nugettest -Scope CurrentUser
        $i.Name | Should -Contain "jquery"
	}

    it "Get-package"  {
        $g = Get-Package -ProviderName NuGet -Name jquery
        $g.Name | Should -Contain "jquery"
	}

    it "save-package"  {
        $s = save-Package -ProviderName NuGet -Name jquery -path $TestDrive -force -source Nugettest
        $s.Name | Should -Contain "jquery"
	}

    it "uninstall-package"  {
        $u = uninstall-Package -ProviderName NuGet -Name jquery
        $u.Name | Should -Contain "jquery"
	}
}
