# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#Region utility functions

$global:sudocmd = "sudo"

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
        Return "/etc/httpd/conf.d/"
    }
    if (Test-Path "/etc/apache2/sites-enabled"){
        Return "/etc/apache2/sites-enabled"
    }
}

Function CleanInputString([string]$inputStr){
    $outputStr = $inputStr.Trim().Replace('`n','').Replace('\n','')
    $outputStr
}

#EndRegion utility functions

#Region Class specifications

Class ApacheModule{
        [string]$ModuleName

        ApacheModule([string]$aModule){
            $this.ModuleName = $aModule
        }
}

Class ApacheVirtualHost{
        [string]$ServerName
        [string]$DocumentRoot
        [string]$VirtualHostIPAddress = "*"
        [string[]]$ServerAliases
        [int]$VirtualHostPort = "80"
        [string]$ServerAdmin
        [string]$CustomLogPath
        [string]$ErrorLogPath
        [string]$ConfigurationFile

        #region class constructors
        ApacheVirtualHost([string]$ServerName, [string]$ConfFile, [string]$VirtualHostIPAddress,[int]$VirtualHostPort){
            $this.ServerName = $ServerName
            $this.ConfigurationFile = $ConfFile
            $this.VirtualHostIPAddress = $VirtualHostIPAddress
            $this.VirtualHostPort = $VirtualHostPort
        }

        #Full specification
        ApacheVirtualHost([string]$ServerName, [string]$DocumentRoot, [string[]]$ServerAliases, [string]$ServerAdmin, [string]$CustomLogPath, [string]$ErrorLogPath, [string]$VirtualHostIPAddress, [int]$VirtualHostPort, [string]$ConfigurationFile){
            $this.ServerName = $ServerName
            $this.DocumentRoot = $DocumentRoot
            $this.ServerAliases = $ServerAliases
            $this.ServerAdmin = $ServerAdmin
            $this.CustomLogPath = $CustomLogPath
            $this.ErrorLogPath = $ErrorLogPath
            $this.VirtualHostIPAddress = $VirtualHostIPAddress
            $this.VirtualHostPort = $VirtualHostPort
            $this.ConfigurationFile = $ConfigurationFile
        }

        #Default Port and IP
        #endregion

        #region class methods
        Save($ConfigurationFile){
            if (!(Test-Path $this.DocumentRoot)){ New-Item -Type Directory $this.DocumentRoot }

            $VHostsDirectory = GetApacheVHostDir
            if (!(Test-Path $VHostsDirectory)){
                Write-Error "Specified virtual hosts directory does not exist: $VHostsDirectory"
                exit 1
            }
            $VHostIPAddress = $this.VirtualHostIPAddress
            [string]$VhostPort = $this.VirtualHostPort
            $VHostDef = "<VirtualHost " + "$VHostIPAddress" + ":" + $VHostPort + " >`n"
            $vHostDef += "DocumentRoot " + $this.DocumentRoot + "`n"
            ForEach ($Alias in $this.ServerAliases){
                if ($Alias.trim() -ne ""){
                    $vHostDef += "ServerAlias " + $Alias + "`n"
                }
            }
            $vHostDef += "ServerName " + $this.ServerName +"`n"
            if ($this.ServerAdmin.Length -gt 1){$vHostDef += "ServerAdmin " + $this.ServerAdmin +"`n"}
            if ($this.CustomLogPath -like "*/*"){$vHostDef += "CustomLog " + $this.CustomLogPath +"`n"}
            if ($this.ErrorLogPath -like "*/*"){$vHostDef += "ErrorLog " + $this.ErrorLogpath +"`n"}
            $vHostDef += "</VirtualHost>"
            $filName = $ConfigurationFile
            $VhostDef | Out-File "/tmp/${filName}" -Force -Encoding:ascii
            & $global:sudocmd "mv" "/tmp/${filName}" "${VhostsDirectory}/${filName}"
            Write-Information "Restarting Apache HTTP Server"
            Restart-ApacheHTTPServer
        }

        #endregion
}

#EndRegion Class Specifications

Function New-ApacheVHost {
    [CmdletBinding()]
    param(
        [parameter (Mandatory = $true)][string]$ServerName,
        [parameter (Mandatory = $true)][string]$DocumentRoot,
        [string]$VirtualHostIPAddress,
        [string[]]$ServerAliases,
        [int]$VirtualHostPort,
        [string]$ServerAdmin,
        [string]$CustomLogPath,
        [string]$ErrorLogPath
        )

        $NewConfFile = $VHostsDirectory + "/" + $ServerName + ".conf"
        if(!($VirtualHostIPAddress)){$VirtualHostIPAddress = "*"}
        if(!($VirtualHostPort)){$VirtualHostPort = "80"}
        $newVHost = [ApacheVirtualHost]::new("$ServerName","$DocumentRoot","$ServerAliases","$ServerAdmin","$CustomLogPath","$ErrorLogPath","$VirtualHostIPAddress",$VirtualHostPort,"$NewConfFile")
        $newVHost.Save("$ServerName.conf")
}

Function GetVHostProps([string]$ConfFile,[string]$ServerName,[string]$Listener){
    $confContents = Get-Content $ConfFile
    [boolean]$Match = $false
    $DocumentRoot = ""
    $CustomLogPath = ""
    $ErrorLogPath = ""
    $ServerAdmin = ""
    ForEach ($confline in $confContents){
        if ($confLine -like "<VirtualHost*${Listener}*"){
            $Match = $true
        }
        if($Match){
            Switch -wildcard  ($confline) {
                "*DocumentRoot*"{$DocumentRoot = $confline.split()[1].trim()}
                "*CustomLog*"{$CustomLogPath = $confline.split()[1].trim()}
                "*ErrorLog*"{$ErrorLogPath = $confline.split()[1].trim()}
                "*ServerAdmin*"{$ServerAdmin = $confline.split()[1].trim()}
               #Todo: Server aliases
            }
            if($confline -like "*</VirtualHost>*"){
                $Match = $false
            }
        }
    }
    @{"DocumentRoot" = "$DocumentRoot"; "CustomLogPath" = "$CustomLogPath"; "ErrorLogPath" = "$ErrorLogPath"; "ServerAdmin" = $ServerAdmin}

}

Function Get-ApacheVHost{
    $cmd = GetApacheCmd

    $Vhosts = @()
    $res = & $global:sudocmd $cmd -t -D DUMP_VHOSTS

    ForEach ($line in $res){
        $ServerName = $null
        if ($line -like "*:*.conf*"){
            $RMatch = $line -match "(?<Listen>.*:[0-9]*)(?<ServerName>.*)\((?<ConfFile>.*)\)"
            $ListenAddress = $Matches.Listen.trim()
            $ServerName = $Matches.ServerName.trim()
            $ConfFile = $Matches.ConfFile.trim().split(":")[0].Replace('(','')
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

        if ($null -ne $ServerName){
            $vHost = [ApacheVirtualHost]::New($ServerName, $ConfFile, $ListenAddress.Split(":")[0],$ListenAddress.Split(":")[1])
            $ExtProps = GetVHostProps $ConfFile $ServerName $ListenAddress
            $vHost.DocumentRoot = $ExtProps.DocumentRoot
            #Custom log requires additional handling. NYI
            #$vHost.CustomLogPath = $ExtProps.CustomLogPath
            $vHost.ErrorLogPath = $ExtProps.ErrorLogPath
            $vHost.ServerAdmin = $ExtProps.ServerAdmin
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

    if ($null -eq $Graceful){$Graceful = $false}
    $cmd = GetApacheCmd
        if ($Graceful){
                & $global:sudocmd $cmd  -k graceful
        }else{
                & $global:sudocmd $cmd  -k restart
        }

}

Function Get-ApacheModule{
    $cmd = GetApacheCmd

        $ApacheModules = @()

        $Results = & $global:sudocmd $cmd -M |grep -v Loaded

        Foreach ($mod in $Results){
        $modInst = [ApacheModule]::new($mod.trim())
        $ApacheModules += ($modInst)
        }

    $ApacheModules

}
