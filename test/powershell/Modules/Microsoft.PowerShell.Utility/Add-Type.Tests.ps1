# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Add-Type" -Tags "CI" {
    BeforeAll {
        $guid = [Guid]::NewGuid().ToString().Replace("-","")

        $code1 = @"
        namespace Test.AddType
        {
            public class BasicTest1
            {
                public static int Add1(int a, int b)
                {
                    return (a + b);
                }
            }
        }
"@
        $code2 = @"
        namespace Test.AddType
        {
            public class BasicTest2
            {
                public static int Add2(int a, int b)
                {
                    return (a + b);
                }
            }
        }
"@
        $codeFile1 = Join-Path -Path $TestDrive -ChildPath "codeFile1.cs"
        $codeFile2 = Join-Path -Path $TestDrive -ChildPath "codeFile2.cs"

        Set-Content -Path $codeFile1 -Value $code1 -Force
        Set-Content -Path $codeFile2 -Value $code2 -Force
    }

    It "Public 'Language' enumeration contains all members" {
        [Enum]::GetNames("Microsoft.PowerShell.Commands.Language") -join "," | Should -BeExactly "CSharp,VisualBasic"
    }

    It "Should not throw given a simple class definition" {
        { Add-Type -TypeDefinition "public static class foo { }" } | Should -Not -Throw
    }

    It "Can use System.Management.Automation.CmdletAttribute" {
        $code = @"
[System.Management.Automation.Cmdlet("Get", "Thing", ConfirmImpact = System.Management.Automation.ConfirmImpact.High, SupportsPaging = true)]
public class AttributeTest$guid {}
"@
        Add-Type -TypeDefinition $code -PassThru | Should -Not -BeNullOrEmpty
    }

    It "Can load TPA assembly System.Runtime.Serialization.Primitives.dll" {
        Add-Type -AssemblyName 'System.Runtime.Serialization.Primitives' -PassThru | Should -Not -BeNullOrEmpty
    }

    It "Can compile C# files" {

        { [Test.AddType.BasicTest1]::Add1(1, 2) } | Should -Throw -ErrorId "TypeNotFound"
        { [Test.AddType.BasicTest2]::Add2(3, 4) } | Should -Throw -ErrorId "TypeNotFound"

        Add-Type -Path $codeFile1,$codeFile2

        { [Test.AddType.BasicTest1]::Add1(1, 2) } | Should -Not -Throw
        { [Test.AddType.BasicTest2]::Add2(3, 4) } | Should -Not -Throw
    }
}
