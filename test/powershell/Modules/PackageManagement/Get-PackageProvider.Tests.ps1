#
#  Copyright (c) Microsoft Corporation. All rights reserved.
#  Licensed under the Apache License, Version 2.0 (the "License");
#  you may not use this file except in compliance with the License.
#  You may obtain a copy of the License at
#  http://www.apache.org/licenses/LICENSE-2.0
#
#  Unless required by applicable law or agreed to in writing, software
#  distributed under the License is distributed on an "AS IS" BASIS,
#  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
#  See the License for the specific language governing permissions and
#  limitations under the License.
#
# ------------------ PackageManagement Test  ----------------------------------------------
ipmo "$PSScriptRoot\utility.psm1"


# ------------------------------------------------------------------------------
# Actual Tests:

Describe "get-packageprovider" -Tags @('BVT', 'DRT'){
    # make sure that packagemanagement is loaded
    import-packagemanagement

    It "lists package providers installed" {
        $x = (get-packageprovider -name "nuget" -ForceBootstrap).name | should match "nuget"
    }

    It "lists package providers installed" {
        $x = (get-packageprovider -name "nuget" -verbose -Force).name | should match "nuget"
    }

    It "EXPECTED:  Gets The 'Programs' Package Provider" {
        $x = (get-packageprovider -name "Programs").name | should match "Programs"
    }
    
    It "EXPECTED:  Gets The 'P*' Package Provider" {
        $x = (get-packageprovider -name "P*").name.Contains('PowerShellGet')| should be $true
    }
   
    It "EXPECTED:  returns an error when asking for a provider that does not exist" {
        $Error.Clear()
        $msg = powershell 'get-packageprovider -name NOT_EXISTS  -warningaction:silentlycontinue -ea silentlycontinue; $ERROR[0].FullyQualifiedErrorId'
        $msg | should be "UnknownProviderFromActivatedList,Microsoft.PowerShell.PackageManagement.Cmdlets.GetPackageProvider"
    }

    It "EXPECTED:  returns an error when asking for multiple providers that do not exist" {
        $Error.Clear()
        $msg = powershell 'get-packageprovider -name NOT_EXISTS,NOT_EXISTS2  -warningaction:silentlycontinue -ea silentlycontinue; $ERROR[0].FullyQualifiedErrorId'
        $msg | should be "UnknownProviderFromActivatedList,Microsoft.PowerShell.PackageManagement.Cmdlets.GetPackageProvider"
    }
    
    It "EXPECTED:  returns an error when asking for multiple providers that do not exist -list" {
        $Error.Clear()
        $msg = powershell 'get-packageprovider -name NOT_EXISTS,NOT_EXISTS2 -ListAvailable -warningaction:silentlycontinue -ea silentlycontinue; $ERROR[0].FullyQualifiedErrorId'
        $msg | should be "UnknownProviders,Microsoft.PowerShell.PackageManagement.Cmdlets.GetPackageProvider"
    }

    It "EXPECTED: returns swidtag conformed object for powershell-based provider" {
        $onegettest = (Get-PackageProvider OneGetTest -ListAvailable | Where-Object {$_.Version.ToString() -eq "9.9.0.0"} | Select -First 1)

        $onegettest.Links.Count | should be 3

        $found = $false

        foreach ($link in $onegettest.Links)
        {
            if ($link.HRef.ToString() -match "http://oneget.org/icon" -and $link.Relationship -match "icon")
            {
                $found = $true
            }
        }

        $found | should be $true
    }
}



Describe "happy" -Tags @('BVT', 'DRT'){
    # make sure that packagemanagement is loaded
    import-packagemanagement

    It "looks for packages in bootstrap" {
        (find-package -provider bootstrap).Length | write-host
    }

    It "does something else" {
        $false | should be $false
    }
}


Describe "Get-PackageProvider with list" -Tags @('BVT', 'DRT'){
    # make sure that packagemanagement is loaded
    import-packagemanagement

    It "lists package providers installed" {
        $x = (Get-PackageProvider).Count -gt 1 | should be $true
        $y = (Get-PackageProvider -ListAvailable).Count -gt $x | should be $true
    }

    It "List two providers" {
        (get-packageprovider -name "OneGetTest" -ListAvailable).name | should match "OneGetTest"
        (get-packageprovider -name "PowerShellGet" -ListAvailable).name | should match "PowerShellGet"

        $providers = get-packageprovider -Name OneGetTest, PowerShellGet -ListAvailable
        
        $providers | ?{ $_.name -eq "OneGetTest" } | should not BeNullOrEmpty
   
        $providers | ?{ $_.name -eq "PowerShellGet" } | should not BeNullOrEmpty   
    }
       
    It "List two providers with wildcard chars" {
        $providers = get-packageprovider -Name OneGetTest* -ListAvailable
        
        $providers | ?{ $_.name -eq "OneGetTest" } | should not BeNullOrEmpty
        #all versions of the OneGetTest provider should be displayed
        $providers.Count -ge 3 | should be $true
    }
}
