#region Package Management

## List available package provider
Get-PackageProvider #(should show 2 providers - NuGet,PowerShellGet)
   
## Using PowerShellGet find and install other demos
Find-Module -Tag 'Open PowerShell','Demos' | Install-Module -Verbose
Get-Module

# Find and Download node.js from nuget.org
Find-Package -Name node.js -ProviderName NuGet -Verbose -Source http://nuget.org/api/v2

# Register trusted endpoints
Register-PackageSource -Name NuGet -Location http://nuget.org/api/v2 -Trusted -ProviderName NuGet

# Finding and installing becomes very easy
Find-Package -Name jQuery -Verbose | Install-Package -Verbose

# Discover installed packages
Get-Package -ProviderName NuGet

#endregion 