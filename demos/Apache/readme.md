## Apache Management Demo

This demo shows management of Apache HTTP Server with PowerShell cmdlets implemented in a script module.

- **Get-ApacheVHost**: Enumerate configured Apache Virtual Host (website) instances as objects.
- **Get-ApacheModule**: Enumerate loaded Apache modules
- **Restart-ApacheHTTPserver**: Restart the Apache web server
- **New-ApacheVHost**: Create a new Apache Virtual Host (website) based on supplied parameters


## Prerequisites ##
- Install PowerShell
- Install Apache packages
	- `sudo apt-get install apache2`
	- `sudo yum install httpd`


Note: Management of Apache requires privileges. The user must have authorization to elevate with sudo. You will be prompted for a sudo password when running the demo.