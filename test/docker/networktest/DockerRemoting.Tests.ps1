$imageName = "remotetestimage"
Describe "Basic remoting test with docker" -tags @("Scenario","Slow"){
    BeforeAll {
        $dockerimage = docker images --format "{{ .Repository }}" $imageName
        if ( $dockerimage -ne $imageName ) {
            $pending = $true
            write-warning "Docker image '$imageName' not found, not running tests"
            return
        }
        else {
            $pending = $false
        }
        # this is all I could figure out how to set up a container which
        # sticks around for a while
        Write-Verbose -verbose "setting up docker container PowerShell server"
        $server = docker run -d remotetest1 powershell -c start-sleep 600
        Write-Verbose -verbose "setting up docker container PowerShell client"
        $client = docker run -d remotetest1 powershell -c start-sleep 600

        # capture some data
        write-verbose -verbose "getting server hostname"
        $serverhostname = docker exec $server hostname
        write-verbose -verbose "getting client hostname"
        $clienthostname = docker exec $client hostname

        write-verbose -verbose "getting powershell full version"
        $fullVersion = docker exec $client powershell -c "`$psversiontable.psversion.tostring()"

        write-verbose -verbose "getting powershell core version"
        $coreVersion = docker exec $client "C:\program files\powershell\6.0.0.17\powershell" -c "`$psversiontable.psversion.tostring()"
    }

    AfterAll {
        # to debug, comment out the following
        if ( $pending -eq $false ) {
            docker rm -f $server
            docker rm -f $client
        }
    }

    It "Full powershell can get correct remote powershell core version" -pending:$pending {
        $result = docker exec $client powershell -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | %{ `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -configurationname powershell.6.0.0-alpha.17 -auth basic -credential `$c; invoke-command -session `$ses { `$psversiontable.psversion.tostring() }"
        $result | should be $coreVersion
    }

    It "Full powershell can get correct remote powershell full version" -pending:$pending {
        $result = docker exec $client powershell -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | %{ `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -auth basic -credential `$c; invoke-command -session `$ses { `$psversiontable.psversion.tostring() }"
        $result | should be $fullVersion
    }

    It "Core powershell can get correct remote powershell core version" -pending:$pending {
        $result = docker exec $client "C:\program files\powershell\6.0.0.17\powershell" -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | %{ `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -configurationname powershell.6.0.0-alpha.17 -auth basic -credential `$c; invoke-command -session `$ses { `$psversiontable.psversion.tostring() }"
        $result | should be $coreVersion
    }

    It "Core powershell can get correct remote powershell full version" -pending:$pending {
        $result = docker exec $client "C:\program files\powershell\6.0.0.17\powershell" -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | %{ `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -auth basic -credential `$c; invoke-command -session `$ses { `$psversiontable.psversion.tostring() }"
        $result | should be $fullVersion
    }
}
