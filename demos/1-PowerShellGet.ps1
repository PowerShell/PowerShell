#region Package Management

## List available package provider
Get-PackageProvider #(should show 2 providers - NuGet,PowerShellGet)
   
## Using PowerShellGet find and install other demos
# Value: equivalent of pypi
# Look for all the modules we'll be demoing today
Find-Module -Tag 'Open PowerShell','Demos'
# Pipe this to Install-Module to install them
Find-Module -Tag 'Open PowerShell','Demos' | Install-Module -Verbose
Get-Module
# Note that ScriptAnalyzer gets installed because the VSCode demo is dependent upon it

# Register trusted endpoints
Register-PackageSource -Name NuGet -Location http://nuget.org/api/v2 -Trusted -ProviderName NuGet

# Finding and installing becomes very easy
Find-Package -Name jQuery -Verbose | Install-Package -Verbose

# Discover installed packages
Get-Package -ProviderName NuGet

#endregion 
