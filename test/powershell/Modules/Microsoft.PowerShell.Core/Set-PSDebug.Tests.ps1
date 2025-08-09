# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Set-PSDebug" -Tags "CI" {
    Context "Tracing can be used" {
        AfterEach {
            Set-PSDebug -Off
        }

        It "Should be able to go through the tracing options" {
            { Set-PSDebug -Trace 0 } | Should -Not -Throw
            { Set-PSDebug -Trace 1 } | Should -Not -Throw
            { Set-PSDebug -Trace 2 } | Should -Not -Throw
        }

        It "Should be able to set strict" {
            { Set-PSDebug -Strict } | Should -Not -Throw
        }
        
        It "Should skip magic extents created by pwsh" {
            class ClassWithDefaultCtor {
                MyMethod() { }
            }
            
            { 
                Set-PSDebug -Trace 1
                [ClassWithDefaultCtor]::new()
            } | Should -Not -Throw
        }
    }
}
