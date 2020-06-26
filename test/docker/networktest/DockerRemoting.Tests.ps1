# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
$imageName = "remotetestimage"
Describe "Basic remoting test with docker" -tags @("Scenario","Slow"){
    BeforeAll {
        $Timeout = 600 # 10 minutes to run these tests
        $dockerimage = docker images --format "{{ .Repository }}" $imageName
        if ( $dockerimage -ne $imageName ) {
            $pending = $true
            Write-Warning "Docker image '$imageName' not found, not running tests"
            return
        }
        else {
            $pending = $false
        }

        # give the containers something to do, otherwise they will exit and be removed
        Write-Verbose -Verbose "setting up docker container PowerShell server"
        $server = docker run -d $imageName powershell -c Start-Sleep -Seconds $timeout
        Write-Verbose -Verbose "setting up docker container PowerShell client"
        $client = docker run -d $imageName powershell -c Start-Sleep -Seconds $timeout

        # get fullpath to installed core powershell
        Write-Verbose -Verbose "Getting path to PowerShell"
        $powershellcorepath = docker exec $server powershell -c "(get-childitem 'c:\program files\powershell\*\pwsh.exe').fullname"
        if ( ! $powershellcorepath )
        {
            $pending = $true
            Write-Warning "Cannot find powershell executable, not running tests"
            return
        }
        $powershellcoreversion = ($powershellcorepath -split "[\\/]")[-2]
        # we will need the configuration of the core powershell endpoint
        $powershellcoreConfiguration = "powershell.${powershellcoreversion}"

        # capture the hostnames of the containers which will be used by the tests
        Write-Verbose -Verbose "getting server hostname"
        $serverhostname = docker exec $server hostname
        Write-Verbose -Verbose "getting client hostname"
        $clienthostname = docker exec $client hostname

        # capture the versions of full and core PowerShell
        Write-Verbose -Verbose "getting powershell full version"
        $fullVersion = docker exec $client powershell -c "`$PSVersionTable.psversion.tostring()"
        if ( ! $fullVersion )
        {
            $pending = $true
            Write-Warning "Cannot determine PowerShell full version, not running tests"
            return
        }

        Write-Verbose -Verbose "getting powershell version"
        $coreVersion = docker exec $client "$powershellcorepath" -c "`$PSVersionTable.psversion.tostring()"
        if ( ! $coreVersion )
        {
            $pending = $true
            Write-Warning "Cannot determine PowerShell version, not running tests"
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

    It "Full powershell can get correct remote powershell version" -Pending:$pending {
        $result = docker exec $client powershell -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | ForEach-Object { `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -configurationname $powershellcoreConfiguration -auth basic -credential `$c; invoke-command -session `$ses { `$PSVersionTable.psversion.tostring() }"
        $result | Should -Be $coreVersion
    }

    It "Full powershell can get correct remote powershell full version" -Pending:$pending {
        $result = docker exec $client powershell -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | ForEach-Object { `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -auth basic -credential `$c; invoke-command -session `$ses { `$PSVersionTable.psversion.tostring() }"
        $result | Should -Be $fullVersion
    }

    It "Core powershell can get correct remote powershell version" -Pending:$pending {
        $result = docker exec $client "$powershellcorepath" -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | ForEach-Object { `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -configurationname $powershellcoreConfiguration -auth basic -credential `$c; invoke-command -session `$ses { `$PSVersionTable.psversion.tostring() }"
        $result | Should -Be $coreVersion
    }

    It "Core powershell can get correct remote powershell full version" -Pending:$pending {
        $result = docker exec $client "$powershellcorepath" -c "`$ss = [security.securestring]::new(); '11aa!!AA'.ToCharArray() | ForEach-Object { `$ss.appendchar(`$_)}; `$c = [pscredential]::new('testuser',`$ss); `$ses=new-pssession $serverhostname -auth basic -credential `$c; invoke-command -session `$ses { `$PSVersionTable.psversion.tostring() }"
        $result | Should -Be $fullVersion
    }
}
