# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Member invocation logging' -Tags 'CI' {
    BeforeAll {
        $type = [psobject].Assembly.GetType('System.Management.Automation.MemberInvocationLoggingOps')
        $argumentToString = $type.GetMethod(
            'ArgumentToString',
            [System.Reflection.BindingFlags]'NonPublic, Static')
    }

    It 'Keeps short string arguments unchanged' {
        $value = 'short argument'

        $argumentToString.Invoke($null, [object[]]@($value)) | Should -BeExactly $value
    }

    It 'Limits long string arguments' {
        $value = 'a' * 5000

        $result = $argumentToString.Invoke($null, [object[]]@($value))

        $result.Length | Should -BeLessThan $value.Length
        $result.StartsWith(('a' * 4096), [System.StringComparison]::Ordinal) | Should -BeTrue
        $result | Should -Match '<truncated; original length: 5000>'
    }
}
