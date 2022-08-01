# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Windows platform file signatures" -Tags 'Feature' {

    It "Verifies Get-AuthenticodeSignature returns correct signature for catalog signed file" -Skip:(!$IsWindows) {

        if ($null -eq $env:windir) {
            throw "Expected Windows platform environment path variable '%windir%' not available."
        }

        $filePath = Join-Path -Path $env:windir -ChildPath 'System32\ntdll.dll'
        if (! (Test-Path -Path $filePath)) {
            throw "Expected Windows PowerShell platform module path '$filePath' not found."
        }

        $signature = Get-AuthenticodeSignature -FilePath $filePath
        $signature | Should -Not -BeNullOrEmpty
        $signature.Status | Should -BeExactly 'Valid'
        $signature.SignatureType | Should -BeExactly 'Catalog'
    }
}
