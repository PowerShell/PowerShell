# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "SSHRemoting Basic Tests" -tags CI {

    # SSH remoting is set up to automatically authenticate current user via SSH keys
    # All tests connect back to same localhost machine

    function VerifySession {
        param (
            $session
        )

        $session.State | Should -BeExactly 'Opened'
        $session.ComputerName | Should -BeExactly 'localhost'
        $session.Transport | Should -BeExactly 'SSH'
        Invoke-Command -Session $session -ScriptBlock { $env:USER } | Should -BeExactly $env:USER
    }

    Context "New-PSSession Tests" {

        AfterEach {
            if ($script:session -ne $null) { Remove-PSSession -session $script:session }
            if ($script:sessions -ne $null) { Remove-PSSession -session $script:sessions }
        }

        It "Verifies new connection with implicit current User" {
            $script:session = New-PSSession -HostName localhost -ErrorVariable err
            $err | Should -HaveCount 0
            VerifySession $script:session
        }

        It "Verifies new connection with explicit User parameter" {
            $script:session = New-PSSession -HostName localhost -UserName ($env:USER) -ErrorVariable err
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
                UserName = $env:USER
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

    <#
    Context "SSH Remoting API Tests" {


    }
    #>
}
