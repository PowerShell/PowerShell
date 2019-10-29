# Copyright (c) Microsoft Corporation. All rights reserved.
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
        $types.Count | Should -BeExactly 273
    }

    It "Should have expected member info for 'System.Diagnostics.ProcessModule'" {
        $typeData = $types | Where-Object TypeName -eq "System.Diagnostics.ProcessModule"
        $typeData | Should -Not -BeNullOrEmpty

        $typeData.Members.Count | Should -BeExactly 6
        $typeData.Members['Size'] | Should -BeOfType "System.Management.Automation.Runspaces.ScriptPropertyData"
        $typeData.Members['Size'].GetScriptBlock.ToString() | Should -BeExactly '$this.ModuleMemorySize / 1024'
        $typeData.Members['Size'].SetScriptBlock | Should -Be $null
        $typeData.Members['Size'].IsHidden | Should -Be $false
        $typeData.Members['Size'].Name | Should -Be "Size"

        $typeData.Members['Company'] | Should -BeOfType "System.Management.Automation.Runspaces.ScriptPropertyData"
        $typeData.Members['Company'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.CompanyName'
        $typeData.Members['Company'].SetScriptBlock | Should -Be $null
        $typeData.Members['Company'].IsHidden | Should -Be $false
        $typeData.Members['Company'].Name | Should -Be "Company"

        $typeData.Members['FileVersion'] | Should -BeOfType "System.Management.Automation.Runspaces.ScriptPropertyData"
        $typeData.Members['FileVersion'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.FileVersion'
        $typeData.Members['FileVersion'].SetScriptBlock | Should -Be $null
        $typeData.Members['FileVersion'].IsHidden | Should -Be $false
        $typeData.Members['FileVersion'].Name | Should -Be "FileVersion"

        $typeData.Members['ProductVersion'] | Should -BeOfType "System.Management.Automation.Runspaces.ScriptPropertyData"
        $typeData.Members['ProductVersion'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.ProductVersion'
        $typeData.Members['ProductVersion'].SetScriptBlock | Should -Be $null
        $typeData.Members['ProductVersion'].IsHidden | Should -Be $false
        $typeData.Members['ProductVersion'].Name | Should -Be "ProductVersion"

        $typeData.Members['Description'] | Should -BeOfType "System.Management.Automation.Runspaces.ScriptPropertyData"
        $typeData.Members['Description'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.FileDescription'
        $typeData.Members['Description'].SetScriptBlock | Should -Be $null
        $typeData.Members['Description'].IsHidden | Should -Be $false
        $typeData.Members['Description'].Name | Should -Be "Description"

        $typeData.Members['Product'] | Should -BeOfType "System.Management.Automation.Runspaces.ScriptPropertyData"
        $typeData.Members['Product'].GetScriptBlock.ToString() | Should -BeExactly '$this.FileVersionInfo.ProductName'
        $typeData.Members['Product'].SetScriptBlock | Should -Be $null
        $typeData.Members['Product'].IsHidden | Should -Be $false
        $typeData.Members['Product'].Name | Should -Be "Product"

        $typeData.TypeConverter | Should -Be $null
        $typeData.TypeAdapter | Should -Be $null
        $typeData.IsOverride | Should -Be $false
        $typeData.SerializationMethod | Should -Be $null
        $typeData.TargetTypeForDeserialization | Should -Be $null
        $typeData.SerializationDepth | Should -Be 0
        $typeData.DefaultDisplayProperty | Should -Be $null
        $typeData.InheritPropertySerializationSet | Should -Be $false
        $typeData.StringSerializationSource | Should -Be $null
        $typeData.StringSerializationSourceProperty | Should -Be $null
        $typeData.DefaultDisplayPropertySet | Should -Be $null
        $typeData.DefaultKeyPropertySet | Should -Be $null
        $typeData.PropertySerializationSet | Should -Be $null
    }

    It "Should have expected member info for 'System.Management.Automation.ParameterSetMetadata'" {
        $typeData = $types | Where-Object TypeName -eq "System.Management.Automation.ParameterSetMetadata"
        $typeData | Should -Not -BeNullOrEmpty

        $typeData.Members.Count | Should -BeExactly 1
        $typeData.Members['Flags'] | Should -BeOfType "System.Management.Automation.Runspaces.CodePropertyData"
        $typeData.Members['Flags'].IsHidden | Should -Be $false
        $typeData.Members['Flags'].Name | Should -Be "Flags"

        $typeData.TypeConverter | Should -Be $null
        $typeData.TypeAdapter | Should -Be $null
        $typeData.IsOverride | Should -Be $false
        $typeData.TargetTypeForDeserialization | Should -Be $null
        $typeData.SerializationDepth | Should -Be 0
        $typeData.DefaultDisplayProperty | Should -Be $null
        $typeData.InheritPropertySerializationSet | Should -Be $false
        $typeData.StringSerializationSource | Should -Be $null
        $typeData.StringSerializationSourceProperty | Should -Be $null
        $typeData.DefaultDisplayPropertySet | Should -Be $null
        $typeData.DefaultKeyPropertySet | Should -Be $null

        $typeData.SerializationMethod | Should -BeExactly "SpecificProperties"
        $typeData.PropertySerializationSet | Should -BeOfType "System.Management.Automation.Runspaces.PropertySetData"
        [string]::Join(",", $typeData.PropertySerializationSet.ReferencedProperties) | Should -BeExactly "Position,Flags,HelpMessage"
    }

    It "Should have expected member info for 'System.Management.Automation.JobStateEventArgs'" {
        $typeData = $types | Where-Object TypeName -eq "System.Management.Automation.JobStateEventArgs"
        $typeData | Should -Not -BeNullOrEmpty

        $typeData.Members.Count | Should -BeExactly 0

        $typeData.TypeConverter | Should -Be $null
        $typeData.TypeAdapter | Should -Be $null
        $typeData.IsOverride | Should -Be $false
        $typeData.SerializationMethod | Should -Be $null
        $typeData.TargetTypeForDeserialization | Should -Be $null
        $typeData.DefaultDisplayProperty | Should -Be $null
        $typeData.InheritPropertySerializationSet | Should -Be $false
        $typeData.StringSerializationSource | Should -Be $null
        $typeData.StringSerializationSourceProperty | Should -Be $null
        $typeData.DefaultDisplayPropertySet | Should -Be $null
        $typeData.DefaultKeyPropertySet | Should -Be $null

        $typeData.SerializationDepth | Should -Be 2
    }
}
