# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "New-CimSession" -Tag @("CI","RequireAdminOnWindows") {
    BeforeAll {
        $sessions = @()
    }

    AfterEach {
            $sessions | Remove-CimSession -ErrorAction SilentlyContinue
            $sessions = @()
    }

    It "A cim session can be created" -Pending:(-not $IsWindows) {
        $sessionName = [guid]::NewGuid().Guid
        $session = New-CimSession -ComputerName . -Name $sessionName
        $sessions += $session
        $session.Name | Should -BeExactly $sessionName
        $session.InstanceId  | Should -BeOfType System.Guid
    }

    It "A Cim session can be retrieved" -Pending:(-not $IsWindows) {
        $sessionName = [guid]::NewGuid().Guid
        $session = New-CimSession -ComputerName . -Name $sessionName
        $sessions += $session
        (Get-CimSession -Name $sessionName).InstanceId | Should -Be $session.InstanceId
        (Get-CimSession -Id $session.Id).InstanceId | Should -Be $session.InstanceId
        (Get-CimSession -InstanceId $session.InstanceId).InstanceId | Should -Be $session.InstanceId
    }

    It "A cim session can be removed" -Pending:(-not $IsWindows) {
        $sessionName = [guid]::NewGuid().Guid
        $session = New-CimSession -ComputerName . -Name $sessionName
        $sessions += $session
        $session.Name | Should -BeExactly $sessionName
        $session | Remove-CimSession
        Get-CimSession $session.Id -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}
