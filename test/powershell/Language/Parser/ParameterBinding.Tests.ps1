
Describe 'Argument transformation attribute on optional argument with explicit $null' -Tags "Feature" {
    $definition = @'
    using System;
    using System.Management.Automation;
    using System.Reflection;

    namespace MSFT_1407291
    {
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
        public class AddressTransformationAttribute : ArgumentTransformationAttribute
        {
            public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
            {
                return (ulong) 42;
            }
        }

        [Cmdlet(VerbsLifecycle.Invoke, "CSharpCmdletTakesUInt64")]
        [OutputType(typeof(System.String))]
        public class Cmdlet1 : PSCmdlet
        {
            [Parameter(Mandatory = false)]
            [AddressTransformation]
            public ulong Address { get; set; }

            protected override void ProcessRecord()
            {
                WriteObject(Address);
            }
        }

        [Cmdlet(VerbsLifecycle.Invoke, "CSharpCmdletTakesObject")]
        [OutputType(typeof(System.String))]
        public class Cmdlet2 : PSCmdlet
        {
            [Parameter(Mandatory = false)]
            [AddressTransformation]
            public object Address { get; set; }

            protected override void ProcessRecord()
            {
                WriteObject(Address ?? "passed in null");
            }
        }
    }
'@

    if ( $IsCore ) {
        $mod = Add-Type -PassThru -TypeDefinition $definition -ref mscorlib,System.Management.Automation
    }
    else {
        $mod = Add-Type -PassThru -TypeDefinition $definition
    }
    Import-Module $mod[0].Assembly

    function Invoke-ScriptFunctionTakesObject
    {
        param([MSFT_1407291.AddressTransformation()]
              [Parameter(Mandatory = $false)]
              [object]$Address = "passed in null")

        return $Address
    }

    function Invoke-ScriptFunctionTakesUInt64
    {
        param([MSFT_1407291.AddressTransformation()]
              [Parameter(Mandatory = $false)]
              [Uint64]$Address = 11)

        return $Address
    }


    $testcases = 
        @{ Command = "Invoke-ScriptFunctionTakesObject"; myargs = @{};                 Result = 42 },
        @{ Command = "Invoke-ScriptFunctionTakesUInt64"; myargs = @{};                 Result = 42 },
        @{ Command = "Invoke-CSharpCmdletTakesObject"  ; myargs = @{};                 Result = "passed in null" },
        @{ Command = "Invoke-CSharpCmdletTakesUInt64"  ; myargs = @{};                 Result = 0 },
        @{ Command = "Invoke-ScriptFunctionTakesObject"; myargs = @{ Address = $null }; Result = 42},
        @{ Command = "Invoke-ScriptFunctionTakesUInt64"; myargs = @{ Address = $null }; Result = 42},
        @{ Command = "Invoke-CSharpCmdletTakesObject"  ; myargs = @{ Address = $null }; Result = 42},
        @{ Command = "Invoke-CSharpCmdletTakesUInt64";   myargs = @{ Address = $null }; Result = 42}

    It "<command> should return '<result>'" -testcases $testcases {
        param ( $command, $result,$myargs )
        & $command @myargs | Should be $result
    }

}
