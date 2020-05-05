# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#Get Distro type and set distro-specific variables
$OSname = Get-Content "/etc/os-release" |Select-String -Pattern "^Name="
$OSName = $OSName.tostring().split("=")[1].Replace('"','')
if ($OSName -like "Ubuntu*"){
    $distro = "Ubuntu"
    $ApachePackages = @("apache2","php5","libapache2-mod-php5")
    $ServiceName = "apache2"
    $VHostDir = "/etc/apache2/sites-enabled"
    $PackageManager = "apt"
}elseif (($OSName -like "CentOS*") -or ($OSName -like "Red Hat*") -or ($OSname -like "Oracle*")){
    $distro = "Fedora"
    $ApachePackages = @("httpd","mod_ssl","php","php-mysql")
    $ServiceName = "httpd"
    $VHostDir = "/etc/httpd/conf.d"
    $PackageManager = "yum"
}else{
    Write-Error "Unknown Linux operating system. Cannot continue."
}

#Get Service Controller
if ((Test-Path "/bin/systemctl") -or (Test-Path "/usr/bin/systemctl")){
    $ServiceCtl = "SystemD"
}else{
    $ServiceCtl = "init"
}

#Get FQDN
$hostname = & hostname --fqdn

Write-Host -ForegroundColor Blue "Compile a DSC MOF for the Apache Server configuration"
Configuration ApacheServer{
    Node localhost{

        ForEach ($Package in $ApachePackages){
            nxPackage $Package{
                Ensure = "Present"
                Name = $Package
                PackageManager = $PackageManager
            }
        }

        nxFile vHostDirectory{
            DestinationPath = $VhostDir
            Type = "Directory"
            Ensure = "Present"
            Owner = "root"
            Mode = "744"
        }

        #Ensure default content does not exist
        nxFile DefVHost{
            DestinationPath = "${VhostDir}/000-default.conf"
            Ensure = "Absent"
        }

        nxFile Welcome.conf{
            DestinationPath = "${VhostDir}/welcome.conf"
            Ensure = "Absent"
        }

        nxFile UserDir.conf{
            DestinationPath = "${VhostDir}/userdir.conf"
            Ensure = "Absent"
        }

        #Ensure website is defined
        nxFile DefaultSiteDir{
            DestinationPath = "/var/www/html/defaultsite"
            Type = "Directory"
            Owner = "root"
            Mode = "744"
            Ensure = "Present"
        }

        nxFile DefaultSite.conf{
            Destinationpath = "${VhostDir}/defaultsite.conf"
            Owner = "root"
            Mode = "744"
            Ensure = "Present"
            Contents = @"
<VirtualHost *:80>
DocumentRoot /var/www/html/defaultsite
ServerName $hostname
</VirtualHost>

"@
            DependsOn = "[nxFile]DefaultSiteDir"
        }

        nxFile TestPhp{
            DestinationPath = "/var/www/html/defaultsite/test.php"
            Ensure = "Present"
            Owner = "root"
            Mode = "744"
            Contents = @'
<?php phpinfo(); ?>

'@
        }

        #Configure Apache Service
        nxService ApacheService{
            Name = "$ServiceName"
            Enabled = $true
            State = "running"
            Controller = $ServiceCtl
            DependsOn = "[nxFile]DefaultSite.conf"
        }

    }
}

ApacheServer -OutputPath "/tmp"

Pause
Write-Host -ForegroundColor Blue "Apply the configuration locally"
& sudo /opt/microsoft/dsc/Scripts/StartDscConfiguration.py -configurationmof /tmp/localhost.mof | Out-Host

Pause
Write-Host -ForegroundColor Blue "Get the current configuration"
& sudo /opt/microsoft/dsc/Scripts/GetDscConfiguration.py | Out-Host
