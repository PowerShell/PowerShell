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

    if (-not $IsWindows) {
        try {
            New-Item -Path ~/.local/share/PackageManagement/NuGet/Packages -ItemType Directory -Force -ErrorAction Stop
        }
        catch {
                .{
                    Get-Item ~/.local -ErrorAction Ignore
                    Get-Item ~/.local/share -ErrorAction Ignore
                    Get-Item ~/.local/share/PackageManagement -ErrorAction Ignore
                    Get-Item ~/.local/share/PackageManagement/NuGet -ErrorAction Ignore
                    Get-Item ~/.local/share/PackageManagement/NuGet/Packages -ErrorAction Ignore
                } | Out-String | Write-Verbose -Verbose
        }
        finally {
            Write-Verbose -Verbose "Create Path: $(Get-Item ~/.local/share/PackageManagement/NuGet/Packages -ErrorAction Ignore)"
        }
    }
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
        $fpp = (Find-PackageProvider -Name "PowerShellGet" -Force).name
        $fpp | Should -Contain "PowerShellGet"
    }

    It "install-packageprovider, Expect succeed" {
        $ipp = (Install-PackageProvider -Name NanoServerPackage -Force -Source $source -Scope CurrentUser).name
        $ipp | Should -Contain "NanoServerPackage"
    }

    It "Find-package"  {
        $f = Find-Package -ProviderName NuGet -Name jquery -Source Nugettest
        $f.Name | Should -Contain "jquery"
	}

    It "Install-package"  {
        if ($env:__INCONTAINER) {
            Write-Verbose -Verbose "Id: $(id)" # retrieve the id of the user
        }

        if (-not $IsWindows) {
            /bin/ls -ld '~/.local/share/PackageManagement/NuGet/Packages' 2>&1 | Out-String | Write-Verbose -Verbose
        }

        $i = Install-Package -ProviderName NuGet -Name jquery -Force -Source Nugettest -Scope CurrentUser
        $i.Name | Should -Contain "jquery"
	}

    It "Get-package"  {
        $g = Get-Package -ProviderName NuGet -Name jquery
        $g.Name | Should -Contain "jquery"
	}

    It "save-package"  {
        $s = Save-Package -ProviderName NuGet -Name jquery -Path $TestDrive -Force -Source Nugettest
        $s.Name | Should -Contain "jquery"
	}

    It "uninstall-package"  {
        $u = Uninstall-Package -ProviderName NuGet -Name jquery
        $u.Name | Should -Contain "jquery"
	}
}
