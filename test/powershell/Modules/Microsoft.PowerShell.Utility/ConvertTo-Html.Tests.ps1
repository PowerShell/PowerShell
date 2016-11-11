Describe "ConvertTo-Html test" -Tags "CI" {
    $customPSObject = [pscustomobject]@{ "prop1" = "val1"; "prop2" = "val2" } 

    It "Test ConvertTo-Html header with default single property" -Skip:( $IsLinux -or $IsOSX -or $IsCoreCLR ) {
        $returnObject = $customPSObject | Select-Object prop1 | ConvertTo-Html
        $columnHeader = $returnObject.Where{ $_.StartsWith( '<tr><th>' ) }[0]
        $expectedValue = '<tr><th>prop1</th></tr>'
        $columnHeader | Should Be $expectedValue
    }

    It "Test ConvertTo-Html header with matched single property" -Skip:( $IsLinux -or $IsOSX -or $IsCoreCLR ) {
        $returnObject = $customPSObject | Select-Object prop1 | ConvertTo-Html -Property prop*
        $columnHeader = $returnObject.Where{ $_.StartsWith( '<tr><th>' ) }[0]
        $expectedValue = '<tr><th>prop1</th></tr>'
        $columnHeader | Should Be $expectedValue
    }
}
