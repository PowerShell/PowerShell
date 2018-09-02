# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe "Test-Connection" -tags "CI" {
    BeforeAll {
        $oldInformationPreference = $InformationPreference
        $oldProgressPreference = $ProgressPreference
        $InformationPreference = "Ignore"
        $ProgressPreference = "SilentlyContinue"

        $hostName = [System.Net.Dns]::GetHostName()
        $targetName = "localhost"
        $targetAddress = "127.0.0.1"
        # TODO:
        # CI Travis don't support IPv6
        # so we use the workaround.
        # $targetAddressIPv6 = "::1"
        $targetAddressIPv6 = [System.Net.Dns]::GetHostEntry($targetName).AddressList[0].IPAddressToString
        $UnreachableAddress = "10.11.12.13"
        $realName = "google-public-dns-a.google.com"
        $realAddress = [System.Net.Dns]::GetHostEntry($realName).AddressList[0].IPAddressToString
        $jobContinues = Start-Job { Test-Connection $using:targetAddress -Continues }
    }

    AfterAll {
        $InformationPreference = $oldInformationPreference
        $ProgressPreference = $oldProgressPreference
    }

    Context "Ping" {
        It "Default parameter set is 'Ping'" {
            $result = Test-Connection $targetName
            $replies = $result.Replies

            $result.Count          | Should -Be 1
            $result[0]             | Should -BeOfType "Microsoft.PowerShell.Commands.TestConnectionCommand+PingReport"
            $result[0].Source      | Should -BeExactly $hostName
            $result[0].Destination | Should -BeExactly $targetName

            $replies.Count           | Should -Be 4
            $replies[0]              | Should -BeOfType "System.Net.NetworkInformation.PingReply"
            $replies[0].Address      | Should -BeExactly $targetAddressIPv6
            $replies[0].Status       | Should -BeExactly "Success"
            # TODO: Here and below we skip the check on Unix because .Net Core issue
            if ($isWindows) {
                $replies[0].Buffer.Count | Should -Be 32
            }
        }

        It "Count parameter" {
            # Also we explicitly test '-Ping' parameter.
            $result1 = Test-Connection -Ping $targetName -Count 1
            $result2 = Test-Connection $targetName -Count 2

            $result1.Replies.Count | Should -Be 1
            $result2.Replies.Count | Should -Be 2
        }

        It "Quiet works" {
            $result1 = Test-Connection $targetName -Count 1 -Quiet
            # Ping unreachable address
            $result2 = Test-Connection $UnreachableAddress -Count 1 -Quiet

            $result1 | Should -BeTrue
            $result2 | Should -BeFalse
        }

        It "Ping fake host" {

            { $result = Test-Connection "fakeHost" -Count 1 -Quiet -ErrorAction Stop } | Should -Throw -ErrorId "TestConnectionException,Microsoft.PowerShell.Commands.TestConnectionCommand"
            # Error code = 11001 - Host not found.
            if (!$isWindows) {
                $Error[0].Exception.InnerException.ErrorCode | Should -Be 6
            } else {
                $Error[0].Exception.InnerException.ErrorCode | Should -Be 11001
            }
        }

        # In VSTS, address is 0.0.0.0
        It "Force IPv4 with implicit PingOptions" -Skip:((Test-IsVstsLinux) -or (Test-IsVstsWindows)) {
            $result = Test-Connection $realName -Count 1 -IPv4

            $result.Replies[0].Address              | Should -BeExactly $realAddress
            $result.Replies[0].Options.Ttl          | Should -BeLessThan 128
            if ($isWindows) {
                $result.Replies[0].Options.DontFragment | Should -BeFalse
            }
        }

        # In VSTS, address is 0.0.0.0
        It "Force IPv4 with explicit PingOptions" -Skip:((Test-IsVstsLinux) -or (Test-IsVstsWindows)) {
            $result1 = Test-Connection $realName -Count 1 -IPv4 -MaxHops 10 -DontFragment

            $result2 = Test-Connection $realName -Count 1 -IPv4 -MaxHops 1 -DontFragment

            $result1.Replies[0].Address              | Should -BeExactly $realAddress
            # .Net Core (.Net Framework) returns Options based on default PingOptions() constructor (Ttl=128, DontFragment = false).
            # After .Net Core fix we should have 'DontFragment | Should -Be $true' here.
            $result1.Replies[0].Options.Ttl          | Should -BeLessThan 128
            if (!$isWindows) {
                $result1.Replies[0].Options.DontFragment | Should -BeNullOrEmpty
                $result2.Replies[0].Status               | Should -BeExactly "Success"
            } else {
                $result1.Replies[0].Options.DontFragment | Should -BeFalse
                # We expect 'TtlExpired' but if a router don't reply we get `TimeOut`
                $result2.Replies[0].Status               | Should -BeIn "TtlExpired","TimedOut"
            }
        }

        It "Force IPv6" -Pending {
            $result = Test-Connection $targetName -Count 1 -IPv6

            $result.Replies[0].Address | Should -BeExactly $targetAddressIPv6
            # We should check Null not Empty!
            $result.Replies[0].Options | Should -Be $null
        }

        It "MaxHops Should -Be greater 0" {
            { Test-Connection $targetName -MaxHops 0 }  | Should -Throw -ErrorId "System.ArgumentOutOfRangeException,Microsoft.PowerShell.Commands.TestConnectionCommand"
            { Test-Connection $targetName -MaxHops -1 } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
        }

        It "Count Should -Be greater 0" {
            { Test-Connection $targetName -Count 0 }  | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
            { Test-Connection $targetName -Count -1 } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
        }

        It "Delay Should -Be greater 0" {
            { Test-Connection $targetName -Delay 0 }  | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
            { Test-Connection $targetName -Delay -1 } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
        }

        It "Delay works" {
            $result1 = measure-command {Test-Connection localhost -Count 2}
            $result2 = measure-command {Test-Connection localhost -Delay 4 -Count 2}

            $result1.TotalSeconds | Should -BeGreaterThan 1
            $result1.TotalSeconds | Should -BeLessThan 3
            $result2.TotalSeconds | Should -BeGreaterThan 4
        }

        It "BufferSize Should -Be between 0 and 65500" {
            { Test-Connection $targetName -BufferSize 0 }     | Should Not Throw
            { Test-Connection $targetName -BufferSize -1 }    | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
            { Test-Connection $targetName -BufferSize 65501 } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
        }

        It "BufferSize works" -Pending:(!$IsWindows) {
            $result = Test-Connection $targetName -Count 1 -BufferSize 2

            if ($isWindows) {
                $result.Replies[0].Buffer.Count | Should -Be 2
            }
        }

        It "ResolveDestination for address" {
            $result = Test-Connection $targetAddress -ResolveDestination -Count 1
            $resolvedName = [System.Net.DNS]::GetHostByName($targetName).HostName

            $result.Destination | Should -BeExactly $resolvedName
            $result.Replies[0].Address     | Should -BeExactly $targetAddress
        }

        It "ResolveDestination for name" {
            $result = Test-Connection $targetName -ResolveDestination -Count 1
            $resolvedName = [System.Net.DNS]::GetHostByName($targetName).HostName

            # PingReply in CoreFX doesn't return ScopeId in IPAddress (Bug?)
            # but GetHostAddresses() returns so remove it.
            $resolvedAddress = ([System.Net.DNS]::GetHostAddresses($resolvedName)[0] -split "%")[0]

            $result.Destination | Should -BeExactly $resolvedName
            $result.Replies[0].Address     | Should -BeExactly $resolvedAddress
        }

        It "TimeOut works" {
            (Measure-Command { Test-Connection $UnreachableAddress -Count 1 -TimeOut 1 }).TotalSeconds | Should -BeLessThan 3
            (Measure-Command { Test-Connection $UnreachableAddress -Count 1 -TimeOut 4 }).TotalSeconds | Should -BeGreaterThan 3
        }

        It "Continues works" {
            # By default we do 4 ping so for '-Continues' we expect to get >4 results.
            # Also we should wait >4 seconds before check results but previous tests already did the pause.
            $result = Receive-Job $jobContinues
            Remove-Job $jobContinues -Force

            $result.Count           | Should -BeGreaterThan 4
            $result[0].Address      | Should -BeExactly $targetAddress
            $result[0].Status       | Should -BeExactly "Success"
            if ($isWindows) {
                $result[0].Buffer.Count | Should -Be 32
            }
        }
}

    # TODO: We skip the MTUSizeDetect tests on Unix because we expect 'TtlExpired' but get 'TimeOut' internally from .Net Core
    # Skipping on VSTS in Windows due to `TimedOut`
    Context "MTUSizeDetect" {
        It "MTUSizeDetect works" -Pending:(!$isWindows -or (Test-IsVstsWindows)) {
            $result = Test-Connection $realName -MTUSizeDetect

            $result | Should -BeOfType "System.Net.NetworkInformation.PingReply"
            $result.Destination | Should -BeExactly $realName
            $result.Status | Should -BeExactly "Success"
            $result.MTUSize | Should -BeGreaterThan 0
        }

        It "Quiet works" -Pending:(!$isWindows -or (Test-IsVstsWindows)) {
            $result = Test-Connection $realName -MTUSizeDetect -Quiet

            $result | Should -BeOfType "Int32"
            $result | Should -BeGreaterThan 0
        }
    }

    Context "TraceRoute" {
        # Hangs in VSTS Linux
        It "TraceRoute works" -skip:((Test-IsVstsLinux) -or (Test-IsVstsWindows)) {
            $result = Test-Connection $realName -TraceRoute
            $replies = $result.Replies
            # Check target host reply.
            $pingReplies = $replies[-1].PingReplies

            $result.Count              | Should -Be 1
            $result                    | Should -BeOfType "Microsoft.PowerShell.Commands.TestConnectionCommand+TraceRouteResult"
            $result.Source             | Should -BeExactly $hostName
            $result.DestinationAddress | Should -BeExactly $realAddress
            $result.DestinationHost    | Should -BeExactly $realName

            $replies.Count               | Should -BeGreaterThan 0
            $replies[0]                  | Should -BeOfType "Microsoft.PowerShell.Commands.TestConnectionCommand+TraceRouteReply"
            $replies[0].Hop              | Should -Be 1

            $pingReplies.Count           | Should -Be 3
            $pingReplies[0].Address      | Should -BeExactly $realAddress
            $pingReplies[0].Status       | Should -BeExactly "Success"
            if (!$isWindows) {
                $pingReplies[0].Buffer.Count | Should -Be 0
            } else {
                $pingReplies[0].Buffer.Count | Should -Be 32
            }
        }

        # Hangs in VSTS Linux
        It "Quiet works" -skip:((Test-IsVstsLinux) -or (Test-IsVstsWindows)) {
            $result = Test-Connection $realName -TraceRoute -Quiet

            $result | Should -BeTrue
        }
    }

    Context "Connection" {
        BeforeAll {
            # Ensure the local host listen on port 80
            $WebListener = Start-WebListener
        }

        It "Test connection to local host port 80" {
            Test-Connection '127.0.0.1' -TCPPort $WebListener.HttpPort | Should -BeTrue
        }

        It "Test connection to unreachable host port 80" {
            Test-Connection $UnreachableAddress -TCPPort 80 -TimeOut 1 | Should -BeFalse
        }
    }
}
