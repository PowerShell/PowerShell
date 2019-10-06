# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Import-Module HelpersCommon

Describe "Test-Connection" -tags "CI" {
    BeforeAll {
        $hostName = [System.Net.Dns]::GetHostName()
        $targetName = "localhost"
        $targetAddress = "127.0.0.1"
        # TODO:
        # CI Travis don't support IPv6
        # so we use the workaround.
        # $targetAddressIPv6 = "::1"
        $targetAddressIPv6 = [System.Net.Dns]::GetHostEntry($targetName).AddressList[0].IPAddressToString
        $UnreachableAddress = "10.11.12.13"
        # this resolves to an actual IP rather than 127.0.0.1
        # this can also include both IPv4 and IPv6, so select InterNetwork rather than InterNetworkV6
        $realAddress = [System.Net.Dns]::GetHostEntry($hostName).AddressList |
            Where-Object { $_.AddressFamily -eq "InterNetwork" } |
            Select-Object -First 1 |
            ForEach-Object { $_.IPAddressToString }
        # under some environments, we can't round trip this and retrieve the real name from the address
        # in this case we will simply use the hostname
        $jobContinues = Start-Job { Test-Connection $using:targetAddress -Repeat }
    }

    Context "Ping" {
        It "Default parameter set is 'Ping'" {
            $pingResults = Test-Connection $targetName
            $pingResults.Count | Should -Be 4

            $result = $pingResults |
                Where-Object Status -eq 'Success' |
                Select-Object -First 1

            $result | Should -BeOfType "Microsoft.PowerShell.Commands.TestConnectionCommand+PingStatus"
            $result.Ping | Should -Be 1
            $result.Source | Should -BeExactly $hostName
            $result.Destination | Should -BeExactly $targetName
            $result.Address | Should -BeExactly $targetAddressIPv6
            $result.Status | Should -BeExactly "Success"
            $result.Latency | Should -BeOfType "long"
            $result.Reply | Should -BeOfType "System.Net.NetworkInformation.PingReply"
            $result.Options | Should -BeOfType "System.Net.NetworkInformation.PingOptions"
            $result.BufferSize | Should -Be 32
        }

        It "Count parameter" {
            # Also we explicitly test '-Ping' parameter.
            $result1 = Test-Connection -Ping $targetName -Count 1
            $result2 = Test-Connection $targetName -Count 2

            $result1.Count | Should -Be 1
            $result2.Count | Should -Be 2
        }

        It "Quiet works" {
            $result1 = Test-Connection $targetName -Count 1 -Quiet
            # Ping unreachable address
            $result2 = Test-Connection $UnreachableAddress -Count 1 -Quiet

            $result1 | Should -BeTrue
            $result2 | Should -BeFalse
        }

        It "Ping fake host" {

            { $result = Test-Connection "fakeHost" -Count 1 -Quiet -ErrorAction Stop } |
                Should -Throw -ErrorId "TestConnectionException,Microsoft.PowerShell.Commands.TestConnectionCommand"
            # Error code = 11001 - Host not found.
            if (!$isWindows) {
                $Error[0].Exception.InnerException.ErrorCode | Should -Be -131073
            }
            else {
                $Error[0].Exception.InnerException.ErrorCode | Should -Be 11001
            }
        }

        # In VSTS, address is 0.0.0.0
        It "Force IPv4 with implicit PingOptions" {
            $result = Test-Connection $hostName -Count 1 -IPv4

            $result[0].Address | Should -BeExactly $realAddress
            $result[0].Options.Ttl | Should -BeLessOrEqual 128
            if ($isWindows) {
                $result[0].Options.DontFragment | Should -BeFalse
            }
        }

        # In VSTS, address is 0.0.0.0
        It "Force IPv4 with explicit PingOptions" {
            $result1 = Test-Connection $hostName -Count 1 -IPv4 -MaxHops 10 -DontFragment

            # explicitly go to google dns. this test will pass even if the destination is unreachable
            # it's more about breaking out of the loop
            $result2 = Test-Connection 8.8.8.8 -Count 1 -IPv4 -MaxHops 1 -DontFragment

            $result1[0].Address | Should -BeExactly $realAddress
            $result1[0].Options.Ttl | Should -BeLessOrEqual 128

            if (!$isWindows) {
                $result1[0].Options.DontFragment | Should -BeTrue
                # Depending on the network configuration any of the following should be returned
                $result2[0].Status | Should -BeIn "TtlExpired", "TimedOut", "Success"
            }
            else {
                $result1[0].Options.DontFragment | Should -BeTrue
                # We expect 'TtlExpired' but if a router don't reply we get `TimedOut`
                # AzPipelines returns $null
                $result2[0].Status | Should -BeIn "TtlExpired", "TimedOut", $null
            }
        }

        It "Force IPv6" -Pending {
            $result = Test-Connection $targetName -Count 1 -IPv6

            $result[0].Address | Should -BeExactly $targetAddressIPv6
            # We should check Null not Empty!
            $result[0].Options | Should -Be $null
        }

        It "MaxHops Should -Be greater 0" {
            { Test-Connection $targetName -MaxHops 0 } |
                Should -Throw -ErrorId "System.ArgumentOutOfRangeException,Microsoft.PowerShell.Commands.TestConnectionCommand"
            { Test-Connection $targetName -MaxHops -1 } |
                Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
        }

        It "Count Should -Be greater 0" {
            { Test-Connection $targetName -Count 0 } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
            { Test-Connection $targetName -Count -1 } | Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
        }

        It "Delay Should -Be greater 0" {
            { Test-Connection $targetName -Delay 0 } |
                Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
            { Test-Connection $targetName -Delay -1 } |
                Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
        }

        It "Delay works" {
            $result1 = Measure-Command { Test-Connection localhost -Count 2 }
            $result2 = Measure-Command { Test-Connection localhost -Delay 4 -Count 2 }

            $result1.TotalSeconds | Should -BeGreaterThan 1
            $result1.TotalSeconds | Should -BeLessThan 3
            $result2.TotalSeconds | Should -BeGreaterThan 4
        }

        It "BufferSize Should -Be between 0 and 65500" {
            { Test-Connection $targetName -BufferSize 0 } | Should -Not -Throw
            { Test-Connection $targetName -BufferSize -1 } |
                Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
            { Test-Connection $targetName -BufferSize 65501 } |
                Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
        }

        It "BufferSize works" {
            $result = Test-Connection $targetName -Count 1 -BufferSize 2

            if ($isWindows) {
                $result.BufferSize | Should -Be 2
            }
        }

        It "ResolveDestination for address" {
            $result = Test-Connection $targetAddress -ResolveDestination -Count 1
            $resolvedName = [System.Net.DNS]::GetHostEntry($targetAddress).HostName

            $result.Destination | Should -BeExactly $resolvedName
            $result.Address | Should -BeExactly $targetAddress
        }

        It "ResolveDestination for name" {
            $result = Test-Connection $targetName -ResolveDestination -Count 1
            $resolvedName = [System.Net.DNS]::GetHostByName($targetName).HostName

            # PingReply in CoreFX doesn't return ScopeId in IPAddress (Bug?)
            # but GetHostAddresses() returns so remove it.
            $resolvedAddress = ([System.Net.DNS]::GetHostAddresses($resolvedName)[0] -split "%")[0]

            $result.Destination | Should -BeExactly $resolvedName
            $result.Address | Should -BeExactly $resolvedAddress
        }

        It "TimeOut works" {
            (Measure-Command { Test-Connection $UnreachableAddress -Count 1 -TimeOut 1 }).TotalSeconds |
                Should -BeLessThan 3
            (Measure-Command { Test-Connection $UnreachableAddress -Count 1 -TimeOut 4 }).TotalSeconds |
                Should -BeGreaterThan 3
        }

        It "Repeat works" {
            # By default we do 4 ping so for '-Repeat' we expect to get >4 results.
            # Also we should wait >4 seconds before check results but previous tests already did the pause.
            $pingResults = Receive-Job $jobContinues
            Remove-Job $jobContinues -Force

            $pingResults.Count | Should -BeGreaterThan 4
            $pingResults[0].Address | Should -BeExactly $targetAddress
            $pingResults.Status | Should -Contain "Success"
            if ($isWindows) {
                $pingResults.Where( { $_.Status -eq 'Success' }, 'Default', 1 ).BufferSize | Should -Be 32
            }
        }
    }

    # TODO: We skip the MTUSize tests on Unix because we expect 'PacketTooBig' but get 'TimeOut' internally from .Net Core
    Context "MTUSizeDetect" {
        It "MTUSizeDetect works" {
            $result = Test-Connection $hostName -MtuSize

            $result | Should -BeOfType "Microsoft.PowerShell.Commands.TestConnectionCommand+PingMtuStatus"
            $result.Destination | Should -BeExactly $hostName
            $result.Status | Should -BeExactly "Success"
            $result.MtuSize | Should -BeGreaterThan 0
        }

        It "Quiet works" {
            $result = Test-Connection $hostName -MtuSize -Quiet

            $result | Should -BeOfType "Int32"
            $result | Should -BeGreaterThan 0
        }
    }

    Context "TraceRoute" {
        It "TraceRoute works" {
            # real address is an ipv4 address, so force IPv4
            $result = Test-Connection $hostName -TraceRoute -IPv4

            $result[0] | Should -BeOfType "Microsoft.PowerShell.Commands.TestConnectionCommand+TraceStatus"
            $result[0].Source | Should -BeExactly $hostName
            $result[0].TargetAddress | Should -BeExactly $realAddress
            $result[0].Target | Should -BeExactly $hostName
            $result[0].Hop | Should -Be 1
            $result[0].HopAddress | Should -BeExactly $realAddress
            $result[0].Status | Should -BeExactly "Success"
            if (!$isWindows) {
                $result[0].Reply.Buffer.Count | Should -Match '^0$|^32$'
            }
            else {
                $result[0].Reply.Buffer.Count | Should -Be 32
            }
        }

        It "Quiet works" {
            $result = Test-Connection $hostName -TraceRoute -Quiet

            $result | Should -BeTrue
        }

        It 'writes an error if MaxHops is exceeded during -Traceroute' {
            { Test-Connection 8.8.8.8 -Traceroute -MaxHops 2 -ErrorAction Stop } |
                Should -Throw -ErrorId 'TestConnectionException,Microsoft.PowerShell.Commands.TestConnectionCommand'
        }

        It 'returns false without error if MaxHops is exceeded during -Traceroute -Quiet' {
            Test-Connection 8.8.8.8 -Traceroute -MaxHops 2 -Quiet | Should -BeFalse
        }
    }
}

Describe "Connection" -Tag "CI", "RequireAdminOnWindows" {
    BeforeAll {
        # Ensure the local host listen on port 80
        $WebListener = Start-WebListener
        $UnreachableAddress = "10.11.12.13"
    }

    It "Test connection to local host port 80" {
        Test-Connection '127.0.0.1' -TcpPort $WebListener.HttpPort | Should -BeTrue
    }

    It "Test connection to unreachable host port 80" {
        Test-Connection $UnreachableAddress -TcpPort 80 -TimeOut 1 | Should -BeFalse
    }
}
