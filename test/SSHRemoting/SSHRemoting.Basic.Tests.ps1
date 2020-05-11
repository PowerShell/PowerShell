# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "SSHRemoting Basic Tests" -tags CI {

    # SSH remoting is set up to automatically authenticate current user via SSH keys
    # All tests connect back to localhost machine

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
            $script:session = New-PSSession -HostName localhost -ErrorVariable err
            $err | Should -HaveCount 0
            VerifySession $script:session
        }

        It "Verifies new connection with explicit User parameter" {
            $script:session = New-PSSession -HostName localhost -UserName (whoami) -ErrorVariable err
            $err | Should -HaveCount 0
            VerifySession $script:session
        }

        It "Verifies explicit Name parameter" {
            $sessionName = 'TestSessionNameA'
            $script:session = New-PSSession -HostName localhost -Name $sessionName -ErrorVariable err
            $err | Should -HaveCount 0
            VerifySession $script:session
            $script:session.Name | Should -BeExactly $sessionName
        }

        It "Verifies explicit Port parameter" {
            $portNum = 22
            $script:session = New-PSSession -HostName localhost -Port $portNum -ErrorVariable err
            $err | Should -HaveCount 0
            VerifySession $script:session
        }

        It "Verifies explicit Subsystem parameter" {
            $portNum = 22
            $subSystem = 'powershell'
            $script:session = New-PSSession -HostName localhost -Port $portNum -SubSystem $subSystem -ErrorVariable err
            $err | Should -HaveCount 0
            VerifySession $script:session
        }

        It "Verifies explicit KeyFilePath parameter" {
            $keyFilePath = "$HOME/.ssh/id_rsa"
            $portNum = 22
            $subSystem = 'powershell'
            $script:session = New-PSSession -HostName localhost -Port $portNum -SubSystem $subSystem -KeyFilePath $keyFilePath -ErrorVariable err
            $err | Should -HaveCount 0
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
            $script:sessions = New-PSSession -SSHConnection $sshConnection -Name 'Connection1','Connection2' -ErrorVariable err
            $err | Should -HaveCount 0
            $script:sessions | Should -HaveCount 2
            $script:sessions[0].Name | Should -BeLike 'Connection*'
            $script:sessions[1].Name | Should -BeLike 'Connection*'
            VerifySession $script:sessions[0]
            VerifySession $script:sessions[1]
        }
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

            $ci = [System.Management.Automation.Runspaces.SSHConnectionInfo]::new($UserName, $ComputerName, $KeyFilePath, $Port, $Subsystem)
            $script:rs = [runspacefactory]::CreateRunspace($host, $ci)
            $script:rs.Open()
            VerifyRunspace $script:rs
        }
    }
}
