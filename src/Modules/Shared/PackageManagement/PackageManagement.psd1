###
# ==++==
#
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
###
@{
    GUID = "4ae9fd46-338a-459c-8186-07f910774cb8"
    Author = "Microsoft Corporation"
    CompanyName = "Microsoft Corporation"
    Copyright = "(C) Microsoft Corporation. All rights reserved."
    HelpInfoUri = "https://go.microsoft.com/fwlink/?linkid=392040"
    ModuleVersion = "1.0.0.1"
    PowerShellVersion = "3.0"
    ClrVersion = "4.0"
    RootModule = "Microsoft.PowerShell.PackageManagement.dll"
	Description = 'PackageManagement (a.k.a. OneGet) is a new way to discover and install software packages from around the web. 
 It is a manager or multiplexor of existing package managers (also called package providers) that unifies Windows package management with a single Windows PowerShell interface. With PackageManagement, you can do the following. 
  - Manage a list of software repositories in which packages can be searched, acquired and installed 
  - Discover software packages 
  - Seamlessly install, uninstall, and inventory packages from one or more software repositories'

    CmdletsToExport = @(
        'Find-Package',
        'Get-Package',
        'Get-PackageProvider', 
        'Get-PackageSource',
        'Install-Package',
        'Import-PackageProvider'
        'Find-PackageProvider'
        'Install-PackageProvider'
        'Register-PackageSource',
        'Set-PackageSource',
        'Unregister-PackageSource',
        'Uninstall-Package'
        'Save-Package'
	)

	FormatsToProcess  = @('PackageManagement.format.ps1xml')

	PrivateData = @{
        PSData = @{        
            Tags = @('PackageManagement')
            ProjectUri = 'https://oneget.org'
        } 
    }
}
