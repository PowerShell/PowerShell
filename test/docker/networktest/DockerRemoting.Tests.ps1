# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
$imageName = "remotetestimage"
Describe "Basic remoting test with docker" -tags @("Scenario","Slow"){
    BeforeAll {
        $Timeout = 600 # 10 minutes to run these tests
        $dockerimage = docker images --format "{{ .Repository }}" $imageName
        if ( $dockerimage -ne $imageName ) {
            $pending = $true
            write-warning "Docker image '$imageName' not found, not running tests"
            return
        }
        else {
            $pending = $false
        }

        # give the containers something to do, otherwise they will exit and be removed
        Write-Verbose -verbose "setting up docker container PowerShell server"
        $server = docker run -d $imageName powershell -c Start-Sleep -Seconds $timeout
        Write-Verbose -verbose "setting up docker container PowerShell client"
        $client = docker run -d $imageName powershell -c Start-Sleep -Seconds $timeout

        # get fullpath to installed core powershell
        Write-Verbose -verbose "Getting path to PowerShell core"
        $powershellcorepath = docker exec $server powershell -c "(get-childitem 'c:\program files\powershell\*\pwsh.exe').fullname"
        if ( ! $powershellcorepath )
        {
            $pending = $true
            write-warning "Cannot find powershell core executable, not running tests"
            return
        }
        $powershellcoreversion = ($powershellcorepath -split "[\\/]")[-2]
        # we will need the configuration of the core powershell endpoint
        $powershellcoreConfiguration = "powershell.${powershellcoreversion}"

        # capture the hostnames of the containers which will be used by the tests
        write-verbose -verbose "getting server hostname"
        $serverhostname = docker exec $server hostname
        write-verbose -verbose "getting client hostname"
        $clienthostname = docker exec $client hostname

        # capture the versions of full and core PowerShell
        write-verbose -verbose "getting powershell full version"
        $fullVersion = docker exec $client powershell -c "`$psversiontable.psversion.tostring()"
        if ( ! $fullVersion )
        {
            $pending = $true
            write-warning "Cannot determine PowerShell full version, not running tests"
            return
        }

        write-verbose -verbose "getting powershell core version"
        $coreVersion = docker exec $client "$powershellcorepath" -c "`$psversiontable.psversion.tostring()"
        if ( ! $coreVersion )
        {
            $pending = $true
            write-warning "Cannot determine PowerShell core version, not running tests"
            return
        }
    }

    AfterAll {
        # to debug, comment out the following
        if ( $pending -eq $false ) {
            docker rm -f $server
            docker rm -f $client
        }
    }

    It "Full powershell can get correct remote powershell core version" -pending:$pending {
        $result = docker exec $client powershell -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | ForEach-Object { `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -configurationname $powershellcoreConfiguration -auth basic -credential `$c; invoke-command -session `$ses { `$psversiontable.psversion.tostring() }"
        $result | should be $coreVersion
    }

    It "Full powershell can get correct remote powershell full version" -pending:$pending {
        $result = docker exec $client powershell -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | ForEach-Object { `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -auth basic -credential `$c; invoke-command -session `$ses { `$psversiontable.psversion.tostring() }"
        $result | should be $fullVersion
    }

    It "Core powershell can get correct remote powershell core version" -pending:$pending {
        $result = docker exec $client "$powershellcorepath" -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | ForEach-Object { `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -configurationname $powershellcoreConfiguration -auth basic -credential `$c; invoke-command -session `$ses { `$psversiontable.psversion.tostring() }"
        $result | should be $coreVersion
    }

    It "Core powershell can get correct remote powershell full version" -pending:$pending {
        $result = docker exec $client "$powershellcorepath" -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | ForEach-Object { `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -auth basic -credential `$c; invoke-command -session `$ses { `$psversiontable.psversion.tostring() }"
        $result | should be $fullVersion
    }
}
