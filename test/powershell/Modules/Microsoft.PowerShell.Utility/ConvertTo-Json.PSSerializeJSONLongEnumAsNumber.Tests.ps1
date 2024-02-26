# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'ConvertTo-Json with PSSerializeJSONLongEnumAsNumber' -tags "CI" {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSSerializeJSONLongEnumAsNumber')
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'Should treat enums as integers' {
        enum LongEnum : long {
            LongValue = -1
        }

        enum ULongEnum : ulong {
            ULongValue = 18446744073709551615
        }

        $obj = [Ordered]@{
            Long = [LongEnum]::LongValue
            ULong = [ULongEnum]::ULongValue
        }

        $actual = ConvertTo-Json -InputObject $obj -Compress
        $actual | Should -Be '{"Long":-1,"ULong":18446744073709551615}'

        $actual = ConvertTo-Json -InputObject $obj -EnumsAsStrings -Compress
        $actual | Should -Be '{"Long":"LongValue","ULong":"ULongValue"}'
    }
}
