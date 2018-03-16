# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
try {
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues['it:pending'] = $true
    }
    Describe "New-CimSession" -Tag @("CI") {
        BeforeAll {
            $sessions = @()
        }
        AfterEach {
            try {
                $sessions | remove-cimsession
            }
            finally {
                $sessions = @()
            }
        }
        It "A cim session can be created" {
            $sessionName = [guid]::NewGuid()
            $session = New-CimSession -ComputerName . -name $sessionName
            $sessions += $session
            $session.Name | Should -Be $sessionName
            $session.InstanceId  | Should -BeOfType "System.Guid"
        }
        It "A Cim session can be retrieved" {
            $sessionName = [guid]::NewGuid()
            $session = New-CimSession -ComputerName . -name $sessionName
            $sessions += $session
            (get-cimsession -Name $sessionName).InstanceId | Should -Be $session.InstanceId
            (get-cimsession -Id $session.Id).InstanceId | Should -Be $session.InstanceId
            (get-cimsession -InstanceId $session.InstanceId).InstanceId | Should -Be $session.InstanceId
        }
        It "A cim session can be removed" {
            $sessionName = [guid]::NewGuid()
            $session = New-CimSession -ComputerName . -name $sessionName
            $sessions += $session
            $session.Name | Should -Be $sessionName
            $session | Remove-CimSession
            Get-CimSession $session.Id -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
    }
}
finally {
    $PSDefaultParameterValues.remove('it:pending')
}
