# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "SSHRemoting Basic Tests" -tags CI {

    # SSH remoting is set up to automatically authenticate current user via SSH keys
    # All tests connect back to localhost machine

    $script:TestConnectingTimeout = 5000    # Milliseconds

    function CheckSSHDService
    {
        if ($IsWindows)
        {
            Write-Verbose -Verbose "Restarting Windows SSHD service..."
            Restart-Service sshd
            Write-Verbose -Verbose "SSHD service status: $(Get-Service sshd | Out-String)"
        }
        else
        {
            Write-Verbose -Verbose "Restarting Linux SSHD service..."
            sudo service ssh restart
            $status = sudo service ssh status
            Write-Verbose -Verbose "SSHD service status: $status"
        }
    }

    function TryNewPSSession
    {
        param(
            [string[]] $HostName,
            [string[]] $Name,
            [int] $Port,
            [string] $UserName,
            [string] $KeyFilePath,
            [string] $Subsystem
        )

        # Try creating a new SSH connection
        $timeout = $script:TestConnectingTimeout
        $connectionError = $null
        $session = $null
        $count = 0
        while (($null -eq $session) -and ($count++ -lt 2))
        {
            $session = New-PSSession @PSBoundParameters -ConnectingTimeout $timeout -ErrorVariable connectionError -ErrorAction SilentlyContinue
            if ($null -eq $session)
            {
                Write-Verbose -Verbose "SSH New-PSSession remoting connect failed after $($timeout/1000) second wait."

                if ($count -eq 1)
                {
                    # Try restarting sshd service
                    CheckSSHDService
                }
            }
        }

        if ($null -eq $session)
        {
            $message = "New-PSSession unable to connect to SSH remoting endpoint after two attempts. Error: $($connectionError.Exception.Message)"
            throw [System.Management.Automation.PSInvalidOperationException]::new($message)
        }

        Write-Verbose -Verbose "SSH New-PSSession remoting connect succeeded."
        Write-Output $session
    }

    function TryNewPSSessionHash
    {
        param (
            [hashtable[]] $SSHConnection,
            [string[]] $Name
        )

        foreach ($connect in $SSHConnection)
        {
            $connect.Add('ConnectingTimeout', $script:TestConnectingTimeout)
        }

        # Try creating a new SSH connection
        $connectionError = $null
        $session = $null
        $count = 0
        while (($null -eq $session) -and ($count++ -lt 2))
        {
            $session = New-PSSession @PSBoundParameters -ErrorVariable connectionError -ErrorAction SilentlyContinue
            if ($null -eq $session)
            {
                Write-Verbose -Verbose "SSH New-PSSession remoting connect failed after $($timeout/1000) second wait."

                if ($count -eq 1)
                {
                    # Try restarting sshd service
                    CheckSSHDService
                }
            }
        }

        if ($null -eq $session)
        {
            $message = "New-PSSession unable to connect to SSH remoting endpoint after two attempts. Error: $($connectionError.Exception.Message)"
            throw [System.Management.Automation.PSInvalidOperationException]::new($message)
        }

        Write-Verbose -Verbose "SSH New-PSSession remoting connect succeeded."
        Write-Output $session
    }

    function VerifySession {
        param (
            [System.Management.Automation.Runspaces.PSSession] $session
        )

        $session.State | Should -BeExactly 'Opened'
        $session.ComputerName | Should -BeExactly 'localhost'
        $session.Transport | Should -BeExactly 'SSH'
        Invoke-Command -Session $session -ScriptBlock { whoami } | Should -BeExactly $(whoami)
        $psRemoteVersion = Invoke-Command -Session $session -ScriptBlock { $PSSenderInfo.ApplicationArguments.PSVersionTable.PSVersion }
        $psRemoteVersion.Major | Should -BeExactly $PSVersionTable.PSVersion.Major
        $psRemoteVersion.Minor | Should -BeExactly $PSVersionTable.PSVersion.Minor
    }

    Context "New-PSSession Tests" {

        AfterEach {
            if ($script:session -ne $null) { Remove-PSSession -Session $script:session }
            if ($script:sessions -ne $null) { Remove-PSSession -Session $script:sessions }
        }

        It "Verifies new connection with implicit current User" {
            $script:session = TryNewPSSession -HostName localhost
            VerifySession $script:session
        }

        It "Verifies new connection with explicit User parameter" {
            $script:session = TryNewPSSession -HostName localhost -UserName (whoami)
            VerifySession $script:session
        }

        It "Verifies explicit Name parameter" {
            $sessionName = 'TestSessionNameA'
            $script:session = TryNewPSSession -HostName localhost -Name $sessionName
            VerifySession $script:session
            $script:session.Name | Should -BeExactly $sessionName
        }

        It "Verifies explicit Port parameter" {
            $portNum = 22
            $script:session = TryNewPSSession -HostName localhost -Port $portNum
            VerifySession $script:session
        }

        It "Verifies explicit Subsystem parameter" {
            $portNum = 22
            $subSystem = 'powershell'
            $script:session = TryNewPSSession -HostName localhost -Port $portNum -SubSystem $subSystem
            VerifySession $script:session
        }

        It "Verifies explicit KeyFilePath parameter" {
            $keyFilePath = "$HOME/.ssh/id_rsa"
            $portNum = 22
            $subSystem = 'powershell'
            $script:session = TryNewPSSession -HostName localhost -Port $portNum -SubSystem $subSystem -KeyFilePath $keyFilePath
            VerifySession $script:session
        }

        It "Verifies SSHConnection hash table parameters" {
            $sshConnection = @(
            @{
                HostName = 'localhost'
                UserName = whoami
                Port = 22
                KeyFilePath = "$HOME/.ssh/id_rsa"
                Subsystem = 'powershell'
            },
            @{
                HostName = 'localhost'
                KeyFilePath = "$HOME/.ssh/id_rsa"
                Subsystem = 'powershell'
            })
            $script:sessions = TryNewPSSessionHash -SSHConnection $sshConnection -Name 'Connection1','Connection2'
            $script:sessions | Should -HaveCount 2
            $script:sessions[0].Name | Should -BeLike 'Connection*'
            $script:sessions[1].Name | Should -BeLike 'Connection*'
            VerifySession $script:sessions[0]
            VerifySession $script:sessions[1]
        }
    }

    function TryCreateRunspace
    {
        param (
            [string] $UserName,
            [string] $ComputerName,
            [string] $KeyFilePath,
            [int] $Port,
            [string] $Subsystem
        )

        $timeout = $script:TestConnectingTimeout
        $connectionError = $null
        $count = 0
        $rs = $null
        $ci = [System.Management.Automation.Runspaces.SSHConnectionInfo]::new($UserName, $ComputerName, $KeyFilePath, $Port, $Subsystem, $timeout)
        while (($null -eq $rs) -and ($count++ -lt 2))
        {
            try
            {
                $rs = [runspacefactory]::CreateRunspace($host, $ci)
                $null = $rs.Open()
            }
            catch
            {
                $connectionError = $_
                $rs = $null
                Write-Verbose -Verbose "SSH Runspace Open remoting connect failed after $($timeout/1000) second wait."

                if ($count -eq 1)
                {
                    # Try restarting sshd service
                    CheckSSHDService
                }
            }
        }

        if ($null -eq $rs)
        {
            $message = "Runspace open unable to connect to SSH remoting endpoint after three attempts. Error: $($connectionError.Message)"
            throw [System.Management.Automation.PSInvalidOperationException]::new($message)
        }

        Write-Verbose -Verbose "SSH Runspace Open remoting connect succeeded."
        Write-Output $rs
    }

    function VerifyRunspace {
        param (
            [runspace] $rs
        )

        $rs.RunspaceStateInfo.State | Should -BeExactly 'Opened'
        $rs.RunspaceAvailability | Should -BeExactly 'Available'
        $rs.RunspaceIsRemote | Should -BeTrue
        $ps = [powershell]::Create()
        try
        {
            $ps.Runspace = $rs
            $psRemoteVersion = $ps.AddScript('$PSSenderInfo.ApplicationArguments.PSVersionTable.PSVersion').Invoke()
            $psRemoteVersion.Major | Should -BeExactly $PSVersionTable.PSVersion.Major
            $psRemoteVersion.Minor | Should -BeExactly $PSVersionTable.PSVersion.Minor

            $ps.Commands.Clear()
            $ps.AddScript('whoami').Invoke() | Should -BeExactly $(whoami)
        }
        finally
        {
            $ps.Dispose()
        }
    }

    Context "SSH Remoting API Tests" {

        AfterEach {
            if ($script:rs -ne $null) { $script:rs.Dispose() }
        }

        $testCases = @(
            @{
                testName = 'Verifies connection with implicit user'
                UserName = $null
                ComputerName = 'localhost'
                KeyFilePath = $null
                Port = 0
                Subsystem = $null
            },
            @{
                testName = 'Verifies connection with UserName'
                UserName = whoami
                ComputerName = 'localhost'
                KeyFilePath = $null
                Port = 0
                Subsystem = $null
            },
            @{
                testName = 'Verifies connection with KeyFilePath'
                UserName = whoami
                ComputerName = 'localhost'
                KeyFilePath = "$HOME/.ssh/id_rsa"
                Port = 0
                Subsystem = $null
            },
            @{
                testName = 'Verifies connection with Port specified'
                UserName = whoami
                ComputerName = 'localhost'
                KeyFilePath = "$HOME/.ssh/id_rsa"
                Port = 22
                Subsystem = $null
            },
            @{
                testName = 'Verifies connection with Subsystem specified'
                UserName = whoami
                ComputerName = 'localhost'
                KeyFilePath = "$HOME/.ssh/id_rsa"
                Port = 22
                Subsystem = 'powershell'
            }
        )

        It "<testName>" -TestCases $testCases {
            param (
                $UserName,
                $ComputerName,
                $KeyFilePath,
                $Port,
                $SubSystem
            )

            $script:rs = TryCreateRunspace -UserName $UserName -ComputerName $ComputerName -KeyFilePath $KeyFilePath -Port $Port -Subsystem $Subsystem
            VerifyRunspace $script:rs
        }
    }
}
