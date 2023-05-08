# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This is a simple type check to validate that types in PowerShellStandard are present in System.Management.Automation.dll
# It does not check member presence and info, just the properties of the type
Describe "Types referenced by PowerShell Standard should not be missing"  -Tags "CI" {
    BeforeAll {
        $assets = [System.IO.Path]::Combine("$PSScriptRoot", "assets", "standardtypes.csv")
        # The properties of a type which should match PowerShell Standard
        # These are not members of the type
        $typeProperties = "IsCollectible",
            "IsSZArray",
            "IsByRefLike",
            "IsConstructedGenericType",
            "IsGenericType",
            "IsGenericTypeDefinition",
            "IsGenericParameter",
            "IsTypeDefinition",
            "IsSecurityCritical",
            "IsSecuritySafeCritical",
            "IsSecurityTransparent",
            "IsInterface",
            "IsNested",
            "IsArray",
            "IsByRef",
            "IsPointer",
            "IsGenericTypeParameter",
            "IsGenericMethodParameter",
            "IsVariableBoundArray",
            "IsAbstract",
            "IsImport",
            "IsSealed",
            "IsSpecialName",
            "IsClass",
            "IsNestedAssembly",
            "IsNestedFamANDAssem",
            "IsNestedFamily",
            "IsNestedFamORAssem",
            "IsNestedPrivate",
            "IsNestedPublic",
            "IsNotPublic",
            "IsPublic",
            "IsAutoLayout",
            "IsExplicitLayout",
            "IsLayoutSequential",
            "IsAnsiClass",
            "IsAutoClass",
            "IsUnicodeClass",
            "IsCOMObject",
            "IsContextful",
            "IsEnum",
            "IsMarshalByRef",
            "IsPrimitive",
            "IsValueType",
            "IsSignatureType",
            "IsSerializable",
            "IsVisible"

        $tests = Import-Csv $assets | ForEach-Object {
            @{ FullName = $_.FullName; TypeMetaData = $_ }
        }
    }

    It "Type '<FullName>' should be present with correct attributes" -TestCases $tests {
        param ( $FullName, $TypeMetaData )
        $type = [psobject].Assembly.GetType($FullName)
        $type | Should -Not -BeNullOrEmpty
        foreach ( $property in $typeProperties ) {
            if ( $typeMetaData.$property -ne $type.$property ) {
                throw "$property value is not correct"
            }
        }
    }
}
