# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Tests conversion of deserialized types to original type using object properties." -Tags "CI" {
    BeforeAll {
        # Create new types and test functions.
        $type1,$type2,$type3,$type4 = Add-Type -PassThru -TypeDefinition @'
        public class test1
        {
            public string name;
            public int port;
            public string scriptText;
        }

        public class test2
        {
            private string name;
            private int port;
            private string scriptText;

            public string Name
            {
                get { return name; }
                set { name = value; }
            }

            public int Port
            {
                get { return port; }
                set { port = value; }
            }

            public string ScriptText
            {
                get { return scriptText; }
                set { scriptText = value; }
            }
        }

        public class test3
        {
            private string name = "default";
            private int port = 80;
            private string scriptText = "1..6";

            public string Name
            {
                get { return name; }
                set { name = value; }
            }

            public int Port
            {
                get { return port; }
                set { port = value; }
            }

            public string ScriptText
            {
                get { return scriptText; }
            }
        }

        public class test4
        {
            private string name = "default";
            private int port = 80;
            private string scriptText = "1..6";

            public string Name
            {
                get { return name; }
                set { name = value; }
            }

            public int Port
            {
                get { return port; }
                set { port = value; }
            }

            internal void Compute()
            {
                scriptText = scriptText + " Computed";
            }
        }
'@
    }

    Context 'Type conversion and parameter binding of deserialized Type case 1: type definition contains public fields' {
        BeforeAll {
            $t1 = New-Object test1 -Property @{name="TestName1";port=80;scriptText="1..5"}
            $s = [System.Management.Automation.PSSerializer]::Serialize($t1)
            $dst1 = [System.Management.Automation.PSSerializer]::Deserialize($s)
        }

        It 'Type casts should succeed.' {
            { $tc1 = [test1]$dst1 } | Should -Not -Throw
        }

        It 'Parameter bindings should succeed.' {

            function test-1
            {
                param(
                    [parameter(position=0, mandatory=1)]
                    [test1] $test
                )

                $test | Format-List | Out-String
            }
            { test-1 $dst1 } | Should -Not -Throw
        }
    }

    Context 'Type conversion and parameter binding of deserialized Type case 2: type definition contains public properties' {
        BeforeAll {
            $t2 = New-Object test2 -Property @{Name="TestName2";Port=80;ScriptText="1..5"}
            $s = [System.Management.Automation.PSSerializer]::Serialize($t2)
            $dst2 = [System.Management.Automation.PSSerializer]::Deserialize($s)
        }
        It 'Type casts should succeed.' {
            { $tc2 = [test2]$dst2 } | Should -Not -Throw
        }

        It 'Parameter bindings should succeed.' {
            function test-2
            {
                param(
                    [parameter(position=0, mandatory=1)]
                    [test2] $test
                )

                $test | Format-List | Out-String
            }
            { test-2 $dst2 } | Should -Not -Throw
        }
    }

    Context 'Type conversion and parameter binding of deserialized Type case 1: type definition contains 2 public properties and 1 read only property' {
        BeforeAll {
            $t3 = New-Object test3 -Property @{Name="TestName3";Port=80}
            $s = [System.Management.Automation.PSSerializer]::Serialize($t3)
            $dst3 = [System.Management.Automation.PSSerializer]::Deserialize($s)
        }

        It 'Type casts should fail.' {
            { $tc3 = [test3]$dst3 } | Should -Throw -ErrorId 'InvalidCastConstructorException'
        }

        It 'Parameter bindings should fail.' {

            function test-3
            {
                param(
                    [parameter(position=0, mandatory=1)]
                    [test3] $test
                )

                $test | Format-List | Out-String
            }

            { test-3 $dst3 } | Should -Throw -ErrorId 'ParameterArgumentTransformationError,test-3'
        }
    }

    Context 'Type conversion and parameter binding of deserialized Type case 1: type definition contains 2 public properties' {
        BeforeAll {
            $t4 = New-Object test4 -Property @{Name="TestName4";Port=80}
            $s = [System.Management.Automation.PSSerializer]::Serialize($t4)
            $dst4 = [System.Management.Automation.PSSerializer]::Deserialize($s)
        }

        It 'Type casts should succeed.' {
            { $tc4 = [test4]$dst4 } | Should -Not -Throw
        }

        It 'Parameter bindings should succeed.' {
            function test-4
            {
                param(
                    [parameter(position=0, mandatory=1)]
                    [test4] $test
                )

                $test | Format-List | Out-String
            }
            { test-4 $dst4 } | Should -Not -Throw
        }
    }

    Context 'Type conversion and parameter binding of deserialized Powershell class with default constructor' {
        BeforeAll {
            class PSClass1 {
                [string] $name = "PSClassName1"
                [int] $port = 80
                [string] $scriptText = "1..6"
            }

            $t5 = [PSClass1]::new()
            $s = [System.Management.Automation.PSSerializer]::Serialize($t5)
            $dst5 = [System.Management.Automation.PSSerializer]::Deserialize($s)
        }

        It 'Type casts should succeed.' {

            { $tc5 = [PSClass1]$dst5 } | Should -Not -Throw
        }

        It 'Parameter bindings should succeed.' {
            function test-PSClass1
            {
                param(
                    [parameter(position=0, mandatory=1)]
                    [PSClass1] $test
                )

                $test | Format-List | Out-String
            }
            { test-PSClass1 $dst5 } | Should -Not -Throw
        }
    }

    Context 'Type conversion and parameter binding of deserialized Powershell class with a defualt constructor and a constructor' {
        BeforeAll {
            class PSClass2 {
                [string] $name = "default"
                [int] $port = 80
                [string] $scriptText = "1..6"
                PSClass2() {}
                PSClass2([string] $name1, [int] $port1, [string] $scriptText1)
                {
                    $this.name = $name1
                    $this.port = $port1
                    $this.scriptText = $scriptText1
                }
            }
            $t6 = [PSClass2]::new("PSClassName2", 80, "1..5")
            $s = [System.Management.Automation.PSSerializer]::Serialize($t6)
            $dst6 = [System.Management.Automation.PSSerializer]::Deserialize($s)
        }

        It 'Type casts should succeed.' {
            { $tc6 = [PSClass2]$dst6 } | Should -Not -Throw
        }

        It 'Parameter bindings should succeed.' {
            function test-PSClass2
            {
                param(
                    [parameter(position=0, mandatory=1)]
                    [PSClass2] $test
                )

                $test | Format-List | Out-String
            }
            { test-PSClass2 $dst6 } | Should -Not -Throw
        }
    }
}
