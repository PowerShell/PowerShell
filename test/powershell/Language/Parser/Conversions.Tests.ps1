Describe 'conversion syntax' -Tags "CI" {
    # these test suite covers ([<type>]<expression>).<method>() syntax.
    # it mixes two purposes: casting and super-class method calls.

    It 'converts array of single enum to bool' {
        # This test relies on the fact that [ConsoleColor]::Black is 0 and all other values are non-zero
        [bool]@([ConsoleColor]::Black) | Should Be $false
        [bool]@([ConsoleColor]::Yellow) | Should Be $true
    }

    It 'calls virtual method non-virtually' {
        ([object]"abc").ToString() | Should Be "System.String"

        # generate random string to avoid JIT optimization
        $r = [guid]::NewGuid().Guid
        ([object]($r + "a")).Equals(($r + "a")) | Should Be $false
    }

    It 'calls method on a super-type, when conversion syntax used' {
        # This test relies on the fact that there are overloads (at least 2) for ToString method.
        ([System.Management.Automation.ActionPreference]"Stop").ToString() | Should Be "Stop"
    }

    Context "Cast object[] to more narrow generic collection" {
        BeforeAll {
            $testCases1 = @(
                ## It's intentional to have 'Command' to be `{$result = ...}` and run it with `. $Command`.
                ## This is because `$result = & {[List[int]]@(1,2)}` will cause the resulted List to be unraveled,
                ## and in that case `$result` would be just an object array.
                ## To prevent unraveling, Command needs to be `{, [List[int]]@(1,2)}`, but then the test case title
                ## would become `, [List[int]]@(1,2)`, which is more confusing than `$result = [List[int]]@(1,2)`.
                ## This is why the current form of `$result = [List[int]]@(1,2)` is used intentionally here.

                @{ Command = {$result = [Collections.Generic.List[int]]@(1)};       CollectionType = 'List`1'; ElementType = "Int32";  Elements = @(1) }
                @{ Command = {$result = [Collections.Generic.List[int]]@(1,2)};     CollectionType = 'List`1'; ElementType = "Int32";  Elements = @(1,2) }
                @{ Command = {$result = [Collections.Generic.List[int]]"4"};        CollectionType = 'List`1'; ElementType = "Int32";  Elements = @(4) }
                @{ Command = {$result = [Collections.Generic.List[int]]@("4","5")}; CollectionType = 'List`1'; ElementType = "Int32";  Elements = @(4,5) }

                @{ Command = {$result = [Collections.Generic.List[string]]@(1)};    CollectionType = 'List`1'; ElementType = "String"; Elements = @("1") }
                @{ Command = {$result = [Collections.Generic.List[string]]@(1,2)};  CollectionType = 'List`1'; ElementType = "String"; Elements = @("1","2") }
                @{ Command = {$result = [Collections.Generic.List[string]]1};       CollectionType = 'List`1'; ElementType = "String"; Elements = @("1") }
                @{ Command = {$result = [Collections.Generic.List[string]]@("4")};  CollectionType = 'List`1'; ElementType = "String"; Elements = @("4") }

                @{ Command = {$result = [System.Collections.ObjectModel.Collection[int]]@(1)};       CollectionType = 'Collection`1'; ElementType = "Int32"; Elements = @(1) }
                @{ Command = {$result = [System.Collections.ObjectModel.Collection[int]]@(1,2)};     CollectionType = 'Collection`1'; ElementType = "Int32"; Elements = @(1,2) }
                @{ Command = {$result = [System.Collections.ObjectModel.Collection[int]]"4"};        CollectionType = 'Collection`1'; ElementType = "Int32"; Elements = @(4) }
                @{ Command = {$result = [System.Collections.ObjectModel.Collection[int]]@("4","5")}; CollectionType = 'Collection`1'; ElementType = "Int32"; Elements = @(4,5) }

                @{ Command = {$result = [Collections.Generic.List[System.IO.FileInfo]]@('TestFile')};
                    CollectionType = 'List`1'; ElementType = "FileInfo";  Elements = @('TestFile') }
                @{ Command = {$result = [Collections.Generic.List[System.IO.FileInfo]]@('TestFile1', 'TestFile2')};
                    CollectionType = 'List`1'; ElementType = "FileInfo";  Elements = @('TestFile1', 'TestFile2') }
                @{ Command = {$result = [Collections.Generic.List[System.IO.FileInfo]]'TestFile'};
                    CollectionType = 'List`1'; ElementType = "FileInfo";  Elements = @('TestFile') }
            )
        }

        It "<Command>" -TestCases $testCases1 {
            param($Command, $CollectionType, $ElementType, $Elements)

            $result = $null
            . $Command

            $result | Should Not BeNullOrEmpty
            $result.GetType().Name | Should Be $CollectionType

            $genericArgs = $result.GetType().GetGenericArguments()
            $genericArgs.Length | Should Be 1
            $genericArgs[0].Name | Should Be $ElementType

            $result.Count | Should Be $Elements.Length
            $result -join ";" | Should Be ($Elements -join ";")
        }
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

        $cmdletDllDir  = Join-Path $TestDrive "cmdlet"
        $dupTypeDllDir = Join-Path $TestDrive "dupType"

        $null = New-Item -Path $cmdletDllDir, $dupTypeDllDir -ItemType Directory -Force

        $cmdletDllPath  = Join-Path $cmdletDllDir "TestCmdlet.dll"
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
        $location | Should Be $cmdletDllPath
    }
}

Describe 'method conversion' -Tags 'CI' {
    class M {
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

        static [int16] Foo([int16] $i) {return $i}
        static [int16] Foo([int16] $i, [int16] $j) {return $i}
        static [int16] Foo([int16] $i, [int16] $j, [int16] $k) {return $i}
        static [int16] Foo([int16] $i, [int16] $j, [int16] $k, [int16] $l) {return $i}
        static [int] Foo([int] $i) {return $i}
        static [int] Foo([int] $i, [int] $j) {return $i}
        static [int] Foo([int] $i, [int] $j, [int] $k) {return $i}
        static [int] Foo([int] $i, [int] $j, [int] $k, [int] $l) {return $i}
        static [Uint64] Foo([Uint64] $i) {return $i}
        static [Uint64] Foo([Uint64] $i, [Uint64] $j) {return $i}
        static [Uint64] Foo([Uint64] $i, [Uint64] $j, [Uint64] $k) {return $i}
        static [Uint64] Foo([Uint64] $i, [Uint64] $j, [Uint64] $k, [Uint64] $l) {return $i}
        static [double] Foo([double] $i) {return $i}
        static [double] Foo([double] $i, [double] $j) {return $i}
        static [double] Foo([double] $i, [double] $j, [double] $k) {return $i}
        static [double] Foo([double] $i, [double] $j, [double] $k, [double] $l) {return $i}
        static [IntPtr] Foo([IntPtr] $i) {return $i}
        static [IntPtr] Foo([IntPtr] $i, [IntPtr] $j) {return $i}
        static [IntPtr] Foo([IntPtr] $i, [IntPtr] $j, [IntPtr] $k) {return $i}
        static [IntPtr] Foo([IntPtr] $i, [IntPtr] $j, [IntPtr] $k, [IntPtr] $l) {return $i}
        static [timespan] Foo([timespan] $i) {return $i}
        static [timespan] Foo([timespan] $i, [timespan] $j) {return $i}
        static [timespan] Foo([timespan] $i, [timespan] $j, [timespan] $k) {return $i}
        static [timespan] Foo([timespan] $i, [timespan] $j, [timespan] $k, [timespan] $l) {return $i}

    }


    It 'converts static method as Func does not throw' {
        {[Func[int, int]] [M]::Thrice} | Should Not Throw
    }

    It 'converts static method as Func is non null' {
        ([Func[int, int]] [M]::Thrice) | Should Not BeNullOrEmpty
    }


    It 'calls static method as Func' {
        $f = [Func[int, int]] [M]::Thrice
        [M]::Apply(1, $f) | Should be 3
    }

    It 'calls static method as Func' {
        $f = [Func[int, int16, int]] [M]::Add
        [M]::Apply(3, 4, $f) | Should be 7
    }

    It 'calls static method as Func no cast' {
        [M]::Apply(3, 4, [M]::Add) | Should be 7
    }

    It 'converts instance psmethodinfo to Func' {
        $m = [M]::new()
        {[Func[int, int]] $m.Twice} | Should Not Throw

        $f = [Func[int, int16, int]] [M]::Add
        $f.Invoke(2, 6) | Should be 8
    }
    It "can call all overloads of M::Foo" {
        [Func[Int16, Int16]] $f1 = [M]::Foo
        $f1.Invoke(10) | Should BE 10
        [Func[Int16, Int16, Int16]] $f2 = [M]::Foo
        $f2.Invoke(10, 1) | Should BE 10
        [Func[Int16, Int16, Int16, Int16]] $f3 = [M]::Foo
        $f3.Invoke(10, 1, 2) | Should BE 10
        [Func[Int16, Int16, Int16, Int16, Int16]] $f4 = [M]::Foo
        $f4.Invoke(10, 1, 2, 3) | Should BE 10
        [Func[Int32, Int32]] $f5 = [M]::Foo
        $f5.Invoke(10) | Should BE 10
        [Func[Int32, Int32, Int32]] $f6 = [M]::Foo
        $f6.Invoke(10, 1) | Should BE 10
        [Func[Int32, Int32, Int32, Int32]] $f7 = [M]::Foo
        $f7.Invoke(10, 1, 2) | Should BE 10
        [Func[Int32, Int32, Int32, Int32, Int32]] $f8 = [M]::Foo
        $f8.Invoke(10, 1, 2, 3) | Should BE 10
        [Func[UInt64, UInt64]] $f9 = [M]::Foo
        $f9.Invoke(10) | Should BE 10
        [Func[UInt64, UInt64, UInt64]] $f10 = [M]::Foo
        $f10.Invoke(10, 1) | Should BE 10
        [Func[UInt64, UInt64, UInt64, UInt64]] $f11 = [M]::Foo
        $f11.Invoke(10, 1, 2) | Should BE 10
        [Func[UInt64, UInt64, UInt64, UInt64, UInt64]] $f12 = [M]::Foo
        $f12.Invoke(10, 1, 2, 3) | Should BE 10
        [Func[Double, Double]] $f13 = [M]::Foo
        $f13.Invoke(10) | Should BE 10
        [Func[Double, Double, Double]] $f14 = [M]::Foo
        $f14.Invoke(10, 1) | Should BE 10
        [Func[Double, Double, Double, Double]] $f15 = [M]::Foo
        $f15.Invoke(10, 1, 2) | Should BE 10
        [Func[Double, Double, Double, Double, Double]] $f16 = [M]::Foo
        $f16.Invoke(10, 1, 2, 3) | Should BE 10
        [Func[IntPtr, IntPtr]] $f17 = [M]::Foo
        $f17.Invoke(10) | Should BE 10
        [Func[IntPtr, IntPtr, IntPtr]] $f18 = [M]::Foo
        $f18.Invoke(10, 1) | Should BE 10
        [Func[IntPtr, IntPtr, IntPtr, IntPtr]] $f19 = [M]::Foo
        $f19.Invoke(10, 1, 2) | Should BE 10
        [Func[IntPtr, IntPtr, IntPtr, IntPtr, IntPtr]] $f20 = [M]::Foo
        $f20.Invoke(10, 1, 2, 3) | Should BE 10
        [Func[TimeSpan, TimeSpan]] $f21 = [M]::Foo
        $timespan = [TimeSpan]::FromMinutes(62)
        $f21.Invoke($timeSpan) | Should BE $timeSpan
        [Func[TimeSpan, TimeSpan, TimeSpan]] $f22 = [M]::Foo
        $f22.Invoke($timeSpan, [Timespan]::Zero) | Should BE $timeSpan
        [Func[TimeSpan, TimeSpan, TimeSpan, TimeSpan]] $f23 = [M]::Foo
        $f23.Invoke($timeSpan, [Timespan]::Zero, [Timespan]::Zero) | Should BE $timeSpan
        [Func[TimeSpan, TimeSpan, TimeSpan, TimeSpan, TimeSpan]] $f24 = [M]::Foo
        $f24.Invoke($timeSpan, [Timespan]::Zero, [Timespan]::Zero, [Timespan]::Zero) | Should BE $timeSpan
    }

}
