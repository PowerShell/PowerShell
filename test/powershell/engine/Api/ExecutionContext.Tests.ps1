# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Basic ExecutionContext API tests' -Tags 'CI' {
    Context 'Runspace::EngineIntrinsics' {
        It 'can get execution context via API' {
            [runspace]::DefaultRunspace.EngineIntrinsics | Should -Be $ExecutionContext
        }
    }
}
