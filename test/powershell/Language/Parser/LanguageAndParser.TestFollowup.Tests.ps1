# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
$powershellexe = (Get-Process -Id $PID).mainmodule.filename

Describe "Clone array" -Tags "CI" {
    It "Cast in target expr" {
        (([int[]](42)).clone()) | Should -Be 42
        (([int[]](1..5)).clone()).Length | Should -Be 5
        (([int[]](1..5)).clone()).GetType() | Should -Be ([int[]])

    }
    It "Cast not in target expr" {
        $e = [int[]](42)
        $e.Clone() | Should -Be 42
        $e = [int[]](1..5)
        $e.Clone().Length | Should -Be 5
        $e.Clone().GetType() | Should -Be ([int[]])
    }
}

Describe "Set fields through PSMemberInfo" -Tags "CI" {
    Add-Type @"
    public struct AStruct { public string s; }
"@

    It "via cast" {
        ([AStruct]@{s = "abc" }).s | Should -BeExactly "abc"
    }
    It "via new-object" {
        (New-Object AStruct -prop @{s="abc"}).s | Should -BeExactly "abc"
    }
    It "via PSObject" {
        $x = [AStruct]::new()
        $x.psobject.properties['s'].Value = 'abc'
        $x.s | Should -Be "abc"
    }
}

Describe "MSFT:3309783" -Tags "CI" {

    It "Run in another process" {
        # For a reliable test, we must run this in a new process because an earlier binding in this process
        # could mask the bug/fix.
        & $powershellexe -noprofile -command "[psobject] | ForEach-Object FullName" | Should -Be System.Management.Automation.PSObject
    }

    It "Run in current process" {
        # For good measure, do the same thing in this process
        [psobject] | ForEach-Object FullName | Should -Be System.Management.Automation.PSObject
    }

    It "Pipe objects derived from PSObject" {
        # Related - make sure we can still pipe objects derived from PSObject
        class MyPsObj : PSObject
        {
            MyPsObj($obj) : base($obj) { }
            [string] ToString() {
                # Don't change access via .psobject, that was also a bug.
                return "MyObj: " + $this.psobject.BaseObject
            }
        }

        [MyPsObj]::new("abc").psobject.ToString() | Should -Be "MyObj: abc"
        [MyPsObj]::new("def") | Out-String | ForEach-Object Trim | Should -Be "MyObj: def"
    }
}

Describe "ScriptBlockAst.GetScriptBlock throws on error" -Tags "CI" {

    $e = $null

    It "with parse error" {
        $ast = [System.Management.Automation.Language.Parser]::ParseInput('function ', [ref]$null, [ref]$e)
        { $ast.GetScriptBlock() } | Should -Throw -ErrorId "PSInvalidOperationException"
    }

    It "with semantic errors" {
        $ast = [System.Management.Automation.Language.Parser]::ParseInput('function foo{param()begin{}end{[ref][ref]1}dynamicparam{}}', [ref]$null, [ref]$e)

        { $ast.GetScriptBlock() } | Should -Throw -ErrorId "PSInvalidOperationException"
        { $ast.EndBlock.Statements[0].Body.GetScriptBlock() } | Should -Throw -ErrorId "PSInvalidOperationException"
    }
}

Describe "Hashtable key property syntax" -Tags "CI" {
    $script = @'
    # First create a hashtable wrapped in PSObject
    $hash = New-Object hashtable
    $key = [ConsoleColor]::Red
    $null = $hash.$key
    $hash = @{}
    $hash.$key = 'Hello'
    # works in PS 2,3,4. Fails in PS 5:
    $hash.$key
'@

    It "In current process" {
        # Run in current process, but something that ran earlier could influence
        # the result
        Invoke-Expression $script | Should -BeExactly 'Hello'
    }

    It "In different process" {
        # So also run in a fresh process
        $bytes = [System.Text.Encoding]::Unicode.GetBytes($script)
        & $powershellexe -noprofile -encodedCommand ([Convert]::ToBase64String($bytes)) | Should -BeExactly 'Hello'
    }
}

Describe "Assign automatic variables" -Tags "CI" {

    $autos = '_', 'args', 'this', 'input', 'pscmdlet', 'psboundparameters', 'myinvocation', 'psscriptroot', 'pscommandpath'

    foreach ($auto in $autos)
    {
        It "Assign auto w/ invalid type constraint - $auto" {
            { & ([ScriptBlock]::Create("[datetime]`$$auto = 1")) } | Should -Throw $auto
            { . ([ScriptBlock]::Create("[datetime]`$$auto = 1")) } | Should -Throw $auto
            { & ([ScriptBlock]::Create("[runspace]`$$auto = 1")) } | Should -Throw $auto
            { . ([ScriptBlock]::Create("[runspace]`$$auto = 1")) } | Should -Throw $auto
            { & ([ScriptBlock]::Create("[notexist]`$$auto = 1")) } | Should -Throw $auto
            { . ([ScriptBlock]::Create("[notexist]`$$auto = 1")) } | Should -Throw $auto
        }
    }

    foreach ($auto in $autos)
    {
        It "Assign auto w/o type constraint - $auto" {
            & ([ScriptBlock]::Create("`$$auto = 1; `$$auto")) | Should -Be 1
            . ([ScriptBlock]::Create("`$$auto = 1; `$$auto")) | Should -Be 1
        }
    }

    It "Assign auto w/ correct type constraint" {
        & { [object]$_ = 1; $_ } | Should -Be 1
        & { [object[]]$args = 1; $args } | Should -Be 1
        & { [object]$this = 1; $this } | Should -Be 1
        & { [object]$input = 1; $input } | Should -Be 1
        # Can't test PSCmdlet or PSBoundParameters, they use an internal type
        & { [System.Management.Automation.InvocationInfo]$MyInvocation = $MyInvocation; $MyInvocation.Line } | Should -Match Automation.InvocationInfo
        & { [string]$PSScriptRoot = 'abc'; $PSScriptRoot } | Should -BeExactly 'abc'
        & { [string]$PSCommandPath = 'abc'; $PSCommandPath } | Should -BeExactly 'abc'
    }
}

Describe "Assign readonly/constant variables" -Tags "CI" {

    function Get-WithoutTypeConstraintTestCases
    {
        [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidAssignmentToAutomaticVariable', '')]
        param()
        @(
            @{ sb_wo_conversion = { $? = 1 }; name = '$? = 1' }
            @{ sb_wo_conversion = { $HOME = 1 }; name = '$HOME = 1' }
            @{ sb_wo_conversion = { $PID = 1 }; name = '$PID = 1' }
        )
    }

    It "Assign readonly/constant variables w/o type constraint - '<name>'" -TestCases (Get-WithoutTypeConstraintTestCases) {
        param($sb_wo_conversion)
        { & $sb_wo_conversion } | Should -Throw -ErrorId "VariableNotWritable"
        { . $sb_wo_conversion } | Should -Throw -ErrorId "VariableNotWritable"
    }

    function Get-WithTypeConstraintTestCases
    {
        [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidAssignmentToAutomaticVariable', '')]
        param()
        @(
            @{ sb_w_conversion = { [datetime]$? = 1 }; name = '[datetime]$? = 1' }
            @{ sb_w_conversion = { [datetime]$HOME = 1 }; name = '[datetime]$HOME = 1' }
            @{ sb_w_conversion = { [datetime]$PID = 1 }; name = '[datetime]$PID = 1' }
        )
    }

    It "Assign readonly/constant variables w/ type constraint - '<name>'" -TestCases (Get-WithTypeConstraintTestCases) {
        param($sb_w_conversion)
        { & $sb_w_conversion } | Should -Throw -ErrorId "VariableNotWritable"
        { . $sb_w_conversion } | Should -Throw -ErrorId "VariableNotWritable"
    }
}

Describe "Attribute error position" -Tags "CI" {
    It "Ambiguous overloads" {
        $e =  {
            & {
                param(
                    [ValidateNotNull(1,2,3,4)]
                    $param
                )
            }
        } | Should -Throw -PassThru -ErrorId 'MethodCountCouldNotFindBest'
        $e.InvocationInfo.Line | Should -Match ValidateNotNull
    }
}

Describe "Multiple alias attributes" -Tags "CI" {
    It "basic test" {
        function foo {
            param(
                [alias('aa')]
                [alias('bb')]
                $cc
            )
            $cc
        }

        foo -aa 1 | Should -Be 1
        foo -bb 2 | Should -Be 2
        foo -cc 3 | Should -Be 3
    }
}

Describe "Members of System.Type" -Tags "CI" {
    It "Members in public classes derived from System.Type should be found" {
        class MyType : System.Collections.IEnumerable
        {
            [System.Collections.IEnumerator] GetEnumerator() { return $null }
        }

        [type] | Get-Member ImplementedInterfaces | Should -Be 'System.Collections.Generic.IEnumerable[type] ImplementedInterfaces {get;}'
        [MyType].ImplementedInterfaces | Should -Be System.Collections.IEnumerable
    }
}

Describe "Hash expression with if statement as value" -Tags "CI" {
    BeforeAll {
        # With no extra new lines after if-statement
        $hash1 = @{
            a = if (1) {'a'}
            b = 'b'
            c = if (0) {2} elseif (1) {'c'}
            d = 'd'
            e = if (0) {2} elseif (0) {2} else {'e'}
            f = 'f'
            g = if (0) {2} else {'g'}
            h = 'h'
        }

        # With extra new lines after if-statement
        $hash2 = @{
            a = if (1) {'a'}

            b = 'b'
            c = if (0) {2} elseif (1) {'c'}

            d = 'd'
            e = if (0) {2} elseif (0) {2} else {'e'}

            f = 'f'
            g = if (0) {2} else {'g'}

            h = 'h'
        }

        # With expanded if-statement
        $hash3 = @{
            a = if (1)
                {
                    'a'
                }
            b = 'b'
            c = if (0)
                {
                    2
                }
                elseif (1)
                {
                    'c'
                }
            d = 'd'
            e = if (0)
                {
                    2
                }
                elseif (0)
                {
                    2
                }
                else
                {
                    'e'
                }
            f = 'f'
            g = if (0)
                {
                    2
                }
                else
                {
                    'g'
                }
            h = 'h'
        }

        $testCases = @(
            @{ name = "No extra new lines"; hash = $hash1 }
            @{ name = "With extra new lines"; hash = $hash2 }
            @{ name = "With expanded if-statement"; hash = $hash3 }
        )
    }

    It "Key-value pairs after an if-statement-value in a HashExpression should continue to be parsed - <name>" -TestCases $testCases {
        param($hash)

        $hash['a'] | Should -BeExactly 'a'
        $hash['b'] | Should -BeExactly 'b'
        $hash['c'] | Should -BeExactly 'c'
        $hash['d'] | Should -BeExactly 'd'
        $hash['e'] | Should -BeExactly 'e'
        $hash['f'] | Should -BeExactly 'f'
        $hash['g'] | Should -BeExactly 'g'
        $hash['h'] | Should -BeExactly 'h'
    }
}

Describe "Support case-insensitive comparison in Posix locale" -Tag CI {
    It "Hashtable is case insensitive" -Skip:($IsWindows) {
        try {
            $oldCulture = [System.Globalization.CultureInfo]::CurrentCulture
            [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::new('en-US-POSIX')

            @{h=1}.H | Should -Be 1 -Because 'hashtables should be case insensitive in en-US-POSIX culture'
        }
        finally {
            [System.Globalization.CultureInfo]::CurrentCulture = $oldCulture
        }
    }

    It "Wildcard support case-insensitive matching" -Skip:($IsWindows) {
        try {
            $oldCulture = [System.Globalization.CultureInfo]::CurrentCulture
            [System.Globalization.CultureInfo]::CurrentCulture = [System.Globalization.CultureInfo]::new('en-US-POSIX')

            $wildcard1 = [WildcardPattern]::new("AbC*", [System.Management.Automation.WildcardOptions]::IgnoreCase)
            $wildcard1.IsMatch("abcd") | Should -BeTrue

            $wildcard2 = [WildcardPattern]::new("DeF", [System.Management.Automation.WildcardOptions]::IgnoreCase);
            $wildcard2.IsMatch("def") | Should -BeTrue
        }
        finally {
            [System.Globalization.CultureInfo]::CurrentCulture = $oldCulture
        }
    }
}
