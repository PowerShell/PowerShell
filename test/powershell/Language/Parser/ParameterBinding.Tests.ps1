# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

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
        $ErrorImportingModule | Should -BeNullOrEmpty
    }

    It "Script function takes object" {
        Invoke-ScriptFunctionTakesObject | Should -Be 42
    }
    It "Script function takes uint64" {
        Invoke-ScriptFunctionTakesUInt64 | Should -Be 42
    }
    it "csharp cmdlet takes object" {
        Invoke-CSharpCmdletTakesObject | Should -Be "passed in null"
    }
    it "csharp cmdlet takes uint64" {
        Invoke-CSharpCmdletTakesUInt64 | Should -Be 0
    }

    it "script function takes object when parameter is null" {
        Invoke-ScriptFunctionTakesObject -Address $null | Should -Be 42
    }
    it "script function takes unit64 when parameter is null" {
        Invoke-ScriptFunctionTakesUInt64 -Address $null | Should -Be 42
    }
    it "script csharp cmdlet takes object when parameter is null" {
        Invoke-CSharpCmdletTakesObject -Address $null | Should -Be 42
    }
    it "script csharp cmdlet takes uint64 when parameter is null" {
        Invoke-CSharpCmdletTakesUInt64 -Address $null | Should -Be 42
    }
}

Describe "Custom type conversion in parameter binding" -Tags 'Feature' {
    BeforeAll {
        ## Prepare the script module
        $content = @'
        function Test-ScriptCmdlet {
            [CmdletBinding(DefaultParameterSetName = "File")]
            param(
                [Parameter(Mandatory, ParameterSetName = "File")]
                [System.IO.FileInfo] $File,

                [Parameter(Mandatory, ParameterSetName = "StartInfo")]
                [System.Diagnostics.ProcessStartInfo] $StartInfo
            )

            if ($PSCmdlet.ParameterSetName -eq "File") {
                $File.Name
            } else {
                $StartInfo.FileName
            }
        }

        function Test-ScriptFunction {
            param(
                [System.IO.FileInfo] $File,
                [System.Diagnostics.ProcessStartInfo] $StartInfo
            )

            if ($null -ne $File) {
                $File.Name
            }
            if ($null -ne $StartInfo) {
                $StartInfo.FileName
            }
        }
'@
        Set-Content -Path $TestDrive\module.psm1 -Value $content -Force

        ## Prepare the C# module
        $code = @'
        using System.IO;
        using System.Diagnostics;
        using System.Management.Automation;

        namespace Test
        {
            [Cmdlet("Test", "BinaryCmdlet", DefaultParameterSetName = "File")]
            public class TestCmdletCommand : PSCmdlet
            {
                [Parameter(Mandatory = true, ParameterSetName = "File")]
                public FileInfo File { get; set; }

                [Parameter(Mandatory = true, ParameterSetName = "StartInfo")]
                public ProcessStartInfo StartInfo { get; set; }

                protected override void ProcessRecord()
                {
                    if (this.ParameterSetName == "File")
                    {
                        WriteObject(File.Name);
                    }
                    else
                    {
                        WriteObject(StartInfo.FileName);
                    }
                }
            }
        }
'@
        $asmFile = [System.IO.Path]::GetTempFileName() + ".dll"
        Add-Type -TypeDefinition $code -OutputAssembly $asmFile

        ## Helper function to execute script
        function Execute-Script {
            [CmdletBinding(DefaultParameterSetName = "Script")]
            param(
                [Parameter(Mandatory)]
                [powershell]$ps,

                [Parameter(Mandatory, ParameterSetName = "Script")]
                [string]$Script,

                [Parameter(Mandatory, ParameterSetName = "Command")]
                [string]$Command,

                [Parameter(Mandatory, ParameterSetName = "Command")]
                [string]$ParameterName,

                [Parameter(Mandatory, ParameterSetName = "Command")]
                [object]$Argument
            )
            $ps.Commands.Clear()
            $ps.Streams.ClearStreams()

            if ($PSCmdlet.ParameterSetName -eq "Script") {
                $ps.AddScript($Script).Invoke()
            } else {
                $ps.AddCommand($Command).AddParameter($ParameterName, $Argument).Invoke()
            }
        }

        ## Helper command strings
        $changeToConstrainedLanguage = '$ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"'
        $getLanguageMode = '$ExecutionContext.SessionState.LanguageMode'
        $importScriptModule = "Import-Module $TestDrive\module.psm1"
        $importCSharpModule = "Import-Module $asmFile"
    }

    AfterAll {
        ## Set the LanguageMode to force rebuilding the type conversion cache.
        ## This is needed because type conversions happen in the new powershell runspace with 'ConstrainedLanguage' mode
        ## will be put in the type conversion cache, and that may affect the default session.
        $ExecutionContext.SessionState.LanguageMode = "FullLanguage"
    }

    It "Custom type conversion in parameter binding is allowed in FullLanguage" {
        ## Create a powershell instance for the test
        $ps = [powershell]::Create()
        try {
            ## Import the modules in FullLanguage mode
            Execute-Script -ps $ps -Script $importScriptModule
            Execute-Script -ps $ps -Script $importCSharpModule

            $languageMode = Execute-Script -ps $ps -Script $getLanguageMode
            $languageMode | Should Be 'FullLanguage'

            $result1 = Execute-Script -ps $ps -Script "Test-ScriptCmdlet -File fileToUse"
            $result1 | Should Be "fileToUse"

            $result2 = Execute-Script -ps $ps -Script "Test-ScriptFunction -File fileToUse"
            $result2 | Should Be "fileToUse"

            $result3 = Execute-Script -ps $ps -Script "Test-BinaryCmdlet -File fileToUse"
            $result3 | Should Be "fileToUse"

            ## Conversion involves setting properties of an instance of the target type is allowed in FullLanguage mode
            $hashValue = @{ FileName = "filename"; Arguments = "args" }
            $psobjValue = [PSCustomObject] $hashValue

            ## Test 'Test-ScriptCmdlet -StartInfo' with IDictionary and PSObject with properties
            $result4 = Execute-Script -ps $ps -Command "Test-ScriptCmdlet" -ParameterName "StartInfo" -Argument $hashValue
            $result4 | Should Be "filename"
            $result5 = Execute-Script -ps $ps -Command "Test-ScriptCmdlet" -ParameterName "StartInfo" -Argument $psobjValue
            $result5 | Should Be "filename"

            ## Test 'Test-ScriptFunction -StartInfo' with IDictionary and PSObject with properties
            $result6 = Execute-Script -ps $ps -Command "Test-ScriptFunction" -ParameterName "StartInfo" -Argument $hashValue
            $result6 | Should Be "filename"
            $result7 = Execute-Script -ps $ps -Command "Test-ScriptFunction" -ParameterName "StartInfo" -Argument $psobjValue
            $result7 | Should Be "filename"

            ## Test 'Test-BinaryCmdlet -StartInfo' with IDictionary and PSObject with properties
            $result8 = Execute-Script -ps $ps -Command "Test-BinaryCmdlet" -ParameterName "StartInfo" -Argument $hashValue
            $result8 | Should Be "filename"
            $result9 = Execute-Script -ps $ps -Command "Test-BinaryCmdlet" -ParameterName "StartInfo" -Argument $psobjValue
            $result9 | Should Be "filename"
        }
        finally {
            $ps.Dispose()
        }
    }

    It "Some custom type conversion in parameter binding is allowed for trusted cmdlets in ConstrainedLanguage" {
        ## Create a powershell instance for the test
        $ps = [powershell]::Create()
        try {
            ## Import the modules in FullLanguage mode
            Execute-Script -ps $ps -Script $importScriptModule
            Execute-Script -ps $ps -Script $importCSharpModule

            $languageMode = Execute-Script -ps $ps -Script $getLanguageMode
            $languageMode | Should Be 'FullLanguage'

            ## Change to ConstrainedLanguage mode
            Execute-Script -ps $ps -Script $changeToConstrainedLanguage
            $languageMode = Execute-Script -ps $ps -Script $getLanguageMode
            $languageMode | Should Be 'ConstrainedLanguage'

            $result1 = Execute-Script -ps $ps -Script "Test-ScriptCmdlet -File fileToUse"
            $result1 | Should Be "fileToUse"

            $result2 = Execute-Script -ps $ps -Script "Test-ScriptFunction -File fileToUse"
            $result2 | Should Be "fileToUse"

            $result3 = Execute-Script -ps $ps -Script "Test-BinaryCmdlet -File fileToUse"
            $result3 | Should Be "fileToUse"

            ## If the conversion involves setting properties of an instance of the target type,
            ## then it's disallowed even for trusted cmdlets.
            $hashValue = @{ FileName = "filename"; Arguments = "args" }
            $psobjValue = [PSCustomObject] $hashValue

            ## Test 'Test-ScriptCmdlet -StartInfo' with IDictionary and PSObject with properties
            try {
                Execute-Script -ps $ps -Command "Test-ScriptCmdlet" -ParameterName "StartInfo" -Argument $hashValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingArgumentTransformationException,Execute-Script"
            }

            try {
                Execute-Script -ps $ps -Command "Test-ScriptCmdlet" -ParameterName "StartInfo" -Argument $psobjValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingArgumentTransformationException,Execute-Script"
            }

            ## Test 'Test-ScriptFunction -StartInfo' with IDictionary and PSObject with properties
            try {
                Execute-Script -ps $ps -Command "Test-ScriptFunction" -ParameterName "StartInfo" -Argument $hashValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingArgumentTransformationException,Execute-Script"
            }

            try {
                Execute-Script -ps $ps -Command "Test-ScriptFunction" -ParameterName "StartInfo" -Argument $psobjValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingArgumentTransformationException,Execute-Script"
            }

            ## Test 'Test-BinaryCmdlet -StartInfo' with IDictionary and PSObject with properties
            try {
                Execute-Script -ps $ps -Command "Test-BinaryCmdlet" -ParameterName "StartInfo" -Argument $hashValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingException,Execute-Script"
            }

            try {
                Execute-Script -ps $ps -Command "Test-BinaryCmdlet" -ParameterName "StartInfo" -Argument $psobjValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingException,Execute-Script"
            }
        }
        finally {
            $ps.Dispose()
        }
    }

    It "Custom type conversion in parameter binding is NOT allowed for untrusted cmdlets in ConstrainedLanguage" {
        ## Create a powershell instance for the test
        $ps = [powershell]::Create()
        try {
            $languageMode = Execute-Script -ps $ps -Script $getLanguageMode
            $languageMode | Should Be 'FullLanguage'

            ## Change to ConstrainedLanguage mode
            Execute-Script -ps $ps -Script $changeToConstrainedLanguage
            $languageMode = Execute-Script -ps $ps -Script $getLanguageMode
            $languageMode | Should Be 'ConstrainedLanguage'

            ## Import the modules in ConstrainedLanguage mode
            Execute-Script -ps $ps -Script $importScriptModule
            Execute-Script -ps $ps -Script $importCSharpModule

            $result1 = Execute-Script -ps $ps -Script "Test-ScriptCmdlet -File fileToUse"
            $result1 | Should Be $null
            $ps.Streams.Error.Count | Should Be 1
            $ps.Streams.Error[0].FullyQualifiedErrorId | Should Be "ParameterArgumentTransformationError,Test-ScriptCmdlet"

            $result2 = Execute-Script -ps $ps -Script "Test-ScriptFunction -File fileToUse"
            $result2 | Should Be $null
            $ps.Streams.Error.Count | Should Be 1
            $ps.Streams.Error[0].FullyQualifiedErrorId | Should Be "ParameterArgumentTransformationError,Test-ScriptFunction"

            ## Binary cmdlets are always marked as trusted because only trusted assemblies can be loaded on DeviceGuard machine.
            $result3 = Execute-Script -ps $ps -Script "Test-BinaryCmdlet -File fileToUse"
            $result3 | Should Be "fileToUse"

            ## Conversion that involves setting properties of an instance of the target type is disallowed.
            $hashValue = @{ FileName = "filename"; Arguments = "args" }
            $psobjValue = [PSCustomObject] $hashValue

            ## Test 'Test-ScriptCmdlet -StartInfo' with IDictionary and PSObject with properties
            try {
                Execute-Script -ps $ps -Command "Test-ScriptCmdlet" -ParameterName "StartInfo" -Argument $hashValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingArgumentTransformationException,Execute-Script"
            }

            try {
                Execute-Script -ps $ps -Command "Test-ScriptCmdlet" -ParameterName "StartInfo" -Argument $psobjValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingArgumentTransformationException,Execute-Script"
            }

            ## Test 'Test-ScriptFunction -StartInfo' with IDictionary and PSObject with properties
            try {
                Execute-Script -ps $ps -Command "Test-ScriptFunction" -ParameterName "StartInfo" -Argument $hashValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingArgumentTransformationException,Execute-Script"
            }

            try {
                Execute-Script -ps $ps -Command "Test-ScriptFunction" -ParameterName "StartInfo" -Argument $psobjValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingArgumentTransformationException,Execute-Script"
            }

            ## Test 'Test-BinaryCmdlet -StartInfo' with IDictionary and PSObject with properties
            try {
                Execute-Script -ps $ps -Command "Test-BinaryCmdlet" -ParameterName "StartInfo" -Argument $hashValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingException,Execute-Script"
            }

            try {
                Execute-Script -ps $ps -Command "Test-BinaryCmdlet" -ParameterName "StartInfo" -Argument $psobjValue
                throw "Expected exception was not thrown!"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "ParameterBindingException,Execute-Script"
            }
        }
        finally {
            $ps.Dispose()
        }
    }
}
