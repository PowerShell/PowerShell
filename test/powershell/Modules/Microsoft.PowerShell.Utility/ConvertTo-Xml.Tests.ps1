# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "ConvertTo-Xml DRT Unit Tests" -Tags "CI" {
    BeforeAll {
        class fruit {
            [string] $name;
        }

        $customPSObject = [PSCustomObject]@{ "prop1" = "val1"; "prop2" = "val2" }
        $newLine = [System.Environment]::NewLine
    }

    It "Can convert to XML with parameter Depth" {
        $returnObject = $customPSObject | ConvertTo-Xml -Depth 1
        $returnObject | Should -BeOfType System.Xml.XmlDocument
        $expectedValue = '<?xml version="1.0" encoding="utf-8"?><Objects><Object Type="System.Management.Automation.PSCustomObject"><Property Name="prop1" Type="System.String">val1</Property><Property Name="prop2" Type="System.String">val2</Property></Object></Objects>'
        $returnObject.OuterXml | Should -Be $expectedValue
    }

    It "Can convert to XML with parameter NoTypeInformation" {
        $returnObject = $customPSObject | ConvertTo-Xml -NoTypeInformation
        $returnObject | Should -BeOfType System.Xml.XmlDocument
        $expectedValue = '<?xml version="1.0" encoding="utf-8"?><Objects><Object><Property Name="prop1">val1</Property><Property Name="prop2">val2</Property></Object></Objects>'
        $returnObject.OuterXml | Should -Be $expectedValue
    }

    It "Can convert to XML as String" {
        $returnObject = $customPSObject | ConvertTo-Xml -As String
        $returnObject | Should -BeOfType System.String
        $expectedValue = @"
<?xml version="1.0" encoding="utf-8"?>$newLine<Objects>$newLine  <Object Type="System.Management.Automation.PSCustomObject">$newLine    <Property Name="prop1" Type="System.String">val1</Property>$newLine    <Property Name="prop2" Type="System.String">val2</Property>$newLine  </Object>$newLine</Objects>
"@
        $returnObject | Should -Be $expectedValue
    }

    It "Can convert to XML as Stream" {
        $returnObject = $customPSObject | ConvertTo-Xml -As Stream
        $returnObject -is [array] | Should -BeTrue # Cannot use -BeOfType syntax due to issue https://github.com/pester/Pester/issues/386
        $stream1 = '<?xml version="1.0" encoding="utf-8"?>'
        $stream2 = '<Objects>'
        $stream3 = @"
<Object Type="System.Management.Automation.PSCustomObject">$newLine  <Property Name="prop1" Type="System.String">val1</Property>$newLine  <Property Name="prop2" Type="System.String">val2</Property>$newLine</Object>
"@
        $stream4 = '</Objects>'

        $returnObject | Should -HaveCount 4
        $returnObject[0] | Should -Be $stream1
        $returnObject[1] | Should -Be $stream2
        $returnObject[2] | Should -Be $stream3
        $returnObject[3] | Should -Be $stream4
    }

    It "Can convert to XML as Document" {
        $returnObject = $customPSObject | ConvertTo-Xml -As Document -NoTypeInformation
        $returnObject | Should -BeOfType System.Xml.XmlDocument
        $expectedValue = '<?xml version="1.0" encoding="utf-8"?><Objects><Object><Property Name="prop1">val1</Property><Property Name="prop2">val2</Property></Object></Objects>'
        $returnObject.OuterXml | Should -Be $expectedValue
    }

    It "Can be stopped with method StopProcessing" {
		$ps = [PowerShell]::Create()
		$ps.AddCommand("Get-Process")
		$ps.AddCommand("ConvertTo-Xml")
		$ps.AddParameter("Depth", 2)
		$ps.BeginInvoke()
		$ps.Stop()
		$ps.InvocationStateInfo.State | Should -BeExactly "Stopped"
    }

    # these tests just cover aspects that aren't normally exercised being used as a cmdlet
	It "Can read back switch and parameter values using API" {
        Add-Type -AssemblyName "${pshome}/Microsoft.PowerShell.Commands.Utility.dll"

		$cmd = [Microsoft.PowerShell.Commands.ConvertToXmlCommand]::new()
		$cmd.NoTypeInformation = $true
		$cmd.NoTypeInformation | Should -BeTrue
    }

    It "Can serialize integer primitive type" {
        [int] $i = 1
        $x = $i | ConvertTo-Xml
        $x.Objects.Object.Type | Should -BeExactly $i.GetType().ToString()
        $x.Objects.Object."#text" | Should -BeExactly $i
    }

    It "Can serialize ContainerType.Dictionary type" {
        $a = @{foo="bar"}
        $x = $a | ConvertTo-Xml
        $x.Objects.Object.Type | Should -BeExactly $a.GetType().ToString()
        $x.Objects.Object.Property[0].Name | Should -BeExactly "Key"
        $x.Objects.Object.Property[0]."#text" | Should -BeExactly "foo"
        $x.Objects.Object.Property[1].Name | Should -BeExactly "Value"
        $x.Objects.Object.Property[1]."#text" | Should -BeExactly "bar"
    }

    It "Can serialize ContainerType.Enumerable type" {
        $fruit1 = [fruit]::new()
        $fruit1.name = "apple"
        $fruit2 = [fruit]::new()
        $fruit2.name = "banana"
        $x = $fruit1,$fruit2 | ConvertTo-Xml
        $x.Objects.Object | Should -HaveCount 2
        $x.Objects.Object[0].Type | Should -BeExactly $fruit1.GetType().FullName
        $x.Objects.Object[0].Property.Name | Should -BeExactly "name"
        $x.Objects.Object[0].Property."#text" | Should -BeExactly "apple"
        $x.Objects.Object[1].Type | Should -BeExactly $fruit2.GetType().FullName
        $x.Objects.Object[1].Property.Name | Should -BeExactly "name"
        $x.Objects.Object[1].Property."#text" | Should -BeExactly "banana"
    }

    It "Can serialize nested PSCustomObject properties" {
        $nestedObject = [PSCustomObject]@{
            Prop1 = [PSCustomObject]@{
                Prop1 = [PSCustomObject]@{
                    Prop1 = 111
                    Prop2 = 222
                }
                Prop2 = 22
            }
            Prop2 = 2
        }
        $x = $nestedObject | ConvertTo-Xml -Depth 1
        $x.OuterXml | Should -Be '<?xml version="1.0" encoding="utf-8"?><Objects><Object Type="System.Management.Automation.PSCustomObject"><Property Name="Prop1" Type="System.Management.Automation.PSCustomObject"><Property Type="System.String">@{Prop1=; Prop2=22}</Property><Property Name="Prop1" Type="System.Management.Automation.PSNoteProperty">@{Prop1=111; Prop2=222}</Property><Property Name="Prop2" Type="System.Management.Automation.PSNoteProperty">22</Property></Property><Property Name="Prop2" Type="System.Int32">2</Property></Object></Objects>'
    }
}
