# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'conversion syntax' -Tags "CI" {
    # these test suite covers ([<type>]<expression>).<method>() syntax.
    # it mixes two purposes: casting and super-class method calls.

    It 'converts array of single enum to bool' {
        # This test relies on the fact that [ConsoleColor]::Black is 0 and all other values are non-zero
        [bool]@([ConsoleColor]::Black) | Should -BeFalse
        [bool]@([ConsoleColor]::Yellow) | Should -BeTrue
    }

    It 'calls virtual method non-virtually' {
        ([object]"abc").ToString() | Should -Be "System.String"

        # generate random string to avoid JIT optimization
        $r = [guid]::NewGuid().Guid
        ([object]($r + "a")).Equals(($r + "a")) | Should -BeFalse
    }

    It 'calls method on a super-type, when conversion syntax used' {
        # This test relies on the fact that there are overloads (at least 2) for ToString method.
        ([System.Management.Automation.ActionPreference]"Stop").ToString() | Should -Be "Stop"
    }

    Context "Cast object[] to more narrow generic collection" {
        BeforeDiscovery {
            $testCases1 = @(
                ## It's intentional to have 'Command' to be `{$result = ...}` and run it with `. $Command`.
                ## This is because `$result = & {[List[int]]@(1,2)}` will cause the resulted List to be unraveled,
                ## and in that case `$result` would be just an object array.
                ## To prevent unraveling, Command needs to be `{, [List[int]]@(1,2)}`, but then the test case title
                ## would become `, [List[int]]@(1,2)`, which is more confusing than `$result = [List[int]]@(1,2)`.
                ## This is why the current form of `$result = [List[int]]@(1,2)` is used intentionally here.

                @{ Command = { $result = [Collections.Generic.List[int]]@(1) }; CollectionType = 'List`1'; ElementType = "Int32"; Elements = @(1) }
                @{ Command = { $result = [Collections.Generic.List[int]]@(1, 2) }; CollectionType = 'List`1'; ElementType = "Int32"; Elements = @(1, 2) }
                @{ Command = { $result = [Collections.Generic.List[int]]"4" }; CollectionType = 'List`1'; ElementType = "Int32"; Elements = @(4) }
                @{ Command = { $result = [Collections.Generic.List[int]]@("4", "5") }; CollectionType = 'List`1'; ElementType = "Int32"; Elements = @(4, 5) }

                @{ Command = { $result = [Collections.Generic.List[string]]@(1) }; CollectionType = 'List`1'; ElementType = "String"; Elements = @("1") }
                @{ Command = { $result = [Collections.Generic.List[string]]@(1, 2) }; CollectionType = 'List`1'; ElementType = "String"; Elements = @("1", "2") }
                @{ Command = { $result = [Collections.Generic.List[string]]1 }; CollectionType = 'List`1'; ElementType = "String"; Elements = @("1") }
                @{ Command = { $result = [Collections.Generic.List[string]]@("4") }; CollectionType = 'List`1'; ElementType = "String"; Elements = @("4") }

                @{ Command = { $result = [System.Collections.ObjectModel.Collection[int]]@(1) }; CollectionType = 'Collection`1'; ElementType = "Int32"; Elements = @(1) }
                @{ Command = { $result = [System.Collections.ObjectModel.Collection[int]]@(1, 2) }; CollectionType = 'Collection`1'; ElementType = "Int32"; Elements = @(1, 2) }
                @{ Command = { $result = [System.Collections.ObjectModel.Collection[int]]"4" }; CollectionType = 'Collection`1'; ElementType = "Int32"; Elements = @(4) }
                @{ Command = { $result = [System.Collections.ObjectModel.Collection[int]]@("4", "5") }; CollectionType = 'Collection`1'; ElementType = "Int32"; Elements = @(4, 5) }

                @{ Command = { $result = [Collections.Generic.List[System.IO.FileInfo]]@('TestFile') };
                    CollectionType = 'List`1'; ElementType = "FileInfo"; Elements = @('TestFile')
                }
                @{ Command = { $result = [Collections.Generic.List[System.IO.FileInfo]]@('TestFile1', 'TestFile2') };
                    CollectionType = 'List`1'; ElementType = "FileInfo"; Elements = @('TestFile1', 'TestFile2')
                }
                @{ Command = { $result = [Collections.Generic.List[System.IO.FileInfo]]'TestFile' };
                    CollectionType = 'List`1'; ElementType = "FileInfo"; Elements = @('TestFile')
                }
            )
        }

        It "<Command>" -TestCases $testCases1 {
            param($Command, $CollectionType, $ElementType, $Elements)

            $result = $null
            . $Command

            $result | Should -Not -BeNullOrEmpty
            $result.GetType().Name | Should -Be $CollectionType

            $genericArgs = $result.GetType().GetGenericArguments()
            $genericArgs.Length | Should -Be 1
            $genericArgs[0].Name | Should -Be $ElementType

            $result.Count | Should -Be $Elements.Length
            $result -join ";" | Should -Be ($Elements -join ";")
        }
    }

    It 'Should not convert invalid strings to type name using -as operator' {
        'int]whatever' -as [type] | Should -Be $null
    }
    
    It 'Should not convert invalid strings to type name using left-hand side operator' {
        {[Type] 'int]whatever'} | Should -Throw
    }
}

Describe "Type resolution should prefer assemblies in powershell assembly cache" -Tags "Feature" {

    BeforeAll {
        $cmdletCode = @'
namespace TestTypeResolution {
    using System.Management.Automation;
    [Cmdlet("Test", "TypeResolution")]
    public class TestTypeResolutionCommand : PSCmdlet {
        [Parameter()]
        public string Name { get; set; }

        protected override void BeginProcessing() {
            WriteObject(Name);
        }
    }

    public class TestTypeFoo {
        public string Foo { get; set; }
    }
}
'@
        $dupTypeCode = @'
namespace TestTypeResolution {
    public class TestTypeFoo {
        public string Bar { get; set; }
    }
}
'@

        $cmdletDllDir = Join-Path $TestDrive "cmdlet"
        $dupTypeDllDir = Join-Path $TestDrive "dupType"

        $null = New-Item -Path $cmdletDllDir, $dupTypeDllDir -ItemType Directory -Force

        $cmdletDllPath = Join-Path $cmdletDllDir "TestCmdlet.dll"
        $dupTypeDllPath = Join-Path $dupTypeDllDir "TestType.dll"

        Add-Type $cmdletCode -OutputAssembly $cmdletDllPath
        Add-Type $dupTypeCode -OutputAssembly $dupTypeDllPath

        $powershell = Join-Path $PSHOME "pwsh"
    }

    It "validate Type resolution should prefer the assembly loaded by Import-Module" {
        $command = @"
            Add-Type -Path $dupTypeDllPath
            Import-Module $cmdletDllPath
            [TestTypeResolution.TestTypeFoo].Assembly.Location
"@
        $location = & $powershell -noprofile -command $command
        $location | Should -Be $cmdletDllPath
    }
}

class M_Conversion {
    [int] Twice([int] $value) { return 2 * $value }
    [int] ThriceInstance([int] $value) { return 3 * $value }
    static [int] Thrice([int] $value) { return 3 * $value }
    static [int] Add([int] $i, [int16] $j) { return $i + $j }

    static [int] Apply([int] $value, [Func[int, int]] $function) {
        return $function.Invoke($value)
    }

    static [int] Apply([int] $v1, [Int16] $v2, [Func[int, int16, int]] $function) {
        return $function.Invoke($v1, $v2)
    }

    # check that we can handle at least 72 overloads
    static [char]     Foo([char] $i) { return $i }
    static [char]     Foo([char] $i, [char] $j) { return $i }
    static [char]     Foo([char] $i, [char] $j, [char] $k) { return $i }
    static [char]     Foo([char] $i, [char] $j, [char] $k, [char] $l) { return $i }
    static [char]     Foo([char] $i, [char] $j, [char] $k, [char] $l, [char] $m) { return $i }
    static [char]     Foo([char] $i, [char] $j, [char] $k, [char] $l, [char] $m, [char] $n) { return $i }
    static [char]     Foo([char] $i, [char] $j, [char] $k, [char] $l, [char] $m, [char] $n, [char] $o) { return $i }
    static [char]     Foo([char] $i, [char] $j, [char] $k, [char] $l, [char] $m, [char] $n, [char] $o, [char] $p) { return $i }
    static [int16]    Foo([int16] $i) { return $i }
    static [int16]    Foo([int16] $i, [int16] $j) { return $i }
    static [int16]    Foo([int16] $i, [int16] $j, [int16] $k) { return $i }
    static [int16]    Foo([int16] $i, [int16] $j, [int16] $k, [int16] $l) { return $i }
    static [int16]    Foo([int16] $i, [int16] $j, [int16] $k, [int16] $l, [int16] $m) { return $i }
    static [int16]    Foo([int16] $i, [int16] $j, [int16] $k, [int16] $l, [int16] $m, [int16] $n) { return $i }
    static [int16]    Foo([int16] $i, [int16] $j, [int16] $k, [int16] $l, [int16] $m, [int16] $n, [int16] $o) { return $i }
    static [int16]    Foo([int16] $i, [int16] $j, [int16] $k, [int16] $l, [int16] $m, [int16] $n, [int16] $o, [int16] $p) { return $i }
    static [int]      Foo([int] $i) { return $i }
    static [int]      Foo([int] $i, [int] $j) { return $i }
    static [int]      Foo([int] $i, [int] $j, [int] $k) { return $i }
    static [int]      Foo([int] $i, [int] $j, [int] $k, [int] $l) { return $i }
    static [int]      Foo([int] $i, [int] $j, [int] $k, [int] $l, [int] $m) { return $i }
    static [int]      Foo([int] $i, [int] $j, [int] $k, [int] $l, [int] $m, [int] $n) { return $i }
    static [int]      Foo([int] $i, [int] $j, [int] $k, [int] $l, [int] $m, [int] $n, [int] $o) { return $i }
    static [int]      Foo([int] $i, [int] $j, [int] $k, [int] $l, [int] $m, [int] $n, [int] $o, [int] $p) { return $i }
    static [UInt32]   Foo([UInt32] $i) { return $i }
    static [UInt32]   Foo([UInt32] $i, [UInt32] $j) { return $i }
    static [UInt32]   Foo([UInt32] $i, [UInt32] $j, [UInt32] $k) { return $i }
    static [UInt32]   Foo([UInt32] $i, [UInt32] $j, [UInt32] $k, [UInt32] $l) { return $i }
    static [UInt32]   Foo([UInt32] $i, [UInt32] $j, [UInt32] $k, [UInt32] $l, [UInt32] $m) { return $i }
    static [UInt32]   Foo([UInt32] $i, [UInt32] $j, [UInt32] $k, [UInt32] $l, [UInt32] $m, [UInt32] $n) { return $i }
    static [UInt32]   Foo([UInt32] $i, [UInt32] $j, [UInt32] $k, [UInt32] $l, [UInt32] $m, [UInt32] $n, [UInt32] $o) { return $i }
    static [UInt32]   Foo([UInt32] $i, [UInt32] $j, [UInt32] $k, [UInt32] $l, [UInt32] $m, [UInt32] $n, [UInt32] $o, [UInt32] $p) { return $i }
    static [UInt64]   Foo([UInt64] $i) { return $i }
    static [UInt64]   Foo([UInt64] $i, [UInt64] $j) { return $i }
    static [UInt64]   Foo([UInt64] $i, [UInt64] $j, [UInt64] $k) { return $i }
    static [UInt64]   Foo([UInt64] $i, [UInt64] $j, [UInt64] $k, [UInt64] $l) { return $i }
    static [UInt64]   Foo([UInt64] $i, [UInt64] $j, [UInt64] $k, [UInt64] $l, [UInt64] $m) { return $i }
    static [UInt64]   Foo([UInt64] $i, [UInt64] $j, [UInt64] $k, [UInt64] $l, [UInt64] $m, [UInt64] $n) { return $i }
    static [UInt64]   Foo([UInt64] $i, [UInt64] $j, [UInt64] $k, [UInt64] $l, [UInt64] $m, [UInt64] $n, [UInt64] $o) { return $i }
    static [UInt64]   Foo([UInt64] $i, [UInt64] $j, [UInt64] $k, [UInt64] $l, [UInt64] $m, [UInt64] $n, [UInt64] $o, [UInt64] $p) { return $i }
    static [float]    Foo([float] $i) { return $i }
    static [float]    Foo([float] $i, [float] $j) { return $i }
    static [float]    Foo([float] $i, [float] $j, [float] $k) { return $i }
    static [float]    Foo([float] $i, [float] $j, [float] $k, [float] $l) { return $i }
    static [float]    Foo([float] $i, [float] $j, [float] $k, [float] $l, [float] $m) { return $i }
    static [float]    Foo([float] $i, [float] $j, [float] $k, [float] $l, [float] $m, [float] $n) { return $i }
    static [float]    Foo([float] $i, [float] $j, [float] $k, [float] $l, [float] $m, [float] $n, [float] $o) { return $i }
    static [float]    Foo([float] $i, [float] $j, [float] $k, [float] $l, [float] $m, [float] $n, [float] $o, [float] $p) { return $i }
    static [double]   Foo([double] $i) { return $i }
    static [double]   Foo([double] $i, [double] $j) { return $i }
    static [double]   Foo([double] $i, [double] $j, [double] $k) { return $i }
    static [double]   Foo([double] $i, [double] $j, [double] $k, [double] $l) { return $i }
    static [double]   Foo([double] $i, [double] $j, [double] $k, [double] $l, [double] $m) { return $i }
    static [double]   Foo([double] $i, [double] $j, [double] $k, [double] $l, [double] $m, [double] $n) { return $i }
    static [double]   Foo([double] $i, [double] $j, [double] $k, [double] $l, [double] $m, [double] $n, [double] $o) { return $i }
    static [double]   Foo([double] $i, [double] $j, [double] $k, [double] $l, [double] $m, [double] $n, [double] $o, [double] $p) { return $i }
    static [IntPtr]   Foo([IntPtr] $i) { return $i }
    static [IntPtr]   Foo([IntPtr] $i, [IntPtr] $j) { return $i }
    static [IntPtr]   Foo([IntPtr] $i, [IntPtr] $j, [IntPtr] $k) { return $i }
    static [IntPtr]   Foo([IntPtr] $i, [IntPtr] $j, [IntPtr] $k, [IntPtr] $l) { return $i }
    static [IntPtr]   Foo([IntPtr] $i, [IntPtr] $j, [IntPtr] $k, [IntPtr] $l, [IntPtr] $m) { return $i }
    static [IntPtr]   Foo([IntPtr] $i, [IntPtr] $j, [IntPtr] $k, [IntPtr] $l, [IntPtr] $m, [IntPtr] $n) { return $i }
    static [IntPtr]   Foo([IntPtr] $i, [IntPtr] $j, [IntPtr] $k, [IntPtr] $l, [IntPtr] $m, [IntPtr] $n, [IntPtr] $o) { return $i }
    static [IntPtr]   Foo([IntPtr] $i, [IntPtr] $j, [IntPtr] $k, [IntPtr] $l, [IntPtr] $m, [IntPtr] $n, [IntPtr] $o, [IntPtr] $p) { return $i }
    static [timespan] Foo([timespan] $i) { return $i }
    static [timespan] Foo([timespan] $i, [timespan] $j) { return $i }
    static [timespan] Foo([timespan] $i, [timespan] $j, [timespan] $k) { return $i }
    static [timespan] Foo([timespan] $i, [timespan] $j, [timespan] $k, [timespan] $l) { return $i }
    static [timespan] Foo([timespan] $i, [timespan] $j, [timespan] $k, [timespan] $l, [timespan] $m) { return $i }
    static [timespan] Foo([timespan] $i, [timespan] $j, [timespan] $k, [timespan] $l, [timespan] $m, [timespan] $n) { return $i }
    static [timespan] Foo([timespan] $i, [timespan] $j, [timespan] $k, [timespan] $l, [timespan] $m, [timespan] $n, [timespan] $o) { return $i }
    static [timespan] Foo([timespan] $i, [timespan] $j, [timespan] $k, [timespan] $l, [timespan] $m, [timespan] $n, [timespan] $o, [timespan] $p) { return $i }
}

Describe 'method conversion' -Tags 'CI' {

    It 'converts static method as Func does not throw' {
        { [Func[int, int]] [M_Conversion]::Thrice } | Should -Not -Throw
    }

    It 'converts static method as Func is non null' {
        ([Func[int, int]] [M_Conversion]::Thrice) | Should -Not -BeNullOrEmpty
    }

    It 'calls static method as Func' {
        $f = [Func[int, int]] [M_Conversion]::Thrice
        [M_Conversion]::Apply(1, $f) | Should -Be 3
    }

    It 'calls static method as Func' {
        $f = [Func[int, int16, int]] [M_Conversion]::Add
        [M_Conversion]::Apply(3, 4, $f) | Should -Be 7
    }

    It 'calls static method as Func no cast' {
        [M_Conversion]::Apply(3, 4, [M_Conversion]::Add) | Should -Be 7
    }

    It 'converts instance psmethodinfo to Func' {
        $m = [M_Conversion]::new()
        { [Func[int, int]] $m.Twice } | Should -Not -Throw

        $f = [Func[int, int16, int]] [M_Conversion]::Add
        $f.Invoke(2, 6) | Should -Be 8
    }
    It "can call all overloads of M::Foo" {
        [Func[char, char]] $f1 = [M_Conversion]::Foo
        $f1.Invoke(10) | Should -Be 10
        [Func[char, char, char]] $f2 = [M_Conversion]::Foo
        $f2.Invoke(10, 1) | Should -Be 10
        [Func[char, char, char, char]] $f3 = [M_Conversion]::Foo
        $f3.Invoke(10, 1, 2) | Should -Be 10
        [Func[char, char, char, char, char]] $f4 = [M_Conversion]::Foo
        $f4.Invoke(10, 1, 2, 3) | Should -Be 10
        [Func[char, char, char, char, char, char]] $f5 = [M_Conversion]::Foo
        $f5.Invoke(10, 1, 2, 3, 4) | Should -Be 10
        [Func[char, char, char, char, char, char, char]] $f6 = [M_Conversion]::Foo
        $f6.Invoke(10, 1, 2, 3, 4, 5) | Should -Be 10
        [Func[char, char, char, char, char, char, char, char]] $f7 = [M_Conversion]::Foo
        $f7.Invoke(10, 1, 2, 3, 4, 5, 6) | Should -Be 10
        [Func[char, char, char, char, char, char, char, char, char]] $f8 = [M_Conversion]::Foo
        $f8.Invoke(10, 1, 2, 3, 4, 5, 6, 7) | Should -Be 10
        [Func[int16, int16]] $f9 = [M_Conversion]::Foo
        $f9.Invoke(10) | Should -Be 10
        [Func[int16, int16, int16]] $f10 = [M_Conversion]::Foo
        $f10.Invoke(10, 1) | Should -Be 10
        [Func[int16, int16, int16, int16]] $f11 = [M_Conversion]::Foo
        $f11.Invoke(10, 1, 2) | Should -Be 10
        [Func[int16, int16, int16, int16, int16]] $f12 = [M_Conversion]::Foo
        $f12.Invoke(10, 1, 2, 3) | Should -Be 10
        [Func[int16, int16, int16, int16, int16, int16]] $f13 = [M_Conversion]::Foo
        $f13.Invoke(10, 1, 2, 3, 4) | Should -Be 10
        [Func[int16, int16, int16, int16, int16, int16, int16]] $f14 = [M_Conversion]::Foo
        $f14.Invoke(10, 1, 2, 3, 4, 5) | Should -Be 10
        [Func[int16, int16, int16, int16, int16, int16, int16, int16]] $f15 = [M_Conversion]::Foo
        $f15.Invoke(10, 1, 2, 3, 4, 5, 6) | Should -Be 10
        [Func[int16, int16, int16, int16, int16, int16, int16, int16, int16]] $f16 = [M_Conversion]::Foo
        $f16.Invoke(10, 1, 2, 3, 4, 5, 6, 7) | Should -Be 10
        [Func[int, int]] $f17 = [M_Conversion]::Foo
        $f17.Invoke(10) | Should -Be 10
        [Func[int, int, int]] $f18 = [M_Conversion]::Foo
        $f18.Invoke(10, 1) | Should -Be 10
        [Func[int, int, int, int]] $f19 = [M_Conversion]::Foo
        $f19.Invoke(10, 1, 2) | Should -Be 10
        [Func[int, int, int, int, int]] $f20 = [M_Conversion]::Foo
        $f20.Invoke(10, 1, 2, 3) | Should -Be 10
        [Func[int, int, int, int, int, int]] $f21 = [M_Conversion]::Foo
        $f21.Invoke(10, 1, 2, 3, 4) | Should -Be 10
        [Func[int, int, int, int, int, int, int]] $f22 = [M_Conversion]::Foo
        $f22.Invoke(10, 1, 2, 3, 4, 5) | Should -Be 10
        [Func[int, int, int, int, int, int, int, int]] $f23 = [M_Conversion]::Foo
        $f23.Invoke(10, 1, 2, 3, 4, 5, 6) | Should -Be 10
        [Func[int, int, int, int, int, int, int, int, int]] $f24 = [M_Conversion]::Foo
        $f24.Invoke(10, 1, 2, 3, 4, 5, 6, 7) | Should -Be 10
        [Func[UInt32, UInt32]] $f25 = [M_Conversion]::Foo
        $f25.Invoke(10) | Should -Be 10
        [Func[UInt32, UInt32, UInt32]] $f26 = [M_Conversion]::Foo
        $f26.Invoke(10, 1) | Should -Be 10
        [Func[UInt32, UInt32, UInt32, UInt32]] $f27 = [M_Conversion]::Foo
        $f27.Invoke(10, 1, 2) | Should -Be 10
        [Func[UInt32, UInt32, UInt32, UInt32, UInt32]] $f28 = [M_Conversion]::Foo
        $f28.Invoke(10, 1, 2, 3) | Should -Be 10
        [Func[UInt32, UInt32, UInt32, UInt32, UInt32, UInt32]] $f29 = [M_Conversion]::Foo
        $f29.Invoke(10, 1, 2, 3, 4) | Should -Be 10
        [Func[UInt32, UInt32, UInt32, UInt32, UInt32, UInt32, UInt32]] $f30 = [M_Conversion]::Foo
        $f30.Invoke(10, 1, 2, 3, 4, 5) | Should -Be 10
        [Func[UInt32, UInt32, UInt32, UInt32, UInt32, UInt32, UInt32, UInt32]] $f31 = [M_Conversion]::Foo
        $f31.Invoke(10, 1, 2, 3, 4, 5, 6) | Should -Be 10
        [Func[UInt32, UInt32, UInt32, UInt32, UInt32, UInt32, UInt32, UInt32, UInt32]] $f32 = [M_Conversion]::Foo
        $f32.Invoke(10, 1, 2, 3, 4, 5, 6, 7) | Should -Be 10
        [Func[UInt64, UInt64]] $f33 = [M_Conversion]::Foo
        $f33.Invoke(10) | Should -Be 10
        [Func[UInt64, UInt64, UInt64]] $f34 = [M_Conversion]::Foo
        $f34.Invoke(10, 1) | Should -Be 10
        [Func[UInt64, UInt64, UInt64, UInt64]] $f35 = [M_Conversion]::Foo
        $f35.Invoke(10, 1, 2) | Should -Be 10
        [Func[UInt64, UInt64, UInt64, UInt64, UInt64]] $f36 = [M_Conversion]::Foo
        $f36.Invoke(10, 1, 2, 3) | Should -Be 10
        [Func[UInt64, UInt64, UInt64, UInt64, UInt64, UInt64]] $f37 = [M_Conversion]::Foo
        $f37.Invoke(10, 1, 2, 3, 4) | Should -Be 10
        [Func[UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64]] $f38 = [M_Conversion]::Foo
        $f38.Invoke(10, 1, 2, 3, 4, 5) | Should -Be 10
        [Func[UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64]] $f39 = [M_Conversion]::Foo
        $f39.Invoke(10, 1, 2, 3, 4, 5, 6) | Should -Be 10
        [Func[UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64, UInt64]] $f40 = [M_Conversion]::Foo
        $f40.Invoke(10, 1, 2, 3, 4, 5, 6, 7) | Should -Be 10
        [Func[float, float]] $f41 = [M_Conversion]::Foo
        $f41.Invoke(10) | Should -Be 10
        [Func[float, float, float]] $f42 = [M_Conversion]::Foo
        $f42.Invoke(10, 1) | Should -Be 10
        [Func[float, float, float, float]] $f43 = [M_Conversion]::Foo
        $f43.Invoke(10, 1, 2) | Should -Be 10
        [Func[float, float, float, float, float]] $f44 = [M_Conversion]::Foo
        $f44.Invoke(10, 1, 2, 3) | Should -Be 10
        [Func[float, float, float, float, float, float]] $f45 = [M_Conversion]::Foo
        $f45.Invoke(10, 1, 2, 3, 4) | Should -Be 10
        [Func[float, float, float, float, float, float, float]] $f46 = [M_Conversion]::Foo
        $f46.Invoke(10, 1, 2, 3, 4, 5) | Should -Be 10
        [Func[float, float, float, float, float, float, float, float]] $f47 = [M_Conversion]::Foo
        $f47.Invoke(10, 1, 2, 3, 4, 5, 6) | Should -Be 10
        [Func[float, float, float, float, float, float, float, float, float]] $f48 = [M_Conversion]::Foo
        $f48.Invoke(10, 1, 2, 3, 4, 5, 6, 7) | Should -Be 10
        [Func[double, double]] $f49 = [M_Conversion]::Foo
        $f49.Invoke(10) | Should -Be 10
        [Func[double, double, double]] $f50 = [M_Conversion]::Foo
        $f50.Invoke(10, 1) | Should -Be 10
        [Func[double, double, double, double]] $f51 = [M_Conversion]::Foo
        $f51.Invoke(10, 1, 2) | Should -Be 10
        [Func[double, double, double, double, double]] $f52 = [M_Conversion]::Foo
        $f52.Invoke(10, 1, 2, 3) | Should -Be 10
        [Func[double, double, double, double, double, double]] $f53 = [M_Conversion]::Foo
        $f53.Invoke(10, 1, 2, 3, 4) | Should -Be 10
        [Func[double, double, double, double, double, double, double]] $f54 = [M_Conversion]::Foo
        $f54.Invoke(10, 1, 2, 3, 4, 5) | Should -Be 10
        [Func[double, double, double, double, double, double, double, double]] $f55 = [M_Conversion]::Foo
        $f55.Invoke(10, 1, 2, 3, 4, 5, 6) | Should -Be 10
        [Func[double, double, double, double, double, double, double, double, double]] $f56 = [M_Conversion]::Foo
        $f56.Invoke(10, 1, 2, 3, 4, 5, 6, 7) | Should -Be 10
        [Func[IntPtr, IntPtr]] $f57 = [M_Conversion]::Foo
        $f57.Invoke(10) | Should -Be 10
        [Func[IntPtr, IntPtr, IntPtr]] $f58 = [M_Conversion]::Foo
        $f58.Invoke(10, 1) | Should -Be 10
        [Func[IntPtr, IntPtr, IntPtr, IntPtr]] $f59 = [M_Conversion]::Foo
        $f59.Invoke(10, 1, 2) | Should -Be 10
        [Func[IntPtr, IntPtr, IntPtr, IntPtr, IntPtr]] $f60 = [M_Conversion]::Foo
        $f60.Invoke(10, 1, 2, 3) | Should -Be 10
        [Func[IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr]] $f61 = [M_Conversion]::Foo
        $f61.Invoke(10, 1, 2, 3, 4) | Should -Be 10
        [Func[IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr]] $f62 = [M_Conversion]::Foo
        $f62.Invoke(10, 1, 2, 3, 4, 5) | Should -Be 10
        [Func[IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr]] $f63 = [M_Conversion]::Foo
        $f63.Invoke(10, 1, 2, 3, 4, 5, 6) | Should -Be 10
        [Func[IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr]] $f64 = [M_Conversion]::Foo
        $f64.Invoke(10, 1, 2, 3, 4, 5, 6, 7) | Should -Be 10
        $timespan = [timespan]::FromMinutes(62)
        [Func[timespan, timespan]] $f65 = [M_Conversion]::Foo
        $f65.Invoke($timeSpan) | Should -Be $timeSpan
        [Func[timespan, timespan, timespan]] $f66 = [M_Conversion]::Foo
        $f66.Invoke($timeSpan, [Timespan]::Zero) | Should -Be $timeSpan
        [Func[timespan, timespan, timespan, timespan]] $f67 = [M_Conversion]::Foo
        $f67.Invoke($timeSpan, [Timespan]::Zero, [Timespan]::Zero) | Should -Be $timeSpan
        [Func[timespan, timespan, timespan, timespan, timespan]] $f68 = [M_Conversion]::Foo
        $f68.Invoke($timeSpan, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero) | Should -Be $timeSpan
        [Func[timespan, timespan, timespan, timespan, timespan, timespan]] $f69 = [M_Conversion]::Foo
        $f69.Invoke($timeSpan, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero) | Should -Be $timeSpan
        [Func[timespan, timespan, timespan, timespan, timespan, timespan, timespan]] $f70 = [M_Conversion]::Foo
        $f70.Invoke($timeSpan, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero) | Should -Be $timeSpan
        [Func[timespan, timespan, timespan, timespan, timespan, timespan, timespan, timespan]] $f71 = [M_Conversion]::Foo
        $f71.Invoke($timeSpan, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero) | Should -Be $timeSpan
        [Func[timespan, timespan, timespan, timespan, timespan, timespan, timespan, timespan, timespan]] $f72 = [M_Conversion]::Foo
        $f72.Invoke($timeSpan, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero) | Should -Be $timeSpan
    }

    BeforeAll {
        enum E {
            Day;
            Week;
            Year;
        }

        class N {
            ## Attempt to convert methods to Func<System.IO.FileInfo, string, object>.

            ## Different methods with same overload signatures.
            ## The second and third overloads match the target delegate with variance.
            [string] GetA([int] $i, [string] $s) { return "GetA-int-string-string" }
            [string] GetA([System.IO.FileSystemInfo] $fsinfo, [object] $o) { return "GetA-filesysteminfo-object-string" }
            [string] GetA([System.IO.FileInfo] $finfo, [object] $o) { return "GetA-fileinfo-object-string" }

            [string] GetAPrime([int] $i, [string] $s) { return "GetAPrime-int-string-string" }
            [string] GetAPrime([System.IO.FileSystemInfo] $fsinfo, [object] $o) { return "GetAPrime-filesysteminfo-object-string" }
            [string] GetAPrime([System.IO.FileInfo] $finfo, [object] $o) { return "GetAPrime-fileinfo-object-string" }

            static [string] GetAStatic([int] $i, [string] $s) { return "GetAStatic-int-string-string" }
            static [string] GetAStatic([System.IO.FileSystemInfo] $fsinfo, [object] $o) { return "GetAStatic-filesysteminfo-object-string" }
            static [string] GetAStatic([System.IO.FileInfo] $finfo, [object] $o) { return "GetAStatic-fileinfo-object-string" }

            ## Different methods with same overload signatures.
            ## The first overload matches the target delegate with variance,
            ## while the second overload matches the target delegate exactly.
            [string] GetB([System.IO.FileSystemInfo] $fsinfo, [object] $o) { return "GetB-filesysteminfo-object-string" }
            [object] GetB([System.IO.FileInfo] $finfo, [string] $s) { return "GetB-fileinfo-string-object" }
            [string] GetB([datetime] $d) { return "GetB-datetime-string" }

            [string] GetBPrime([System.IO.FileSystemInfo] $fsinfo, [object] $o) { return "GetBPrime-filesysteminfo-object-string" }
            [object] GetBPrime([System.IO.FileInfo] $finfo, [string] $s) { return "GetBPrime-fileinfo-string-object" }
            [string] GetBPrime([datetime] $d) { return "GetBPrime-datetime-string" }

            static [string] GetBStatic([System.IO.FileSystemInfo] $fsinfo, [object] $o) { return "GetBStatic-filesysteminfo-object-string" }
            static [object] GetBStatic([System.IO.FileInfo] $finfo, [string] $s) { return "GetBStatic-fileinfo-string-object" }
            static [string] GetBStatic([datetime] $d) { return "GetBStatic-datetime-string" }

            ## Test enum parameter type
            [object] GetC([E] $e) { return $e.ToString() }
        }
    }

    It "Different method overloads with same signatures/orders should have same PSMethod type" {
        $n = [N]::new()

        $n.GetA.GetType() | Should -Be ($n.GetAPrime.GetType())
        $n.GetA.GetType() | Should -Be ([N]::GetAStatic.GetType())

        $n.GetB.GetType() | Should -Be ($n.GetBPrime.GetType())
        $n.GetB.GetType() | Should -Be ([N]::GetBStatic.GetType())
    }

    It "Match signature with variance and use the first match when there is no exact match" {
        $n = [N]::new()

        [Func[[System.IO.FileInfo], [string], [object]]] $f = $n.GetA
        $f.Invoke($null, $null) | Should -BeExactly "GetA-filesysteminfo-object-string"

        $f = $n.GetAPrime ## $n.GetAPrime has the same type as $n.GetA, so it should hit the conversion cache
        $f.Invoke([System.IO.FileInfo]::new("aaa"), "bbb") | Should -BeExactly "GetAPrime-filesysteminfo-object-string"

        $f = [N]::GetAStatic ## [N]::GetAStatic has the same type as $n.GetA, so it should hit the conversion cache
        $f.Invoke($null, "") | Should -BeExactly "GetAStatic-filesysteminfo-object-string"
    }

    It "Exact match is preferred over match with variance" {
        $n = [N]::new()

        [Func[[System.IO.FileInfo], [string], [object]]] $f = $n.GetB
        $f.Invoke($null, $null) | Should -BeExactly "GetB-fileinfo-string-object"

        $f = $n.GetBPrime ## $n.GetBPrime has the same type as $n.GetB, so it should hit the conversion cache
        $f.Invoke([System.IO.FileInfo]::new("ccc"), "ddd") | Should -BeExactly "GetBPrime-fileinfo-string-object"

        $f = [N]::GetBStatic ## [N]::GetBStatic has the same type as $n.GetB, so it should hit the conversion cache
        $f.Invoke($null, "") | Should -BeExactly "GetBStatic-fileinfo-string-object"
    }

    It "Test enum type parameter" {
        $n = [N]::new()

        [Func[[E], [object]]] $f = $n.GetC
        $f.Invoke([E]::Week) | Should -BeExactly "Week"
    }

    It "Test fail-to-convert code path" {
        $n = [N]::new()
        { [System.Management.Automation.LanguagePrimitives]::ConvertTo($n.GetC, [Func[[int], [object]]]) } | Should -Throw -ErrorId "PSInvalidCastException"
    }

    BeforeDiscovery {
        $TestCases = @(
            @{ Number = "100y"; Value = "100"; Type = [int] }
            @{ Number = "100uy"; Value = "100"; Type = [double] }
            @{ Number = "1200u"; Value = "1200"; Type = [short] }
            @{ Number = "1200L"; Value = "1200"; Type = [int] }
            @{ Number = "127ul"; Value = "127"; Type = [ulong] }
            @{ Number = "127d"; Value = "127"; Type = [byte] }
            @{ Number = "127s"; Value = "127"; Type = [sbyte] }
            @{ Number = "127y"; Value = "127"; Type = [uint] }
            @{ Number = "100n"; Value = "100"; Type = [int] }
            @{ Number = "1234s"; Value = "1234"; Type = [bigint] }
        )
    }
    It "Correctly casts <Number> to value <Value> as type <Type>" -TestCases $TestCases {
        param($Number, $Value, $Type)

        $Result = $Number -as $Type
        $Result | Should -Be $Value
        $Result | Should -BeOfType $Type
    }

    BeforeDiscovery {
        $TestCases = @(
            @{ Number = "200y" }
            @{ Number = "300uy" }
            @{ Number = "70000us" }
            @{ Number = "40000s" }
        )
    }
    It "Fails to cast invalid PowerShell-Style suffixed numeral <Number>" -TestCases $TestCases {
        param($Number)

        $Result = $Number -as [int]
        $Result | Should -BeNullOrEmpty
    }
}

Describe 'float/double precision when converting to string' -Tags "CI" {
    It "<SourceType>-to-[string] conversion in PowerShell should use the precision specifier <Format> (<ToStringResult>)" -TestCases @(
        @{ SourceType = [double]; Format = "G15"; ValueScript = { 1.1 * 3 }; StringConversionResult = "3.3"; ToStringResult = "3.3000000000000003" }
        @{ SourceType = [double]; Format = "G15"; ValueScript = { 1.1 * 6 }; StringConversionResult = "6.6"; ToStringResult = "6.6000000000000005" }
        @{ SourceType = [double]; Format = "G15"; ValueScript = { [System.Math]::E }; StringConversionResult = [System.Math]::E.ToString("G15"); ToStringResult = [System.Math]::E.ToString() }
        @{ SourceType = [double]; Format = "G15"; ValueScript = { [System.Math]::PI }; StringConversionResult = [System.Math]::PI.ToString("G15"); ToStringResult = [System.Math]::PI.ToString() }
        @{ SourceType = [float]; Format = "G7"; ValueScript = { [float]$f = 1.1; ($f * 3).ToSingle([cultureinfo]::InvariantCulture) }; StringConversionResult = "3.3"; ToStringResult = "3.3000002" }
        @{ SourceType = [float]; Format = "G7"; ValueScript = { [float]$f = 1.1; ($f * 6).ToSingle([cultureinfo]::InvariantCulture) }; StringConversionResult = "6.6"; ToStringResult = "6.6000004" }
        @{ SourceType = [float]; Format = "G7"; ValueScript = { [float]::MaxValue }; StringConversionResult = [float]::MaxValue.ToString("G7"); ToStringResult = [float]::MaxValue.ToString() }
        @{ SourceType = [float]; Format = "G7"; ValueScript = { [float]::MinValue }; StringConversionResult = [float]::MinValue.ToString("G7"); ToStringResult = [float]::MinValue.ToString() }
    ) {
        param($SourceType, $ValueScript, $StringConversionResult, $ToStringResult)

        $value = & $ValueScript
        $value | Should -BeOfType $SourceType
        $value.ToString() | Should -BeExactly $ToStringResult

        $value -as [string] | Should -BeExactly $StringConversionResult
        [string]$value | Should -BeExactly $StringConversionResult
        [System.Management.Automation.LanguagePrimitives]::ConvertTo($value, [string]) | Should -BeExactly $StringConversionResult
        "$value" | Should -BeExactly $StringConversionResult
        $value | Out-String | ForEach-Object -MemberName Trim | Should -BeExactly $StringConversionResult
    }
}

Describe 'Casting Behaviour of Boolean/Null to Numeral' -Tags CI {

    BeforeDiscovery {
        $NullToNumeral = @(
            @{ Type = [sbyte]; ExpectedResult = 0y }
            @{ Type = [byte]; ExpectedResult = 0uy }
            @{ Type = [short]; ExpectedResult = 0s }
            @{ Type = [ushort]; ExpectedResult = 0us }
            @{ Type = [int]; ExpectedResult = 0 }
            @{ Type = [uint]; ExpectedResult = 0u }
            @{ Type = [long]; ExpectedResult = 0l }
            @{ Type = [ulong]; ExpectedResult = 0ul }
            @{ Type = [decimal]; ExpectedResult = 0d }
            @{ Type = [float]; ExpectedResult = [float]0 }
            @{ Type = [double]; ExpectedResult = 0.0 }
            @{ Type = [bigint]; ExpectedResult = 0n }
        )
        $BoolToNumeral = @(
            @{ Type = [sbyte]; Value = $true; ExpectedResult = 1y }
            @{ Type = [sbyte]; Value = $false; ExpectedResult = 0y }
            @{ Type = [byte]; Value = $true; ExpectedResult = 1uy }
            @{ Type = [byte]; Value = $false; ExpectedResult = 0uy }
            @{ Type = [short]; Value = $true; ExpectedResult = 1s }
            @{ Type = [short]; Value = $false; ExpectedResult = 0s }
            @{ Type = [ushort]; Value = $true; ExpectedResult = 1us }
            @{ Type = [ushort]; Value = $false; ExpectedResult = 0us }
            @{ Type = [int]; Value = $true; ExpectedResult = 1 }
            @{ Type = [int]; Value = $false; ExpectedResult = 0 }
            @{ Type = [uint]; Value = $true; ExpectedResult = 1u }
            @{ Type = [uint]; Value = $false; ExpectedResult = 0u }
            @{ Type = [long]; Value = $true; ExpectedResult = 1l }
            @{ Type = [long]; Value = $false; ExpectedResult = 0l }
            @{ Type = [ulong]; Value = $true; ExpectedResult = 1ul }
            @{ Type = [ulong]; Value = $false; ExpectedResult = 0ul }
            @{ Type = [decimal]; Value = $true; ExpectedResult = 1d }
            @{ Type = [decimal]; Value = $false; ExpectedResult = 0d }
            @{ Type = [float]; Value = $true; ExpectedResult = [float]1 }
            @{ Type = [float]; Value = $false; ExpectedResult = [float]0 }
            @{ Type = [double]; Value = $true; ExpectedResult = 1.0 }
            @{ Type = [double]; Value = $false; ExpectedResult = 0.0 }
            @{ Type = [bigint]; Value = $true; ExpectedResult = 1n }
            @{ Type = [bigint]; Value = $false; ExpectedResult = 0n }
        )
    }

    It 'should correctly convert $null to <Type> as <ExpectedResult>' -TestCases $NullToNumeral {
        param($Type, $ExpectedResult)

        $result = $null -as $Type
        $result | Should -Be $ExpectedResult
        $result | Should -BeOfType $Type
    }

    It 'should correctly convert <Value> to <Type> as <ExpectedResult>' -TestCases $BoolToNumeral {
        param($Type, $Value, $ExpectedResult)

        $result = $Value -as $Type
        $result | Should -Be $ExpectedResult
        $result | Should -BeOfType $Type
    }
}
