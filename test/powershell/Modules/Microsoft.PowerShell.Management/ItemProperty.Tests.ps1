# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Simple ItemProperty Tests" -Tag "CI" {
    It "Can retrieve the PropertyValue with Get-ItemPropertyValue" {
        Get-ItemPropertyValue -Path $TESTDRIVE -Name Attributes | Should -Be "Directory"
    }
    It "Can clear the PropertyValue with Clear-ItemProperty" {
        Setup -f file1.txt
        Set-ItemProperty $TESTDRIVE/file1.txt -Name Attributes -Value ReadOnly
        Get-ItemPropertyValue -Path $TESTDRIVE/file1.txt -Name Attributes | Should -Match "ReadOnly"
        Clear-ItemProperty $TESTDRIVE/file1.txt -Name Attributes
        Get-ItemPropertyValue -Path $TESTDRIVE/file1.txt -Name Attributes | Should -Not -Match "ReadOnly"
    }
    # these cmdlets are targeted at the windows registry, and don't have an linux equivalent
    Context "Registry targeted cmdlets" {
        It "Copy ItemProperty" -Pending { }
        It "Move ItemProperty" -Pending { }
        It "New ItemProperty" -Pending { }
        It "Rename ItemProperty" -Pending { }
    }
}
