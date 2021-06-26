# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Parameter Binding Tests" -Tags "CI" {
    It "Should throw a parameter binding exception when two parameters have the same position" {
        function test-PositionalBinding1 {
            [CmdletBinding()]
            param (
            [Parameter(Position = 0)] [int]$Parameter1 = 0,
            [Parameter(Position = 0)] [int]$Parameter2 = 0
            )

            Process {
                return $true
            }
        }

        { test-PositionalBinding1 1 } | Should -Throw -ErrorId "AmbiguousPositionalParameterNoName,test-PositionalBinding1"
    }

    It "a mandatory parameter can't be passed a null if it doesn't have AllowNullAttribute" {
        function test-allownullattributes {
            [CmdletBinding()]
            param (
            [string]$Parameter1 = "default1",
            [Parameter(Mandatory = $true)] [string]$Parameter2 = "default2",
            [Parameter(Mandatory = $true)] [string]$Parameter3 = "default3",
            [AllowNull()] [int]$Parameter4 = 0,
            [AllowEmptyString()][int]$Parameter5 = 0,
            [Parameter(Mandatory = $true)] [int]$ShowMe = 0
            )

            Process {
                switch ( $ShowMe )
                {
                    1 {
                        return $Parameter1
                        break
                        }
                    2 {
                        return $Parameter2
                        break
                        }
                    3 {
                        return $Parameter3
                        break
                        }
                    4 {
                        return $Parameter4
                        break
                        }
                    5 {
                        return $Parameter5
                        break
                        }
                }
            }
        }

        $e = { test-allownullattributes -Parameter2 1 -Parameter3 $null -ShowMe 1 } |
            Should -Throw -ErrorId "ParameterArgumentValidationErrorEmptyStringNotAllowed,test-allownullattributes" -PassThru
        $e.CategoryInfo | Should -Match "ParameterBindingValidationException"
        $e.Exception.Message | Should -Match "Parameter3"
    }

    It "can't pass an argument that looks like a boolean parameter to a named string parameter" {
        function test-namedwithboolishargument {
            [CmdletBinding()]
            param (
            [bool] $Parameter1 = $false,
            [Parameter(Position = 0)] [string]$Parameter2 = ""
            )

            Process {
                return $Parameter2
            }
        }

        $e = { test-namedwithboolishargument -Parameter2 -Parameter1 } | Should -Throw -ErrorId "MissingArgument,test-namedwithboolishargument" -PassThru
        $e.CategoryInfo | Should -Match "ParameterBindingException"
        $e.Exception.Message | Should -Match "Parameter2"
    }

    It "Verify that a SwitchParameter's IsPresent member is false if the parameter is not specified" {
        function test-singleswitchparameter {
            [CmdletBinding()]
            param (
            [switch]$Parameter1
            )

            Process {
                return $Parameter1.IsPresent
            }
        }

        $result = test-singleswitchparameter
        $result | Should -BeFalse
    }

    It "Verify that a bool parameter returns proper value" {
        function test-singleboolparameter {
            [CmdletBinding()]
            param (
            [bool]$Parameter1 = $false
            )

            Process {
                return $Parameter1
            }
        }

        $result1 = test-singleboolparameter
        $result1 | Should -BeFalse

        $result2 = test-singleboolparameter -Parameter1:1
        $result2 | Should -BeTrue
    }

    It "Should throw a exception when passing a string that can't be parsed by Int" {
        function test-singleintparameter {
            [CmdletBinding()]
            param (
            [int]$Parameter1 = 0
            )

            Process {
                return $Parameter1
            }
        }

        $e = { test-singleintparameter -Parameter1 'exampleInvalidParam' } |
            Should -Throw -ErrorId "ParameterArgumentTransformationError,test-singleintparameter" -PassThru
        $e.CategoryInfo | Should -Match "ParameterBindingArgumentTransformationException"
        $e.Exception.Message | Should -Match "Input string was not in a correct format"
        $e.Exception.Message | Should -Match "Parameter1"
    }

    It "Verify that WhatIf is available when SupportShouldProcess is true" {
        function test-supportsshouldprocess2 {
            [CmdletBinding(SupportsShouldProcess = $true)]
            Param ()

            Process {
                return 1
            }
        }

        $result = test-supportsshouldprocess2 -Whatif
        $result | Should -Be 1
    }

    It "Verify that ValueFromPipeline takes precedence over ValueFromPipelineByPropertyName without type coercion" {
        function test-bindingorder2 {
            [CmdletBinding()]
            param (
            [Parameter(ValueFromPipeline = $true, ParameterSetName = "one")] [string]$Parameter1 = "",
            [Parameter(ValueFromPipelineByPropertyName = $true, ParameterSetName = "two")] [int]$Length = 0
            )

            Process {
                return "$Parameter1 - $Length"
            }
        }

        $result = '0123' | test-bindingorder2
        $result | Should -Be "0123 - 0"
    }

    It "Verify that a ScriptBlock object can be delay-bound to a parameter of type FileInfo with pipeline input" {
        function test-scriptblockbindingfrompipeline {
            [CmdletBinding()]
            param (
            [Parameter(ValueFromPipeline = $true)] [System.IO.FileInfo]$Parameter1
            )

            Process {
                return $Parameter1.Name
            }
        }
        $testFile = Join-Path $TestDrive -ChildPath "testfile.txt"
        New-Item -Path $testFile -ItemType file -Force
        $result = Get-Item $testFile | test-scriptblockbindingfrompipeline -Parameter1 {$_}
        $result | Should -Be "testfile.txt"
    }

    It "Verify that a dynamic parameter named WhatIf doesn't conflict if SupportsShouldProcess is false" {
        function test-dynamicparameters3 {
            [CmdletBinding(SupportsShouldProcess = $false)]
            param (
            [Parameter(ParameterSetName = "one")] [int]$Parameter1 = 0,
            [Parameter(ParameterSetName = "two")] [int]$Parameter2 = 0,
            [int]$WhatIf = 0
            )
        }

        { test-dynamicparameters3 -Parameter1 1 } | Should -Not -Throw
    }

    It "Verify that an int can be bound to a parameter of type Array" {
        function test-collectionbinding1 {
            [CmdletBinding()]
            param (
            [array]$Parameter1,
            [int[]]$Parameter2
            )

            Process {
                $result = ""
                if($null -ne $Parameter1)
                {
                    $result += " P1"
                    foreach ($object in $Parameter1)
                    {
                        $result = $result + ":" + $object.GetType().Name + "," + $object
                    }
                }
                if($null -ne $Parameter2)
                {
                    $result += " P2"
                    foreach ($object in $Parameter2)
                    {
                        $result = $result + ":" + $object.GetType().Name + "," + $object
                    }
                }
                return $result.Trim()
            }
        }

        $result = test-collectionbinding1 -Parameter1 1 -Parameter2 2
        $result | Should -Be "P1:Int32,1 P2:Int32,2"
    }

    It "Verify that a dynamic parameter and an alias can't have the same name" {
        function test-nameconflicts6 {
            [CmdletBinding()]
            param (
            [Alias("Parameter2")]
            [int]$Parameter1 = 0,
            [int]$Parameter2 = 0
            )
        }

        $e = { test-nameconflicts6 -Parameter2 1 } | Should -Throw -ErrorId "ParameterNameConflictsWithAlias" -PassThru
        $e.CategoryInfo | Should -Match "MetadataException"
        $e.Exception.Message | Should -Match "Parameter1"
        $e.Exception.Message | Should -Match "Parameter2"
    }

    It "PipelineVariable shouldn't cause a NullRef exception when 'DynamicParam' block is present" {
        function DynamicParamTest {
            [CmdletBinding()]
            param()
            dynamicparam { }
            process { 'hi' }
        }

        DynamicParamTest -PipelineVariable bar | ForEach-Object { $bar } | Should -Be "hi"
    }

    It 'Dynamic parameter is found even if globbed path does not exist' {
        $guid = New-Guid

        # This test verifies that the ErrorRecord is coming from parameter validation on a dynamic parameter
        # instead of an error indicating that the dynamic parameter is not found
        { Copy-Item "~\$guid*" -Destination ~ -ToSession $null } | Should -Throw -ErrorId 'ParameterArgumentValidationError'
    }

    Context "PipelineVariable Behaviour" {

        BeforeAll {
            function Write-PipelineVariable {
                [CmdletBinding()]
                [OutputType([int])]
                param(
                    [Parameter(ValueFromPipeline)]
                    $a
                )
                begin { 1 }
                process { 2 }
                end { 3 }
            }

            $testScripts = @(
                @{
                    CmdletType = 'Script Cmdlet'
                    Script     = {
                        1..3 |
                            Write-PipelineVariable -PipelineVariable pipe |
                            Select-Object -Property @(
                                @{ Name = "PipelineVariableSet"; Expression = { $null -ne $pipe ? $true : $false } }
                                @{ Name = "PipelineVariable"; Expression = { $pipe } }
                            )
                    }
                }
                @{
                    CmdletType = 'Compiled Cmdlet'
                    Script     = {
                        1..3 |
                            Write-PipelineVariable |
                            ForEach-Object { $_ } -PipelineVariable pipe |
                            Select-Object -Property @(
                                @{ Name = "PipelineVariableSet"; Expression = { $null -ne $pipe ? $true : $false } }
                                @{ Name = "PipelineVariable"; Expression = { $pipe } }
                            )
                    }
                }
            )
        }

        AfterAll {
            Remove-Item -Path 'function:Write-PipelineVariable'
        }

        It 'should set the pipeline variable every time for a <CmdletType>' -TestCases $testScripts {
            param($Script, $CmdletType)

            $result = & $Script
            $result.Count | Should -Be 5
            $result.PipelineVariableSet | Should -Not -Contain $false
            $result.PipelineVariable | Should -Be 1, 2, 2, 2, 3
        }
    }

    Context "Use automatic variables as default value for parameters" {
        BeforeAll {
            ## Explicit use of 'CmdletBinding' make it a script cmdlet
            $test1 = @'
                [CmdletBinding()]
                param ($Root = $PSScriptRoot)
                "[$Root]"
'@
            ## Use of 'Parameter' implicitly make it a script cmdlet
            $test2 = @'
                param (
                    [Parameter()]
                    $Root = $PSScriptRoot
                )
                "[$Root]"
'@
            $tempDir = Join-Path -Path $TestDrive -ChildPath "DefaultValueTest"
            $test1File = Join-Path -Path $tempDir -ChildPath "test1.ps1"
            $test2File = Join-Path -Path $tempDir -ChildPath "test2.ps1"

            $expected = "[$tempDir]"
            $pwsh = "$PSHOME\pwsh"

            $null = New-Item -Path $tempDir -ItemType Directory -Force
            Set-Content -Path $test1File -Value $test1 -Force
            Set-Content -Path $test2File -Value $test2 -Force
        }

        AfterAll {
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Test dot-source should evaluate '`$PSScriptRoot' for parameter default value" {
            $result = . $test1File
            $result | Should -Be $expected

            $result = . $test2File
            $result | Should -Be $expected
        }

        It "Test 'powershell -File' should evaluate '`$PSScriptRoot' for parameter default value" {
            $result = & $pwsh -NoProfile -File $test1File
            $result | Should -Be $expected

            $result = & $pwsh -NoProfile -File $test2File
            $result | Should -Be $expected
        }
    }

    Context "ValueFromRemainingArguments" {
        BeforeAll {
            function Test-BindingFunction {
                param (
                    [Parameter(ValueFromRemainingArguments)]
                    [object[]] $Parameter
                )

                return [pscustomobject] @{
                    ArgumentCount = $Parameter.Count
                    Value = $Parameter
                }
            }

            # Deliberately not using TestDrive:\ here because Pester will fail to clean it up due to the
            # assembly being loaded in our process.

            if ($IsWindows)
            {
                $tempDir = $env:temp
            }
            else
            {
                $tempDir = '/tmp'
            }

            $dllPath = Join-Path $tempDir TestBindingCmdlet.dll

            $typeDefinition = '
                using System;
                using System.Management.Automation;

                [Cmdlet("Test", "BindingCmdlet")]
                public class TestBindingCommand : PSCmdlet
                {
                    [Parameter(Position = 0, ValueFromRemainingArguments = true)]
                    public string[] Parameter { get; set; }

                    protected override void ProcessRecord()
                    {
                        PSObject obj = new PSObject();

                        obj.Properties.Add(new PSNoteProperty("ArgumentCount", Parameter.Length));
                        obj.Properties.Add(new PSNoteProperty("Value", Parameter));

                        WriteObject(obj);
                    }
                }
            '
            if ( !(Test-Path $dllPath))
            {
                Add-Type -OutputAssembly $dllPath -TypeDefinition $typeDefinition
            }

            Import-Module $dllPath
        }

        AfterAll {
            Get-Module TestBindingCmdlet | Remove-Module -Force
        }

        It "Binds properly when passing an explicit array to an advanced function" {
            $result = Test-BindingFunction 1,2,3

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when passing multiple arguments to an advanced function" {
            $result = Test-BindingFunction 1 2 3

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when passing an explicit array to a cmdlet" {
            $result = Test-BindingCmdlet 1,2,3

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when passing multiple arguments to a cmdlet" {
            $result = Test-BindingCmdlet 1 2 3

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when collections of type other than object[] are used on an advanced function" {
            $list = [Collections.Generic.List[int]](1..3)
            $result = Test-BindingFunction $list

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }

        It "Binds properly when collections of type other than object[] are used on a cmdlet" {
            $list = [Collections.Generic.List[int]](1..3)
            $result = Test-BindingCmdlet $list

            $result.ArgumentCount | Should -Be 3
            $result.Value[0] | Should -Be 1
            $result.Value[1] | Should -Be 2
            $result.Value[2] | Should -Be 3
        }
    }
}
