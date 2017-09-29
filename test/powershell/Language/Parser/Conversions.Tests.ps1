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

        $powershell = Join-Path $PSHOME "powershell"
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
