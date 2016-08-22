Describe "Simple ItemProperty Tests" -Tag "CI" {
    It "Can retrieve the PropertyValue with Get-ItemPropertyValue" {
        Get-ItemPropertyValue -path $TESTDRIVE -Name Attributes | should be "Directory"
    }
    It "Can clear the PropertyValue with Clear-ItemProperty" {
        setup -f file1.txt
        Set-ItemProperty $TESTDRIVE/file1.txt -Name Attributes -Value ReadOnly
        Get-ItemPropertyValue -path $TESTDRIVE/file1.txt -Name Attributes | should match "ReadOnly"
        Clear-ItemProperty $TESTDRIVE/file1.txt -Name Attributes
        Get-ItemPropertyValue -path $TESTDRIVE/file1.txt -Name Attributes | should not match "ReadOnly"
    }
    # these cmdlets are targeted at the windows registry, and don't have an linux equivalent
    Context "Registry targeted cmdlets" {
        It "Copy ItemProperty" -pending { }
        It "Move ItemProperty" -pending { }
        It "New ItemProperty" -pending { }
        It "Rename ItemProperty" -pending { }
    }
}
