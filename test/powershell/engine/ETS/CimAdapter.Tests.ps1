# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "CIM Objects are adapted properly" -Tag @("CI") {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        
        function getIndex
        {
            param([string[]]$strings,[string]$pattern)
            for ($i = 0; $i -lt $strings.Count; $i++) {
                if ($strings[$i] -like $pattern) {
                    return $i
                }
            }
            return -1
        }

        if ( ! $IsWindows ) {
            $PSDefaultParameterValues["it:pending"] = $true
        }
        else {
            $p = Get-CimInstance win32_process |Select-Object -First 1

            $indexOf_namespaceQualified_Win32Process            = getIndex $p.PSTypeNames "*root?cimv2?Win32_Process"
            $indexOf_namespaceQualified_CimProcess              = getIndex $p.PSTypeNames "*root?cimv2?CIM_Process"
            $indexOf_namespaceQualified_CimLogicalElement       = getIndex $p.PSTypeNames "*root?cimv2?CIM_LogicalElement"
            $indexOf_namespaceQualified_CimManagedSystemElement = getIndex $p.PSTypeNames "*root?cimv2?CIM_ManagedSystemElement"

            $indexOf_className_Win32Process            = getIndex $p.PSTypeNames "*#Win32_Process"
            $indexOf_className_CimProcess              = getIndex $p.PSTypeNames "*#CIM_Process"
            $indexOf_className_CimLogicalElement       = getIndex $p.PSTypeNames "*#CIM_LogicalElement"
            $indexOf_className_CimManagedSystemElement = getIndex $p.PSTypeNames "*#CIM_ManagedSystemElement"
        }
    }
    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "Namespace-qualified Win32_Process is present" -Skip:(!$IsWindows) {
        $indexOf_namespaceQualified_Win32Process | Should -Not -Be (-1)
    }
    It "Namespace-qualified CIM_Process is present" {
        $indexOf_namespaceQualified_CimProcess | Should -Not -Be (-1)
    }
    It "Namespace-qualified CIM_LogicalElement is present" {
        $indexOf_namespaceQualified_CimLogicalElement | Should -Not -Be (-1)
    }
    It "Namespace-qualified CIM_ManagedSystemElement is present" {
        $indexOf_namespaceQualified_CimManagedSystemElement | Should -Not -Be (-1)
    }

    It "Classname of Win32_Process is present" -Skip:(!$IsWindows) {
        $indexOf_className_Win32Process | Should -Not -Be (-1)
    }
    It "Classname of CIM_Process is present" {
        $indexOf_className_CimProcess | Should -Not -Be (-1)
    }
    It "Classname of CIM_LogicalElement is present" {
        $indexOf_className_CimLogicalElement | Should -Not -Be (-1)
    }
    It "Classname of CIM_ManagedSystemElement is present" {
        $indexOf_className_CimManagedSystemElement | Should -Not -Be (-1)
    }

    It "Win32_Process comes after CIM_Process (namespace qualified)" -Skip:(!$IsWindows) {
        $indexOf_namespaceQualified_Win32Process | Should -BeLessThan $indexOf_namespaceQualified_CimProcess
    }
    It "CIM_Process comes after CIM_LogicalElement (namespace qualified)" {
        $indexOf_namespaceQualified_CimProcess | Should -BeLessThan $indexOf_namespaceQualified_CimLogicalElement
    }
    It "CIM_LogicalElement comes after CIM_ManagedSystemElement (namespace qualified)" {
        $indexOf_namespaceQualified_CimLogicalElement | Should -BeLessThan $indexOf_namespaceQualified_CimManagedSystemElement
    }

    It "Win32_Process comes after CIM_Process (classname only)" -Skip:(!$IsWindows) {
        $indexOf_className_Win32Process | Should -BeLessThan $indexOf_className_CimProcess
    }
    It "CIM_Process comes after CIM_LogicalElement (classname only)" {
        $indexOf_className_CimProcess | Should -BeLessThan $indexOf_className_CimLogicalElement
    }
    It "CIM_LogicalElement comes after CIM_ManagedSystemElement (classname only)" {
        $indexOf_className_CimLogicalElement | Should -BeLessThan $indexOf_className_CimManagedSystemElement
    }

    It "Namespace qualified PSTypenames comes after class-only PSTypeNames" -Skip:(!$IsWindows) {
        $indexOf_namespaceQualified_CimManagedSystemElement | Should -BeLessThan $indexOf_className_Win32Process
    }
}
