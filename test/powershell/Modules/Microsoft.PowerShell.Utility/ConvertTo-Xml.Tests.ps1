Describe "ConvertTo-Xml DRT Unit Tests" -Tags "CI" {
    $customPSObject = [pscustomobject]@{ "prop1" = "val1"; "prop2" = "val2" }
    $newLine = [System.Environment]::NewLine
    It "Test convertto-xml with a depth parameter" {
        $returnObject = $customPSObject | ConvertTo-Xml -Depth 1
        $returnObject -is [System.Xml.XmlDocument] | Should Be $true
        #$xml = [System.Xml.XmlDocument]$returnObject
        $expectedValue = '<?xml version="1.0" encoding="utf-8"?><Objects><Object Type="System.Management.Automation.PSCustomObject">' + '<Property Name="prop1" Type="System.String">val1</Property><Property Name="prop2" Type="System.String">val2</Property></Object></Objects>'
        $returnObject.OuterXml | Should Be $expectedValue
    }

    It "Test convertto-xml with notypeinfo parameter" {
        $returnObject = $customPSObject | ConvertTo-Xml -NoTypeInformation
        $returnObject -is [System.Xml.XmlDocument] | Should Be $true
        $expectedValue = '<?xml version="1.0" encoding="utf-8"?><Objects><Object>' + '<Property Name="prop1">val1</Property><Property Name="prop2">val2</Property></Object></Objects>'
        $returnObject.OuterXml | Should Be $expectedValue
    }

    It "Test convertto-xml as String" {
        $returnObject = $customPSObject | ConvertTo-Xml -As String
        $expectedValue = @"
<?xml version="1.0" encoding="utf-8"?>$newLine<Objects>$newLine  <Object Type="System.Management.Automation.PSCustomObject">$newLine    <Property Name="prop1" Type="System.String">val1</Property>$newLine    <Property Name="prop2" Type="System.String">val2</Property>$newLine  </Object>$newLine</Objects>
"@
        $returnObject -is [System.String] | Should Be $true
        $returnObject | Should Be $expectedValue
        #$returnObject.Trim($newLine) | Should Be $expectedValue.Trim($newLine)
    }

    It "Test convertto-xml as Stream" {
        $returnObject = $customPSObject | ConvertTo-Xml -As Stream
        $returnObject -is [System.Array] | Should Be $true
        $stream1 = '<?xml version="1.0" encoding="utf-8"?>'
        $stream2 = '<Objects>'
        $stream3 = @"
<Object Type="System.Management.Automation.PSCustomObject">$newLine  <Property Name="prop1" Type="System.String">val1</Property>$newLine  <Property Name="prop2" Type="System.String">val2</Property>$newLine</Object>
"@
        $stream4 = '</Objects>'

        $returnObject.Count | Should Be 4
        $returnObject[0] | Should Be $stream1
        $returnObject[1] | Should Be $stream2
        $returnObject[2] | Should Be $stream3
        $returnObject[3] | Should Be $stream4
    }

    It "Test convertto-xml as Document" {
        $returnObject = $customPSObject | ConvertTo-Xml -As Document -NoTypeInformation
        $returnObject -is [System.Xml.XmlDocument] | Should Be $true
        $expectedValue = '<?xml version="1.0" encoding="utf-8"?><Objects><Object><Property Name="prop1">val1</Property><Property Name="prop2">val2</Property></Object></Objects>'
        $returnObject.OuterXml | Should Be $expectedValue
    }
}

