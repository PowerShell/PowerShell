# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'Resolve-ErrorRecord tests' -Tag CI {

    It 'Resolve-ErrorRecord resolves $Error[0] and includes InnerException' {
        try {
            1/0
        }
        catch {
        }

        $out = Resolve-ErrorRecord | Out-String
        $out | Should -BeLikeExactly '*InnerException*'
    }

    It 'Resolve-ErrorRecord -Newest works' {
        try {
            1/0
        }
        catch {
        }

        try {
            get-item (new-guid) -ErrorAction SilentlyContinue
        }
        catch {
        }

        $out = Resolve-ErrorRecord -Newest 2
        $out.Count | Should -Be 2
    }

    It 'Resolve-ErrorRecord will accept pipeline input' {
        try {
            1/0
        }
        catch {
        }

        $out = $error[0] | Resolve-ErrorRecord | Out-String
        $out | Should -BeLikeExactly '*-2146233087*'
    }
}
