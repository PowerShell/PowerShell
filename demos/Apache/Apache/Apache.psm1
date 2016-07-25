
#Region utility functions
Function GetApacheCmd{
    if (Test-Path "/usr/sbin/apache2ctl"){
        $cmd = "/usr/sbin/apache2ctl"
    }elseif(Test-Path "/usr/sbin/httpd"){
        $cmd = "/usr/sbin/httpd"
    }else{
        Write-Error "Unable to find httpd or apache2ctl program. Unable to continue"
        exit -1
    }
    $cmd
}

Function GetApacheVHostDir{
    if (Test-Path "/etc/httpd/conf.d"){
        $VHostsDirectory = "/etc/httpd/conf.d/"
    }
    if (Test-Path "/etc/apache2/sites-enabled"){
        $VHostsDirectory = "/etc/apache2/sites-enabled"
    }else{
        $VHostsDirectory = $null
    }
    $VHostsDirectory
}

Function CleanInputString([string]$inputStr){
    $outputStr = $inputStr.Trim().Replace('`n','').Replace('\n','')
    $outputStr
}

#EndRegion utilty functions

#Region Class specifications


Class ApacheVirtualHost{
        [string]$ServerName
        [string]$DocumentRoot
        [string]$VirtualHostIPAddress = "*"
        [string[]]$ServerAliases
        [string]$VirtualHostPort = "80"
        [string]$ServerAdmin
        [string]$CustomLogPath
        [string]$ErrorLogPath   
        [string]$ConfigurationFile

        #region class constructors
        #endregion

        #region class methods
        Save($ConfigurationFile){
            if (!(Test-Path $this.DocumentRoot)){ New-Item -type Directory $this.DocumentRoot }

            $VHostsDirectory = GetApacheVHostDir
            if (!(Test-Path $VHostsDirectory)){
                Write-Error "Specified virtual hosts directory does not exist: $VHostsDirectory"
                exit 1
            }
            $vHostDef = "<VirtualHost $this.VirtualHostIPAddress:$this.VirtualHostPort >`n"
            $vHostDef += "DocumentRoot " + $this.DocumentRoot + "`n"
            ForEach ($Alias in $this.ServerAliases){
                $vHostDef += "ServerAlias " + $Alias + "`n"
            }
            if ($this.ServerAdmin.Length -gt 1){$vHostDef += "ServerAdmin " + $this.ServerAdmin +"`n"}
            if ($this.CustomLogPath -like "*/*"){$vHostDef += "CustomLog " + $this.CustomLogPath +"`n"}
            if ($this.ErrorLogPath -like "*/*"){$vHostDef += "ErrorLog " + $this.ErrorLogpath +"`n"}
            $vHostDef += "</VirtualHost>"
            $filName = $ConfigurationFile
            $VhostDef |out-file "${VHostsDirectory}/${filName}" -Force -Encoding:ascii
            Write-Information "Restarting Apache HTTP Server"
            Restart-ApacheHTTPServer
        }

        #endregion
}

#EndRegion Class Specifications

Function New-ApacheVHost {
    [CmdletBinding()]
    param (
        [parameter (Mandatory = $true)][string]$ServerName,
        [parameter (Mandatory = $true)][string]$DocumentRoot,
        [string]$VirtualHostIPAddress,
        [string[]]$ServerAliases,
        [string]$VirtualHostPort,
        [string]$ServerAdmin,
        [string]$CustomLogPath,
        [string]$ErrorLogPath
        )

        $newVHost = [ApacheVirtualHost]::new()
        $newVHost.ServerName = $ServerName
        $newVHost.DocumentRoot = $DocumentRoot
        $newVHost.ServerAliases = $ServerAliases
        if ($VirtualHostIPAddress){$newVHost.VirtualHostIPAddress = $VirtualHostIPAddress}
        if ($VirtualHostPort){$newVHost.VirtualHostPort = $VirtualHostPort}
        $newVHost.ServerAdmin = $ServerAdmin
        $newVHost.CustomLogPath = $CustomLogPath
        $newVHost.ErrorLogPath = $ErrorLogPath
        $newVHost.Save("$ServerName.conf")        
}

Function Get-ApacheVHost{
    $cmd = GetApacheCmd
   
    $Vhosts = @()
    $res = & $cmd -t -D DUMP_VHOSTS

    ForEach ($line in $res){
        $ServerName = $null
        if ($line -like "*:*.conf*"){
            #$vhobject = New-VHostObj
            $RMatch = $line -match "(?<Listen>.*:[0-9]*)(?<ServerName>.*)\((?<ConfFile>.*)\)"
            $ListenAddress = $Matches.Listen.trim()
            $ServerName = $Matches.ServerName.trim()
            $ConfFile = $Matches.ConfFile.trim()
        }else{
            if ($line.trim().split()[0] -like "*:*"){
                $ListenAddress = $line.trim().split()[0]
            }elseif($line -like "*.conf*"){
                if ($line -like "*default*"){
                    $ServerName = "_Default"
                    $ConfFile = $line.trim().split()[3].split(":")[0].Replace('(','')
                }elseif($line -like "*namevhost*"){
                    $ServerName = $line.trim().split()[3]
                    $ConfFile = $line.trim().split()[4].split(":")[0].Replace('(','')
                }
            }
        }

        if ($ServerName -ne $null){
            $vHost = [ApacheVirtualHost]::New
            $vHost.ServerName = $ServerName
            $vHost.ConfFile = $ConfFile
            $vHost.VirtualHostIPAddress = $ListenAddress.Split(":")[0]
            $vHost.VirtualHostPort = $ListenAddress.Split(":")[1]
            $Vhosts += $vHost
            }
        }
    Return $Vhosts
    }

Function Restart-ApacheHTTPServer{
  [CmdletBinding()]
  Param(
   [switch]$Graceful
   )
  
    if ($Graceful -eq $null){$Graceful = $fase}
    $cmd = GetApacheCmd
	if ($Graceful){
		& $cmd  -k graceful
	}else{
		& $cmd  -k restart
	}

}


