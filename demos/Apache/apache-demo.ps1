# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module $PSScriptRoot/Apache/Apache.psm1

#list Apache Modules
Write-Host -Foreground Blue "Get installed Apache Modules like *proxy* and Sort by name"
Get-ApacheModule | Where-Object {$_.ModuleName -like "*proxy*"} | Sort-Object ModuleName | Out-Host

#Graceful restart of Apache
Write-Host -Foreground Blue "Restart Apache Server gracefully"
Restart-ApacheHTTPServer -Graceful | Out-Host

#Enumerate current virtual hosts (web sites)
Write-Host -Foreground Blue "Enumerate configured Apache Virtual Hosts"
Get-ApacheVHost |Out-Host

#Add a new virtual host
Write-Host -Foreground Yellow "Create a new Apache Virtual Host"
New-ApacheVHost -ServerName "mytestserver" -DocumentRoot /var/www/html/mytestserver -VirtualHostIPAddress * -VirtualHostPort 8090 | Out-Host

#Enumerate new set of virtual hosts
Write-Host -Foreground Blue "Enumerate Apache Virtual Hosts Again"
Get-ApacheVHost |Out-Host

#Cleanup
Write-Host -Foreground Blue "Remove demo virtual host"
if (Test-Path "/etc/httpd/conf.d"){
    & sudo rm "/etc/httpd/conf.d/mytestserver.conf"
}
if (Test-Path "/etc/apache2/sites-enabled"){
    & sudo rm "/etc/apache2/sites-enabled/mytestserver.conf"
}
