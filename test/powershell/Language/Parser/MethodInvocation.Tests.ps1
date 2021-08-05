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

Describe 'Generic Method invocation' -Tags 'CI' {

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

        $ExpectedParseErrors = @(
            @{
                Script         = '$object.Method[incompl'
                ExpectedErrors = 'EndSquareBracketExpectedAtEndOfType'
                ErrorCount     = 1
            }
            @{
                Script         = '[type]::Member[incompl'
                ExpectedErrors = 'EndSquareBracketExpectedAtEndOfType'
                ErrorCount     = 1
            }
            @{
                Script         = '$object.Method[Type1[Type2'
                ExpectedErrors = 'UnexpectedToken','EndSquareBracketExpectedAtEndOfType'
                ErrorCount     = 2
            }
            @{
                Script         = '[array]::empty[type]]()'
                ExpectedErrors = 'UnexpectedToken', 'ExpectedExpression'
                ErrorCount     = 2
            }
            @{
                Script         = '$object.Method[type,]()'
                ExpectedErrors = 'MissingTypeName'
                ErrorCount     = 1
            }
            @{
                Script         = '$object.Method[]()'
                ExpectedErrors = 'MissingArrayIndexExpression', 'UnexpectedToken', 'ExpectedExpression'
                ErrorCount     = 3
            }
            @{
                Script         = '$object.Method[,]()'
                ExpectedErrors = 'MissingExpressionAfterOperator', 'UnexpectedToken', 'ExprectedExpression'
                ErrorCount     = 3
            }
            @{
                Script         = '$object.Method[,type]()'
                ExpectedErrors = 'MissingExpressionAfterOperator', 'UnexpectedToken', 'ExpectedExpression'
                ErrorCount     = 3
            }
            @{
                Script         = '$object.Method[type()'
                ExpectedErrors = 'UnexpectedToken', 'ExpectedExpression'
                ErrorCount     = 2
            }
            @{
                Script         = '$object.Method[type)'
                ExpectedErrors = 'EndSquareBracketExpectedAtEndOfType', 'UnexpectedToken'
                ErrorCount     = 2
            }
        )
    }

    It 'does not throw a parse error for "<Script>"' -TestCases $EmptyArrayCases {
        param($Script)

        { [scriptblock]::Create($script) } | Should -Not -Throw
    }

    It 'reports a parse error for "<Script>"' -TestCases $ExpectedParseErrors {
        param($Script, $ExpectedError, $ErrorCount)

        $parseErrors = $null
        [System.Management.Automation.Language.Parser]::ParseInput($Script, [ref]$null, [ref]$parseErrors)

        $parseErrors.Count | Should -Be $ErrorCount
        $parseErrors.ErrorId | Should -BeIn $ExpectedError
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
            [func[int, int]] { $args[0] + 2 }
        ) | Should -Be @(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)
    }

    It 'gives a runtime error if too many type parameters are given' {
        try {
            [array]::Empty[string, int]()
        }
        catch {
            $_.Exception.Message | Should -BeExactly 'Could not find a suitable generic method overload for "Empty" with "2" type parameters, and the argument count: "0".'
            $_.FullyQualifiedErrorId | Should -BeExactly 'MethodCountCouldNotFindBestGeneric'
        }
    }

    It 'gives a runtime error if a nonexistent type is specified' {
        try {
            [array]::Empty[thisdoesnotexist]()
        }
        catch {
            $_.Exception.Message | Should -BeExactly 'One or more of the generic type parameters provided for the method "Empty" refers to a type which cannot be found.'
            $_.FullyQualifiedErrorId | Should -BeExactly 'TypeNotFoundForGenericMethod'
        }
    }

    It 'lists the same method group information with and without type parameters' {
        $dict = [System.Collections.Concurrent.ConcurrentDictionary[string, int]]::new()
        $noTypeParams = $dict.AddOrUpdate
        $typeParams = $dict.AddOrUpdate[string]
        $extraTypeParams = $dict.AddOrUpdate[string, int]

        $typeParams.OverloadDefinitions | Should -BeExactly $noTypeParams.OverloadDefinitions
        $extraTypeParams.OverloadDefinitions | Should -BeExactly $typeParams.OverloadDefinitions
    }

    It 'successfully invokes common Linq generic methods' {
        [System.Collections.Generic.List[int]]$list = @( 1, 2, 3, 4, 5 )
        $result = [System.Linq.Enumerable]::Select[int, float](
            $list,
            [Func[int, float]]{
                param($item)
                [math]::Pow($item, 3)
            }
        )

        $result.GetType().Name | Should -BeExactly 'SelectListIterator`2'
        $typeArgs = $result.GetType().GenericTypeArguments
        $typeArgs[0] | Should -Be ([int])
        $typeArgs[1] | Should -Be ([float])

        $resultList = $result.ToList()
        $resultList.GetType().Name | Should -Be 'List`1'
        $resultList.GetType().GenericTypeArguments | Should -Be ([float])
        $resultList | Should -Be @( 1, 8, 27, 64, 125)
    }
}
