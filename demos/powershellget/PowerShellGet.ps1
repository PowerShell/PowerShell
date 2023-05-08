# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#region find, install, update, uninstall the PowerShell scripts from an online repository.
# Value: equivalent of pypi

# List of PowerShellGet commands
Get-Command -Module PowerShellGet

# Discover PowerShell Scripts
Find-Script
Find-Script -Name Start-Demo

# Save scripts to a specified location
Save-Script Start-Demo -Repository PSGallery -Path /tmp
Get-ChildItem -Path /tmp/Start-Demo.ps1

# Install a script to the common scripts location
Find-Script -Name Start-Demo -Repository PSGallery  | Install-Script
Get-InstalledScript

# Install another script to show the update functionality
Install-Script Fabrikam-Script -RequiredVersion 1.0
Get-InstalledScript
Get-InstalledScript Fabrikam-Script | Format-List *

# Update the installed scripts
Update-Script -WhatIf
Update-Script
Get-InstalledScript

# Uninstall a script file
Uninstall-Script Fabrikam-Script -Verbose

#endregion

#region Using PowerShellGet find and install modules

# Value: equivalent of pypi
# Look for all the modules we'll be demoing today
Find-Module -Tag 'PowerShellCore_Demo'

# Save module to specified location
Save-Module -Tag 'PowerShellCore_Demo' -Path /tmp

# Pipe this to Install-Module to install them
Find-Module -Tag 'PowerShellCore_Demo' | Install-Module -Verbose
Get-InstalledModule

# Update all installed modules
Update-Module

#endregion

#region Using PowerShellGet with tags

# Look for all the scripts we'll be demoing today
Find-Script -Tag 'PowerShellCore_Demo'

# Pipe this to Install-Script to install them
Find-Script -Tag 'PowerShellCore_Demo' | Install-Script -Verbose
Get-InstalledScript

#endregion

#region Working with PowerShellGet repositories

# List available PS repositories
Get-PSRepository

# Register a new private feed
Register-PSRepository -Name "myPrivateGallery" –SourceLocation "https://www.myget.org/F/powershellgetdemo/api/v2" -InstallationPolicy Trusted

# Change the trust level for a repositories
Set-PSRepository -Name "myPrivateGallery" -InstallationPolicy "Untrusted"

# Remove a private feed
Unregister-PSRepository -Name "myPrivateGallery"

#endregion
