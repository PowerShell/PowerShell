# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Member invocation logging' -Tags 'CI' {
    BeforeAll {
        $type = [psobject].Assembly.GetType('System.Management.Automation.MemberInvocationLoggingOps')
        $argumentToString = $type.GetMethod(
            'ArgumentToString',
            [System.Reflection.BindingFlags]'NonPublic, Static')
        $maxLoggedArgumentStringLength = [int]$type.GetField(
            'MaxLoggedArgumentStringLength',
            [System.Reflection.BindingFlags]'NonPublic, Static').GetRawConstantValue()
    }

    It 'Keeps short string arguments unchanged' {
        $value = 'short argument'

        $argumentToString.Invoke($null, [object[]]@($value)) | Should -BeExactly $value
    }

    It 'Keeps string arguments at the maximum length unchanged' {
        $value = 'a' * $maxLoggedArgumentStringLength

        $argumentToString.Invoke($null, [object[]]@($value)) | Should -BeExactly $value
    }

    It 'Limits long string arguments' {
        $originalLength = $maxLoggedArgumentStringLength + 904
        $value = 'a' * $originalLength

        $result = $argumentToString.Invoke($null, [object[]]@($value))
        $truncationMarker = "...<truncated; original length: $originalLength>"
        $expectedPrefixLength = $maxLoggedArgumentStringLength - $truncationMarker.Length

        $result.Length | Should -Be $maxLoggedArgumentStringLength
        $result.StartsWith(('a' * $expectedPrefixLength), [System.StringComparison]::Ordinal) | Should -BeTrue
        $result | Should -Match $truncationMarker
    }

    It 'Limits string arguments just over the maximum length' {
        $originalLength = $maxLoggedArgumentStringLength + 1
        $value = 'a' * $originalLength

        $result = $argumentToString.Invoke($null, [object[]]@($value))

        $result.Length | Should -Be $maxLoggedArgumentStringLength
        $result.Length | Should -BeLessThan $value.Length
        $result | Should -Match "<truncated; original length: $originalLength>"
    }
}
