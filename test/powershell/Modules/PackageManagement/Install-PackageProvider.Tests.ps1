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
# ------------------ PackageManagement Test  -----------------------------------
$InternalGallery = "https://dtlgalleryint.cloudapp.net/api/v2/"
$InternalGallery2 = "http://dtlgalleryint.cloudapp.net/api/v2/"
$InternalSource = 'OneGetTestSource'
$InternalSource2 = 'OneGetTestSource2'
$ProviderFolder = "$env:ProgramFiles\PackageManagement\ProviderAssemblies"

#make sure the package repository exists
<# Cannot run these yet since powershellget not working yet
$a=Get-PackageSource -ForceBootstrap| select Name, Location, ProviderName
    
$found = $false
foreach ($item in $a)
{       
    #name contains "." foo.bar for example for the registered sources internally
    if($item.ProviderName -eq "PowerShellGet")
    {
        if (($item.Location -eq $InternalGallery) -or ($item.Location -eq $InternalGallery2)) {
            Unregister-PackageSource $item.Name -Provider "PowerShellGet" -ErrorAction SilentlyContinue
        }
    }
}

Register-PackageSource -Name $InternalSource -Location $InternalGallery -ProviderName 'PowerShellGet' -Trusted -ForceBootstrap -ErrorAction SilentlyContinue
Register-PackageSource -Name $InternalSource2 -Location $InternalGallery2 -ProviderName 'PowerShellGet' -ForceBootstrap -ErrorAction SilentlyContinue
#>

# ------------------------------------------------------------------------------
# Actual Tests:

Describe "install-packageprovider" -Tags "Feature" {

    <#
    BeforeEach {

        $m= Get-InstalledModule -Name myalbum -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        if($m -and $m.InstalledLocation)
        {
            Remove-Item -Path $m.InstalledLocation -Recurse -force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Verbose
        }
    }
    #>

    It "install-packageprovider, Expect succeed" -Pending {
        $a = (install-PackageProvider -name gistprovider -force -source $InternalSource).name 
        $a -contains "gistprovider" | should be $true "gistprovider"

        # check for swidtag field
        $gist = (Get-PackageProvider gist -ListAvailable | Select -First 1)

        $gist.Entities.Count | should be 2

        $found = $false

        foreach ($entity in $gist.Entities)
        {
            if ($entity.Name -match "Doug Finke" -and $entity.Role -match "author")
            {
                $found = $true
            }
        }

        $found | should be $true
        
        $gist.VersionScheme | should match "MultiPartNumeric"
    }
            

    It "install-packageprovider from bootstrap web site, Expect succeed" -Skip {
        $a = (Install-PackageProvider -Name nuget -RequiredVersion 2.8.5.127 -Force).name 
        $a | should match "nuget"

        # check for swidtag field
        $nugetBootstrapped = (Get-PackageProvider -Name nuget -ListAvailable | Where-Object {$_.Version.ToString() -eq "2.8.5.127"} | Select-Object -First 1)
        
        $nugetBootstrapped.Links[0].HRef.ToString() | should match "https://oneget.org/nuget-anycpu-2.8.5.127.exe"
        $nugetBootstrapped.Links[0].Relationship | should match "installationmedia"
    }
    
    It "find | install-packageprovider -name array, Expect succeed" -Pending {
        $names=@("gistprovider", "TSDProvider")

        #
        $a = (find-PackageProvider -name $names -Source $InternalSource | Install-PackageProvider -force).name
        $a -contains "GistProvider" | should be $true
        $a -contains "TSDProvider" | should be $true
    }

    It "find | install-packageprovider nuget should imported and installed, Expect succeed" -Skip {
       
        $a = Find-PackageProvider -name NuGet -RequiredVersion 2.8.5.202 | install-PackageProvider -force 
        $a.Name | Should match "NuGet"
        $a.Version | Should match "2.8.5.202"
        
    }

    It "find | install-packageprovider nuget should imported and installed, Expect succeed" -Skip {       
       
        $a = install-PackageProvider -name NuGet  -force
        $a | ?{ $_.name -eq "NuGet" } | should not BeNullOrEmpty
        $a | ?{ $_.Version -gt "2.8.5.202"  } | should not BeNullOrEmpty       
    }

    It "install-packageprovider myalbum should imported and installed, Expect succeed" -Pending {
       
        $a = Install-PackageProvider -name MyAlbum -Source $InternalSource  -force
        $a | ?{ $_.name -eq "MyAlbum" } | should not BeNullOrEmpty          
    }

    It "find | install-packageprovider myalbum should imported and installed, Expect succeed" -Pending {
       
        $a = Find-PackageProvider -name MyAlbum -Source $InternalSource -RequiredVersion 0.1.2 | install-PackageProvider -force
        $a | ?{ $_.name -eq "MyAlbum" } | should not BeNullOrEmpty
        $a | ?{ $_.Version -eq "0.1.2"  } | should not BeNullOrEmpty             
    }
 }

<# Don't need this test since we are not boostraping
Describe "install-packageprovider with local source" -Tags "Feature" {

    BeforeAll{
        if( test-path $destination ) {
            rmdir -recurse -force $destination -ea silentlycontinue
        }
        mkdir $destination -ea silentlycontinue

        $nugetprovider = (install-PackageProvider -name nuget -force -RequiredVersion 2.8.5.201).name
        $nugetprovider -contains "nuget" | should be $true "nuget"
        
        $nugetprovider = (install-PackageProvider -name nuget -force -RequiredVersion 2.8.5.202).name
        $nugetprovider -contains "nuget" | should be $true "nuget"

        # setup the test folder

        mkdir $destination\2.8.5.202 -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        mkdir $destination\2.8.5.201 -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        Get-ChildItem "$ProviderFolder\Nuget\2.8.5.202" -Recurse | Copy-Item -Destination $destination\2.8.5.202\Microsoft.PackageManagement.NuGetProvider.dll -Force -Verbose
        Get-ChildItem "$ProviderFolder\Nuget\2.8.5.201" -Recurse | Copy-Item -Destination $destination\2.8.5.201\Microsoft.PackageManagement.NuGetProvider.dll -Force -Verbose
        Get-ChildItem "$Env:ProgramFiles\WindowsPowerShell\Modules\Microsoft.PackageManagement.Test.dll" | Copy-Item  -Destination $destination -Force -verbose

    }

    AfterAll{

        if( test-path $destination ) {
            rmdir -recurse -force $destination -ea silentlycontinue
        }

        $testprovider = "$ProviderFolder\Test Providers for OneGet"
        if( test-path $testprovider ) {
            rmdir -recurse -force $testprovider -ea silentlycontinue
        }
          
        #make sure we are using the latest Nuget provider
        Install-PackageProvider -name nuget -force    
    }



    It "Find-packageprovider -source filefolder, Expect succeed" {      
 
        $a= (Find-PackageProvider -source $destination).Name

        $a -like "nuget*" | should be $true 
        $a -like "Test Providers for OneGet" | should be $true 

    }

    It "Find-packageprovider -source filefolder -name, Expect the latest version returned" {      
 
        $a= Find-PackageProvider -source $destination -name nuget*
     
        $a.Name -like "nuget*" | should match $true 
        $a.Version | should match "2.8.5.202"
    }

    It "Find-packageprovider -source filefolder -name -allversions, Expect All versions returned" {      
 
        $a= Find-PackageProvider -source $destination -name nuget* -AllVersions

        #  all versions returned
        $a | ?{$_.version -eq "2.8.5.202" } | should not BeNullOrEmpty
        $a | ?{$_.version -eq "2.8.5.201" } | should not BeNullOrEmpty

    }
    
    It "Find-packageprovider -source file, Expect succeed" {      
        $a= (Find-PackageProvider -source "$destination\Microsoft.PackageManagement.Test.dll").Name

        $a -like "nuget" | should not be $true 
        $a -contains "Test Providers for OneGet" | should be $true 

    }        


   It "Install-packageprovider -source file, Expect succeed" {      

        $a= (Install-PackageProvider -source "$destination\Microsoft.PackageManagement.Test.dll" -name "Test Providers for OneGet" -force).Name

        $a -eq "Test Providers for OneGet" | should be $true 

    } 

    It "install-PackageProvider -source folder, Expect succeed" {      
      
        $a= (Install-PackageProvider -force -source $destination -name "Test Providers for OneGet")
       

        $a.Name -eq "Test Providers for OneGet" | should be $true 

    }

    It "Find-packageprovider and install-PackageProvider, Expect succeed" {      
      
        $a= (find-PackageProvider  -Name "Test Providers for OneGet" -Source $destination | Install-PackageProvider -force)
       
        $a.Name -contains "Test Providers for OneGet" | should be $true 

    }   

    It "Find and Install PackageProvider with version, Expect succeed" {      
      
        $a= install-PackageProvider -Name nugetprovider -Source $destination -RequiredVersion 2.8.5.201 -verbose -force
       
        $a.Name -like "nuget*" | should be $true
        $a.Version | should match "2.8.5.201"

    }
 }
#>

Describe "Install-Save-Package with multiple sources" -Tags "Feature" {
    $destination = Join-Path $TestDrive "installpp"

    It "install-package with array of registered sources with a single provider, Expect succeed" -Pending {

        #powershellget is the provider selected
        $x= install-package TSDProvider -force -Source @($InternalSource, $InternalSource2)      
                
        $x | ?{ $_.name -eq "TSDProvider" } | should not BeNullOrEmpty

        $y= install-package TSDProvider -force -Source @($InternalGallery, $InternalGallery2) -ProviderName nuget     
                
        $y | ?{ $_.name -eq "TSDProvider" } | should not BeNullOrEmpty

    }

    It "install-package with array of sources, Expect succeed" {

        $x= install-package jquery -force -Source @('foooobarrrr', 'https://www.nuget.org/api/v2')  -ProviderName @('PowershellGet', 'NuGet')    
                
        $x | ?{ $_.name -eq "jquery" } | should not BeNullOrEmpty
        #$x | ?{ $_.Source -eq "https://www.nuget.org/api/v2" } | should not BeNullOrEmpty
    }

    It "install-save-package matches with multiple providers with single source, Expect succeed" -Pending {

        try
        {
            Register-PackageSource -Name foo -Location $InternalGallery -ProviderName 'PowerShellGet' -Trusted  -ErrorAction SilentlyContinue
            Register-PackageSource -Name bar -Location $InternalGallery -ProviderName 'NuGet'  -ErrorAction SilentlyContinue
            if (test-path "$destination") {
                Remove-Item $destination -force -Recurse
            }

         
            $x= install-package tsdprovider -force -Source $InternalGallery  -ProviderName @('NuGet', 'PowershellGet')    
                
            $x | ?{ $_.name -eq "tsdprovider" } | should not BeNullOrEmpty
            $x | ?{ $_.Source -eq "bar" } | should not BeNullOrEmpty
            $x | ?{ $_.Providername -eq "NuGet" } | should not BeNullOrEmpty

            $y= save-package tsdprovider -force -Source $InternalGallery  -ProviderName @('NuGet', 'PowershellGet') -path $destination   
                
            $y | ?{ $_.name -eq "tsdprovider" } | should not BeNullOrEmpty
            $y | ?{ $_.Source -eq "bar" } | should not BeNullOrEmpty
            $y | ?{ $_.Providername -eq "NuGet" } | should not BeNullOrEmpty
        
            (test-path "$destination\TSDProvider*") | should be $true
            if (test-path "$destination\TSDProvider*") {
                Remove-Item $destination\TSDProvider* -force -Recurse
            }
        }
        finally
        {
            UnRegister-PackageSource -Name foo  -ErrorAction SilentlyContinue
            UnRegister-PackageSource -Name bar  -ErrorAction SilentlyContinue
          
        }
    }

    It "install-save-package does not match with any providers with single source, Expect fail" -Pending {

        try
        {
            Register-PackageSource -Name foo -Location $InternalGallery -ProviderName 'PowerShellGet' -Trusted  -ErrorAction SilentlyContinue
            Register-PackageSource -Name bar -Location $InternalGallery -ProviderName 'NuGet'  -ErrorAction SilentlyContinue


            $Error.Clear()
            $x= install-package jquery -force -Source $InternalGallery  -ProviderName @('NuGet', 'PowershellGet') -ErrorVariable theError  -ErrorAction SilentlyContinue
            $theError.FullyQualifiedErrorId | should be "NoMatchFoundForCriteria,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackage"

        
            $Error.Clear()
            $y= save-package jquery -force -Source $InternalGallery  -ProviderName @('NuGet', 'PowershellGet') -path $destination -ErrorVariable theError -ErrorAction SilentlyContinue  
            $theError.FullyQualifiedErrorId | should be "NoMatchFoundForCriteria,Microsoft.PowerShell.PackageManagement.Cmdlets.SavePackage"
                
        }
        finally
        {
            UnRegister-PackageSource -Name foo  -ErrorAction SilentlyContinue
            UnRegister-PackageSource -Name bar  -ErrorAction SilentlyContinue
          
        }
    }

    It "install-save-package when the multiple providers find a package but no providers specified, Expect fail" -Pending {

        try
        {
            Register-PackageSource -Name foo -Location $InternalGallery -ProviderName 'PowerShellGet' -Trusted  -ErrorAction SilentlyContinue
            Register-PackageSource -Name bar -Location $InternalGallery -ProviderName 'NuGet'  -ErrorAction SilentlyContinue


            $Error.Clear()
            $x= install-package tsdprovider -force -Source @($InternalGallery)  -ErrorVariable theError  -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
            $theError.FullyQualifiedErrorId | should be "DisambiguateForInstall,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackage"

        
            $Error.Clear()
            $y= save-package tsdprovider -force -Source @($InternalGallery)  -path $destination -ErrorVariable theError -ErrorAction SilentlyContinue -WarningAction SilentlyContinue 
            $theError.FullyQualifiedErrorId | should be "DisambiguateForInstall,Microsoft.PowerShell.PackageManagement.Cmdlets.SavePackage"
                
        }
        finally
        {
            UnRegister-PackageSource -Name foo  -ErrorAction SilentlyContinue
            UnRegister-PackageSource -Name bar  -ErrorAction SilentlyContinue
          
        }
    }
    
    # Skip as we don't have chocolatey anymore
    It "install-save-package with multiple names, providers, Expect succeed" -Skip {   
                    
       try {
          
            Register-PackageSource -Name foobar -Location http://www.nuget.org/api/v2 -ProviderName 'NuGet'  -ErrorAction SilentlyContinue

            if (test-path "$destination") {
                Remove-Item $destination -force -Recurse
            }

         
            $x= install-package  -name @('Bootstrap', 'jquery') -force  -ProviderName @('NuGet', 'Chocolatey')   
                
            $x | ?{ $_.name -eq "jquery" } | should not BeNullOrEmpty
            $x | ?{ $_.name -eq "Bootstrap" } | should not BeNullOrEmpty
            

            $y= save-package -name 'jquery' -force -ProviderName @('NuGet', 'Chocolatey') -path $destination   
                
            $x | ?{ $_.name -eq "jquery" } | should not BeNullOrEmpty

        
            (test-path "$destination\jquery*") | should be $true
            if (test-path "$destination\jquery*") {
                Remove-Item $destination\jquery* -force -Recurse
            }
        }
        finally
        {
            UnRegister-PackageSource -Name foobar  -ErrorAction SilentlyContinue          
        }
     
    }
    
    # Skip as no chocolatey
    It "install-save-package with multiple names, providers and sources, Expect succeed" -Skip {   
             
                 
        # Contoso and Contososerver can be found from $InternalGallery - No ambiguity.
        # Jquery can be found by the NuGet from both source locations provided.  
        

        if (test-path "$destination") {
            Remove-Item $destination -force -Recurse
        }

         
        $x= install-package  -name @('Contoso', 'jquery', 'ContosoServer') -force -Source @($InternalGallery, 'http://www.nuget.org/api/v2', 'https://www.nuget.org/api/v2') -ProviderName @('NuGet', 'Chocolatey')   
                
        $x | ?{ $_.name -eq "jquery" } | should not BeNullOrEmpty
        $x | ?{ $_.name -eq "Contoso" } | should not BeNullOrEmpty
        $x | ?{ $_.name -eq "ContosoServer" } | should not BeNullOrEmpty
            

        $y= save-package -name @('Contoso', 'jquery', 'ContosoServer') -force -Source @($InternalGallery,'http://www.nuget.org/api/v2','https://www.nuget.org/api/v2')  -ProviderName @('NuGet', 'Chocolatey') -path $destination   
                
        $x | ?{ $_.name -eq "jquery" } | should not BeNullOrEmpty
        $x | ?{ $_.name -eq "Contoso" } | should not BeNullOrEmpty
        $x | ?{ $_.name -eq "ContosoServer" } | should not BeNullOrEmpty
        
        (test-path "$destination\Contoso*") | should be $true
        if (test-path "$destination\Contoso*") {
            Remove-Item $destination\Contoso* -force -Recurse
        }

    }

    It "save-package with array of registered sources, Expect succeed" -Pending {
        if (test-path "$destination") {
            Remove-Item $destination -force -Recurse
        }

        $x= save-package TSDProvider -force -Source @($InternalSource, $InternalSource2) -path $destination  -ProviderName @('PowershellGet', 'NuGet')   
                
        $x | ?{ $_.name -eq "TSDProvider" } | should not BeNullOrEmpty

        (test-path "$destination\TSDProvider*") | should be $true
        if (test-path "$destination\TSDProvider*") {
            Remove-Item $destination\TSDProvider* -force -Recurse
        }
    }

    It "save-package with array of sources, Expect succeed" -Skip {
        if (test-path "$destination") {
            Remove-Item $destination -force -Recurse
        }

        $x= save-package jquery -force -Source @('fffffbbbbb', 'https://www.nuget.org/api/v2') -path $destination   -ProviderName @('Nuget', 'Chocolatey')     
                
        $x | ?{ $_.name -eq "jquery" } | should not BeNullOrEmpty
        $x | ?{ $_.Source -eq "https://www.nuget.org/api/v2" } | should not BeNullOrEmpty
        
        (test-path "$destination\jquery*") | should be $true
        if (test-path "$destination\jquery*") {
            Remove-Item $destination\jquery* -force -Recurse
        }
    }
}
 Describe "install-packageprovider with Whatif" -Tags "Feature" {
    # make sure that packagemanagement is loaded
    #import-packagemanagement

    BeforeEach{
        $tempFile = [System.IO.Path]::GetTempFileName() 
        $whatif = "What if: Performing the operation";
    }

    AfterEach {
        if(Test-Path $tempFile)
        {
            Remove-Item $tempFile -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        }
    }

     It "install-packageprovider -name nuget with whatif, Expect succeed" -Skip {
       
       if($PSCulture -eq 'en-US'){

        # Start Transcript
        Start-Transcript -Path $tempFile
		
        install-PackageProvider -name nuget -force -warningaction:silentlycontinue -ErrorAction SilentlyContinue -whatif  

        # Stop Transcript and get content of transcript file
        Stop-Transcript
        $transcriptContent = Get-Content $tempFile

        $transcriptContent | where { $_.Contains( $whatif ) } | should be $true


        Remove-Item $whatif -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        }
    }

    It "install-packageprovider -name gistprovider with whatif, Expect succeed" -Pending {
       
       if($PSCulture -eq 'en-US'){
        # Start Transcript
        Start-Transcript -Path $tempFile
		
        install-PackageProvider -name gistprovider -force -source $InternalGallery -warningaction:silentlycontinue -ErrorAction SilentlyContinue -whatif  

        # Stop Transcript and get content of transcript file
        Stop-Transcript
        $transcriptContent = Get-Content $tempFile

        $transcriptContent | where { $_.Contains( $whatif ) } | should be $true


        Remove-Item $whatif -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        }
    }
}

Describe "install-packageprovider with Scope" -Tags "Feature" {
    # PENDING a lot of these tests because jobs are broken on PowerShell from GitHub

    BeforeAll {
        if ($IsWindows)
        {
            $userName = "smartguy"
            $password = "password%1"
            #net user $userName /delete | Out-Null
            net user $userName $password /add
            $secesurestring = ConvertTo-SecureString $password -AsPlainText -Force
            $credential = new-object -typename System.Management.Automation.PSCredential -argumentlist $userName, $secesurestring
        }
    }
    
    AfterEach {

        $m= Get-InstalledModule -Name tsdprovider -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        if($m -and $m.InstalledLocation)
        {
            Remove-Item -Path $m.InstalledLocation -Recurse -force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Verbose
        }
    }

    It "install-packageprovider without scope in a non-admin console, expect fail" -Pending {
       
        $Error.Clear()
                      
        $job=Start-Job -ScriptBlock { Install-PackageProvider -Name gistprovider  -force -requiredVersion 2.8.5.127} -Credential $credential

        Receive-Job -Wait -Job $job -ErrorVariable theError 2>&1
        $theError.FullyQualifiedErrorId | should be "InstallRequiresCurrentUserScopeParameterForNonAdminUser,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    } 
        
    It "install-packageprovider without scope in a non-admin console, expect fail" -Pending {
       
        $Error.Clear()
                      
        $job=Start-Job -ScriptBlock { Install-PackageProvider -Name gistprovider  -force } -Credential $credential

        Receive-Job -Wait -Job $job -ErrorVariable theError 2>&1
        $theError.FullyQualifiedErrorId | should be "InstallRequiresCurrentUserScopeParameterForNonAdminUser,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    } 

    It "install-packageprovider with AllUsers scope in a non-admin console, expect fail" -Pending {
        $Error.Clear()
                      
        $job=Start-Job -ScriptBlock { Install-PackageProvider -Name gistprovider -force -scope AllUsers} -Credential $credential

        Receive-Job -Wait -Job $job -ErrorVariable theError2 2>&1
        $theError2.FullyQualifiedErrorId | should be "InstallRequiresCurrentUserScopeParameterForNonAdminUser,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"

    } 

    It "install-packageprovider CurrentUser scope in a non-admin console, expect succeed" -Pending {
    $Error.Clear()

    $job=Start-Job -ScriptBlock {
        
        $source="testsource"
        $x =Get-PackageSource -Name $source -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        if ($x)
        {
            Write-Verbose "exist $source"
        }
        else
        {
            Write-Verbose "'$source' does not exist. Registering it"
            $InternalGallery = "https://dtlgalleryint.cloudapp.net/api/v2/"
            Register-PackageSource -Name $source -Location $InternalGallery -ProviderName 'PowerShellGet' -Trusted -ForceBootstrap -ErrorAction SilentlyContinue
        }
           
        Install-PackageProvider -Name tsdprovider -force -scope CurrentUser -source $source
    }   -Credential $credential


    $a= Receive-Job -Wait -Job $job
    $a | ?{ $_.name -eq "tsdprovider" } | should not BeNullOrEmpty 
    }

    It "find and install-packageprovider without scope in a non-admin console, expect fail" -Pending {
        $Error.Clear()
                      
        $job=Start-Job -ScriptBlock { Find-PackageProvider -Name gistprovider | Install-PackageProvider -force} -Credential $credential

        Receive-Job -Wait -Job $job -ErrorVariable theError3 2>&1
        $theError3.FullyQualifiedErrorId | should be "InstallRequiresCurrentUserScopeParameterForNonAdminUser,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"

    } 
    
    It "find and install-packageprovider CurrentUser scope in a non-admin console, expect succeed" -Pending {
        $Error.Clear()
                      
        $job=Start-Job -ScriptBlock { Find-PackageProvider -Name tsdprovider | Install-PackageProvider -force -scope CurrentUser} -Credential $credential

        $a= Receive-Job -Wait -Job $job 
        $a | ?{ $_.name -eq "tsdprovider" } | should not BeNullOrEmpty
    } 
}
Describe "install-PackageProvider with Versions" -Tags "Feature" {
    # make sure that packagemanagement is loaded
    <# Nuget
    2.8.5.127
    2.8.5.122
    2.8.5.120
    2.8.5.101
    2.8.5.24#>

    It "EXPECTED: success 'install, import, and get nuget package provider'" -Pending {
        # Have to change from using nuget to gist when we enable this test
        (install-packageprovider -name Nuget -requiredVersion 2.8.5.122 -force).Version.ToString() | should match "2.8.5.122"
        
        $x = powershell {(import-packageprovider -name nuget -requiredVersion  2.8.5.122 -force > $null); get-packageprovider -name nuget}
        $x.Name | should match  "Nuget"
        $x | ?{ $_.Version.ToString() -eq "2.8.5.122" } | should not BeNullOrEmpty   
    }
    
    It "Install, import, and get a powershell package provider-required version" -Pending {
        $a = (install-PackageProvider -name gistprovider -force -requiredversion 1.5 -source $InternalSource) 
        $a.Name -contains "gistprovider" | should be $true
        $a.Version -contains "1.5" | should be $true

        $x = powershell {(import-packageprovider -name gist -requiredVersion 1.5 -force > $null); get-packageprovider -name gist -list}
        
        $x | ?{ $_.name -eq "Gist" } | should not BeNullOrEmpty
        $x | ?{ $_.Version.ToString() -eq "1.5.0.0" } | should not BeNullOrEmpty  
    }

    It "EXPECTED: success 'install a provider with MinimumVersion and MaximumVersion'" -Pending {
        # Have to change from using nuget to gist when we enable this test
        (install-packageprovider -name nuget -MinimumVersion 2.8.5.101 -MaximumVersion 2.8.5.123 -force).Version.ToString() | should match "2.8.5.122"
    }
    
    It "EXPECTED: success 'install a provider with MaximumVersion'" -Pending {
        # Have to change from using nuget to gist when we enable this test
        (install-packageprovider -name nuget -MaximumVersion 2.8.5.122 -force).Version.ToString() | should match "2.8.5.122"
    }
    
    It "EXPECTED: success 'install a provider with MaximumVersion'" -Pending {
        $a = (install-packageprovider -name gistprovider -force -Source $InternalGallery).Version.ToString() 
        $b = (install-packageprovider -name gistprovider -MinimumVersion 0.6 -force -Source $InternalSource).Version.ToString() 

        $a -eq $b | should be $true
    }
}    


Describe "Get-package with mulitiple providers" -Tags "Feature" {

    It "Get-package with multiple providers" -Pending {

        $a = Install-package -Name TSDProvider -Source $InternalSource -ProviderName PowerShellGet -Force
        $b = install-package -name TSDProvider  -Source $InternalGallery -ProviderName NuGet -Force
        

        $a.Name | should be "TSDProvider"
        $b.Name | should be "TSDProvider"

        $c = Get-Package -name TSDProvider

        $c.Count -ge 2 | should be $true
        $c | ?{ $_.ProviderName -eq "PowerShellGet" } | should not BeNullOrEmpty 
        $c | ?{ $_.ProviderName -eq "NuGet" } | should not BeNullOrEmpty   

    }

}

Describe "install-packageprovider Error Cases" -Tags "Feature" {

  AfterAll {
        Unregister-PackageSource -Name OneGetTestSource -Verbose -ErrorAction SilentlyContinue
        Unregister-PackageSource -Name OneGetTestSource2 -Verbose -ErrorAction SilentlyContinue
   }
  BeforeAll {
        <#
        #commented out as powershellget is not working yet
        #make sure we are using the latest Nuget provider
        Register-PackageSource -Name $InternalSource -Location $InternalGallery -ProviderName 'PowerShellGet' -Trusted -ForceBootstrap -ErrorAction SilentlyContinue
        Register-PackageSource -Name $InternalSource2 -Location $InternalGallery2 -ProviderName 'PowerShellGet' -ForceBootstrap -ErrorAction SilentlyContinue
        #>
   }

    It "install-packageprovider -name with wildcards, Expect error" -Pending {
        $Error.Clear()
        install-PackageProvider -name gist* -force -source $InternalGallery -warningaction:silentlycontinue -ErrorVariable wildcardError -ErrorAction SilentlyContinue        
        $wildcardError.FullyQualifiedErrorId| should be "WildCardCharsAreNotSupported,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    }

    It "install-packageprovider - EXPECTED:  returns an error when multiples sources contain the same package provider" -Pending {
        $Error.Clear()
        $providers = find-packageprovider -name gistprovider
        $providers | ?{ $_.Source -match $InternalSource } | should not BeNullOrEmpty
        $providers | ?{ $_.Source -match $InternalSource2 } | should not BeNullOrEmpty

        install-packageprovider -name gistprovider -source @($InternalSource, $InternalSource2) -ErrorVariable theError -force
        $theError.FullyQualifiedErrorId| should BeNullOrEmpty
    }

    It "install-package - EXPECTED:  returns an error when multiples sources contain the same package provider" -Pending {
        $Error.Clear()
        $providers = find-package -name gistprovider 
        $providers | ?{ $_.Source -match $InternalSource } | should not BeNullOrEmpty
        $providers | ?{ $_.Source -match $InternalSource2 } | should not BeNullOrEmpty

        install-package -name gistprovider -force -Source @($InternalSource,$InternalSource2) -warningaction:silentlycontinue -ErrorVariable theError2  -ErrorAction SilentlyContinue
        $theError2.FullyQualifiedErrorId| should BeNullOrEmpty
    }

    It "save-package - EXPECTED:  returns an error when multiples sources contain the same package provider" -Pending {
        $Error.Clear()
        $providers = find-package -name gistprovider 
        $providers | ?{ $_.Source -match $InternalSource } | should not BeNullOrEmpty
        $providers | ?{ $_.Source -match $InternalSource2 } | should not BeNullOrEmpty

        if(-not (test-path $destination) ) {
             mkdir $destination -ea silentlycontinue
        }
       
        save-package -name gistprovider -Source @($InternalSource,$InternalSource2) -path $destination -warningaction:silentlycontinue -ErrorVariable theError2  -ErrorAction SilentlyContinue
        
        $theError2.FullyQualifiedErrorId| should BeNullOrEmpty
    }

    It "EXPECTED:  returns an error when inputing a bad version format" {
        $Error.Clear()
        install-packageprovider -name nuget -RequiredVersion BOGUSVERSION  -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "InvalidVersion,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    }


  It "EXPECTED:  returns an error when asking for a provider that does not exist" {
        $Error.Clear()
        install-packageprovider -name NOT_EXISTS -Scope CurrentUser -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "NoMatchFoundForProvider,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    }
 

   It "EXPECTED:  returns an error when asking for a provider with RequiredVersoin and MinimumVersion" {
        $Error.Clear()
        install-packageprovider -name NOT_EXISTS -Scope CurrentUser -RequiredVersion 1.0 -MinimumVersion 2.0  -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "VersionRangeAndRequiredVersionCannotBeSpecifiedTogether,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    }

   It "EXPECTED:  returns an error when asking for a provider with RequiredVersoin and MaximumVersion" {
        $Error.Clear()
        install-packageprovider -name NOT_EXISTS -Scope CurrentUser -RequiredVersion 1.0 -MaximumVersion 2.0  -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "VersionRangeAndRequiredVersionCannotBeSpecifiedTogether,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    }

   It "EXPECTED:  returns an error when asking for a provider with a MinimumVersion greater than MaximumVersion" {
        $Error.Clear()
        install-packageprovider -name nuget -Scope CurrentUser -MaximumVersion 1.0 -MinimumVersion 2.0 -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "NoMatchFoundForProvider,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    }

   It "EXPECTED:  returns an error when asking for a provider with MinimumVersion that does not exist" {
        $Error.Clear()
        install-packageprovider -name gistprovider -MinimumVersion 20.2 -warningaction:silentlycontinue -Scope CurrentUser -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "NoMatchFoundForProvider,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    }

   It "EXPECTED:  returns an error when asking for a provider with MaximumVersion that does not exist" {
        $Error.Clear()
        install-packageprovider -name gistprovider -Scope CurrentUser -MaximumVersion 0.1 -warningaction:silentlycontinue -ea silentlycontinue
        $ERROR[0].FullyQualifiedErrorId | should be "NoMatchFoundForProvider,Microsoft.PowerShell.PackageManagement.Cmdlets.InstallPackageProvider"
    }        
}
