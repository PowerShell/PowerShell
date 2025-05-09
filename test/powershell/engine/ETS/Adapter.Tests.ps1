# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Adapter Tests" -tags "CI" {
    Context "Property Adapter Tests" {
        BeforeAll {
            $pso  = [System.Diagnostics.Process]::GetCurrentProcess()
            $processName = $pso.Name

            if(-not ('TestCodeMethodClass' -as "type"))
            {
                class TestCodeMethodClass {
                    static [int] TestCodeMethod([PSObject] $target, [int] $i)
                    {
                        return 1;
                    }
                }
            }

            $psmemberset = New-Object System.Management.Automation.PSMemberSet 'setname1'
            $psmemberset | Add-Member -MemberType NoteProperty -Name NoteName -Value 1
            $testmethod = [TestCodeMethodClass].GetMethod("TestCodeMethod")
            $psmemberset | Add-Member -MemberType CodeMethod -Name TestCodeMethod -Value $testmethod

            $document = New-Object System.Xml.XmlDocument
            $document.LoadXml("<book ISBN='12345'><title>Pride And Prejudice</title><price>19.95</price></book>")
            $doc = $document.DocumentElement
        }

        It "Can get a Dotnet parameterized property" {
            $col  = $pso.psobject.Properties.Match("*")
            $prop = $col.psobject.Members["Item"]
            $prop | Should -Not -BeNullOrEmpty
            $prop.IsGettable | Should -BeTrue
            $prop.IsSettable | Should -BeFalse
            $prop.TypeNameOfValue | Should -Be "System.Management.Automation.PSPropertyInfo"
            $prop.Invoke("ProcessName").Value | Should -Be $processName
        }

        It "Can get a property" {
            $pso.psobject.Properties["ProcessName"] | Should -Not -BeNullOrEmpty
        }

        It "Can access all properties" {
            $props = $pso.psobject.Properties.Match("*")
            $props | Should -Not -BeNullOrEmpty
            $props["ProcessName"].Value | Should -Be $processName
        }

        It "Can invoke a method" {
            $method = $pso.psobject.Methods["ToString"]
            $method.Invoke() | Should -Be ($pso.ToString())
        }

        It "Access a Method via MemberSet adapter" {
            $prop = $psmemberset.psobject.Members["TestCodeMethod"]
            $prop.Invoke(2) | Should -Be 1
        }

        It "Access misc properties via MemberSet adapter" {
            $prop  = $psmemberset.psobject.Properties["NoteName"]
            $prop | Should -Not -BeNullOrEmpty
            $prop.IsGettable | Should -BeTrue
            $prop.IsSettable | Should -BeTrue
            $prop.TypeNameOfValue | Should -Be "System.Int32"
        }

        It "Access all the properties via XmlAdapter" {
            $col  = $doc.psobject.Properties.Match("*")
            $col.Count | Should -Not -Be 0
            $prop = $col["price"]
            $prop | Should -Not -BeNullOrEmpty
        }

        It "Access all the properties via XmlAdapter" {
            $prop  = $doc.psobject.Properties["price"]
            $prop.Value | Should -Be "19.95"
            $prop.IsGettable | Should -Not -BeNullOrEmpty
            $prop.IsSettable | Should -Not -BeNullOrEmpty
            $prop.TypeNameOfValue | Should -Be "System.String"
        }

        It "Call to string on a XmlNode object" {
            $val  = $doc.ToString()
            $val | Should -Be "book"
        }

        It "Calls CodeMethod with void result" {

            class TestCodeMethodInvokationWithVoidReturn {
                [int] $CallCounter

                static [int] IntMethodCM([PSObject] $self) {
                    return $self.CallCounter
                }

                static [void] VoidMethodCM([PSObject] $self) {
                    $self.CallCounter++
                }

                static [Reflection.MethodInfo] GetMethodInfo([string] $name) {
                    return [TestCodeMethodInvokationWithVoidReturn].GetMethod($name)
                }
            }

            Update-TypeData -Force -TypeName TestCodeMethodInvokationWithVoidReturn -MemberType CodeMethod -MemberName IntMethod -Value ([TestCodeMethodInvokationWithVoidReturn]::GetMethodInfo('IntMethodCM'))
            Update-TypeData -Force -TypeName TestCodeMethodInvokationWithVoidReturn -MemberType CodeMethod -MemberName VoidMethod -Value ([TestCodeMethodInvokationWithVoidReturn]::GetMethodInfo('VoidMethodCM'))
            try {
                $o = [TestCodeMethodInvokationWithVoidReturn]::new()
                $o.CallCounter | Should -Be 0
                $o.VoidMethod()
                $o.CallCounter | Should -Be 1

                $o.IntMethod() | Should -Be 1
            }
            finally {
                Remove-TypeData TestCodeMethodInvokationWithVoidReturn
            }
        }

        It "Count and length property works for singletons" {
            # Return magic Count and Length property if it absent.
            $x = 5
            $x.Count | Should -Be 1
            $x.Length | Should -Be 1

            $null.Count | Should -Be 0
            $null.Length | Should -Be 0

            (10).Count | Should -Be 1
            (10).Length | Should -Be 1

            ("a").Count | Should -Be 1
            # The Length property exists in String type, so here we check that we don't break strings.
            ("a").Length | Should -Be 1
            ("aa").Length | Should -Be 2

            ([psobject] @{ foo = 'bar' }).Count | Should -Be 1
            ([psobject] @{ foo = 'bar' }).Length | Should -Be 1

            ([pscustomobject] @{ foo = 'bar' }).Count | Should -Be 1
            ([pscustomobject] @{ foo = 'bar' }).Length | Should -Be 1

            # Return real Count and Length property if it present.
            ([pscustomobject] @{ foo = 'bar'; count = 5 }).Count | Should -Be 5
            ([pscustomobject] @{ foo = 'bar'; length = 5 }).Length | Should -Be 5
        }
    }

    Context "Null Magic Method Adapter Tests" {
        It "ForEach and Where works for Null" {
            $res = $null.ForEach({1})
            $res.Count | Should -Be 0
            $res.GetType().Name | Should -BeExactly "Collection``1"

            $null.Where({$true})
            $res.Count | Should -Be 0
            $res.GetType().Name | Should -BeExactly "Collection``1"
        }
    }

    Context "ForEach Magic Method Adapter Tests" {
        It "Common ForEach magic method tests" -Pending:$true {
        }

        It "ForEach magic method works for singletons" {
            $x = 5
            $x.ForEach({$_}) | Should -Be 5
            (5).ForEach({$_}) | Should -Be 5
            ("a").ForEach({$_}) | Should -BeExactly "a"

            ([pscustomobject]@{ foo = 'bar' }).ForEach({1}) | Should -Be 1

            $x = ([pscustomobject]@{ foo = 'bar' }).ForEach({$_})
            $x.Count | Should -Be 1
            $x[0].foo | Should -BeExactly "bar"

            $x = ([pscustomobject]@{ foo = 'bar' }).ForEach({$_ | Add-Member -NotePropertyName "foo2" -NotePropertyValue "bar2" -PassThru})
            $x.Count | Should -Be 1
            $x[0].foo | Should -BeExactly "bar"
            $x[0].foo2 | Should -BeExactly "bar2"

            # We call ForEach method defined in an object if it is present (not magic ForEach method).
            $x = [pscustomobject]@{ foo = 'bar' }
            $x | Add-Member -MemberType ScriptMethod -Name ForEach -Value {
                param ( [int]$param1 )
                   $param1*2
                } -PassThru -Force
            $x.ForEach(5) | Should -Be 10
        }

        # Pending: https://github.com/PowerShell/PowerShell/issues/6567
        It "ForEach magic method works for dynamic (DLR) things" -Pending:$true {
            Add-TestDynamicType

            $dynObj = [TestDynamic]::new()
            $results = @($dynObj, $dynObj).ForEach('FooProp')
            $results.Count | Should -Be 2
            $results[0] | Should -Be 123
            $results[1] | Should -Be 123

            # TODO: dynamic method calls
        }

        It "Can use PSForEach as an alias for the Foreach magic method" {
            $x = 5
            $x.PSForEach({$_}) | Should -Be 5

            $p = $null.PSForEach{"I didn't run"}
            $p.GetType().Name | Should -BeExactly 'Collection`1'
            $p.Count | Should -BeExactly 0

            ([pscustomobject]@{ Name = 'bar' }).PSForEach({$_.Name}) | Should -BeExactly 'bar'
        }
    }

    Context "Where Magic Method Adapter Tests" {
        It "Common Where magic method tests" -Pending:$true {
        }

        It "Where magic method works for singletons" {
            $x = 5
            $x.Where({$true}) | Should -Be 5
            (5).Where({$true}) | Should -Be 5
            ("a").Where({$true}) | Should -Be "a"

            $x = ([pscustomobject] @{ foo = 'bar' }).Where({$true})
            $x.Count | Should -Be 1
            $x[0].foo | Should -BeExactly "bar"

            $x = ([pscustomobject] @{ foo = 'bar' }).Where({$true}, 0)
            $x.Count | Should -Be 1
            $x[0].foo | Should -BeExactly "bar"

            $x = ([pscustomobject] @{ foo = 'bar' }).Where({$true}, "Default")
            $x.Count | Should -Be 1
            $x[0].foo | Should -BeExactly "bar"

            $x = ([pscustomobject] @{ foo = 'bar' }).Where({$true}, "Default", 0)
            $x.Count | Should -Be 1
            $x[0].foo | Should -BeExactly "bar"

            $x = ([pscustomobject] @{ foo = 'bar' }).Where({$true}, "Default", "0")
            $x.Count | Should -Be 1
            $x[0].foo | Should -BeExactly "bar"

            # We call Where method defined in an object if it is present (not magic Where method).
            $x = [pscustomobject]@{ foo = 'bar' }
            $x | Add-Member -MemberType ScriptMethod -Name Where -Value {
                param ( [int]$param1 )
                   $param1*2
                } -PassThru -Force
            $x.Where(5) | Should -Be 10
        }

        It "Can use PSWhere as an alias for the Where magic method" {
            $x = 5
            $x.PSWhere{$true} | Should -Be 5

            $p = $null.PSWhere{"I didn't run"}
            $p.GetType().Name | Should -BeExactly 'Collection`1'
            $p.Count | Should -BeExactly 0

            ([pscustomobject]@{ Name = 'bar' }).PSWhere({$_.Name}) | ForEach-Object Name | Should -BeExactly 'bar'
        }
    }
}

Describe "Adapter XML Tests" -tags "CI" {
    BeforeAll {
        [xml]$x  = "<root><data/></root>"
        $testCases =
            @{ rval = @{testprop = 1}; value = 'a hash (psobject)' },
            @{ rval = $null;           value = 'a null (codemethod)' },
            @{ rval = 1;               value = 'a int (codemethod)' },
            @{ rval = "teststring";    value = 'a string (codemethod)' },
            @{ rval = @("teststring1", "teststring2");  value = 'a string array (codemethod)' },
            @{ rval = @(1,2); value = 'a int array (codemethod)' },
            @{ rval = [PSObject]::AsPSObject(1); value = 'a int (psobject wrapping)' },
            @{ rval = [PSObject]::AsPSObject("teststring"); value = 'a string (psobject wrapping)' },
            @{ rval = [PSObject]::AsPSObject([psobject]@("teststring1", "teststring2")); value = 'a string array (psobject wrapping)' },
            @{ rval = [PSObject]::AsPSObject(@(1,2)); value = 'int array (psobject wrapping)' }
    }

    Context "Can set XML node property to non-string object" {
        It "rval is <value>" -TestCases $testCases {
            # rval will be implicitly converted to 'string' type
            param($rval)
            {
                { $x.root.data = $rval } | Should -Not -Throw
                $x.root.data | Should -Be [System.Management.Automation.LanguagePrimitives]::ConvertTo($rval, [string])
            }
        }
    }
}

Describe "DataRow and DataRowView Adapter tests" -tags "CI" {

    BeforeAll {
        ## Define the DataTable schema
        $dataTable = [System.Data.DataTable]::new("inputs")
        $dataTable.Locale = [cultureinfo]::InvariantCulture
        $dataTable.Columns.Add("Id", [int]) > $null
        $dataTable.Columns.Add("FirstName", [string]) > $null
        $dataTable.Columns.Add("LastName", [string]) > $null
        $dataTable.Columns.Add("YearsInMS", [int]) > $null

        ## Add data entries
        $dataTable.Rows.Add(@(1, "joseph", "smith", 15)) > $null
        $dataTable.Rows.Add(@(2, "paul", "smith", 15)) > $null
        $dataTable.Rows.Add(@(3, "mary jo", "soe", 5)) > $null
        $dataTable.Rows.Add(@(4, "edmund`todd `n", "bush", 9)) > $null
    }

    Context "DataRow Adapter tests" {

        It "Should be able to access data columns" {
            $row = $dataTable.Rows[0]
            $row.Id | Should -Be 1
            $row.FirstName | Should -Be "joseph"
            $row.LastName | Should -Be "smith"
            $row.YearsInMS | Should -Be 15
        }

        It "DataTable should be enumerable in PowerShell" {
            ## Get the third entry in the data table
            $row = $dataTable | Select-Object -Skip 2 -First 1
            $row.Id | Should -Be 3
            $row.FirstName | Should -Be "mary jo"
            $row.LastName | Should -Be "soe"
            $row.YearsInMS | Should -Be 5
        }
    }

    Context "DataRowView Adapter tests" {

        It "Should be able to access data columns" {
            $rowView = $dataTable.DefaultView[1]
            $rowView.Id | Should -Be 2
            $rowView.FirstName | Should -Be "paul"
            $rowView.LastName | Should -Be "smith"
            $rowView.YearsInMS | Should -Be 15
        }

        It "DataView should be enumerable" {
            $rowView = $dataTable.DefaultView | Select-Object -Last 1
            $rowView.Id | Should -Be 4
            $rowView.FirstName | Should -Be "edmund`todd `n"
            $rowView.LastName | Should -Be "bush"
            $rowView.YearsInMS | Should -Be 9
        }
    }
}

Describe "Base method call on object mapped to PropertyOnlyAdapter should work" -tags "CI" {
    It "Base method call on object of a subclass of 'XmlDocument' -- Add-Type" {
        $code =@'
namespace BaseMethodCallTest.OnXmlDocument {
    public class Foo : System.Xml.XmlDocument {
        public string MyName { get; set; }
        public override void LoadXml(string content) {
            MyName = content;
        }
    }
}
'@
        try {
            $null = [BaseMethodCallTest.OnXmlDocument.Foo]
        } catch {
            Add-Type -TypeDefinition $code
        }

        $foo = [BaseMethodCallTest.OnXmlDocument.Foo]::new()
        $foo.LoadXml('<test>bar</test>')
        $foo.MyName | Should -BeExactly '<test>bar</test>'
        $foo.ChildNodes.Count | Should -Be 0

        ([System.Xml.XmlDocument]$foo).LoadXml('<test>bar</test>')
        $foo.test | Should -BeExactly 'bar'
        $foo.ChildNodes.Count | Should -Be 1
    }

    It "Base method call on object of a subclass of 'XmlDocument' -- PowerShell Class" {
        class XmlDocChild : System.Xml.XmlDocument {
            [string] $MyName
            [void] LoadXml([string]$content) {
                $this.MyName = $content
                # Try to call the base type's .LoadXml() method.
                ([System.Xml.XmlDocument] $this).LoadXml($content)
            }
        }

        $child = [XmlDocChild]::new()
        $child.LoadXml('<test>bar</test>')
        $child.MyName | Should -BeExactly '<test>bar</test>'
        $child.test | Should -BeExactly 'bar'
        $child.ChildNodes.Count | Should -Be 1
    }
}
