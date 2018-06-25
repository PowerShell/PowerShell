# Copyright (c) Microsoft Corporation. All rights reserved.
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

    It "Infers type from Array literal" {
        $res = [AstTypeInference]::InferTypeOf( { , 1 }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.object[]'
    }

    It "Infers type from array IndexExpresssion" {
        $res = [AstTypeInference]::InferTypeOf( { (1, 2, 3)[0] }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be 'System.object'
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
        $res = [AstTypeInference]::InferTypeOf( { $pid = 1; $using:pid }.Ast.EndBlock.Statements[1].PipelineElements[0].Expression)
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
        $ast = {Get-Process | Foreach-Object -Member FileVersion}.Ast
        $typeNames = [AstTypeInference]::InferTypeof($ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)
        $typeNames.Count | Should -Be 1
        $typeNames[0] | Should -Be 'System.String'
    }

    It 'Infers typeof Foreach-Object -Member when member is ScriptProperty' {
        $ast = {Get-Process | Foreach-Object -Member Description}.Ast
        $typeNames = [AstTypeInference]::InferTypeof($ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)
        $typeNames.Count | Should -Be 1
        $typeNames[0] | Should -Be 'System.String'
    }

    It 'Infers typeof Foreach-Object -Member when Member is Alias' {
        $ast = {Get-Process | Foreach-Object -Member Handles}.Ast
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
            $ast = {[InferScriptPropLevel2]::new() | Foreach-Object -MemberName XVal | ForEach-Object -MemberName TheValue}.Ast
            $typeNames = [AstTypeInference]::InferTypeof($ast, [TypeInferenceRuntimePermissions]::AllowSafeEval)
            $typeNames.Count | Should -Be 1
            $typeNames[0] | Should -Be 'System.String'
        }
        finally {
            Remove-TypeData -TypeName InferScriptPropLevel1
            Remove-TypeData -TypeName InferScriptPropLevel2
        }
    }

    It "Infers typeof Select-Object when Member is ExpandProperty" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | Select-Object -ExpandProperty Directory }.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be "System.IO.DirectoryInfo"
    }

    It "Infers typeof Select-Object when No projection is done" {
        $res = [AstTypeInference]::InferTypeOf( { "Hello" | Select-Object -First 1}.Ast)
        $res.Count | Should -Be 1
        $res.Name | Should -Be "System.String"
    }

    It "Don't Infer typeof Select-Object when projection is done" {
        $res = [AstTypeInference]::InferTypeOf( { Get-ChildItem | Select-Object -Property Name}.Ast)
        $res.Count | Should -Be 0
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
        Update-TypeData -Typename X -MemberType AliasProperty -MemberName AliasLength -Value Length -Force
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
            $p = Resolve-path TestDrive:/
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
        $variableAst = {1..10 | Format-table @{n = 'x'; ex = {$_}}}.ast.Find( {param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst]}, $true)
        $res = [AstTypeInference]::InferTypeOf( $variableAst)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of variable $_ in hashtable from Array' {
        $variableAst = { [int[]]::new(10) | Format-table @{n = 'x'; ex = {$_}}}.ast.Find( {param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst]}, $true)
        $res = [AstTypeInference]::InferTypeOf( $variableAst)

        $res.Count | Should -Be 1
        $res.Name | Should -Be System.Int32
    }

    It 'Infers type of variable $_ in hashtable from generic IEnumerable ' {
        $variableAst = { [System.Collections.Generic.List[int]]::new() | Format-table @{n = 'x'; ex = {$_}}}.ast.Find( {param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst]}, $true)
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
