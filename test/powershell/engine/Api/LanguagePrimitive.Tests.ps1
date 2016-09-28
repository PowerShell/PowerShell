Describe "Language Primitive Tests" -Tags "CI" {
    It "Equality comparison with string and non-numeric type should not be culture sensitive" {
        $date = [datetime]'2005,3,10'
        $val = [System.Management.Automation.LanguagePrimitives]::Equals($date, "3/10/2005")
        $val | Should Be $true
    }

    It "Test conversion of an PSObject with Null Base Object to bool" {
        $mshObj = New-Object psobject
        { [System.Management.Automation.LanguagePrimitives]::ConvertTo($mshObj, [bool]) } | Should Be $true             
    }

    It "Test conversion of an PSObject with Null Base Object to string" {
        $mshObj = New-Object psobject
        { [System.Management.Automation.LanguagePrimitives]::ConvertTo($mshObj, [string]) -eq "" } | Should Be $true 
    }

    It "Test conversion of an PSObject with Null Base Object to object" {
        $mshObj = New-Object psobject
        { $mshObj -eq [System.Management.Automation.LanguagePrimitives]::ConvertTo($mshObj, [Object]) } | Should Be $true
    }

    It "Test Conversion of an IEnumerable to object[]" {
        $col = [System.Diagnostics.Process]::GetCurrentProcess().Modules
        $ObjArray = [System.Management.Automation.LanguagePrimitives]::ConvertTo($col, [object[]])
        $ObjArray.Length | Should Be $col.Count
    }
}
