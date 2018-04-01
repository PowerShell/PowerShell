# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "ConvertTo-Xml DRT Unit Tests" -Tags "CI" {
    BeforeAll {
        class fruit {
            [string] $name;
        }
    }

    $customPSObject = [pscustomobject]@{ "prop1" = "val1"; "prop2" = "val2" }
    $newLine = [System.Environment]::NewLine
    It "Test convertto-xml with a depth parameter" {
        $returnObject = $customPSObject | ConvertTo-Xml -Depth 1
        $returnObject -is [System.Xml.XmlDocument] | Should -BeTrue
        #$xml = [System.Xml.XmlDocument]$returnObject
        $expectedValue = '<?xml version="1.0" encoding="utf-8"?><Objects><Object Type="System.Management.Automation.PSCustomObject">' + '<Property Name="prop1" Type="System.String">val1</Property><Property Name="prop2" Type="System.String">val2</Property></Object></Objects>'
        $returnObject.OuterXml | Should -Be $expectedValue
    }

    It "Test convertto-xml with notypeinfo parameter" {
        $returnObject = $customPSObject | ConvertTo-Xml -NoTypeInformation
        $returnObject -is [System.Xml.XmlDocument] | Should -BeTrue
        $expectedValue = '<?xml version="1.0" encoding="utf-8"?><Objects><Object>' + '<Property Name="prop1">val1</Property><Property Name="prop2">val2</Property></Object></Objects>'
        $returnObject.OuterXml | Should -Be $expectedValue
    }

    It "Test convertto-xml as String" {
        $returnObject = $customPSObject | ConvertTo-Xml -As String
        $expectedValue = @"
<?xml version="1.0" encoding="utf-8"?>$newLine<Objects>$newLine  <Object Type="System.Management.Automation.PSCustomObject">$newLine    <Property Name="prop1" Type="System.String">val1</Property>$newLine    <Property Name="prop2" Type="System.String">val2</Property>$newLine  </Object>$newLine</Objects>
"@
        $returnObject -is [System.String] | Should -BeTrue
        $returnObject | Should -Be $expectedValue
        #$returnObject.Trim($newLine) | Should Be $expectedValue.Trim($newLine)
    }

    It "Test convertto-xml as Stream" {
        $returnObject = $customPSObject | ConvertTo-Xml -As Stream
        $returnObject -is [System.Array] | Should -BeTrue
        $stream1 = '<?xml version="1.0" encoding="utf-8"?>'
        $stream2 = '<Objects>'
        $stream3 = @"
<Object Type="System.Management.Automation.PSCustomObject">$newLine  <Property Name="prop1" Type="System.String">val1</Property>$newLine  <Property Name="prop2" Type="System.String">val2</Property>$newLine</Object>
"@
        $stream4 = '</Objects>'

        $returnObject.Count | Should -Be 4
        $returnObject[0] | Should -Be $stream1
        $returnObject[1] | Should -Be $stream2
        $returnObject[2] | Should -Be $stream3
        $returnObject[3] | Should -Be $stream4
    }

    It "Test convertto-xml as Document" {
        $returnObject = $customPSObject | ConvertTo-Xml -As Document -NoTypeInformation
        $returnObject -is [System.Xml.XmlDocument] | Should -BeTrue
        $expectedValue = '<?xml version="1.0" encoding="utf-8"?><Objects><Object><Property Name="prop1">val1</Property><Property Name="prop2">val2</Property></Object></Objects>'
        $returnObject.OuterXml | Should -Be $expectedValue
    }

    It "StopProcessing should work" {
		$ps = [PowerShell]::Create()
		$ps.AddCommand("Get-Process")
		$ps.AddCommand("ConvertTo-Xml")
		$ps.AddParameter("Depth", 2)
		$ps.BeginInvoke()
		$ps.Stop()
		$ps.InvocationStateInfo.State | Should -BeExactly "Stopped"
    }

    # these tests just cover aspects that aren't normally exercised being used as a cmdlet
	It "Can read back switch and parameter values using api" {
        Add-Type -AssemblyName "${pshome}/Microsoft.PowerShell.Commands.Utility.dll"

		$cmd = [Microsoft.PowerShell.Commands.ConvertToXmlCommand]::new()
		$cmd.NoTypeInformation = $true
		$cmd.NoTypeInformation | Should -BeTrue
    }

    It "Serialize primitive type" {
        [int] $i = 1
        $x = $i | ConvertTo-Xml
        $x.Objects.Object.Type | Should -BeExactly $i.GetType().ToString()
        $x.Objects.Object."#text" | Should -BeExactly $i
    }

    It "Serialize ContainerType.Dictionary type" {
        $a = @{foo="bar"}
        $x = $a | ConvertTo-Xml
        $x.Objects.Object.Type | Should -BeExactly $a.GetType().ToString()
        $x.Objects.Object.Property[0].Name | Should -BeExactly "Key"
        $x.Objects.Object.Property[0]."#text" | Should -BeExactly "foo"
        $x.Objects.Object.Property[1].Name | Should -BeExactly "Value"
        $x.Objects.Object.Property[1]."#text" | Should -BeExactly "bar"
    }

    It "Serialize ContainerType.Enumerable type" {
        $fruit1 = [fruit]::new()
        $fruit1.name = "apple"
        $fruit2 = [fruit]::new()
        $fruit2.name = "banana"
        $x = $fruit1,$fruit2 | ConvertTo-Xml
        $x.Objects.Object.Count | Should -BeExactly 2
        $x.Objects.Object[0].Type | Should -BeExactly $fruit1.GetType().FullName
        $x.Objects.Object[0].Property.Name | Should -BeExactly "name"
        $x.Objects.Object[0].Property."#text" | Should -BeExactly "apple"
        $x.Objects.Object[1].Type | Should -BeExactly $fruit2.GetType().FullName
        $x.Objects.Object[1].Property.Name | Should -BeExactly "name"
        $x.Objects.Object[1].Property."#text" | Should -BeExactly "banana"
    }
}

