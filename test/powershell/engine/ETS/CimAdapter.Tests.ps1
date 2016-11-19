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

try {
    if ( ! $IsWindows ) {
        $PSDefaultParameterValues["it:pending"] = $true
    }
    Describe "CIM Objects are adapted properly" -Tag @("CI") {
        BeforeAll {
            if ( ! $IsWindows ) {
                return
            }
            $p = get-ciminstance win32_process |Select-object -first 1

            $indexOf_namespaceQualified_Win32Process            = getIndex $p.PSTypeNames "*root?cimv2?Win32_Process"
            $indexOf_namespaceQualified_CimProcess              = getIndex $p.PSTypeNames "*root?cimv2?CIM_Process"
            $indexOf_namespaceQualified_CimLogicalElement       = getIndex $p.PSTypeNames "*root?cimv2?CIM_LogicalElement"
            $indexOf_namespaceQualified_CimManagedSystemElement = getIndex $p.PSTypeNames "*root?cimv2?CIM_ManagedSystemElement"

            $indexOf_className_Win32Process            = getIndex $p.PSTypeNames "*#Win32_Process"
            $indexOf_className_CimProcess              = getIndex $p.PSTypeNames "*#CIM_Process"
            $indexOf_className_CimLogicalElement       = getIndex $p.PSTypeNames "*#CIM_LogicalElement"
            $indexOf_className_CimManagedSystemElement = getIndex $p.PSTypeNames "*#CIM_ManagedSystemElement"
        }
        AfterAll {
            $PSDefaultParameterValues.Remove("it:pending")
        }

        It "Namespace-qualified Win32_Process is present" -skip:(!$IsWindows) {
            $indexOf_namespaceQualified_Win32Process |Should not Be (-1)
        }
        It "Namespace-qualified CIM_Process is present" {
            $indexOf_namespaceQualified_CimProcess |Should not Be (-1)
        }
        It "Namespace-qualified CIM_LogicalElement is present" {
            $indexOf_namespaceQualified_CimLogicalElement |Should not Be (-1)
        }
        It "Namespace-qualified CIM_ManagedSystemElement is present" {
            $indexOf_namespaceQualified_CimManagedSystemElement |Should not Be (-1)
        }

        It "Classname of Win32_Process is present" -skip:(!$IsWindows) {
            $indexOf_className_Win32Process |Should not Be (-1)
        }
        It "Classname of CIM_Process is present" {
            $indexOf_className_CimProcess |Should not Be (-1)
        }
        It "Classname of CIM_LogicalElement is present" {
            $indexOf_className_CimLogicalElement |Should not Be (-1)
        }
        It "Classname of CIM_ManagedSystemElement is present" {
            $indexOf_className_CimManagedSystemElement |Should not Be (-1)
        }

        It "Win32_Process comes after CIM_Process (namespace qualified)" -skip:(!$IsWindows) {
            $indexOf_namespaceQualified_Win32Process |should belessthan $indexOf_namespaceQualified_CimProcess
        }
        It "CIM_Process comes after CIM_LogicalElement (namespace qualified)" {
            $indexOf_namespaceQualified_CimProcess |should belessthan $indexOf_namespaceQualified_CimLogicalElement
        }
        It "CIM_LogicalElement comes after CIM_ManagedSystemElement (namespace qualified)" {
            $indexOf_namespaceQualified_CimLogicalElement |should belessthan $indexOf_namespaceQualified_CimManagedSystemElement
        }

        It "Win32_Process comes after CIM_Process (classname only)" -skip:(!$IsWindows) {
            $indexOf_className_Win32Process |should belessthan $indexOf_className_CimProcess
        }
        It "CIM_Process comes after CIM_LogicalElement (classname only)" {
            $indexOf_className_CimProcess |should belessthan $indexOf_className_CimLogicalElement
        }
        It "CIM_LogicalElement comes after CIM_ManagedSystemElement (classname only)" {
            $indexOf_className_CimLogicalElement |should belessthan $indexOf_className_CimManagedSystemElement
        }

        It "Namespace qualified PSTypenames comes after class-only PSTypeNames" -skip:(!$IsWindows) {
            $indexOf_namespaceQualified_CimManagedSystemElement |should belessthan $indexOf_className_Win32Process
        }
    }
}
finally {
    $PSDefaultParameterValues.Remove("it:pending")
}
