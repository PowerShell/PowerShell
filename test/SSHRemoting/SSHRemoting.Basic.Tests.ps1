# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "SSHRemoting Basic Tests" -tags CI {

    Context "New-PSSession Tests" {

        It "Verifies New-PSSession can create a localhost connection" {
            # Create loopback connection to localhost with current user context.
            # Authentication is performed via SSH keys for current user.
            $session = New-PSSession -HostName localhost -ErrorVariable err
            $err | Should -HaveCount 0
            $session.State | Should -BeExactly 'Opened'
            $session.ComputerName | Should -BeExactly 'localhost'
            Remove-PSSession -Session $session
        }
    }
}
