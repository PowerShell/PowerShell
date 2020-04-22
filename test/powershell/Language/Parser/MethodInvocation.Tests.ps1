# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
if ( $IsCoreCLR ) {
    return
}

Describe "Interface inheritance with remoting proxies" -Tags "CI" {
    $src = @"
using System;
using System.ServiceModel;

namespace MSFT_716893
{
    [ServiceContract]
    public interface IInterface1
    {
        [OperationContract]string BaseOperation(int i);
    }

    [ServiceContract]
    public interface IInterface2 : IInterface1
    {
        [OperationContract(Name="op1")]string Operation(string a);
        [OperationContract(Name="op2")]string Operation(string a, string b);
    }

    public class ServiceImplementation : IInterface2
    {
        public string Operation(string a) { return "1 - " + a; }
        public string Operation(string a, string b) { return "2 - " + a + " " + b; }
        public string BaseOperation(int i) { return "3 - " + i; }
    }

    public static class Service
    {
        static ServiceHost serviceHost;

        public static void Init()
        {
            Uri baseAddress = new Uri("http://localhost:8080/service");
            serviceHost = new ServiceHost(typeof(ServiceImplementation), baseAddress);
            serviceHost.Open();
        }

        public static IInterface1 GetProxy()
        {
            ChannelFactory<IInterface2> factory = new ChannelFactory<IInterface2>(
                serviceHost.Description.Endpoints[0].Binding,
                serviceHost.Description.Endpoints[0].Address);
            return factory.CreateChannel();
        }

        public static void Close()
        {
            serviceHost.Close();
        }
    }
}
"@

    Add-Type -TypeDefinition $src -ReferencedAssemblies System.ServiceModel.dll

    BeforeEach {
        [MSFT_716893.Service]::Init()
        $proxy = [MSFT_716893.Service]::GetProxy()
    }

    AfterEach {
        [MSFT_716893.Service]::Close()
    }

    It "Direct invocation" {
        $proxy.Operation("a") | Should -Be "1 - a"
        $proxy.Operation("a", "b") | Should -Be "2 - a b"
        $proxy.BaseOperation(42) | Should -Be "3 - 42"
    }

    It "Invocation via method constraints" {
        ([MSFT_716893.IInterface2]$proxy).Operation("c") | Should -Be "1 - c"
        ([MSFT_716893.IInterface2]$proxy).Operation("d", "e") | Should -Be "2 - d e"
        ([MSFT_716893.IInterface1]$proxy).BaseOperation(22) | Should -Be "3 - 22"
    }
}

Describe 'Generic Method invocation' {

    BeforeAll {
        $EmptyArrayCases = @(
            @{
                Script       = '[Array]::Empty[string]()'
                ExpectedType = [string[]]
            }
            @{
                Script       = '[Array]::Empty[System.Collections.Generic.Dictionary[System.Numerics.BigInteger, System.Collections.Generic.List[string[,]]]]()'
                ExpectedType = [System.Collections.Generic.Dictionary[System.Numerics.BigInteger, System.Collections.Generic.List[string[, ]]][]]
            }
            @{
                Script       = '[Array]::$("Empty")[[System.Collections.Generic.Dictionary[[System.String, System.Private.CoreLib],[System.Numerics.BigInteger, System.Runtime.Numerics]], System.Private.CoreLib]]()'
                ExpectedType = [System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib], [System.Numerics.BigInteger, System.Runtime.Numerics]][], System.Private.CoreLib]
            }
        )
    }

    It 'does not throw a parse error for "<Script>"' -TestCases $EmptyArrayCases {
        param($Script)

        { [scriptblock]::Create($script) } | Should -Not -Throw
    }

    It 'can call a generic method "<Script>" with no arguments' -TestCases $EmptyArrayCases {
        param($Script, $ExpectedType)

        $Result = & [scriptblock]::Create($Script)
        $Result.GetType() | Should -Be $ExpectedType

        $Result.Length | Should -Be 0
    }

    It 'can call generic instance methods' {
        $dictionary = [System.Collections.Concurrent.ConcurrentDictionary[string, int]]::new()

        $addEntryScript = {
            param($key, $float)

            if ($float -gt 0.5) {
                return 10
            }
            else {
                return 1
            }
        }

        $updateEntryScript = {
            param($key, $currentValue, $float)

            if ($currentValue / $float -gt 2) {
                return 5
            }
            else {
                return 0
            }
        }

        $FloatValue = 0.4
        $Key = 'Test'

        # Add entry
        $dictionary.AddOrUpdate[float]($Key, $addEntryScript, $updateEntryScript, $FloatValue)
        $dictionary.$Key | Should -Be 1

        # Update entry
        $dictionary.AddOrUpdate[float]($Key, $addEntryScript, $updateEntryScript, $FloatValue)
        $dictionary.$Key | Should -Be 5
    }

    It 'can call generic static methods with arguments' {
        [System.Linq.Enumerable]::Select[int, int](
            [int[]](0..10),
            [func[int, int]]{ $args[0] + 2 }
        ) | Should -Be @(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)
    }
}
