
Describe 'Argument transformation attribute on optional argument with explicit $null' -Tags "CI" {
    $tdefinition = @'
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
    $mod = Add-Type -PassThru -TypeDefinition $tdefinition

    Import-Module $mod[0].Assembly -ErrorVariable ErrorImportingModule

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


    It "There was no error importing the in-memory module" {
        $ErrorImportingModule | Should Be $null
    }

    It "Script function takes object" {
        Invoke-ScriptFunctionTakesObject | Should Be 42
    }
    It "Script function takes uint64" {
        Invoke-ScriptFunctionTakesUInt64 | Should Be 42
    }
    it "csharp cmdlet takes object" {
        Invoke-CSharpCmdletTakesObject | Should Be "passed in null"
    }
    it "csharp cmdlet takes uint64" {
        Invoke-CSharpCmdletTakesUInt64 | Should Be 0
    }

    it "script function takes object when parameter is null" {
        Invoke-ScriptFunctionTakesObject -Address $null | Should Be 42
    }
    it "script function takes unit64 when parameter is null" {
        Invoke-ScriptFunctionTakesUInt64 -Address $null | Should Be 42
    }
    it "script csharp cmdlet takes object when parameter is null" {
        Invoke-CSharpCmdletTakesObject -Address $null | Should Be 42
    }
    it "script csharp cmdlet takes uint64 when parameter is null" {
        Invoke-CSharpCmdletTakesUInt64 -Address $null | Should Be 42
    }
}
