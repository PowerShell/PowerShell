# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "TabCompletion" -Tags CI {
    BeforeAll {
        $separator = [System.IO.Path]::DirectorySeparatorChar
    }

    It 'Should complete Command' {
        $res = TabExpansion2 -inputScript 'Get-Com' -cursorColumn 'Get-Com'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Get-Command'
    }

    It 'Should complete abbreviated cmdlet' {
        $res = (TabExpansion2 -inputScript 'i-psdf' -cursorColumn 'pschr'.Length).CompletionMatches.CompletionText
        $res | Should -HaveCount 1
        $res | Should -BeExactly 'Import-PowerShellDataFile'
    }

    It 'Should complete abbreviated function' {
        function Test-AbbreviatedFunctionExpansion {}
        $res = (TabExpansion2 -inputScript 't-afe' -cursorColumn 't-afe'.Length).CompletionMatches.CompletionText
        $res.Count | Should -BeGreaterOrEqual 1
        $res | Should -BeExactly 'Test-AbbreviatedFunctionExpansion'
    }

    It 'Should complete module by shortname' {
        $res = TabExpansion2 -inputScript 'Get-Module -ListAvailable -Name Host'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Microsoft.PowerShell.Host'
    }

    It 'Should complete native exe' -Skip:(!$IsWindows) {
        $res = TabExpansion2 -inputScript 'notep' -cursorColumn 'notep'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'notepad.exe'
    }

    It 'Should not include duplicate command results' {
        $OldModulePath = $env:PSModulePath
        $tempDir = Join-Path -Path $TestDrive -ChildPath "TempPsModuleDir"
        $ModuleDirs = @(
            Join-Path $tempDir "TestModule1\1.0"
            Join-Path $tempDir "TestModule1\1.1"
            Join-Path $tempDir "TestModule2\1.0"
        )
        try
        {
            foreach ($Dir in $ModuleDirs)
            {
                $NewDir = New-Item -Path $Dir -ItemType Directory -Force
                $ModuleName = $NewDir.Parent.Name
                Set-Content -Value 'MyTestFunction{}' -LiteralPath "$($NewDir.FullName)\$ModuleName.psm1"
                New-ModuleManifest -Path "$($NewDir.FullName)\$ModuleName.psd1" -RootModule "$ModuleName.psm1" -FunctionsToExport "MyTestFunction" -ModuleVersion $NewDir.Name
            }

            $env:PSModulePath += [System.IO.Path]::PathSeparator + $tempDir            
            $Res = TabExpansion2 -inputScript MyTestFunction
            $Res.CompletionMatches.Count | Should -Be 2
            $SortedMatches = $Res.CompletionMatches.CompletionText | Sort-Object
            $SortedMatches[0] | Should -Be "TestModule1\MyTestFunction"
            $SortedMatches[1] | Should -Be "TestModule2\MyTestFunction"
        }
        finally
        {
            $env:PSModulePath = $OldModulePath
            Remove-Item -LiteralPath $ModuleDirs -Recurse -Force
        }
    }

    It 'Should not include duplicate module results' {
        $OldModulePath = $env:PSModulePath
        $tempDir = Join-Path -Path $TestDrive -ChildPath "TempPsModuleDir"
        try
        {
            $ModuleDirs = @(
                Join-Path $tempDir "TestModule1\1.0"
                Join-Path $tempDir "TestModule1\1.1"
            )
            foreach ($Dir in $ModuleDirs)
            {
                $NewDir = New-Item -Path $Dir -ItemType Directory -Force
                $ModuleName = $NewDir.Parent.Name
                Set-Content -Value 'MyTestFunction{}' -LiteralPath "$($NewDir.FullName)\$ModuleName.psm1"
                New-ModuleManifest -Path "$($NewDir.FullName)\$ModuleName.psd1" -RootModule "$ModuleName.psm1" -FunctionsToExport "MyTestFunction" -ModuleVersion $NewDir.Name
            }

            $env:PSModulePath += [System.IO.Path]::PathSeparator + $tempDir            
            $Res = TabExpansion2 -inputScript 'Import-Module -Name TestModule'
            $Res.CompletionMatches.Count | Should -Be 1
            $Res.CompletionMatches[0].CompletionText | Should -Be TestModule1
        }
        finally
        {
            $env:PSModulePath = $OldModulePath
            Remove-Item -LiteralPath $ModuleDirs -Recurse -Force
        }
    }

    It 'Should complete dotnet method' {
        $res = TabExpansion2 -inputScript '(1).ToSt' -cursorColumn '(1).ToSt'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'ToString('
    }

    It 'Should complete dotnet method with null conditional operator' {
        $res = TabExpansion2 -inputScript '(1)?.ToSt' -cursorColumn '(1)?.ToSt'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'ToString('
    }

    It 'Should complete dotnet method with null conditional operator without first letter' {
        $res = TabExpansion2 -inputScript '(1)?.' -cursorColumn '(1)?.'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'CompareTo('
    }

    It 'should complete generic type parameters for static methods' {
        $script = '[array]::Empty[pscu'

        $results = TabExpansion2 -inputScript $script -cursorColumn $script.Length
        $results.CompletionMatches.CompletionText | Should -Contain 'pscustomobject'
    }

    It 'should complete generic type parameters for instance methods' {
        $script = '
            $dict = [System.Collections.Concurrent.ConcurrentDictionary[string, int]]::new()
            $dict.AddOrUpdate[pscu'

        $results = TabExpansion2 -inputScript $script -cursorColumn $script.Length
        $results.CompletionMatches.CompletionText | Should -Contain 'pscustomobject'
    }

    It 'Should complete Magic foreach' {
        $res = TabExpansion2 -inputScript '(1..10).Fo' -cursorColumn '(1..10).Fo'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'ForEach('
    }

    It "Should complete Magic where" {
        $res = TabExpansion2 -inputScript '(1..10).wh' -cursorColumn '(1..10).wh'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Where('
    }

    It 'Should complete types' {
        $res = TabExpansion2 -inputScript '[pscu' -cursorColumn '[pscu'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'pscustomobject'
    }

    It 'Should complete foreach variable' {
        $res = TabExpansion2 -inputScript 'foreach ($CurrentItem in 1..10){$CurrentIt'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$CurrentItem'
    }

    It 'Should complete variables set with an attribute' {
        $res = TabExpansion2 -inputScript '[ValidateNotNull()]$Var1 = 1; $Var'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$Var1'
    }

    It 'Should use the first type constraint in a variable assignment in the tooltip' {
        $res = TabExpansion2 -inputScript '[int] [string] $Var1 = 1; $Var'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$Var1'
        $res.CompletionMatches[0].ToolTip | Should -BeExactly '[int]$Var1'
    }

    It 'Should not complete parameter name' {
        $res = TabExpansion2 -inputScript 'param($P'
        $res.CompletionMatches.Count | Should -Be 0
    }

    It 'Should complete variable in default value of a parameter' {
        $res = TabExpansion2 -inputScript 'param($PS = $P'
        $res.CompletionMatches.Count | Should -BeGreaterThan 0
    }
    
    It 'Should complete variable with description and value <Value>' -TestCases @(
        @{ Value = 1; Expected = '[int]$VariableWithDescription - Variable description' }
        @{ Value = 'string'; Expected = '[string]$VariableWithDescription - Variable description' }
        @{ Value = $null; Expected = 'VariableWithDescription - Variable description' }
    ) {
        param ($Value, $Expected)
        
        New-Variable -Name VariableWithDescription -Value $Value -Description 'Variable description' -Force
        $res = TabExpansion2 -inputScript '$VariableWithDescription'
        $res.CompletionMatches.Count | Should -Be 1
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$VariableWithDescription'
        $res.CompletionMatches[0].ToolTip | Should -BeExactly $Expected
    }
    
    It 'Should complete environment variable' {
        try {
            $env:PWSH_TEST_1 = 'value 1'
            $env:PWSH_TEST_2 = 'value 2'

            $res = TabExpansion2 -inputScript '$env:PWSH_TEST_'
            $res.CompletionMatches.Count | Should -Be 2
            $res.CompletionMatches[0].CompletionText | Should -BeExactly '$env:PWSH_TEST_1'
            $res.CompletionMatches[0].ListItemText | Should -BeExactly 'PWSH_TEST_1'
            $res.CompletionMatches[0].ToolTip | Should -BeExactly 'PWSH_TEST_1'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly '$env:PWSH_TEST_2'
            $res.CompletionMatches[1].ListItemText | Should -BeExactly 'PWSH_TEST_2'
            $res.CompletionMatches[1].ToolTip | Should -BeExactly 'PWSH_TEST_2'
        }
        finally {
            $env:PWSH_TEST_1 = $null
            $env:PWSH_TEST_2 = $null
        }
    }

    It 'Should complete function variable' {
        try {
            Function Test-PwshTest1 {}
            Function Test-PwshTest2 {}

            $res = TabExpansion2 -inputScript '${function:Test-PwshTest'
            $res.CompletionMatches.Count | Should -Be 2
            $res.CompletionMatches[0].CompletionText | Should -BeExactly '${function:Test-PwshTest1}'
            $res.CompletionMatches[0].ListItemText | Should -BeExactly 'Test-PwshTest1'
            $res.CompletionMatches[0].ToolTip | Should -BeExactly 'Test-PwshTest1'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly '${function:Test-PwshTest2}'
            $res.CompletionMatches[1].ListItemText | Should -BeExactly 'Test-PwshTest2'
            $res.CompletionMatches[1].ToolTip | Should -BeExactly 'Test-PwshTest2'
        }
        finally {
            Remove-Item function:Test-PwshTest1 -ErrorAction SilentlyContinue
            Remove-Item function:Test-PwshTest1 -ErrorAction SilentlyContinue
        }
    }

    It 'Should complete scoped variable with description and value <Value>' -TestCases @(
        @{ Value = 1; Expected = '[int]$VariableWithDescription - Variable description' }
        @{ Value = 'string'; Expected = '[string]$VariableWithDescription - Variable description' }
        @{ Value = $null; Expected = 'VariableWithDescription - Variable description' }
    ) {
        param ($Value, $Expected)
        
        New-Variable -Name VariableWithDescription -Value $Value -Description 'Variable description' -Force
        $res = TabExpansion2 -inputScript '$local:VariableWithDescription'
        $res.CompletionMatches.Count | Should -Be 1
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$local:VariableWithDescription'
        $res.CompletionMatches[0].ToolTip | Should -BeExactly $Expected
    }

    It 'Should not complete property name in class definition' {
        $res = TabExpansion2 -inputScript 'class X {$P'
        $res.CompletionMatches.Count | Should -Be 0
    }

    foreach ($Operator in [System.Management.Automation.CompletionCompleters]::CompleteOperator(""))
    {
        It "Should complete $($Operator.CompletionText)" {
            $res = TabExpansion2 -inputScript "'' $($Operator.CompletionText)" -cursorColumn ($Operator.CompletionText.Length + 3)
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $Operator.CompletionText
        }
    }

    context CustomProviderTests {
        BeforeAll {
            $testModulePath = Join-Path $TestDrive "ReproModule"
            New-Item -Path $testModulePath -ItemType Directory > $null

            New-ModuleManifest -Path "$testModulePath/ReproModule.psd1" -RootModule 'testmodule.dll'

            $testBinaryModulePath = Join-Path $testModulePath "testmodule.dll"
            $binaryModule = @'
using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace BugRepro
{
    public class IntItemInfo
    {
        public string Name;
        public IntItemInfo(string name) => Name = name;
    }

    [CmdletProvider("Int", ProviderCapabilities.None)]
    public class IntProvider : NavigationCmdletProvider
    {
        public static string[] ToChunks(string path) => path.Split("/", StringSplitOptions.RemoveEmptyEntries);

        protected string _ChildName(string path)
        {
            var name = ToChunks(path).LastOrDefault();
            return name ?? string.Empty;
        }

        protected string Normalize(string path) => string.Join("/", ToChunks(path));

        protected override string GetChildName(string path)
        {
            var name = _ChildName(path);
            // if (!IsItemContainer(path)) { return string.Empty; }
            return name;
        }

        protected override bool IsValidPath(string path) => int.TryParse(GetChildName(path), out int _);

        protected override bool IsItemContainer(string path)
        {
            var name = _ChildName(path);
            if (!int.TryParse(name, out int value))
            {
                return false;
            }
            if (ToChunks(path).Count() > 3)
            {
                return false;
            }
            return value % 2 == 0;
        }

        protected override bool ItemExists(string path)
        {
            foreach (var chunk in ToChunks(path))
            {
                if (!int.TryParse(chunk, out int value))
                {
                    return false;
                }
                if (value < 0 || value > 9)
                {
                    return false;
                }
            }
            return true;
        }

        protected override void GetItem(string path)
        {
            var name = GetChildName(path);
            if (!int.TryParse(name, out int _))
            {
                return;
            }
            WriteItemObject(new IntItemInfo(name), path, IsItemContainer(path));
        }
        protected override bool HasChildItems(string path) => IsItemContainer(path);

        protected override void GetChildItems(string path, bool recurse)
        {
            if (!IsItemContainer(path)) { GetItem(path); return; }

            for (var i = 0; i <= 9; i++)
            {
                var _path = $"{Normalize(path)}/{i}";
                if (recurse)
                {
                    GetChildItems(_path, recurse);
                }
                else
                {
                    GetItem(_path);
                }
            }
        }
    }
}
'@
            Add-Type -OutputAssembly $testBinaryModulePath -TypeDefinition $binaryModule

            $pwsh = "$PSHOME\pwsh"
        }

        It "Should not complete invalid items when a provider path returns itself instead of its children" {
            $result = & $pwsh -NoProfile -Command "Import-Module -Name $testModulePath; (TabExpansion2 'Get-ChildItem Int::/2/3/').CompletionMatches.Count"
            $result | Should -BeExactly "0"
        }
    }

    It 'should complete index expression for <Intent>' -TestCases @(
        @{
            Intent = 'Hashtable with no user input'
            Expected = "'PSVersion'"
            TestString = '$PSVersionTable[^'
        }
        @{
            Intent = 'Hashtable with partial input'
            Expected = "'PSVersion'"
            TestString = '$PSVersionTable[ PSvers^'
        }
        @{
            Intent = 'Hashtable with partial quoted input'
            Expected = "'PSVersion'"
            TestString = '$PSVersionTable["PSvers^'
        }
        @{
            Intent = 'Hashtable from Ast'
            Expected = "'Hello'"
            TestString = '$Table = @{Hello = "World"};$Table[^'
        }
        @{
            Intent = 'Hashtable with cursor on new line'
            Expected = "'Hello'"
            TestString = @'
$Table = @{Hello = "World"}
$Table[
^
'@
        }
    ) -Test {
        param($Expected, $TestString)
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $res.CompletionMatches[0].CompletionText | Should -BeExactly $Expected
    }

    it 'should add quotes when completing hashtable key from Ast with member syntax' -Test {
        $res = TabExpansion2 -inputScript '$Table = @{"Hello World" = "World"};$Table.'
        $res.CompletionMatches.CompletionText | Where-Object {$_ -eq "'Hello World'"} | Should -BeExactly "'Hello World'"
    }

    It '<Intent>' -TestCases @(
        @{
            Intent = 'Complete member with space between dot and cursor'
            Expected = 'value__'
            TestString = '[System.Management.Automation.ActionPreference]::Break.  ^'
        }
        @{
            Intent = 'Complete member when cursor is in-between existing members and spaces'
            Expected = 'value__'
            TestString = '[System.Management.Automation.ActionPreference]::Break. ^ ToString()'
        }
        @{
            Intent = 'Complete static member with space between colons and cursor'
            Expected = 'Break'
            TestString = '[System.Management.Automation.ActionPreference]::  ^'
        }
        @{
            Intent = 'Complete static member with new line between colons and cursor'
            Expected = 'Break'
            TestString = @'
[System.Management.Automation.ActionPreference]::
^
'@
        }
        @{
            Intent = 'Complete static member with partial input and incomplete input at end of line'
            Expected = 'Break'
            TestString = '[System.Management.Automation.ActionPreference]::  Brea^.   value__.'
        }
        @{
            Intent = 'Complete static member with partial input and valid input at end of line'
            Expected = 'Break'
            TestString = '[System.Management.Automation.ActionPreference]::  Brea^.   value__'
        }
        @{
            Intent = 'Complete member with new line between colons and cursor'
            Expected = 'value__'
            TestString = '[System.Management.Automation.ActionPreference]::Break. ^ ToString()'
        }
        @{
            Intent = 'Complete type with incomplete expression input at end of line'
            Expected = 'System.Management.Automation.ActionPreference'
            TestString = '[System.Management.Automation.ActionPreference^]::'
        }
        @{
            Intent = 'Complete member inside switch expression'
            Expected = 'Length'
            TestString = @'
switch ($x)
{
    'RandomString'.^
    {}
}
'@
        }
        @{
            Intent = 'Complete member in commandast'
            Expected = 'Length'
            TestString = 'ls "".^'
        }
        ){
            param($Expected, $TestString)
            $CursorIndex = $TestString.IndexOf('^')
            $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $Expected
        }

    It 'Should Complete and replace existing member with space in front of cursor and cursor in front of word' {
        $TestString = '[System.Management.Automation.ActionPreference]:: ^Break'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $res.ReplacementIndex | Should -BeExactly $CursorIndex
        $res.ReplacementLength | Should -Be 5
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Break'
    }

    It 'Complete and replace existing member with colons in front of cursor and cursor in front of word' {
        $TestString = '[System.Management.Automation.ActionPreference]::^Break'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $res.ReplacementIndex | Should -BeExactly $CursorIndex
        $res.ReplacementLength | Should -Be 5
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Break'
    }

    It 'Should complete namespaces' {
        $res = TabExpansion2 -inputScript 'using namespace Sys' -cursorColumn 'using namespace Sys'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'System'
    }

    It 'Should complete format-table hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Format-Table @{ ' -cursorColumn 'Get-ChildItem | Format-Table @{ '.Length
        $res.CompletionMatches | Should -HaveCount 5
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should -BeExactly 'Alignment Expression FormatString Label Width'
    }

    It 'Should complete format-* hashtable on GroupBy: <cmd>' -TestCases (
        @{cmd = 'Format-Table'},
        @{cmd = 'Format-List'},
        @{cmd = 'Format-Wide'},
        @{cmd = 'Format-Custom'}
    ) {
        param($cmd)
        $res = TabExpansion2 -inputScript "Get-ChildItem | $cmd -GroupBy @{ " -cursorColumn "Get-ChildItem | $cmd -GroupBy @{ ".Length
        $res.CompletionMatches | Should -HaveCount 3
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should -BeExactly 'Expression FormatString Label'
    }

    It 'Should complete format-list hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Format-List @{ ' -cursorColumn 'Get-ChildItem | Format-List @{ '.Length
        $res.CompletionMatches | Should -HaveCount 3
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should -BeExactly 'Expression FormatString Label'
    }

    It 'Should complete format-wide hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Format-Wide @{ ' -cursorColumn 'Get-ChildItem | Format-Wide @{ '.Length
        $res.CompletionMatches | Should -HaveCount 2
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should -BeExactly 'Expression FormatString'
    }

    It 'Should complete format-custom hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Format-Custom @{ ' -cursorColumn 'Get-ChildItem | Format-Custom @{ '.Length
        $res.CompletionMatches | Should -HaveCount 2
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should -BeExactly 'Depth Expression'
    }

    It 'Should complete Select-Object hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Select-Object @{ ' -cursorColumn 'Get-ChildItem | Select-Object @{ '.Length
        $res.CompletionMatches | Should -HaveCount 2
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should -BeExactly 'Expression Name'
    }

    It 'Should complete Sort-Object hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Sort-Object @{ ' -cursorColumn 'Get-ChildItem | Sort-Object @{ '.Length
        $res.CompletionMatches | Should -HaveCount 3
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should -BeExactly 'Ascending Descending Expression'
    }

    It 'Should complete variable assigned in other scriptblock' {
        $res = TabExpansion2 -inputScript 'ForEach-Object -Begin {$Test1 = "Hello"} -Process {$Test'
        $res.CompletionMatches[0].CompletionText | Should -Be '$Test1'
    }

    It 'Should complete variable assigned in an array of scriptblocks' {
        $res = TabExpansion2 -inputScript 'ForEach-Object -Process @({"Block1"},{$Test1="Hello"});$Test'
        $res.CompletionMatches[0].CompletionText | Should -Be '$Test1'
    }

    It 'Should not complete variable assigned in an ampersand executed scriptblock' {
        $res = TabExpansion2 -inputScript '& {$AmpeersandVarCompletionTest = "Hello"};$AmpeersandVarCompletionTes'
        $res.CompletionMatches.Count | Should -Be 0
    }

    It 'Should complete variable assigned in command redirection to variable' {
        $res = TabExpansion2 -inputScript 'New-Guid 1>variable:Redir1 2>variable:Redir2 3>variable:Redir3 4>variable:Redir4 5>variable:Redir5 6>variable:Redir6; $Redir'
        $res.CompletionMatches[0].CompletionText | Should -Be '$Redir1'
        $res.CompletionMatches[1].CompletionText | Should -Be '$Redir2'
        $res.CompletionMatches[1].ToolTip | Should -Be '[ErrorRecord]$Redir2'
        $res.CompletionMatches[2].CompletionText | Should -Be '$Redir3'
        $res.CompletionMatches[2].ToolTip | Should -Be '[WarningRecord]$Redir3'
        $res.CompletionMatches[3].CompletionText | Should -Be '$Redir4'
        $res.CompletionMatches[3].ToolTip | Should -Be '[VerboseRecord]$Redir4'
        $res.CompletionMatches[4].CompletionText | Should -Be '$Redir5'
        $res.CompletionMatches[4].ToolTip | Should -Be '[DebugRecord]$Redir5'
        $res.CompletionMatches[5].CompletionText | Should -Be '$Redir6'
        $res.CompletionMatches[5].ToolTip | Should -Be '[InformationRecord]$Redir6'
    }

    context TypeConstructionWithHashtable {
        BeforeAll {
            class RandomTestType {
                $A
                $B
                $C
            }
            function RandomTestTypeClassTestCompletion([RandomTestType]$Param1){}
            Class LevelOneClass {
                [LevelTwoClass] $Property1
            }
            class LevelTwoClass {
                [string] $Property2
            }
            function LevelOneClassTestCompletion([LevelOneClass[]]$Param1){}
            Add-Type -TypeDefinition 'public interface IRandomInterfaceTest{string DemoProperty { get; set; }}'
            function functionWithInterfaceParam ([IRandomInterfaceTest]$Param1){}
        }
        It 'Should complete New-Object hashtable' {
            $res = TabExpansion2 -inputScript 'New-Object -TypeName RandomTestType -Property @{ '
            $res.CompletionMatches | Should -HaveCount 3
            $res.CompletionMatches.CompletionText -join ' ' | Should -BeExactly 'A B C'
        }

        It 'Complete hashtable key without duplicate keys' {
            $TestString = '[RandomTestType]@{A="";^}'
            $CursorIndex = $TestString.IndexOf('^')
            $res = TabExpansion2 -inputScript $TestString.Remove($CursorIndex, 1) -cursorColumn $CursorIndex
            $res.CompletionMatches | Should -HaveCount 2
            $res.CompletionMatches.CompletionText -join ' ' | Should -BeExactly 'B C'
        }

        It 'Complete hashtable key on empty line after key/value pair' {
            $TestString = @'
[RandomTestType]@{
    B=""
    ^
}
'@
            $CursorIndex = $TestString.IndexOf('^')
            $res = TabExpansion2 -inputScript $TestString.Remove($CursorIndex, 1) -cursorColumn $CursorIndex
            $res.CompletionMatches | Should -HaveCount 2
            $res.CompletionMatches.CompletionText -join ' ' | Should -BeExactly 'A C'
        }

        It 'Should complete class properties for typed variable declaration with hashtable' {
            $res = TabExpansion2 -inputScript '[RandomTestType]$TestVar = @{'
            $res.CompletionMatches | Should -HaveCount 3
            $res.CompletionMatches.CompletionText -join ' ' | Should -BeExactly 'A B C'
        }

        It 'Should complete class properties for typed command parameter with hashtable input' {
            $res = TabExpansion2 -inputScript 'RandomTestTypeClassTestCompletion -Param1 @{'
            $res.CompletionMatches | Should -HaveCount 3
            $res.CompletionMatches.CompletionText -join ' ' | Should -BeExactly 'A B C'
        }

        It 'Should complete class properties for nested hashtable' {
            $res = TabExpansion2 -inputScript '[LevelOneClass]@{Property1=@{'
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Property2'
        }

        It 'Should complete class properties for underlying type in array parameter' {
            $res = TabExpansion2 -inputScript 'LevelOneClassTestCompletion @{'
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Property1'
        }

        It 'Should complete class properties for new class assignment to property' {
            $res = TabExpansion2 -inputScript '$Var=[LevelOneClass]::new();$Var.Property1=@{'
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Property2'
        }

        It 'Should not complete class properties from class with constructor that takes arguments' {
            $res = TabExpansion2 -inputScript 'class ClassWithCustomConstructor {ClassWithCustomConstructor ($Param){}$A};[ClassWithCustomConstructor]@{'
            $res.CompletionMatches[0].CompletionText | Should -BeNullOrEmpty
        }

        It 'Should complete class properties for function with an interface type' {
            $res = TabExpansion2 -inputScript 'functionWithInterfaceParam -Param1 @{'
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'DemoProperty'
        }
    }

    It 'Complete hashtable keys for Get-WinEvent FilterHashtable' -Skip:(!$IsWindows) {
        $TestString = 'Get-WinEvent -FilterHashtable @{^'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -inputScript $TestString.Remove($CursorIndex, 1) -cursorColumn $CursorIndex
        $res.CompletionMatches | Should -HaveCount 11
        $res.CompletionMatches.CompletionText -join ' ' | Should -BeExactly 'LogName ProviderName Path Keywords ID Level StartTime EndTime UserID Data SuppressHashFilter'
    }

    It 'Complete hashtable keys for Get-WinEvent SuppressHashFilter' -Skip:(!$IsWindows) {
        $TestString = 'Get-WinEvent -FilterHashtable @{SuppressHashFilter=@{'
        $res = TabExpansion2 -inputScript $TestString
        $res.CompletionMatches | Should -HaveCount 10
        $res.CompletionMatches.CompletionText -join ' ' | Should -BeExactly 'LogName ProviderName Path Keywords ID Level StartTime EndTime UserID Data'
    }

    It 'Complete hashtable keys for hashtable in array of arguments' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Format-Table -Property Attributes,@{'
        $res.CompletionMatches.CompletionText -join ' ' | Should -BeExactly 'Expression FormatString Label Width Alignment'
    }

    It 'Complete hashtable keys for a hashtable used for splatting' {
        $TestString = '$GetChildItemParams=@{^};Get-ChildItem @GetChildItemParams -Force -Recurse'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -inputScript $TestString.Remove($CursorIndex, 1) -cursorColumn $CursorIndex
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Path'
    }

    It 'Should complete "Get-Process -Id " with Id and name in tooltip' {
        Set-StrictMode -Version 3.0
        $cmd = 'Get-Process -Id '
        [System.Management.Automation.CommandCompletion]$res = TabExpansion2 -inputScript $cmd  -cursorColumn $cmd.Length
        $res.CompletionMatches[0].CompletionText -match '^\d+$' | Should -BeTrue
        $res.CompletionMatches[0].ListItemText -match '^\d+ -' | Should -BeTrue
        $res.CompletionMatches[0].ToolTip -match '^\d+ -' | Should -BeTrue
    }

    It 'Should complete "Get-Process" with process names' {
        $cmd = "Get-Process "
        $res = TabExpansion2 -inputScript $cmd  -cursorColumn $cmd.Length
        # Can't compare to number of processes since macOS has a large number of processes
        # that have empty Name which should be skipped
        $res.CompletionMatches.Count | Should -BeGreaterThan 0
    }

    It 'Should complete keyword with partial input' {
        $res = TabExpansion2 -inputScript 'using nam' -cursorColumn 'using nam'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'namespace'
    }

    It 'Should complete keyword with no input' {
        $res = TabExpansion2 -inputScript 'using ' -cursorColumn 'using '.Length
        $res.CompletionMatches.CompletionText | Should -BeExactly 'assembly','module','namespace','type'
    }

    It 'Should complete keyword with no input after line continuation' {
        $InputScript = @'
using `

'@
        $res = TabExpansion2 -inputScript $InputScript -cursorColumn $InputScript.Length
        $res.CompletionMatches.CompletionText | Should -BeExactly 'assembly','module','namespace','type'
    }

    It 'Should first suggest -Full and then -Functionality when using Get-Help -Fu<tab>' -Skip {
        $res = TabExpansion2 -inputScript 'Get-Help -Fu' -cursorColumn 'Get-Help -Fu'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '-Full'
        $res.CompletionMatches[1].CompletionText | Should -BeExactly '-Functionality'
    }

    It 'Should first suggest -Full and then -Functionality when using help -Fu<tab>' -Skip {
        $res = TabExpansion2 -inputScript 'help -Fu' -cursorColumn 'help -Fu'.Length
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '-Full'
        $res.CompletionMatches[1].CompletionText | Should -BeExactly '-Functionality'
    }

    It 'Should not remove braces when completing variable with braces' {
        $Text = '"Hello${psversiont}World"'
        $res = TabExpansion2 -inputScript $Text -cursorColumn $Text.IndexOf('p')
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '${PSVersionTable}'
    }

    It 'Should work for property assignment of enum type:' {
        $res = TabExpansion2 -inputScript '$psstyle.Progress.View="Clas'
        $res.CompletionMatches[0].CompletionText | Should -Be '"Classic"'
    }

    It 'Should work for variable assignment of enum type with type inference' {
        $res = TabExpansion2 -inputScript '[System.Management.Automation.ProgressView]$MyUnassignedVar = $psstyle.Progress.View; $MyUnassignedVar = "Class'
        $res.CompletionMatches[0].CompletionText | Should -Be '"Classic"'
    }

    It 'Should work for property assignment of enum type with type inference with PowerShell class' {
        $res = TabExpansion2 -inputScript 'enum Animals{Cat= 0;Dog= 1};class AnimalTestClass{[Animals] $Prop1};$Test1 = [AnimalTestClass]::new();$Test1.Prop1 = "C'
        $res.CompletionMatches[0].CompletionText | Should -Be '"Cat"'
    }

    It 'Should work for variable assignment with type inference of PowerShell Enum' {
        $res = TabExpansion2 -inputScript 'enum Animals{Cat= 0;Dog= 1}; [Animals]$TestVar1 = "D'
        $res.CompletionMatches[0].CompletionText | Should -Be '"Dog"'
    }

    It 'Should work for variable assignment of enum type: <inputStr>' -TestCases @(
        @{ inputStr = '$ErrorActionPreference = '; filter = ''; doubleQuotes = $false }
        @{ inputStr = '$ErrorActionPreference='; filter = ''; doubleQuotes = $false }
        @{ inputStr = '$ErrorActionPreference="'; filter = ''; doubleQuotes = $true }
        @{ inputStr = '$ErrorActionPreference = ''s'; filter = '| Where-Object { $_ -like "''s*" }'; doubleQuotes = $false }
        @{ inputStr = '$ErrorActionPreference = "siL'; filter = '| Where-Object { $_ -like ''"sil*'' }'; doubleQuotes = $true }
        @{ inputStr = '[System.Management.Automation.ActionPreference]$e='; filter = ''; doubleQuotes = $false }
        @{ inputStr = '[System.Management.Automation.ActionPreference]$e = '; filter = ''; doubleQuotes = $false }
        @{ inputStr = '[System.Management.Automation.ActionPreference]$e = "'; filter = ''; doubleQuotes = $true }
        @{ inputStr = '[System.Management.Automation.ActionPreference]$e = "s'; filter = '| Where-Object { $_ -like """s*" }'; doubleQuotes = $true }
        @{ inputStr = '[System.Management.Automation.ActionPreference]$e = "x'; filter = '| Where-Object { $_ -like """x*" }'; doubleQuotes = $true }
    ){
        param($inputStr, $filter, $doubleQuotes)

        $quote = ''''
        if ($doubleQuotes) {
            $quote = '"'
        }

        $sb = [scriptblock]::Create(@"
            [cmdletbinding()] param([Parameter(ValueFromPipeline=`$true)]`$obj) process { `$obj $filter }
"@)

        $expectedValues = [enum]::GetValues("System.Management.Automation.ActionPreference") | ForEach-Object { $quote + $_.ToString() + $quote } | & $sb | Sort-Object
        if ($expectedValues.Count -gt 0) {
            $expected = [string]::Join(",",$expectedValues)
        }
        else {
            $expected = ''
        }

        $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
        if ($res.CompletionMatches.Count -gt 0) {
            $actual = [string]::Join(",",$res.CompletionMatches.completiontext)
        }
        else {
            $actual = ''
        }

        $actual | Should -BeExactly $expected
    }

    It 'Should work for variable assignment of custom enum: <inputStr>' -TestCases @(
        @{ inputStr = '[Animal]$c="g'; expected = '"Giraffe"','"Goose"' }
        @{ inputStr = '[Animal]$c='; expected = "'Duck'","'Giraffe'","'Goose'","'Horse'" }
        @{ inputStr = '$script:test = "g'; expected = '"Giraffe"','"Goose"' }
        @{ inputStr = '$script:test='; expected = "'Duck'","'Giraffe'","'Goose'","'Horse'" }
        @{ inputStr = '$script:test = "x'; expected = @() }
    ){
        param($inputStr, $expected)

        enum Animal { Duck; Goose; Horse; Giraffe }
        [Animal]$script:test = 'Duck'

        $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
        if ($res.CompletionMatches.Count -gt 0) {
            $actual = [string]::Join(",",$res.CompletionMatches.completiontext)
        }
        else {
            $actual = ''
        }

        $actual | Should -BeExactly ([string]::Join(",",$expected))
    }

    It 'Should work for assignment of variable with validateset of strings: <inputStr>' -TestCases @(
        @{ inputStr = '$test='; expected = "'a'","'aa'","'aab'","'b'"; doubleQuotes = $false }
        @{ inputStr = '$test="a'; expected = "'a'","'aa'","'aab'"; doubleQuotes = $true }
        @{ inputStr = '$test = "aa'; expected = "'aa'","'aab'"; doubleQuotes = $true }
        @{ inputStr = '$test=''aab'; expected = "'aab'"; doubleQuotes = $false }
        @{ inputStr = '$test="c'; expected = ''; doubleQuotes = $true }
    ){
        param($inputStr, $expected, $doubleQuotes)

        [ValidateSet('a','aa','aab','b')][string]$test = 'b'

        $expected = [string]::Join(",",$expected)
        if ($doubleQuotes) {
            $expected = $expected.Replace("'", """")
        }

        $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
        if ($res.CompletionMatches.Count -gt 0) {
            $actual = [string]::Join(",",$res.CompletionMatches.completiontext)
        }
        else {
            $actual = ''
        }

        $actual | Should -BeExactly $expected
    }

    It 'Should work for assignment of variable with validateset of int: <inputStr>' -TestCases @(
        @{ inputStr = '$test='; expected = 2,3,11,112 }
        @{ inputStr = '$test = 1'; expected = 11,112 }
        @{ inputStr = '$test =11'; expected = 11,112 }
        @{ inputStr = '$test =4'; expected = @() }
    ){
        param($inputStr, $expected)

        [ValidateSet(2,3,11,112)][int]$test = 2

        $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
        if ($res.CompletionMatches.Count -gt 0) {
            $actual = [string]::Join(",",$res.CompletionMatches.completiontext)
        }
        else {
            $actual = ''
        }

        $actual | Should -BeExactly ([string]::Join(",",$expected))
    }

    It 'Should work for assignment of variable with validateset of strings: <inputStr>' -TestCases @(
        @{ inputStr = '[validateset("a","aa","aab","b")][string]$test='; expected = "'a'","'aa'","'aab'","'b'"; doubleQuotes = $false }
        @{ inputStr = '[validateset("a","aa","aab","b")][string]$test="a'; expected = "'a'","'aa'","'aab'"; doubleQuotes = $true }
        @{ inputStr = '[validateset("a","aa","aab","b")][string]$test = "aa'; expected = "'aa'","'aab'"; doubleQuotes = $true }
        @{ inputStr = '[validateset("a","aa","aab","b")][string]$test=''aab'; expected = "'aab'"; doubleQuotes = $false }
        @{ inputStr = '[validateset("a","aa","aab","b")][string]$test=''c'; expected = ''; doubleQuotes = $false }
    ){
        param($inputStr, $expected, $doubleQuotes)

        $expected = [string]::Join(",",$expected)
        if ($doubleQuotes) {
            $expected = $expected.Replace("'", """")
        }

        $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
        if ($res.CompletionMatches.Count -gt 0) {
            $actual = [string]::Join(",",$res.CompletionMatches.completiontext)
        }
        else {
            $actual = ''
        }

        $actual | Should -BeExactly $expected
    }

    It 'ForEach-Object member completion results should include methods' {
        $res = TabExpansion2 -inputScript '1..10 | ForEach-Object -MemberName '
        $res.CompletionMatches.CompletionText | Should -Contain "GetType"
    }

    It 'Should complete variable member inferred from command inside scriptblock' {
        $res = TabExpansion2 -inputScript '& {(New-Guid).'
        $res.CompletionMatches.Count | Should -BeGreaterThan 0
    }

    It 'Should not complete void instance members' {
        $res = TabExpansion2 -inputScript '([void]("")).'
        $res.CompletionMatches | Should -BeNullOrEmpty
    }

    It 'Should complete custom constructor from class using the AST' {
        $res = TabExpansion2 -inputScript 'class ConstructorTestClass{ConstructorTestClass ([string] $s){}};[ConstructorTestClass]::'
        $res.CompletionMatches | Should -HaveCount 3
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should -BeExactly 'Equals( new( ReferenceEquals('
    }

    It 'Should complete variables assigned inside do while loop' {
        $TestString =  'do{$Var1 = 1; $Var^ }while ($true)'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$Var1'
    }

    It 'Should complete variables assigned inside do until loop' {
        $TestString =  'do{$Var1 = 1; $Var^ }until ($null = Get-ChildItem)'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$Var1'
    }

    It 'Should show multiple constructors in the tooltip' {
        $res = TabExpansion2 -inputScript 'class ConstructorTestClass{ConstructorTestClass ([string] $s){}ConstructorTestClass ([int] $i){}ConstructorTestClass ([int] $i, [bool]$b){}};[ConstructorTestClass]::new'
        $res.CompletionMatches | Should -HaveCount 1
        $completionText = $res.CompletionMatches.ToolTip
        $completionText.replace("`r`n", [System.Environment]::NewLine).trim()

        $expected = @'
ConstructorTestClass(string s)
ConstructorTestClass(int i)
ConstructorTestClass(int i, bool b)
'@
        $expected.replace("`r`n", [System.Environment]::NewLine).trim()
        $completionText.replace("`r`n", [System.Environment]::NewLine).trim() | Should -BeExactly $expected
    }

    It 'Should complete parameter in param block' {
        $res = TabExpansion2 -inputScript 'Param($Param1=(Get-ChildItem -))' -cursorColumn 30
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '-Path'
    }

    It 'Should complete member in param block' {
        $res = TabExpansion2 -inputScript 'Param($Param1=($PSVersionTable.))' -cursorColumn 31
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Count'
    }

    It 'Should complete attribute argument in param block' {
        $res = TabExpansion2 -inputScript 'Param([Parameter()]$Param1)' -cursorColumn 17
        $names = [Parameter].GetProperties() | Where-Object CanWrite | ForEach-Object Name

        $diffs = Compare-Object -ReferenceObject $res.CompletionMatches.CompletionText -DifferenceObject $names
        $diffs | Should -BeNullOrEmpty
    }

    It 'Should complete attribute argument in incomplete param block' {
        $res = TabExpansion2 -inputScript 'param([ValidatePattern('
        $Expected = ([ValidatePattern].GetProperties() | Where-Object {$_.CanWrite}).Name -join ','
        $res.CompletionMatches.CompletionText -join ',' | Should -BeExactly $Expected
    }

    It 'Should complete attribute argument in incomplete param block on new line' {
        $TestString =  @'
param([ValidatePattern(
^)])
'@
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $Expected = ([ValidatePattern].GetProperties() | Where-Object {$_.CanWrite}).Name -join ','
        $res.CompletionMatches.CompletionText -join ',' | Should -BeExactly $Expected
    }

    It 'Should complete attribute argument with partially written name in incomplete param block' {
        $TestString =  'param([ValidatePattern(op^)]'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Options'
    }

    It 'Should complete attribute argument for incomplete standalone attribute' {
        $res = TabExpansion2 -inputScript '[ValidatePattern('
        $Expected = ([ValidatePattern].GetProperties() | Where-Object {$_.CanWrite}).Name -join ','
        $res.CompletionMatches.CompletionText -join ',' | Should -BeExactly $Expected
    }

    It 'Should complete argument for second parameter' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem -Path $HOME -ErrorAction '
        $res.CompletionMatches[0].CompletionText | Should -BeExactly Break
    }

    It 'Should complete argument with validateset attribute after comma' {
        $TestString = 'function Test-ValidateSet{Param([ValidateSet("Cat","Dog")]$Param1,$Param2)};Test-ValidateSet -Param1 Dog, -Param2'
        $res = TabExpansion2 -inputScript $TestString -cursorColumn ($TestString.LastIndexOf(',') + 1)
        $res.CompletionMatches[0].CompletionText | Should -BeExactly Cat
    }

    It 'Should complete cim ETS member added by shortname' -Skip:(!$IsWindows -or (Test-IsWinServer2012R2) -or (Test-IsWindows2016)) {
        $res = TabExpansion2 -inputScript '(Get-NetFirewallRule).Nam'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Name'
    }

    It 'Should complete variable assigned with Data statement' {
        $TestString = 'data MyDataVar {"Hello"};$MyDatav'
        $res = TabExpansion2 -inputScript $TestString
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$MyDataVar'
    }

    It 'Should complete global variable without scope' {
        $res = TabExpansion2 -inputScript '$Global:MyTestVar = "Hello";$MyTestV'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$MyTestVar'
    }

    It 'Should complete previously assigned variable in using: scope' {
        $res = TabExpansion2 -inputScript '$MyTestVar = "Hello";$Using:MyTestv'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$Using:MyTestVar'
    }

    it 'Should complete "Value" parameter value in "Where-Object" for Enum property with no input' {
        $res = TabExpansion2 -inputScript 'Get-Command | where-Object CommandType -eq '
        $res.CompletionMatches[0].CompletionText | Should -BeExactly Alias
    }

    it 'Should complete "Value" parameter value in "Where-Object" for Enum property with partial input' {
        $res = TabExpansion2 -inputScript 'Get-Command | where-Object CommandType -ne Ali'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly Alias
    }

    it 'Should complete the right hand side of a comparison operator when left is an Enum with no input' {
        $res = TabExpansion2 -inputScript 'Get-Command | Where-Object -FilterScript {$_.CommandType -like '
        $res.CompletionMatches[0].CompletionText | Should -BeExactly "'Alias'"
    }

    it 'Should complete the right hand side of a comparison operator when left is an Enum with partial input' {
        $TempVar = Get-Command
        $res = TabExpansion2 -inputScript '$tempVar[0].CommandType -notlike "Ali"'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly "'Alias'"
    }

    it 'Should complete the right hand side of a comparison operator when left is an Enum when cursor is on a newline' {
        $res = TabExpansion2 -inputScript "Get-Command | Where-Object -FilterScript {`$_.CommandType -like`n"
        $res.CompletionMatches[0].CompletionText | Should -BeExactly "'Alias'"
    }

    it 'Should complete provider dynamic parameters with quoted path' {
        $Script = if ($IsWindows)
        {
            'Get-ChildItem -Path "C:\" -Director'
        }
        else
        {
            'Get-ChildItem -Path "/" -Director'
        }
        $res = TabExpansion2 -inputScript $Script
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '-Directory'
    }

    it 'Should complete dynamic parameters while providing values to non-string parameters' {
        $res = TabExpansion2 -inputScript 'Get-Content -Path $HOME -Verbose:$false -'
        $res.CompletionMatches.CompletionText | Should -Contain '-Raw'
    }

    It 'Should enumerate types when completing member names for Select-Object' {
        $TestString = '"Hello","World" | select-object '
        $res = TabExpansion2 -inputScript $TestString
        $res | Should -HaveCount 1
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Length'
    }

    It 'Should complete psobject members for variable' {
        $TestVar = Get-Command Get-Command | Select-Object CommandType
        $res = TabExpansion2 -inputScript '$TestVar | ForEach-Object {$_.commandtype'
        $res | Should -HaveCount 1
        $res.CompletionMatches[0].CompletionText | Should -BeExactly 'CommandType'
    }

    It 'Should not complete variables that appear after the cursor' {
        $TestString = '$TestVar1 = 1; $TestVar^ ; $TestVar2 = 2'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $res | Should -HaveCount 1
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$TestVar1'
    }

    It 'Should not complete pipeline variables outside the pipeline' {
        $TestString = 'Get-ChildItem -PipelineVariable TestVar1;$TestVar^'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $res.CompletionMatches | Should -HaveCount 0
    }

    It 'Should complete pipeline variables inside the pipeline' {
        $TestString = 'Get-ChildItem -PipelineVariable TestVar1 | ForEach-Object -Process {$TestVar^}'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
        $res | Should -HaveCount 1
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$TestVar1'
    }

    It 'Should complete variable assigned in ParenExpression' {
        $res = TabExpansion2 -inputScript '($ParenVar) = 1; $ParenVa'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$ParenVar'
    }

    It 'Should complete variable assigned in ArrayLiteral' {
        $res = TabExpansion2 -inputScript '$DemoVar1, $DemoVar2 = 1..10; $DemoVar'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly '$DemoVar1'
        $res.CompletionMatches[1].CompletionText | Should -BeExactly '$DemoVar2'
    }

    It 'Should include parameter help message in tool tip - SingleMatch <SingleMatch>' -TestCases @(
        @{ SingleMatch = $true }
        @{ SingleMatch = $false }
    ) {
        param ($SingleMatch)

        Function Test-Function {
            param (
                [Parameter(HelpMessage = 'Some help message')]
                $ParamWithHelp,

                $ParamWithoutHelp
            )
        }

        $expected = '[Object] ParamWithHelp - Some help message'

        if ($SingleMatch) {
            $Script = 'Test-Function -ParamWithHelp'
            $res = (TabExpansion2 -inputScript $Script).CompletionMatches
        }
        else {
            $Script = 'Test-Function -'
            $res = (TabExpansion2 -inputScript $Script).CompletionMatches | Where-Object CompletionText -eq '-ParamWithHelp'
        }

        $res.Count | Should -Be 1
        $res.CompletionText | Should -BeExactly '-ParamWithHelp'
        $res.ToolTip | Should -BeExactly $expected
    }

    It 'Should include parameter help resource message in tool tip - SingleMatch <SingleMatch>' -TestCases @(
        @{ SingleMatch = $true }
        @{ SingleMatch = $false }
    ) {
        param ($SingleMatch)

        $expected = '`[string`] Activity - *'

        if ($SingleMatch) {
            $Script = 'Write-Progress -Activity'
            $res = (TabExpansion2 -inputScript $Script).CompletionMatches
        }
        else {
            $Script = 'Write-Progress -'
            $res = (TabExpansion2 -inputScript $Script).CompletionMatches | Where-Object CompletionText -eq '-Activity'
        }

        $res.Count | Should -Be 1
        $res.CompletionText | Should -BeExactly '-Activity'
        $res.ToolTip | Should -BeLikeExactly $expected
    }

    It 'Should skip empty parameter HelpMessage with multiple parameters - SingleMatch <SingleMatch>' -TestCases @(
        @{ SingleMatch = $true }
        @{ SingleMatch = $false }
    ) {
        param ($SingleMatch)

        Function Test-Function {
            [CmdletBinding(DefaultParameterSetName = 'SetWithoutHelp')]
            param (
                [Parameter(ParameterSetName = 'SetWithHelp', HelpMessage = 'Help Message')]
                [Parameter(ParameterSetName = 'SetWithoutHelp')]
                [string]
                $ParamWithHelp,
                
                [Parameter(ParameterSetName = 'SetWithHelp')]
                [switch]
                $ParamWithoutHelp
            )
        }

        $expected = '[string] ParamWithHelp - Help Message'

        if ($SingleMatch) {
            $Script = 'Test-Function -ParamWithHelp'
            $res = (TabExpansion2 -inputScript $Script).CompletionMatches
        }
        else {
            $Script = 'Test-Function -'
            $res = (TabExpansion2 -inputScript $Script).CompletionMatches | Where-Object CompletionText -eq '-ParamWithHelp'
        }

        $res.Count | Should -Be 1
        $res.CompletionText | Should -BeExactly '-ParamWithHelp'
        $res.ToolTip | Should -BeExactly $expected
    }

    It 'Should retrieve help message from dynamic parameter' {
        Function Test-Function {
            [CmdletBinding()]
            param ()
            dynamicparam {
                $attr = [System.Management.Automation.ParameterAttribute]@{
                    HelpMessage = "Howdy partner"
                }
                $attrCollection = [System.Collections.ObjectModel.Collection[System.Attribute]]::new()
                $attrCollection.Add($attr)

                $dynParam = [System.Management.Automation.RuntimeDefinedParameter]::new('DynamicParam', [string], $attrCollection)

                $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                $paramDictionary.Add('DynamicParam', $dynParam)
                $paramDictionary
            }

            end {}
        }

        $expected = '[string] DynamicParam - Howdy partner'
        $Script = 'Test-Function -'
        $res = (TabExpansion2 -inputScript $Script).CompletionMatches | Where-Object CompletionText -eq '-DynamicParam'
        $res.Count | Should -Be 1
        $res.CompletionText | Should -BeExactly '-DynamicParam'
        $res.ToolTip | Should -BeExactly $expected
    }

    It 'Should have type and name for parameter without help message' {
        Function Test-Function {
            param (
                [Parameter()]
                $WithParamAttribute,

                $WithoutParamAttribute
            )
        }

        $Script = 'Test-Function -'
        $res = (TabExpansion2 -inputScript $Script).CompletionMatches |
            Where-Object CompletionText -in '-WithParamAttribute', '-WithoutParamAttribute' |
            Sort-Object CompletionText
        $res.Count | Should -Be 2

        $res.CompletionText[0] | Should -BeExactly '-WithoutParamAttribute'
        $res.ToolTip[0] | Should -BeExactly '[Object] WithoutParamAttribute'

        $res.CompletionText[1] | Should -BeExactly '-WithParamAttribute'
        $res.ToolTip[1] | Should -BeExactly '[Object] WithParamAttribute'
    }

    It 'Should ignore errors when faling to get HelpMessage resource' {
        Function Test-Function {
            param (
                [Parameter(HelpMessageBaseName="invalid", HelpMessageResourceId="SomeId")]
                $InvalidHelpParam
            )
        }

        $expected = '[Object] InvalidHelpParam'
        $Script = 'Test-Function -InvalidHelpParam'
        $res = (TabExpansion2 -inputScript $Script).CompletionMatches
        $res.Count | Should -Be 1
        $res.CompletionText | Should -BeExactly '-InvalidHelpParam'
        $res.ToolTip | Should -BeExactly $expected
    }

    Context 'Start-Process -Verb parameter completion' {
        BeforeAll {
            function GetProcessInfoVerbs([string]$path, [switch]$singleQuote, [switch]$doubleQuote) {
                $verbs = (New-Object -TypeName System.Diagnostics.ProcessStartInfo -ArgumentList $path).Verbs

                if ($singleQuote) {
                    return ($verbs | ForEach-Object { "'$_'" })
                }
                elseif ($doubleQuote) {
                    return ($verbs | ForEach-Object { """$_""" })
                }

                return $verbs
            }

            $cmdPath = Join-Path -Path $TestDrive -ChildPath 'test.cmd'
            $cmdVerbs = GetProcessInfoVerbs -Path $cmdPath
            $cmdVerbsSingleQuote = GetProcessInfoVerbs -Path $cmdPath -SingleQuote
            $cmdVerbsDoubleQuote = GetProcessInfoVerbs -Path $cmdPath -DoubleQuote
            $exePath = Join-Path -Path $TestDrive -ChildPath 'test.exe'
            $exeVerbs = GetProcessInfoVerbs -Path $exePath
            $exeVerbsStartingWithRun = $exeVerbs | Where-Object { $_ -like 'run*' }
            $exeVerbsSingleQuote = GetProcessInfoVerbs -Path $exePath -SingleQuote
            $exeVerbsStartingWithRunSingleQuote = $exeVerbsSingleQuote | Where-Object { $_ -like "'run*" }
            $exeVerbsDoubleQuote = GetProcessInfoVerbs -Path $exePath -DoubleQuote
            $exeVerbsStartingWithRunDoubleQuote = $exeVerbsDoubleQuote | Where-Object { $_ -like """run*" }
            $powerShellExeWithNoExtension = 'powershell'
            $txtPath = Join-Path -Path $TestDrive -ChildPath 'test.txt'
            $txtVerbs = GetProcessInfoVerbs -Path $txtPath
            $wavPath = Join-Path -Path $TestDrive -ChildPath 'test.wav'
            $wavVerbs = GetProcessInfoVerbs -Path $wavPath
            $docxPath = Join-Path -Path $TestDrive -ChildPath 'test.docx'
            $docxVerbs = GetProcessInfoVerbs -Path $docxPath
            $fileWithNoExtensionPath = Join-Path -Path $TestDrive -ChildPath 'test'
            $fileWithNoExtensionVerbs = GetProcessInfoVerbs -Path $fileWithNoExtensionPath
        }

        It "Should complete Verb parameter for '<TextInput>'" -Skip:(!([System.Management.Automation.Platform]::IsWindowsDesktop)) -TestCases @(
            @{ TextInput = 'Start-Process -Verb '; ExpectedVerbs = '' }
            @{ TextInput = "Start-Process -FilePath $cmdPath -Verb "; ExpectedVerbs =  $cmdVerbs -join ' ' }
            @{ TextInput = "Start-Process -FilePath $cmdPath -Verb '"; ExpectedVerbs =  $cmdVerbsSingleQuote -join ' ' }
            @{ TextInput = "Start-Process -FilePath $cmdPath -Verb """; ExpectedVerbs =   $cmdVerbsDoubleQuote -join ' ' }
            @{ TextInput = "Start-Process -FilePath $exePath -Verb "; ExpectedVerbs = $exeVerbs -join ' ' }
            @{ TextInput = "Start-Process -FilePath $exePath -Verb run"; ExpectedVerbs = $exeVerbsStartingWithRun -join ' ' }
            @{ TextInput = "Start-Process -FilePath $exePath -Verb 'run"; ExpectedVerbs = $exeVerbsStartingWithRunSingleQuote -join ' ' }
            @{ TextInput = "Start-Process -FilePath $exePath -Verb ""run"; ExpectedVerbs = $exeVerbsStartingWithRunDoubleQuote -join ' ' }
            @{ TextInput = "Start-Process -FilePath $powerShellExeWithNoExtension -Verb "; ExpectedVerbs = $exeVerbs -join ' ' }
            @{ TextInput = "Start-Process -FilePath $txtPath -Verb "; ExpectedVerbs = $txtVerbs -join ' ' }
            @{ TextInput = "Start-Process -FilePath $wavPath -Verb "; ExpectedVerbs = $wavVerbs -join ' ' }
            @{ TextInput = "Start-Process -FilePath $docxPath -Verb "; ExpectedVerbs = $docxVerbs -join ' ' }
            @{ TextInput = "Start-Process -FilePath $fileWithNoExtensionPath -Verb "; ExpectedVerbs = $fileWithNoExtensionVerbs -join ' ' }
        ) {
            param($TextInput, $ExpectedVerbs)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText | Sort-Object
            $completionText -join ' ' | Should -BeExactly $ExpectedVerbs
        }
    }

    Context 'Scope parameter completion' {
        BeforeAll {
            $allScopes = 'Global Local Script'
            $allScopesSingleQuote = "'Global' 'Local' 'Script'"
            $allScopesDoubleQuote = """Global"" ""Local"" ""Script"""
            $globalScope = 'Global'
            $globalScopeSingleQuote = "'Global'"
            $globalScopeDoubleQuote = """Global"""
            $localScope = 'Local'
            $localScopeSingleQuote = "'Local'"
            $localScopeDoubleQuote = """Local"""
            $scriptScope = 'Script'
            $scriptScopeSingleQuote = "'Script'"
            $scriptScopeDoubleQuote = """Script"""
            $allScopeCommands = 'Clear-Variable', 'Export-Alias', 'Get-Alias', 'Get-PSDrive', 'Get-Variable', 'Import-Alias', 'New-Alias', 'New-PSDrive', 'New-Variable', 'Remove-Alias', 'Remove-PSDrive', 'Remove-Variable', 'Set-Alias', 'Set-Variable'
        }

        It "Should complete '<ParameterInput>' for '<Commands>'" -TestCases @(
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope "; ExpectedScopes = $allScopes }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope '"; ExpectedScopes = $allScopesSingleQuote }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope """; ExpectedScopes = $allScopesDoubleQuote }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope G"; ExpectedScopes = $globalScope }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope 'G"; ExpectedScopes = $globalScopeSingleQuote }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope ""G"; ExpectedScopes = $globalScopeDoubleQuote }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope Lo"; ExpectedScopes = $localScope }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope 'Lo"; ExpectedScopes = $localScopeSingleQuote }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope ""Lo"; ExpectedScopes = $localScopeDoubleQuote }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope Scr"; ExpectedScopes = $scriptScope }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope 'Scr"; ExpectedScopes = $scriptScopeSingleQuote }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope ""Scr"; ExpectedScopes = $scriptScopeDoubleQuote }
            @{ Commands = $allScopeCommands; ParameterInput = "-Scope NonExistentScope"; ExpectedScopes = '' }
        ) {
            param($Commands, $ParameterInput, $ExpectedScopes)
            foreach ($command in $Commands) {
                $joinedCommand = "$command $ParameterInput"
                $res = TabExpansion2 -inputScript $joinedCommand -cursorColumn $joinedCommand.Length
                $completionText = $res.CompletionMatches.CompletionText | Sort-Object
                $completionText -join ' ' | Should -BeExactly $ExpectedScopes
            }
        }
    }

    Context 'Get-Verb & Get-Command -Verb parameter completion' {
        BeforeAll {
            $allVerbs = 'Add Approve Assert Backup Block Build Checkpoint Clear Close Compare Complete Compress Confirm Connect Convert ConvertFrom ConvertTo Copy Debug Deny Deploy Disable Disconnect Dismount Edit Enable Enter Exit Expand Export Find Format Get Grant Group Hide Import Initialize Install Invoke Join Limit Lock Measure Merge Mount Move New Open Optimize Out Ping Pop Protect Publish Push Read Receive Redo Register Remove Rename Repair Request Reset Resize Resolve Restart Restore Resume Revoke Save Search Select Send Set Show Skip Split Start Step Stop Submit Suspend Switch Sync Test Trace Unblock Undo Uninstall Unlock Unprotect Unpublish Unregister Update Use Wait Watch Write'
            $allVerbsSingleQuote = "'Add' 'Approve' 'Assert' 'Backup' 'Block' 'Build' 'Checkpoint' 'Clear' 'Close' 'Compare' 'Complete' 'Compress' 'Confirm' 'Connect' 'Convert' 'ConvertFrom' 'ConvertTo' 'Copy' 'Debug' 'Deny' 'Deploy' 'Disable' 'Disconnect' 'Dismount' 'Edit' 'Enable' 'Enter' 'Exit' 'Expand' 'Export' 'Find' 'Format' 'Get' 'Grant' 'Group' 'Hide' 'Import' 'Initialize' 'Install' 'Invoke' 'Join' 'Limit' 'Lock' 'Measure' 'Merge' 'Mount' 'Move' 'New' 'Open' 'Optimize' 'Out' 'Ping' 'Pop' 'Protect' 'Publish' 'Push' 'Read' 'Receive' 'Redo' 'Register' 'Remove' 'Rename' 'Repair' 'Request' 'Reset' 'Resize' 'Resolve' 'Restart' 'Restore' 'Resume' 'Revoke' 'Save' 'Search' 'Select' 'Send' 'Set' 'Show' 'Skip' 'Split' 'Start' 'Step' 'Stop' 'Submit' 'Suspend' 'Switch' 'Sync' 'Test' 'Trace' 'Unblock' 'Undo' 'Uninstall' 'Unlock' 'Unprotect' 'Unpublish' 'Unregister' 'Update' 'Use' 'Wait' 'Watch' 'Write'"
            $allVerbsDoubleQuote = """Add"" ""Approve"" ""Assert"" ""Backup"" ""Block"" ""Build"" ""Checkpoint"" ""Clear"" ""Close"" ""Compare"" ""Complete"" ""Compress"" ""Confirm"" ""Connect"" ""Convert"" ""ConvertFrom"" ""ConvertTo"" ""Copy"" ""Debug"" ""Deny"" ""Deploy"" ""Disable"" ""Disconnect"" ""Dismount"" ""Edit"" ""Enable"" ""Enter"" ""Exit"" ""Expand"" ""Export"" ""Find"" ""Format"" ""Get"" ""Grant"" ""Group"" ""Hide"" ""Import"" ""Initialize"" ""Install"" ""Invoke"" ""Join"" ""Limit"" ""Lock"" ""Measure"" ""Merge"" ""Mount"" ""Move"" ""New"" ""Open"" ""Optimize"" ""Out"" ""Ping"" ""Pop"" ""Protect"" ""Publish"" ""Push"" ""Read"" ""Receive"" ""Redo"" ""Register"" ""Remove"" ""Rename"" ""Repair"" ""Request"" ""Reset"" ""Resize"" ""Resolve"" ""Restart"" ""Restore"" ""Resume"" ""Revoke"" ""Save"" ""Search"" ""Select"" ""Send"" ""Set"" ""Show"" ""Skip"" ""Split"" ""Start"" ""Step"" ""Stop"" ""Submit"" ""Suspend"" ""Switch"" ""Sync"" ""Test"" ""Trace"" ""Unblock"" ""Undo"" ""Uninstall"" ""Unlock"" ""Unprotect"" ""Unpublish"" ""Unregister"" ""Update"" ""Use"" ""Wait"" ""Watch"" ""Write"""
            $verbsStartingWithRe = 'Read Receive Redo Register Remove Rename Repair Request Reset Resize Resolve Restart Restore Resume Revoke'
            $verbsStartingWithEx = 'Exit Expand Export'
            $verbsStartingWithConv = 'Convert ConvertFrom ConvertTo'
            $lifeCycleVerbsStartingWithRe = 'Register Request Restart Resume'
            $lifeCycleVerbsStartingWithReSingleQuote = "'Register' 'Request' 'Restart' 'Resume'"
            $lifeCycleVerbsStartingWithReDoubleQuote = """Register"" ""Request"" ""Restart"" ""Resume"""
            $dataVerbsStartingwithEx = 'Expand Export'
            $lifeCycleAndCommmonVerbsStartingWithRe = 'Redo Register Remove Rename Request Reset Resize Restart Resume'
            $allLifeCycleAndCommonVerbs = 'Add Approve Assert Build Clear Close Complete Confirm Copy Deny Deploy Disable Enable Enter Exit Find Format Get Hide Install Invoke Join Lock Move New Open Optimize Pop Push Redo Register Remove Rename Request Reset Resize Restart Resume Search Select Set Show Skip Split Start Step Stop Submit Suspend Switch Undo Uninstall Unlock Unregister Wait Watch'
            $allJsonVerbs = 'ConvertFrom ConvertTo Test'
            $jsonVerbsStartingWithConv = 'ConvertFrom ConvertTo'
            $jsonVerbsStartingWithConvSingleQuote = "'ConvertFrom' 'ConvertTo'"
            $jsonVerbsStartingWithConvDoubleQuote = """ConvertFrom"" ""ConvertTo"""
            $allJsonAndJobVerbs = 'ConvertFrom ConvertTo Debug Get Receive Remove Start Stop Test Wait'
            $jsonAndJobVerbsStartingWithSt = 'Start Stop'
            $allObjectVerbs = 'Compare ForEach Group Measure New Select Sort Tee Where'
            $utilityModuleObjectVerbs = 'Compare Group Measure New Select Sort Tee'
            $utilityModuleObjectVerbsStartingWithS = 'Select Sort'
            $utilityModuleObjectVerbsStartingWithSSingleQuote = "'Select' 'Sort'"
            $utilityModuleObjectVerbsStartingWithSDoubleQuote = """Select"" ""Sort"""
            $utilityModuleObjectVerbsStartingWithS
            $coreModuleObjectVerbs = 'ForEach Where'
        }

        It "Should complete Verb parameter for '<TextInput>'" -TestCases @(
            @{ TextInput = 'Get-Verb -Verb '; ExpectedVerbs = $allVerbs }
            @{ TextInput = "Get-Verb -Verb '"; ExpectedVerbs = $allVerbsSingleQuote }
            @{ TextInput = "Get-Verb -Verb """; ExpectedVerbs = $allVerbsDoubleQuote }
            @{ TextInput = 'Get-Verb -Group Lifecycle, Common -Verb '; ExpectedVerbs = $allLifeCycleAndCommonVerbs }
            @{ TextInput = 'Get-Verb -Verb Re'; ExpectedVerbs = $verbsStartingWithRe }
            @{ TextInput = 'Get-Verb -Group Lifecycle -Verb Re'; ExpectedVerbs = $lifeCycleVerbsStartingWithRe }
            @{ TextInput = "Get-Verb -Group Lifecycle -Verb 'Re"; ExpectedVerbs = $lifeCycleVerbsStartingWithReSingleQuote }
            @{ TextInput = "Get-Verb -Group Lifecycle -Verb ""Re"; ExpectedVerbs = $lifeCycleVerbsStartingWithReDoubleQuote }
            @{ TextInput = 'Get-Verb -Group Lifecycle -Verb Re'; ExpectedVerbs = $lifeCycleVerbsStartingWithRe }
            @{ TextInput = 'Get-Verb -Group Lifecycle, Common -Verb Re'; ExpectedVerbs = $lifeCycleAndCommmonVerbsStartingWithRe }
            @{ TextInput = 'Get-Verb -Verb Ex'; ExpectedVerbs = $verbsStartingWithEx }
            @{ TextInput = 'Get-Verb -Group Data -Verb Ex'; ExpectedVerbs = $dataVerbsStartingwithEx }
            @{ TextInput = 'Get-Verb -Group NonExistentGroup -Verb '; ExpectedVerbs = '' }
            @{ TextInput = 'Get-Verb -Verb Conv'; ExpectedVerbs = $verbsStartingWithConv }
            @{ TextInput = 'Get-Command -Verb '; ExpectedVerbs = $allVerbs }
            @{ TextInput = 'Get-Command -Verb Re'; ExpectedVerbs = $verbsStartingWithRe }
            @{ TextInput = 'Get-Command -Verb Ex'; ExpectedVerbs = $verbsStartingWithEx }
            @{ TextInput = 'Get-Command -Verb Conv'; ExpectedVerbs = $verbsStartingWithConv }
            @{ TextInput = 'Get-Command -Noun Json -Verb '; ExpectedVerbs = $allJsonVerbs }
            @{ TextInput = 'Get-Command -Noun Json -Verb Conv'; ExpectedVerbs = $jsonVerbsStartingWithConv }
            @{ TextInput = "Get-Command -Noun Json -Verb 'Conv"; ExpectedVerbs = $jsonVerbsStartingWithConvSingleQuote }
            @{ TextInput = "Get-Command -Noun Json -Verb ""Conv"; ExpectedVerbs = $jsonVerbsStartingWithConvDoubleQuote }
            @{ TextInput = 'Get-Command -Noun Json, Job -Verb '; ExpectedVerbs = $allJsonAndJobVerbs }
            @{ TextInput = 'Get-Command -Noun Json, Job -Verb St'; ExpectedVerbs = $jsonAndJobVerbsStartingWithSt }
            @{ TextInput = 'Get-Command -Noun NonExistentNoun -Verb '; ExpectedVerbs = '' }
            @{ TextInput = 'Get-Command -Noun Object -Module Microsoft.PowerShell.Utility,Microsoft.PowerShell.Core -Verb '; ExpectedVerbs = $allObjectVerbs }
            @{ TextInput = 'Get-Command -Noun Object -Module Microsoft.PowerShell.Utility -Verb '; ExpectedVerbs = $utilityModuleObjectVerbs }
            @{ TextInput = 'Get-Command -Noun Object -Module Microsoft.PowerShell.Utility -Verb S'; ExpectedVerbs = $utilityModuleObjectVerbsStartingWithS }
            @{ TextInput = "Get-Command -Noun Object -Module Microsoft.PowerShell.Utility -Verb 'S"; ExpectedVerbs = $utilityModuleObjectVerbsStartingWithSSingleQuote }
            @{ TextInput = "Get-Command -Noun Object -Module Microsoft.PowerShell.Utility -Verb ""S"; ExpectedVerbs = $utilityModuleObjectVerbsStartingWithSDoubleQuote }
            @{ TextInput = 'Get-Command -Noun Object -Module Microsoft.PowerShell.Core -Verb '; ExpectedVerbs = $coreModuleObjectVerbs }
        ) {
            param($TextInput, $ExpectedVerbs)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText | Sort-Object
            $completionText -join ' ' | Should -BeExactly $ExpectedVerbs
        }
    }

    Context 'StrictMode Version parameter completion' {
        BeforeAll {
            $allStrictModeVersions = '1.0 2.0 3.0 Latest'
            $allStrictModeVersionsSingleQuote = "'1.0' '2.0' '3.0' 'Latest'"
            $allStrictModeVersionsDoubleQuote = """1.0"" ""2.0"" ""3.0"" ""Latest"""
            $versionOne = '1.0'
            $versionTwo = '2.0'
            $versionThree = '3.0'
            $latestVersion = 'Latest'
            $latestVersionSingleQuote = "'Latest'"
            $latestVersionDoubleQuote = """Latest"""
        }

        It "Should complete Version for '<TextInput>'" -TestCases @(
            @{ TextInput = "Set-StrictMode -Version "; ExpectedVersions = $allStrictModeVersions }
            @{ TextInput = "Set-StrictMode -Version '"; ExpectedVersions = $allStrictModeVersionsSingleQuote }
            @{ TextInput = "Set-StrictMode -Version """; ExpectedVersions = $allStrictModeVersionsDoubleQuote }
            @{ TextInput = "Set-StrictMode -Version 1"; ExpectedVersions = $versionOne }
            @{ TextInput = "Set-StrictMode -Version 2"; ExpectedVersions = $versionTwo }
            @{ TextInput = "Set-StrictMode -Version 3"; ExpectedVersions = $versionThree }
            @{ TextInput = "Set-StrictMode -Version Lat"; ExpectedVersions = $latestVersion }
            @{ TextInput = "Set-StrictMode -Version 'Lat"; ExpectedVersions = $latestVersionSingleQuote }
            @{ TextInput = "Set-StrictMode -Version ""Lat"; ExpectedVersions = $latestVersionDoubleQuote }
            @{ TextInput = "Set-StrictMode -Version NonExistentVersion"; ExpectedVersions = '' }
        ) {
            param($TextInput, $ExpectedVersions)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText | Sort-Object
            $completionText -join ' ' | Should -BeExactly $ExpectedVersions
        }
    }

    Context 'Help Module parameter completion' {
        BeforeAll {
            $utilityModule = 'Microsoft.PowerShell.Utility'
            $managementModule = 'Microsoft.PowerShell.Management'
            $allMicrosoftPowerShellModules = (Get-Module -Name Microsoft.PowerShell* -ListAvailable).Name
            Import-Module -Name $allMicrosoftPowerShellModules -ErrorAction SilentlyContinue
            $allMicrosoftPowerShellModules = ($allMicrosoftPowerShellModules | Sort-Object -Unique) -join ' '
        }

        It "Should complete Module for '<TextInput>'" -TestCases @(
            @{ TextInput = "Save-Help -Module Microsoft.PowerShell.U"; ExpectedModules = $utilityModule }
            @{ TextInput = "Update-Help -Module Microsoft.PowerShell.U"; ExpectedModules = $utilityModule }
            @{ TextInput = "Save-Help -Module Microsoft.PowerShell.Man"; ExpectedModules = $managementModule }
            @{ TextInput = "Update-Help -Module Microsoft.PowerShell.Man"; ExpectedModules = $managementModule }
            @{ TextInput = "Save-Help -Module Microsoft.Powershell"; ExpectedModules = $allMicrosoftPowerShellModules }
            @{ TextInput = "Update-Help -Module Microsoft.PowerShell"; ExpectedModules = $allMicrosoftPowerShellModules }
            @{ TextInput = "Save-Help -Module NonExistentModulePrefix"; ExpectedModules = '' }
            @{ TextInput = "Update-Help -Module NonExistentModulePrefix"; ExpectedModules = '' }
        ) {
            param($TextInput, $ExpectedModules)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText | Sort-Object -Unique
            $completionText -join ' ' | Should -BeExactly $ExpectedModules
        }
    }

    Context 'New-ItemProperty -PropertyType parameter completion' {
        BeforeAll {
            if ($IsWindows) {
                $allRegistryValueKinds = 'String ExpandString Binary DWord MultiString QWord Unknown'
                $allRegistryValueKindsWithQuotes = "'String' 'ExpandString' 'Binary' 'DWord' 'MultiString' 'QWord' 'Unknown'"
                $dwordValueKind = 'DWord'
                $qwordValueKind = 'QWord'
                $binaryValueKind = 'Binary'
                $multiStringValueKind = 'MultiString'
                $registryPath = "HKCU:\test1\sub"
                New-Item -Path $registryPath -Force
                $registryLiteralPath = "HKCU:\test2\*\sub"
                New-Item -Path $registryLiteralPath -Force
                $fileSystemPath = "TestDrive:\test1.txt"
                New-Item -Path $fileSystemPath -Force
                $fileSystemLiteralPathDir = "TestDrive:\[]"
                $fileSystemLiteralPath = "$fileSystemLiteralPathDir\test2.txt"
                New-Item -Path $fileSystemLiteralPath -Force
            }
        }

        It "Should complete Property Type for '<TextInput>'" -Skip:(!$IsWindows) -TestCases @(
            # -Path completions
            @{ TextInput = "New-ItemProperty -Path $registryPath -PropertyType "; ExpectedPropertyTypes = $allRegistryValueKinds }
            @{ TextInput = "New-ItemProperty -Path $registryPath -PropertyType d"; ExpectedPropertyTypes = $dwordValueKind }
            @{ TextInput = "New-ItemProperty -Path $registryPath -PropertyType q"; ExpectedPropertyTypes = $qwordValueKind }
            @{ TextInput = "New-ItemProperty -Path $registryPath -PropertyType bin"; ExpectedPropertyTypes = $binaryValueKind }
            @{ TextInput = "New-ItemProperty -Path $registryPath -PropertyType multi"; ExpectedPropertyTypes = $multiStringValueKind }
            @{ TextInput = "New-ItemProperty -Path $registryPath -PropertyType invalidproptype"; ExpectedPropertyTypes = '' }
            @{ TextInput = "New-ItemProperty -Path $fileSystemPath -PropertyType "; ExpectedPropertyTypes = '' }

            # -LiteralPath completions
            @{ TextInput = "New-ItemProperty -LiteralPath $registryLiteralPath -PropertyType "; ExpectedPropertyTypes = $allRegistryValueKinds }
            @{ TextInput = "New-ItemProperty -LiteralPath $registryLiteralPath -PropertyType d"; ExpectedPropertyTypes = $dwordValueKind }
            @{ TextInput = "New-ItemProperty -LiteralPath $registryLiteralPath -PropertyType q"; ExpectedPropertyTypes = $qwordValueKind }
            @{ TextInput = "New-ItemProperty -LiteralPath $registryLiteralPath -PropertyType bin"; ExpectedPropertyTypes = $binaryValueKind }
            @{ TextInput = "New-ItemProperty -LiteralPath $registryLiteralPath -PropertyType multi"; ExpectedPropertyTypes = $multiStringValueKind }
            @{ TextInput = "New-ItemProperty -LiteralPath $registryLiteralPath -PropertyType invalidproptype"; ExpectedPropertyTypes = '' }
            @{ TextInput = "New-ItemProperty -LiteralPath $fileSystemLiteralPath -PropertyType "; ExpectedPropertyTypes = '' }

            # All of these should return no completion since they don't specify -Path/-LiteralPath
            @{ TextInput = "New-ItemProperty -PropertyType "; ExpectedPropertyTypes = '' }
            @{ TextInput = "New-ItemProperty -PropertyType d"; ExpectedPropertyTypes = '' }
            @{ TextInput = "New-ItemProperty -PropertyType q"; ExpectedPropertyTypes = '' }
            @{ TextInput = "New-ItemProperty -PropertyType bin"; ExpectedPropertyTypes = '' }
            @{ TextInput = "New-ItemProperty -PropertyType multi"; ExpectedPropertyTypes = '' }
            @{ TextInput = "New-ItemProperty -PropertyType invalidproptype"; ExpectedPropertyTypes = '' }

            # All of these should return completion even with quotes included
            @{ TextInput = "New-ItemProperty -Path $registryPath -PropertyType '"; ExpectedPropertyTypes = $allRegistryValueKindsWithQuotes }
            @{ TextInput = "New-ItemProperty -Path $registryPath -PropertyType 'bin"; ExpectedPropertyTypes = "'$binaryValueKind'" }
        ) {
            param($TextInput, $ExpectedPropertyTypes)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText
            $completionText -join ' ' | Should -BeExactly $ExpectedPropertyTypes

            foreach ($match in $res.CompletionMatches) {
                $completionText = $match.CompletionText.Replace("""", "").Replace("'", "")
                $listItemText = $match.ListItemText
                $completionText | Should -BeExactly $listItemText
                $match.ToolTip | Should -Not -BeNullOrEmpty
            }
        }

        It "Test fallback to provider of current location if no path specified" -Skip:(!$IsWindows) {
            try {
                Push-Location HKCU:\
                $textInput = "New-ItemProperty -PropertyType "
                $res = TabExpansion2 -inputScript $textInput -cursorColumn $textInput.Length
                $completionText = $res.CompletionMatches.CompletionText
                $completionText -join ' ' | Should -BeExactly $allRegistryValueKinds
            }
            finally {
                Pop-Location
            }
        }

        AfterAll {
            if ($IsWindows) {
                Remove-Item -Path $registryPath -Force
                Remove-Item -LiteralPath $registryLiteralPath -Force
                Remove-Item -Path $fileSystemPath -Force
                Remove-Item -LiteralPath $fileSystemLiteralPathDir -Recurse -Force
            }
        }
    }

    Context 'Get-Command -Noun parameter completion' {
        BeforeAll {
            function GetModuleCommandNouns(
                [string]$Module,
                [string]$Verb,
                [switch]$SingleQuote,
                [switch]$DoubleQuote)
            {

                $commandParams = @{}

                if ($PSBoundParameters.ContainsKey('Module')) {
                    $commandParams['Module'] = $Module
                }

                if ($PSBoundParameters.ContainsKey('Verb')) {
                    $commandParams['Verb'] = $Verb
                }

                $nouns = (Get-Command @commandParams).Noun

                if ($SingleQuote) {
                    return ($nouns | ForEach-Object { "'$_'" })
                }
                elseif ($DoubleQuote) {
                    return ($nouns | ForEach-Object { """$_""" })
                }

                return $nouns
            }

            $utilityModuleName = 'Microsoft.PowerShell.Utility'

            $allUtilityCommandNouns = GetModuleCommandNouns -Module $utilityModuleName
            $allUtilityCommandNounsSingleQuote = GetModuleCommandNouns -Module $utilityModuleName -SingleQuote
            $allUtilityCommandNounsDoubleQuote = GetModuleCommandNouns -Module $utilityModuleName -DoubleQuote
            $utilityCommandNounsStartingWithF = $allUtilityCommandNouns | Where-Object { $_ -like 'F*'}
            $utilityCommandNounsStartingWithFSingleQuote = $allUtilityCommandNounsSingleQuote | Where-Object { $_ -like "'F*"}
            $utilityCommandNounsStartingWithFDoubleQuote = $allUtilityCommandNounsDoubleQuote | Where-Object { $_ -like """F*"}

            $allUtilityCommandNounsWithConvertToVerb = GetModuleCommandNouns -Module $utilityModuleName -Verb 'ConvertTo'
            $allUtilityCommandNounsWithConvertToVerbSingleQuote = GetModuleCommandNouns -Module $utilityModuleName -SingleQuote -Verb 'ConvertTo'
            $allUtilityCommandNounsWithConvertToVerbDoubleQuote = GetModuleCommandNouns -Module $utilityModuleName -DoubleQuote -Verb 'ConvertTo'
            $utilityCommandNounsWithConvertToVerb = $allUtilityCommandNounsWithConvertToVerb | Where-Object { $_ -in 'CliXml', 'Csv', 'Html', 'Json', 'Xml' }
            $utilityCommandNounsWithConvertToVerbSingleQuote = $allUtilityCommandNounsWithConvertToVerbSingleQuote | Where-Object { $_ -in "'CliXml'", "'Csv'", "'Html'", "'Json'", "'Xml'" }
            $utilityCommandNounsWithConvertToVerbDoubleQuote = $allUtilityCommandNounsWithConvertToVerbDoubleQuote | Where-Object { $_ -in """CliXml""", """Csv""", """Html""", """Json""", """Xml""" }
            $utilityCommandNounsWithConvertToVerbStartingWithC = $allUtilityCommandNounsWithConvertToVerb | Where-Object { $_ -in 'CliXml', 'Csv' }
            $utilityCommandNounsWithConvertToVerbStartingWithCSingleQuote = $allUtilityCommandNounsWithConvertToVerbSingleQuote | Where-Object { $_ -in "'CliXml'", "'Csv'" }
            $utilityCommandNounsWithConvertToVerbStartingWithCDoubleQuote = $allUtilityCommandNounsWithConvertToVerbDoubleQuote | Where-Object { $_ -in """CliXml""", """Csv""" }
        }

        It "Should complete Noun for '<TextInput>'" -TestCases @(
            @{ TextInput = "Get-Command -Module $utilityModuleName -Noun "; ExpectedNouns = $allUtilityCommandNouns }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Noun '"; ExpectedNouns = $allUtilityCommandNounsSingleQuote }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Noun """; ExpectedNouns = $allUtilityCommandNounsDoubleQuote }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Noun F"; ExpectedNouns = $utilityCommandNounsStartingWithF  }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Noun 'F"; ExpectedNouns = $utilityCommandNounsStartingWithFSingleQuote }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Noun ""F"; ExpectedNouns = $utilityCommandNounsStartingWithFDoubleQuote }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Verb ConvertTo -Noun "; ExpectedNouns = $utilityCommandNounsWithConvertToVerb  }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Verb ConvertTo -Noun '"; ExpectedNouns = $utilityCommandNounsWithConvertToVerbSingleQuote }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Verb ConvertTo -Noun """; ExpectedNouns = $utilityCommandNounsWithConvertToVerbDoubleQuote }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Verb ConvertTo -Noun C"; ExpectedNouns = $utilityCommandNounsWithConvertToVerbStartingWithC  }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Verb ConvertTo -Noun 'C"; ExpectedNouns = $utilityCommandNounsWithConvertToVerbStartingWithCSingleQuote }
            @{ TextInput = "Get-Command -Module $utilityModuleName -Verb ConvertTo -Noun ""C"; ExpectedNouns = $utilityCommandNounsWithConvertToVerbStartingWithCDoubleQuote }
        ) {
            param($TextInput, $ExpectedNouns)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText

            # Avoid using Sort-Object -Unique because it generates different order than SortedSet on MacOS/Linux
            $sortedSetExpectedNouns = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
            foreach ($noun in $ExpectedNouns)
            {
                $sortedSetExpectedNouns.Add($noun) | Out-Null
            }

            $completionText -join ' ' | Should -BeExactly ($sortedSetExpectedNouns -join ' ')
        }
    }

    Context "Get-ExperimentalFeature -Name parameter completion" {
        BeforeAll {
            function GetExperimentalFeatureNames([switch]$SingleQuote, [switch]$DoubleQuote) {
                $features = (Get-ExperimentalFeature).Name

                if ($SingleQuote) {
                    return ($features | ForEach-Object { "'$_'" })
                }
                elseif ($DoubleQuote) {
                    return ($features | ForEach-Object { """$_""" })
                }

                return $features
            }

            $allExperimentalFeatures = GetExperimentalFeatureNames
            $allExperimentalFeaturesSingleQuote = GetExperimentalFeatureNames -SingleQuote
            $allExperimentalFeaturesDoubleQuote = GetExperimentalFeatureNames -DoubleQuote
            $experimentalFeaturesStartingWithPS = $allExperimentalFeatures | Where-Object { $_ -like 'PS*'}
            $experimentalFeaturesStartingWithPSSingleQuote = $allExperimentalFeaturesSingleQuote | Where-Object { $_ -like "'PS*" }
            $experimentalFeaturesStartingWithPSDoubleQuote = $allExperimentalFeaturesDoubleQuote | Where-Object { $_ -like """PS*" }
        }

        It "Should complete Name for '<TextInput>'" -TestCases @(
            @{ TextInput = "Get-ExperimentalFeature -Name "; ExpectedExperimentalFeatureNames = $allExperimentalFeatures }
            @{ TextInput = "Get-ExperimentalFeature -Name '"; ExpectedExperimentalFeatureNames = $allExperimentalFeaturesSingleQuote }
            @{ TextInput = "Get-ExperimentalFeature -Name """; ExpectedExperimentalFeatureNames = $allExperimentalFeaturesDoubleQuote }
            @{ TextInput = "Get-ExperimentalFeature -Name PS"; ExpectedExperimentalFeatureNames = $experimentalFeaturesStartingWithPS }
            @{ TextInput = "Get-ExperimentalFeature -Name 'PS"; ExpectedExperimentalFeatureNames = $experimentalFeaturesStartingWithPSSingleQuote }
            @{ TextInput = "Get-ExperimentalFeature -Name ""PS"; ExpectedExperimentalFeatureNames = $experimentalFeaturesStartingWithPSDoubleQuote }
        ) {
            param($TextInput, $ExpectedExperimentalFeatureNames)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText
            $completionText -join ' ' | Should -BeExactly (($ExpectedExperimentalFeatureNames | Sort-Object -Unique) -join ' ')
        }
    }

    Context "Join-String -Separator & -FormatString parameter completion" {
        BeforeAll {
            if ($IsWindows) {
                $allSeparators = "',' ', ' ';' '; ' ""``r``n"" '-' ' '"
                $allFormatStrings = "'[{0}]' '{0:N2}' ""``r``n    ```${0}"" ""``r``n    [string] ```${0}"""
                $newlineSeparator = """``r``n"""
                $newlineFormatStrings = """``r``n    ```${0}"" ""``r``n    [string] ```${0}"""
            }
            else {
                $allSeparators = "',' ', ' ';' '; ' ""``n"" '-' ' '"
                $allFormatStrings = "'[{0}]' '{0:N2}' ""``n    ```${0}"" ""``n    [string] ```${0}"""
                $newlineSeparator = """``n"""
                $newlineFormatStrings = """``n    ```${0}"" ""``n    [string] ```${0}"""
            }

            $commaSeparators = "',' ', '"
            $semiColonSeparators = "';' '; '"
            
            $squareBracketFormatString = "'[{0}]'"
            $curlyBraceFormatString = "'{0:N2}'"
        }

        It "Should complete for '<TextInput>'" -TestCases @(
            @{ TextInput = "Join-String -Separator "; Expected = $allSeparators }
            @{ TextInput = "Join-String -Separator '"; Expected = $allSeparators }
            @{ TextInput = "Join-String -Separator """; Expected = $allSeparators.Replace("'", """") }
            @{ TextInput = "Join-String -Separator ',"; Expected = $commaSeparators }
            @{ TextInput = "Join-String -Separator "","; Expected = $commaSeparators.Replace("'", """") }
            @{ TextInput = "Join-String -Separator ';"; Expected = $semiColonSeparators }
            @{ TextInput = "Join-String -Separator "";"; Expected = $semiColonSeparators.Replace("'", """") }
            @{ TextInput = "Join-String -FormatString "; Expected = $allFormatStrings }
            @{ TextInput = "Join-String -FormatString '"; Expected = $allFormatStrings }
            @{ TextInput = "Join-String -FormatString """; Expected = $allFormatStrings.Replace("'", """") }
            @{ TextInput = "Join-String -FormatString ["; Expected = $squareBracketFormatString }
            @{ TextInput = "Join-String -FormatString '["; Expected = $squareBracketFormatString }
            @{ TextInput = "Join-String -FormatString ""["; Expected = $squareBracketFormatString.Replace("'", """") }
            @{ TextInput = "Join-String -FormatString '{"; Expected = $curlyBraceFormatString }
            @{ TextInput = "Join-String -FormatString ""{"; Expected = $curlyBraceFormatString.Replace("'", """") }
        ) {
            param($TextInput, $Expected)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText
            $completionText -join ' ' | Should -BeExactly $Expected

            foreach ($match in $res.CompletionMatches) {
                $toolTip = $match.ToolTip.Replace("""", "").Replace("'", "")
                $completionText = $match.CompletionText.Replace("""", "").Replace("'", "")
                $listItemText = $match.ListItemText
                $toolTip.StartsWith($completionText) | Should -BeTrue
                $toolTip.EndsWith($listItemText) | Should -BeTrue
            }
        }

        It "Should complete for '<TextInput>'" -Skip:(!$IsWindows) -TestCases @(
            @{ TextInput = "Join-String -Separator '``"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -Separator ""``"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -Separator '``r"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -Separator ""``r"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -Separator '``r``"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -Separator ""``r``"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -FormatString '``"; Expected = $newlineFormatStrings }
            @{ TextInput = "Join-String -FormatString ""``"; Expected = $newlineFormatStrings }
            @{ TextInput = "Join-String -FormatString '``r"; Expected = $newlineFormatStrings }
            @{ TextInput = "Join-String -FormatString ""``r"; Expected = $newlineFormatStrings }
            @{ TextInput = "Join-String -FormatString '``r``"; Expected = $newlineFormatStrings }
            @{ TextInput = "Join-String -FormatString ""``r``"; Expected = $newlineFormatStrings }
        ) {
            param($TextInput, $Expected)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText
            $completionText -join ' ' | Should -BeExactly $Expected

            foreach ($match in $res.CompletionMatches) {
                $toolTip = $match.ToolTip.Replace("""", "").Replace("'", "")
                $completionText = $match.CompletionText.Replace("""", "").Replace("'", "")
                $listItemText = $match.ListItemText
                $toolTip.StartsWith($completionText) | Should -BeTrue
                $toolTip.EndsWith($listItemText) | Should -BeTrue
            }
        }

        It "Should complete for '<TextInput>'" -Skip:($IsWindows) -TestCases @(
            @{ TextInput = "Join-String -Separator '``"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -Separator ""``"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -Separator '``n"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -Separator ""``n"; Expected = $newlineSeparator }
            @{ TextInput = "Join-String -FormatString '``"; Expected = $newlineFormatStrings }
            @{ TextInput = "Join-String -FormatString ""``"; Expected = $newlineFormatStrings }
            @{ TextInput = "Join-String -FormatString '``n"; Expected = $newlineFormatStrings }
            @{ TextInput = "Join-String -FormatString ""``n"; Expected = $newlineFormatStrings }
        ) {
            param($TextInput, $Expected)
            $res = TabExpansion2 -inputScript $TextInput -cursorColumn $TextInput.Length
            $completionText = $res.CompletionMatches.CompletionText
            $completionText -join ' ' | Should -BeExactly $Expected

            foreach ($match in $res.CompletionMatches) {
                $toolTip = $match.ToolTip.Replace("""", "").Replace("'", "")
                $completionText = $match.CompletionText.Replace("""", "").Replace("'", "")
                $listItemText = $match.ListItemText
                $toolTip.StartsWith($completionText) | Should -BeTrue
                $toolTip.EndsWith($listItemText) | Should -BeTrue
            }
        }
    }

    Context "Format cmdlet's View paramter completion" {
        BeforeAll {
            $viewDefinition = @'
<?xml version="1.0" encoding="utf-8"?>
<Configuration>
  <ViewDefinitions>
    <View>
      <Name>R A M</Name>
      <ViewSelectedBy>
        <TypeName>System.Diagnostics.Process</TypeName>
      </ViewSelectedBy>
      <TableControl>
        <TableHeaders>
          <TableColumnHeader>
            <Label>ProcName</Label>
            <Width>40</Width>
            <Alignment>Center</Alignment>
          </TableColumnHeader>
          <TableColumnHeader>
            <Label>PagedMem</Label>
            <Width>40</Width>
            <Alignment>Center</Alignment>
          </TableColumnHeader>
          <TableColumnHeader>
            <Label>PeakWS</Label>
            <Width>40</Width>
            <Alignment>Center</Alignment>
          </TableColumnHeader>
        </TableHeaders>
        <TableRowEntries>
          <TableRowEntry>
            <TableColumnItems>
              <TableColumnItem>
                <Alignment>Center</Alignment>
                <PropertyName>Name</PropertyName>
              </TableColumnItem>
              <TableColumnItem>
                <Alignment>Center</Alignment>
                <PropertyName>PagedMemorySize</PropertyName>
              </TableColumnItem>
              <TableColumnItem>
                <Alignment>Center</Alignment>
                <PropertyName>PeakWorkingSet</PropertyName>
              </TableColumnItem>
            </TableColumnItems>
          </TableRowEntry>
        </TableRowEntries>
      </TableControl>
    </View>
  </ViewDefinitions>
</Configuration>
'@

            $tempViewFile = Join-Path -Path $TestDrive -ChildPath 'processViewDefinition.ps1xml'
            Set-Content -LiteralPath $tempViewFile -Value $viewDefinition -Force

            $ps = [PowerShell]::Create()
            $null = $ps.AddScript("Update-FormatData -AppendPath $tempViewFile")
            $ps.Invoke()
            $ps.HadErrors | Should -BeFalse
            $ps.Commands.Clear()

            Remove-Item -LiteralPath $tempViewFile -Force -ErrorAction SilentlyContinue
        }

        It 'Should complete Get-ChildItem | <cmd> -View' -TestCases (
            @{ cmd = 'Format-Table'; expected = "children childrenWithHardlink$(if (!$IsWindows) { ' childrenWithUnixStat' })" },
            @{ cmd = 'Format-List'; expected = 'children' },
            @{ cmd = 'Format-Wide'; expected = 'children' },
            @{ cmd = 'Format-Custom'; expected = '' }
        ) {
            param($cmd, $expected)

            # The completion is based on OutputTypeAttribute() of the cmdlet.
            $res = TabExpansion2 -inputScript "Get-ChildItem | $cmd -View " -cursorColumn "Get-ChildItem | $cmd -View ".Length
            $completionText = $res.CompletionMatches.CompletionText | Sort-Object
            $completionText -join ' ' | Should -BeExactly $expected
        }

        It 'Should complete $processList = Get-Process; $processList | <cmd>' -TestCases (
            @{ cmd = 'Format-Table -View '; expected = "'R A M'", "Priority", "process", "ProcessModule", "ProcessWithUserName", "StartTime" },
            @{ cmd = 'Format-List -View '; expected = '' },
            @{ cmd = 'Format-Wide -View '; expected = 'process' },
            @{ cmd = 'Format-Custom -View '; expected = '' },
            @{ cmd = 'Format-Table -View S'; expected = "StartTime" },
            @{ cmd = "Format-Table -View 'S"; expected = "'StartTime'" },
            @{ cmd = "Format-Table -View R"; expected = "'R A M'" }
        ) {
            param($cmd, $expected)

            $null = $ps.AddScript({
                param ($cmd)
                $processList = Get-Process
                $res = TabExpansion2 -inputScript "`$processList | $cmd" -cursorColumn "`$processList | $cmd".Length
                $completionText = $res.CompletionMatches.CompletionText | Sort-Object
                $completionText
            }).AddArgument($cmd)

            $result = $ps.Invoke()
            $ps.Commands.Clear()
            $expected = ($expected | Sort-Object) -join ' '
            $result -join ' ' | Should -BeExactly $expected
        }
    }

    Context NativeCommand {
        BeforeAll {
            ## Find a native command that is not 'pwsh'. We will use 'pwsh' for fallback completer tests later.
            $nativeCommand = Get-Command -CommandType Application -TotalCount 2 |
                Where-Object Name -NotLike pwsh* |
                Select-Object -First 1
        }

        It 'Completes native commands with -' {
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '-') {
                    return "-flag"
                }
                else {
                    return "unexpected wordtocomplete"
                }
            }
            $line = "$nativeCommand -"
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches.CompletionText | Should -BeExactly "-flag"
        }

        It 'Completes native commands with --' {
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '--') {
                    return "--flag"
                }
                else {
                    return "unexpected wordtocomplete"
                }
            }
            $line = "$nativeCommand --"
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches.CompletionText | Should -BeExactly "--flag"
        }

        It 'Completes native commands with --f' {
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '--f') {
                    return "--flag"
                }
                else {
                    return "unexpected wordtocomplete"
                }
            }
            $line = "$nativeCommand --f"
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches.CompletionText | Should -BeExactly "--flag"
        }

        It 'Completes native commands with -o' {
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '-o') {
                    return "-option"
                }
                else {
                    return "unexpected wordtocomplete"
                }
            }
            $line = "$nativeCommand -o"
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches.CompletionText | Should -BeExactly "-option"
        }

        It 'Covers an arbitrary unbound native command with -t' {
            ## Register a completer for $nativeCommand.
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '-t') {
                    return "-terminal"
                }
            }

            ## Register a fallback native command completer.
            Register-ArgumentCompleter -NativeFallback -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '-t') {
                    return "-testing"
                }
            }

            ## The specific completer will be used if it exists.
            $line = "$nativeCommand -t"
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches.CompletionText | Should -BeExactly "-terminal"

            ## Otherwise, the fallback completer will kick in.
            $line = "pwsh -t"
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches.CompletionText | Should -BeExactly "-testing"

            ## Remove the completer for $nativeCommand.
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock $null

            ## The fallback completer will be used for $nativeCommand.
            $line = "$nativeCommand -t"
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches.CompletionText | Should -BeExactly "-testing"

            ## Remove the fallback completer for $nativeCommand.
            Register-ArgumentCompleter -NativeFallback -ScriptBlock $null

            ## The fallback completer will be used for $nativeCommand.
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches | Should -HaveCount 0
        }
    }

    It 'Should complete "Export-Counter -FileFormat" with available output formats' -Pending {
        $res = TabExpansion2 -inputScript 'Export-Counter -FileFormat ' -cursorColumn 'Export-Counter -FileFormat '.Length
        $res.CompletionMatches | Should -HaveCount 3
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should -BeExactly 'blg csv tsv'
    }

    it 'Should include positionally bound parameters when completing in front of parameter value' {
        $TestString = 'Get-ChildItem -^ $HOME'
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -inputScript $TestString.Remove($CursorIndex, 1) -cursorColumn $CursorIndex
        $res.CompletionMatches.CompletionText | Should -Contain "-Path"
    }

    it 'Should find the closest positional parameter match' {
        $TestString = @'
function Verb-Noun
{
    Param
    (
        [Parameter(Position = 0)]
        [string]
        $Param1,
        [Parameter(Position = 1)]
        [System.Management.Automation.ActionPreference]
        $Param2
    )
}
Verb-Noun -Param1 Hello ^
'@
        $CursorIndex = $TestString.IndexOf('^')
        $res = TabExpansion2 -inputScript $TestString.Remove($CursorIndex, 1) -cursorColumn $CursorIndex
        $res.CompletionMatches[0].CompletionText | Should -Be "Break"
    }

    it 'Should complete command with an empty arrayexpression element' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem @()' -cursorColumn 1
        $res.CompletionMatches[0].CompletionText | Should -Be "Get-ChildItem"
    }

    it 'Should not complete TabExpansion2 variables' {
        $res = TabExpansion2 -inputScript '$' -cursorColumn 1
        $res.CompletionMatches.CompletionText | Should -Not -Contain '$positionOfCursor'
    }

    it 'Should prefer the default parameterset when completing positional parameters' {
        $ScriptInput = 'Get-ChildItem | Where-Object '
        $res = TabExpansion2 -inputScript $ScriptInput -cursorColumn $ScriptInput.Length
        $res.CompletionMatches[0].CompletionText | Should -Be "Attributes"
    }

    it 'Should complete base class members of types without type definition AST' {
        $res = TabExpansion2 -inputScript @'
class InheritedClassTest : System.Attribute
{
    [void] TestMethod()
    {
        $this.
'@
        $res.CompletionMatches.CompletionText | Should -Contain 'TypeId'
    }

    it 'Should not complete parameter aliases if the real parameter is in the completion results' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem -p'
        $res.CompletionMatches.CompletionText | Should -Not -Contain '-proga'
        $res.CompletionMatches.CompletionText | Should -Contain '-ProgressAction'
    }

    it 'Should not complete parameter aliases if the real parameter is in the completion results (Non ambiguous parameters)' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem -prog'
        $res.CompletionMatches.CompletionText | Should -Not -Contain '-proga'
        $res.CompletionMatches.CompletionText | Should -Contain '-ProgressAction'
    }

    It 'Should complete dynamic parameters with partial input' {
        # See issue: #19498
        try
        {
            Push-Location function:
            $res = TabExpansion2 -inputScript 'Get-ChildItem -LiteralPath $PSHOME -Fi'
            $res.CompletionMatches[1].CompletionText | Should -Be '-File'
        }
        finally
        {
            Pop-Location
        }
    }
    it 'Should complete enum class members for Enums in script text' {
        $res = TabExpansion2 -inputScript 'enum Test1 {Val1};([Test1]"").'
        $res.CompletionMatches.CompletionText[0] | Should -Be 'value__'
        $res.CompletionMatches.CompletionText | Should -Contain 'HasFlag('
    }

    Context "Script name completion" {
        BeforeAll {
            Setup -f 'install-powershell.ps1' -Content ""
            Setup -f 'remove-powershell.ps1' -Content ""

            $scriptWithWildcardCases = @(
                @{
                    command = '.\install-*.ps1'
                    expectedCommand = Join-Path -Path '.' -ChildPath 'install-powershell.ps1'
                    name = "'$(Join-Path -Path '.' -ChildPath 'install-powershell.ps1')'"
                }
                @{
                    command = (Join-Path ${TestDrive}  -ChildPath 'install-*.ps1')
                    expectedCommand = (Join-Path ${TestDrive}  -ChildPath 'install-powershell.ps1')
                    name = "'$(Join-Path -Path '.' -ChildPath 'install-powershell.ps1')' by fully qualified path"
                }
                @{
                    command = '.\?emove-powershell.ps1'
                    expectedCommand = Join-Path -Path '.' -ChildPath 'remove-powershell.ps1'
                    name = "'$(Join-Path -Path '.' -ChildPath '?emove-powershell.ps1')'"
                }
                @{
                    # [] cause the parser to create a new token.
                    # So, the command must be quoted to tab complete.
                    command = "'.\[ra]emove-powershell.ps1'"
                    expectedCommand = "'$(Join-Path -Path '.' -ChildPath 'remove-powershell.ps1')'"
                    name = "'$(Join-Path -Path '.' -ChildPath '[ra]emove-powershell.ps1')'"
                }
            )

            Push-Location ${TestDrive}\
        }

        AfterAll {
            Pop-Location
        }

        It "Input <name> should successfully complete" -TestCases $scriptWithWildcardCases {
            param($command, $expectedCommand)
            $res = TabExpansion2 -inputScript $command -cursorColumn $command.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $expectedCommand
        }
    }

    Context "Script parameter completion" {
        BeforeAll {
            Setup -File -Path 'ModuleReqTest.ps1' -Content @'
#requires -Modules ThisModuleDoesNotExist
param ($Param1)
'@
            Setup -File -Path 'AdminReqTest.ps1' -Content @'
#requires -RunAsAdministrator
param ($Param1)
'@
            Push-Location ${TestDrive}\
        }

        AfterAll {
            Pop-Location
        }

        It "Input should successfully complete script parameter for script with failed script requirements" {
            $res = TabExpansion2 -inputScript '.\ModuleReqTest.ps1 -'
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly '-Param1'
        }

        It "Input should successfully complete script parameter for admin script while not elevated" {
            $res = TabExpansion2 -inputScript '.\AdminReqTest.ps1 -'
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly '-Param1'
        }
    }

    Context "File name completion" {
        BeforeAll {
            $tempDir = Join-Path -Path $TestDrive -ChildPath "baseDir"
            $oneSubDir = Join-Path -Path $tempDir -ChildPath "oneSubDir"
            $oneSubDirPrime = Join-Path -Path $tempDir -ChildPath "prime"
            $twoSubDir = Join-Path -Path $oneSubDir -ChildPath "twoSubDir"
            $caseTestPath = Join-Path $testdrive "CaseTest"

            New-Item -Path $tempDir -ItemType Directory -Force > $null
            New-Item -Path $oneSubDir -ItemType Directory -Force > $null
            New-Item -Path $oneSubDirPrime -ItemType Directory -Force > $null
            New-Item -Path $twoSubDir -ItemType Directory -Force > $null

            $testCases = @(
                @{ inputStr = "ab"; name = "abc"; localExpected = ".${separator}abc"; oneSubExpected = "..${separator}abc"; twoSubExpected = "..${separator}..${separator}abc" }
                @{ inputStr = "asaasas"; name = "asaasas!popee"; localExpected = ".${separator}asaasas!popee"; oneSubExpected = "..${separator}asaasas!popee"; twoSubExpected = "..${separator}..${separator}asaasas!popee" }
                @{ inputStr = "asaasa"; name = "asaasas!popee"; localExpected = ".${separator}asaasas!popee"; oneSubExpected = "..${separator}asaasas!popee"; twoSubExpected = "..${separator}..${separator}asaasas!popee" }
                @{ inputStr = "bbbbbbbbbb"; name = 'bbbbbbbbbb`'; localExpected = "& '.${separator}bbbbbbbbbb``'"; oneSubExpected = "& '..${separator}bbbbbbbbbb``'"; twoSubExpected = "& '..${separator}..${separator}bbbbbbbbbb``'" }
                @{ inputStr = "bbbbbbbbb"; name = "bbbbbbbbb#"; localExpected = ".${separator}bbbbbbbbb#"; oneSubExpected = "..${separator}bbbbbbbbb#"; twoSubExpected = "..${separator}..${separator}bbbbbbbbb#" }
                @{ inputStr = "bbbbbbbb"; name = "bbbbbbbb{"; localExpected = "& '.${separator}bbbbbbbb{'"; oneSubExpected = "& '..${separator}bbbbbbbb{'"; twoSubExpected = "& '..${separator}..${separator}bbbbbbbb{'" }
                @{ inputStr = "bbbbbbb"; name = "bbbbbbb}"; localExpected = "& '.${separator}bbbbbbb}'"; oneSubExpected = "& '..${separator}bbbbbbb}'"; twoSubExpected = "& '..${separator}..${separator}bbbbbbb}'" }
                @{ inputStr = "bbbbbb"; name = "bbbbbb("; localExpected = "& '.${separator}bbbbbb('"; oneSubExpected = "& '..${separator}bbbbbb('"; twoSubExpected = "& '..${separator}..${separator}bbbbbb('" }
                @{ inputStr = "bbbbb"; name = "bbbbb)"; localExpected = "& '.${separator}bbbbb)'"; oneSubExpected = "& '..${separator}bbbbb)'"; twoSubExpected = "& '..${separator}..${separator}bbbbb)'" }
                @{ inputStr = "bbbb"; name = "bbbb$"; localExpected = "& '.${separator}bbbb$'"; oneSubExpected = "& '..${separator}bbbb$'"; twoSubExpected = "& '..${separator}..${separator}bbbb$'" }
                @{ inputStr = "bbb"; name = "bbb'"; localExpected = "& '.${separator}bbb'''"; oneSubExpected = "& '..${separator}bbb'''"; twoSubExpected = "& '..${separator}..${separator}bbb'''" }
                @{ inputStr = "bb"; name = "bb,"; localExpected = "& '.${separator}bb,'"; oneSubExpected = "& '..${separator}bb,'"; twoSubExpected = "& '..${separator}..${separator}bb,'" }
                @{ inputStr = "b"; name = "b;"; localExpected = "& '.${separator}b;'"; oneSubExpected = "& '..${separator}b;'"; twoSubExpected = "& '..${separator}..${separator}b;'" }
            )

            try {
                Push-Location -Path $tempDir
                foreach ($entry in $testCases) {
                    New-Item -Path $tempDir -Name $entry.name -ItemType File -ErrorAction SilentlyContinue > $null
                }
            } finally {
                Pop-Location
            }
        }

        BeforeEach {
            New-Item -ItemType Directory -Path $caseTestPath > $null
        }

        AfterAll {
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        AfterEach {
            Pop-Location
            Remove-Item -Path $caseTestPath -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Input '<inputStr>' should successfully complete" -TestCases $testCases {
            param ($inputStr, $localExpected)

            Push-Location -Path $tempDir
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $localExpected
        }

        It "Input '<inputStr>' should successfully complete with relative path '..\'" -TestCases $testCases {
            param ($inputStr, $oneSubExpected)

            Push-Location -Path $oneSubDir
            $inputStr = "..\${inputStr}"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $oneSubExpected
        }

        It "Input '<inputStr>' should successfully complete with relative path '..\..\'" -TestCases $testCases {
            param ($inputStr, $twoSubExpected)

            Push-Location -Path $twoSubDir
            $inputStr = "../../${inputStr}"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $twoSubExpected
        }

        It "Input '<inputStr>' should successfully complete with relative path '..\..\..\ba*\'" -TestCases $testCases {
            param ($inputStr, $twoSubExpected)

            Push-Location -Path $twoSubDir
            $inputStr = "..\..\..\ba*\${inputStr}"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $twoSubExpected
        }

        It "Test relative path" {
            Push-Location -Path $oneSubDir
            $beforeTab = "twoSubDir/../../pri"
            $afterTab = "..${separator}prime"
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $afterTab
        }

        It "Test path with both '\' and '/'" {
            Push-Location -Path $twoSubDir
            $beforeTab = "..\../..\ba*/ab"
            $afterTab = "..${separator}..${separator}abc"
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $afterTab
        }

        It "Test case insensitive <type> path" -Skip:(!$IsLinux) -TestCases @(
            @{ type = "File"     ; beforeTab = "Get-Content f" },
            @{ type = "Directory"; beforeTab = "cd f" }
        ) {
            param ($type, $beforeTab)

            $testItems = "foo", "Foo", "fOO"
            $testItems | ForEach-Object {
                $itemPath = Join-Path $caseTestPath $_
                New-Item -ItemType $type -Path $itemPath
            }
            Push-Location $caseTestPath
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches | Should -HaveCount $testItems.Count

            # order isn't guaranteed so we'll sort them first
            $completions = ($res.CompletionMatches | Sort-Object CompletionText -CaseSensitive).CompletionText -join ":"
            $expected = ($testItems | Sort-Object -CaseSensitive | ForEach-Object { "./$_" }) -join ":"

            $completions | Should -BeExactly $expected
        }

        It "Test case insensitive file and folder path completing for <type>" -Skip:(!$IsLinux) -TestCases @(
            @{ type = "File"     ; beforeTab = "Get-Content f"; expected = "foo","Foo" },  # Get-Content passes thru to provider
            @{ type = "Directory"; beforeTab = "cd f"         ; expected = "Foo" }  # Set-Location is aware of Files vs Folders
        ) {
            param ($beforeTab, $expected)

            $filePath = Join-Path $caseTestPath "foo"
            $folderPath = Join-Path $caseTestPath "Foo"
            New-Item -ItemType File -Path $filePath
            New-Item -ItemType Directory -Path $folderPath
            Push-Location $caseTestPath
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches | Should -HaveCount $expected.Count

            # order isn't guaranteed so we'll sort them first
            $completions = ($res.CompletionMatches | Sort-Object CompletionText -CaseSensitive).CompletionText -join ":"
            $expected = ($expected | Sort-Object -CaseSensitive | ForEach-Object { "./$_" }) -join ":"

        }

        It "PSScriptRoot path completion when AST extent has file identity" {
            $scriptText = '"$PSScriptRoot\BugFix.Tests"'
            $tokens = $null
            $scriptAst = [System.Management.Automation.Language.Parser]::ParseInput(
                $scriptText,
                $PSCommandPath,
                [ref] $tokens,
                [ref] $null)

            $cursorPosition = $scriptAst.Extent.StartScriptPosition.
                GetType().
                GetMethod('CloneWithNewOffset', [System.Reflection.BindingFlags]'NonPublic, Instance').
                Invoke($scriptAst.Extent.StartScriptPosition, @($scriptText.Length - 1))

            $res = TabExpansion2 -ast $scriptAst -tokens $tokens -positionOfCursor $cursorPosition
            $res.CompletionMatches | Should -HaveCount 1
            $expectedPath = Join-Path $PSScriptRoot -ChildPath BugFix.Tests.ps1
            $res.CompletionMatches[0].CompletionText | Should -Be "`"$expectedPath`""
        }

        It "Relative path completion for using <UsingKind> statement when AST extent has file identity" -TestCases @(
            @{UsingKind = "module";  ExpectedFileName = 'UsingFileCompletionModuleTest.psm1'}
            @{UsingKind = "assembly";ExpectedFileName = 'UsingFileCompletionAssemblyTest.dll'}
        ) -test {
            param($UsingKind, $ExpectedFileName)
            $scriptText = "using $UsingKind .\UsingFileCompletion"
            $tokens = $null
            $scriptAst = [System.Management.Automation.Language.Parser]::ParseInput(
                $scriptText,
                (Join-Path -Path $tempDir -ChildPath ScriptInEditor.ps1),
                [ref] $tokens,
                [ref] $null)

            $cursorPosition = $scriptAst.Extent.StartScriptPosition.
                GetType().
                GetMethod('CloneWithNewOffset', [System.Reflection.BindingFlags]'NonPublic, Instance').
                Invoke($scriptAst.Extent.StartScriptPosition, @($scriptText.Length - 1))

            Push-Location -LiteralPath $PSHOME
            $TestFile = Join-Path -Path $tempDir -ChildPath $ExpectedFileName
            $null = New-Item -Path $TestFile
            $res = TabExpansion2 -ast $scriptAst -tokens $tokens -positionOfCursor $cursorPosition
            Pop-Location
                        
            $ExpectedPath = Join-Path -Path '.\' -ChildPath $ExpectedFileName
            $res.CompletionMatches.CompletionText | Where-Object {$_ -Like "*$ExpectedFileName"} | Should -Be $ExpectedPath
        }

        It "Should handle '~' in completiontext when it's used to refer to home in input" {
            $res = TabExpansion2 -inputScript "~$separator"
            # select the first answer which does not have a space in the completion (those completions look like & '3D Objects')
            $observedResult = $res.CompletionMatches.Where({$_.CompletionText.IndexOf("&") -eq -1})[0].CompletionText
            $completedText = $res.CompletionMatches.CompletionText -join ","
            if ($IsWindows) {
                $observedResult | Should -BeLike "$home$separator*" -Because "$completedText"
            } else {
                $observedResult | Should -BeLike "~$separator*" -Because "$completedText"
            }
        }

        It "Should use '~' as relative filter text when not followed by separator" {
            $TempDirName = "~TempDir"
            $TempDirPath = Join-Path -Path $TestDrive -ChildPath "~TempDir"
            $TempDir = New-Item -Path $TempDirPath -ItemType Directory -Force
            Push-Location -Path $TestDrive
            $res = TabExpansion2 -inputScript ~
            $res.CompletionMatches[0].CompletionText | Should -Be ".${separator}${TempDirName}"
        }

        It 'Escapes backtick properly for path: <LiteralPath>' -TestCases @(
            @{LiteralPath = 'BacktickTest[';   BacktickSingle = 1; BacktickDouble = 2;  LiteralBacktickSingle = 0; LiteralBacktickDouble = 0}
            @{LiteralPath = 'BacktickTest`[';  BacktickSingle = 3; BacktickDouble = 6;  LiteralBacktickSingle = 1; LiteralBacktickDouble = 2}
            @{LiteralPath = 'BacktickTest``['; BacktickSingle = 5; BacktickDouble = 10; LiteralBacktickSingle = 2; LiteralBacktickDouble = 4}
            @{LiteralPath = 'BacktickTest$';   BacktickSingle = 0; BacktickDouble = 1;  LiteralBacktickSingle = 0; LiteralBacktickDouble = 1}
            @{LiteralPath = 'BacktickTest`$';  BacktickSingle = 2; BacktickDouble = 3;  LiteralBacktickSingle = 1; LiteralBacktickDouble = 3}
            @{LiteralPath = 'BacktickTest``$'; BacktickSingle = 4; BacktickDouble = 7;  LiteralBacktickSingle = 2; LiteralBacktickDouble = 5}
        ) {
            param($LiteralPath, $BacktickSingle, $BacktickDouble, $LiteralBacktickSingle, $LiteralBacktickDouble)
            $NewPath = Join-Path -Path $TestDrive -ChildPath $LiteralPath
            $null = New-Item -Path $NewPath -Force
            Push-Location $TestDrive

            $InputText = "Get-ChildItem -Path {0}.${separator}BacktickTest"
            $InputTextLiteral = "Get-ChildItem -LiteralPath {0}.${separator}BacktickTest"

            $Text = (TabExpansion2 -inputScript ($InputText -f "'")).CompletionMatches[0].CompletionText
            $Text.Length - $Text.Replace('`','').Length | Should -Be $BacktickSingle

            $Text = (TabExpansion2 -inputScript ($InputText -f '"')).CompletionMatches[0].CompletionText
            $Text.Length - $Text.Replace('`','').Length | Should -Be $BacktickDouble

            $Text = (TabExpansion2 -inputScript ($InputTextLiteral -f "'")).CompletionMatches[0].CompletionText
            $Text.Length - $Text.Replace('`','').Length | Should -Be $LiteralBacktickSingle

            $Text = (TabExpansion2 -inputScript ($InputTextLiteral -f '"')).CompletionMatches[0].CompletionText
            $Text.Length - $Text.Replace('`','').Length | Should -Be $LiteralBacktickDouble

            Remove-Item -LiteralPath $LiteralPath
        }

        It "Should add single quotes if there are double quotes in bare word file path" {
			$BadQuote = [char]8220
		    $TestFile1 = Join-Path -Path $TestDrive -ChildPath "Test1${BadQuote}File"
            $null = New-Item -Path $TestFile1 -Force
            $res = TabExpansion2 -inputScript "Get-ChildItem -Path $TestDrive\"
            ($res.CompletionMatches | Where-Object ListItemText -Like "Test1?File").CompletionText | Should -Be "'$TestFile1'"
            Remove-Item -LiteralPath $TestFile1 -Force
        }

        It "Should escape double quote if the input string uses double quotes" {
			$BadQuote = [char]8220
		    $TestFile1 = Join-Path -Path $TestDrive -ChildPath "Test1${BadQuote}File"
            $null = New-Item -Path $TestFile1 -Force
            $res = TabExpansion2 -inputScript "Get-ChildItem -Path `"$TestDrive\"
            $Expected = "`"$($TestFile1.Insert($TestFile1.LastIndexOf($BadQuote), '`'))`""
            ($res.CompletionMatches | Where-Object ListItemText -Like "Test1?File").CompletionText | Should -Be $Expected
            Remove-Item -LiteralPath $TestFile1 -Force
        }

        It "Should escape single quotes in file paths" {
			$SingleQuote = "'"
		    $TestFile1 = Join-Path -Path $TestDrive -ChildPath "Test1${SingleQuote}File"
            $null = New-Item -Path $TestFile1 -Force
            # Regardless if the input string was singlequoted or not, we expect to add surrounding single quotes and
            # escape the single quote in the file path with another singlequote.
            $Expected = "'$($TestFile1.Insert($TestFile1.LastIndexOf($SingleQuote), "'"))'"

            $res = TabExpansion2 -inputScript "Get-ChildItem -Path '$TestDrive\"
            ($res.CompletionMatches | Where-Object ListItemText -Like "Test1?File").CompletionText | Should -Be $Expected

            $res = TabExpansion2 -inputScript "Get-ChildItem -Path $TestDrive\"
            ($res.CompletionMatches | Where-Object ListItemText -Like "Test1?File").CompletionText | Should -Be $Expected

            Remove-Item -LiteralPath $TestFile1 -Force
        }
    }

    It 'Should correct slashes in UNC path completion' -Skip:(!$IsWindows) {
        $Res = TabExpansion2 -inputScript 'Get-ChildItem //localhost/c$/Windows'
        $Res.CompletionMatches[0].CompletionText | Should -Be "'\\localhost\c$\Windows'"
    }

    It 'Should keep custom drive names when completing file paths' {
        $TempDriveName = "asdf"
        $null = New-PSDrive -Name $TempDriveName -PSProvider FileSystem -Root $HOME

        $completions = (TabExpansion2 -inputScript "${TempDriveName}:\")
        # select the first answer which does not have a space in the completion (those completions look like & '3D Objects')
        $observedResult = $completions.CompletionMatches.Where({$_.CompletionText.IndexOf("&") -eq -1})[0].CompletionText
        $completedText = $completions.CompletionMatches.CompletionText -join ","

        $observedResult | Should -BeLike "${TempDriveName}:*" -Because "$completionText"
        Remove-PSDrive -Name $TempDriveName
    }

    Context "Cmdlet name completion" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = "get-ch*item"; expected = "Get-ChildItem" }
                @{ inputStr = "set-alia?"; expected = "Set-Alias" }
                @{ inputStr = "s*-alias"; expected = "Set-Alias" }
                @{ inputStr = "se*-alias"; expected = "Set-Alias" }
                @{ inputStr = "set-al"; expected = "Set-Alias" }
                @{ inputStr = "set-a?i"; expected = "Set-Alias" }
                @{ inputStr = "set-?lias"; expected = "Set-Alias" }
                @{ inputStr = "get-c*ditem"; expected = "Get-ChildItem" }
                @{ inputStr = "Microsoft.PowerShell.Management\get-c*item"; expected = "Microsoft.PowerShell.Management\Get-ChildItem" }
                @{ inputStr = "Microsoft.PowerShell.Utility\set-alia?"; expected = "Microsoft.PowerShell.Utility\Set-Alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\s*-alias"; expected = "Microsoft.PowerShell.Utility\Set-Alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\se*-alias"; expected = "Microsoft.PowerShell.Utility\Set-Alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\set-al"; expected = "Microsoft.PowerShell.Utility\Set-Alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\set-a?i"; expected = "Microsoft.PowerShell.Utility\Set-Alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\set-?lias"; expected = "Microsoft.PowerShell.Utility\Set-Alias" }
                @{ inputStr = "Microsoft.PowerShell.Management\get-*ditem"; expected = "Microsoft.PowerShell.Management\Get-ChildItem" }
            )
        }

        It "Input '<inputStr>' should successfully complete" -TestCases $testCases {
            param($inputStr, $expected)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $expected
        }
    }

    Context "Miscellaneous completion tests" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = "get-childitem -"; expected = "-Path"; setup = $null }
                @{ inputStr = "get-childitem -Fil"; expected = "-Filter"; setup = $null }
                @{ inputStr = '$arg'; expected = '$args'; setup = $null }
                @{ inputStr = '$args.'; expected = 'Count'; setup = $null }
                @{ inputStr = '$Host.UI.Ra'; expected = 'RawUI'; setup = $null }
                @{ inputStr = '$Host.UI.WriteD'; expected = 'WriteDebugLine('; setup = $null }
                @{ inputStr = '$MaximumHistoryCount.'; expected = 'CompareTo('; setup = $null }
                @{ inputStr = '$A=[datetime]::now;$A.'; expected = 'Date'; setup = $null }
                @{ inputStr = '$e=$null;try { 1/0 } catch {$e=$_};$e.'; expected = 'CategoryInfo'; setup = $null }
                @{ inputStr = '$x= gps pwsh;$x.*pm'; expected = 'NPM'; setup = $null }
                @{ inputStr = 'function Get-ScrumData {}; Get-Scrum'; expected = 'Get-ScrumData'; setup = $null }
                @{ inputStr = 'function write-output {param($abcd) $abcd};Write-Output -a'; expected = '-abcd'; setup = $null }
                @{ inputStr = 'function write-output {param($abcd) $abcd};Microsoft.PowerShell.Utility\Write-Output -'; expected = '-InputObject'; setup = $null }
                @{ inputStr = '[math]::Co'; expected = 'CopySign('; setup = $null }
                @{ inputStr = '[math]::PI.GetT'; expected = 'GetType('; setup = $null }
                @{ inputStr = '[math]'; expected = '::E'; setup = $null }
                @{ inputStr = '[math].'; expected = 'Assembly'; setup = $null }
                @{ inputStr = '[math].G'; expected = 'GenericParameterAttributes'; setup = $null }
                @{ inputStr = '[Environment+specialfolder]::App'; expected = 'ApplicationData'; setup = $null }
                @{ inputStr = 'icm {get-pro'; expected = 'Get-Process'; setup = $null }
                @{ inputStr = 'write-output (get-pro'; expected = 'Get-Process'; setup = $null }
                @{ inputStr = 'iex "get-pro'; expected = '"Get-Process"'; setup = $null }
                @{ inputStr = '$variab'; expected = '$variableA'; setup = { $variableB = 2; $variableA = 1 } }
                @{ inputStr = 'a -'; expected = '-keys'; setup = { function a {param($keys) $a} } }
                @{ inputStr = 'Get-Content -Li'; expected = '-LiteralPath'; setup = $null }
                @{ inputStr = 'New-Item -W'; expected = '-WhatIf'; setup = $null }
                @{ inputStr = 'Get-Alias gs'; expected = 'gsn'; setup = $null }
                @{ inputStr = 'Get-Alias -Definition cd'; expected = 'cd..'; setup = $null }
                @{ inputStr = 'remove-psdrive fun'; expected = 'Function'; setup = $null }
                @{ inputStr = 'new-psdrive -PSProvider fi'; expected = 'FileSystem'; setup = $null }
                @{ inputStr = 'Get-PSDrive -PSProvider En'; expected = 'Environment'; setup = $null }
                @{ inputStr = 'remove-psdrive fun'; expected = 'Function'; setup = $null }
                @{ inputStr = 'get-psprovider ali'; expected = 'Alias'; setup = $null }
                @{ inputStr = 'Get-PSDrive -PSProvider Variable '; expected = 'Variable'; setup = $null }
                @{ inputStr = 'Get-Command Get-Chil'; expected = 'Get-ChildItem'; setup = $null }
                @{ inputStr = 'Get-Variable psver'; expected = 'PSVersionTable'; setup = $null }
                @{ inputStr = 'Get-Help get-c*ditem'; expected = 'Get-ChildItem'; setup = $null }
                @{ inputStr = 'Trace-Command e'; expected = 'ETS'; setup = $null }
                @{ inputStr = 'Get-TraceSource e'; expected = 'ETS'; setup = $null }
                @{ inputStr = '[int]:: max'; expected = 'MaxValue'; setup = $null }
                @{ inputStr = '"string". l*'; expected = 'Length'; setup = $null }
                @{ inputStr = '("a" * 5).e'; expected = 'EndsWith('; setup = $null }
                @{ inputStr = '([string][int]1).e'; expected = 'EndsWith('; setup = $null }
                @{ inputStr = '(++$i).c'; expected = 'CompareTo('; setup = $null }
                @{ inputStr = '"a".Length.c'; expected = 'CompareTo('; setup = $null }
                @{ inputStr = '@(1, "a").c'; expected = 'Count'; setup = $null }
                @{ inputStr = '{1}.is'; expected = 'IsConfiguration'; setup = $null }
                @{ inputStr = '@{ }.'; expected = 'Count'; setup = $null }
                @{ inputStr = '@{abc=1}.a'; expected = 'Add('; setup = $null }
                @{ inputStr = '$a.f'; expected = "'fo-o'"; setup = { $a = @{'fo-o'='bar'} } }
                @{ inputStr = 'dir | % { $_.Full'; expected = 'FullName'; setup = $null }
                @{ inputStr = '@{a=$(exit)}.Ke'; expected = 'Keys'; setup = $null }
                @{ inputStr = '@{$(exit)=1}.Va'; expected = 'Values'; setup = $null }
                @{ inputStr = 'switch -'; expected = '-CaseSensitive'; setup = $null }
                @{ inputStr = 'gm -t'; expected = '-Type'; setup = $null }
                @{ inputStr = 'foo -aa -aa'; expected = '-aaa'; setup = { function foo {param($a, $aa, $aaa)} } }
                @{ inputStr = 'switch ( gps -'; expected = '-Name'; setup = $null }
                @{ inputStr = 'set-executionpolicy '; expected = 'AllSigned'; setup = $null }
                @{ inputStr = 'Set-ExecutionPolicy -exe: b'; expected = 'Bypass'; setup = $null }
                @{ inputStr = 'Set-ExecutionPolicy -exe:b'; expected = 'Bypass'; setup = $null }
                @{ inputStr = 'Set-ExecutionPolicy -ExecutionPolicy:'; expected = 'AllSigned'; setup = $null }
                @{ inputStr = 'Set-ExecutionPolicy by -for:'; expected = '$true'; setup = $null }
                @{ inputStr = 'Import-Csv -Encoding '; expected = 'ascii'; setup = $null }
                @{ inputStr = 'Get-Process | % ModuleM'; expected = 'ModuleMemorySize'; setup = $null }
                @{ inputStr = 'Get-Process | % {$_.MainModule} | % Com'; expected = 'Company'; setup = $null }
                @{ inputStr = 'Get-Process | % MainModule | % Com'; expected = 'Company'; setup = $null }
                @{ inputStr = '$p = Get-Process; $p | % ModuleM'; expected = 'ModuleMemorySize'; setup = $null }
                @{ inputStr = 'gmo Microsoft.PowerShell.U'; expected = 'Microsoft.PowerShell.Utility'; setup = $null }
                @{ inputStr = 'rmo Microsoft.PowerShell.U'; expected = 'Microsoft.PowerShell.Utility'; setup = $null }
                @{ inputStr = 'gcm -Module Microsoft.PowerShell.U'; expected = 'Microsoft.PowerShell.Utility'; setup = $null }
                @{ inputStr = 'gcm -ExcludeModule Microsoft.PowerShell.U'; expected = 'Microsoft.PowerShell.Utility'; setup = $null }
                @{ inputStr = 'gmo -list PackageM'; expected = 'PackageManagement'; setup = $null }
                @{ inputStr = 'gcm -Module PackageManagement Find-Pac'; expected = 'Find-Package'; setup = $null }
                @{ inputStr = 'ipmo PackageM'; expected = 'PackageManagement'; setup = $null }
                @{ inputStr = 'Get-Process pws'; expected = 'pwsh'; setup = $null }
                @{ inputStr = "function bar { [OutputType('System.IO.FileInfo')][OutputType('System.Diagnostics.Process')]param() }; bar | ? { `$_.ProcessN"; expected = 'ProcessName'; setup = $null }
                @{ inputStr = "function bar { [OutputType('System.IO.FileInfo')][OutputType('System.Diagnostics.Process')]param() }; bar | ? { `$_.LastAc"; expected = 'LastAccessTime'; setup = $null }
                @{ inputStr = "& 'get-comm"; expected = "'Get-Command'"; setup = $null }
                @{ inputStr = 'alias:dir'; expected = Join-Path 'Alias:' 'dir'; setup = $null }
                @{ inputStr = 'gc alias::ipm'; expected = 'alias::ipmo'; setup = $null }
                @{ inputStr = 'gc enVironment::psmod'; expected = 'enVironment::PSModulePath'; setup = $null }
                ## tab completion safe expression evaluator tests
                @{ inputStr = '@{a=$(exit)}.Ke'; expected = 'Keys'; setup = $null }
                @{ inputStr = '@{$(exit)=1}.Ke'; expected = 'Keys'; setup = $null }
                ## tab completion variable names
                @{ inputStr = '@PSVer'; expected = '@PSVersionTable'; setup = $null }
                @{ inputStr = '$global:max'; expected = '$global:MaximumHistoryCount'; setup = $null }
                @{ inputStr = '$PSMod'; expected = '$PSModuleAutoLoadingPreference'; setup = $null }
                ## tab completion for variable in path
                ## if $PSHOME contains a space tabcompletion adds ' around the path
                @{ inputStr = 'cd $PSHOME\Modu'; expected = if($PSHOME.Contains(' ')) { "'$(Join-Path $PSHOME 'Modules')'" } else { Join-Path $PSHOME 'Modules' }; setup = $null }
                @{ inputStr = 'cd "$PSHOME\Modu"'; expected = "`"$(Join-Path $PSHOME 'Modules')`""; setup = $null }
                @{ inputStr = '$PSHOME\System.Management.Au'; expected = if($PSHOME.Contains(' ')) { "`& '$(Join-Path $PSHOME 'System.Management.Automation.dll')'" }  else { Join-Path $PSHOME 'System.Management.Automation.dll'}; Setup = $null }
                @{ inputStr = '"$PSHOME\System.Management.Au"'; expected = "`"$(Join-Path $PSHOME 'System.Management.Automation.dll')`""; setup = $null }
                @{ inputStr = '& "$PSHOME\System.Management.Au"'; expected = "`"$(Join-Path $PSHOME 'System.Management.Automation.dll')`""; setup = $null }
                ## tab completion AST-based tests
                @{ inputStr = 'get-date | ForEach-Object { $PSItem.h'; expected = 'Hour'; setup = $null }
                @{ inputStr = '$a=gps;$a[0].h'; expected = 'Handle'; setup = $null }
                @{ inputStr = "`$(1,'a',@{})[-1].k"; expected = 'Keys'; setup = $null }
                @{ inputStr = "`$(1,'a',@{})[1].tri"; expected = 'Trim('; setup = $null }
                ## tab completion for type names
                @{ inputStr = '[ScriptBlockAst'; expected = 'System.Management.Automation.Language.ScriptBlockAst'; setup = $null }
                @{ inputStr = 'New-Object dict'; expected = 'System.Collections.Generic.Dictionary'; setup = $null }
                @{ inputStr = 'New-Object System.Collections.Generic.List[datet'; expected = "'System.Collections.Generic.List[datetime]'"; setup = $null }
                @{ inputStr = '[System.Management.Automation.Runspaces.runspacef'; expected = 'System.Management.Automation.Runspaces.RunspaceFactory'; setup = $null }
                @{ inputStr = '[specialfol'; expected = 'System.Environment+SpecialFolder'; setup = $null }
                ## tab completion for variable names in '{}'
                @{ inputStr = '${PSDefault'; expected = '${PSDefaultParameterValues}'; setup = $null }
            )
        }

        It "Input '<inputStr>' should successfully complete" -TestCases $testCases {
            param($inputStr, $expected, $setup)

            if ($null -ne $setup) { . $setup }
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches.CompletionText | Should -Contain $expected
        }

        It "Tab completion UNC path" -Skip:(!$IsWindows) {
            $beforeTab = "\\localhost\ADMIN$\boo"
            $afterTab = "& '\\localhost\ADMIN$\Boot'"
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $afterTab
        }

        It "Tab completion UNC path with forward slashes" -Skip:(!$IsWindows) {
            $beforeTab = "//localhost/admin"
            # it is expected that tab completion turns forward slashes into backslashes
            $afterTab = "\\localhost\ADMIN$"
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $afterTab
        }

        It "Tab completion UNC path with filesystem provider" -Skip:(!$IsWindows) {
            $res = TabExpansion2 -inputScript 'Filesystem::\\localhost\admin'
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Filesystem::\\localhost\ADMIN$'
        }

        It "Tab completion for registry" -Skip:(!$IsWindows) {
            $beforeTab = 'registry::HKEY_l'
            $afterTab = 'Registry::HKEY_LOCAL_MACHINE'
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $afterTab
        }

        It "Tab completion for wsman provider" -Skip:(!$IsWindows) {
            $beforeTab = 'wsman::localh'
            $afterTab = 'WSMan::localhost'
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $afterTab
        }

        It "Tab completion for filesystem provider qualified path" {
            $tempFolder = [System.IO.Path]::GetTempPath()
            try
            {
                New-Item -ItemType Directory -Path "$tempFolder/helloworld" > $null
                $tempFolder | Should -Exist
                $beforeTab = 'filesystem::{0}hello' -f $tempFolder
                $afterTab = 'FileSystem::{0}helloworld' -f $tempFolder
                $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
                $res.CompletionMatches.Count | Should -BeGreaterThan 0
                $res.CompletionMatches[0].CompletionText | Should -BeExactly $afterTab
            }
            finally
            {
                Remove-Item -Path "$tempFolder/helloworld" -Force -ErrorAction SilentlyContinue
            }
        }

        It "Tab completion dynamic parameter of a custom function" {
            function Test-DynamicParam {
                [CmdletBinding()]
                PARAM( $DeFirst )

                DYNAMICPARAM {
                    $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                    $attributeCollection = [System.Collections.ObjectModel.Collection[Attribute]]::new()
                    $attributeCollection.Add([Parameter]::new())
                    $deSecond = [System.Management.Automation.RuntimeDefinedParameter]::new('DeSecond', [System.Array], $attributeCollection)
                    $deThird = [System.Management.Automation.RuntimeDefinedParameter]::new('DeThird', [System.Array], $attributeCollection)
                    $null = $paramDictionary.Add('DeSecond', $deSecond)
                    $null = $paramDictionary.Add('DeThird', $deThird)
                    return $paramDictionary
                }

                PROCESS {
                    Write-Host 'Hello'
                    Write-Host $PSBoundParameters
                }
            }

            $inputStr = "Test-DynamicParam -D"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 3
            $res.CompletionMatches[0].CompletionText | Should -BeExactly '-DeFirst'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly '-DeSecond'
            $res.CompletionMatches[2].CompletionText | Should -BeExactly '-DeThird'
        }

        It "Tab completion dynamic parameter '-CodeSigningCert'" -Skip:(!$IsWindows) {
            try {
                Push-Location cert:\
                $inputStr = "gci -co"
                $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
                $res.CompletionMatches[0].CompletionText | Should -BeExactly '-CodeSigningCert'
            } finally {
                Pop-Location
            }
        }

        It "Tab completion for file system takes precedence over functions" {
            try {
                Push-Location $TestDrive
                New-Item -Name myf -ItemType File -Force
                function MyFunction { "Hi there" }

                $inputStr = "myf"
                $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
                $res.CompletionMatches | Should -HaveCount 2
                $res.CompletionMatches[0].CompletionText | Should -BeExactly (Resolve-Path myf -Relative)
                $res.CompletionMatches[1].CompletionText | Should -BeExactly "MyFunction"
            } finally {
                Remove-Item -Path myf -Force
                Pop-Location
            }
        }

        It "Tab completion for validateSet attribute" {
            function foo { param([ValidateSet('cat','dog')]$p) }
            $inputStr = "foo "
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 2
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'cat'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly 'dog'
        }

        It "Tab completion for validateSet attribute takes precedence over enums" {
            function foo { param([ValidateSet('DarkBlue','DarkCyan')][ConsoleColor]$p) }
            $inputStr = "foo "
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 2
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'DarkBlue'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly 'DarkCyan'
        }

        It "Tab completion for attribute type" {
            $inputStr = '[validateset()]$var1'
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn 2
            $res.CompletionMatches.CompletionText | Should -Contain 'ValidateSet'
        }

        It "Tab completion for ArgumentCompleter when AST is passed to CompleteInput" {
            $scriptBl = {
                function Test-Completion {
                    param (
                        [String]$TestVal
                    )
                }
                [scriptblock]$completer = {
                    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

                    @('Val1', 'Val2')
                }
                Register-ArgumentCompleter -CommandName Test-Completion -ParameterName TestVal -ScriptBlock $completer
            }
            $pwsh = [PowerShell]::Create()
            $pwsh.AddScript($scriptBl)
            $pwsh.Invoke()

            $completeInput_Input = $scriptBl.ToString()
            $completeInput_Input += "`nTest-Completion -TestVal "
            $res = [System.Management.Automation.CommandCompletion]::CompleteInput($completeInput_Input, $completeInput_Input.Length, $null, $pwsh)
            $res.CompletionMatches | Should -HaveCount 2
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Val1'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly 'Val2'
        }

        It "Tab completion for enum type parameter of a custom function" {
            function baz ([consolecolor]$name, [ValidateSet('cat','dog')]$p){}
            $inputStr = "baz -name "
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 16
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Black'

            $inputStr = "baz Black "
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 2
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'cat'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly 'dog'
        }

        It "Tab completion for enum members after colon with <Space> space" -TestCases @(
            @{ Space = 0 }
            @{ Space = 1 }
        ) {
            param ($Space)
            $inputStr = "Get-Command -Type:$(' ' * $Space)Al"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 2
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Alias'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly 'All'
        }

        It "Tab completion for enum members between colon with <LeftSpace> space and <RightSpace> space with value" -TestCases @(
            @{ LeftSpace = 0; RightSpace = 0 }
            @{ LeftSpace = 0; RightSpace = 1 }
            @{ LeftSpace = 1; RightSpace = 0 }
            @{ LeftSpace = 1; RightSpace = 1 }
        ) {
            param ($LeftSpace, $RightSpace)
            $inputStrEndsWithCursor = "Get-Command -Type:$(' ' * $LeftSpace)"
            $inputStr = $inputStrEndsWithCursor + "$(' ' * $RightSpace)Alias"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStrEndsWithCursor.Length
            $expectedArray = [enum]::GetNames([System.Management.Automation.CommandTypes]) | Sort-Object
            $res.CompletionMatches.CompletionText | Should -Be $expectedArray
        }

        It "Tab completion for enum members between comma with <LeftSpace> space and <RightSpace> space with parameter" -TestCases @(
            @{ LeftSpace = 0; RightSpace = 0 }
            @{ LeftSpace = 0; RightSpace = 1 }
            @{ LeftSpace = 1; RightSpace = 0 }
            @{ LeftSpace = 1; RightSpace = 1 }
        ) {
            param ($LeftSpace, $RightSpace)
            $inputStrEndsWithCursor = "Get-Command -Type Alias,$(' ' * $LeftSpace)"
            $inputStr = $inputStrEndsWithCursor + "$(' ' * $RightSpace)-All"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStrEndsWithCursor.Length
            $expectedArray = [enum]::GetNames([System.Management.Automation.CommandTypes]) | Sort-Object
            $res.CompletionMatches.CompletionText | Should -Be $expectedArray
        }

        It "Tab completion for enum members after comma" {
            $inputStr = "Get-Command -Type Alias,c"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 2
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Cmdlet'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly 'Configuration'
        }

        It 'Tab completion for enum parameter is filtered against <Name>' -TestCases @(
            @{ Name = 'ValidateRange with enum-values'; Attribute = '[ValidateRange([System.ConsoleColor]::Blue, [System.ConsoleColor]::Cyan)]' }
            @{ Name = 'ValidateRange with int-values'; Attribute = '[ValidateRange(9, 11)]' }
            @{ Name = 'multiple ValidateRange-attributes'; Attribute = '[ValidateRange([System.ConsoleColor]::Blue, [System.ConsoleColor]::Cyan)][ValidateRange([System.ConsoleColor]::Gray, [System.ConsoleColor]::Red)]' }
        ) {
            param($Name, $Attribute)
            $functionDefinition = 'param ( {0}[consolecolor]$color )' -f $Attribute
            Set-Item -Path function:baz -Value $functionDefinition
            $inputStr = 'baz -color '
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 3
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Blue'
            $res.CompletionMatches[1].CompletionText | Should -BeExactly 'Cyan'
            $res.CompletionMatches[2].CompletionText | Should -BeExactly 'Green'
        }

        It 'Tab completion for enum parameter is filtered with ValidateRange using rangekind' {
            $functionDefinition = 'param ( [ValidateRange([System.Management.Automation.ValidateRangeKind]::NonPositive)][consolecolor]$color )' -f $Attribute
            Set-Item -Path function:baz -Value $functionDefinition
            $inputStr = 'baz -color '
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly 'Black' # 0 = NonPositive
        }

        It 'Tab completion of $_ inside incomplete switch condition' {
            $res = TabExpansion2 -inputScript 'Get-PSDrive | Sort-Object -Property {switch ($_.nam'
            $res.CompletionMatches[0].CompletionText | Should -Be 'Name'
        }

        It "Test [CommandCompletion]::GetNextResult" {
            $inputStr = "Get-Command -Type Alias,c"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 2
            $res.GetNextResult($false).CompletionText | Should -BeExactly 'Configuration'
            $res.GetNextResult($true).CompletionText | Should -BeExactly 'Cmdlet'
            $res.GetNextResult($true).CompletionText | Should -BeExactly 'Configuration'
        }

        It "Test history completion" {
            $startDate = Get-Date
            $endDate = $startDate.AddSeconds(1)
            $history = [pscustomobject]@{
                CommandLine = "Test history completion"
                ExecutionStatus = "Stopped"
                StartExecutionTime = $startDate
                EndExecutionTime = $endDate
            }
            Add-History -InputObject $history
            $res = TabExpansion2 -inputScript "#" -cursorColumn 1
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "Test history completion"
        }

        It "Test #requires parameter completion" {
            $res = TabExpansion2 -inputScript "#requires -" -cursorColumn 11
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "Modules"
        }

        It "Test #requires parameter value completion" {
            $res = TabExpansion2 -inputScript "#requires -PSEdition " -cursorColumn 21
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "Core"
        }

        It "Test no completion after #requires -RunAsAdministrator" {
            $res = TabExpansion2 -inputScript "#requires -RunAsAdministrator -" -cursorColumn 31
            $res.CompletionMatches | Should -HaveCount 0
        }

        It "Test no suggestions for already existing parameters in #requires" {
            $res = TabExpansion2 -inputScript "#requires -Modules -" -cursorColumn 20
            $res.CompletionMatches.CompletionText | Should -Not -Contain "Modules"
        }

        It "Test module completion in #requires without quotes" {
            $res = TabExpansion2 -inputScript "#requires -Modules P" -cursorColumn 20
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches.CompletionText | Should -Contain "Pester"
        }

        It "Test module completion in #requires with quotes" {
            $res = TabExpansion2 -inputScript '#requires -Modules "' -cursorColumn 20
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches.CompletionText | Should -Contain "Pester"
        }

        It "Test module completion in #requires with multiple modules" {
            $res = TabExpansion2 -inputScript "#requires -Modules Pester," -cursorColumn 26
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches.CompletionText | Should -Contain "Pester"
        }

        It "Test hashtable key completion in #requires statement for modules" {
            $res = TabExpansion2 -inputScript "#requires -Modules @{"
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "GUID"
        }

        It "Test no suggestions for already existing hashtable keys in #requires statement for modules" {
            $res = TabExpansion2 -inputScript '#requires -Modules @{ModuleName="Pester";' -cursorColumn 41
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches.CompletionText | Should -Not -Contain "ModuleName"
        }

        It "Test no suggestions for mutually exclusive hashtable keys in #requires statement for modules" {
            $res = TabExpansion2 -inputScript '#requires -Modules @{ModuleName="Pester";RequiredVersion="1.0";' -cursorColumn 63
            $res.CompletionMatches.CompletionText | Should -BeExactly "GUID"
        }

        It "Test no suggestions for RequiredVersion key in #requires statement when ModuleVersion is specified" {
            $res = TabExpansion2 -inputScript '#requires -Modules @{ModuleName="Pester";ModuleVersion="1.0";' -cursorColumn 61
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches.CompletionText | Should -Not -Contain "RequiredVersion"
        }

        It "Test module completion in #requires statement for hashtables" {
            $res = TabExpansion2 -inputScript '#requires -Modules @{ModuleName="p' -cursorColumn 34
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches.CompletionText | Should -Contain "Pester"
        }

        It "Test Attribute member completion" {
            $inputStr = "function bar { [parameter(]param() }"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn ($inputStr.IndexOf('(') + 1)
            $res.CompletionMatches | Should -HaveCount 10
            $entry = $res.CompletionMatches | Where-Object CompletionText -EQ "Position"
            $entry.CompletionText | Should -BeExactly "Position"
        }

        It "Test Attribute member completion multiple members" {
            $inputStr = "function bar { [parameter(Position,]param() }"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn ($inputStr.IndexOf(',') + 1)
            $res.CompletionMatches | Should -HaveCount 9
            $entry = $res.CompletionMatches | Where-Object CompletionText -EQ "Mandatory"
            $entry.CompletionText | Should -BeExactly "Mandatory"
        }

        It "Should complete member in attribute argument value" {
            $inputStr = '[ValidateRange(1,[int]::Maxva^)]$a'
            $CursorIndex = $inputStr.IndexOf('^')
            $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $inputStr.Remove($CursorIndex, 1)
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "MaxValue"
        }

        It "Test Attribute scriptblock completion" {
            $inputStr = '[ValidateScript({Get-Child})]$Test=ls'
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn ($inputStr.IndexOf('}'))
            $res.CompletionMatches | Should -HaveCount 1
            $entry = $res.CompletionMatches | Where-Object CompletionText -EQ "Get-ChildItem"
            $entry.CompletionText | Should -BeExactly "Get-ChildItem"
        }

        It '<Intent>' -TestCases @(
            @{
                Intent = 'Complete attribute members on empty line'
                Expected = @('Position','ParameterSetName','Mandatory','ValueFromPipeline','ValueFromPipelineByPropertyName','ValueFromRemainingArguments','HelpMessage','HelpMessageBaseName','HelpMessageResourceId','DontShow')
                TestString = @'
function bar { [parameter(


^

        )]param() }
'@
            }
            @{
                Intent = 'Complete attribute members on empty line with preceding member'
                Expected = @('Position','ParameterSetName','Mandatory','ValueFromPipeline','ValueFromPipelineByPropertyName','ValueFromRemainingArguments','HelpMessage','HelpMessageBaseName','HelpMessageResourceId','DontShow')
                TestString = @'
function bar { [parameter(
Mandatory,

^

        )]param() }
'@
            }
        ){
            param($Expected, $TestString)
            $CursorIndex = $TestString.IndexOf('^')
            $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
            $res.CompletionMatches[0].CompletionText | Should -BeIn $Expected
        }

        It "Test completion with line continuation" {
            $inputStr = @'
dir -Recurse `
-Lite
'@
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "-LiteralPath"
        }

        It "Test member completion of a static method invocation" {
            $inputStr = '[powershell]::Create().'
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 33
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "Commands"
        }

        It "Test completion with common parameters" {
            $inputStr = 'invoke-webrequest -out'
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 3
            [string]::Join(',', ($res.CompletionMatches.completiontext | Sort-Object)) | Should -BeExactly "-OutBuffer,-OutFile,-OutVariable"
        }

        It "Test completion with exact match" {
            $inputStr = 'get-content -wa'
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 3
            [string]::Join(',', ($res.CompletionMatches.completiontext | Sort-Object)) | Should -BeExactly "-Wait,-WarningAction,-WarningVariable"
        }

        It "Test completion with splatted variable" {
            $inputStr = 'Get-Content @Splat -P'
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 4
            [string]::Join(',', ($res.CompletionMatches.completiontext | Sort-Object)) | Should -BeExactly "-Path,-PipelineVariable,-ProgressAction,-PSPath"
        }

        It "Test completion for HttpVersion parameter name" {
            $inputStr = 'Invoke-WebRequest -HttpV'
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "-HttpVersion"
        }

        It "Test completion for HttpVersion parameter" {
            $inputStr = 'Invoke-WebRequest -HttpVersion '
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 4
            [string]::Join(',', ($res.CompletionMatches.completiontext | Sort-Object)) | Should -BeExactly "1.0,1.1,2.0,3.0"
        }

        It "Test completion for HttpVersion parameter with input" {
            $inputStr = 'Invoke-WebRequest -HttpVersion 1'
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 2
            [string]::Join(',', ($res.CompletionMatches.completiontext | Sort-Object)) | Should -BeExactly "1.0,1.1"
        }

        It 'Should complete Select-Object properties without duplicates' {
            $res = TabExpansion2 -inputScript '$PSVersionTable | Select-Object -Property Count,'
            $res.CompletionMatches.CompletionText | Should -Not -Contain "Count"
        }

        It '<Intent>' -TestCases @(
            @{
                Intent = 'Complete loop labels with no input'
                Expected = 'Outer','Inner'
                TestString = ':Outer while ($true){:Inner while ($true){ break ^ }}'
            }
            @{
                Intent = 'Complete loop labels that are accessible'
                Expected = 'Outer'
                TestString = ':Outer do {:Inner while ($true){ break } continue ^ } until ($false)'
            }
            @{
                Intent = 'Complete loop labels with partial input'
                Expected = 'Outer'
                TestString = ':Outer do {:Inner while ($true){ break } continue o^ut } while ($true)'
            }
            @{
                Intent = 'Complete loop label for incomplete switch'
                Expected = 'Outer'
                TestString = ':Outer switch ($x){"randomValue"{ continue ^'
            }
            @{
                Intent = 'Complete loop label for incomplete do loop'
                Expected = 'Outer'
                TestString = ':Outer do {:Inner while ($true){ break } continue ^'
            }
            @{
                Intent = 'Complete loop label for incomplete for loop'
                Expected = 'forLoop'
                TestString = ':forLoop for ($i = 0; $i -lt $SomeCollection.Count; $i++) {continue ^'
            }
            @{
                Intent = 'Complete loop label for incomplete while loop'
                Expected = 'WhileLoop'
                TestString = ':WhileLoop while ($true){ break ^'
            }
            @{
                Intent = 'Complete loop label for incomplete foreach loop'
                Expected = 'foreachLoop'
                TestString = ':foreachLoop foreach ($x in $y) { break ^'
            }
            @{
                Intent = 'Not Complete loop labels with colon'
                Expected = $null
                TestString = ':Outer foreach ($x in $y){:Inner for ($i = 0; $i -lt $X.Count; $i++){ break :O^}}'
            }
            @{
                Intent = 'Not Complete loop labels if cursor is in front of existing label'
                Expected = $null
                TestString = ':Outer switch ($x){"Value1"{break ^ Outer}}'
            }
        ){
            param($Expected, $TestString)
            $CursorIndex = $TestString.IndexOf('^')
            $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
            $res.CompletionMatches.CompletionText | Should -BeExactly $Expected
        }
    }

    Context "Module completion for 'using module'" {
        BeforeAll {
            $tempDir = Join-Path -Path $TestDrive -ChildPath "UsingModule"
            New-Item -Path $tempDir -ItemType Directory -Force > $null
            New-Item -Path "$tempDir\testModule.psm1" -ItemType File -Force > $null

            Push-Location -Path $tempDir
        }

        AfterAll {
            Pop-Location
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Test complete module file name" {
            $inputStr = "using module testm"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly ".${separator}testModule.psm1"
        }

        It "Test complete module name" {
            $inputStr = "using module PSRead"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "PSReadLine"
        }

        It "Test complete module name with wildcard" {
            $inputStr = "using module *ReadLi"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "PSReadLine"
        }
    }

    Context "Completion on 'comma', 'redirection' and 'minus' tokens" {
        BeforeAll {
            $tempDir = Join-Path -Path $TestDrive -ChildPath "CommaTest"
            New-Item -Path $tempDir -ItemType Directory -Force > $null
            New-Item -Path "$tempDir\commaA.txt" -ItemType File -Force > $null

            $redirectionTestCases = @(
                @{ inputStr = "gps >";  expected = ".${separator}commaA.txt" }
                @{ inputStr = "gps >>"; expected = ".${separator}commaA.txt" }
                @{ inputStr = "dir con 2>";  expected = ".${separator}commaA.txt" }
                @{ inputStr = "dir con 2>>"; expected = ".${separator}commaA.txt" }
                @{ inputStr = "gps 2>&1>";   expected = ".${separator}commaA.txt" }
                @{ inputStr = "gps 2>&1>>";  expected = ".${separator}commaA.txt" }
            )

            Push-Location -Path $tempDir
        }

        AfterAll {
            Pop-Location
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Test comma with file array element" {
            $inputStr = "dir .\commaA.txt,"
            $expected = ".${separator}commaA.txt"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $expected
        }

        It "Tab completion for file array element between comma with <LeftSpace> space and <RightSpace> space with parameter" -TestCases @(
            @{ LeftSpace = 0; RightSpace = 0 }
            @{ LeftSpace = 0; RightSpace = 1 }
            @{ LeftSpace = 1; RightSpace = 0 }
            @{ LeftSpace = 1; RightSpace = 1 }
        ) {
            param ($LeftSpace, $RightSpace)
            $inputStrEndsWithCursor = "dir .\commaA.txt,$(' ' * $LeftSpace)"
            $inputStr = $inputStrEndsWithCursor + "$(' ' * $RightSpace)-File"
            $expected = ".${separator}commaA.txt"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStrEndsWithCursor.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $expected
        }

        It "Test comma with Enum array element" {
            $inputStr = "gcm -CommandType Cmdlet,"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount ([System.Enum]::GetNames([System.Management.Automation.CommandTypes]).Count)
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "Alias"
        }

        It "Test redirection operator '<inputStr>'" -TestCases $redirectionTestCases {
            param($inputStr, $expected)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $expected
        }

        It "Test complete the minus token to operators" {
            $inputStr = "55 -"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount ([System.Management.Automation.CompletionCompleters]::CompleteOperator("").Count)
            $res.CompletionMatches[0].CompletionText | Should -BeExactly '-and'
        }
    }

    Context "Folder/File path tab completion with special characters" {
        BeforeAll {
            $tempDir = Join-Path -Path $TestDrive -ChildPath "SpecialChar"
            New-Item -Path $tempDir -ItemType Directory -Force > $null

            New-Item -Path "$tempDir\My [Path]" -ItemType Directory -Force > $null
            New-Item -Path "$tempDir\My [Path]\test.ps1" -ItemType File -Force > $null
            New-Item -Path "$tempDir\)file.txt" -ItemType File -Force > $null

            $testCases = @(
                @{ inputStr = "cd My"; expected = "'.${separator}My ``[Path``]'" }
                @{ inputStr = "Get-Help '.\My ``[Path``]'\"; expected = "'.${separator}My ``[Path``]${separator}test.ps1'" }
                @{ inputStr = "Get-Process >My"; expected = "'.${separator}My ``[Path``]'" }
                @{ inputStr = "Get-Process >'.\My ``[Path``]\'"; expected = "'.${separator}My ``[Path``]${separator}test.ps1'" }
                @{ inputStr = "Get-Process >${tempDir}\My"; expected = "'${tempDir}${separator}My ``[Path``]'" }
                @{ inputStr = "Get-Process > '${tempDir}\My ``[Path``]\'"; expected = "'${tempDir}${separator}My ``[Path``]${separator}test.ps1'" }
            )

            Push-Location -Path $tempDir
        }

        AfterAll {
            Pop-Location
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        It "Complete special relative path '<inputStr>'" -TestCases $testCases {
            param($inputStr, $expected)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $expected
        }

        It "Complete file name starting with special char" {
            $inputStr = ")"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -HaveCount 1
            $res.CompletionMatches[0].CompletionText | Should -BeExactly "& '.${separator})file.txt'"
        }
    }

    Context "Local tab completion with AST" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = '$p = Get-Process; $p | % ProcessN '; bareWord = 'ProcessN'; expected = 'ProcessName' }
                @{ inputStr = 'function bar { Get-Ali* }'; bareWord = 'Get-Ali*'; expected = 'Get-Alias' }
                @{ inputStr = 'function baz ([string]$version, [consolecolor]$name){} baz version bl'; bareWord = 'bl'; expected = 'Black' }
            )
        }

        It "Input '<inputStr>' should successfully complete via AST" -TestCases $testCases {
            param($inputStr, $bareWord, $expected)

            $tokens = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput($inputStr, [ref] $tokens, [ref]$null)
            $elementAst = $ast.Find(
                { $args[0] -is [System.Management.Automation.Language.StringConstantExpressionAst] -and $args[0].Value -eq $bareWord },
                $true
            )

            $res = TabExpansion2 -ast $ast -tokens $tokens -positionOfCursor $elementAst.Extent.EndScriptPosition
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $expected
        }
    }

    Context "No tab completion tests" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = 'function new-' }
                @{ inputStr = 'filter new-' }
                @{ inputStr = '@pid.' }
            )
        }

        It "Input '<inputStr>' should not complete to anything" -TestCases $testCases {
            param($inputStr)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -BeNullOrEmpty
        }

        It "A single dash should not complete to anything" {
            function test-{}
            $inputStr = 'git -'
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches | Should -BeNullOrEmpty
        }
    }

    Context "Tab completion error tests" {
        BeforeAll {
            $ast = {}.Ast;
            $tokens = [System.Management.Automation.Language.Token[]]@()
            $testCases = @(
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::MapStringInputToParsedInput('$PID.', 7)}; expected = "PSArgumentException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($null, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput('$PID.', 7, $null, $null)}; expected = "PSArgumentException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput('$PID.', 5, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($null, $null, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $null, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $ast.Extent.EndScriptPosition, $null, $null)}; expected = "PSArgumentNullException" }
            )
        }

        It "Input '<inputStr>' should throw in tab completion" -TestCases $testCases {
            param($inputStr, $expected)
            $inputStr | Should -Throw -ErrorId $expected
        }

        It "Should not throw errors in tab completion with empty input string" {
            {[System.Management.Automation.CommandCompletion]::CompleteInput("", 0, $null)} | Should -Not -Throw
        }

        It "Should not throw errors in tab completion with empty input ast" {
            {[System.Management.Automation.CommandCompletion]::CompleteInput({}.Ast, @(), {}.Ast.Extent.StartScriptPosition, $null)} | Should -Not -Throw
        }
    }

    Context "DSC tab completion tests" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = 'Configura'; expected = 'Configuration' }
                @{ inputStr = '$extension = New-Object [System.Collections.Generic.List[string]]; $extension.wh'; expected = "Where(" }
                @{ inputStr = '$extension = New-Object [System.Collections.Generic.List[string]]; $extension.fo'; expected = 'ForEach(' }
                @{ inputStr = 'Configuration foo { node $SelectedNodes.'; expected = 'Where(' }
                @{ inputStr = 'Configuration foo { node $SelectedNodes.fo'; expected = 'ForEach(' }
                @{ inputStr = 'Configuration foo { node $AllNodes.'; expected = 'Where(' }
                @{ inputStr = 'Configuration foo { node $ConfigurationData.AllNodes.'; expected = 'Where(' }
                @{ inputStr = 'Configuration foo { node $ConfigurationData.AllNodes.fo'; expected = 'ForEach(' }
                @{ inputStr = 'Configuration bar { File foo { Destinat'; expected = 'DestinationPath = ' }
                @{ inputStr = 'Configuration bar { File foo { Content'; expected = 'Contents = ' }
                @{ inputStr = 'Configuration bar { Fil'; expected = 'File' }
                @{ inputStr = 'Configuration bar { Import-Dsc'; expected = 'Import-DscResource' }
                @{ inputStr = 'Configuration bar { Import-DscResource -Modu'; expected = '-ModuleName' }
                @{ inputStr = 'Configuration bar { Import-DscResource -ModuleName blah -Modu'; expected = '-ModuleVersion' }
                @{ inputStr = 'Configuration bar { Scri'; expected = 'Script' }
                @{ inputStr = 'configuration foo { Script ab {Get'; expected = 'GetScript = ' }
                @{ inputStr = 'configuration foo { Script ab { '; expected = 'DependsOn = ' }
                @{ inputStr = 'configuration foo { File ab { Attributes ='; expected = "'Archive'" }
                @{ inputStr = "configuration foo { File ab { Attributes = "; expected = "'Archive'" }
                @{ inputStr = "configuration foo { File ab { Attributes = ar"; expected = "Archive" }
                @{ inputStr = "configuration foo { File ab { Attributes = 'ar"; expected = "Archive" }
                @{ inputStr = 'configuration foo { File ab { Attributes =('; expected = "'Archive'" }
                @{ inputStr = 'configuration foo { File ab { Attributes =( '; expected = "'Archive'" }
                @{ inputStr = "configuration foo { File ab { Attributes =('Archive',"; expected = "'Hidden'" }
                @{ inputStr = "configuration foo { File ab { Attributes =('Archive', "; expected = "'Hidden'" }
                @{ inputStr = "configuration foo { File ab { Attributes =('Archive', 'Hi"; expected = "Hidden" }
            )
        }

        It "Input '<inputStr>' should successfully complete" -TestCases $testCases -Skip:(!$IsWindows) {
            param($inputStr, $expected)

            if (Test-IsWindowsArm64) {
                Set-ItResult -Pending -Because "TBD"
            }


            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $expected
        }
    }

    Context "CIM cmdlet completion tests" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = "Invoke-CimMethod -ClassName Win32_Process -MethodName Crea"; expected = "Create" }
                @{ inputStr = "Get-CimInstance -ClassName Win32_Process | Invoke-CimMethod -MethodName AttachDeb"; expected = "AttachDebugger" }
                @{ inputStr = 'Get-CimInstance Win32_Process | ?{ $_.ProcessId -eq $PID } | Get-CimAssociatedInstance -ResultClassName Win32_Co*uterSyst'; expected = "Win32_ComputerSystem" }
                @{ inputStr = "Get-CimInstance -ClassName Win32_Environm"; expected = "Win32_Environment" }
                @{ inputStr = "New-CimInstance -ClassName Win32_Environm"; expected = "Win32_Environment" }
                @{ inputStr = 'New-CimInstance -ClassName Win32_Process | %{ $_.Captio'; expected = "Caption" }
                @{ inputStr = "Invoke-CimMethod -ClassName Win32_Environm"; expected = 'Win32_Environment' }
                @{ inputStr = "Get-CimClass -ClassName Win32_Environm"; expected = 'Win32_Environment' }
                @{ inputStr = 'Get-CimInstance -ClassName Win32_Process | Invoke-CimMethod -MethodName SetPriorit'; expected = 'SetPriority' }
                @{ inputStr = 'Invoke-CimMethod -Namespace root/StandardCimv2 -ClassName MSFT_NetIPAddress -MethodName Crea'; expected = 'Create' }
                @{ inputStr = '$win32_process = Get-CimInstance -ClassName Win32_Process; $win32_process | Invoke-CimMethod -MethodName AttachDe'; expected = 'AttachDebugger' }
                @{ inputStr = '$win32_process = Get-CimInstance -ClassName Win32_Process; Invoke-CimMethod -InputObject $win32_process -MethodName AttachDe'; expected = 'AttachDebugger' }
                @{ inputStr = 'Get-CimInstance Win32_Process | ?{ $_.ProcessId -eq $PID } | Get-CimAssociatedInstance -ResultClassName Win32_ComputerS'; expected = 'Win32_ComputerSystem' }
                @{ inputStr = 'Get-CimInstance -Namespace root/Interop -ClassName Win32_PowerSupplyP'; expected = 'Win32_PowerSupplyProfile' }
                @{ inputStr = 'Get-CimInstance __NAMESP'; expected = '__NAMESPACE' }
                @{ inputStr = 'Get-CimInstance -Namespace root/Inter'; expected = 'root/Interop' }
                @{ inputStr = 'Get-CimInstance -Namespace root/Int*ro'; expected = 'root/Interop' }
                @{ inputStr = 'Get-CimInstance -Namespace root/Interop/'; expected = 'root/Interop/ms_409' }
                @{ inputStr = 'New-CimInstance -Namespace root/Inter'; expected = 'root/Interop' }
                @{ inputStr = 'Invoke-CimMethod -Namespace root/Inter'; expected = 'root/Interop' }
                @{ inputStr = 'Get-CimClass -Namespace root/Inter'; expected = 'root/Interop' }
                @{ inputStr = 'Register-CimIndicationEvent -Namespace root/Inter'; expected = 'root/Interop' }
                @{ inputStr = '[Microsoft.Management.Infrastructure.CimClass]$c = $null; $c.CimClassNam'; expected = 'CimClassName' }
                @{ inputStr = '[Microsoft.Management.Infrastructure.CimClass]$c = $null; $c.CimClassName.Substrin'; expected = 'Substring(' }
                @{ inputStr = 'Get-CimInstance -ClassName Win32_Process | %{ $_.ExecutableP'; expected = 'ExecutablePath' }
                @{ inputStr = 'Get-CimInstance -ClassName Win32_Process | Invoke-CimMethod -MethodName SetPriority -Arguments @{'; expected = 'Priority' }
                @{ inputStr = 'Get-CimInstance -ClassName Win32_Service | Invoke-CimMethod -MethodName Change -Arguments @{d'; expected = 'DesktopInteract' }
                @{ inputStr = 'Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{'; expected = 'CommandLine' }
                @{ inputStr = 'New-CimInstance Win32_Environment -Property @{'; expected = 'Caption' }
                @{ inputStr = 'Get-CimInstance Win32_Environment | Set-CimInstance -Property @{'; expected = 'Name' }
                @{ inputStr = 'Set-CimInstance -Namespace root/CIMV'; expected = 'root/CIMV2' }
                @{ inputStr = 'Get-CimInstance Win32_Process -Property '; expected = 'Caption' }
                @{ inputStr = 'Get-CimInstance Win32_Process -Property Caption,'; expected = 'Description' }
            )
            $FailCases = @(
                @{ inputStr = "Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments " }
                @{ inputStr = "New-CimInstance Win32_Process -Property " }
            )
        }

        It "CIM cmdlet input '<inputStr>' should successfully complete" -TestCases $testCases -Skip:(!$IsWindows) {
            param($inputStr, $expected)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should -BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should -Be $expected
        }

        It "CIM cmdlet input '<inputStr>' should not successfully complete" -TestCases $FailCases -Skip:(!$IsWindows) {
            param($inputStr)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches[0].ResultType | should -Not -Be 'Property'
        }
    }

    Context "Module cmdlet completion tests" {
        It "ArugmentCompleter for PSEdition should work for '<cmd>'" -TestCases @(
            @{cmd = "Get-Module -PSEdition "; expected = "Desktop", "Core"}
            @{cmd = "Get-Module -PSEdition '"; expected = "'Desktop'", "'Core'"}
            @{cmd = "Get-Module -PSEdition """; expected = """Desktop""", """Core"""}
            @{cmd = "Get-Module -PSEdition 'Desk"; expected = "'Desktop'"}
            @{cmd = "Get-Module -PSEdition ""Desk"; expected = """Desktop"""}
            @{cmd = "Get-Module -PSEdition Co"; expected = "Core"}
            @{cmd = "Get-Module -PSEdition 'Co"; expected = "'Core'"}
            @{cmd = "Get-Module -PSEdition ""Co"; expected = """Core"""}
        ) {
            param($cmd, $expected)
            $res = TabExpansion2 -inputScript $cmd -cursorColumn $cmd.Length
            $completionText = $res.CompletionMatches.CompletionText
            $completionText -join ' ' | Should -BeExactly ($expected -join ' ')
        }
    }

    Context "Tab completion help test" {
        BeforeAll {
            New-Item -ItemType File (Join-Path ${TESTDRIVE} "pwsh.xml")
            if ($IsWindows) {
                $userHelpRoot = Join-Path $HOME "Documents/PowerShell/Help/"
            } else {
                $userModulesRoot = [System.Management.Automation.Platform]::SelectProductNameForDirectory([System.Management.Automation.Platform+XDG_Type]::USER_MODULES)
                $userHelpRoot = Join-Path $userModulesRoot -ChildPath ".." -AdditionalChildPath "Help"
            }
        }

        It 'Should complete about help topic' {
            $helpName = "about_Splatting"
            $helpFileName = "${helpName}.help.txt"
            $inputScript = "get-help about_spla"
            $culture = "en-US"
            $aboutHelpPathUserScope = Join-Path $userHelpRoot $culture
            $aboutHelpPathAllUsersScope = Join-Path $PSHOME $culture
            $expectedCompletionCount = 0

            ## If help content does not exist, tab completion will not work. So update it first.
            $userHelpPath = Join-Path $aboutHelpPathUserScope $helpFileName
            $userScopeHelp = Test-Path $userHelpPath
            if ($userScopeHelp) {
                $expectedCompletionCount++
            } else {
                Update-Help -Force -ErrorAction SilentlyContinue -Scope 'CurrentUser'
                if (Test-Path $userHelpPath) {
                    $expectedCompletionCount++
                }
            }

            $allUserScopeHelpPath = Test-Path (Join-Path $aboutHelpPathAllUsersScope $helpFileName)
            if ($allUserScopeHelpPath) {
                $expectedCompletionCount++
            }

            $res = TabExpansion2 -inputScript $inputScript -cursorColumn $inputScript.Length
            $res.CompletionMatches | Should -HaveCount $expectedCompletionCount
            $res.CompletionMatches[0].CompletionText | Should -BeExactly $helpName
        }

        It 'Should complete about help topic regardless of culture' {
            try
            {
                ## Save original culture and temporarily set it to da-DK because there's no localized help for da-DK.
                $OriginalCulture = [cultureinfo]::CurrentCulture
                $defaultCulture = "en-US"
                $culture = "da-DK"
                [cultureinfo]::CurrentCulture = $culture
                $helpName = "about_Splatting"
                $helpFileName = "${helpName}.help.txt"

                $aboutHelpPathUserScope = Join-Path $userHelpRoot $culture
                $aboutHelpPathAllUsersScope = Join-Path $PSHOME $culture
                $expectedCompletionCount = 0

                ## If help content does not exist, tab completion will not work. So update it first.
                $userHelpPath = Join-Path $aboutHelpPathUserScope $helpFileName
                $userScopeHelp = Test-Path $userHelpPath
                if ($userScopeHelp) {
                    $expectedCompletionCount++
                }
                else {                    Update-Help -Force -ErrorAction SilentlyContinue -Scope 'CurrentUser'
                    if (Test-Path $userHelpPath) {
                        $expectedCompletionCount++
                    }
                    else {
                        $aboutHelpPathUserScope = Join-Path $userHelpRoot $defaultCulture
                        $aboutHelpPathAllUsersScope = Join-Path $PSHOME $defaultCulture
                        $userHelpDefaultPath = Join-Path $aboutHelpPathUserScope $helpFileName
                        $userDefaultScopeHelp = Test-Path $userHelpDefaultPath

                        if ($userDefaultScopeHelp) {
                            $expectedCompletionCount++
                        }
                    }
                }

                $allUserScopeHelpPath = Test-Path (Join-Path $aboutHelpPathAllUsersScope $helpFileName)
                if ($allUserScopeHelpPath) {
                    $expectedCompletionCount++
                }
                else {
                    $aboutHelpPathAllUsersDefaultScope = Join-Path $PSHOME $defaultCulture
                    $allUsersDefaultScopeHelpPath = Test-Path (Join-Path $aboutHelpPathAllUsersDefaultScope $helpFileName)

                    if ($allUsersDefaultScopeHelpPath) {
                        $expectedCompletionCount++
                    }
                }

                $res = TabExpansion2 -inputScript 'get-help about_spla' -cursorColumn 'get-help about_spla'.Length
                $res.CompletionMatches | Should -HaveCount $expectedCompletionCount
                $res.CompletionMatches[0].CompletionText | Should -BeExactly $helpName
            }
            finally
            {
                [cultureinfo]::CurrentCulture = $OriginalCulture
            }
        }
        It '<Intent>' -TestCases @(
            @{
                Intent = 'Complete help keywords with minimal input'
                Expected = @(
                    "COMPONENT",
                    "DESCRIPTION",
                    "EXAMPLE",
                    "EXTERNALHELP",
                    "FORWARDHELPCATEGORY",
                    "FORWARDHELPTARGETNAME",
                    "FUNCTIONALITY",
                    "INPUTS",
                    "LINK",
                    "NOTES",
                    "OUTPUTS",
                    "PARAMETER",
                    "REMOTEHELPRUNSPACE",
                    "ROLE",
                    "SYNOPSIS"
                )
                TestString = @'
<#
.^
#>
'@
            }
            @{
                Intent = 'Complete help keywords without duplicates'
                Expected = $null
                TestString = @'
<#
.SYNOPSIS
.S^
#>
'@
            }
            @{
                Intent = 'Complete help keywords with allowed duplicates'
                Expected = 'PARAMETER'
                TestString = @'
<#
.PARAMETER
.Paramet^
#>
'@
            }
            @{
                Intent = 'Complete help keyword FORWARDHELPTARGETNAME argument'
                Expected = 'Get-ChildItem'
                TestString = @'
<#
.FORWARDHELPTARGETNAME  Get-Child^
#>
'@
            }
            @{
                Intent = 'Complete help keyword FORWARDHELPCATEGORY argument'
                Expected = 'Cmdlet'
                TestString = @'
<#
.FORWARDHELPCATEGORY C^
#>
'@
            }
            @{
                Intent = 'Complete help keyword REMOTEHELPRUNSPACE argument'
                Expected = 'PSEdition'
                TestString = @'
<#
.REMOTEHELPRUNSPACE PSEditi^
#>
'@
            }
            @{
                Intent = 'Complete help keyword EXTERNALHELP argument'
                Expected = Join-Path $TESTDRIVE "pwsh.xml"
                TestString = @"
<#
.EXTERNALHELP $TESTDRIVE\pwsh.^
#>
"@
            }
            @{
                Intent = 'Complete help keyword PARAMETER argument for script'
                Expected = 'Param1'
                TestString = @'
<#
.PARAMETER ^
#>
param($Param1)
'@
            }
            @{
                Intent = 'Complete help keyword PARAMETER argument for function with help inside'
                Expected = 'param2'
                TestString = @'
function MyFunction ($param1, $param2)
{
<#
.PARAMETER param1
.PARAMETER ^
#>
}
'@
            }
            @{
                Intent = 'Complete help keyword PARAMETER argument for function with help before it'
                Expected = 'param1','param2'
                TestString = @'
<#
.PARAMETER ^
#>
function MyFunction ($param1, $param2)
{
}
'@
            }
            @{
                Intent = 'Complete help keyword PARAMETER argument for advanced function with help inside'
                Expected = 'Param1'
                TestString = @'
function Verb-Noun
{
<#
.PARAMETER ^
#>
    [CmdletBinding()]
    Param
    (
        $Param1
    )

    Begin
    {
    }
    Process
    {
    }
    End
    {
    }
}
'@
            }
            @{
                Intent = 'Complete help keyword PARAMETER argument for nested function with help before it'
                Expected = 'param3','param4'
                TestString = @'
function MyFunction ($param1, $param2)
{
    <#
    .PARAMETER ^
    #>
    function MyFunction2 ($param3, $param4)
    {
    }
}
'@
            }
            @{
                Intent = 'Complete help keyword PARAMETER argument for function inside advanced function'
                Expected = 'param1','param2'
                TestString = @'
function Verb-Noun
{
    Param
    (
        [Parameter()]
        [string[]]
        $ParamA
    )
    Begin
    {
        <#
            .Parameter ^
        #>
        function MyFunction ($param1, $param2)
        {
        }
    }
}
'@
            }
            @{
                Intent = 'Not complete help keyword PARAMETER argument if following function is too far away'
                Expected = $null
                TestString = @'
<#
.PARAMETER ^
#>


function MyFunction ($param1, $param2)
{
}
'@
            }
        ){
            param($Expected, $TestString)
            $CursorIndex = $TestString.IndexOf('^')
            $res = TabExpansion2 -cursorColumn $CursorIndex -inputScript $TestString.Remove($CursorIndex, 1)
            $res.CompletionMatches.CompletionText | Should -BeExactly $Expected
        }
    }

    It 'Should complete module specification keys in using module statement' {
        $res = TabExpansion2 -inputScript 'using module @{'
        $res.CompletionMatches.CompletionText -join ' ' | Should -BeExactly "GUID MaximumVersion ModuleName ModuleVersion RequiredVersion"
        $res.CompletionMatches[0].ToolTip | Should -Not -Be $res.CompletionMatches[0].CompletionText
    }

    It 'Should not fallback to file completion when completing typenames' {
        $Text = '[abcdefghijklmnopqrstuvwxyz]'
        $res = TabExpansion2 -inputScript $Text -cursorColumn ($Text.Length - 1)
        $res.CompletionMatches | Should -HaveCount 0
    }
}

Describe "TabCompletion elevated tests" -Tags CI, RequireAdminOnWindows {
    It "Tab completion UNC path with spaces" -Skip:(!$IsWindows) {
        $Share = New-SmbShare -Temporary -ReadAccess (whoami.exe) -Path C:\ -Name "Test Share"
        $res = TabExpansion2 -inputScript '\\localhost\test'
        $res.CompletionMatches[0].CompletionText | Should -BeExactly "& '\\localhost\Test Share'"
        if ($null -ne $Share)
        {
            Remove-SmbShare -InputObject $Share -Force -Confirm:$false
        }
    }
}

Describe "Tab completion tests with remote Runspace" -Tags Feature,RequireAdminOnWindows {
    BeforeAll {
        $skipTest = -not $IsWindows
        $pendingTest = $IsWindows -and (Test-IsWinWow64)

        if (-not $skipTest -and -not $pendingTest) {
            $session = New-RemoteSession
            $powershell = [powershell]::Create()
            $powershell.Runspace = $session.Runspace

            $testCases = @(
                @{ inputStr = 'Get-Proc'; expected = 'Get-Process' }
                @{ inputStr = 'Get-Process | % ProcessN'; expected = 'ProcessName' }
                @{ inputStr = 'Get-ChildItem alias: | % { $_.Defini'; expected = 'Definition' }
            )

            $testCasesWithAst = @(
                @{ inputStr = '$p = Get-Process; $p | % ProcessN '; bareWord = 'ProcessN'; expected = 'ProcessName' }
                @{ inputStr = 'function bar { Get-Ali* }'; bareWord = 'Get-Ali*'; expected = 'Get-Alias' }
                @{ inputStr = 'function baz ([string]$version, [consolecolor]$name){} baz version bl'; bareWord = 'bl'; expected = 'Black' }
            )
        } else {
            $defaultParameterValues = $PSDefaultParameterValues.Clone()

            if ($skipTest) {
                $PSDefaultParameterValues["It:Skip"] = $true
            } elseif ($pendingTest) {
                $PSDefaultParameterValues["It:Pending"] = $true
            }
        }
    }
    AfterAll {
        if (-not $skipTest -and -not $pendingTest) {
            Remove-PSSession $session
            $powershell.Dispose()
        } else {
            $Global:PSDefaultParameterValues = $defaultParameterValues
        }
    }

    It "Input '<inputStr>' should successfully complete in remote runspace" -TestCases $testCases {
        param($inputStr, $expected)
        $res = [System.Management.Automation.CommandCompletion]::CompleteInput($inputStr, $inputStr.Length, $null, $powershell)
        $res.CompletionMatches.Count | Should -BeGreaterThan 0
        $res.CompletionMatches[0].CompletionText | Should -BeExactly $expected
    }

    It "Input '<inputStr>' should successfully complete via AST in remote runspace" -TestCases $testCasesWithAst {
        param($inputStr, $bareWord, $expected)

        $tokens = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($inputStr, [ref] $tokens, [ref]$null)
        $elementAst = $ast.Find(
            { $args[0] -is [System.Management.Automation.Language.StringConstantExpressionAst] -and $args[0].Value -eq $bareWord },
            $true
        )

        $res = [System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $elementAst.Extent.EndScriptPosition, $null, $powershell)
        $res.CompletionMatches.Count | Should -BeGreaterThan 0
        $res.CompletionMatches[0].CompletionText | Should -BeExactly $expected
    }
}

Describe "WSMan Config Provider tab complete tests" -Tags Feature,RequireAdminOnWindows {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues["it:skip"] = !$IsWindows
    }

    AfterAll {
        $Global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "Tab completion works correctly for Listeners" {
        $path = "wsman:\localhost\listener\listener"
        $res = TabExpansion2 -inputScript $path -cursorColumn $path.Length
        $listener = Get-ChildItem WSMan:\localhost\Listener
        $res.CompletionMatches.Count | Should -Be $listener.Count
        $res.CompletionMatches.ListItemText | Should -BeIn $listener.Name
    }

    It "Tab completion gets dynamic parameters for '<path>' using '<parameter>'" -TestCases @(
        @{path = ""; parameter = "-conn"; expected = "ConnectionURI"},
        @{path = ""; parameter = "-op"; expected = "OptionSet"},
        @{path = ""; parameter = "-au"; expected = "Authentication"},
        @{path = ""; parameter = "-ce"; expected = "CertificateThumbprint"},
        @{path = ""; parameter = "-se"; expected = "SessionOption"},
        @{path = ""; parameter = "-ap"; expected = "ApplicationName"},
        @{path = ""; parameter = "-po"; expected = "Port"},
        @{path = ""; parameter = "-u"; expected = "UseSSL"},
        @{path = "localhost\plugin"; parameter = "-pl"; expected = "Plugin"},
        @{path = "localhost\plugin"; parameter = "-sd"; expected = "SDKVersion"},
        @{path = "localhost\plugin"; parameter = "-re"; expected = "Resource"},
        @{path = "localhost\plugin"; parameter = "-ca"; expected = "Capability"},
        @{path = "localhost\plugin"; parameter = "-xm"; expected = "XMLRenderingType"},
        @{path = "localhost\plugin"; parameter = "-fi"; expected = @("FileName", "File")},
        @{path = "localhost\plugin"; parameter = "-ru"; expected = "RunAsCredential"},
        @{path = "localhost\plugin"; parameter = "-us"; expected = "UseSharedProcess"},
        @{path = "localhost\plugin"; parameter = "-au"; expected = "AutoRestart"},
        @{path = "localhost\plugin"; parameter = "-proc"; expected = "ProcessIdleTimeoutSec"},
        @{path = "localhost\Plugin\microsoft.powershell\Resources\"; parameter = "-re"; expected = "ResourceUri"},
        @{path = "localhost\Plugin\microsoft.powershell\Resources\"; parameter = "-ca"; expected = "Capability"}
    ) {
        param($path, $parameter, $expected)
        $script = "new-item wsman:\$path $parameter"
        $res = TabExpansion2 -inputScript $script
        $res.CompletionMatches | Should -HaveCount $expected.Count
        $completionOptions = ""
        foreach ($completion in $res.CompletionMatches) {
            $completionOptions += $completion.ListItemText
        }
        $completionOptions | Should -BeExactly ([string]::Join("", $expected))
    }

    It "Tab completion get dynamic parameters for initialization parameters" -Pending -TestCases @(
        @{path = "localhost\Plugin\microsoft.powershell\InitializationParameters\"; parameter = "-pa"; expected = @("ParamName", "ParamValue")}
    ) {
        # https://github.com/PowerShell/PowerShell/issues/4744
        # TODO: move to test cases above once working
    }

    It "[CompletionCompleters]::CompleteFilename should not throw NullReferenceException with empty string" {
        # This test verifies the fix for a bug where CompleteFilename("") would throw NullReferenceException
        # because context.RelatedAsts was null when called via the public API
        { [System.Management.Automation.CompletionCompleters]::CompleteFilename("") } | Should -Not -Throw
        
        # Verify it returns results (may be empty or contain current directory items)
        $result = [System.Management.Automation.CompletionCompleters]::CompleteFilename("")
        $result | Should -Not -BeNullOrEmpty
    }
}
