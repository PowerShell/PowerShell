# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
using namespace System.Management.Automation
using namespace System.Collections.Generic

Describe "Type inference Tests" -tags "CI" {
    BeforeAll {
        $ati = [Cmdlet].Assembly.GetType("System.Management.Automation.AstTypeInference")
        $inferType = $ati.GetMethods().Where{$_.Name -ceq "InferTypeOf"}
        $m1 = 'System.Collections.Generic.IList`1[System.Management.Automation.PSTypeName] InferTypeOf(System.Management.Automation.Language.Ast)'
        $m2 = 'System.Collections.Generic.IList`1[System.Management.Automation.PSTypeName] InferTypeOf(System.Management.Automation.Language.Ast, System.Management.Automation.TypeInferenceRuntimePermissions)'
        $m3 = 'System.Collections.Generic.IList`1[System.Management.Automation.PSTypeName] InferTypeOf(System.Management.Automation.Language.Ast, System.Management.Automation.PowerShell)'
        $m4 = 'System.Collections.Generic.IList`1[System.Management.Automation.PSTypeName] InferTypeOf(System.Management.Automation.Language.Ast, System.Management.Automation.PowerShell, System.Management.Automation.TypeInferenceRuntimePermissions)'

        $inferTypeOf1 = $inferType.Where{$m1 -eq $_}[0]
        $inferTypeOf2 = $inferType.Where{$m2 -eq $_}[0]
        $inferTypeOf3 = $inferType.Where{$m3 -eq $_}[0]
        $inferTypeOf4 = $inferType.Where{$m4 -eq $_}[0]

        class AstTypeInference {
            static [IList[PSTypeName]] InferTypeOf([Language.Ast] $ast) {
                return  $script:inferTypeOf1.Invoke($null, $ast)
            }

            static [IList[PSTypeName]] InferTypeOf([Language.Ast] $ast, [System.Management.Automation.TypeInferenceRuntimePermissions] $runtimePermissions) {
                return $script:inferTypeOf2.Invoke($null, @($ast, $runtimePermissions))
            }

            static [IList[PSTypeName]] InferTypeOf([Language.Ast] $ast, [System.Management.Automation.PowerShell] $powershell) {
                return $script:inferTypeOf3.Invoke($null, @($ast, $powershell))
            }

            static [IList[PSTypeName]] InferTypeOf([Language.Ast] $ast, [PowerShell] $powerShell, [System.Management.Automation.TypeInferenceRuntimePermissions] $runtimePermissions) {
                return $script:inferTypeOf4.Invoke($null, @($ast, $powerShell, $runtimePermissions))
            }
        }
    }

    It "Infers type from integer" {
        $res = [AstTypeInference]::InferTypeOf( { 1 }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It "Infers type from string literal" {
        $res = [AstTypeInference]::InferTypeOf( { "Text" }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String'
    }

    It "Infers type from type expression" {
        $res = [AstTypeInference]::InferTypeOf( { [int] }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Type'
    }

    It "Infers type from hashtable" {
        $res = [AstTypeInference]::InferTypeOf( { @{} }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Collections.Hashtable'
    }

    It "Infers type from array expression" {
        $res = [AstTypeInference]::InferTypeOf( { @() }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.object[]'
    }

    It "Infers type from array expression with a single statement" {
        $res = [AstTypeInference]::InferTypeOf( { @('test') }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String[]'
    }

    It "Infers type from array expression with multiple statements" {
        $res = [AstTypeInference]::InferTypeOf( { @('test'; 'second test') }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String[]'
    }

    It "Infers type from array expression with mixed types" {
        $res = [AstTypeInference]::InferTypeOf( { @('test'; 1) }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Object[]'
    }

    It "Infers type from array expression with nested arrays" {
        $res = [AstTypeInference]::InferTypeOf( { @(@('test'); @('test2')) }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String[]'
    }

    It "Infers type from array expression with a non-generic dictionary enumerator" {
        $res = [AstTypeInference]::InferTypeOf( { @(@{}.GetEnumerator()) }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Collections.DictionaryEntry[]'
    }

    It "Infers type from array expression with a generic dictionary enumerator" {
        $res = [AstTypeInference]::InferTypeOf( { @([Dictionary[int, string]]::new().GetEnumerator()) }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be ([KeyValuePair[int, string][]].FullName)
    }

    It "Infers type from array expression with nested non-array collections" {
        $res = [AstTypeInference]::InferTypeOf( {
            $list = [List[string]]::new()
            $list2 = [List[string]]::new()
            @($list; $list2)
        }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String[]'
    }

    It "Infers type from Array literal" {
        $res = [AstTypeInference]::InferTypeOf( { , 1 }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32[]'
    }

    It "Infers type from Array literal with multiple elements" {
        $res = [AstTypeInference]::InferTypeOf( { 0, 1 }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32[]'
    }

    It "Infers type from Array literal with mixed types" {
        $res = [AstTypeInference]::InferTypeOf( { 'test', 1 }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Object[]'
    }

    It "Infers type from Array literal with nested arrays" {
        $res = [AstTypeInference]::InferTypeOf( { @('test'), @('test2') }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String[]'
    }

    It "Infers type from array expression with a non-generic dictionary enumerator" {
        $res = [AstTypeInference]::InferTypeOf( { , @{}.GetEnumerator() }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Collections.DictionaryEntry[]'
    }

    It "Infers type from array expression with a generic dictionary enumerator" {
        $res = [AstTypeInference]::InferTypeOf( { , [Dictionary[int, string]]::new().GetEnumerator() }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be ([KeyValuePair[int, string][]].FullName)
    }

    It "Infers type from Array literal with nested non-array collections" {
        $res = [AstTypeInference]::InferTypeOf( {
            $list = [List[string]]::new()
            $list2 = [List[string]]::new()
            $list, $list2
        }.Ast.EndBlock.Statements[2].PipelineElements[0].Expression)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String[]'
    }

    It "Infers type from array IndexExpresssion" {
        $res = [AstTypeInference]::InferTypeOf( { (1, 2, 3)[0] }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It "Infers type from generic container IndexExpression" {
        $res = [AstTypeInference]::InferTypeOf( {
                [System.Collections.Generic.List[int]]::new()[0]
            }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It 'Infers type of Index expression on Dictionary' {
        $ast = {
            [System.Collections.Generic.Dictionary[int, DateTime]]::new()[1]
        }.ast.EndBlock.Statements[0].PipelineElements[0].Expression
        $res = [AstTypeInference]::InferTypeOf( $ast )

        $res.Count | Should -Be 1
        $res.Name | Should -BeExactly 'System.DateTime'
    }

    It "Infers type from ScriptblockExpresssion" {
        $res = [AstTypeInference]::InferTypeOf( { {} }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Management.Automation.Scriptblock'
    }

    It "Infers type from paren expression" {
        $res = [AstTypeInference]::InferTypeOf( { (1) }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It "Infers type from expandable string expression" {
        $res = [AstTypeInference]::InferTypeOf( { "$(1)" }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String'
    }

    It "Infers type from cast expression" {
        $res = [AstTypeInference]::InferTypeOf( { [int] '1'}.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It "Infers type from using namespace" {
        $errors = $null
        $tokens = $null
        $ast = [Language.Parser]::ParseInput("using namespace System", [ref] $tokens, [ref] $errors)
        $res = [AstTypeInference]::InferTypeOf( $ast.Find( {param($a) $a -is [System.Management.Automation.Language.UsingStatementAst] }, $true))
        $res.Count | Should -Be 0
    }

    It "Infers type from unary expression" {
        $res = [AstTypeInference]::InferTypeOf( { !$true }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Boolean'
    }

    It "Infers type from param block" {
        $res = [AstTypeInference]::InferTypeOf( { param() }.Ast)
        $res.Count | Should -Be 0
    }

    It "Infers type from using statement" {
        $res = [AstTypeInference]::InferTypeOf( { $int = 1; $using:int }.Ast.EndBlock.Statements[1].PipelineElements[0].Expression)
        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It "Infers type from param block" {
        $res = [AstTypeInference]::InferTypeOf( { param([int] $i)}.Ast.ParamBlock)
        $res.Count | Should -Be 0
    }

    It "Infers type no type from Attribute" {
        $res = [AstTypeInference]::InferTypeOf( {
                [OutputType([int])]
                param(
                )}.Ast.ParamBlock.Attributes[0])
        $res.Count | Should -Be 0
    }

    It "Infers type no type from named Attribute argument" {
        $res = [AstTypeInference]::InferTypeOf( {
                [OutputType(Type = [int])]
                param(
                )}.Ast.ParamBlock.Attributes[0].NamedArguments[0])
        $res.Count | Should -Be 0
    }

    It "Infers type parameter types" {
        $res = [AstTypeInference]::InferTypeOf( {
                param([int] $i, [string] $s)
            }.Ast.ParamBlock.Parameters[0])
        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It "Infers type parameter from PSTypeNameAttribute type" -Skip:(!$IsWindows) {
        $res = [AstTypeInference]::InferTypeOf( {
                param([int] $i, [PSTypeName('System.Management.ManagementObject#root\cimv2\Win32_Process')] $s)
            }.Ast.ParamBlock.Parameters[1])
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Management.ManagementObject#root\cimv2\Win32_Process'
    }

    It "Infers type from DATA statement" {
        $res = [AstTypeInference]::InferTypeOf( {
                DATA {
                    "text"
                }
            }.Ast.EndBlock)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String'
    }

    It "Infers type from named block" {
        $res = [AstTypeInference]::InferTypeOf( { begin {1}}.Ast.BeginBlock)
        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It "Infers type from function definition" {
        $res = [AstTypeInference]::InferTypeOf( {
                function foo {
                    return 1
                }
            }.Ast.EndBlock)
        $res.Count | Should -Be 0
    }

    It "Infers type from convert expression" {
        $errors = $null
        $tokens = $null
        $ast = [Language.Parser]::ParseInput('[int] "4"', [ref] $tokens, [ref] $errors)
        $res = [AstTypeInference]::InferTypeOf( $ast.EndBlock.Statements[0])
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It "Infers type from type constraint" {
        $errors = $null
        $tokens = $null
        $ast = [Language.Parser]::ParseInput('[int] $i', [ref] $tokens, [ref] $errors)
        $res = [AstTypeInference]::InferTypeOf( $ast.EndBlock.Statements[0].PipelineElements[0].Expression.Attribute)
        $res.Count | Should -Be 0
    }

    It "Infers type from instance member property" {
        $res = [AstTypeInference]::InferTypeOf( { 'Text'.Length }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It "Infers type from static member property" {
        $res = [AstTypeInference]::InferTypeOf( { [DateTime]::Now }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.DateTime'
    }

    It "Infers type from instance member method" {
        $res = [AstTypeInference]::InferTypeOf( { [int[]].GetElementType() }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Type'
    }

    It "Infers type from static member method" {
        $res = [AstTypeInference]::InferTypeOf( { [powershell]::Create() }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Management.Automation.PowerShell'
    }

    It "Infers type from integer * stringliteral" {
        $res = [AstTypeInference]::InferTypeOf( {  5 * "5" }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It "Infers type from string literal" {
        $res = [AstTypeInference]::InferTypeOf( { "Text" }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String'
    }

    It "Infers type from stringliteral * integer" {
        $res = [AstTypeInference]::InferTypeOf( { "5" * 2 }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String'
    }

    It "Infers type from where-object of integer" {
        $res = [AstTypeInference]::InferTypeOf( { [int[]] $i = 1..20; $i | Where-Object {$_ -gt 10} }.Ast)
        foreach ($r in $res) {
            $r.Name -In 'System.Int32', 'System.Int32[]' | Should -BeTrue
        }
    }

    It "Infers type from foreach-object of integer" {
        $res = [AstTypeInference]::InferTypeOf( { [int[]] $i = 1..20; $i | ForEach-Object {$_ * 10} }.Ast)
        $res.Count | Should -Be 2
        foreach ($r in $res) {
            $r.Name -In 'System.Int32', 'System.Int32[]' | Should -BeTrue
        }
    }

    It "Infers type from generic new" {
        $res = [AstTypeInference]::InferTypeOf( { [System.Collections.Generic.List[int]]::new() }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Match 'System.Collections.Generic.List`1\[\[System.Int32.*'

    }

    It "Infers type from cim command"  -Skip:(!$IsWindows) {
        $res = [AstTypeInference]::InferTypeOf( { Get-CimInstance -Namespace root/CIMV2 -ClassName Win32_Bios }.Ast)
        $res.Count | Should -Be 2

        foreach ($r in $res) {
            $r.Name -In 'Microsoft.Management.Infrastructure.CimInstance#root/CIMV2/Win32_Bios',
            'Microsoft.Management.Infrastructure.CimInstance' | Should -BeTrue
        }
    }

    It "Infers type from foreach-object with begin/end" {
        $res = [AstTypeInference]::InferTypeOf( { [int[]] $i = 1..20; $i | ForEach-Object -Begin {"Hi"} {$_ * 10} -End {[int]} }.Ast)
        $res.Count | Should -Be 4
        foreach ($r in $res) {
            $r.Name -In 'System.Int32', 'System.Int32[]', 'System.String', 'System.Type' | Should -BeTrue
        }
    }

    It "Infers type from foreach-object with membername" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | ForEach-Object -MemberName Directory }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be "System.IO.DirectoryInfo"
    }

    It 'Infers typeof Foreach-Object -Member when Member is Property' {
        $ast = {Get-Process | ForEach-Object -Member FileVersion}.Ast
        $typeNames = [AstTypeInference]::InferTypeof($ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)
        $typeNames.Count | Should -Be 1
        $typeNames[0] | Should -Be 'System.String'
    }

    It 'Infers typeof Foreach-Object -Member when member is ScriptProperty' {
        $ast = {Get-Process | ForEach-Object -Member Description}.Ast
        $typeNames = [AstTypeInference]::InferTypeof($ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)
        $typeNames.Count | Should -Be 1
        $typeNames[0] | Should -Be 'System.String'
    }

    It 'Infers typeof Foreach-Object -Member when Member is Alias' {
        $ast = {Get-Process | ForEach-Object -Member Handles}.Ast
        $typeNames = [AstTypeInference]::InferTypeof($ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)
        $typeNames.Count | Should -Be 1
        $typeNames[0] | Should -Be 'System.Int32'
    }

    It 'Infers typeof Foreach-Object -Member when using dependent scriptproperties' {
        class InferScriptPropLevel1 {
            [string] $Value
            InferScriptPropLevel1() {
                $this.Value = "TheValue"
            }
        }
        class InferScriptPropLevel2 {
            [InferScriptPropLevel1] $X
            InferScriptPropLevel2() {$this.X = [InferScriptPropLevel1]::new()}
        }
        Update-TypeData -TypeName InferScriptPropLevel1 -MemberName TheValue -MemberType ScriptProperty -Value { return $this.Value } -Force
        Update-TypeData -TypeName InferScriptPropLevel2 -MemberName XVal -MemberType ScriptProperty -Value {return $this.X } -Force
        try {
            $ast = {[InferScriptPropLevel2]::new() | ForEach-Object -MemberName XVal | ForEach-Object -MemberName TheValue}.Ast
            $typeNames = [AstTypeInference]::InferTypeof($ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)
            $typeNames.Count | Should -Be 1
            $typeNames[0] | Should -Be 'System.String'
        }
        finally {
            Remove-TypeData -TypeName InferScriptPropLevel1
            Remove-TypeData -TypeName InferScriptPropLevel2
        }
    }

    It "Infers typeof pscustomobject" {

        $res = [AstTypeInference]::InferTypeOf( { [pscustomobject] @{
                    B = "X"
                    A = 1
                }}.Ast)
        $res.Count | Should -Be 1
        $res[0].GetType().Name | Should -Be "PSSyntheticTypeName"
        $res[0].Name | Should -Be "System.Management.Automation.PSObject#A:B"
        $res[0].Members[0].Name | Should -Be "A"
        $res[0].Members[0].PSTypeName | Should -Be "System.Int32"
        $res[0].Members[1].Name | Should -Be "B"
        $res[0].Members[1].PSTypeName | Should -Be "System.String"
    }

    It "Infers typeof pscustomobject with PSTypeName" {

        $res = [AstTypeInference]::InferTypeOf( { [pscustomobject] @{
                    A          = 1
                    B          = "X"
                    PSTypeName = "MyType"
                }}.Ast)
        $res.Count | Should -Be 1
        $res[0].GetType().Name | Should -Be "PSSyntheticTypeName"
        $res.Members.Count  | Should -Be 2
        $res[0].Name | Should -Be "MyType#A:B"
        $res[0].Members[0].Name | Should -Be "A"
        $res[0].Members[0].PSTypeName | Should -Be "System.Int32"
    }

    It "Infers typeof Select-Object when Parameter is Property" {
        $res = [AstTypeInference]::InferTypeOf( { [io.fileinfo]::new("file") | Select-Object -Property Directory }.Ast)
        $res.Count | Should -Be 1
        $res[0].GetType().Name | Should -Be "PSSyntheticTypeName"
        $res[0].Name | Should -Be "System.Management.Automation.PSObject#Directory"
        $res[0].Members[0].Name | Should -Be "Directory"
        $res[0].Members[0].PSTypeName | Should -Be "System.IO.DirectoryInfo"
    }

    It "Infers typeof Select-Object when PSObject and Parameter is Property" {
        $res = [AstTypeInference]::InferTypeOf( { [PSCustomObject] @{A = 1; B = "2"} | Select-Object -Property A}.Ast)
        $res.Count | Should -Be 1
        $res[0].Name | Should -Be "System.Management.Automation.PSObject#A"
        $res[0].Members[0].Name | Should -Be "A"
        $res[0].Members[0].PSTypeName | Should -Be "System.Int32"
    }

    It "Infers typeof Select-Object when Parameter is Properties" {
        $res = [AstTypeInference]::InferTypeOf( {  [io.fileinfo]::new("file")  | Select-Object -Property Director*, Name }.Ast)
        $res.Count | Should -Be 1
        $res[0].Name | Should -Be "System.Management.Automation.PSObject#Directory:DirectoryName:Name"
        $res[0].Members[0].Name | Should -Be "Directory"
        $res[0].Members[0].PSTypeName | Should -Be "System.IO.DirectoryInfo"
        $res[0].Members[1].Name | Should -Be "DirectoryName"
        $res[0].Members[1].PSTypeName | Should -Be "System.String"
    }

    It "Infers typeof Select-Object when Parameter is ExcludeProperty" {
        $res = [AstTypeInference]::InferTypeOf( {  [io.fileinfo]::new("file")  |  Select-Object -ExcludeProperty *Time*, E* }.Ast)
        $res.Count | Should -Be 1
        $res[0].Name | Should -BeExactly "System.Management.Automation.PSObject#Attributes:BaseName:Directory:DirectoryName:FullName:IsReadOnly:Length:LengthString:LinkTarget:LinkType:Mode:ModeWithoutHardLink:Name:NameString:ResolvedTarget:Target:UnixFileMode:VersionInfo"
        $names = $res[0].Members.Name
        $names -contains "BaseName" | Should -BeTrue
        $names -contains "Name" | Should -BeTrue
        $names -contains "Mode" | Should -BeTrue
        $names -contains "Exits" | Should -BeFalse
    }

    It "Infers typeof Select-Object when Parameter is ExpandProperty" {
        $res = [AstTypeInference]::InferTypeOf( { [io.fileinfo]::new("file")  | Select-Object -ExpandProperty Directory }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be "System.IO.DirectoryInfo"
    }

    It "Infers typeof Select-Object when No projection is done" {
        $res = [AstTypeInference]::InferTypeOf( { "Hello" | Select-Object -First 1}.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be "System.String"
    }

    It "Infers typeof Get-Random with pipeline input" {
        $res = [AstTypeInference]::InferTypeOf( { "Hello","World" | Get-Random }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be "System.String"
    }

    It "Infers typeof Get-Random with astpair input" {
        $res = [AstTypeInference]::InferTypeOf( { Get-Random -InputObject Hello,World }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be "System.String[]"
    }

    It "Infers typeof Get-Random with no input" {
        $res = [AstTypeInference]::InferTypeOf( { Get-Random }.Ast)
        $res.Count | Should -Be 3
        $res.Name -join ', ' | Should -Be "System.Int32, System.Int64, System.Double"
    }

    It "Infers typeof Group-Object Group" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | Group-Object | ForEach-Object Group  }.Ast)
        $res.Count | Should -Be 3
        ($res.Name | Sort-Object)[1,2] -join ', ' | Should -Be "System.IO.DirectoryInfo, System.IO.FileInfo"
    }

    It "Infers typeof Group-Object Values" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | Group-Object | ForEach-Object Values  }.Ast)
        $res.Count | Should -Be 3
        ($res.Name | Sort-Object)[1,2] -join ', ' | Should -Be "System.IO.DirectoryInfo, System.IO.FileInfo"
    }

    It "Infers typeof Group-Object Group with Property" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | Group-Object -Property Name | ForEach-Object Group  }.Ast)
        $res.Count | Should -Be 3
        ($res.Name | Sort-Object)[1,2] -join ', ' | Should -Be "System.IO.DirectoryInfo, System.IO.FileInfo"
    }

    It "Infers typeof Group-Object Values with Property" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | Group-Object -Property Name | ForEach-Object Values  }.Ast)
        $res.Count | Should -Be 2
        $res.Name -join ', ' | Should -Be "System.String, System.Collections.ArrayList"
    }

    It "Infers typeof Group-Object Group with NoElement" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | Group-Object -Property Name -NoElement | ForEach-Object Group  }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -BeLike "*Collection*PSObject*"
    }

    It "Infers typeof Group-Object Values with Properties" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | Group-Object -Property Name,CreationTime | ForEach-Object Values  }.Ast)
        $res.Count | Should -Be 3
        ($res.Name | Sort-Object)  -join ', ' | Should -Be "System.Collections.ArrayList, System.DateTime, System.String"
    }

    It "ignores Group-Object Group with Scriptblock" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | Group-Object -Property {$_.Name} | ForEach-Object Values  }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be "System.Collections.ArrayList"
    }

    It "Infers type from OutputTypeAttribute" {
        $res = [AstTypeInference]::InferTypeOf( { Get-Process -Id 2345 }.Ast)
        $gpsOutput = [Microsoft.PowerShell.Commands.GetProcessCommand].GetCustomAttributes([System.Management.Automation.OutputTypeAttribute], $false).Type
        $names = $gpsOutput.Name
        foreach ($r in $res) {
            $r.Name -In $names | Should -BeTrue
        }
    }

    It "Infers type from variable with AllowSafeEval" {
        function Hide-GetProcess { Get-Process }
        $p = Hide-GetProcess
        $res = [AstTypeInference]::InferTypeOf( { $p }.Ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)
        $res.Name | Should -Be 'System.Diagnostics.Process'
    }

    It "Infers type from variable with type in scope" {

        $res = [AstTypeInference]::InferTypeOf( {
                $p = 1
                $p
            }.Ast)
        $res.Name | Should -Be 'System.Int32'
    }

    It "Infers type from block statement" {
        $errors = $null
        $tokens = $null
        $ast = [Language.Parser]::ParseInput("parallel {1}", [ref] $tokens, [ref] $errors)

        $res = [AstTypeInference]::InferTypeOf( $ast.EndBlock.Statements[0])
        $res.Name | Should -Be 'System.Int32'
    }

    It 'Infers type from attributed expession' {
        $res = [AstTypeInference]::InferTypeOf( {
                [ValidateRange(1, 2)]
                [int]$i = 1
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type from if statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                if ($true) { return 1}
                else { return 'Text'}
            }.Ast)

        $res.Count | Should -Be 2
        foreach ($r in $res) {
            $r.Name -In 'System.Int32', 'System.String' | Should -BeTrue
        }
    }

    It 'Infers type from switch statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                switch (1, 2, 3) {
                    (1) { return 'Hello'}
                    (2) {return [int]}
                    default {return 1}
                }
            }.Ast)

        $res.Count | Should -Be 3
        foreach ($r in $res) {
            $r.Name -In 'System.Type', 'System.Int32', 'System.String' | Should -BeTrue
        }
    }

    It 'Infers type from Foreach statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                foreach ($i in 1, 2, 3) {
                    if ($i -eq 1) { return 'Hello'}
                    if ($i -eq 2) {return [int]}
                    return 1
                }
            }.Ast)

        $res.Count | Should -Be 3
        foreach ($r in $res) {
            $r.Name -In 'System.Type', 'System.Int32', 'System.String' | Should -BeTrue
        }
    }

    It 'Infers type of a Foreach statement current value variable' {
        $res = [AstTypeInference]::InferTypeOf( {
            foreach ($intValue in 1, 2, 3) {
                $intValue
            }
        }.Ast.EndBlock.Statements[0].Body.Statements[0].PipelineElements[0].Expression)

        $res.Count | Should -Be 1
        $res.Name | Should -BeExactly 'System.Int32'
    }

    It 'Infers type of a Foreach statement current value variable with hashtable enumerator' {
        $res = [AstTypeInference]::InferTypeOf( {
            foreach ($dictionaryEntry in @{}.GetEnumerator()) {
                $dictionaryEntry
            }
        }.Ast.EndBlock.Statements[0].Body.Statements[0].PipelineElements[0].Expression)

        $res.Count | Should -Be 2
        $res.Name | Should -Be 'System.Collections.DictionaryEntry', 'System.Object'
    }

    It 'Infers type of a Foreach statement current value variable with dictionary enumerator' {
        $res = [AstTypeInference]::InferTypeOf( {
            foreach ($keyValuePair in [Dictionary[int, string]]::new().GetEnumerator()) {
                $keyValuePair
            }
        }.Ast.EndBlock.Statements[0].Body.Statements[0].PipelineElements[0].Expression)

        $res.Count | Should -Be 3
        $res.Name | Should -Be ([KeyValuePair[int, string]].FullName), 'System.Object', 'System.Collections.DictionaryEntry'
    }

    It 'Infers type of a Foreach statement current value variable with generic IEnumerable' {
        $res = [AstTypeInference]::InferTypeOf( {
            $debugger = [Debugger]$Host.Runspace.Debugger
            foreach ($subscriber in $debugger.GetCallStack()) {
                $subscriber
            }
        }.Ast.EndBlock.Statements[1].Body.Statements[0].PipelineElements[0].Expression)

        $res.Count | Should -Be 1
        $res.Name | Should -BeExactly 'System.Management.Automation.CallStackFrame'
    }

    It 'Infers type from While statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                while ($true) {
                    if ($i -eq 1) { return 'Hello'}
                    if ($i -eq 2) {return [int]}
                    return 1
                }
            }.Ast)

        $res.Count | Should -Be 3
        foreach ($r in $res) {
            $r.Name -In 'System.Type', 'System.Int32', 'System.String' | Should -BeTrue
        }
    }

    It 'Infers type from For statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                for ($i = 0; $i -lt 10; $i++) {
                    if ($i -eq 1) { return 'Hello'}
                    if ($i -eq 2) {return [int]}
                    return 1
                }
            }.Ast)

        $res.Count | Should -Be 3
        foreach ($r in $res) {
            $r.Name -In 'System.Type', 'System.Int32', 'System.String' | Should -BeTrue
        }
    }

    It 'Infers type from Do-While statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                do {
                    if ($i -eq 1) { return 'Hello'}
                    if ($i -eq 2) {return [int]}
                    return 1
                }while ($true)
            }.Ast)

        $res.Count | Should -Be 3
        foreach ($r in $res) {
            $r.Name -In 'System.Type', 'System.Int32', 'System.String' | Should -BeTrue
        }
    }

    It 'Infers type from Do-Until statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                do {
                    if ($i -eq 1) { return 'Hello'}
                    if ($i -eq 2) {return [int]}
                    return 1
                } until ($true)
            }.Ast)

        $res.Count | Should -Be 3
        foreach ($r in $res) {
            $r.Name -In 'System.Type', 'System.Int32', 'System.String' | Should -BeTrue
        }
    }

    It 'Infers type from full scriptblock' {
        $res = [AstTypeInference]::InferTypeOf( {
                begin {1}
                process {"text"}
                end {[int]}
            }.Ast)

        $res.Count | Should -Be 3
        foreach ($r in $res) {
            $r.Name -In 'System.Type', 'System.Int32', 'System.String' | Should -BeTrue
        }
    }

    It 'Infers type from sub expression' {
        $res = [AstTypeInference]::InferTypeOf( {
                $(1)
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type from Throw statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                throw 'Foo'
            }.Ast)

        $res.Count | Should -Be 0
    }

    It 'Infers type from Return statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                return 1
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It 'Infers type from empty Return statement' {
        $res = [AstTypeInference]::InferTypeOf( { return }.Ast)
        $res.Count | Should -Be 0
    }

    It 'Infers type from New-Object statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                New-Object -TypeName 'System.Diagnostics.Stopwatch'
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Diagnostics.Stopwatch'
    }

    It 'Infers type from Continue statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                continue
            }.Ast)

        $res.Count | Should -Be 0
    }

    It 'Infers type from Break statement' {
        $res = [AstTypeInference]::InferTypeOf( {
                break
            }.Ast)

        $res.Count | Should -Be 0
    }

    It 'Infers type from Merging redirection' {
        $errors = $null
        $tokens = $null
        $ast = [Language.Parser]::ParseInput("p4 resolve ... 2>&1", [ref] $tokens, [ref] $errors)
        $res = [AstTypeInference]::InferTypeOf( $ast.EndBlock.Statements[0].PipelineElements[0].Redirections[0] )
        $res.Count | Should -Be 0
    }

    It 'Infers type from File redirection' {
        $errors = $null
        $tokens = $null
        $ast = [Language.Parser]::ParseInput("p4 resolve ... > foo.txt", [ref] $tokens, [ref] $errors)
        $res = [AstTypeInference]::InferTypeOf( $ast.EndBlock.Statements[0].PipelineElements[0].Redirections[0] )
        $res.Count | Should -Be 0
    }

    It 'Infers type of alias property' {
        class X {
            [int] $Length
        }
        Update-TypeData -TypeName X -MemberType AliasProperty -MemberName AliasLength -Value Length -Force
        $res = [AstTypeInference]::InferTypeOf( {
                [x]::new().AliasLength
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of code property' {
        class X {
            static [int] CodeProp([psobject] $o) { return 1 }
        }

        class Y {}
        Update-TypeData -TypeName Y -MemberName CodeProp -MemberType CodeProperty -Value ([X].GetMethod("CodeProp")) -Force
        $res = [AstTypeInference]::InferTypeOf( {
                [Y]::new().CodeProp
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of script property' {
        class Y {}
        Update-TypeData -TypeName Y -MemberName ScriptProp -MemberType ScriptProperty -Value {1} -Force
        $res = [AstTypeInference]::InferTypeOf( {
                [Y]::new().ScriptProp
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of script property with outputtype' {
        class Y {}
        Update-TypeData -TypeName Y -MemberName ScriptProp -MemberType ScriptProperty -Value {[OutputType([int])]param()1} -Force
        $res = [AstTypeInference]::InferTypeOf( {
                [Y]::new().ScriptProp
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of script method with outputtype' {
        class Y {}
        Update-TypeData -TypeName Y -MemberName MyScriptMethod -MemberType ScriptMethod -Value {[OutputType([int])]param()1} -Force
        $res = [AstTypeInference]::InferTypeOf( {
                [Y]::new().MyScriptMethod
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of note property' {

        $res = [AstTypeInference]::InferTypeOf( {
                [pscustomobject] @{
                    A = ''
                }.A
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Management.Automation.PSObject'
    }

    It 'Infers type of try catch finally' {

        $res = [AstTypeInference]::InferTypeOf( {
                try {
                    1
                }
                catch [exception] {
                    "Text"
                }
                finally {
                    [int]
                }
            }.Ast)

        $res.Count | Should -Be 3
        foreach ($r in $res) {
            $r.Name -In 'System.Int32', 'System.String', 'System.Type' | Should -BeTrue
        }
    }

    It "Infers type from trap statement" {
        $res = [AstTypeInference]::InferTypeOf( {
                trap {
                    "text"
                }
            }.Ast.EndBlock.Traps[0])
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String'
    }

    It "Infers type from exit statement" {
        $res = [AstTypeInference]::InferTypeOf( {
                exit
            }.Ast.EndBlock)
        $res.Count | Should -Be 0
    }

    It 'Infers type of Where/Sort/Foreach pipeline' {
        $res = [AstTypeInference]::InferTypeOf( {
                [int[]](1..10) | Sort-Object -Descending | Where-Object {$_ -gt 3} | ForEach-Object {$_.ToString()}
            }.Ast)

        $res.Name | Should -Be System.String
    }

    It 'Infers type of Method accessed as Property' {
        $res = [AstTypeInference]::InferTypeOf( {
                ''.ToString
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Management.Automation.PSMethod
    }

    It 'Infers int from List[int] with foreach' {
        $res = [AstTypeInference]::InferTypeOf( {
                $l = [System.Collections.Generic.List[string]]::new()
                $l | ForEach-Object {$_}
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.String
    }

    It 'Infers class type' {
        $res = [AstTypeInference]::InferTypeOf( {
                class X {
                    [int] $I
                    [bool] Method() {
                        return $true
                    }
                }
            }.Ast)

        $res.Count | Should -Be 0
    }

    Context "TestDrivePath" {
        BeforeAll {
            $errors = $null
            $tokens = $null
            $p = Resolve-Path TestDrive:/
        }
        It 'Infers type of command parameter' {
            $ast = [Language.Parser]::ParseInput("Get-ChildItem -Path $p/foo.txt", [ref] $tokens, [ref] $errors)
            $cmdParam = $ast.EndBlock.Statements[0].Pipelineelements[0].CommandElements[1]
            $res = [AstTypeInference]::InferTypeOf( $cmdParam )

            $res.Count | Should -Be 0
        }

        It 'Infers type of command parameter - second form' {
            $ast = [Language.Parser]::ParseInput("Get-ChildItem -LiteralPath $p/foo.txt", [ref] $tokens, [ref] $errors)
            $cmdParam = $ast.EndBlock.Statements[0].Pipelineelements[0].CommandElements[1]
            $res = [AstTypeInference]::InferTypeOf( $cmdParam )
            $res.Count | Should -Be 0
        }

        It 'Infers type of common commands with Path parameter' {
            $ast = [Language.Parser]::ParseInput("Get-ChildItem -Path:$p/foo.txt", [ref] $tokens, [ref] $errors)
            $cmdAst = $ast.EndBlock.Statements[0].Pipelineelements[0]
            $res = [AstTypeInference]::InferTypeOf( $cmdAst )

            $res.Count | Should -Be 2
            foreach ($r in $res) {
                $r.Name -In 'System.IO.FileInfo', 'System.IO.DirectoryInfo' | Should -BeTrue
            }
        }

        It 'Infers type of common commands with LiteralPath parameter' {
            $ast = [Language.Parser]::ParseInput("Get-ChildItem -LiteralPath:$p/foo.txt", [ref] $tokens, [ref] $errors)
            $cmdAst = $ast.EndBlock.Statements[0].Pipelineelements[0]
            $res = [AstTypeInference]::InferTypeOf( $cmdAst )

            $res.Count | Should -Be 2
            foreach ($r in $res) {
                $r.Name -In 'System.IO.FileInfo', 'System.IO.DirectoryInfo' | Should -BeTrue
            }
        }
    }

    It 'Infers type of variable $_ in hashtable in command parameter' {
        $variableAst = {1..10 | Format-Table @{n = 'x'; ex = {$_}}}.ast.Find( {param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst]}, $true)
        $res = [AstTypeInference]::InferTypeOf( $variableAst)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of variable $_ in hashtable from Array' {
        $variableAst = { [int[]]::new(10) | Format-Table @{n = 'x'; ex = {$_}}}.ast.Find( {param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst]}, $true)
        $res = [AstTypeInference]::InferTypeOf( $variableAst)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of variable $_ in hashtable from generic IEnumerable ' {
        $variableAst = { [System.Collections.Generic.List[int]]::new() | Format-Table @{n = 'x'; ex = {$_}}}.ast.Find( {param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst]}, $true)
        $res = [AstTypeInference]::InferTypeOf( $variableAst)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of variable $_ command parameter' {
        $variableAst = { 1..10 | Group-Object {$_.Length}}.ast.Find( {param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst]}, $true)
        $res = [AstTypeInference]::InferTypeOf( $variableAst)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of variable $_ in catch block' {
        $variableAst = { try {} catch { $_ } }.Ast.Find({ param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)
        $res = [AstTypeInference]::InferTypeOf($variableAst)

        $res | Should -HaveCount 1
        $res.Name | Should -Be System.Management.Automation.ErrorRecord
    }

    It 'Infers type of untyped $_.Exception in catch block' {
        $memberAst = { try {} catch { $_.Exception } }.Ast.Find({ param($a) $a -is [System.Management.Automation.Language.MemberExpressionAst] }, $true)
        $res = [AstTypeInference]::InferTypeOf($memberAst)

        $res | Should -HaveCount 1
        $res.Name | Should -Be System.Exception
    }

    It 'Infers type of variable $_ in pipeline with more than one element' {
        $memberAst = { Get-Date | New-Guid | Select-Object -Property {$_} }.Ast.Find({ param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)
        $res = [AstTypeInference]::InferTypeOf($memberAst)

        $res | Should -HaveCount 1
        $res.Name | Should -Be System.Guid
    }

    It 'Infers type of variable $_ in array of calculated properties' {
        $variableAst = { New-TimeSpan | Select-Object -Property Day,@{n="min";e={$_}} }.Ast.Find({ param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)
        $res = [AstTypeInference]::InferTypeOf($variableAst)

        $res | Should -HaveCount 1
        $res.Name | Should -Be System.TimeSpan
    }

    It 'Infers type of variable $_ in switch statement' {
        $variableAst = {
        switch ("Hello","World")
        {
            'Hello'
            {
                $_
            }
        } }.Ast.Find({ param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)
        $res = [AstTypeInference]::InferTypeOf($variableAst)

        $res | Should -HaveCount 1
        $res.Name | Should -Be System.String
    }

    It 'Does not infer string in pipeline as char' {
        $variableAst = { "Hello" | Select-Object -Property @{n="min";e={$_}} }.Ast.Find({ param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)
        $res = [AstTypeInference]::InferTypeOf($variableAst)
        $res.Name | Should -Be System.String
    }

    $catchClauseTypes = @(
        @{ Type = 'System.ArgumentException' }
        @{ Type = 'System.ArgumentNullException' }
        @{ Type = 'System.ArgumentOutOfRangeException' }
        @{ Type = 'System.Collections.Generic.KeyNotFoundException' }
        @{ Type = 'System.DivideByZeroException' }
        @{ Type = 'System.FormatException' }
        @{ Type = 'System.IndexOutOfRangeException' }
        @{ Type = 'System.InvalidOperationException' }
        @{ Type = 'System.IO.DirectoryNotFoundException' }
        @{ Type = 'System.IO.DriveNotFoundException' }
        @{ Type = 'System.IO.FileNotFoundException' }
        @{ Type = 'System.IO.PathTooLongException' }
        @{ Type = 'System.Management.Automation.CommandNotFoundException' }
        @{ Type = 'System.Management.Automation.JobFailedException' }
        @{ Type = 'System.Management.Automation.RuntimeException' }
        @{ Type = 'System.Management.Automation.ValidationMetadataException' }
        @{ Type = 'System.NotImplementedException' }
        @{ Type = 'System.NotSupportedException' }
        @{ Type = 'System.ObjectDisposedException' }
        @{ Type = 'System.OverflowException' }
        @{ Type = 'System.PlatformNotSupportedException' }
        @{ Type = 'System.RankException' }
        @{ Type = 'System.TimeoutException' }
        @{ Type = 'System.UriFormatException' }
    )

    It 'Infers type of $_.Exception in [<Type>] typed catch block' -TestCases $catchClauseTypes {
        param($Type)

        $memberAst = [scriptblock]::Create("try {} catch [$Type] { `$_.Exception }").Ast.Find(
            { param($a) $a -is [System.Management.Automation.Language.MemberExpressionAst] },
            $true
        )
        $res = [AstTypeInference]::InferTypeOf($memberAst)

        $res | Should -HaveCount 1
        $res.Name | Should -Be $Type
    }

    It 'Infers possible types of $_.Exception in multi-typed catch block' {
        $memberAst = { try {} catch [System.ArgumentException], [System.NotImplementedException] { $_.Exception } }.Ast.Find(
            { param($a) $a -is [System.Management.Automation.Language.MemberExpressionAst] },
            $true
        )
        $res = [AstTypeInference]::InferTypeOf($memberAst)

        $res | Should -HaveCount 2
        $res[0].Name | Should -Be System.ArgumentException
        $res[1].Name | Should -Be System.NotImplementedException
    }

    It 'Infers type of $_.Exception in each successive catch block' {
        $memberAst = {
            try {}
            catch [System.ArgumentException] { $_.Exception }
            catch { $_.Exception }
        }.Ast.FindAll(
            { param($a) $a -is [System.Management.Automation.Language.MemberExpressionAst] },
            $true
        )
        $res = foreach ($item in $memberAst) { [AstTypeInference]::InferTypeOf($item) }

        $res | Should -HaveCount 2
        $res[0].Name | Should -Be System.ArgumentException
        $res[1].Name | Should -Be System.Exception
    }

    It 'falls back to a generic ErrorRecord if catch exception type is invalid' {
        $VariableAst = {
            try {}
            catch [ThisTypeDoesNotExist] { $_ }
        }.Ast.Find(
            { param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] },
            $true
        )
        $res = [AstTypeInference]::InferTypeOf($VariableAst)

        $res.Name | Should -Be System.Management.Automation.ErrorRecord
    }

    It 'Infers type of trap statement' {
        $VariableAst = {
            trap { $_ }
        }.Ast.Find(
            { param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] },
            $true
        )
        $res = [AstTypeInference]::InferTypeOf($VariableAst)

        $res.Name | Should -Be System.Management.Automation.ErrorRecord
    }

    It 'Infers type of exception in typed trap statement' {
        $memberAst = {
            trap [System.DivideByZeroException] { $_.Exception }
        }.Ast.Find(
            { param($a) $a -is [System.Management.Automation.Language.MemberExpressionAst] },
            $true
        )
        $res = [AstTypeInference]::InferTypeOf($memberAst)

        $res.Name | Should -Be System.DivideByZeroException
    }

    It 'falls back to a generic ErrorRecord if trap exception type is invalid' {
        $VariableAst = {
            trap [ThisTypeDoesNotExist] { $_ }
        }.Ast.Find(
            { param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] },
            $true
        )
        $res = [AstTypeInference]::InferTypeOf($VariableAst)

        $res.Name | Should -Be System.Management.Automation.ErrorRecord
    }

    It 'Infers type of function member' {
        $res = [AstTypeInference]::InferTypeOf( {
                class X {
                    [DateTime] GetDate() { return [datetime]::Now }
                }
            }.Ast.Find( {param($ast) $ast -is [System.Management.Automation.Language.FunctionMemberAst]}, $true))

        $res.Count | Should -Be 0
    }

    It 'Infers type of MemberExpression on class property' {
        class X {
            [DateTime] $Date
        }
        $x = [X]::new()
        $res = [AstTypeInference]::InferTypeOf( {
                $x.Date
            }.Ast.Find( {param($ast) $ast -is [System.Management.Automation.Language.MemberExpressionAst] -and $ast.Member.Value -eq 'Date'}, $true))

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.DateTime
    }

    It 'Infers type of MemberExpression on class Method' {
        class X {
            [DateTime] GetDate() { return [DateTime]::Now }
        }
        $x = [X]::new()
        $res = [AstTypeInference]::InferTypeOf( {
                $x.GetDate()
            }.Ast.Find( {param($ast) $ast -is [System.Management.Automation.Language.MemberExpressionAst] -and $ast.Member.Value -eq 'GetDate'}, $true))

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.DateTime
    }

    It 'Infers type of note property with safe eval' -Skip {
        $res = [AstTypeInference]::InferTypeOf( {
                [pscustomobject] @{
                    A = ''
                }.A
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String'
    }

    It 'Infers type of invoke operator scriptblock' -Skip {
        $res = [AstTypeInference]::InferTypeOf( {
                & {1}
            }.Ast)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of script property with safe eval' -Skip {
        class Y {}
        Update-TypeData -TypeName Y -MemberName SafeEvalScriptProp -MemberType ScriptProperty -Value {1} -Force
        $res = [AstTypeInference]::InferTypeOf( {
                [Y]::new().SafeEvalScriptProp
            }.Ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of base ctor' -Skip {
        $res = [AstTypeInference]::InferTypeOf( {
                class BaseType {
                    [string] $s
                    BaseType([string]$s) {$this.s = $s}
                }
                class X : BaseType {
                    X() : base("foo") {}
                }
            }.Ast.Find( {param($ast) $ast -is [System.Management.Automation.Language.BaseCtorInvokeMemberExpressionAst]}, $true))

        $res.Count | Should -Be BaseType
    }

    It 'Infers type of "as" operator statement' {
        $res = [AstTypeInference]::InferTypeOf( { $null -as [int] }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It 'Infers type of $_ in a method scriptblock' {
        $res = [AstTypeInference]::InferTypeOf( { ("","").ForEach({$_}) }.Ast.Find({param($ast) $ast -is [System.Management.Automation.Language.VariableExpressionAst]}, $true))
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.String'
    }

    It 'Infers type of index item in an IList' {
        $res = [AstTypeInference]::InferTypeOf( { ([System.Collections.Generic.IList[System.Text.StringBuilder]]$null)[0] }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Text.StringBuilder'
    }

    It 'Infers type of generic method invocation with type parameters' {
        $res = [AstTypeInference]::InferTypeOf( { [array]::Empty[int]() }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32[]'
    }

    It 'Infers type of index item in an ICollection' -Skip:(!$IsWindows) {
        $res = [AstTypeInference]::InferTypeOf( { (Get-Acl -Path C:\).Access[0] }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Security.AccessControl.AuthorizationRule'
    }

    It 'Enumerates the inferred type after *-Object commands' {
        $res = [AstTypeInference]::InferTypeOf( { (([System.Management.Automation.Language.Ast]$null).FindAll() | Select-Object -First 1) }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Management.Automation.Language.Ast'
    }

    It 'Infers type of hashtable key with multiple types' {
        $res = [AstTypeInference]::InferTypeOf( { (@{RandomKey = Get-ChildItem $HOME}).RandomKey }.Ast)
        $res.Count | Should -Be 2
        $res.Name -join ' ' | Should -Be "System.IO.FileInfo System.IO.DirectoryInfo"
    }

    It 'Falls back to type inference for hashtable assignments with pure expression with no value' {
        $res = [AstTypeInference]::InferTypeOf( {$KeyWithNoValue = Get-ChildItem $HOME; (@{RandomKey = $KeyWithNoValue}).RandomKey }.Ast)
        $Res.Count | Should -Be 2
        $res.Name -join ' ' | Should -Be "System.IO.FileInfo System.IO.DirectoryInfo"
    }

    It 'Infers type of index expression on hashtable with synthetic type' {
        $res = [AstTypeInference]::InferTypeOf( { (@{RandomKey = Get-ChildItem $HOME})['RandomKey'] }.Ast)
        $res.Count | Should -Be 2
        $res.Name -join ' ' | Should -Be "System.IO.FileInfo System.IO.DirectoryInfo"
    }

    It 'Infers type of member expression on a custom object' {
        $res = [AstTypeInference]::InferTypeOf( { ([pscustomobject]@{RandomProp1 = Get-ChildItem $HOME}).RandomProp1 }.Ast)
        $res.Count | Should -Be 2
        $res.Name -join ' ' | Should -Be "System.IO.FileInfo System.IO.DirectoryInfo"
    }

    It 'Infers closest variable type' {
        $res = [AstTypeInference]::InferTypeOf( { [string]$TestVar = "";[hashtable]$TestVar = @{};$TestVar }.Ast)
        $res.Name | Select-Object -Last 1 | Should -Be "System.Collections.Hashtable"
    }

    It 'Infers closest variable type and ignores unrelated param blocks' {
        $res = [AstTypeInference]::InferTypeOf( {
        [hashtable]$ParameterName=@{}
        function Verb-Noun {
            param
            (
                [string]$ParameterName
            )
        }
        $ParameterName }.Ast)
        $res.Name | Should -Be "System.Collections.Hashtable"
    }

    It 'Infers type of $null after variable assignment' {
        $res = [AstTypeInference]::InferTypeOf( { $null = "Hello";$null }.Ast)
        $res.Count | Should -Be 0
    }

    It 'Infers type of all scope variable after variable assignment' {
        $res = [AstTypeInference]::InferTypeOf( { $true = "Hello";$true }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Boolean'
    }

    It 'Infers type of all scope variable host after variable assignment' {
        $res = [AstTypeInference]::InferTypeOf( { $Host = "Hello";$Host }.Ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Management.Automation.Internal.Host.InternalHost'
    }

    It 'Infers type of external applications' {
        $res = [AstTypeInference]::InferTypeOf( { pwsh }.Ast)
        $res.Name | Should -Be 'System.String'
    }

    It 'Should not throw when inferring $_ in switch condition' {
        $FoundAst = { switch($_){default{}} }.Ast.Find(
            {param($Ast) $Ast -is [Language.VariableExpressionAst]},
            $true
        )
        $null = [AstTypeInference]::InferTypeOf($FoundAst)
    }
}

Describe "AstTypeInference tests" -Tags CI {
    It "Infers type from integer with passed in powershell instance" {
        $powerShell = [PowerShell]::Create([RunspaceMode]::CurrentRunspace)
        $res = [AstTypeInference]::InferTypeOf( { 1 }.Ast, $powerShell)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.Int32'
    }

    It "Infers type from integer with passed in powershell instance and typeinferencespermissions" {
        $powerShell = [PowerShell]::Create([RunspaceMode]::CurrentRunspace)
        $v = 1
        $res = [AstTypeInference]::InferTypeOf( { $v }.Ast, $powerShell, [TypeInferenceRuntimePermissions]::AllowSafeEval)
        $res.Name | Should -Be 'System.Int32'
    }

}
