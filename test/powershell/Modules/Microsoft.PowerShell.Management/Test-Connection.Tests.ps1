# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

function GetGatewayAddress
{
    return [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() |
        Where-Object { $_.OperationalStatus -eq 'Up' -and $_.NetworkInterfaceType -ne 'Loopback' } |
        ForEach-Object { $_.GetIPProperties().GatewayAddresses } |
        Select-Object -First 1 |
        ForEach-Object { $_.Address.IPAddressToString }
}

function GetExternalHostAddress([string]$HostName)
{
    if (-not $HostName)
    {
        return
    }

    try
    {
        return [System.Net.Dns]::GetHostEntry($HostName).AddressList |
            Where-Object { $_.AddressFamily -eq 'InterNetwork' } |
            Select-Object -First 1 |
            ForEach-Object { $_.IPAddressToString }
    }
    catch
    {
        return
    }
}

# Adding RequireSudoOnUnix due to an intentional breaking change.
# See https://github.com/dotnet/runtime/issues/66746
Describe "Test-Connection" -tags "CI", "RequireSudoOnUnix" {
    BeforeAll {
        $hostName = [System.Net.Dns]::GetHostName()
        $gatewayAddress = GetGatewayAddress
        $publicHostAddress = GetExternalHostAddress -HostName $hostName

        $testAddress = if ($publicHostAddress) { $publicHostAddress } else  { $gatewayAddress }

        $targetName = "localhost"
        $targetAddress = "127.0.0.1"
        $targetAddressIPv6 = "::1"
        $UnreachableAddress = "10.11.12.13"

        # under some environments, we can't round trip this and retrieve the real name from the address
        # in this case we will simply use the hostname
        $jobContinues = Start-Job { Test-Connection $using:targetAddress -Repeat }
    }

    Context "Ping" {
        It "Default parameter set is 'Ping'" {
            $pingResults = Test-Connection $targetName
            $pingResults.Count | Should -Be 4

            $result = $pingResults |
                Where-Object Status -EQ 'Success' |
                Select-Object -First 1

            $result | Should -BeOfType Microsoft.PowerShell.Commands.TestConnectionCommand+PingStatus
            $result.Ping | Should -Be 1
            $result.Source | Should -BeExactly $hostName
            $result.Destination | Should -BeExactly $targetName
            $result.Address | Should -BeIn @($targetAddress, $targetAddressIPv6)
            $result.Status | Should -BeExactly "Success"
            $result.Latency | Should -BeOfType long
            $result.Reply | Should -BeOfType System.Net.NetworkInformation.PingReply
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

        It 'returns false without errors for an unresolvable address when using -Quiet' {
            Test-Connection -Quiet -ErrorAction Stop -Count 1 -TargetName "fakeHost" | Should -BeFalse
        }

        It "Ping fake host" {
            { Test-Connection "fakeHost" -Count 1 -ErrorAction Stop } |
                Should -Throw -ErrorId "TestConnectionException,Microsoft.PowerShell.Commands.TestConnectionCommand"
            # Error code = 11001 - Host not found.
            # Error code = -131073 - Invalid address
            $error[0].Exception.InnerException.ErrorCode | Should -BeIn 11, -131073, 11001
        }

        It "Force IPv4 with implicit PingOptions" {
            $result = Test-Connection $testAddress -Count 1 -IPv4

            $resultStatus = $result.Reply.Status
            if ($resultStatus -eq "Success") {
                $result[0].Address | Should -BeExactly $testAddress
                $result[0].Reply.Options.Ttl | Should -BeLessOrEqual 128
                if ($IsWindows) {
                    $result[0].Reply.Options.DontFragment | Should -BeFalse
                }
            }
            else {
                Set-ItResult -Skipped -Because "Ping reply not Success, was: '$resultStatus'"
            }
        }

        # In VSTS, address is 0.0.0.0
        # This test is marked as PENDING as .NET Core does not return correct PingOptions from ping request
        It "Force IPv4 with explicit PingOptions" -Pending {
            $result1 = Test-Connection $testAddress -Count 1 -IPv4 -MaxHops 10 -DontFragment

            # explicitly go to google dns. this test will pass even if the destination is unreachable
            # it's more about breaking out of the loop
            $result2 = Test-Connection 8.8.8.8 -Count 1 -IPv4 -MaxHops 1 -DontFragment

            $result1.Address | Should -BeExactly $testAddress
            $result1.Reply.Options.Ttl | Should -BeLessOrEqual 128

            if (!$IsWindows) {
                $result1.Reply.Options.DontFragment | Should -BeFalse
                # Depending on the network configuration any of the following should be returned
                $result2.Status | Should -BeIn "TtlExpired", "TimedOut", "Success"
            } else {
                # This assertion currently fails, see https://github.com/PowerShell/PowerShell/issues/12967
                #$result1.Reply.Options.DontFragment | Should -BeTrue

                # We expect 'TtlExpired' but if a router don't reply we get `TimedOut`
                # AzPipelines returns $null
                $result2.Status | Should -BeIn "TtlExpired", "TimedOut", $null
            }
        }

        Context 'IPv6 Tests' {
            # IPv6 tests are marked pending because while the functionality is present
            # and works in local testing, it is not functional in CI. There appears to
            # be a lack of or inconsistent support for IPv6 in CI environments.
            It "Allows us to Force IPv6" -Pending {
                $result = Test-Connection $targetName -IPv6 -Count 4 |
                    Where-Object Status -EQ Success |
                    Select-Object -First 1

                $result.Address | Should -BeExactly $targetAddressIPv6
                $result.Reply.Options | Should -Not -BeNullOrEmpty
            }

            It 'can convert IPv6 addresses to IPv4 with -IPv4 parameter' -Pending {
                $result = Test-Connection '2001:4860:4860::8888' -IPv4 -Count 4 |
                    Where-Object Status -EQ Success |
                    Select-Object -First 1
                # Google's DNS can resolve to either address.
                $result.Address.IPAddressToString | Should -BeIn @('8.8.8.8', '8.8.4.4')
                $result.Address.AddressFamily | Should -BeExactly 'InterNetwork'
            }

            It 'can convert IPv4 addresses to IPv6 with -IPv6 parameter' -Pending {
                $result = Test-Connection '8.8.8.8' -IPv6 -Count 4 |
                    Where-Object Status -EQ Success |
                    Select-Object -First 1
                # Google's DNS can resolve to either address.
                $result.Address.IPAddressToString | Should -BeIn @('2001:4860:4860::8888', '2001:4860:4860::8844')
                $result.Address.AddressFamily | Should -BeExactly 'InterNetworkV6'
            }
        }

        It "MaxHops Should -Be greater 0" {
            { Test-Connection $targetName -MaxHops 0 } |
                Should -Throw -ErrorId "ParameterArgumentValidationError,Microsoft.PowerShell.Commands.TestConnectionCommand"
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

            $result.BufferSize | Should -Be 2
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
            if ($IsWindows) {
                $pingResults.Where( { $_.Status -eq 'Success' }, 'Default', 1 ).BufferSize | Should -Be 32
            }
        }
    }

    Context "MTUSizeDetect" {
        It "MTUSizeDetect works" {

            $platform = Get-PlatformInfo
            $platform | Out-String -Stream | Write-Verbose -Verbose

            if ($platform.platform -match 'sles' -and $platform.version -match '15') {
                Set-ItResult -Skipped -Because "MTUSizeDetect is not supported on OpenSUSE 15"
                return
            }

            $result = Test-Connection $testAddress -MtuSize

            $result | Should -BeOfType Microsoft.PowerShell.Commands.TestConnectionCommand+PingMtuStatus
            $result.Destination | Should -BeExactly $testAddress
            $result.Status | Should -BeExactly "Success"
            $result.MtuSize | Should -BeGreaterThan 0
        }

        It "Quiet works" {
            $result = Test-Connection $gatewayAddress -MtuSize -Quiet

            $result | Should -BeOfType Int32
            $result | Should -BeGreaterThan 0
        }
    }

    Context "TraceRoute" {
        It "TraceRoute works" -Pending {
            # real address is an ipv4 address, so force IPv4
            $result = Test-Connection $testAddress -TraceRoute -IPv4

            $result[0] | Should -BeOfType Microsoft.PowerShell.Commands.TestConnectionCommand+TraceStatus
            $result[0].Source | Should -BeExactly $testAddress
            $result[0].TargetAddress | Should -BeExactly $testAddress
            $result[0].Target | Should -BeExactly $testAddress
            $result[0].Hop | Should -Be 1
            $result[0].HopAddress.IPAddressToString | Should -BeExactly $testAddress
            $result[0].Status | Should -BeExactly "Success"
            if (!$IsWindows) {
                $result[0].Reply.Buffer.Count | Should -Match '^0$|^32$'
            } else {
                $result[0].Reply.Buffer.Count | Should -Be 32
            }
        }

        It "Quiet works" {
            $result = Test-Connection localhost -TraceRoute -Quiet

            $result | Should -BeTrue
        }

        It 'writes an error if MaxHops is exceeded during -Traceroute' {
            { Test-Connection 8.8.8.8 -Traceroute -MaxHops 2 -ErrorAction Stop } |
                Should -Throw -ErrorId 'TestConnectionException,Microsoft.PowerShell.Commands.TestConnectionCommand'
        }

        It 'returns false without error if MaxHops is exceeded during -Traceroute -Quiet' {
            Test-Connection 8.8.8.8 -Traceroute -MaxHops 2 -Quiet | Should -BeFalse
        }

        It 'has a non-null value for Destination for reachable hosts' {
            $results = Test-Connection 127.0.0.1 -Traceroute

            $results.Hostname | Should -Not -BeNullOrEmpty
        }
    }
}

Describe "Connection" -Tag "CI", "RequireAdminOnWindows" {
    BeforeAll {
        # Ensure the local host listen on port 80
        $WebListener = Start-WebListener
        $UnreachableAddress = "10.11.12.13"
    }

    It "Test connection to local host on working port" {
        Test-Connection '127.0.0.1' -TcpPort $WebListener.HttpPort | Should -BeTrue
    }

    It "Test connection to unreachable host port 80" {
        Test-Connection $UnreachableAddress -TcpPort 80 -TimeOut 1 | Should -BeFalse
    }

    It "Test detailed connection to local host on working port" {
        $result = Test-Connection '127.0.0.1' -TcpPort $WebListener.HttpPort -Detailed

        $result.Count | Should -Be 1
        $result[0].Id | Should -BeExactly 1
        $result[0].TargetAddress | Should -BeExactly '127.0.0.1'
        $result[0].Port | Should -Be $WebListener.HttpPort
        $result[0].Latency | Should -BeGreaterOrEqual 0
        $result[0].Connected | Should -BeTrue
        $result[0].Status | Should -BeExactly 'Success'
    }

    It "Test detailed connection to local host on working port with modified count" {
        $result = Test-Connection '127.0.0.1' -TcpPort $WebListener.HttpPort -Detailed -Count 2

        $result.Count | Should -Be 2
        $result[0].Id | Should -BeExactly 1
        $result[0].TargetAddress | Should -BeExactly '127.0.0.1'
        $result[0].Port | Should -Be $WebListener.HttpPort
        $result[0].Latency | Should -BeGreaterOrEqual 0
        $result[0].Connected | Should -BeTrue
        $result[0].Status | Should -BeExactly 'Success'
    }

    It "Test detailed connection to unreachable host port 80" {
        $result = Test-Connection $UnreachableAddress -TcpPort 80 -Detailed -TimeOut 1

        $result.Count | Should -Be 1
        $result[0].Id | Should -BeExactly 1
        $result[0].TargetAddress | Should -BeExactly $UnreachableAddress
        $result[0].Port | Should -Be 80
        $result[0].Latency | Should -BeExactly 0
        $result[0].Connected | Should -BeFalse
        $result[0].Status | Should -Not -BeExactly 'Success'
    }
}

Describe "Test-Connection should run in the default synchronization context (threadpool)" -Tag "CI" {
    It "Test-Connection works after constructing a WindowsForm object" -Skip:(!$IsWindows) {
        $pwsh = Join-Path $PSHOME "pwsh"
        $pingResults = & $pwsh -NoProfile {
            Add-Type -AssemblyName System.Windows.Forms
            $null = New-Object System.Windows.Forms.Form
            Test-Connection localhost
        }

        $pingResults.Length | Should -Be 4
        $result = $pingResults | Select-Object -First 1

        $result.Ping | Should -Be 1
        $result.Source | Should -BeExactly ([System.Net.Dns]::GetHostName())
        $result.Destination | Should -BeExactly localhost
        $result.Latency | Should -BeOfType "long"
        $result.Reply.Status | Should -BeExactly "Success"
        $result.BufferSize | Should -Be 32
    }
}
