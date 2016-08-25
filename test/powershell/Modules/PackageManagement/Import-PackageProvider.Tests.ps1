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

#$ProgramModulePath = "$Env:ProgramFiles\WindowsPowerShell\Modules"

$InternalGallery = "https://dtlgalleryint.cloudapp.net/api/v2/"
$InternalSource = 'OneGetTestSource'

<#
SKIP FOR NOW SINCE POWERSHELLGET NOT WORKING YET
make sure the package repository exists
$a=Get-PackageSource -ForceBootstrap| select Name, Location, ProviderName
    
$found = $false
foreach ($item in $a)
{       
    #name contains "." foo.bar for example for the registered sources internally
    if($item.ProviderName -eq "PowerShellGet")
    {
        if ($item.Location -eq $InternalGallery) {
            Unregister-PackageSource $item.Name -Provider "PowerShellGet" -ErrorAction SilentlyContinue
        }
    }
}

Register-PackageSource -Name $InternalSource -Location $InternalGallery -ProviderName 'PowerShellGet' -Trusted -ForceBootstrap -ErrorAction SilentlyContinue
#>
# ------------------------------------------------------------------------------
# Actual Tests:

Describe "import-packageprovider" -Tags "Feature" {
    
    It "Import -force 'PowerShellGet', a builtin package provider, Expect succeed" {
        #avoid popup for installing nuget-any.exe
        Find-PackageProvider -force 
        (Import-PackageProvider  'PowerShellGet' -verbose -force).name | should match "PowerShellGet"
    }
              
        
    It "Import a PowerShell package provider Expect succeed" -Skip {
        (get-packageprovider -name "OneGetTest" -list).name | should match "OneGetTest"
        $x = PowerShell '(Import-PackageProvider  OneGetTest -WarningAction SilentlyContinue).Name'
        $x | should match "OneGetTest"

        $x = PowerShell '(Import-PackageProvider  OneGetTest -WarningAction SilentlyContinue -force).Name'
        $x | should match "OneGetTest"
    } 

    It "Import 'OneGetTestProvider' CSharp package provider with filePath from programs folder, Expect succeed" -Skip {
    
        $path = "$($ProgramProviderInstalledPath)\Microsoft.PackageManagement.OneGetTestProvider.dll" 
        $path | should Exist

        $job=Start-Job -ScriptBlock {
            param($path) import-packageprovider -name $path;
         } -ArgumentList @($path)

        $a= $job | Receive-Job -Wait
        $a.Name | should match "OneGetTestProvider"

    } 
          
    It "Import 'PSChained1Provider' PowerShell package provider with filePath from programfiles folder, Expect succeed" -Skip {
 
        $path = "$($ProgramModulePath)\PSChained1Provider.psm1" 
        $path | should Exist

        $job=Start-Job -ScriptBlock {
            param($path) import-packageprovider -name $path; 
         } -ArgumentList @($path)

        $a= $job | Receive-Job -Wait
        $a.Name | should match "PSChained1Provider"
    }   

          
    It "Import a CSharp package provider with filePath from user folder -force, Expect succeed" -Skip {
        $path = "$($UserProviderInstalledPath)\Microsoft.PackageManagement.OneGetTestProvider.dll" 
        $path | should Exist         
        
        $job=Start-Job -ScriptBlock {
            param($path) import-packageprovider -name $path; 
         } -ArgumentList @($path)

        $a= $job | Receive-Job -Wait
        $a.Name | should match "OneGetTestProvider"
    }

    It "Import a PowerShell package provider with filePath from user folder -force, Expect succeed" -Skip {

         $path = "$($UserModuleFolder)\PSChained1Provider.psm1"
         $path  | should Exist

         $job=Start-Job -ScriptBlock {
            param($path) import-packageprovider -name $path; 
            } -ArgumentList @($path)

        $a= $job | Receive-Job -Wait
        $a.Name | should match "PSChained1Provider"
    }

    It "Import a PowerShell package provider with -force, Expect succeed" -Skip {

         $path = "$($UserModuleFolder)\PSChained1Provider.psm1"
         $path  | should Exist

         $newPath = "$($UserModuleFolder)\MyTest.psm1"
         Copy-Item -Path $path  -Destination $newPath -Force
                  

         $job=Start-Job -ScriptBlock {
            param($newPath) import-packageprovider -name $newPath -Force; 
            } -ArgumentList @($newPath)

        $a= $job | Receive-Job -Wait
        $a.Name | should match "PSChained1Provider"
         
        $job=Start-Job -ScriptBlock {

            param($newPath)

                import-packageprovider -name $newPath -Force            
                Set-Content -Path $newPath -Value "#test" -force
                import-packageprovider -name $newPath -Force 

            } -ArgumentList @($newPath)


        Receive-Job -Wait -Job $job -ErrorVariable theError 2>&1
        $theError.FullyQualifiedErrorId | should be "FailedToImportProvider,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"      
    }

    It "Import 'OneGetTest' PowerShell package provider that has multiple versions, Expect succeed" -Skip {
        #check all version of OneGetTest is listed
        $x = get-packageprovider "OneGetTest" -ListAvailable
       

        $x | ?{ $_.Version.ToString() -eq "9.9.0.0" } | should not BeNullOrEmpty          
        $x | ?{ $_.Version.ToString() -eq "3.5.0.0" } | should not BeNullOrEmpty          
        $x | ?{ $_.Version.ToString() -eq "1.1.0.0" } | should not BeNullOrEmpty   
        
        #latest one is imported
        $y = powershell '(import-packageprovider -name "OneGetTest").Version.Tostring()' 
        $y | should match  "9.9.0.0"
    } 
}



Describe "import-packageprovider Error Cases" -Tags "Feature" {

     It "Expected error when importing wildcard chars 'OneGetTest*" {
        $Error.Clear()
        import-packageprovider -name OneGetTest* -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "InvalidParameter,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"     
    }

  It "EXPECTED:  returns an error when inputting a bad version format" {
        $Error.Clear()
        import-packageprovider -name Gistprovider -RequiredVersion BOGUSVERSION -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "InvalidVersion,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"
    }

  It "EXPECTED:  returns an error when asking for a provider that does not exist" {
        $Error.Clear()
        import-packageprovider -name NOT_EXISTS  -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "NoMatchFoundForCriteria,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"
    }
   
   It "EXPECTED:  returns an error when asking for a provider with file full path and version" -Skip {
        $Error.Clear()
        import-packageprovider -name "$($ProgramModulePath)\PSChained1Provider.psm1" -RequiredVersion 9.9.9  -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "FullProviderFilePathVersionNotAllowed,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"
    }
 

   It "EXPECTED:  returns an error when asking for a provider with RequiredVersion and MinimumVersion" {
        $Error.Clear()
        import-packageprovider -name PowerShellGet -RequiredVersion 1.0 -MinimumVersion 2.0  -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "VersionRangeAndRequiredVersionCannotBeSpecifiedTogether,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"
    }

   It "EXPECTED:  returns an error when asking for a provider with RequiredVersion and MaximumVersion" {
        $Error.Clear()
        import-packageprovider -name PowerShellGet -RequiredVersion 1.0 -MaximumVersion 2.0  -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "VersionRangeAndRequiredVersionCannotBeSpecifiedTogether,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"
    }

   It "EXPECTED:  returns an error when asking for a provider with a MinimumVersion greater than MaximumVersion" {
        $Error.Clear()
        import-packageprovider -name PowerShellGet -MaximumVersion 1.0 -MinimumVersion 2.0 -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "MinimumVersionMustBeLessThanMaximumVersion,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"
    }

   It "EXPECTED:  returns an error when asking for a provider with MinimumVersion that does not exist" {
        $Error.Clear()
        Import-packageprovider -name OneGetTest -MinimumVersion 20.2 -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "NoMatchFoundForCriteria,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"
    }

   It "EXPECTED:  returns an error when asking for a provider with MaximumVersion that does not exist" {
        $Error.Clear()
        Import-packageprovider -name OneGetTest -MaximumVersion 0.2 -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "NoMatchFoundForCriteria,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"
    }

   It "EXPECTED:  returns an error when asking for a provider that has name with wildcard and version" {
        $Error.Clear()
        Import-packageprovider -name "OneGetTest*" -RequiredVersion 4.5 -force -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "MultipleNamesWithVersionNotAllowed,Microsoft.PowerShell.PackageManagement.Cmdlets.ImportPackageProvider"
    }
    
}


Describe "Import-PackageProvider with OneGetTest that has 3 versions: 1.1, 3.5, and 9.9." -Tags "Feature" {

    It "EXPECTED: success 'import OneGetTest -requiredVersion 3.5'" -Skip {
        powershell '(Import-packageprovider -name OneGetTest -requiredVersion 3.5 -WarningAction SilentlyContinue).Version.ToString()' | should match "3.5.0.0"

        # test that if we call a function with error, powershell does not hang for the provider
        $warningMsg = powershell '(Import-packageprovider -name OneGetTest -requiredVersion 3.5) | Out-Null; Get-Package -ProviderName OneGetTest 3>&1'
        $result = $warningMsg[0]

        if ($PSCulture -eq "en-US") {
            foreach($w in $warningMsg) 
            { 
                if($w -match 'WARNING: Cannot bind parameter')
                {
                    $result = $w
                }
        
            }
            $result.StartsWith('WARNING: Cannot bind parameter') | should be $true
        }
    }

    It "EXPECTED: success 'Import OneGetTest -requiredVersion 3.5 and then 9.9 -force'" -Skip {
        $a = powershell {(Import-packageprovider -name OneGetTest -RequiredVersion 3.5) > $null; (Import-packageprovider -name OneGetTest -requiredVersion 9.9 -force)} 
        $a.Version.ToString()| should match "9.9.0.0"
    }

    It "EXPECTED: success 'import OneGetTest with MinimumVersion and MaximumVersion'" -Skip {
        powershell '(Import-packageprovider -name OneGetTest -MinimumVersion 1.2 -MaximumVersion 5.0 -WarningAction SilentlyContinue).Version.ToString()' | should match "3.5.0.0"
    }
    
    It "EXPECTED: success 'OneGetTest with MaximumVersion'" -Skip {
        powershell '(Import-packageprovider -name OneGetTest -MaximumVersion 3.5 -WarningAction SilentlyContinue).Version.ToString()' | should match "3.5.0.0"
    }
    
    It "EXPECTED: success 'OneGetTest with MinimumVersion'" -Skip {
        powershell '(Import-packageprovider -name OneGetTest -MinimumVersion 2.2 -WarningAction SilentlyContinue).Version.ToString()' | should match "9.9.0.0"
    }

    It "EXPECTED: success 'OneGetTest Find-Package with Progress'" -Skip {
        $ps = [PowerShell]::Create()
        $ps.AddScript("Import-PackageProvider -Name OneGetTest -RequiredVersion 9.9; Find-Package -ProviderName OneGetTest")
        $ps.Invoke()

        $ps.Streams.Progress.Count | Should be 29

        $culture = (Get-Culture).Name

        if ($culture -eq "en-us") {
            $ps.Streams.Progress[1].Activity | Should match "Starting some progress"
            $ps.Streams.Progress[1].StatusDescription | should match "Processing"
            $ps.Streams.Progress[1].CurrentOperation | should match "Starting"

            $ps.Streams.Progress[5].Activity | should match "Updating"
            $ps.Streams.Progress[5].StatusDescription | should match "Finding packages"

            $ps.Streams.Progress[6].Activity | should match "Updating Inner"
            $ps.Streams.Progress[6].StatusDescription | should match "Finding inner packages"

            $ps.Streams.Progress[7].Activity | should match "Updating Inner"

        }

        $ps.Streams.Progress[1].ActivityId | Should Be 0
        $ps.Streams.Progress[1].ParentActivityId | should be -1
        $ps.Streams.Progress[1].SecondsRemaining | should match 10
        $ps.Streams.Progress[1].RecordType -eq [System.Management.Automation.ProgressRecordType]::Processing | should be true
        $ps.Streams.Progress[1].PercentComplete | should be 22

        $ps.Streams.Progress[2].SecondsRemaining | should be 5

        $ps.Streams.Progress[4].PercentComplete | should be 100
        $ps.Streams.Progress[4].RecordType -eq [System.Management.Automation.ProgressRecordType]::Completed | should be true
        
        $ps.Streams.Progress[5].PercentComplete | should be 0

        $ps.Streams.Progress[6].PercentComplete | should be 0

        $ps.Streams.Progress[7].PercentComplete | should be 25
    }

    It "EXPECTED: success 'OneGetTest Find-Package returns correct TagId'" -Skip {
        $ps = [PowerShell]::Create()
        $ps.AddScript("Import-PackageProvider -Name OneGetTest -RequiredVersion 9.9; Find-Package -ProviderName OneGetTest")
        $result = $ps.Invoke() | Select -First 1

        $result.TagId | should match "MyVeryUniqueTagId"
    }

    It "EXPECTED: success 'OneGetTest Get-Package returns correct package object using swidtag'" -Skip {
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript("`$null = Import-PackageProvider -Name OneGetTest -RequiredVersion 9.9; Get-Package -ProviderName OneGetTest")
        $result = $ps.Invoke() 
        
        $result.Count | should Be 2

        ($result | Select -Last 1).TagId | should match "jWhat-jWhere-jWho-jQuery"
    }

    It "EXPECTED: success 'OneGetTest Get-Package returns correct progress message'" -Skip {
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript("`$null = Import-PackageProvider -Name OneGetTest -RequiredVersion 9.9; Get-Package -ProviderName OneGetTest")
        $result = $ps.Invoke() 
        
        $ps.Streams.Progress[1].PercentComplete | should be 0
        $ps.Streams.Progress[1].Activity | should match "Updating"
        $ps.Streams.Progress[1].ActivityId | should be 10

        $ps.Streams.Progress[2].PercentComplete | should be 0
        $ps.Streams.Progress[2].Activity | should match "Updating Inner"
        $ps.Streams.Progress[2].ParentActivityId | should be 10

        $ps.Streams.Progress[3].PercentComplete | should be 25
        $ps.Streams.Progress[3].Activity | should match "Updating Inner"
        $ps.Streams.Progress[3].ParentActivityId | should be 10

    }
}

Describe "Import-PackageProvider with OneGetTestProvider that has 2 versions: 4.5, 6.1" -Tags "Feature" {
    # install onegettestprovider
    # Not working yet since powershellget not working
    # Install-PackageProvider -Name OneGetTestProvider -RequiredVersion 4.5.0.0 -Source $InternalGallery
    # Install-PackageProvider -Name OneGetTestProvider -RequiredVersion 6.1.0.0 -Source $InternalGallery

    It "EXPECTED: Get-PackageProvider -ListAvailable succeeds" -Pending {
        $providers = Get-PackageProvider -ListAvailable
        ($providers | Where-Object {$_.Name -eq 'OneGetTest'}).Count | should match 3
        ($providers | Where-Object {$_.Name -eq 'OneGetTestProvider'}).Count -ge 2 | should be $true
    }

    It "EXPECTED: Get-PackageProvider -ListAvailable succeeds even after importing gist provider" -Pending {
    	Install-PackageProvider GistProvider -Source $InternalGallery
        Import-PackageProvider Gist
        $providers = Get-PackageProvider -ListAvailable
        ($providers | Where-Object {$_.Name -eq 'OneGetTest'}).Count | should match 3
        ($providers | Where-Object {$_.Name -eq 'OneGetTestProvider'}).Count -ge 2 | should be $true
    }

    It "EXPECTED: success 'import OneGetTestProvider -requiredVersion 4.5'" -Pending {
        Import-PackageProvider -Name OneGetTestProvider -RequiredVersion 4.5 -Force
        (Get-PackageProvider OneGetTestProvider).Version.ToString() | should match '4.5.0.0'
    }

    It "EXPECTED: success 'import OneGetTestProvider with MinimumVersion and MaximumVersion'" -Pending {
        Import-packageprovider -name OneGetTestProvider -MinimumVersion 4.6 -MaximumVersion 6.2 -Force
        (Get-PackageProvider OneGetTestProvider).Version.ToString() | should match '6.1.0.0'
    }
    
    It "EXPECTED: success 'import OneGetTestProvider with MaximumVersion'" -Pending {
        Import-packageprovider -name OneGetTestProvider -MaximumVersion 4.6 -Force
        (Get-PackageProvider OneGetTestProvider).Version.ToString() | should match '4.5.0.0'
    }
    
    It "EXPECTED: success 'OneGetTestProvider with MinimumVersion'" -Pending {
        Import-packageprovider -name OneGetTestProvider -MinimumVersion 6.0.5 -Force

        (Get-PackageProvider OneGetTestProvider).Version -ge [version]'6.1.0.0' | should be $true
    }

    It "EXPECTED: success 'Import OneGetTestProvider -requiredVersion 4.5 and then 6.1 -force'" -Pending {
        Import-PackageProvider -Name OneGetTestProvider -RequiredVersion 4.5 -Force;
        Import-PackageProvider -Name OneGetTestProvider -RequiredVersion 6.1 -Force;
        (Get-PackageProvider OneGetTestProvider).Version.ToString() | should match '6.1.0.0'
    }

    It "EXPECTED: success 'Import OneGetTestProvider -MinimumVersion 4.5 and then MaximumVersion 5.0 -force'" -Pending {
        Import-PackageProvider -Name OneGetTestProvider -MinimumVersion 4.5 -Force;
        (Get-PackageProvider OneGetTestProvider).Version -ge [version]'6.1.0.0' | should be $true
        Import-PackageProvider -Name OneGetTestProvider -MaximumVersion 5.0 -Force;
        (Get-PackageProvider OneGetTestProvider).Version.ToString() | should match '4.5.0.0'
    }
}
