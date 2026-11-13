# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "SSHRemoting Basic Tests" -tags CI {

    # SSH remoting is set up to automatically authenticate current user via SSH keys
    # All tests connect back to localhost machine

    $script:TestConnectingTimeout = 5000    # Milliseconds

    # Shared SSH key path used by all tests. The helper module provisions an
    # rsa key pair for the current user on both Windows and Linux.
    $script:SshKeyFilePath = "$HOME/.ssh/id_rsa"

    # Platform-appropriate current username for SSH connections.
    # On Windows, $env:USERNAME is set; on Linux/macOS, use whoami.
    $script:CurrentUserName = if ($IsWindows) { $env:USERNAME } else { (whoami) }

    function RestartSSHDService
    {
        if ($IsWindows)
        {
            Write-Verbose -Verbose "Restarting Windows SSHD service..."
            Restart-Service sshd
            Write-Verbose -Verbose "SSHD service status: $(Get-Service sshd | Out-String)"
        }
        else
        {
            Write-Verbose -Verbose "Restarting Unix SSHD service..."
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

        Write-Verbose -Verbose "Starting TryNewPSSession ..."

        $attempt = NewPSSessionAttempt @PSBoundParameters

        if ($null -eq $attempt.Session)
        {
            $message = "New-PSSession unable to connect to SSH remoting endpoint after two attempts. Error: $($attempt.Error.Exception.Message)"
            throw [System.Management.Automation.PSInvalidOperationException]::new($message)
        }

        Write-Verbose -Verbose "SSH New-PSSession remoting connect succeeded."
        Write-Output $attempt.Session
    }

    function NewPSSessionAttempt
    {
        param(
            [string[]] $HostName,
            [string[]] $Name,
            [object] $Port,
            [string] $UserName,
            [string] $KeyFilePath,
            [string] $Subsystem,
            [switch] $SkipRetry
        )

        Write-Verbose -Verbose "Starting NewPSSessionAttempt ..."

        # Try creating a new SSH connection
        $timeout = $script:TestConnectingTimeout
        $newPSSessionParameters = @{} + $PSBoundParameters
        $null = $newPSSessionParameters.Remove('SkipRetry')
        $connectionError = $null
        $connectionException = $null
        $session = $null
        $count = 0
        $maximumRetryCount = if ($SkipRetry) { 1 } else { 2 }
        while (($null -eq $session) -and ($null -eq $connectionException) -and ($count++ -lt $maximumRetryCount))
        {
            try
            {
                $session = New-PSSession @newPSSessionParameters -ConnectingTimeout $timeout -ErrorVariable connectionError -ErrorAction SilentlyContinue
            }
            catch
            {
                $connectionException = $_
                $connectionError = $_
                Write-Verbose -Verbose "SSH New-PSSession remoting connect threw exception."
            }

            if (($null -eq $session) -and ($null -eq $connectionException))
            {
                Write-Verbose -Verbose "SSH New-PSSession remoting connect failed."

                if (($count -eq 1) -and -not $SkipRetry)
                {
                    # Try restarting sshd service
                    RestartSSHDService
                }
            }
        }

        [pscustomobject]@{
            Session = $session
            Error = if ($null -ne $connectionException) { $connectionException } else { $connectionError }
        }
    }

    function TryNewPSSessionHash
    {
        param (
            [hashtable[]] $SSHConnection,
            [string[]] $Name
        )

        Write-Verbose -Verbose "Starting TryNewPSSessionHash ..."

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
                Write-Verbose -Verbose "SSH New-PSSession remoting connect failed."

                if ($count -eq 1)
                {
                    # Try restarting sshd service
                    RestartSSHDService
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

        if ($null -eq $session)
        {
            return
        }

        Write-Verbose -Verbose "VerifySession called for session: $($session.Id)"

        $session.State | Should -BeExactly 'Opened'
        $session.ComputerName | Should -BeExactly 'localhost'
        $session.Transport | Should -BeExactly 'SSH'
        Write-Verbose -Verbose "Invoking whoami"
        Invoke-Command -Session $session -ScriptBlock { whoami } | Should -BeExactly $(whoami)
        Write-Verbose -Verbose "Invoking PSSenderInfo"
        $psRemoteVersion = Invoke-Command -Session $session -ScriptBlock { $PSSenderInfo.ApplicationArguments.PSVersionTable.PSVersion }
        $psRemoteVersion.Major | Should -BeExactly $PSVersionTable.PSVersion.Major
        $psRemoteVersion.Minor | Should -BeExactly $PSVersionTable.PSVersion.Minor
        Write-Verbose -Verbose "VerifySession complete"
    }

    Context "New-PSSession Tests" {

        AfterEach {
            Write-Verbose -Verbose "Starting New-PSSession AfterEach"
            if ($script:session -ne $null) { Remove-PSSession -Session $script:session }
            if ($script:sessions -ne $null) { Remove-PSSession -Session $script:sessions }
            Write-Verbose -Verbose "AfterEach complete"
        }

        It "Verifies new connection with implicit current User" {
            Write-Verbose -Verbose "It Starting: Verifies new connection with implicit current User"
            $script:session = TryNewPSSession -HostName localhost
            $script:session | Should -Not -BeNullOrEmpty
            VerifySession $script:session
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies new connection with explicit User parameter" {
            Write-Verbose -Verbose "It Starting: Verifies new connection with explicit User parameter"
            $script:session = TryNewPSSession -HostName localhost -UserName $script:CurrentUserName
            $script:session | Should -Not -BeNullOrEmpty
            VerifySession $script:session
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies no connection with malformed User parameter" {
            Write-Verbose -Verbose "It Starting: Verifies no connection with malformed User parameter"
            $attempt = NewPSSessionAttempt -HostName localhost -UserName "$script:CurrentUserName -o PasswordAuthentication=no" -SkipRetry
            $attempt.Session | Should -BeNullOrEmpty
            $attempt.Error | Should -Not -BeNullOrEmpty
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies no connection with malformed User parameter with double quotes" {
            Write-Verbose -Verbose "It Starting: Verifies no connection with malformed User parameter with double quotes"
            $attempt = NewPSSessionAttempt -HostName localhost -UserName "$script:CurrentUserName`" `"-o PasswordAuthentication=no" -SkipRetry
            $attempt.Session | Should -BeNullOrEmpty
            $attempt.Error | Should -Not -BeNullOrEmpty
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies explicit Name parameter" {
            Write-Verbose -Verbose "It Starting: Verifies explicit Name parameter"
            $sessionName = 'TestSessionNameA'
            $script:session = TryNewPSSession -HostName localhost -Name $sessionName
            $script:session | Should -Not -BeNullOrEmpty
            VerifySession $script:session
            $script:session.Name | Should -BeExactly $sessionName
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies explicit Port parameter" {
            Write-Verbose -Verbose "It Starting: Verifies explicit Port parameter"
            $portNum = 22
            $script:session = TryNewPSSession -HostName localhost -Port $portNum
            $script:session | Should -Not -BeNullOrEmpty
            VerifySession $script:session
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies no connection with malformed Port parameter" {
            Write-Verbose -Verbose "It Starting: Verifies no connection with malformed Port parameter"
            $portNum = 22
            $attempt = NewPSSessionAttempt -HostName localhost -Port "$portNum -o PasswordAuthentication=no" -SkipRetry
            $attempt.Session | Should -BeNullOrEmpty
            $attempt.Error | Should -Not -BeNullOrEmpty
            Write-Verbose "$($attempt.Error)" -Verbose
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies explicit Options parameter" {
            $options = @{"Port"="22"}
            $script:session = New-PSSession -HostName localhost -Options $options -ErrorVariable err
            $err | Should -HaveCount 0
            VerifySession $script:session
        }

        It "Verifies explicit Subsystem parameter" {
            Write-Verbose -Verbose "It Starting: Verifies explicit Subsystem parameter"
            $portNum = 22
            $subSystem = 'powershell'
            $script:session = TryNewPSSession -HostName localhost -Port $portNum -SubSystem $subSystem
            $script:session | Should -Not -BeNullOrEmpty
            VerifySession $script:session
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies no connection with malformed Subsystem parameter" {
            Write-Verbose -Verbose "It Starting: Verifies no connection with malformed Subsystem parameter"
            $subSystem = 'powershell'
            $attempt = NewPSSessionAttempt -HostName localhost -Subsystem "$subSystem -o PasswordAuthentication=no" -SkipRetry
            $attempt.Session | Should -BeNullOrEmpty
            $attempt.Error | Should -Not -BeNullOrEmpty
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies no connection with trailing backslash in Subsystem parameter" {
            Write-Verbose -Verbose "It Starting: Verifies no connection with trailing backslash in Subsystem parameter"
            $subSystem = 'powershell\'
            $attempt = NewPSSessionAttempt -HostName localhost -Subsystem $subSystem -SkipRetry
            $attempt.Session | Should -BeNullOrEmpty
            $attempt.Error | Should -Not -BeNullOrEmpty
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies no connection with two trailing backslashes in Subsystem parameter" {
            Write-Verbose -Verbose "It Starting: Verifies no connection with two trailing backslashes in Subsystem parameter"
            $subSystem = 'powershell\\'
            $attempt = NewPSSessionAttempt -HostName localhost -Subsystem $subSystem -SkipRetry
            $attempt.Session | Should -BeNullOrEmpty
            $attempt.Error | Should -Not -BeNullOrEmpty
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies explicit KeyFilePath parameter" {
            Write-Verbose -Verbose "It Starting: Verifies explicit KeyFilePath parameter"
            $keyFilePath = $script:SshKeyFilePath
            $portNum = 22
            $subSystem = 'powershell'
            $script:session = TryNewPSSession -HostName localhost -Port $portNum -SubSystem $subSystem -KeyFilePath $keyFilePath
            $script:session | Should -Not -BeNullOrEmpty
            VerifySession $script:session
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies explicit KeyFilePath parameter with backslashes on Windows" -Skip:(-not $IsWindows) {
            Write-Verbose -Verbose "It Starting: Verifies explicit KeyFilePath parameter with backslashes on Windows"
            $keyFilePath = Join-Path -Path $HOME -ChildPath '.ssh\id_rsa'
            $portNum = 22
            $subSystem = 'powershell'
            $script:session = TryNewPSSession -HostName localhost -Port $portNum -SubSystem $subSystem -KeyFilePath $keyFilePath
            $script:session | Should -Not -BeNullOrEmpty
            VerifySession $script:session
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies no connection with malformed KeyFilePath parameter" {
            Write-Verbose -Verbose "It Starting: Verifies no connection with malformed KeyFilePath parameter"
            $keyFilePath = $script:SshKeyFilePath
            $attempt = NewPSSessionAttempt -HostName localhost -KeyFilePath "$keyFilePath -o PasswordAuthentication=no" -SkipRetry
            $attempt.Session | Should -BeNullOrEmpty
            $attempt.Error | Should -Not -BeNullOrEmpty
            Write-Verbose "$($attempt.Error)" -Verbose
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies SSHConnection hash table parameters" {
            Write-Verbose -Verbose "It Starting: Verifies SSHConnection hash table parameters"
            $sshConnection = @(
            @{
                HostName = 'localhost'
                UserName = $script:CurrentUserName
                Port = 22
                KeyFilePath = $script:SshKeyFilePath
                Subsystem = 'powershell'
            },
            @{
                HostName = 'localhost'
                KeyFilePath = $script:SshKeyFilePath
                Subsystem = 'powershell'
            })
            $script:sessions = TryNewPSSessionHash -SSHConnection $sshConnection -Name 'Connection1','Connection2'
            $script:sessions | Should -HaveCount 2
            $script:sessions[0].Name | Should -BeLike 'Connection*'
            $script:sessions[1].Name | Should -BeLike 'Connection*'
            VerifySession $script:sessions[0]
            VerifySession $script:sessions[1]
            Write-Verbose -Verbose "It Complete"
        }

        It "Verifies the 'pwshconfig' configured endpoint." -Skip:$IsWindows {
            Write-Verbose -Verbose "It Starting: Verifies the 'pwshconfig' configured endpoint."
            $script:session = TryNewPSSession -HostName localhost -Subsystem 'pwshconfig'
            $script:session | Should -Not -BeNullOrEmpty
            # Configured session should be in ConstrainedLanguage mode.
            $sessionLangMode = Invoke-Command -Session $script:session -ScriptBlock { "$($ExecutionContext.SessionState.LanguageMode)" }
            $sessionLangMode | Should -BeExactly "ConstrainedLanguage"
            Write-Verbose -Verbose "It Complete"
        }

        <#
        It "Verifes that 'pwshbroken' throws expected error for missing config file." {
            Write-Verbose -Verbose "It Starting: Verifes that 'pwshbroken' throws expected error for missing config file."
            { $script:session = TryNewPSSession -HostName localhost -Subsystem 'pwshbroken' } | Should -Throw
            $script:session = $null
            Write-Verbose -Verbose "It Complete"
        }
        #>
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

        Write-Verbose -Verbose "Starting TryCreateRunspace ..."

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
                Write-Verbose -Verbose "SSH Runspace Open remoting connect failed."

                if ($count -eq 1)
                {
                    # Try restarting sshd service
                    RestartSSHDService
                }
            }
        }

        if (($null -eq $rs) -or !($rs -is [runspace]))
        {
            $message = "Runspace open unable to connect to SSH remoting endpoint after two attempts. Error: $($connectionError.Message)"
            throw [System.Management.Automation.PSInvalidOperationException]::new($message)
        }

        Write-Verbose -Verbose "SSH Runspace Open remoting connect succeeded."
        Write-Output $rs
    }

    function VerifyRunspace {
        param (
            [runspace] $rs
        )

        if ($null -eq $rs)
        {
            return
        }

        Write-Verbose -Verbose "VerifyRunspace called for runspace: $($rs.Id)"

        $rs.RunspaceStateInfo.State | Should -BeExactly 'Opened'
        $rs.RunspaceAvailability | Should -BeExactly 'Available'
        $rs.RunspaceIsRemote | Should -BeTrue
        $ps = [powershell]::Create()
        try
        {
            Write-Verbose -Verbose "VerifyRunspace: Invoking PSSenderInfo"
            $ps.Runspace = $rs
            $psRemoteVersion = $ps.AddScript('$PSSenderInfo.ApplicationArguments.PSVersionTable.PSVersion').Invoke()
            $psRemoteVersion.Major | Should -BeExactly $PSVersionTable.PSVersion.Major
            $psRemoteVersion.Minor | Should -BeExactly $PSVersionTable.PSVersion.Minor

            $ps.Commands.Clear()
            Write-Verbose -Verbose "VerifyRunspace: Invoking whoami"
            $ps.AddScript('whoami').Invoke() | Should -BeExactly $(whoami)
            Write-Verbose -Verbose "VerifyRunspace complete"
        }
        finally
        {
            $ps.Dispose()
        }
    }

    Context "SSH Remoting API Tests" {

        AfterEach {
            Write-Verbose -Verbose "Starting Runspace close AfterEach"
            if (($script:rs -ne $null) -and ($script:rs -is [runspace])) { $script:rs.Dispose() }
            Write-Verbose -Verbose "AfterEach complete"
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
                UserName = $script:CurrentUserName
                ComputerName = 'localhost'
                KeyFilePath = $null
                Port = 0
                Subsystem = $null
            },
            @{
                testName = 'Verifies connection with KeyFilePath'
                UserName = $script:CurrentUserName
                ComputerName = 'localhost'
                KeyFilePath = $script:SshKeyFilePath
                Port = 0
                Subsystem = $null
            },
            @{
                testName = 'Verifies connection with Port specified'
                UserName = $script:CurrentUserName
                ComputerName = 'localhost'
                KeyFilePath = $script:SshKeyFilePath
                Port = 22
                Subsystem = $null
            },
            @{
                testName = 'Verifies connection with Subsystem specified'
                UserName = $script:CurrentUserName
                ComputerName = 'localhost'
                KeyFilePath = $script:SshKeyFilePath
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
                $SubSystem,
                $TestName
            )

            Write-Verbose -Verbose "It Starting: $TestName"
            $script:rs = TryCreateRunspace -UserName $UserName -ComputerName $ComputerName -KeyFilePath $KeyFilePath -Port $Port -Subsystem $Subsystem
            $script:rs | Should -Not -BeNullOrEmpty
            VerifyRunspace $script:rs
            Write-Verbose -Verbose "It Complete"
        }
    }
}
