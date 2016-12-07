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
        $proxy.Operation("a") | Should Be "1 - a"
        $proxy.Operation("a", "b") | Should Be "2 - a b"
        $proxy.BaseOperation(42) | Should Be "3 - 42"
    }

    It "Invocation via method constraints" {
        ([MSFT_716893.IInterface2]$proxy).Operation("c") | Should Be "1 - c"
        ([MSFT_716893.IInterface2]$proxy).Operation("d", "e") | Should Be "2 - d e"
        ([MSFT_716893.IInterface1]$proxy).BaseOperation(22) | Should Be "3 - 22"
    }
}
