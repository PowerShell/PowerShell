# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Built-in type information tests" -Tag "CI" {
    BeforeAll {
        $iss = [initialsessionstate]::CreateDefault2()
        $rs = [runspacefactory]::CreateRunspace($iss)
        $rs.Open()

        $ps = [powershell]::Create()
        $ps.Runspace = $rs
        $types = $ps.AddCommand("Get-TypeData").Invoke()
    }

    AfterAll {
        $ps.Dispose()
        $rs.Dispose()
    }

    It "Should have correct number of built-in type items in type table" {
        $expected = if ($IsWindows) {
            272
        } else {
            271
        }
        $types.Count | Should -BeExactly $expected
    }

    It "Should have expected member info for 'System.Diagnostics.ProcessModule'" {
        $typeData = $types | Where-Object TypeName -EQ "System.Diagnostics.ProcessModule"
        $typeData | Should -Not -BeNullOrEmpty

        $typeData.Members.Count | Should -BeExactly 6
        $typeData.Members['Size'] | Should -BeOfType System.Management.Automation.Runspaces.ScriptPropertyData
        $typeData.Members['Size'].GetScriptBlock.ToString() | Should -BeExactly '$this.ModuleMemorySize / 1024'
        $typeData.Members['Size'].SetScriptBlock | Should -BeNullOrEmpty
        $typeData.Members['Size'].IsHidden | Should -BeFalse
        $typeData.Members['Size'].Name | Should -Be "Size"

        $typeData.Members['Company'] | Should -BeOfType System.Management.Automation.Runspaces.ScriptPropertyData
        $typeData.Members['Company'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.CompanyName'
        $typeData.Members['Company'].SetScriptBlock | Should -BeNullOrEmpty
        $typeData.Members['Company'].IsHidden | Should -BeFalse
        $typeData.Members['Company'].Name | Should -Be "Company"

        $typeData.Members['FileVersion'] | Should -BeOfType System.Management.Automation.Runspaces.ScriptPropertyData
        $typeData.Members['FileVersion'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.FileVersion'
        $typeData.Members['FileVersion'].SetScriptBlock | Should -BeNullOrEmpty
        $typeData.Members['FileVersion'].IsHidden | Should -BeFalse
        $typeData.Members['FileVersion'].Name | Should -Be "FileVersion"

        $typeData.Members['ProductVersion'] | Should -BeOfType System.Management.Automation.Runspaces.ScriptPropertyData
        $typeData.Members['ProductVersion'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.ProductVersion'
        $typeData.Members['ProductVersion'].SetScriptBlock | Should -BeNullOrEmpty
        $typeData.Members['ProductVersion'].IsHidden | Should -BeFalse
        $typeData.Members['ProductVersion'].Name | Should -Be "ProductVersion"

        $typeData.Members['Description'] | Should -BeOfType System.Management.Automation.Runspaces.ScriptPropertyData
        $typeData.Members['Description'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.FileDescription'
        $typeData.Members['Description'].SetScriptBlock | Should -BeNullOrEmpty
        $typeData.Members['Description'].IsHidden | Should -BeFalse
        $typeData.Members['Description'].Name | Should -Be "Description"

        $typeData.Members['Product'] | Should -BeOfType System.Management.Automation.Runspaces.ScriptPropertyData
        $typeData.Members['Product'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.ProductName'
        $typeData.Members['Product'].SetScriptBlock | Should -BeNullOrEmpty
        $typeData.Members['Product'].IsHidden | Should -BeFalse
        $typeData.Members['Product'].Name | Should -Be "Product"

        $typeData.TypeConverter | Should -BeNullOrEmpty
        $typeData.TypeAdapter | Should -BeNullOrEmpty
        $typeData.IsOverride | Should -BeFalse
        $typeData.SerializationMethod | Should -BeNullOrEmpty
        $typeData.TargetTypeForDeserialization | Should -BeNullOrEmpty
        $typeData.SerializationDepth | Should -Be 0
        $typeData.DefaultDisplayProperty | Should -BeNullOrEmpty
        $typeData.InheritPropertySerializationSet | Should -BeFalse
        $typeData.StringSerializationSource | Should -BeNullOrEmpty
        $typeData.StringSerializationSourceProperty | Should -BeNullOrEmpty
        $typeData.DefaultDisplayPropertySet | Should -BeNullOrEmpty
        $typeData.DefaultKeyPropertySet | Should -BeNullOrEmpty
        $typeData.PropertySerializationSet | Should -BeNullOrEmpty
    }

    It "Should have expected member info for 'System.Management.Automation.ParameterSetMetadata'" {
        $typeData = $types | Where-Object TypeName -EQ "System.Management.Automation.ParameterSetMetadata"
        $typeData | Should -Not -BeNullOrEmpty

        $typeData.Members.Count | Should -BeExactly 1
        $typeData.Members['Flags'] | Should -BeOfType System.Management.Automation.Runspaces.CodePropertyData
        $typeData.Members['Flags'].IsHidden | Should -BeFalse
        $typeData.Members['Flags'].Name | Should -Be "Flags"

        $typeData.TypeConverter | Should -BeNullOrEmpty
        $typeData.TypeAdapter | Should -BeNullOrEmpty
        $typeData.IsOverride | Should -BeFalse
        $typeData.TargetTypeForDeserialization | Should -BeNullOrEmpty
        $typeData.SerializationDepth | Should -Be 0
        $typeData.DefaultDisplayProperty | Should -BeNullOrEmpty
        $typeData.InheritPropertySerializationSet | Should -BeFalse
        $typeData.StringSerializationSource | Should -BeNullOrEmpty
        $typeData.StringSerializationSourceProperty | Should -BeNullOrEmpty
        $typeData.DefaultDisplayPropertySet | Should -BeNullOrEmpty
        $typeData.DefaultKeyPropertySet | Should -BeNullOrEmpty

        $typeData.SerializationMethod | Should -BeExactly "SpecificProperties"
        $typeData.PropertySerializationSet | Should -BeOfType System.Management.Automation.Runspaces.PropertySetData
        [string]::Join(",", $typeData.PropertySerializationSet.ReferencedProperties) | Should -BeExactly "Position,Flags,HelpMessage"
    }

    It "Should have expected member info for 'System.Management.Automation.JobStateEventArgs'" {
        $typeData = $types | Where-Object TypeName -EQ "System.Management.Automation.JobStateEventArgs"
        $typeData | Should -Not -BeNullOrEmpty

        $typeData.Members.Count | Should -BeExactly 0

        $typeData.TypeConverter | Should -BeNullOrEmpty
        $typeData.TypeAdapter | Should -BeNullOrEmpty
        $typeData.IsOverride | Should -BeFalse
        $typeData.SerializationMethod | Should -BeNullOrEmpty
        $typeData.TargetTypeForDeserialization | Should -BeNullOrEmpty
        $typeData.DefaultDisplayProperty | Should -BeNullOrEmpty
        $typeData.InheritPropertySerializationSet | Should -BeFalse
        $typeData.StringSerializationSource | Should -BeNullOrEmpty
        $typeData.StringSerializationSourceProperty | Should -BeNullOrEmpty
        $typeData.DefaultDisplayPropertySet | Should -BeNullOrEmpty
        $typeData.DefaultKeyPropertySet | Should -BeNullOrEmpty

        $typeData.SerializationDepth | Should -Be 2
    }
}
