
Describe 'Argument transformation attribute on optional argument with explicit $null' -Tags "P1", "RI" {
    $mod = Add-Type -PassThru -TypeDefinition @'
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


    Invoke-ScriptFunctionTakesObject | Should Be 42
    Invoke-ScriptFunctionTakesUInt64 | Should Be 42
    Invoke-CSharpCmdletTakesObject | Should Be "passed in null"
    Invoke-CSharpCmdletTakesUInt64 | Should Be 0

    Invoke-ScriptFunctionTakesObject -Address $null | Should Be 42
    Invoke-ScriptFunctionTakesUInt64 -Address $null | Should Be 42
    Invoke-CSharpCmdletTakesObject -Address $null | Should Be 42
    Invoke-CSharpCmdletTakesUInt64 -Address $null | Should Be 42
}
