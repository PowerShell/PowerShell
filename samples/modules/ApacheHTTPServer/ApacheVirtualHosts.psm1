$moduleRoot = Split-Path -Path $MyInvocation.MyCommand.Path


Function New-ApacheVHost {
    [CmdletBinding()]
    param (
        [parameter (Mandatory = $true)][string]$ServerName,
        [string]$VHostsDirectory,
        [parameter (Mandatory = $true)][string]$DocumentRoot,
         [parameter (Mandatory = $true)][string]$VirtualHostIPAddress,
        [string[]]$ServerAliases,
         [parameter (Mandatory = $true)][string]$VirtualHostPort,
        [string]$ServerAdmin,
        [string]$CustomLogPath,
        [string]$ErrorLogPath
        )

        if ($VHostsDirectory -notlike "*/*"){
            #Try to find default
            if (Test-Path "/etc/httpd/conf.d"){
                $VHostsDirectory = "/etc/httpd/conf.d/"
            }
            if (Test-Path "/etc/apache2/sites-enabled"){
                $VHostsDirectory = "/etc/apache2/sites-enabled"
            }
        }

        if (!(Test-Path $VHostsDirectory)){
            Write-Error "Specified virtual hosts directory does not exist: $VHostsDirectory"
            exit 1
        }

        $vHostDef = "<VirtualHost " 
        if ($VirtualHostIPAddress -ne $null){
            $vHostDef += $VirtualHostIPAddress
        }
        if ($VirtualHostPort -ne $null){
            $vHostDef += ":$VirtualHostPort"
        }
        $vHostDef += ">"

        $vHostDef += "`n"
        $vHostDef += "DocumentRoot " + $DocumentRoot + "`n"
        $vHostDef += "ServerName " + $ServerName + "`n"
        if ($ServerAliases -ne $null){
            ForEach ($Alias in $ServerAliases){
                $vHostDef += "ServerAlias " + $Alias + "`n"
            }
        }
        if ($ServerAdmin.Length -gt 1){
            $vHostDef += "ServerAdmin " + $ServerAdmin +"`n"
        }
        if ($CustomLogPath -like "*/*"){
            $vHostDef += "CustomLog " + $CustomLogPath +"`n"
        }
        if ($ErrorLogPath -like "*/*"){
            $vHostDef += "ErrorLog " + $ErrorLogpath +"`n"
        }
        $vHostDef += "</VirtualHost>"
        $filName = $ServerName + ".conf"
        Write-Output "Writing $filName to $VHostsDirectory"
        $VhostDef |out-file "$VHostsDirectory/$filName" -Force -Encoding:ascii
        
        if (Test-Path "/usr/sbin/apache2ctl"){
            $cmd = "/usr/sbin/apache2ctl"
        }elseif(Test-Path "/usr/sbin/httpd"){
            $cmd = "/usr/sbin/httpd"
        }
        $cmd += " -k restart"
        write-output "Restarting Apache HTTP daemon"
        Invoke-expression $cmd
}

Function Get-ApacheVHosts{
    $cmd = $null
    if (Test-Path "/usr/sbin/apache2ctl"){
        $cmd = "/usr/sbin/apache2ctl"
    }elseif(Test-Path "/usr/sbin/httpd"){
        $cmd = "/usr/sbin/httpd"
    }


    function New-VHostObj {
        New-Object PSObject -Property @{}
    }



    if ($cmd -eq $null){
        Write-Error "Unable to find httpd/apache2ctl command"
        exit -1
    }

    $cmd  = $cmd += " -t -D DUMP_VHOSTS"
    $Vhosts = @()
    $res = Invoke-expression $cmd
    ForEach ($line in $res){         
        if ($line -like "*.conf*"){
           $vhobject = New-VHostObj
           $RMatch = $line -match "(?<Listen>.*:[0-9]*)(?<ServerName>.*)\((?<ConfFile>.*)\)"
           $VHostProps = $matches
           $vhobject |Add-Member -type NoteProperty -name "ServerName" -value $matches.ServerName.Trim()
           $vhobject |Add-Member -type NoteProperty -name "ListenAddress" -value $matches.Listen.Trim()
           $vhobject |Add-Member -type NoteProperty -name "ConfFile" -value $matches.ConfFile.Trim()
           $Vhosts += $vhobject
        }
    
    }
   
    Return $Vhosts

}

Function Restart-ApacheHTTPServer{
  [CmdletBinding()]
  Param(
   [boolean]$Graceful
   )
  
    if ($Graceful -eq $null){$Graceful = $fase}
	
    $cmd = $null
    if (Test-Path "/usr/sbin/apache2ctl"){
        $cmd = "/usr/sbin/apache2ctl"
    }elseif(Test-Path "/usr/sbin/httpd"){
        $cmd = "/usr/sbin/httpd"
    }
	
	if ($cmd -eq $null){
        Write-Error "Unable to find httpd/apache2ctl command"
        exit -1
    }
	
	if ($Graceful){
		$cmd = $cmd += " -k graceful"
	}else{
		$cmd = $cmd += " -k restart"
	}
	
	Invoke-Expression $Cmd
}

Function Get-ApacheModules{
    $cmd = $null
    if (Test-Path "/usr/sbin/apache2ctl"){
        $cmd = "/usr/sbin/apache2ctl"
    }elseif(Test-Path "/usr/sbin/httpd"){
        $cmd = "/usr/sbin/httpd"
    }
	
	if ($cmd -eq $null){
        Write-Error "Unable to find httpd/apache2ctl command"
        exit -1
    }
	
	$cmd = $cmd += " -M |grep -v Loaded"
	
	$ApacheModules = @()
	function New-ModuleObj {
        New-Object PSObject -Property @{}
    }


	$Results = Invoke-Expression $cmd
	Foreach ($mod in $Results){
	  $modObj = New-ModuleObj
	  $modObj | Add-Member -type NoteProperty -name "Module" -value $mod.trim()
	  $ApacheModules += $modObj
	} 

	Return $ApacheModules
	
}
