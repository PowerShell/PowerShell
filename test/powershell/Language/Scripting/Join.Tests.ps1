# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Tests for join operator' -Tags "CI" {
    BeforeAll {
    }

    It 'Join operator can use the custom ToString method defined in ETS' {
        Add-Type -TypeDefinition @"
        namespace TestNS
        {
           public class TestClass
           {
               public override string ToString()
               {
                   return "1";
               }
           }
        }
"@

        $a1=[TestNS.TestClass]::new()
        $a2=[TestNS.TestClass]::new()
        $a3=[TestNS.TestClass]::new()

        $a1,$a2,$a3 -join ";" | Should -BeExactly "1;1;1"

        Update-TypeData -TypeName "TestNS.TestClass" -MemberType ScriptMethod -MemberName "ToString" -Value { return "test"}

        $a1,$a2,$a3 -join ";" | Should -BeExactly "test;test;test"
    }
}
